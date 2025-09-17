using CommunityToolkit.Mvvm.Input;
using Itemize.Models;

namespace Itemize.PageModels
{
    public interface IProjectTaskPageModel
    {
        IAsyncRelayCommand<ProjectTask> NavigateToTaskCommand { get; }
        bool IsBusy { get; }
    }
}