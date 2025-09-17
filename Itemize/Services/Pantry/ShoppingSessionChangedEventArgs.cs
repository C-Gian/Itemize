using Itemize.Models.Pantry;

namespace Itemize.Services.Pantry;

public class ShoppingSessionChangedEventArgs : EventArgs
{
    public ShoppingSessionChangedEventArgs(Guid familyId, ShoppingSession session)
    {
        FamilyId = familyId;
        Session = session;
    }

    public Guid FamilyId { get; }

    public ShoppingSession Session { get; }
}
