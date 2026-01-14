// <copyright file="CharacterGlyphTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.UI.Tests;

using BadMango.Emulator.Debug.UI.Editor.Models;

/// <summary>
/// Tests for the <see cref="CharacterGlyph"/> class.
/// </summary>
[TestFixture]
public class CharacterGlyphTests
{
    /// <summary>
    /// Verifies that the glyph dimensions are correct.
    /// </summary>
    [Test]
    public void GlyphDimensionsAreCorrect()
    {
        Assert.Multiple(() =>
        {
            Assert.That(CharacterGlyph.Width, Is.EqualTo(7));
            Assert.That(CharacterGlyph.Height, Is.EqualTo(8));
        });
    }

    /// <summary>
    /// Verifies that a new glyph has all pixels clear.
    /// </summary>
    [Test]
    public void NewGlyphHasAllPixelsClear()
    {
        var glyph = new CharacterGlyph();

        for (int y = 0; y < CharacterGlyph.Height; y++)
        {
            Assert.That(glyph.Scanlines[y], Is.EqualTo(0), $"Scanline {y} should be 0");
        }
    }

    /// <summary>
    /// Verifies that setting and getting a pixel works correctly.
    /// </summary>
    [Test]
    public void SetAndGetPixelWorks()
    {
        var glyph = new CharacterGlyph();

        // Set pixel at (3, 4)
        glyph[3, 4] = true;
        Assert.That(glyph[3, 4], Is.True);

        // Clear it
        glyph[3, 4] = false;
        Assert.That(glyph[3, 4], Is.False);
    }

    /// <summary>
    /// Verifies that pixel indexing uses correct bit ordering.
    /// </summary>
    [Test]
    public void PixelIndexingUsesCorrectBitOrdering()
    {
        var glyph = new CharacterGlyph();

        // Set leftmost pixel (x=0) - should be bit 6
        glyph[0, 0] = true;
        Assert.That(glyph.Scanlines[0], Is.EqualTo(0x40));

        glyph.Clear();

        // Set rightmost pixel (x=6) - should be bit 0
        glyph[6, 0] = true;
        Assert.That(glyph.Scanlines[0], Is.EqualTo(0x01));
    }

    /// <summary>
    /// Verifies that Clear method clears all pixels.
    /// </summary>
    [Test]
    public void ClearMethodClearsAllPixels()
    {
        var glyph = new CharacterGlyph();

        // Set some pixels
        glyph[0, 0] = true;
        glyph[3, 4] = true;
        glyph[6, 7] = true;

        glyph.Clear();

        for (int y = 0; y < CharacterGlyph.Height; y++)
        {
            Assert.That(glyph.Scanlines[y], Is.EqualTo(0), $"Scanline {y} should be 0 after clear");
        }
    }

    /// <summary>
    /// Verifies that Fill method sets all pixels.
    /// </summary>
    [Test]
    public void FillMethodSetsAllPixels()
    {
        var glyph = new CharacterGlyph();
        glyph.Fill();

        for (int y = 0; y < CharacterGlyph.Height; y++)
        {
            Assert.That(glyph.Scanlines[y], Is.EqualTo(0x7F), $"Scanline {y} should be 0x7F after fill");
        }
    }

    /// <summary>
    /// Verifies that Invert method inverts all pixels.
    /// </summary>
    [Test]
    public void InvertMethodInvertsAllPixels()
    {
        var glyph = new CharacterGlyph();
        glyph.Scanlines[0] = 0x55; // 01010101

        glyph.Invert();

        Assert.That(glyph.Scanlines[0], Is.EqualTo(0x2A)); // 00101010 (7 bits inverted)
    }

    /// <summary>
    /// Verifies that Clone method creates a deep copy.
    /// </summary>
    [Test]
    public void CloneCreatesDeepCopy()
    {
        var original = new CharacterGlyph();
        original[3, 4] = true;

        var clone = original.Clone();

        Assert.That(clone[3, 4], Is.True);

        // Modify original, clone should not change
        original[3, 4] = false;
        Assert.That(clone[3, 4], Is.True, "Clone should not be affected by changes to original");
    }

    /// <summary>
    /// Verifies that CopyFrom method copies data correctly.
    /// </summary>
    [Test]
    public void CopyFromCopiesDataCorrectly()
    {
        var glyph = new CharacterGlyph();
        byte[] source = [0x7F, 0x41, 0x41, 0x41, 0x7F, 0x41, 0x41, 0x41];

        glyph.CopyFrom(source);

        for (int i = 0; i < 8; i++)
        {
            Assert.That(glyph.Scanlines[i], Is.EqualTo(source[i]));
        }
    }

    /// <summary>
    /// Verifies that pixel access with out-of-range coordinates throws.
    /// </summary>
    [Test]
    public void OutOfRangeCoordinatesThrow()
    {
        var glyph = new CharacterGlyph();

        Assert.Throws<ArgumentOutOfRangeException>(() => _ = glyph[-1, 0]);
        Assert.Throws<ArgumentOutOfRangeException>(() => _ = glyph[7, 0]);
        Assert.Throws<ArgumentOutOfRangeException>(() => _ = glyph[0, -1]);
        Assert.Throws<ArgumentOutOfRangeException>(() => _ = glyph[0, 8]);
    }
}