namespace Itemize.Models.Pantry;

public class FamilyMembership
{
    public Guid UserId { get; set; }
        = Guid.Empty;

    public FamilyRole Role { get; set; } = FamilyRole.Member;

    public FamilyMembership Clone()
        => new()
        {
            UserId = UserId,
            Role = Role,
        };
}
