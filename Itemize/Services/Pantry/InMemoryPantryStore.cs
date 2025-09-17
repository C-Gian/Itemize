using Microsoft.Maui.Graphics;

using Itemize.Models.Pantry;

namespace Itemize.Services.Pantry;

public class InMemoryPantryStore : IPantryStore, IProductCatalog
{
    private readonly object _syncRoot = new();
    private readonly List<User> _users = new();
    private readonly List<Family> _families = new();
    private readonly List<Product> _products = new();
    private readonly List<ShoppingSession> _sessions = new();


    public InMemoryPantryStore()
    {
        Seed();
    }

    public event EventHandler<PantryChangedEventArgs>? PantryChanged;

    public event EventHandler<ShoppingSessionChangedEventArgs>? ShoppingSessionChanged;

    public Task<IReadOnlyList<Family>> GetFamiliesAsync()
    {
        lock (_syncRoot)
        {
            return Task.FromResult<IReadOnlyList<Family>>(_families.Select(f => f.Clone()).ToList());
        }
    }

    public Task<Pantry> GetPantryAsync(Guid familyId)
    {
        lock (_syncRoot)
        {
            var family = _families.First(f => f.Id == familyId);
            return Task.FromResult(family.Pantry.Clone());
        }
    }

    public Task DeletePantryItemAsync(Guid familyId, Guid pantryItemId)
    {
        lock (_syncRoot)
        {
            var family = _families.First(f => f.Id == familyId);
            var removed = family.Pantry.Items.RemoveAll(i => i.Id == pantryItemId);
            if (removed > 0)
            {
                PantryChanged?.Invoke(this, new PantryChangedEventArgs(familyId));
            }
        }

        return Task.CompletedTask;
    }

    public Task UpdatePantryItemAsync(Guid familyId, PantryItem item)
    {
        lock (_syncRoot)
        {
            var family = _families.First(f => f.Id == familyId);
            var existing = family.Pantry.Items.FirstOrDefault(i => i.Id == item.Id);
            if (existing is null)
            {
                family.Pantry.Items.Add(item.Clone());
            }
            else
            {
                existing.Quantity = item.Quantity;
                existing.Unit = item.Unit;
                existing.ExpirationDate = item.ExpirationDate;
                existing.MinimumThreshold = item.MinimumThreshold;
                existing.Notes = item.Notes;
                existing.State = item.State;
            }

            PantryChanged?.Invoke(this, new PantryChangedEventArgs(familyId));
        }

        return Task.CompletedTask;
    }

    public Task<ShoppingSession?> GetCurrentSessionAsync(Guid familyId)
    {
        lock (_syncRoot)
        {
            var session = _sessions
                .Where(s => s.FamilyId == familyId && s.State == ShoppingSessionState.Active)
                .OrderByDescending(s => s.StartedAt)
                .FirstOrDefault();

            return Task.FromResult(session?.Clone());
        }
    }

    public Task<ShoppingSession> StartSessionAsync(Guid familyId, string? title = null)
    {
        ShoppingSession session;
        lock (_syncRoot)
        {
            session = new ShoppingSession
            {
                FamilyId = familyId,
                Title = title ?? $"Spesa del {DateTime.Now:dd MMMM}",
                State = ShoppingSessionState.Active,
                StartedAt = DateTime.Now,
            };

            _sessions.Add(session);
        }

        var cloned = session.Clone();
        ShoppingSessionChanged?.Invoke(this, new ShoppingSessionChangedEventArgs(familyId, cloned));
        return Task.FromResult(cloned);
    }

    public Task AddOrUpdateShoppingItemAsync(Guid sessionId, ShoppingItem item)
    {
        ShoppingSession session;
        lock (_syncRoot)
        {
            session = _sessions.First(s => s.Id == sessionId);
            var existing = session.Items.FirstOrDefault(i => i.ProductId == item.ProductId && !i.IsRemoved);
            if (existing is null)
            {
                item.SessionId = sessionId;
                session.Items.Add(item.Clone());
            }
            else
            {
                existing.Quantity += item.Quantity;
                var additional = item.Price ?? 0m;
                existing.Price = (existing.Price ?? 0m) + additional;
                existing.AddedAt = DateTime.UtcNow;
            }
        }

        RaiseSessionChanged(session);
        return Task.CompletedTask;
    }

