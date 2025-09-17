namespace Itemize.Models.Pantry;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string DisplayName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public List<Guid> FamilyIds { get; set; } = new();

    public User Clone()
        => new()
        {
            Id = Id,
            DisplayName = DisplayName,
            Email = Email,
            FamilyIds = new(FamilyIds),
        };
}
