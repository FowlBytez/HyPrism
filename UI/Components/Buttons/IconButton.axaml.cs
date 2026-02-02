using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;

namespace HyPrism.UI.Components.Buttons;

public partial class IconButton : UserControl
{
    public static readonly StyledProperty<ICommand?> CommandProperty =
        AvaloniaProperty.Register<IconButton, ICommand?>(nameof(Command));

    public ICommand? Command
    {
        get => GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }
    
    public static readonly StyledProperty<object?> CommandParameterProperty =
        AvaloniaProperty.Register<IconButton, object?>(nameof(CommandParameter));

    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    public IconButton()
    {
        InitializeComponent();
    }
}
