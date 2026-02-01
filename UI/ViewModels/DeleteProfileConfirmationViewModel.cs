using System;
using System.Reactive;
using ReactiveUI;

namespace HyPrism.UI.ViewModels;

public class DeleteProfileConfirmationViewModel : ReactiveObject
{
    private string _profileName = "";
    private double _overlayOpacity = 1.0;
    private double _dialogOpacity = 1.0;
    
    public DeleteProfileConfirmationViewModel()
    {
        ConfirmCommand = ReactiveCommand.Create(OnConfirm);
        CancelCommand = ReactiveCommand.Create(OnCancel);
    }
    
    public string ProfileName
    {
        get => _profileName;
        set => this.RaiseAndSetIfChanged(ref _profileName, value);
    }
    
    public double OverlayOpacity
    {
        get => _overlayOpacity;
        set => this.RaiseAndSetIfChanged(ref _overlayOpacity, value);
    }
    
    public double DialogOpacity
    {
        get => _dialogOpacity;
        set => this.RaiseAndSetIfChanged(ref _dialogOpacity, value);
    }
    
    public ReactiveCommand<Unit, Unit> ConfirmCommand { get; }
    public ReactiveCommand<Unit, Unit> CancelCommand { get; }
    
    public event EventHandler? Confirmed;
    public event EventHandler? Cancelled;
    
    private void OnConfirm()
    {
        Confirmed?.Invoke(this, EventArgs.Empty);
    }
    
    private void OnCancel()
    {
        Cancelled?.Invoke(this, EventArgs.Empty);
    }
}
