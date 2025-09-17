using Itemize.Models.Pantry;

namespace Itemize.Services.Pantry;

public interface IPantryStore
{
    event EventHandler<PantryChangedEventArgs>? PantryChanged;

    event EventHandler<ShoppingSessionChangedEventArgs>? ShoppingSessionChanged;

    Task<IReadOnlyList<Family>> GetFamiliesAsync();

    Task<Pantry> GetPantryAsync(Guid familyId);

    Task DeletePantryItemAsync(Guid familyId, Guid pantryItemId);

    Task UpdatePantryItemAsync(Guid familyId, PantryItem item);

    Task<ShoppingSession?> GetCurrentSessionAsync(Guid familyId);

    Task<ShoppingSession> StartSessionAsync(Guid familyId, string? title = null);

    Task AddOrUpdateShoppingItemAsync(Guid sessionId, ShoppingItem item);

    Task UpdateShoppingItemAsync(Guid sessionId, ShoppingItem item);

    Task RemoveShoppingItemAsync(Guid sessionId, Guid shoppingItemId);

    Task ConfirmSessionAsync(Guid sessionId, DateTime confirmationDate);
}
