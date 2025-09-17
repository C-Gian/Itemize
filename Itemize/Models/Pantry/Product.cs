namespace Itemize.Models.Pantry;

public class Product
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    public string? Brand { get; set; }
        = string.Empty;

    public string? Category { get; set; }
        = string.Empty;

    public string Barcode { get; set; } = string.Empty;

    public QuantityUnit DefaultUnit { get; set; } = QuantityUnit.Pack;

    public double DefaultQuantity { get; set; } = 1d;

    public decimal? AveragePrice { get; set; }
        = null;

    public string? ImageUrl { get; set; }
        = null;

    public Product Clone()
        => new()
        {
            Id = Id,
            Name = Name,
            Brand = Brand,
            Category = Category,
            Barcode = Barcode,
            DefaultUnit = DefaultUnit,
            DefaultQuantity = DefaultQuantity,
            AveragePrice = AveragePrice,
            ImageUrl = ImageUrl,
        };
}
