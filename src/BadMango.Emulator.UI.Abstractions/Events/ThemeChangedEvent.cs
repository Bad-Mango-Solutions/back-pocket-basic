// <copyright file="ThemeChangedEvent.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.UI.Abstractions.Events;

/// <summary>
/// Event raised when the application theme has changed.
/// </summary>
/// <param name="IsDarkTheme">True if the new theme is dark, false if light.</param>
public record ThemeChangedEvent(bool IsDarkTheme);