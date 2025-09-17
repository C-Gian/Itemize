using Itemize.PageModels;

namespace Itemize.Pages;

public partial class ShoppingSessionPage : ContentPage
{
    public ShoppingSessionPage(ShoppingSessionPageModel model)
    {
        InitializeComponent();
        BindingContext = model;
    }
}
