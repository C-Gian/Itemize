using System.Collections.ObjectModel;
using System.Globalization;
using Microsoft.Maui.Controls;
using System.Linq;


using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel;

namespace Itemize.PageModels;

public partial class MainPageModel : ObservableObject
{
    private const int ExpiringThresholdDays = 5;

    private readonly IPantryStore _pantryStore;
    private readonly RecipeSuggestionService _recipeSuggestionService;
    private readonly FamilyContext _familyContext;

    private bool _isInitialized;
    private bool _isUpdatingFamilySelection;
    private bool _isLoading;
    private List<PantryItem> _currentItems = new();

    public ObservableCollection<Family> Families { get; } = new();

    public IReadOnlyList<PantrySortOption> SortOptions { get; }
        = new List<PantrySortOption>
        {
            new("Alfabetico", PantrySortType.Alphabetical),
            new("Data di scadenza", PantrySortType.ExpirationDate),
            new("Data di aggiunta", PantrySortType.AddedDate),
            new("Categoria", PantrySortType.Category),
        };

    [ObservableProperty]
    private Family? _selectedFamily;

    [ObservableProperty]
    private PantrySortOption? _selectedSortOption;

    [ObservableProperty]
    private ObservableCollection<PantrySection> _pantrySections = new();

    [ObservableProperty]
    private ObservableCollection<PantryItem> _expiringItems = new();

    [ObservableProperty]
    private ObservableCollection<PantryItem> _lowStockItems = new();

    [ObservableProperty]
    private ObservableCollection<RecipeSuggestion> _recipeSuggestions = new();

    [ObservableProperty]
    private PantrySummary _summary = new()
    {
        TotalItems = 0,
        UniqueProducts = 0,
        ExpiringSoonCount = 0,
        ExpiredCount = 0,
        LowStockCount = 0,
    };

    [ObservableProperty]
    private bool _isRefreshing;

    [ObservableProperty]
    private bool _isBusy;

    public MainPageModel(IPantryStore pantryStore, RecipeSuggestionService recipeSuggestionService, FamilyContext familyContext)
    {
        _pantryStore = pantryStore;
        _recipeSuggestionService = recipeSuggestionService;
        _familyContext = familyContext;

        _selectedSortOption = SortOptions.First();

        _pantryStore.PantryChanged += OnPantryChanged;
    }

    [RelayCommand]
    private async Task Appearing()
    {
        if (_isInitialized)
        {
            if (SelectedFamily is not null)
            {
                await RefreshInternalAsync(SelectedFamily.Id);
            }
            return;
        }

        _isInitialized = true;
        await LoadFamiliesAsync();
        if (SelectedFamily is not null)
        {
            await RefreshInternalAsync(SelectedFamily.Id);
        }
    }

    [RelayCommand]
    private async Task Refresh()
    {
        if (SelectedFamily is null)
        {
            return;
        }

        IsRefreshing = true;
        await RefreshInternalAsync(SelectedFamily.Id);
        IsRefreshing = false;
    }

    [RelayCommand]
    private async Task StartShopping()
    {
        if (SelectedFamily is null)
        {
            return;
        }

        await Shell.Current.GoToAsync("//shopping");
    }

    [RelayCommand]
    private async Task EditItem(PantryItem item)
    {
        if (SelectedFamily is null)
        {
            return;
        }

        var result = await Shell.Current.DisplayPromptAsync(
            "Modifica quantità",
            $"Aggiorna la quantità per {item.Name}",
            keyboard: Keyboard.Numeric,
            initialValue: item.Quantity.ToString("0.##", CultureInfo.CurrentCulture));

        if (string.IsNullOrWhiteSpace(result))
        {
            return;
        }

        if (!TryParseQuantity(result, out var quantity) || quantity < 0)
        {
            await AppShell.DisplaySnackbarAsync("Quantità non valida.");
            return;
        }

        var updated = item.Clone();
        updated.Quantity = quantity;

        await _pantryStore.UpdatePantryItemAsync(SelectedFamily.Id, updated);
        await RefreshInternalAsync(SelectedFamily.Id);
        await AppShell.DisplayToastAsync($"{item.Name} aggiornato");
    }

    [RelayCommand]
    private async Task DeleteItem(PantryItem item)
    {
        if (SelectedFamily is null)
        {
            return;
        }

        var confirm = await Shell.Current.DisplayAlert(
            "Rimuovi prodotto",
            $"Vuoi rimuovere {item.Name} dalla dispensa?",
            "Rimuovi",
            "Annulla");

        if (!confirm)
        {
            return;
        }

        await _pantryStore.DeletePantryItemAsync(SelectedFamily.Id, item.Id);
        await RefreshInternalAsync(SelectedFamily.Id);
        await AppShell.DisplayToastAsync($"{item.Name} rimosso");
    }

