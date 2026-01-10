// <copyright file="ScaledPixelWriterTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Rendering.Tests;

/// <summary>
/// Unit tests for the <see cref="ScaledPixelWriter"/> class.
/// Tests focus on boundary conditions, buffer overruns, and scaling behavior.
/// </summary>
[TestFixture]
public class ScaledPixelWriterTests
{
    private const int BufferWidth = 100;
    private const int BufferHeight = 100;
    private const int BufferSize = BufferWidth * BufferHeight;

    /// <summary>
    /// Verifies that WriteScaledPixel with scale 1 sets a single pixel.
    /// </summary>
    [Test]
    public void WriteScaledPixel_Scale1_SetsSinglePixel()
    {
        var pixels = new uint[BufferSize];
        const uint testColor = DisplayColors.GreenPhosphor;

        ScaledPixelWriter.WriteScaledPixel(pixels, BufferWidth, 50, 50, testColor, 1);

        Assert.That(pixels[(50 * BufferWidth) + 50], Is.EqualTo(testColor));
    }

    /// <summary>
    /// Verifies that WriteScaledPixel with scale 2 sets a 2x2 block.
    /// </summary>
    [Test]
    public void WriteScaledPixel_Scale2_Sets2x2Block()
    {
        var pixels = new uint[BufferSize];
        const uint testColor = DisplayColors.AmberPhosphor;

        ScaledPixelWriter.WriteScaledPixel(pixels, BufferWidth, 10, 10, testColor, 2);

        Assert.That(pixels[(10 * BufferWidth) + 10], Is.EqualTo(testColor));
        Assert.That(pixels[(10 * BufferWidth) + 11], Is.EqualTo(testColor));
        Assert.That(pixels[(11 * BufferWidth) + 10], Is.EqualTo(testColor));
        Assert.That(pixels[(11 * BufferWidth) + 11], Is.EqualTo(testColor));
    }

    /// <summary>
    /// Verifies that WriteScaledPixel with scale 3 sets a 3x3 block.
    /// </summary>
    [Test]
    public void WriteScaledPixel_Scale3_Sets3x3Block()
    {
        var pixels = new uint[BufferSize];
        const uint testColor = DisplayColors.WhitePhosphor;

        ScaledPixelWriter.WriteScaledPixel(pixels, BufferWidth, 20, 20, testColor, 3);

        for (int y = 0; y < 3; y++)
        {
            for (int x = 0; x < 3; x++)
            {
                int index = ((20 + y) * BufferWidth) + 20 + x;
                Assert.That(pixels[index], Is.EqualTo(testColor), $"Pixel at ({20 + x}, {20 + y}) should be set");
            }
        }
    }

    /// <summary>
    /// Verifies that WriteScaledPixelSafe returns false when completely outside buffer.
    /// </summary>
    [Test]
    public void WriteScaledPixelSafe_OutsideBuffer_ReturnsFalse()
    {
        var pixels = new uint[BufferSize];

        bool result = ScaledPixelWriter.WriteScaledPixelSafe(
            pixels,
            BufferWidth,
            BufferHeight,
            -10,
            -10,
            DisplayColors.GreenPhosphor,
            2);

        Assert.That(result, Is.False);
    }

    /// <summary>
    /// Verifies that WriteScaledPixelSafe returns true and clips when partially outside buffer.
    /// </summary>
    [Test]
    public void WriteScaledPixelSafe_PartiallyOutside_ClipsAndReturnsTrue()
    {
        var pixels = new uint[BufferSize];
        Array.Fill(pixels, DisplayColors.Black);

        bool result = ScaledPixelWriter.WriteScaledPixelSafe(
            pixels,
            BufferWidth,
            BufferHeight,
            BufferWidth - 1,
            50,
            DisplayColors.GreenPhosphor,
            2);

        Assert.That(result, Is.True);
        Assert.That(pixels[(50 * BufferWidth) + 99], Is.EqualTo(DisplayColors.GreenPhosphor));
        Assert.That(pixels[(51 * BufferWidth) + 99], Is.EqualTo(DisplayColors.GreenPhosphor));
    }

    /// <summary>
    /// Verifies that WriteScaledPixelSafe handles negative coordinates correctly.
    /// </summary>
    [Test]
    public void WriteScaledPixelSafe_NegativeCoordinates_ClipsCorrectly()
    {
        var pixels = new uint[BufferSize];
        Array.Fill(pixels, DisplayColors.Black);

        bool result = ScaledPixelWriter.WriteScaledPixelSafe(
            pixels,
            BufferWidth,
            BufferHeight,
            -1,
            -1,
            DisplayColors.GreenPhosphor,
            3);

        Assert.That(result, Is.True);
        Assert.That(pixels[0], Is.EqualTo(DisplayColors.GreenPhosphor));
        Assert.That(pixels[1], Is.EqualTo(DisplayColors.GreenPhosphor));
        Assert.That(pixels[BufferWidth], Is.EqualTo(DisplayColors.GreenPhosphor));
        Assert.That(pixels[BufferWidth + 1], Is.EqualTo(DisplayColors.GreenPhosphor));
    }

