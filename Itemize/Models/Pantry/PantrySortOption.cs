namespace Itemize.Models.Pantry;

public enum PantrySortType
{
    Alphabetical,
    ExpirationDate,
    AddedDate,
    Category,
}

public class PantrySortOption
{
    public PantrySortOption(string label, PantrySortType sortType)
    {
        Label = label;
        SortType = sortType;
    }

    public string Label { get; }

    public PantrySortType SortType { get; }

    public override string ToString()
        => Label;
}