    partial void OnSelectedFamilyChanged(Family? value)
    {
        if (value is null)
        {
            return;
        }

        if (_isUpdatingFamilySelection)
        {
            return;
        }

        if (_familyContext.SelectedFamilyId != value.Id)
        {
            _familyContext.SelectedFamilyId = value.Id;
        }

        if (_isInitialized)
        {
            _ = RefreshInternalAsync(value.Id);
        }
    }

    partial void OnSelectedSortOptionChanged(PantrySortOption? value)
    {
        if (value is null || !_currentItems.Any())
        {
            return;
        }

        UpdateSections(_currentItems);
    }

    private async Task LoadFamiliesAsync()
    {
        var families = await _pantryStore.GetFamiliesAsync();

        _isUpdatingFamilySelection = true;
        Families.Clear();
        foreach (var family in families)
        {
            Families.Add(family);
        }

        Family? selected = null;
        if (_familyContext.SelectedFamilyId is Guid currentId)
        {
            selected = Families.FirstOrDefault(f => f.Id == currentId);
        }

        SelectedFamily = selected ?? Families.FirstOrDefault();
        _isUpdatingFamilySelection = false;
    }

    private async Task RefreshInternalAsync(Guid familyId)
    {
        if (_isLoading)
        {
            return;
        }

        _isLoading = true;
        try
        {
            IsBusy = true;
            var pantry = await _pantryStore.GetPantryAsync(familyId);
            var items = pantry.Items
                .Where(i => i.State == PantryItemState.Active)
                .Select(i => i.Clone())
                .ToList();

            _currentItems = items;

            Summary = PantrySummary.FromItems(items, ExpiringThresholdDays);
            UpdateHighlights(items);
            UpdateSections(items);
            UpdateRecipes(items);
        }
        finally
        {
            IsBusy = false;
            _isLoading = false;
        }
    }

    private void UpdateHighlights(List<PantryItem> items)
    {
        var today = DateTime.Today;
        var expiring = items
            .Where(i => i.IsExpiringSoon(today, ExpiringThresholdDays) || i.IsExpired(today))
            .OrderBy(i => i.ExpirationDate ?? DateTime.MaxValue)
            .Select(i => i.Clone())
            .ToList();

        ExpiringItems = new ObservableCollection<PantryItem>(expiring);

        var lowStock = items
            .Where(i => i.IsLowStock)
            .OrderBy(i => i.Quantity)
            .Select(i => i.Clone())
            .ToList();

        LowStockItems = new ObservableCollection<PantryItem>(lowStock);
    }

    private void UpdateSections(List<PantryItem> items)
    {
        IEnumerable<PantrySection> sections;
        if (SelectedSortOption?.SortType == PantrySortType.Category)
        {
            sections = items
                .GroupBy(i => string.IsNullOrWhiteSpace(i.Category) ? "Altro" : i.Category)
                .OrderBy(g => g.Key)
                .Select(g => new PantrySection(g.Key, SortItems(g, PantrySortType.Alphabetical)));
        }
        else
        {
            var sorted = SortItems(items, SelectedSortOption?.SortType ?? PantrySortType.Alphabetical);
            sections = new[] { new PantrySection("Tutti i prodotti", sorted) };
        }

        PantrySections = new ObservableCollection<PantrySection>(sections);
    }

    private void UpdateRecipes(List<PantryItem> items)
    {
        var suggestions = _recipeSuggestionService.BuildSuggestions(items);
        RecipeSuggestions = new ObservableCollection<RecipeSuggestion>(suggestions);
    }

    private static IEnumerable<PantryItem> SortItems(IEnumerable<PantryItem> items, PantrySortType sortType)
    {
        return sortType switch
        {
            PantrySortType.ExpirationDate => items.OrderBy(i => i.ExpirationDate ?? DateTime.MaxValue),
            PantrySortType.AddedDate => items.OrderByDescending(i => i.AddedAt),
            PantrySortType.Category => items.OrderBy(i => i.Name),
            _ => items.OrderBy(i => i.Name),
        };
    }

    private static bool TryParseQuantity(string text, out double value)
    {
        return double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value)
            || double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    private void OnPantryChanged(object? sender, PantryChangedEventArgs e)
    {
        if (SelectedFamily?.Id != e.FamilyId)
        {
            return;
        }

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            if (SelectedFamily is not null)
            {
                await RefreshInternalAsync(SelectedFamily.Id);
            }
        });
    }
}
