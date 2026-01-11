// <copyright file="SyntaxHighlightingManager.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.TextEditor;

/// <summary>
/// Manages syntax highlighting configuration for the text editor.
/// </summary>
/// <remarks>
/// <para>
/// This class provides functionality to determine the appropriate syntax
/// highlighting mode based on file extensions and to allow manual override
/// of the highlighting mode.
/// </para>
/// <para>
/// Supported file types:
/// <list type="bullet">
/// <item><description>.txt - Plain text</description></item>
/// <item><description>.md - Markdown</description></item>
/// <item><description>.json - JSON</description></item>
/// <item><description>.s, .asm, .h - Assembly source</description></item>
/// </list>
/// </para>
/// </remarks>
public static class SyntaxHighlightingManager
{
    private static readonly Dictionary<string, SyntaxLanguage> ExtensionMapping = new(StringComparer.OrdinalIgnoreCase)
    {
        { ".txt", SyntaxLanguage.PlainText },
        { ".md", SyntaxLanguage.Markdown },
        { ".json", SyntaxLanguage.Json },
        { ".s", SyntaxLanguage.Assembly },
        { ".asm", SyntaxLanguage.Assembly },
        { ".h", SyntaxLanguage.Assembly },
    };

    /// <summary>
    /// Gets all supported syntax languages.
    /// </summary>
    /// <returns>A collection of all available syntax languages.</returns>
    public static IReadOnlyCollection<SyntaxLanguage> GetSupportedLanguages()
    {
        return Enum.GetValues<SyntaxLanguage>();
    }

    /// <summary>
    /// Gets the appropriate syntax language for a file based on its extension.
    /// </summary>
    /// <param name="filePath">The file path or name to analyze.</param>
    /// <returns>
    /// The detected <see cref="SyntaxLanguage"/> for the file extension,
    /// or <see cref="SyntaxLanguage.PlainText"/> if the extension is not recognized.
    /// </returns>
    public static SyntaxLanguage GetLanguageForFile(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return SyntaxLanguage.PlainText;
        }

        string extension = Path.GetExtension(filePath);
        if (string.IsNullOrEmpty(extension))
        {
            return SyntaxLanguage.PlainText;
        }

        return ExtensionMapping.TryGetValue(extension, out var language)
            ? language
            : SyntaxLanguage.PlainText;
    }

    /// <summary>
    /// Gets all supported file extensions for a given syntax language.
    /// </summary>
    /// <param name="language">The syntax language to query.</param>
    /// <returns>
    /// A collection of file extensions that map to the specified language.
    /// </returns>
    public static IReadOnlyCollection<string> GetExtensionsForLanguage(SyntaxLanguage language)
    {
        return ExtensionMapping
            .Where(kvp => kvp.Value == language)
            .Select(kvp => kvp.Key)
            .ToList();
    }

    /// <summary>
    /// Gets the TextMate grammar scope for a syntax language.
    /// </summary>
    /// <param name="language">The syntax language.</param>
    /// <returns>
    /// The TextMate grammar scope identifier, or <c>null</c> for plain text.
    /// </returns>
    /// <remarks>
    /// TextMate scopes are used to identify the grammar to apply for syntax highlighting.
    /// </remarks>
    public static string? GetTextMateScope(SyntaxLanguage language)
    {
        return language switch
        {
            SyntaxLanguage.PlainText => null,
            SyntaxLanguage.Markdown => "text.html.markdown",
            SyntaxLanguage.Json => "source.json",
            SyntaxLanguage.Assembly => "source.asm.x86_64", // Closest available grammar
            _ => null,
        };
    }

    /// <summary>
    /// Gets a human-readable display name for a syntax language.
    /// </summary>
    /// <param name="language">The syntax language.</param>
    /// <returns>A display name suitable for UI presentation.</returns>
    public static string GetDisplayName(SyntaxLanguage language)
    {
        return language switch
        {
            SyntaxLanguage.PlainText => "Plain Text",
            SyntaxLanguage.Markdown => "Markdown",
            SyntaxLanguage.Json => "JSON",
            SyntaxLanguage.Assembly => "Assembly",
            _ => language.ToString(),
        };
    }
}