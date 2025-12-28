// <copyright file="SettingsChangedEvent.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Configuration.Events;

/// <summary>
/// Event raised when application settings have changed.
/// This event can be published through any pub/sub system (e.g., IEventAggregator).
/// </summary>
/// <param name="SettingName">The name of the setting that changed, or null if multiple settings changed.</param>
/// <param name="NewValue">The new value of the setting, or null if not applicable.</param>
public record SettingsChangedEvent(string? SettingName, object? NewValue);