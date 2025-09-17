using CommunityToolkit.Mvvm.ComponentModel;

namespace Itemize.Services;

public class FamilyContext : ObservableObject
{
    private Guid? _selectedFamilyId;

    public Guid? SelectedFamilyId
    {
        get => _selectedFamilyId;
        set => SetProperty(ref _selectedFamilyId, value);
    }
}