    /// <summary>
    /// Verifies that WriteScaledPixelSafe handles bottom-right corner correctly.
    /// </summary>
    [Test]
    public void WriteScaledPixelSafe_BottomRightCorner_ClipsCorrectly()
    {
        var pixels = new uint[BufferSize];
        Array.Fill(pixels, DisplayColors.Black);

        bool result = ScaledPixelWriter.WriteScaledPixelSafe(
            pixels,
            BufferWidth,
            BufferHeight,
            BufferWidth - 1,
            BufferHeight - 1,
            DisplayColors.AmberPhosphor,
            5);

        Assert.That(result, Is.True);
        Assert.That(pixels[((BufferHeight - 1) * BufferWidth) + (BufferWidth - 1)], Is.EqualTo(DisplayColors.AmberPhosphor));
    }

    /// <summary>
    /// Verifies that FillRectangle fills the correct region.
    /// </summary>
    [Test]
    public void FillRectangle_ValidRegion_FillsCorrectly()
    {
        var pixels = new uint[BufferSize];
        Array.Fill(pixels, DisplayColors.Black);
        const uint testColor = DisplayColors.GreenPhosphor;

        ScaledPixelWriter.FillRectangle(pixels, BufferWidth, 10, 10, 5, 5, testColor);

        for (int y = 10; y < 15; y++)
        {
            for (int x = 10; x < 15; x++)
            {
                Assert.That(pixels[(y * BufferWidth) + x], Is.EqualTo(testColor), $"Pixel at ({x}, {y}) should be filled");
            }
        }

        Assert.That(pixels[(9 * BufferWidth) + 9], Is.EqualTo(DisplayColors.Black));
    }

    /// <summary>
    /// Verifies that FillRectangle with zero width or height does nothing.
    /// </summary>
    [Test]
    public void FillRectangle_ZeroSize_DoesNothing()
    {
        var pixels = new uint[BufferSize];
        Array.Fill(pixels, DisplayColors.Black);

        ScaledPixelWriter.FillRectangle(pixels, BufferWidth, 50, 50, 0, 10, DisplayColors.GreenPhosphor);
        ScaledPixelWriter.FillRectangle(pixels, BufferWidth, 50, 50, 10, 0, DisplayColors.GreenPhosphor);

        Assert.That(pixels, Is.All.EqualTo(DisplayColors.Black));
    }

    /// <summary>
    /// Verifies that FillRectangleSafe clips correctly at edges.
    /// </summary>
    [Test]
    public void FillRectangleSafe_PartiallyOutside_ClipsCorrectly()
    {
        var pixels = new uint[BufferSize];
        Array.Fill(pixels, DisplayColors.Black);
        const uint testColor = DisplayColors.AmberPhosphor;

        ScaledPixelWriter.FillRectangleSafe(
            pixels,
            BufferWidth,
            BufferHeight,
            95,
            95,
            10,
            10,
            testColor);

        for (int y = 95; y < BufferHeight; y++)
        {
            for (int x = 95; x < BufferWidth; x++)
            {
                Assert.That(pixels[(y * BufferWidth) + x], Is.EqualTo(testColor), $"Pixel at ({x}, {y}) should be filled");
            }
        }

        Assert.That(pixels[(94 * BufferWidth) + 95], Is.EqualTo(DisplayColors.Black));
    }

    /// <summary>
    /// Verifies that FillRectangleSafe handles fully outside bounds.
    /// </summary>
    [Test]
    public void FillRectangleSafe_FullyOutside_DoesNothing()
    {
        var pixels = new uint[BufferSize];
        Array.Fill(pixels, DisplayColors.Black);

        ScaledPixelWriter.FillRectangleSafe(
            pixels,
            BufferWidth,
            BufferHeight,
            BufferWidth + 10,
            BufferHeight + 10,
            5,
            5,
            DisplayColors.GreenPhosphor);

        Assert.That(pixels, Is.All.EqualTo(DisplayColors.Black));
    }

    /// <summary>
    /// Verifies that FillRectangleSafe handles negative coordinates.
    /// </summary>
    [Test]
    public void FillRectangleSafe_NegativeCoordinates_ClipsCorrectly()
    {
        var pixels = new uint[BufferSize];
        Array.Fill(pixels, DisplayColors.Black);
        const uint testColor = DisplayColors.WhitePhosphor;

        ScaledPixelWriter.FillRectangleSafe(
            pixels,
            BufferWidth,
            BufferHeight,
            -5,
            -5,
            10,
            10,
            testColor);

        for (int y = 0; y < 5; y++)
        {
            for (int x = 0; x < 5; x++)
            {
                Assert.That(pixels[(y * BufferWidth) + x], Is.EqualTo(testColor), $"Pixel at ({x}, {y}) should be filled");
            }
        }

        Assert.That(pixels[5], Is.EqualTo(DisplayColors.Black));
    }

    /// <summary>
    /// Verifies WriteScaledPixel at buffer origin.
    /// </summary>
    [Test]
    public void WriteScaledPixel_AtOrigin_SetsCorrectly()
    {
        var pixels = new uint[BufferSize];
        const uint testColor = DisplayColors.GreenPhosphor;

        ScaledPixelWriter.WriteScaledPixel(pixels, BufferWidth, 0, 0, testColor, 1);

        Assert.That(pixels[0], Is.EqualTo(testColor));
    }

    /// <summary>
    /// Verifies that scale factor 0 writes nothing.
    /// </summary>
    [Test]
    public void WriteScaledPixel_Scale0_WritesNothing()
    {
        var pixels = new uint[BufferSize];
        Array.Fill(pixels, DisplayColors.Black);

        ScaledPixelWriter.WriteScaledPixel(pixels, BufferWidth, 50, 50, DisplayColors.GreenPhosphor, 0);

        Assert.That(pixels, Is.All.EqualTo(DisplayColors.Black));
    }
}