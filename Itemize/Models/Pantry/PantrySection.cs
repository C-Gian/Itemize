using System.Collections.ObjectModel;

namespace Itemize.Models.Pantry;

public class PantrySection
{
    public PantrySection(string title, IEnumerable<PantryItem> items)
    {
        Title = title;
        Items = new ObservableCollection<PantryItem>(items);
    }

    public string Title { get; }

    public ObservableCollection<PantryItem> Items { get; }
}
