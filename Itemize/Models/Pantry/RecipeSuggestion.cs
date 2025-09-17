using System.Linq;

namespace Itemize.Models.Pantry;

public class RecipeSuggestion
{
    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public IReadOnlyList<string> Ingredients { get; set; } = Array.Empty<string>();

    public string IngredientList => Ingredients.Any()
        ? string.Join(", ", Ingredients)
        : "Aggiungi ingredienti per suggerimenti personalizzati";
}
