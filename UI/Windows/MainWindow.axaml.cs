using Avalonia.Controls;
using HyPrism.UI.ViewModels;

namespace HyPrism.UI;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }
}
