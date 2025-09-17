using Microsoft.Maui.Graphics;

namespace Itemize.Models.Pantry;

public class Family
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    public Color AccentColor { get; set; } = Colors.Orange;

    public List<FamilyMembership> Members { get; set; } = new();

    public Pantry Pantry { get; set; } = new();

    public Family Clone()
        => new()
        {
            Id = Id,
            Name = Name,
            AccentColor = AccentColor,
            Members = Members.Select(m => m.Clone()).ToList(),
            Pantry = Pantry.Clone(),
        };
}
