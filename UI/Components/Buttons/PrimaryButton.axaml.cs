using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Metadata;

namespace HyPrism.UI.Components.Buttons;

public partial class PrimaryButton : UserControl
{
    // Redirect content to ButtonContent so we don't overwrite the UserControl's root content (the Button wrapper)
    [Content]
    public object? ButtonContent
    {
        get => GetValue(ButtonContentProperty);
        set => SetValue(ButtonContentProperty, value);
    }

    public static readonly StyledProperty<object?> ButtonContentProperty =
        AvaloniaProperty.Register<PrimaryButton, object?>(nameof(ButtonContent));

    public static readonly StyledProperty<ICommand?> CommandProperty =
        AvaloniaProperty.Register<PrimaryButton, ICommand?>(nameof(Command));

    public ICommand? Command
    {
        get => GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }
    
    public static readonly StyledProperty<object?> CommandParameterProperty =
        AvaloniaProperty.Register<PrimaryButton, object?>(nameof(CommandParameter));

    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }
    
    public PrimaryButton()
    {
        InitializeComponent();
    }
}