    public Task UpdateShoppingItemAsync(Guid sessionId, ShoppingItem item)
    {
        ShoppingSession session;
        lock (_syncRoot)
        {
            session = _sessions.First(s => s.Id == sessionId);
            var existing = session.Items.First(i => i.Id == item.Id);
            existing.Quantity = item.Quantity;
            existing.Unit = item.Unit;
            existing.Price = item.Price;
        }

        RaiseSessionChanged(session);
        return Task.CompletedTask;
    }

    public Task RemoveShoppingItemAsync(Guid sessionId, Guid shoppingItemId)
    {
        ShoppingSession session;
        lock (_syncRoot)
        {
            session = _sessions.First(s => s.Id == sessionId);
            var existing = session.Items.FirstOrDefault(i => i.Id == shoppingItemId);
            if (existing is not null)
            {
                existing.IsRemoved = true;
            }
        }

        RaiseSessionChanged(session);
        return Task.CompletedTask;
    }

    public Task ConfirmSessionAsync(Guid sessionId, DateTime confirmationDate)
    {
        ShoppingSession session;
        Family family;
        lock (_syncRoot)
        {
            session = _sessions.First(s => s.Id == sessionId);
            family = _families.First(f => f.Id == session.FamilyId);

            if (session.State != ShoppingSessionState.Active)
            {
                return Task.CompletedTask;
            }

            session.State = ShoppingSessionState.Completed;
            session.ConfirmedAt = confirmationDate;

            foreach (var item in session.Items.Where(i => !i.IsRemoved))
            {
                var pantryItem = family.Pantry.Items.FirstOrDefault(i => i.ProductId == item.ProductId);
                if (pantryItem is null)
                {
                    pantryItem = new PantryItem
                    {
                        PantryId = family.Pantry.Id,
                        ProductId = item.ProductId,
                        Name = item.Product.Name,
                        Brand = item.Product.Brand,
                        Category = item.Product.Category,
                        Quantity = item.Quantity,
                        Unit = item.Unit,
                        AddedAt = confirmationDate,
                        MinimumThreshold = 1,
                    };

                    family.Pantry.Items.Add(pantryItem);
                }
                else
                {
                    pantryItem.Quantity += item.Quantity;
                    pantryItem.AddedAt = confirmationDate;
                }
            }
        }

        PantryChanged?.Invoke(this, new PantryChangedEventArgs(family.Id));
        RaiseSessionChanged(session);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Product>> GetProductsAsync()
    {
        lock (_syncRoot)
        {
            return Task.FromResult<IReadOnlyList<Product>>(_products.Select(p => p.Clone()).ToList());
        }
    }

    public Task<Product?> GetByBarcodeAsync(string barcode)
    {
        lock (_syncRoot)
        {
            var product = _products.FirstOrDefault(p => string.Equals(p.Barcode, barcode, StringComparison.OrdinalIgnoreCase));
            return Task.FromResult(product?.Clone());
        }
    }

    private void RaiseSessionChanged(ShoppingSession session)
    {
        ShoppingSessionChanged?.Invoke(this, new ShoppingSessionChangedEventArgs(session.FamilyId, session.Clone()));
    }

    private void Seed()
    {
        var userAlice = new User { DisplayName = "Alice", Email = "alice@example.com" };
        var userMarco = new User { DisplayName = "Marco", Email = "marco@example.com" };
        var userSofia = new User { DisplayName = "Sofia", Email = "sofia@example.com" };

        _users.AddRange(new[] { userAlice, userMarco, userSofia });

        var pasta = new Product
        {
            Name = "Spaghetti n.5",
            Brand = "De Cecco",
            Category = "Pasta",
            Barcode = "8001250120057",
            DefaultUnit = QuantityUnit.Pack,
            DefaultQuantity = 1,
            AveragePrice = 1.69m,
        };

        var tomatoSauce = new Product
        {
            Name = "Passata di Pomodoro",
            Brand = "Mutti",
            Category = "Salse",
            Barcode = "8005121001008",
            DefaultUnit = QuantityUnit.Bottle,
            DefaultQuantity = 1,
            AveragePrice = 1.39m,
        };

        var tuna = new Product
        {
            Name = "Tonno all'olio di oliva",
            Brand = "Rio Mare",
            Category = "Dispensa",
            Barcode = "8004030000035",
            DefaultUnit = QuantityUnit.Can,
            DefaultQuantity = 1,
            AveragePrice = 1.99m,
        };

        var milk = new Product
        {
            Name = "Latte fresco intero",
            Brand = "Parmalat",
            Category = "Freschi",
            Barcode = "8002200150026",
            DefaultUnit = QuantityUnit.Liter,
            DefaultQuantity = 1,
            AveragePrice = 1.45m,
        };

        var yogurt = new Product
        {
            Name = "Yogurt bianco",
            Brand = "Fage",
            Category = "Freschi",
            Barcode = "5213000114535",
            DefaultUnit = QuantityUnit.Pack,
            DefaultQuantity = 1,
            AveragePrice = 0.99m,
        };

        var rice = new Product
        {
            Name = "Riso Carnaroli",
            Brand = "Scotti",
            Category = "Dispensa",
            Barcode = "8001860001050",
            DefaultUnit = QuantityUnit.Pack,
            DefaultQuantity = 1,
            AveragePrice = 2.29m,
        };

        var eggs = new Product
        {
            Name = "Uova bio",
            Brand = "Fattoria",
            Category = "Freschi",
            Barcode = "8059300022334",
            DefaultUnit = QuantityUnit.Pack,
            DefaultQuantity = 1,
            AveragePrice = 2.89m,
        };

        var bread = new Product
        {
            Name = "Pane integrale",
            Brand = "Panificio Napoli",
            Category = "Forno",
            Barcode = "200000000001",
            DefaultUnit = QuantityUnit.Pack,
            DefaultQuantity = 1,
            AveragePrice = 2.5m,
        };

        var salad = new Product
        {
            Name = "Insalata mista",
            Brand = "OrtoVivo",
            Category = "Ortaggi",
            Barcode = "200000000002",
            DefaultUnit = QuantityUnit.Pack,
            DefaultQuantity = 1,
            AveragePrice = 1.2m,
        };

        var beans = new Product
        {
            Name = "Fagioli cannellini",
            Brand = "Cirio",
            Category = "Dispensa",
            Barcode = "8004900002330",
            DefaultUnit = QuantityUnit.Can,
            DefaultQuantity = 1,
            AveragePrice = 1.1m,
        };

        _products.AddRange(new[]
        {
            pasta, tomatoSauce, tuna, milk, yogurt, rice, eggs, bread, salad, beans
        });

        var familyId = Guid.NewGuid();
        var family = new Family
        {
            Id = familyId,
            Name = "Famiglia Rossi",
            AccentColor = Color.FromArgb("#FF7B54"),
            Pantry = new Pantry
            {
                Id = Guid.NewGuid(),
                FamilyId = familyId,
                Items = new List<PantryItem>
                {
                    new()
                    {
                        PantryId = family.Pantry.Id,
                        ProductId = pasta.Id,
                        Name = pasta.Name,
                        Brand = pasta.Brand,
                        Category = pasta.Category,
                        Quantity = 3,
                        Unit = QuantityUnit.Pack,
                        MinimumThreshold = 1,
                        ExpirationDate = DateTime.Today.AddMonths(10),
                        AddedAt = DateTime.Today.AddDays(-12),
                    },
                    new()
                    {
                        PantryId = family.Pantry.Id,
                        ProductId = tomatoSauce.Id,
                        Name = tomatoSauce.Name,
                        Brand = tomatoSauce.Brand,
                        Category = tomatoSauce.Category,
                        Quantity = 2,
                        Unit = QuantityUnit.Bottle,
                        MinimumThreshold = 1,
                        ExpirationDate = DateTime.Today.AddMonths(8),
                        AddedAt = DateTime.Today.AddDays(-6),
                    },
                    new()
                    {
                        PantryId = family.Pantry.Id,
                        ProductId = milk.Id,
                        Name = milk.Name,
                        Brand = milk.Brand,
                        Category = milk.Category,
                        Quantity = 1,
                        Unit = QuantityUnit.Liter,
                        MinimumThreshold = 1,
                        ExpirationDate = DateTime.Today.AddDays(2),
                        AddedAt = DateTime.Today.AddDays(-1),
                    },
                    new()
                    {
                        PantryId = family.Pantry.Id,
                        ProductId = yogurt.Id,
                        Name = yogurt.Name,
                        Brand = yogurt.Brand,
                        Category = yogurt.Category,
                        Quantity = 4,
                        Unit = QuantityUnit.Pack,
                        MinimumThreshold = 2,
                        ExpirationDate = DateTime.Today.AddDays(4),
                        AddedAt = DateTime.Today.AddDays(-2),
                    },
                    new()
                    {
                        PantryId = family.Pantry.Id,
                        ProductId = tuna.Id,
                        Name = tuna.Name,
                        Brand = tuna.Brand,
                        Category = tuna.Category,
                        Quantity = 5,
                        Unit = QuantityUnit.Can,
                        MinimumThreshold = 2,
                        ExpirationDate = DateTime.Today.AddMonths(18),
                        AddedAt = DateTime.Today.AddDays(-40),
                    },
                    new()
                    {
                        PantryId = family.Pantry.Id,
                        ProductId = rice.Id,
                        Name = rice.Name,
                        Brand = rice.Brand,
                        Category = rice.Category,
                        Quantity = 1,
                        Unit = QuantityUnit.Pack,
                        MinimumThreshold = 1,
                        ExpirationDate = DateTime.Today.AddMonths(12),
                        AddedAt = DateTime.Today.AddDays(-15),
                    },
                    new()
                    {
                        PantryId = family.Pantry.Id,
                        ProductId = eggs.Id,
                        Name = eggs.Name,
                        Brand = eggs.Brand,
                        Category = eggs.Category,
                        Quantity = 1,
                        Unit = QuantityUnit.Pack,
                        MinimumThreshold = 1,
                        ExpirationDate = DateTime.Today.AddDays(1),
                        AddedAt = DateTime.Today.AddDays(-3),
                    },
                    new()
                    {
                        PantryId = family.Pantry.Id,
                        ProductId = bread.Id,
                        Name = bread.Name,
                        Brand = bread.Brand,
                        Category = bread.Category,
                        Quantity = 1,
                        Unit = QuantityUnit.Pack,
                        MinimumThreshold = 1,
                        ExpirationDate = DateTime.Today.AddDays(1),
                        AddedAt = DateTime.Today.AddDays(-1),
                    },
                    new()
                    {
                        PantryId = family.Pantry.Id,
                        ProductId = beans.Id,
                        Name = beans.Name,
                        Brand = beans.Brand,
                        Category = beans.Category,
                        Quantity = 2,
                        Unit = QuantityUnit.Can,
                        MinimumThreshold = 1,
                        ExpirationDate = DateTime.Today.AddMonths(14),
                        AddedAt = DateTime.Today.AddDays(-20),
                    },
                }
            },
            Members = new List<FamilyMembership>
            {
                new() { UserId = userAlice.Id, Role = FamilyRole.Owner },
                new() { UserId = userMarco.Id, Role = FamilyRole.Member },
                new() { UserId = userSofia.Id, Role = FamilyRole.Member },
            },
        };

        userAlice.FamilyIds.Add(familyId);
        userMarco.FamilyIds.Add(familyId);
        userSofia.FamilyIds.Add(familyId);

        _families.Add(family);
    }
}
