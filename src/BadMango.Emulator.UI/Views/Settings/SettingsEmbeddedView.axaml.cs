// <copyright file="SettingsEmbeddedView.axaml.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.UI.Views.Settings;

using Avalonia.Controls;

/// <summary>
/// Embedded view for settings panel (used within the main window).
/// </summary>
public partial class SettingsEmbeddedView : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsEmbeddedView"/> class.
    /// </summary>
    public SettingsEmbeddedView()
    {
        InitializeComponent();
    }
}