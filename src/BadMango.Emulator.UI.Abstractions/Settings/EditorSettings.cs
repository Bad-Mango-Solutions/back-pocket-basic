// <copyright file="EditorSettings.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.UI.Abstractions.Settings;

/// <summary>
/// Assembly editor settings.
/// </summary>
public record EditorSettings
{
    /// <summary>
    /// Gets the editor font family.
    /// </summary>
    public string FontFamily { get; init; } = "Cascadia Mono";

    /// <summary>
    /// Gets the editor font size.
    /// </summary>
    public int FontSize { get; init; } = 12;

    /// <summary>
    /// Gets the number of spaces per tab.
    /// </summary>
    public int TabSize { get; init; } = 4;

    /// <summary>
    /// Gets a value indicating whether to use spaces instead of tabs.
    /// </summary>
    public bool InsertSpaces { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether auto-completion is enabled.
    /// </summary>
    public bool AutoComplete { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether syntax highlighting is enabled.
    /// </summary>
    public bool SyntaxHighlighting { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether line numbers are shown.
    /// </summary>
    public bool LineNumbers { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether long lines are wrapped.
    /// </summary>
    public bool WordWrap { get; init; }

    /// <summary>
    /// Gets the default assembler dialect (Merlin, ACME, CA65, etc.).
    /// </summary>
    public string AssemblerDialect { get; init; } = "Merlin";
}