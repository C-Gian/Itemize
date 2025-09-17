namespace Itemize.Models.Pantry;

public class ShoppingItem
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid SessionId { get; set; }
        = Guid.Empty;

    public Guid ProductId { get; set; }
        = Guid.Empty;

    public Product Product { get; set; } = null!;

    public double Quantity { get; set; } = 0d;

    public QuantityUnit Unit { get; set; } = QuantityUnit.Pack;

    public decimal? Price { get; set; }
        = null;

    public DateTime AddedAt { get; set; } = DateTime.UtcNow;

    public bool IsRemoved { get; set; }
        = false;

    public bool IsCommitted { get; set; }
        = false;

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

    public ShoppingItem Clone()
        => new()
        {
            Id = Id,
            SessionId = SessionId,
            ProductId = ProductId,
            Product = Product.Clone(),
            Quantity = Quantity,
            Unit = Unit,
            Price = Price,
            AddedAt = AddedAt,
            IsRemoved = IsRemoved,
            IsCommitted = IsCommitted,
        };
}
