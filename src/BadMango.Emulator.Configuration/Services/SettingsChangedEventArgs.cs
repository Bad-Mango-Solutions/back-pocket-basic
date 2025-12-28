// <copyright file="SettingsChangedEventArgs.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Configuration.Services;

/// <summary>
/// Event arguments for settings change notifications.
/// </summary>
public record SettingsChangedEventArgs
{
    /// <summary>
    /// Gets the keys of settings that changed.
    /// </summary>
    public IReadOnlyList<string> ChangedKeys { get; init; } = [];

    /// <summary>
    /// Gets a value indicating whether this was a full reload of all settings.
    /// </summary>
    public bool IsFullReload { get; init; }
}