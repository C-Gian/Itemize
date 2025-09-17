namespace Itemize.Models.Pantry;

public class PantryItem
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid PantryId { get; set; }
        = Guid.Empty;

    public Guid ProductId { get; set; }
        = Guid.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Brand { get; set; }
        = string.Empty;

    public string? Category { get; set; }
        = string.Empty;

    public double Quantity { get; set; } = 0d;

    public QuantityUnit Unit { get; set; } = QuantityUnit.Pack;

    public double? MinimumThreshold { get; set; }
        = null;

    public DateTime? ExpirationDate { get; set; }
        = null;

    public DateTime AddedAt { get; set; } = DateTime.UtcNow;

    public PantryItemState State { get; set; } = PantryItemState.Active;

    public string? Notes { get; set; }
        = null;

    public bool IsFavorite { get; set; }
        = false;

    public bool IsLowStock
        => MinimumThreshold.HasValue && Quantity <= MinimumThreshold.Value;

    public string QuantityDisplay
        => Unit switch
        {
            QuantityUnit.Gram => $"{Quantity:0} g",
            QuantityUnit.Kilogram => $"{Quantity:0.##} kg",
            QuantityUnit.Milliliter => $"{Quantity:0} ml",
            QuantityUnit.Liter => $"{Quantity:0.##} l",
            QuantityUnit.Piece => $"{Quantity:0} pz",
            QuantityUnit.Bottle => $"{Quantity:0} bott.",
            QuantityUnit.Can => $"{Quantity:0} latt.",
            _ => $"{Quantity:0.##} conf."
        };

    public bool IsExpired(DateTime today)
        => ExpirationDate.HasValue && ExpirationDate.Value.Date < today.Date;

    public bool IsExpiringSoon(DateTime today, int thresholdDays)
        => ExpirationDate.HasValue
           && !IsExpired(today)
           && (ExpirationDate.Value.Date - today.Date).TotalDays <= thresholdDays;

    public PantryItem Clone()
        => new()
        {
            Id = Id,
            PantryId = PantryId,
            ProductId = ProductId,
            Name = Name,
            Brand = Brand,
            Category = Category,
            Quantity = Quantity,
            Unit = Unit,
            MinimumThreshold = MinimumThreshold,
            ExpirationDate = ExpirationDate,
            AddedAt = AddedAt,
            State = State,
            Notes = Notes,
            IsFavorite = IsFavorite,
        };
}
