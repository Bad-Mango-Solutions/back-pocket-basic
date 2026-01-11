// <copyright file="TestApp.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.TextEditor.Tests;

using Avalonia;
using Avalonia.Markup.Xaml.Styling;

/// <summary>
/// A minimal Avalonia application for testing purposes.
/// </summary>
public class TestApp : Application
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        // Add Fluent theme
        this.Styles.Add(new Avalonia.Themes.Fluent.FluentTheme());

        try
        {
            // Try to load AvaloniaEdit styles
            var avaloniaEditStyles = new StyleInclude(new Uri("avares://AvaloniaEdit"))
            {
                Source = new Uri("avares://AvaloniaEdit/Themes/Fluent/AvaloniaEdit.xaml"),
            };
            this.Styles.Add(avaloniaEditStyles);
        }
        catch
        {
            // AvaloniaEdit styles may not be available in all test scenarios
        }
    }
}