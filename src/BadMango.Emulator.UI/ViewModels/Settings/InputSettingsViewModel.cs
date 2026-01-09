// <copyright file="InputSettingsViewModel.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.UI.ViewModels.Settings;

using BadMango.Emulator.Configuration.Models;
using BadMango.Emulator.Configuration.Services;

using CommunityToolkit.Mvvm.ComponentModel;

/// <summary>
/// ViewModel for input and control settings.
/// </summary>
public partial class InputSettingsViewModel : SettingsPageViewModelBase
{
    [ObservableProperty]
    private string keyboardMapping = "Standard";

    [ObservableProperty]
    private string? customKeyMapFile;

    [ObservableProperty]
    private bool mouseCapture;

    [ObservableProperty]
    private bool joystickEnabled;

    [ObservableProperty]
    private string joystickDevice = "Auto";

    [ObservableProperty]
    private int paddleSensitivity = 50;

    /// <summary>
    /// Initializes a new instance of the <see cref="InputSettingsViewModel"/> class.
    /// </summary>
    /// <param name="settingsService">The settings service.</param>
    public InputSettingsViewModel(ISettingsService settingsService)
        : base(settingsService, "Input", "KeyboardIcon", 3)
    {
    }

    /// <summary>
    /// Gets the available keyboard mappings.
    /// </summary>
    public IReadOnlyList<string> AvailableKeyboardMappings { get; } =
    [
        "Standard",
        "Positional",
        "Custom",
    ];

    /// <inheritdoc/>
    public override Task LoadAsync()
    {
        var settings = SettingsService.Current.Input;
        KeyboardMapping = settings.KeyboardMapping;
        CustomKeyMapFile = settings.CustomKeyMapFile;
        MouseCapture = settings.MouseCapture;
        JoystickEnabled = settings.JoystickEnabled;
        JoystickDevice = settings.JoystickDevice;
        PaddleSensitivity = settings.PaddleSensitivity;
        HasChanges = false;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public override async Task SaveAsync()
    {
        var current = SettingsService.Current;
        var newSettings = current with
        {
            Input = new()
            {
                KeyboardMapping = KeyboardMapping,
                CustomKeyMapFile = CustomKeyMapFile,
                MouseCapture = MouseCapture,
                JoystickEnabled = JoystickEnabled,
                JoystickDevice = JoystickDevice,
                PaddleSensitivity = PaddleSensitivity,
            },
        };
        await SettingsService.SaveAsync(newSettings).ConfigureAwait(false);
        HasChanges = false;
    }

    /// <inheritdoc/>
    public override Task ResetToDefaultsAsync()
    {
        var defaults = new InputSettings();
        KeyboardMapping = defaults.KeyboardMapping;
        CustomKeyMapFile = defaults.CustomKeyMapFile;
        MouseCapture = defaults.MouseCapture;
        JoystickEnabled = defaults.JoystickEnabled;
        JoystickDevice = defaults.JoystickDevice;
        PaddleSensitivity = defaults.PaddleSensitivity;
        MarkAsChanged();
        return Task.CompletedTask;
    }

    partial void OnKeyboardMappingChanged(string value) => MarkAsChanged();

    partial void OnCustomKeyMapFileChanged(string? value) => MarkAsChanged();

    partial void OnMouseCaptureChanged(bool value) => MarkAsChanged();

    partial void OnJoystickEnabledChanged(bool value) => MarkAsChanged();

    partial void OnJoystickDeviceChanged(string value) => MarkAsChanged();

    partial void OnPaddleSensitivityChanged(int value) => MarkAsChanged();
}