// <copyright file="EditorSettingsViewModel.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.UI.ViewModels.Settings;

using BadMango.Emulator.UI.Abstractions.Settings;

using CommunityToolkit.Mvvm.ComponentModel;

/// <summary>
/// ViewModel for assembly editor settings.
/// </summary>
public partial class EditorSettingsViewModel : SettingsPageViewModelBase
{
    [ObservableProperty]
    private string fontFamily = "Cascadia Mono";

    [ObservableProperty]
    private int fontSize = 12;

    [ObservableProperty]
    private int tabSize = 4;

    [ObservableProperty]
    private bool insertSpaces = true;

    [ObservableProperty]
    private bool autoComplete = true;

    [ObservableProperty]
    private bool syntaxHighlighting = true;

    [ObservableProperty]
    private bool lineNumbers = true;

    [ObservableProperty]
    private bool wordWrap;

    [ObservableProperty]
    private string assemblerDialect = "Merlin";

    /// <summary>
    /// Initializes a new instance of the <see cref="EditorSettingsViewModel"/> class.
    /// </summary>
    /// <param name="settingsService">The settings service.</param>
    public EditorSettingsViewModel(ISettingsService settingsService)
        : base(settingsService, "Editor", "CodeIcon", 5)
    {
    }

    /// <summary>
    /// Gets the available font families.
    /// </summary>
    public IReadOnlyList<string> AvailableFontFamilies { get; } =
    [
        "Cascadia Mono",
        "Consolas",
        "Courier New",
        "JetBrains Mono",
        "Fira Code",
        "Source Code Pro",
    ];

    /// <summary>
    /// Gets the available assembler dialects.
    /// </summary>
    public IReadOnlyList<string> AvailableAssemblerDialects { get; } =
    [
        "Merlin",
        "ACME",
        "CA65",
        "DASM",
    ];

    /// <inheritdoc/>
    public override Task LoadAsync()
    {
        var settings = SettingsService.Current.Editor;
        FontFamily = settings.FontFamily;
        FontSize = settings.FontSize;
        TabSize = settings.TabSize;
        InsertSpaces = settings.InsertSpaces;
        AutoComplete = settings.AutoComplete;
        SyntaxHighlighting = settings.SyntaxHighlighting;
        LineNumbers = settings.LineNumbers;
        WordWrap = settings.WordWrap;
        AssemblerDialect = settings.AssemblerDialect;
        HasChanges = false;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public override async Task SaveAsync()
    {
        var current = SettingsService.Current;
        var newSettings = current with
        {
            Editor = new EditorSettings
            {
                FontFamily = FontFamily,
                FontSize = FontSize,
                TabSize = TabSize,
                InsertSpaces = InsertSpaces,
                AutoComplete = AutoComplete,
                SyntaxHighlighting = SyntaxHighlighting,
                LineNumbers = LineNumbers,
                WordWrap = WordWrap,
                AssemblerDialect = AssemblerDialect,
            },
        };
        await SettingsService.SaveAsync(newSettings).ConfigureAwait(false);
        HasChanges = false;
    }

    /// <inheritdoc/>
    public override Task ResetToDefaultsAsync()
    {
        var defaults = new EditorSettings();
        FontFamily = defaults.FontFamily;
        FontSize = defaults.FontSize;
        TabSize = defaults.TabSize;
        InsertSpaces = defaults.InsertSpaces;
        AutoComplete = defaults.AutoComplete;
        SyntaxHighlighting = defaults.SyntaxHighlighting;
        LineNumbers = defaults.LineNumbers;
        WordWrap = defaults.WordWrap;
        AssemblerDialect = defaults.AssemblerDialect;
        MarkAsChanged();
        return Task.CompletedTask;
    }

    partial void OnFontFamilyChanged(string value) => MarkAsChanged();

    partial void OnFontSizeChanged(int value) => MarkAsChanged();

    partial void OnTabSizeChanged(int value) => MarkAsChanged();

    partial void OnInsertSpacesChanged(bool value) => MarkAsChanged();

    partial void OnAutoCompleteChanged(bool value) => MarkAsChanged();

    partial void OnSyntaxHighlightingChanged(bool value) => MarkAsChanged();

    partial void OnLineNumbersChanged(bool value) => MarkAsChanged();

    partial void OnWordWrapChanged(bool value) => MarkAsChanged();

    partial void OnAssemblerDialectChanged(string value) => MarkAsChanged();
}