// <copyright file="GlyphFileService.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.UI.Editor.Services;

using Avalonia.Controls;
using Avalonia.Platform.Storage;

using Models;

using Rendering;

/// <summary>
/// Service implementation for glyph file operations and dialogs.
/// </summary>
public sealed class GlyphFileService : IGlyphFileService
{
    private readonly Window parentWindow;

    /// <summary>
    /// Initializes a new instance of the <see cref="GlyphFileService"/> class.
    /// </summary>
    /// <param name="parentWindow">The parent window for dialogs.</param>
    public GlyphFileService(Window parentWindow)
    {
        this.parentWindow = parentWindow;
    }

    /// <inheritdoc />
    public async Task<string?> ShowOpenDialogAsync()
    {
        var storageProvider = parentWindow.StorageProvider;

        var result = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open Glyph File",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("Glyph Files") { Patterns = ["*.glyph"] },
                new FilePickerFileType("Memory Image Files") { Patterns = ["*.bin", "*.rom"] },
                new FilePickerFileType("All Files") { Patterns = ["*.*"] },
            ],
        });

        return result.Count > 0 ? result[0].TryGetLocalPath() : null;
    }

    /// <inheritdoc />
    public async Task<string?> ShowSaveDialogAsync()
    {
        var storageProvider = parentWindow.StorageProvider;

        var result = await storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save Glyph File",
            DefaultExtension = ".glyph",
            FileTypeChoices =
            [
                new FilePickerFileType("Glyph Files") { Patterns = ["*.glyph"] },
                new FilePickerFileType("Memory Image Files") { Patterns = ["*.bin", "*.rom"] },
            ],
        });

        return result?.TryGetLocalPath();
    }

    /// <inheritdoc />
    public async Task<string?> ShowOpenRomDialogAsync()
    {
        var storageProvider = parentWindow.StorageProvider;

        var result = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Import Character ROM",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("Memory Image Files") { Patterns = ["*.rom", "*.bin"] },
                new FilePickerFileType("All Files") { Patterns = ["*.*"] },
            ],
        });

        return result.Count > 0 ? result[0].TryGetLocalPath() : null;
    }

    /// <inheritdoc />
    public async Task<string?> ShowSaveImageDialogAsync()
    {
        var storageProvider = parentWindow.StorageProvider;

        var result = await storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export Preview Image",
            DefaultExtension = ".png",
            FileTypeChoices =
            [
                new FilePickerFileType("PNG Image") { Patterns = ["*.png"] },
            ],
        });

        return result?.TryGetLocalPath();
    }

    /// <inheritdoc />
    public async Task<bool> ShowConfirmDiscardDialogAsync()
    {
        // For now, return true (discard). In a full implementation,
        // this would show a dialog asking the user to confirm.
        return await Task.FromResult(true);
    }

    /// <inheritdoc />
    public async Task ShowErrorDialogAsync(string message)
    {
        // For now, just log to console. In a full implementation,
        // this would show a dialog with the error message.
        await Task.CompletedTask;
        Console.WriteLine($"Error: {message}");
    }

    /// <inheritdoc />
    public Task ExportCharacterSetPreview(GlyphFile glyphFile, bool useAlternateSet, string path)
    {
        ArgumentNullException.ThrowIfNull(glyphFile);
        ArgumentNullException.ThrowIfNull(path);

        const int gridSize = 16;
        const int cellSpacing = 2;
        const int scale = 2;
        int cellWidth = (CharacterRenderer.CharacterWidth * scale) + cellSpacing;
        int cellHeight = (CharacterRenderer.CharacterHeight * scale) + cellSpacing;
        int totalWidth = gridSize * cellWidth;
        int totalHeight = gridSize * cellHeight;

        using var pixelBuffer = new PixelBuffer(totalWidth, totalHeight);
        pixelBuffer.Fill(DisplayColors.DarkGray);

        var pixels = pixelBuffer.GetPixels();
        var glyphData = glyphFile.ToByteArray();
        int romOffset = useAlternateSet ? GlyphFile.CharacterSetSize : 0;

        for (int charIndex = 0; charIndex < 256; charIndex++)
        {
            int gridRow = charIndex / gridSize;
            int gridCol = charIndex % gridSize;
            int cellX = gridCol * cellWidth;
            int cellY = gridRow * cellHeight;

            // Render character
            CharacterRenderer.RenderCharacterScaled(
                pixels,
                totalWidth,
                glyphData,
                charIndex,
                romOffset,
                cellX + 1,
                cellY + 1,
                DisplayColors.GreenPhosphor,
                scale);
        }

        pixelBuffer.Commit();

        // Save bitmap to file
        using var stream = File.Create(path);
        pixelBuffer.Bitmap.Save(stream);

        return Task.CompletedTask;
    }
}