// <copyright file="DisplaySettingsViewModel.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.UI.ViewModels.Settings;

using BadMango.Emulator.UI.Abstractions.Settings;

using CommunityToolkit.Mvvm.ComponentModel;

/// <summary>
/// ViewModel for display and video output settings.
/// </summary>
public partial class DisplaySettingsViewModel : SettingsPageViewModelBase
{
    [ObservableProperty]
    private string scalingMode = "Integer";

    [ObservableProperty]
    private int scaleFactor = 2;

    [ObservableProperty]
    private bool scanlineEffect;

    [ObservableProperty]
    private string colorPalette = "NTSC";

    [ObservableProperty]
    private int frameRateCap = 60;

    [ObservableProperty]
    private bool vSync = true;

    [ObservableProperty]
    private int fullScreenMonitor;

    /// <summary>
    /// Initializes a new instance of the <see cref="DisplaySettingsViewModel"/> class.
    /// </summary>
    /// <param name="settingsService">The settings service.</param>
    public DisplaySettingsViewModel(ISettingsService settingsService)
        : base(settingsService, "Display", "DisplayIcon", 2)
    {
    }

    /// <summary>
    /// Gets the available scaling modes.
    /// </summary>
    public IReadOnlyList<string> AvailableScalingModes { get; } =
    [
        "Integer",
        "AspectCorrect",
        "Fill",
        "Custom",
    ];

    /// <summary>
    /// Gets the available color palettes.
    /// </summary>
    public IReadOnlyList<string> AvailableColorPalettes { get; } =
    [
        "NTSC",
        "RGB",
        "Monochrome",
        "Amber",
        "Custom",
    ];

    /// <inheritdoc/>
    public override Task LoadAsync()
    {
        var settings = SettingsService.Current.Display;
        ScalingMode = settings.ScalingMode;
        ScaleFactor = settings.ScaleFactor;
        ScanlineEffect = settings.ScanlineEffect;
        ColorPalette = settings.ColorPalette;
        FrameRateCap = settings.FrameRateCap;
        VSync = settings.VSync;
        FullScreenMonitor = settings.FullScreenMonitor;
        HasChanges = false;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public override async Task SaveAsync()
    {
        var current = SettingsService.Current;
        var newSettings = current with
        {
            Display = new DisplaySettings
            {
                ScalingMode = ScalingMode,
                ScaleFactor = ScaleFactor,
                ScanlineEffect = ScanlineEffect,
                ColorPalette = ColorPalette,
                FrameRateCap = FrameRateCap,
                VSync = VSync,
                FullScreenMonitor = FullScreenMonitor,
            },
        };
        await SettingsService.SaveAsync(newSettings).ConfigureAwait(false);
        HasChanges = false;
    }

    /// <inheritdoc/>
    public override Task ResetToDefaultsAsync()
    {
        var defaults = new DisplaySettings();
        ScalingMode = defaults.ScalingMode;
        ScaleFactor = defaults.ScaleFactor;
        ScanlineEffect = defaults.ScanlineEffect;
        ColorPalette = defaults.ColorPalette;
        FrameRateCap = defaults.FrameRateCap;
        VSync = defaults.VSync;
        FullScreenMonitor = defaults.FullScreenMonitor;
        MarkAsChanged();
        return Task.CompletedTask;
    }

    partial void OnScalingModeChanged(string value) => MarkAsChanged();

    partial void OnScaleFactorChanged(int value) => MarkAsChanged();

    partial void OnScanlineEffectChanged(bool value) => MarkAsChanged();

    partial void OnColorPaletteChanged(string value) => MarkAsChanged();

    partial void OnFrameRateCapChanged(int value) => MarkAsChanged();

    partial void OnVSyncChanged(bool value) => MarkAsChanged();

    partial void OnFullScreenMonitorChanged(int value) => MarkAsChanged();
}