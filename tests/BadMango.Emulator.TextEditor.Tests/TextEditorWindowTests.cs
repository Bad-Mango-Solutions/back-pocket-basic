// <copyright file="TextEditorWindowTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.TextEditor.Tests;

using Avalonia.Controls;
using Avalonia.Headless.NUnit;

/// <summary>
/// Functional tests for the <see cref="TextEditorWindow"/> class.
/// </summary>
[TestFixture]
public class TextEditorWindowTests
{
    /// <summary>
    /// Verifies that TextEditorWindow can be created successfully.
    /// </summary>
    [AvaloniaTest]
    public void TextEditorWindow_CanBeCreated()
    {
        // Act
        var window = new TextEditorWindow();

        // Assert
        Assert.That(window, Is.Not.Null);
    }

    /// <summary>
    /// Verifies that TextEditorWindow has correct default title.
    /// </summary>
    [AvaloniaTest]
    public void TextEditorWindow_HasDefaultTitle()
    {
        // Act
        var window = new TextEditorWindow();

        // Assert
        Assert.That(window.Title, Is.EqualTo("Text Editor"));
    }

    /// <summary>
    /// Verifies that TextEditorWindow has correct default dimensions.
    /// </summary>
    [AvaloniaTest]
    public void TextEditorWindow_HasCorrectDefaultDimensions()
    {
        // Act
        var window = new TextEditorWindow();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(window.Width, Is.EqualTo(800));
            Assert.That(window.Height, Is.EqualTo(600));
            Assert.That(window.MinWidth, Is.EqualTo(400));
            Assert.That(window.MinHeight, Is.EqualTo(300));
        });
    }

    /// <summary>
    /// Verifies that TextEditorWindow contains the Editor control.
    /// </summary>
    [AvaloniaTest]
    public void TextEditorWindow_ContainsEditorControl()
    {
        // Arrange
        var window = new TextEditorWindow();

        // Act
        var editor = window.FindControl<AvaloniaEdit.TextEditor>("Editor");

        // Assert
        Assert.That(editor, Is.Not.Null);
    }

    /// <summary>
    /// Verifies that the Editor control is not read-only by default.
    /// </summary>
    [AvaloniaTest]
    public void TextEditorWindow_EditorIsNotReadOnly()
    {
        // Arrange
        var window = new TextEditorWindow();
        var editor = window.FindControl<AvaloniaEdit.TextEditor>("Editor");

        // Assert
        Assert.That(editor, Is.Not.Null);
        Assert.That(editor!.IsReadOnly, Is.False);
    }

    /// <summary>
    /// Verifies that the Editor control is focusable.
    /// </summary>
    [AvaloniaTest]
    public void TextEditorWindow_EditorIsFocusable()
    {
        // Arrange
        var window = new TextEditorWindow();
        var editor = window.FindControl<AvaloniaEdit.TextEditor>("Editor");

        // Assert
        Assert.That(editor, Is.Not.Null);
        Assert.That(editor!.Focusable, Is.True);
    }

    /// <summary>
    /// Verifies that the Editor control shows line numbers.
    /// </summary>
    [AvaloniaTest]
    public void TextEditorWindow_EditorShowsLineNumbers()
    {
        // Arrange
        var window = new TextEditorWindow();
        var editor = window.FindControl<AvaloniaEdit.TextEditor>("Editor");

        // Assert
        Assert.That(editor, Is.Not.Null);
        Assert.That(editor!.ShowLineNumbers, Is.True);
    }

    /// <summary>
    /// Verifies that the Editor control has a monospace font.
    /// </summary>
    [AvaloniaTest]
    public void TextEditorWindow_EditorHasMonospaceFont()
    {
        // Arrange
        var window = new TextEditorWindow();
        var editor = window.FindControl<AvaloniaEdit.TextEditor>("Editor");

        // Assert
        Assert.That(editor, Is.Not.Null);
        var fontFamily = editor!.FontFamily.ToString();
        Assert.That(fontFamily, Does.Contain("Cascadia Mono").Or.Contain("Consolas").Or.Contain("Courier"));
    }

    /// <summary>
    /// Verifies that the Editor text can be set programmatically.
    /// </summary>
    [AvaloniaTest]
    public void TextEditorWindow_EditorTextCanBeSet()
    {
        // Arrange
        var window = new TextEditorWindow();
        var editor = window.FindControl<AvaloniaEdit.TextEditor>("Editor");
        const string testContent = "Hello, World!";

        // Act
        editor!.Text = testContent;

        // Assert
        Assert.That(editor.Text, Is.EqualTo(testContent));
    }

    /// <summary>
    /// Verifies that the Editor text can be modified.
    /// </summary>
    [AvaloniaTest]
    public void TextEditorWindow_EditorTextCanBeModified()
    {
        // Arrange
        var window = new TextEditorWindow();
        var editor = window.FindControl<AvaloniaEdit.TextEditor>("Editor");
        editor!.Text = "Initial";

        // Act
        editor.Text = "Modified";

        // Assert
        Assert.That(editor.Text, Is.EqualTo("Modified"));
    }

    /// <summary>
    /// Verifies that multiline text can be set in the editor.
    /// </summary>
    [AvaloniaTest]
    public void TextEditorWindow_EditorSupportsMultilineText()
    {
        // Arrange
        var window = new TextEditorWindow();
        var editor = window.FindControl<AvaloniaEdit.TextEditor>("Editor");
        const string multilineContent = "Line 1\nLine 2\nLine 3";

        // Act
        editor!.Text = multilineContent;

        // Assert
        Assert.That(editor.Text, Is.EqualTo(multilineContent));
        Assert.That(editor.Document.LineCount, Is.EqualTo(3));
    }

    /// <summary>
    /// Verifies that TextEditorWindow contains the syntax menu.
    /// </summary>
    [AvaloniaTest]
    public void TextEditorWindow_ContainsSyntaxMenu()
    {
        // Arrange
        var window = new TextEditorWindow();

        // Act
        var syntaxMenu = window.FindControl<Avalonia.Controls.MenuItem>("SyntaxMenuItem");

        // Assert
        Assert.That(syntaxMenu, Is.Not.Null);
    }

    /// <summary>
    /// Verifies that the syntax menu has items for all supported languages.
    /// </summary>
    [AvaloniaTest]
    public void TextEditorWindow_SyntaxMenuHasLanguageItems()
    {
        // Arrange
        var window = new TextEditorWindow();
        var syntaxMenu = window.FindControl<Avalonia.Controls.MenuItem>("SyntaxMenuItem");
        var supportedLanguages = SyntaxHighlightingManager.GetSupportedLanguages();

        // Assert
        Assert.That(syntaxMenu, Is.Not.Null);
        Assert.That(syntaxMenu!.Items.Count, Is.EqualTo(supportedLanguages.Count));
    }

    /// <summary>
    /// Verifies that the status bar displays file path.
    /// </summary>
    [AvaloniaTest]
    public void TextEditorWindow_StatusBarShowsFilePath()
    {
        // Arrange
        var window = new TextEditorWindow();

        // Act
        var filePathText = window.FindControl<Avalonia.Controls.TextBlock>("FilePathText");

        // Assert
        Assert.That(filePathText, Is.Not.Null);
        Assert.That(filePathText!.Text, Is.EqualTo("Untitled"));
    }

    /// <summary>
    /// Verifies that the status bar displays syntax type.
    /// </summary>
    [AvaloniaTest]
    public void TextEditorWindow_StatusBarShowsSyntaxType()
    {
        // Arrange
        var window = new TextEditorWindow();

        // Act
        var syntaxText = window.FindControl<Avalonia.Controls.TextBlock>("SyntaxText");

        // Assert
        Assert.That(syntaxText, Is.Not.Null);
        Assert.That(syntaxText!.Text, Is.EqualTo("Plain Text"));
    }

    /// <summary>
    /// Verifies that the status bar displays cursor position.
    /// </summary>
    [AvaloniaTest]
    public void TextEditorWindow_StatusBarShowsCursorPosition()
    {
        // Arrange
        var window = new TextEditorWindow();

        // Act
        var cursorPositionText = window.FindControl<Avalonia.Controls.TextBlock>("CursorPositionText");

        // Assert
        Assert.That(cursorPositionText, Is.Not.Null);
        Assert.That(cursorPositionText!.Text, Does.Match(@"Ln \d+, Col \d+"));
    }

    /// <summary>
    /// Verifies that the editor document exists and is editable.
    /// </summary>
    [AvaloniaTest]
    public void TextEditorWindow_EditorDocumentIsEditable()
    {
        // Arrange
        var window = new TextEditorWindow();
        var editor = window.FindControl<AvaloniaEdit.TextEditor>("Editor");

        // Assert
        Assert.That(editor, Is.Not.Null);
        Assert.That(editor!.Document, Is.Not.Null);

        // Act - Insert text via document
        editor.Document.Insert(0, "Test insert");

        // Assert
        Assert.That(editor.Text, Is.EqualTo("Test insert"));
    }

    /// <summary>
    /// Verifies that the editor TextArea exists and is accessible.
    /// </summary>
    [AvaloniaTest]
    public void TextEditorWindow_EditorTextAreaExists()
    {
        // Arrange
        var window = new TextEditorWindow();
        var editor = window.FindControl<AvaloniaEdit.TextEditor>("Editor");

        // Assert
        Assert.That(editor, Is.Not.Null);
        Assert.That(editor!.TextArea, Is.Not.Null);
    }

    /// <summary>
    /// Verifies that the editor caret exists and can be positioned.
    /// </summary>
    [AvaloniaTest]
    public void TextEditorWindow_EditorCaretCanBePositioned()
    {
        // Arrange
        var window = new TextEditorWindow();
        var editor = window.FindControl<AvaloniaEdit.TextEditor>("Editor");
        editor!.Text = "Hello World";

        // Act
        editor.TextArea.Caret.Offset = 5;

        // Assert
        Assert.That(editor.TextArea.Caret.Offset, Is.EqualTo(5));
    }
}