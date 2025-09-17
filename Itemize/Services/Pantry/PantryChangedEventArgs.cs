namespace Itemize.Services.Pantry;

public class PantryChangedEventArgs : EventArgs
{
    public PantryChangedEventArgs(Guid familyId)
    {
        FamilyId = familyId;
    }

    public Guid FamilyId { get; }
}
