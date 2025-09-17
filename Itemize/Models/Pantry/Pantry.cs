namespace Itemize.Models.Pantry;

public class Pantry
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid FamilyId { get; set; }
        = Guid.Empty;

    public List<PantryItem> Items { get; set; } = new();

    public Pantry Clone()
        => new()
        {
            Id = Id,
            FamilyId = FamilyId,
            Items = Items.Select(i => i.Clone()).ToList(),
        };
}
