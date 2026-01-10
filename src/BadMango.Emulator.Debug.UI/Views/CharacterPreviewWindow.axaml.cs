// <copyright file="CharacterPreviewWindow.axaml.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.UI.Views;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;

using BadMango.Emulator.Devices;
using BadMango.Emulator.Devices.Interfaces;

/// <summary>
/// Character preview window displaying the Apple II character set as a visual grid.
/// </summary>
/// <remarks>
/// <para>
/// This window renders all 256 characters from the character ROM in a 16x16 grid.
/// Each character is displayed at 2x scale (14x16 pixels) for readability.
/// </para>
/// <para>
/// The window loads the default character ROM when opened and displays the
/// bitmap patterns for each character code from 0x00 to 0xFF.
/// </para>
/// </remarks>
public partial class CharacterPreviewWindow : Window
{
    private const int CharWidth = 7;
    private const int CharHeight = 8;
    private const int Scale = 2;
    private const int GridSize = 16;
    private const int CellWidth = (CharWidth * Scale) + 2;
    private const int CellHeight = (CharHeight * Scale) + 2;

    private byte[]? characterRomData;

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

    private void OnWindowOpened(object? sender, EventArgs e)
    {
        // Try to load default character ROM
        if (DefaultCharacterRom.TryGetRomData(out var romData) && romData != null)
        {
            this.characterRomData = romData;
            this.CharsetInfoText.Text = "Primary Character Set (Default ROM)";
            this.RenderCharacters();
        }
        else
        {
            this.CharsetInfoText.Text = "No character ROM available";
        }
    }

    private void RenderCharacters()
    {
        if (this.characterRomData == null)
        {
            return;
        }

        this.CharacterCanvas.Children.Clear();

        var foregroundBrush = new SolidColorBrush(Color.FromRgb(0x33, 0xFF, 0x33)); // Apple II green
        var backgroundBrush = new SolidColorBrush(Color.FromRgb(0x1A, 0x1A, 0x1A));

        for (int charIndex = 0; charIndex < 256; charIndex++)
        {
            int gridRow = charIndex / GridSize;
            int gridCol = charIndex % GridSize;

            int cellX = gridCol * CellWidth;
            int cellY = gridRow * CellHeight;

            // Render each character
            this.RenderCharacter(charIndex, cellX, cellY, foregroundBrush);
        }
    }

    private void RenderCharacter(int charCode, int cellX, int cellY, IBrush foregroundBrush)
    {
        if (this.characterRomData == null)
        {
            return;
        }

        // Calculate ROM offset for this character (primary set only for now)
        int romOffset = charCode * 8;

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
                    var rect = new Rectangle
                    {
                        Width = Scale,
                        Height = Scale,
                        Fill = foregroundBrush,
                    };

                    Canvas.SetLeft(rect, cellX + (pixel * Scale));
                    Canvas.SetTop(rect, cellY + (scanline * Scale));
                    this.CharacterCanvas.Children.Add(rect);
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