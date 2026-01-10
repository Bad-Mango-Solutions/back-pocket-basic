// <copyright file="CharacterPreviewWindow.axaml.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.UI.Views;

using System.Runtime.InteropServices;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

using BadMango.Emulator.Devices;
using BadMango.Emulator.Devices.Interfaces;

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
/// Uses a <see cref="WriteableBitmap"/> for efficient rendering instead of creating
/// individual Rectangle controls for each pixel.
/// </para>
/// </remarks>
public partial class CharacterPreviewWindow : Window
{
    private const int CharWidth = 7;
    private const int CharHeight = 8;
    private const int Scale = 2;
    private const int GridSize = 16;
    private const int CellSpacing = 4;
    private const int CellWidth = (CharWidth * Scale) + CellSpacing;
    private const int CellHeight = (CharHeight * Scale) + CellSpacing;
    private const int PrimarySetOffset = 0x0000;
    private const int SecondarySetOffset = 0x0800;

    // Apple II green phosphor color (BGRA format for Bgra8888)
    private const uint ForegroundColor = 0xFF33FF33;
    private const uint BackgroundColor = 0xFF1A1A1A;
    private const uint CellBackgroundColor = 0xFF222222;

    private byte[]? characterRomData;
    private int currentSetOffset = PrimarySetOffset;
    private WriteableBitmap? characterBitmap;

    /// <summary>
    /// Initializes a new instance of the <see cref="CharacterPreviewWindow"/> class.
    /// </summary>
    public CharacterPreviewWindow()
    {
        this.InitializeComponent();

        // Allow closing with Escape key
        this.KeyDown += this.OnKeyDown;

        // Load and render when window opens
        this.Opened += this.OnWindowOpened;
    }

    /// <summary>
    /// Sets the character ROM data to display.
    /// </summary>
    /// <param name="romData">The 4KB character ROM data.</param>
    public void SetCharacterRomData(byte[] romData)
    {
        this.characterRomData = romData;
        this.RenderCharacters();
    }

    private static string GetCharacterDisplay(int charCode)
    {
        // Map character codes to their display representation:
        // $00-$3F: Inverse characters (show uppercase equivalent)
        // $40-$7F: Flashing characters
        // $80-$BF: Normal uppercase
        // $C0-$FF: Lowercase
        if (charCode < 0x20)
        {
            // Control characters / inverse - show the character they represent
            char c = (char)(charCode + 0x40); // @ A B C ... Z [ \ ] ^ _
            return $"'{c}' Inverse";
        }
        else if (charCode < 0x40)
        {
            // Inverse punctuation/numbers
            char c = (char)charCode; // space ! " # ... / 0-9 : ; < = > ?
            return $"'{c}' Inverse";
        }
        else if (charCode < 0x60)
        {
            // Normal/flashing @ A-Z [ \ ] ^ _
            char c = (char)charCode;
            return $"'{c}' Flashing";
        }
        else if (charCode < 0x80)
        {
            // Lowercase a-z and symbols (flashing zone)
            char c = (char)(charCode - 0x40);
            return $"'{c}' Flashing";
        }
        else if (charCode < 0xA0)
        {
            // Normal inverse representation area
            char c = (char)(charCode - 0x40);
            return $"Control-'{c}'";
        }
        else if (charCode < 0xC0)
        {
            // Normal punctuation/numbers
            char c = (char)(charCode - 0x80);
            return $"'{c}'";
        }
        else if (charCode < 0xE0)
        {
            // Normal @ A-Z [ \ ] ^ _
            char c = (char)(charCode - 0x80);
            return $"'{c}'";
        }
        else
        {
            // Lowercase a-z and symbols
            char c = (char)(charCode - 0x80);
            return $"'{c}'";
        }
    }

    private static void DrawCellBackground(uint[] pixels, int stride, int cellX, int cellY)
    {
        int cellDrawWidth = CellWidth - CellSpacing;
        int cellDrawHeight = CellHeight - CellSpacing;

        for (int y = 0; y < cellDrawHeight; y++)
        {
            for (int x = 0; x < cellDrawWidth; x++)
            {
                int px = cellX + x;
                int py = cellY + y;
                pixels[(py * stride) + px] = CellBackgroundColor;
            }
        }
    }

    private void OnWindowOpened(object? sender, EventArgs e)
    {
        // Try to load default character ROM
        if (DefaultCharacterRom.TryGetRomData(out var romData) && romData != null)
        {
            this.characterRomData = romData;
            this.UpdateCharsetInfoText();
            this.RenderCharacters();
        }
        else
        {
            this.CharsetInfoText.Text = "No character ROM available";
        }
    }

