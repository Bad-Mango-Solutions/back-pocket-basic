// <copyright file="SyntaxHighlightingManagerTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.TextEditor.Tests;

/// <summary>
/// Unit tests for the <see cref="SyntaxHighlightingManager"/> class.
/// </summary>
[TestFixture]
public class SyntaxHighlightingManagerTests
{
    /// <summary>
    /// Verifies that GetLanguageForFile returns PlainText for .txt files.
    /// </summary>
    [Test]
    public void GetLanguageForFile_TxtExtension_ReturnsPlainText()
    {
        var result = SyntaxHighlightingManager.GetLanguageForFile("test.txt");

        Assert.That(result, Is.EqualTo(SyntaxLanguage.PlainText));
    }

    /// <summary>
    /// Verifies that GetLanguageForFile returns Markdown for .md files.
    /// </summary>
    [Test]
    public void GetLanguageForFile_MdExtension_ReturnsMarkdown()
    {
        var result = SyntaxHighlightingManager.GetLanguageForFile("readme.md");

        Assert.That(result, Is.EqualTo(SyntaxLanguage.Markdown));
    }

    /// <summary>
    /// Verifies that GetLanguageForFile returns Json for .json files.
    /// </summary>
    [Test]
    public void GetLanguageForFile_JsonExtension_ReturnsJson()
    {
        var result = SyntaxHighlightingManager.GetLanguageForFile("config.json");

        Assert.That(result, Is.EqualTo(SyntaxLanguage.Json));
    }

    /// <summary>
    /// Verifies that GetLanguageForFile returns Assembly for .s files.
    /// </summary>
    [Test]
    public void GetLanguageForFile_SExtension_ReturnsAssembly()
    {
        var result = SyntaxHighlightingManager.GetLanguageForFile("program.s");

        Assert.That(result, Is.EqualTo(SyntaxLanguage.Assembly));
    }

    /// <summary>
    /// Verifies that GetLanguageForFile returns Assembly for .asm files.
    /// </summary>
    [Test]
    public void GetLanguageForFile_AsmExtension_ReturnsAssembly()
    {
        var result = SyntaxHighlightingManager.GetLanguageForFile("program.asm");

        Assert.That(result, Is.EqualTo(SyntaxLanguage.Assembly));
    }

    /// <summary>
    /// Verifies that GetLanguageForFile returns Assembly for .h files.
    /// </summary>
    [Test]
    public void GetLanguageForFile_HExtension_ReturnsAssembly()
    {
        var result = SyntaxHighlightingManager.GetLanguageForFile("definitions.h");

        Assert.That(result, Is.EqualTo(SyntaxLanguage.Assembly));
    }

    /// <summary>
    /// Verifies that GetLanguageForFile returns PlainText for unknown extensions.
    /// </summary>
    [Test]
    public void GetLanguageForFile_UnknownExtension_ReturnsPlainText()
    {
        var result = SyntaxHighlightingManager.GetLanguageForFile("file.xyz");

        Assert.That(result, Is.EqualTo(SyntaxLanguage.PlainText));
    }

    /// <summary>
    /// Verifies that GetLanguageForFile returns PlainText for null input.
    /// </summary>
    [Test]
    public void GetLanguageForFile_NullPath_ReturnsPlainText()
    {
        var result = SyntaxHighlightingManager.GetLanguageForFile(null);

        Assert.That(result, Is.EqualTo(SyntaxLanguage.PlainText));
    }

    /// <summary>
    /// Verifies that GetLanguageForFile returns PlainText for empty string.
    /// </summary>
    [Test]
    public void GetLanguageForFile_EmptyPath_ReturnsPlainText()
    {
        var result = SyntaxHighlightingManager.GetLanguageForFile(string.Empty);

        Assert.That(result, Is.EqualTo(SyntaxLanguage.PlainText));
    }

    /// <summary>
    /// Verifies that GetLanguageForFile handles file paths with directories.
    /// </summary>
    [Test]
    public void GetLanguageForFile_PathWithDirectory_ReturnsCorrectLanguage()
    {
        var result = SyntaxHighlightingManager.GetLanguageForFile("/path/to/file.json");

        Assert.That(result, Is.EqualTo(SyntaxLanguage.Json));
    }

    /// <summary>
    /// Verifies that extension matching is case-insensitive.
    /// </summary>
    [Test]
    public void GetLanguageForFile_MixedCaseExtension_ReturnsCorrectLanguage()
    {
        var result = SyntaxHighlightingManager.GetLanguageForFile("README.MD");

        Assert.That(result, Is.EqualTo(SyntaxLanguage.Markdown));
    }

    /// <summary>
    /// Verifies that GetSupportedLanguages returns all defined languages.
    /// </summary>
    [Test]
    public void GetSupportedLanguages_ReturnsAllLanguages()
    {
        var languages = SyntaxHighlightingManager.GetSupportedLanguages();

        Assert.That(languages, Contains.Item(SyntaxLanguage.PlainText));
        Assert.That(languages, Contains.Item(SyntaxLanguage.Markdown));
        Assert.That(languages, Contains.Item(SyntaxLanguage.Json));
        Assert.That(languages, Contains.Item(SyntaxLanguage.Assembly));
    }

    /// <summary>
    /// Verifies that GetDisplayName returns non-empty string for all languages.
    /// </summary>
    /// <param name="language">The syntax language to test.</param>
    /// <param name="expected">The expected display name.</param>
    [TestCase(SyntaxLanguage.PlainText, "Plain Text")]
    [TestCase(SyntaxLanguage.Markdown, "Markdown")]
    [TestCase(SyntaxLanguage.Json, "JSON")]
    [TestCase(SyntaxLanguage.Assembly, "Assembly")]
    public void GetDisplayName_ReturnsCorrectDisplayName(SyntaxLanguage language, string expected)
    {
        var result = SyntaxHighlightingManager.GetDisplayName(language);

        Assert.That(result, Is.EqualTo(expected));
    }

    /// <summary>
    /// Verifies that GetTextMateScope returns null for PlainText.
    /// </summary>
    [Test]
    public void GetTextMateScope_PlainText_ReturnsNull()
    {
        var result = SyntaxHighlightingManager.GetTextMateScope(SyntaxLanguage.PlainText);

        Assert.That(result, Is.Null);
    }

    /// <summary>
    /// Verifies that GetTextMateScope returns valid scope for Markdown.
    /// </summary>
    [Test]
    public void GetTextMateScope_Markdown_ReturnsValidScope()
    {
        var result = SyntaxHighlightingManager.GetTextMateScope(SyntaxLanguage.Markdown);

        Assert.That(result, Is.EqualTo("text.html.markdown"));
    }

    /// <summary>
    /// Verifies that GetTextMateScope returns valid scope for Json.
    /// </summary>
    [Test]
    public void GetTextMateScope_Json_ReturnsValidScope()
    {
        var result = SyntaxHighlightingManager.GetTextMateScope(SyntaxLanguage.Json);

        Assert.That(result, Is.EqualTo("source.json"));
    }

    /// <summary>
    /// Verifies that GetExtensionsForLanguage returns correct extensions for Assembly.
    /// </summary>
    [Test]
    public void GetExtensionsForLanguage_Assembly_ReturnsMultipleExtensions()
    {
        var extensions = SyntaxHighlightingManager.GetExtensionsForLanguage(SyntaxLanguage.Assembly);

        Assert.Multiple(() =>
        {
            Assert.That(extensions, Contains.Item(".s"));
            Assert.That(extensions, Contains.Item(".asm"));
            Assert.That(extensions, Contains.Item(".h"));
        });
    }
}