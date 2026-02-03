using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using HyPrism.Services.Core;

namespace HyPrism.UI.Components.Buttons;

public class IconButton : Button
{
    protected override Type StyleKeyOverride => typeof(IconButton);
    private IDisposable? _resourceSubscription;
    private IDisposable? _brushSubscription;

    public static readonly StyledProperty<IBrush?> ButtonBackgroundProperty =
        AvaloniaProperty.Register<IconButton, IBrush?>(nameof(ButtonBackground));

    public IBrush? ButtonBackground
    {
        get => GetValue(ButtonBackgroundProperty);
        set => SetValue(ButtonBackgroundProperty, value);
    }
    
    public static readonly StyledProperty<string?> IconPathProperty =
        AvaloniaProperty.Register<IconButton, string?>(nameof(IconPath));

    public string? IconPath
    {
        get => GetValue(IconPathProperty);
        set => SetValue(IconPathProperty, value);
    }

    public static readonly StyledProperty<double> IconSizeProperty =
        AvaloniaProperty.Register<IconButton, double>(nameof(IconSize), 24.0);

    public double IconSize
    {
        get => GetValue(IconSizeProperty);
        set => SetValue(IconSizeProperty, value);
    }

    public static readonly StyledProperty<string> HoverCssProperty =
        AvaloniaProperty.Register<IconButton, string>(nameof(HoverCss), "* { stroke: #FFA845; fill: none; }");

    public string HoverCss
    {
        get => GetValue(HoverCssProperty);
        private set => SetValue(HoverCssProperty, value);
    }

    public IconButton()
    {
        // Initialize with accent color as soon as possible
        UpdateHoverCss();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ForegroundProperty)
        {
            UpdateHoverCss();
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        
        if (Application.Current != null)
        {
            // Subscribe to the resource itself (in case the brush instance is replaced)
             _resourceSubscription = Application.Current.GetResourceObservable("SystemAccentBrush")
                .Subscribe(obj => 
                {
                    if (obj is SolidColorBrush brush)
                    {
                        SubscribeToBrush(brush);
                    }
                });
        }
    }

    private void SubscribeToBrush(SolidColorBrush brush)
    {
        // Clean up previous brush subscription
        _brushSubscription?.Dispose();
        
        // Subscribe to Color changes on the specific brush instance
        // This handles the smooth animation updates
        _brushSubscription = brush.GetObservable(SolidColorBrush.ColorProperty)
            .Subscribe(color => 
            {
                 Dispatcher.UIThread.InvokeAsync(() => UpdateCssFromColor(color));
            });
    }

    private void UpdateCssFromColor(Color c)
    {
        var hexColor = $"#{c.R:X2}{c.G:X2}{c.B:X2}";
        HoverCss = $"* {{ stroke: {hexColor}; fill: none; }}";
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _brushSubscription?.Dispose();
        _resourceSubscription?.Dispose();
        _brushSubscription = null;
        _resourceSubscription = null;
    }

    private void UpdateHoverCss()
    {
        // Fallback / Initial manual update if needed (though observables should cover it)
        if (Application.Current?.TryGetResource("SystemAccentBrush", null, out var resource) == true 
            && resource is ISolidColorBrush accentBrush)
        {
            UpdateCssFromColor(accentBrush.Color);
        }
        else
        {
            HoverCss = "* { stroke: #FFA845; fill: none; }";
        }
    }
}
