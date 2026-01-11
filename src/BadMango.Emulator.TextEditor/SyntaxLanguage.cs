// <copyright file="SyntaxLanguage.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.TextEditor;

/// <summary>
/// Defines the supported syntax highlighting languages for the text editor.
/// </summary>
public enum SyntaxLanguage
{
    /// <summary>
    /// Plain text with no syntax highlighting.
    /// </summary>
    PlainText,

    /// <summary>
    /// Markdown formatting.
    /// </summary>
    Markdown,

    /// <summary>
    /// JSON format.
    /// </summary>
    Json,

    /// <summary>
    /// Assembly language source code.
    /// </summary>
    Assembly,
}