using Avalonia.Controls;
using HyPrism.ViewModels;

namespace HyPrism;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }
}
