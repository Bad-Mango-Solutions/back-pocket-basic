// <copyright file="DebugSettingsViewModel.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.UI.ViewModels.Settings;

using BadMango.Emulator.UI.Abstractions.Settings;

using CommunityToolkit.Mvvm.ComponentModel;

/// <summary>
/// ViewModel for debug and logging settings.
/// </summary>
public partial class DebugSettingsViewModel : SettingsPageViewModelBase
{
    [ObservableProperty]
    private bool autoAttachDebugger;

    [ObservableProperty]
    private bool breakOnReset;

    [ObservableProperty]
    private string logLevel = "Information";

    [ObservableProperty]
    private bool logToFile = true;

    [ObservableProperty]
    private int maxLogFileSizeMB = 10;

    [ObservableProperty]
    private bool traceInstructions;

    [ObservableProperty]
    private bool showCycleCount = true;

    /// <summary>
    /// Initializes a new instance of the <see cref="DebugSettingsViewModel"/> class.
    /// </summary>
    /// <param name="settingsService">The settings service.</param>
    public DebugSettingsViewModel(ISettingsService settingsService)
        : base(settingsService, "Debug", "BugIcon", 4)
    {
    }

    /// <summary>
    /// Gets the available log levels.
    /// </summary>
    public IReadOnlyList<string> AvailableLogLevels { get; } =
    [
        "Verbose",
        "Debug",
        "Information",
        "Warning",
        "Error",
        "Fatal",
    ];

    /// <inheritdoc/>
    public override Task LoadAsync()
    {
        var settings = SettingsService.Current.Debug;
        AutoAttachDebugger = settings.AutoAttachDebugger;
        BreakOnReset = settings.BreakOnReset;
        LogLevel = settings.LogLevel;
        LogToFile = settings.LogToFile;
        MaxLogFileSizeMB = settings.MaxLogFileSizeMB;
        TraceInstructions = settings.TraceInstructions;
        ShowCycleCount = settings.ShowCycleCount;
        HasChanges = false;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public override async Task SaveAsync()
    {
        var current = SettingsService.Current;
        var newSettings = current with
        {
            Debug = new DebugSettings
            {
                AutoAttachDebugger = AutoAttachDebugger,
                BreakOnReset = BreakOnReset,
                LogLevel = LogLevel,
                LogToFile = LogToFile,
                MaxLogFileSizeMB = MaxLogFileSizeMB,
                TraceInstructions = TraceInstructions,
                ShowCycleCount = ShowCycleCount,
            },
        };
        await SettingsService.SaveAsync(newSettings).ConfigureAwait(false);
        HasChanges = false;
    }

    /// <inheritdoc/>
    public override Task ResetToDefaultsAsync()
    {
        var defaults = new DebugSettings();
        AutoAttachDebugger = defaults.AutoAttachDebugger;
        BreakOnReset = defaults.BreakOnReset;
        LogLevel = defaults.LogLevel;
        LogToFile = defaults.LogToFile;
        MaxLogFileSizeMB = defaults.MaxLogFileSizeMB;
        TraceInstructions = defaults.TraceInstructions;
        ShowCycleCount = defaults.ShowCycleCount;
        MarkAsChanged();
        return Task.CompletedTask;
    }

    partial void OnAutoAttachDebuggerChanged(bool value) => MarkAsChanged();

    partial void OnBreakOnResetChanged(bool value) => MarkAsChanged();

    partial void OnLogLevelChanged(string value) => MarkAsChanged();

    partial void OnLogToFileChanged(bool value) => MarkAsChanged();

    partial void OnMaxLogFileSizeMBChanged(int value) => MarkAsChanged();

    partial void OnTraceInstructionsChanged(bool value) => MarkAsChanged();

    partial void OnShowCycleCountChanged(bool value) => MarkAsChanged();
}