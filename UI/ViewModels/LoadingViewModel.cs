using System;
using System.Threading.Tasks;
using ReactiveUI;

namespace HyPrism.UI.ViewModels;

public class LoadingViewModel : ReactiveObject
{
    private bool _isLoading = true;
    private string _loadingText = "Loading";
    private double _logoOpacity = 0;
    private double _contentOpacity = 0;
    private double _spinnerOpacity = 0;
    private bool _isExiting;
    
    // Animation durations for XAML binding
    public string LogoFadeDuration => LoadingAnimationConstants.GetDurationString(LoadingAnimationConstants.LogoFadeDuration);
    public string ContentFadeDuration => LoadingAnimationConstants.GetDurationString(LoadingAnimationConstants.ContentFadeDuration);
    public string SpinnerFadeDuration => LoadingAnimationConstants.GetDurationString(LoadingAnimationConstants.SpinnerFadeDuration);
    public string ExitAnimationDuration => LoadingAnimationConstants.GetDurationString(LoadingAnimationConstants.ExitAnimationDuration);
    
    public bool IsLoading
    {
        get => _isLoading;
        set => this.RaiseAndSetIfChanged(ref _isLoading, value);
    }
    
    public string LoadingText
    {
        get => _loadingText;
        set => this.RaiseAndSetIfChanged(ref _loadingText, value);
    }
    
    public double LogoOpacity
    {
        get => _logoOpacity;
        set => this.RaiseAndSetIfChanged(ref _logoOpacity, value);
    }
    
    public double ContentOpacity
    {
        get => _contentOpacity;
        set => this.RaiseAndSetIfChanged(ref _contentOpacity, value);
    }
    
    public double SpinnerOpacity
    {
        get => _spinnerOpacity;
        set => this.RaiseAndSetIfChanged(ref _spinnerOpacity, value);
    }
    
    public bool IsExiting
    {
        get => _isExiting;
        set => this.RaiseAndSetIfChanged(ref _isExiting, value);
    }
    
    public LoadingViewModel()
    {
        _ = StartEntranceAnimationAsync();
    }
    
    private async Task StartEntranceAnimationAsync()
    {
        await Task.Delay(LoadingAnimationConstants.InitialDelay);
        
        // Fade in logo
        LogoOpacity = 1;
        await Task.Delay(LoadingAnimationConstants.LogoFadeDelay);
        
        // Fade in content and spinner
        ContentOpacity = 1;
        SpinnerOpacity = 1;
        
        // Keep visible for minimum time
        await Task.Delay(LoadingAnimationConstants.MinimumVisibleTime);
    }
    
    public async Task CompleteLoadingAsync()
    {
        // Hide spinner (uses SpinnerFadeDuration from constants)
        SpinnerOpacity = 0;
        await Task.Delay(LoadingAnimationConstants.SpinnerFadeWaitTime);
        
        // Delay before exit animation
        await Task.Delay(LoadingAnimationConstants.PreExitDelay);
        
        // Trigger exit animation
        IsExiting = true;
        
        // Wait for exit animation to complete
        await Task.Delay(LoadingAnimationConstants.ExitAnimationWaitTime);
        
        // Hide loading screen
        IsLoading = false;
    }
}
