namespace Itemize.Models.Pantry;

public class PantrySummary
{
    public int TotalItems { get; init; }

    public int UniqueProducts { get; init; }

    public int ExpiringSoonCount { get; init; }

    public int ExpiredCount { get; init; }

    public int LowStockCount { get; init; }

    public static PantrySummary FromItems(IEnumerable<PantryItem> items, int expiringThresholdDays)
    {
        var today = DateTime.Today;
        var activeItems = items.Where(i => i.State == PantryItemState.Active).ToList();

        return new PantrySummary
        {
            TotalItems = activeItems.Count,
            UniqueProducts = activeItems.Select(i => i.ProductId).Distinct().Count(),
            ExpiringSoonCount = activeItems.Count(i => i.IsExpiringSoon(today, expiringThresholdDays)),
            ExpiredCount = activeItems.Count(i => i.IsExpired(today)),
            LowStockCount = activeItems.Count(i => i.IsLowStock),
        };
    }
}
