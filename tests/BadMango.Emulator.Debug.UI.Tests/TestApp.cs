// <copyright file="TestApp.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.UI.Tests;

using Avalonia;

/// <summary>
/// A minimal Avalonia application for testing purposes.
/// </summary>
public class TestApp : Application
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        // Add Fluent theme for basic styling
        this.Styles.Add(new Avalonia.Themes.Fluent.FluentTheme());
    }
}