// <copyright file="CharacterPreviewWindow.axaml.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.UI.Views;

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

using Devices;
using Devices.Interfaces;

using Rendering;

/// <summary>
/// Character preview window displaying the Apple II character set as a visual grid.
/// </summary>
/// <remarks>
/// <para>
/// This window renders all 256 characters from the character ROM in a 16x16 grid.
/// Each character is displayed at 2x scale with tooltips showing hex, decimal, and
/// character representations when hovering over a cell.
/// </para>
/// <para>
/// The window loads the default character ROM when opened and displays the
/// bitmap patterns for each character code from 0x00 to 0xFF.
/// </para>
/// <para>
/// Uses the shared <see cref="PixelBuffer"/> and <see cref="CharacterRenderer"/>
/// infrastructure for efficient rendering.
/// </para>
/// </remarks>
public partial class CharacterPreviewWindow : Window
{
    private const int Scale = 2;
    private const int GridSize = 16;
    private const int CellSpacing = 4;
    private const int CellWidth = (CharacterRenderer.CharacterWidth * Scale) + CellSpacing;
    private const int CellHeight = (CharacterRenderer.CharacterHeight * Scale) + CellSpacing;
    private const int PrimarySetOffset = 0x0000;
    private const int SecondarySetOffset = 0x0800;

    private byte[]? characterRomData;
    private ICharacterRomProvider? characterRomProvider;
    private int currentSetOffset = PrimarySetOffset;
    private PixelBuffer? pixelBuffer;

    /// <summary>
    /// Initializes a new instance of the <see cref="CharacterPreviewWindow"/> class.
    /// </summary>
    public CharacterPreviewWindow()
    {
        InitializeComponent();

        // Allow closing with Escape key
        KeyDown += OnKeyDown;

        // Load and render when window opens
        Opened += OnWindowOpened;
    }

    /// <summary>
    /// Sets the character ROM data to display.
    /// </summary>
    /// <param name="romData">The 4KB character ROM data.</param>
    public void SetCharacterRomData(byte[] romData)
    {
        characterRomData = romData;
        RenderCharacters();
    }

    /// <summary>
    /// Sets the character ROM provider to use for getting ROM data.
    /// </summary>
    /// <param name="provider">The character ROM provider (typically the video device).</param>
    public void SetCharacterRomProvider(ICharacterRomProvider provider)
    {
        characterRomProvider = provider;
    }

    /// <inheritdoc/>
    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);

        // Clean up pixel buffer
        pixelBuffer?.Dispose();
        pixelBuffer = null;
    }

    private void OnWindowOpened(object? sender, EventArgs e)
    {
        // Try to get ROM data from the provider first (e.g., video device from machine)
        if (characterRomProvider is { IsCharacterRomLoaded: true })
        {
            Memory<byte> romMemory = characterRomProvider.GetCharacterRomData();
            if (!romMemory.IsEmpty)
            {
                characterRomData = romMemory.ToArray();
                UpdateCharsetInfoText(fromProvider: true);
                RenderCharacters();
                return;
            }
        }

        // Fall back to default character ROM if no provider or provider has no ROM
        if (DefaultCharacterRom.TryGetRomData(out var romData) && romData != null)
        {
            characterRomData = romData;
            UpdateCharsetInfoText(fromProvider: false);
            RenderCharacters();
        }
        else
        {
            CharsetInfoText.Text = "No character ROM available";
        }
    }

    private void OnCharacterSetChanged(object? sender, RoutedEventArgs e)
    {
        currentSetOffset = PrimarySetRadio.IsChecked == true
            ? PrimarySetOffset
            : SecondarySetOffset;

        bool fromProvider = characterRomProvider != null && characterRomProvider.IsCharacterRomLoaded;
        UpdateCharsetInfoText(fromProvider);
        RenderCharacters();
    }

    private void UpdateCharsetInfoText(bool fromProvider = false)
    {
        string setName = currentSetOffset == PrimarySetOffset ? "Primary" : "Secondary";
        string source = fromProvider ? "Machine ROM" : "Default ROM";
        CharsetInfoText.Text = $"{setName} Character Set ({source})";
    }

    private void RenderCharacters()
    {
        if (characterRomData == null)
        {
            return;
        }

        CharacterCanvas.Children.Clear();

        // Calculate total bitmap size
        const int totalWidth = GridSize * CellWidth;
        const int totalHeight = GridSize * CellHeight;
        CharacterCanvas.Width = totalWidth;
        CharacterCanvas.Height = totalHeight;

        // Create or reuse PixelBuffer
        if (pixelBuffer == null ||
            pixelBuffer.Width != totalWidth ||
            pixelBuffer.Height != totalHeight)
        {
            pixelBuffer?.Dispose();
            pixelBuffer = new PixelBuffer(totalWidth, totalHeight);
        }

        // Fill with background color
        pixelBuffer.Fill(DisplayColors.DarkGray);

        // Get pixel span for rendering
        var pixels = pixelBuffer.GetPixels();

        // Render each character
        for (int charIndex = 0; charIndex < 256; charIndex++)
        {
            int gridRow = charIndex / GridSize;
            int gridCol = charIndex % GridSize;

            int cellX = gridCol * CellWidth;
            int cellY = gridRow * CellHeight;

            // Draw cell background
            ScaledPixelWriter.FillRectangle(
                pixels,
                totalWidth,
                cellX + 2,
                cellY + 2,
                CellWidth - CellSpacing,
                CellHeight - CellSpacing,
                DisplayColors.CellBackground);

            // Render the character bitmap
            CharacterRenderer.RenderCharacterScaled(
                pixels,
                totalWidth,
                characterRomData,
                charIndex,
                currentSetOffset,
                cellX + 4,
                cellY + 4,
                DisplayColors.GreenPhosphor,
                Scale);
        }

        // Commit pixel buffer to bitmap
        pixelBuffer.Commit();

        // Add the bitmap as an Image control
        var image = new Image
        {
            Source = pixelBuffer.Bitmap,
            Width = totalWidth,
            Height = totalHeight,
        };

        // Set up pointer tracking for tooltips
        image.PointerMoved += OnImagePointerMoved;

        Canvas.SetLeft(image, 0);
        Canvas.SetTop(image, 0);
        CharacterCanvas.Children.Add(image);
    }

    private void OnImagePointerMoved(object? sender, PointerEventArgs e)
    {
        if (sender is not Image image)
        {
            return;
        }

        var position = e.GetPosition(image);
        int gridCol = (int)(position.X / CellWidth);
        int gridRow = (int)(position.Y / CellHeight);

        if (gridCol >= 0 && gridCol < GridSize && gridRow >= 0 && gridRow < GridSize)
        {
            int charIndex = (gridRow * GridSize) + gridCol;
            string charDisplay = CharacterRenderer.GetCharacterDisplayString(charIndex);
            string setName = currentSetOffset == PrimarySetOffset ? "Primary" : "Secondary";
            string tooltipText = $"${charIndex:X2} ({charIndex}, {charDisplay}) [{setName}]";
            ToolTip.SetTip(image, tooltipText);
        }
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape) { return; }

        Close();
        e.Handled = true;
    }
}