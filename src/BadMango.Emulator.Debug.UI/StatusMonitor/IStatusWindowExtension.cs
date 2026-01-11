// <copyright file="IStatusWindowExtension.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.UI.StatusMonitor;

using Avalonia.Controls;

/// <summary>
/// Interface for device-registered status window display components.
/// </summary>
/// <remarks>
/// <para>
/// This interface allows devices (like PocketWatch) to register UI components
/// that will be displayed in the status monitor window. Each extension provides
/// a control that displays device-specific status information.
/// </para>
/// <para>
/// Extensions are automatically discovered and rendered in the status monitor
/// window's extension panel, allowing for modular and extensible status display.
/// </para>
/// </remarks>
public interface IStatusWindowExtension
{
    /// <summary>
    /// Gets the display name of the extension shown in the status window header.
    /// </summary>
    /// <value>A human-readable name for the extension.</value>
    string Name { get; }

    /// <summary>
    /// Gets the sort order for positioning the extension in the status window.
    /// </summary>
    /// <value>Lower values appear first. Default should be 100.</value>
    int Order { get; }

    /// <summary>
    /// Creates the UI control to display in the status monitor window.
    /// </summary>
    /// <returns>An Avalonia control that displays the extension's status information.</returns>
    /// <remarks>
    /// This method is called once when the status monitor window is opened.
    /// The returned control should update its display when <see cref="UpdateDisplay"/> is called.
    /// </remarks>
    Control CreateControl();

    /// <summary>
    /// Updates the display with current device state.
    /// </summary>
    /// <remarks>
    /// This method is called periodically by the status monitor's timer.
    /// Implementations should update any displayed values to reflect current state.
    /// </remarks>
    void UpdateDisplay();
}