using Itemize.Models.Pantry;

namespace Itemize.Services.Pantry;

public interface IProductCatalog
{
    Task<IReadOnlyList<Product>> GetProductsAsync();

    Task<Product?> GetByBarcodeAsync(string barcode);
}
