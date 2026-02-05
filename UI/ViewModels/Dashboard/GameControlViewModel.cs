using System;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using ReactiveUI;
using HyPrism.Services;
using HyPrism.Services.Core;
using HyPrism.Services.Game;
using System.Threading.Tasks;
using Avalonia.Threading;

namespace HyPrism.UI.ViewModels.Dashboard;

public class GameControlViewModel : ReactiveObject
{
    private readonly InstanceService _instanceService;
    private readonly FileService _fileService;
    private readonly GameProcessService _gameProcessService;

    // Commands
    public ReactiveCommand<Unit, Unit> ToggleModsCommand { get; }
    public ReactiveCommand<Unit, Unit> OpenFolderCommand { get; }
    public ReactiveCommand<Unit, Unit> ToggleSettingsCommand { get; }
    public ReactiveCommand<Unit, Unit> LaunchCommand { get; }

    // Properties
    private string _selectedBranch = "Release";
    public string SelectedBranch
    {
        get => _selectedBranch;
        set => this.RaiseAndSetIfChanged(ref _selectedBranch, value);
    }
    
    private int _selectedVersion = 0;
    public int SelectedVersion
    {
        get => _selectedVersion;
        set => this.RaiseAndSetIfChanged(ref _selectedVersion, value);
    }

    private bool _isGameRunning;
    public bool IsGameRunning
    {
        get => _isGameRunning;
        set => this.RaiseAndSetIfChanged(ref _isGameRunning, value);
    }

    public ObservableCollection<string> Branches { get; } = new() { "Release", "Pre-Release" };

    // Localization
    public IObservable<string> MainEducational { get; }
    public IObservable<string> MainBuyIt { get; }
    public IObservable<string> MainPlay { get; }

    public GameControlViewModel(
        InstanceService instanceService,
        FileService fileService,
        GameProcessService gameProcessService,
        Action<string, int> toggleMods, 
        Action toggleSettings,
        Func<Task> launchAction)
    {
        _instanceService = instanceService;
        _fileService = fileService;
        _gameProcessService = gameProcessService;

        var loc = LocalizationService.Instance;
        MainEducational = loc.GetObservable("main.educational");
        MainBuyIt = loc.GetObservable("main.buyIt");
        
        // Dynamic Play Button Text
        MainPlay = this.WhenAnyValue(x => x.IsGameRunning)
            .CombineLatest(
                loc.GetObservable("main.play"), 
                loc.GetObservable("main.running"),
                (running, playText, runningText) => running ? (string.IsNullOrEmpty(runningText) ? "RUNNED" : runningText) : playText
            );
        
        ToggleModsCommand = ReactiveCommand.Create(() => toggleMods(SelectedBranch, SelectedVersion));
        ToggleSettingsCommand = ReactiveCommand.Create(toggleSettings);
        
        OpenFolderCommand = ReactiveCommand.Create(() =>  
        {
            var branch = SelectedBranch?.ToLower().Replace(" ", "-") ?? "release";
            // Logic moved from GameUtilityService
            string branchNormalized = UtilityService.NormalizeVersionType(branch);
            var path = _instanceService.ResolveInstancePath(branchNormalized, SelectedVersion, true);
            _fileService.OpenFolder(path);
        });

        var canLaunch = this.WhenAnyValue(x => x.IsGameRunning, running => !running);
        LaunchCommand = ReactiveCommand.CreateFromTask(launchAction, canLaunch);

        // Periodically check for game process status
        DispatcherTimer.Run(() => 
        {
            IsGameRunning = _gameProcessService.CheckForRunningGame();
            return true;
        }, TimeSpan.FromSeconds(2));
    }
}
