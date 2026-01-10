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
/// Each character is displayed at 2x scale with labels showing hex, decimal, and
/// character representations.
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
    private const int CellSpacing = 8;
    private const int LabelHeight = 28;
    private const int CellWidth = (CharWidth * Scale) + CellSpacing;
    private const int CellHeight = (CharHeight * Scale) + LabelHeight + CellSpacing;

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
            return $"'{c}'";
        }
        else if (charCode < 0x40)
        {
            // Inverse punctuation/numbers
            char c = (char)(charCode + 0x20); // space ! " # ... / 0-9 : ; < = > ?
            return $"'{c}'";
        }
        else if (charCode < 0x60)
        {
            // Normal/flashing @ A-Z [ \ ] ^ _
            char c = (char)charCode;
            return $"'{c}'";
        }
        else if (charCode < 0x80)
        {
            // Lowercase a-z and symbols (flashing zone)
            char c = (char)charCode;
            return $"'{c}'";
        }
        else if (charCode < 0xA0)
        {
            // Normal inverse representation area
            char c = (char)(charCode - 0x40);
            return $"'{c}'";
        }
        else if (charCode < 0xC0)
        {
            // Normal punctuation/numbers
            char c = (char)(charCode - 0x60);
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

        // Set canvas size to fit all cells
        int totalWidth = GridSize * CellWidth;
        int totalHeight = GridSize * CellHeight;
        this.CharacterCanvas.Width = totalWidth;
        this.CharacterCanvas.Height = totalHeight;

        var foregroundBrush = new SolidColorBrush(Color.FromRgb(0x33, 0xFF, 0x33)); // Apple II green
        var labelBrush = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88)); // Gray for labels
        var cellBackgroundBrush = new SolidColorBrush(Color.FromRgb(0x22, 0x22, 0x22)); // Slightly lighter background

        for (int charIndex = 0; charIndex < 256; charIndex++)
        {
            int gridRow = charIndex / GridSize;
            int gridCol = charIndex % GridSize;

            int cellX = gridCol * CellWidth;
            int cellY = gridRow * CellHeight;

            // Draw cell background
            var cellBackground = new Rectangle
            {
                Width = CellWidth - 4,
                Height = CellHeight - 4,
                Fill = cellBackgroundBrush,
            };
            Canvas.SetLeft(cellBackground, cellX + 2);
            Canvas.SetTop(cellBackground, cellY + 2);
            this.CharacterCanvas.Children.Add(cellBackground);

            // Render the character bitmap
            this.RenderCharacter(charIndex, cellX + 4, cellY + 4, foregroundBrush);

            // Add label below the character
            this.AddCharacterLabel(charIndex, cellX, cellY + (CharHeight * Scale) + 6, labelBrush);
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

    private void AddCharacterLabel(int charCode, int cellX, int cellY, IBrush labelBrush)
    {
        // Format: $XX (DDD, 'C') or $XX (DDD) for non-printable
        string charDisplay = GetCharacterDisplay(charCode);
        string labelText = $"${charCode:X2} ({charCode}, {charDisplay})";

        var label = new TextBlock
        {
            Text = labelText,
            FontSize = 8,
            Foreground = labelBrush,
            FontFamily = new FontFamily("Consolas, Courier New, monospace"),
        };

        Canvas.SetLeft(label, cellX);
        Canvas.SetTop(label, cellY);
        this.CharacterCanvas.Children.Add(label);
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