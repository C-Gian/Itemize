using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;


using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;

namespace Itemize.PageModels;

public partial class ShoppingSessionPageModel : ObservableObject
{
    private readonly IPantryStore _pantryStore;
    private readonly IProductCatalog _productCatalog;
    private readonly FamilyContext _familyContext;

    private bool _isInitialized;
    private bool _isUpdatingSelection;

    public ObservableCollection<Family> Families { get; } = new();

    [ObservableProperty]
    private Family? _selectedFamily;

    [ObservableProperty]
    private ShoppingSession? _session;

    [ObservableProperty]
    private ObservableCollection<ShoppingItem> _items = new();

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _isManualEntryVisible;

    [ObservableProperty]
    private string _manualBarcode = string.Empty;

    [ObservableProperty]
    private string _sessionTitle = string.Empty;

    [ObservableProperty]
    private decimal _estimatedTotal;

    public bool HasActiveSession => Session?.State == ShoppingSessionState.Active;

    public bool HasItems => Items.Any();

    public ShoppingSessionPageModel(IPantryStore pantryStore, IProductCatalog productCatalog, FamilyContext familyContext)
    {
        _pantryStore = pantryStore;
        _productCatalog = productCatalog;
        _familyContext = familyContext;

        _pantryStore.ShoppingSessionChanged += OnShoppingSessionChanged;
        _familyContext.PropertyChanged += OnFamilyContextChanged;
    }

    [RelayCommand]
    private async Task Appearing()
    {
        if (!_isInitialized)
        {
            await LoadFamiliesAsync();
            _isInitialized = true;
        }

        if (SelectedFamily is not null)
        {
            await LoadSessionAsync(SelectedFamily.Id);
        }
    }

    [RelayCommand]
    private Task ToggleManualEntry()
    {
        IsManualEntryVisible = !IsManualEntryVisible;
        return Task.CompletedTask;
    }

    [RelayCommand]
    private async Task StartNewSession()
    {
        if (SelectedFamily is null)
        {
            await AppShell.DisplaySnackbarAsync("Seleziona una famiglia per iniziare.");
            return;
        }

        var defaultTitle = $"Spesa del {DateTime.Now:dd MMMM}";
        var title = await Shell.Current.DisplayPromptAsync(
            "Nuova spesa",
            "Dai un nome alla sessione",
            accept: "Crea",
            cancel: "Annulla",
            initialValue: defaultTitle);

        if (title is null)
        {
            return;
        }

        title = string.IsNullOrWhiteSpace(title) ? defaultTitle : title.Trim();

        var session = await _pantryStore.StartSessionAsync(SelectedFamily.Id, title);
        Session = session;
        SessionTitle = session.Title;
        Items = new ObservableCollection<ShoppingItem>(session.Items.Where(i => !i.IsRemoved).Select(i => i.Clone()));
        EstimatedTotal = Items.Sum(i => i.Price ?? 0m);
        OnPropertyChanged(nameof(HasActiveSession));
        OnPropertyChanged(nameof(HasItems));
        await AppShell.DisplayToastAsync("Sessione di spesa avviata");
    }

    [RelayCommand]
    private async Task ScanProduct()
    {
        if (Session is null)
        {
            await AppShell.DisplaySnackbarAsync("Avvia prima una sessione di spesa.");
            return;
        }

        var products = await _productCatalog.GetProductsAsync();
        if (!products.Any())
        {
            await AppShell.DisplaySnackbarAsync("Catalogo prodotti vuoto.");
            return;
        }

        var options = products
            .Select(p => $"{p.Name} ({p.Brand})")
            .ToArray();

        var selectedLabel = await Shell.Current.DisplayActionSheet(
            "Simula scansione",
            "Annulla",
            null,
            options);

        if (string.IsNullOrWhiteSpace(selectedLabel) || selectedLabel == "Annulla")
        {
            return;
        }

        var product = products.First(p => $"{p.Name} ({p.Brand})" == selectedLabel);
        await PromptAndAddProductAsync(product);
    }

    [RelayCommand]
    private async Task AddManualProduct()
    {
        if (Session is null)
        {
            await AppShell.DisplaySnackbarAsync("Avvia prima una sessione di spesa.");
            return;
        }

        var barcode = ManualBarcode.Trim();
        if (string.IsNullOrWhiteSpace(barcode))
        {
            await AppShell.DisplaySnackbarAsync("Inserisci un codice valido.");
            return;
        }

        var product = await _productCatalog.GetByBarcodeAsync(barcode);
        if (product is null)
        {
            await AppShell.DisplaySnackbarAsync("Prodotto non trovato in catalogo.");
            return;
        }

        ManualBarcode = string.Empty;
        IsManualEntryVisible = false;
        await PromptAndAddProductAsync(product);
    }

    [RelayCommand]
    private async Task RemoveItem(ShoppingItem item)
    {
        if (Session is null)
        {
            return;
        }

        var confirm = await Shell.Current.DisplayAlert(
            "Rimuovi prodotto",
            $"Vuoi rimuovere {item.Product.Name}?",
            "Rimuovi",
            "Annulla");

        if (!confirm)
        {
            return;
        }

        await _pantryStore.RemoveShoppingItemAsync(Session.Id, item.Id);
    }

    [RelayCommand]
    private async Task EditItem(ShoppingItem item)
    {
        if (Session is null)
        {
            return;
        }

        var result = await Shell.Current.DisplayPromptAsync(
            "Modifica quantità",
            $"Aggiorna la quantità per {item.Product.Name}",
            keyboard: Keyboard.Numeric,
            initialValue: item.Quantity.ToString("0.##", CultureInfo.CurrentCulture));

        if (string.IsNullOrWhiteSpace(result))
        {
            return;
        }

        if (!TryParseQuantity(result, out var quantity) || quantity <= 0)
        {
            await AppShell.DisplaySnackbarAsync("Quantità non valida.");
            return;
        }

        var updated = item.Clone();
        updated.Quantity = quantity;
        updated.Price = item.Product.AveragePrice.HasValue
            ? item.Product.AveragePrice.Value * (decimal)quantity
            : item.Price;

        await _pantryStore.UpdateShoppingItemAsync(Session.Id, updated);
    }

