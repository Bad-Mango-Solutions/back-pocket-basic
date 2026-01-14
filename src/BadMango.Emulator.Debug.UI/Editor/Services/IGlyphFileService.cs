// <copyright file="IGlyphFileService.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.UI.Editor.Services;

using Models;

/// <summary>
/// Service interface for glyph file operations and dialogs.
/// </summary>
public interface IGlyphFileService
{
    /// <summary>
    /// Shows the open file dialog for glyph files.
    /// </summary>
    /// <returns>The selected file path, or null if cancelled.</returns>
    Task<string?> ShowOpenDialogAsync();

    /// <summary>
    /// Shows the save file dialog for glyph files.
    /// </summary>
    /// <returns>The selected file path, or null if cancelled.</returns>
    Task<string?> ShowSaveDialogAsync();

    /// <summary>
    /// Shows the open file dialog for ROM files.
    /// </summary>
    /// <returns>The selected file path, or null if cancelled.</returns>
    Task<string?> ShowOpenRomDialogAsync();

    /// <summary>
    /// Shows the save file dialog for image export.
    /// </summary>
    /// <returns>The selected file path, or null if cancelled.</returns>
    Task<string?> ShowSaveImageDialogAsync();

    /// <summary>
    /// Shows a confirmation dialog for discarding unsaved changes.
    /// </summary>
    /// <returns>True if the user wants to discard changes; otherwise, false.</returns>
    Task<bool> ShowConfirmDiscardDialogAsync();

    /// <summary>
    /// Shows an error dialog with the specified message.
    /// </summary>
    /// <param name="message">The error message to display.</param>
    /// <returns>A task representing the async operation.</returns>
    Task ShowErrorDialogAsync(string message);

    /// <summary>
    /// Exports a character set preview image to a PNG file.
    /// </summary>
    /// <param name="glyphFile">The glyph file to export.</param>
    /// <param name="useAlternateSet">Whether to export the alternate set.</param>
    /// <param name="path">The destination file path.</param>
    /// <returns>A task representing the async operation.</returns>
    Task ExportCharacterSetPreview(GlyphFile glyphFile, bool useAlternateSet, string path);
}