    private void OnCharacterSetChanged(object? sender, RoutedEventArgs e)
    {
        this.currentSetOffset = this.PrimarySetRadio.IsChecked == true
            ? PrimarySetOffset
            : SecondarySetOffset;

        this.UpdateCharsetInfoText();
        this.RenderCharacters();
    }

    private void UpdateCharsetInfoText()
    {
        string setName = this.currentSetOffset == PrimarySetOffset ? "Primary" : "Secondary";
        this.CharsetInfoText.Text = $"{setName} Character Set (Default ROM)";
    }

    private void RenderCharacters()
    {
        if (this.characterRomData == null)
        {
            return;
        }

        this.CharacterCanvas.Children.Clear();

        // Calculate total bitmap size
        int totalWidth = GridSize * CellWidth;
        int totalHeight = GridSize * CellHeight;
        this.CharacterCanvas.Width = totalWidth;
        this.CharacterCanvas.Height = totalHeight;

        // Create or reuse WriteableBitmap for efficient rendering
        if (this.characterBitmap == null ||
            this.characterBitmap.PixelSize.Width != totalWidth ||
            this.characterBitmap.PixelSize.Height != totalHeight)
        {
            this.characterBitmap = new WriteableBitmap(
                new PixelSize(totalWidth, totalHeight),
                new Vector(96, 96),
                Avalonia.Platform.PixelFormat.Bgra8888,
                AlphaFormat.Premul);
        }

        // Create pixel buffer for safe rendering
        int stride = totalWidth;
        var pixels = new uint[totalWidth * totalHeight];

        // Fill with background color
        Array.Fill(pixels, BackgroundColor);

        // Render each character
        for (int charIndex = 0; charIndex < 256; charIndex++)
        {
            int gridRow = charIndex / GridSize;
            int gridCol = charIndex % GridSize;

            int cellX = gridCol * CellWidth;
            int cellY = gridRow * CellHeight;

            // Draw cell background
            DrawCellBackground(pixels, stride, cellX + 2, cellY + 2);

            // Render the character bitmap
            this.RenderCharacterToBitmap(pixels, stride, charIndex, cellX + 4, cellY + 4);
        }

        // Copy pixel data to WriteableBitmap
        using (var framebuffer = this.characterBitmap.Lock())
        {
            var byteSpan = MemoryMarshal.AsBytes(pixels.AsSpan());
            Marshal.Copy(byteSpan.ToArray(), 0, framebuffer.Address, byteSpan.Length);
        }

        // Add the bitmap as an Image control
        var image = new Image
        {
            Source = this.characterBitmap,
            Width = totalWidth,
            Height = totalHeight,
        };

        // Set up pointer tracking for tooltips
        image.PointerMoved += this.OnImagePointerMoved;

        Canvas.SetLeft(image, 0);
        Canvas.SetTop(image, 0);
        this.CharacterCanvas.Children.Add(image);
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
            string charDisplay = GetCharacterDisplay(charIndex);
            string setName = this.currentSetOffset == PrimarySetOffset ? "Primary" : "Secondary";
            string tooltipText = $"${charIndex:X2} ({charIndex}, {charDisplay}) [{setName}]";
            ToolTip.SetTip(image, tooltipText);
        }
    }

    private void RenderCharacterToBitmap(uint[] pixels, int stride, int charCode, int cellX, int cellY)
    {
        if (this.characterRomData == null)
        {
            return;
        }

        // Calculate ROM offset for this character using the current set offset
        // with bounds checking to prevent buffer overruns
        int romOffset = this.currentSetOffset + (charCode * 8);
        if (romOffset < 0 || romOffset + CharHeight > this.characterRomData.Length)
        {
            return; // Invalid offset - skip this character
        }

        // Render each scanline of the character
        for (int scanline = 0; scanline < CharHeight; scanline++)
        {
            byte scanlineData = this.characterRomData[romOffset + scanline];

            // Render each pixel in the scanline (7 bits)
            for (int pixel = 0; pixel < CharWidth; pixel++)
            {
                // Bit 6 is leftmost, bit 0 is rightmost
                bool isSet = (scanlineData & (1 << (6 - pixel))) != 0;

                if (isSet)
                {
                    // Draw scaled pixel
                    for (int sy = 0; sy < Scale; sy++)
                    {
                        for (int sx = 0; sx < Scale; sx++)
                        {
                            int px = cellX + (pixel * Scale) + sx;
                            int py = cellY + (scanline * Scale) + sy;
                            pixels[(py * stride) + px] = ForegroundColor;
                        }
                    }
                }
            }
        }
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        this.Close();
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            this.Close();
            e.Handled = true;
        }
    }
}