    [RelayCommand]
    private async Task ConfirmSession()
    {
        if (Session is null || !Items.Any())
        {
            await AppShell.DisplaySnackbarAsync("Aggiungi almeno un prodotto prima di confermare.");
            return;
        }

        var confirm = await Shell.Current.DisplayAlert(
            "Conferma spesa",
            "I prodotti verranno aggiunti alla dispensa. Confermi?",
            "Conferma",
            "Annulla");

        if (!confirm)
        {
            return;
        }

        await _pantryStore.ConfirmSessionAsync(Session.Id, DateTime.Now);
        await AppShell.DisplayToastAsync("Spesa confermata! I prodotti sono in dispensa.");
    }

    private async Task LoadFamiliesAsync()
    {
        var families = await _pantryStore.GetFamiliesAsync();
        _isUpdatingSelection = true;
        Families.Clear();
        foreach (var family in families)
        {
            Families.Add(family);
        }

        Family? target = null;
        if (_familyContext.SelectedFamilyId is Guid familyId)
        {
            target = Families.FirstOrDefault(f => f.Id == familyId);
        }

        SelectedFamily = target ?? Families.FirstOrDefault();
        _isUpdatingSelection = false;
    }

    private async Task LoadSessionAsync(Guid familyId)
    {
        IsBusy = true;
        try
        {
            var session = await _pantryStore.GetCurrentSessionAsync(familyId);
            Session = session;
            SessionTitle = session?.Title ?? string.Empty;
            if (session is null)
            {
                Items = new ObservableCollection<ShoppingItem>();
                EstimatedTotal = 0;
            }
            else
            {
                var visibleItems = session.Items
                    .Where(i => !i.IsRemoved)
                    .Select(i => i.Clone())
                    .ToList();
                Items = new ObservableCollection<ShoppingItem>(visibleItems);
                EstimatedTotal = visibleItems.Sum(i => i.Price ?? 0m);
            }

            OnPropertyChanged(nameof(HasActiveSession));
            OnPropertyChanged(nameof(HasItems));
        }
        finally
        {
            IsBusy = false;
        }
    }

    partial void OnSelectedFamilyChanged(Family? value)
    {
        if (value is null)
        {
            return;
        }

        if (_isUpdatingSelection)
        {
            return;
        }

        if (_familyContext.SelectedFamilyId != value.Id)
        {
            _familyContext.SelectedFamilyId = value.Id;
        }

        _ = LoadSessionAsync(value.Id);
    }

    partial void OnSessionChanged(ShoppingSession? value)
    {
        OnPropertyChanged(nameof(HasActiveSession));
    }

    partial void OnItemsChanged(ObservableCollection<ShoppingItem> value)
    {
        EstimatedTotal = value.Sum(i => i.Price ?? 0m);
        OnPropertyChanged(nameof(HasItems));
    }

    private async Task PromptAndAddProductAsync(Product product)
    {
        if (Session is null)
        {
            return;
        }

        var quantityInput = await Shell.Current.DisplayPromptAsync(
            product.Name,
            "Quante unità vuoi aggiungere?",
            keyboard: Keyboard.Numeric,
            initialValue: product.DefaultQuantity.ToString("0.##", CultureInfo.CurrentCulture));

        if (string.IsNullOrWhiteSpace(quantityInput))
        {
            return;
        }

        if (!TryParseQuantity(quantityInput, out var quantity) || quantity <= 0)
        {
            await AppShell.DisplaySnackbarAsync("Quantità non valida.");
            return;
        }

        var shoppingItem = new ShoppingItem
        {
            SessionId = Session.Id,
            ProductId = product.Id,
            Product = product,
            Quantity = quantity,
            Unit = product.DefaultUnit,
            Price = product.AveragePrice.HasValue
                ? product.AveragePrice.Value * (decimal)quantity
                : null,
        };

        await _pantryStore.AddOrUpdateShoppingItemAsync(Session.Id, shoppingItem);
    }

    private void OnShoppingSessionChanged(object? sender, ShoppingSessionChangedEventArgs e)
    {
        if (SelectedFamily?.Id != e.FamilyId)
        {
            return;
        }

        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (e.Session.State != ShoppingSessionState.Active)
            {
                Session = null;
                SessionTitle = string.Empty;
                Items = new ObservableCollection<ShoppingItem>();
                EstimatedTotal = 0;
            }
            else
            {
                Session = e.Session;
                SessionTitle = e.Session.Title;
                var visibleItems = e.Session.Items
                    .Where(i => !i.IsRemoved)
                    .Select(i => i.Clone())
                    .ToList();
                Items = new ObservableCollection<ShoppingItem>(visibleItems);
                EstimatedTotal = visibleItems.Sum(i => i.Price ?? 0m);
            }

            OnPropertyChanged(nameof(HasActiveSession));
            OnPropertyChanged(nameof(HasItems));
        });
    }

    private void OnFamilyContextChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(FamilyContext.SelectedFamilyId))
        {
            return;
        }

        if (_familyContext.SelectedFamilyId is Guid id)
        {
            var target = Families.FirstOrDefault(f => f.Id == id);
            if (target is not null && SelectedFamily?.Id != target.Id)
            {
                SelectedFamily = target;
            }
        }
    }

    private static bool TryParseQuantity(string text, out double value)
    {
        return double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value)
            || double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }
}
