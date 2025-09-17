namespace Itemize.Models.Pantry;

public class ShoppingSession
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid FamilyId { get; set; }
        = Guid.Empty;

    public string Title { get; set; } = string.Empty;

    public ShoppingSessionState State { get; set; } = ShoppingSessionState.Active;

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ConfirmedAt { get; set; }
        = null;

    public List<ShoppingItem> Items { get; set; } = new();

    public decimal TotalAmount
        => Items.Where(i => !i.IsRemoved)
                 .Sum(i => i.Price.HasValue ? i.Price.Value : 0m);

    public ShoppingSession Clone()
        => new()
        {
            Id = Id,
            FamilyId = FamilyId,
            Title = Title,
            State = State,
            StartedAt = StartedAt,
            ConfirmedAt = ConfirmedAt,
            Items = Items.Select(i => i.Clone()).ToList(),
        };
}
