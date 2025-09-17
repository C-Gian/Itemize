using Itemize.Models.Pantry;

namespace Itemize.Services;

public class RecipeSuggestionService
{
    private static readonly string[] PastaKeywords = ["pasta", "spaghetti", "penne"];
    private static readonly string[] TomatoKeywords = ["pomodoro", "passata", "salsa"];
    private static readonly string[] RiceKeywords = ["riso"];
    private static readonly string[] TunaKeywords = ["tonno"];
    private static readonly string[] EggKeywords = ["uova"];

    public IReadOnlyList<RecipeSuggestion> BuildSuggestions(IEnumerable<PantryItem> pantryItems)
    {
        var items = pantryItems.Where(i => i.State == PantryItemState.Active).ToList();
        var suggestions = new List<RecipeSuggestion>();

        if (HasAll(items, PastaKeywords) && HasAll(items, TomatoKeywords))
        {
            suggestions.Add(new RecipeSuggestion
            {
                Title = "Spaghetti al pomodoro",
                Description = "Pasta veloce con salsa di pomodoro e basilico fresco.",
                Ingredients = new[] { "Spaghetti", "Passata di pomodoro", "Olio", "Basilico" },
            });
        }

        if (HasAll(items, RiceKeywords) && HasAll(items, TunaKeywords))
        {
            suggestions.Add(new RecipeSuggestion
            {
                Title = "Insalata di riso mediterranea",
                Description = "Riso freddo con tonno, verdure croccanti e mais.",
                Ingredients = new[] { "Riso", "Tonno", "Mais", "Olive" },
            });
        }

        if (HasAll(items, EggKeywords) && HasAll(items, TomatoKeywords))
        {
            suggestions.Add(new RecipeSuggestion
            {
                Title = "Shakshuka veloce",
                Description = "Uova in umido con salsa di pomodoro e paprika.",
                Ingredients = new[] { "Uova", "Passata di pomodoro", "Cipolla", "Paprika" },
            });
        }

        if (!suggestions.Any())
        {
            suggestions.Add(new RecipeSuggestion
            {
                Title = "Idea rapida",
                Description = "Controlla cosa sta per scadere e crea una ricetta zero sprechi!",
                Ingredients = Array.Empty<string>(),
            });
        }

        return suggestions;
    }

    private static bool HasAll(IEnumerable<PantryItem> items, IEnumerable<string> keywords)
    {
        return keywords.All(keyword =>
            items.Any(item => item.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase)));
    }
}
