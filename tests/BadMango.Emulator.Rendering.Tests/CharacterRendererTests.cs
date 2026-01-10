// <copyright file="CharacterRendererTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Rendering.Tests;

/// <summary>
/// Unit tests for the <see cref="CharacterRenderer"/> class.
/// Tests focus on ROM bounds checking, character rendering, and edge cases.
/// </summary>
[TestFixture]
public class CharacterRendererTests
{
    private const int BufferWidth = 280;
    private const int BufferHeight = 192;
    private const int BufferSize = BufferWidth * BufferHeight;

    private readonly byte[] testRomData = new byte[2048];

    /// <summary>
    /// Sets up test fixtures before each test.
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        for (int i = 0; i < testRomData.Length; i++)
        {
            testRomData[i] = (byte)(i % 256);
        }

        testRomData[0] = 0b01000001;
        testRomData[1] = 0b00100010;
        testRomData[2] = 0b00010100;
        testRomData[3] = 0b00001000;
        testRomData[4] = 0b00010100;
        testRomData[5] = 0b00100010;
        testRomData[6] = 0b01000001;
        testRomData[7] = 0b00000000;
    }

    /// <summary>
    /// Verifies that CharacterWidth constant is 7.
    /// </summary>
    [Test]
    public void CharacterWidth_Is7()
    {
        Assert.That(CharacterRenderer.CharacterWidth, Is.EqualTo(7));
    }

    /// <summary>
    /// Verifies that CharacterHeight constant is 8.
    /// </summary>
    [Test]
    public void CharacterHeight_Is8()
    {
        Assert.That(CharacterRenderer.CharacterHeight, Is.EqualTo(8));
    }

    /// <summary>
    /// Verifies that BytesPerCharacter constant is 8.
    /// </summary>
    [Test]
    public void BytesPerCharacter_Is8()
    {
        Assert.That(CharacterRenderer.BytesPerCharacter, Is.EqualTo(8));
    }

    /// <summary>
    /// Verifies that RenderCharacter returns true for valid parameters.
    /// </summary>
    [Test]
    public void RenderCharacter_ValidParameters_ReturnsTrue()
    {
        var pixels = new uint[BufferSize];

        bool result = CharacterRenderer.RenderCharacter(
            pixels,
            BufferWidth,
            testRomData,
            charCode: 0,
            romOffset: 0,
            destX: 0,
            destY: 0,
            foregroundColor: DisplayColors.GreenPhosphor);

        Assert.That(result, Is.True);
    }

    /// <summary>
    /// Verifies that RenderCharacter returns false for invalid ROM offset.
    /// </summary>
    [Test]
    public void RenderCharacter_InvalidRomOffset_ReturnsFalse()
    {
        var pixels = new uint[BufferSize];

        bool result = CharacterRenderer.RenderCharacter(
            pixels,
            BufferWidth,
            testRomData,
            charCode: 255,
            romOffset: testRomData.Length,
            destX: 0,
            destY: 0,
            foregroundColor: DisplayColors.GreenPhosphor);

        Assert.That(result, Is.False);
    }

    /// <summary>
    /// Verifies that RenderCharacter returns false for negative ROM offset.
    /// </summary>
    [Test]
    public void RenderCharacter_NegativeRomOffset_ReturnsFalse()
    {
        var pixels = new uint[BufferSize];

        bool result = CharacterRenderer.RenderCharacter(
            pixels,
            BufferWidth,
            testRomData,
            charCode: 0,
            romOffset: -8,
            destX: 0,
            destY: 0,
            foregroundColor: DisplayColors.GreenPhosphor);

        Assert.That(result, Is.False);
    }

    /// <summary>
    /// Verifies that RenderCharacter returns false when character exceeds ROM bounds.
    /// </summary>
    [Test]
    public void RenderCharacter_CharacterExceedsRomBounds_ReturnsFalse()
    {
        var smallRom = new byte[16];

        bool result = CharacterRenderer.RenderCharacter(
            new uint[BufferSize],
            BufferWidth,
            smallRom,
            charCode: 5,
            romOffset: 0,
            destX: 0,
            destY: 0,
            foregroundColor: DisplayColors.GreenPhosphor);

        Assert.That(result, Is.False);
    }

    /// <summary>
    /// Verifies that RenderCharacter renders correct pixels for test pattern.
    /// </summary>
    [Test]
    public void RenderCharacter_WithTestPattern_RendersCorrectPixels()
    {
        var pixels = new uint[BufferSize];
        Array.Fill(pixels, DisplayColors.Black);

        CharacterRenderer.RenderCharacter(
            pixels,
            BufferWidth,
            testRomData,
            charCode: 0,
            romOffset: 0,
            destX: 0,
            destY: 0,
            foregroundColor: DisplayColors.GreenPhosphor);

        Assert.That(pixels[0], Is.EqualTo(DisplayColors.GreenPhosphor), "Pixel 0 should be set");
        Assert.That(pixels[6], Is.EqualTo(DisplayColors.GreenPhosphor), "Pixel 6 should be set");
        Assert.That(pixels[1], Is.EqualTo(DisplayColors.Black), "Pixel 1 should be unset");
    }

    /// <summary>
    /// Verifies that RenderCharacterScaled with scale 2 doubles the output size.
    /// </summary>
    [Test]
    public void RenderCharacterScaled_Scale2_DoublesSize()
    {
        var pixels = new uint[BufferSize];
        Array.Fill(pixels, DisplayColors.Black);

        var simpleRom = new byte[8];
        simpleRom[0] = 0b01000000;

        CharacterRenderer.RenderCharacterScaled(
            pixels,
            BufferWidth,
            simpleRom,
            charCode: 0,
            romOffset: 0,
            destX: 0,
            destY: 0,
            foregroundColor: DisplayColors.GreenPhosphor,
            scale: 2);

        Assert.That(pixels[0], Is.EqualTo(DisplayColors.GreenPhosphor), "(0,0)");
        Assert.That(pixels[1], Is.EqualTo(DisplayColors.GreenPhosphor), "(1,0)");
        Assert.That(pixels[BufferWidth], Is.EqualTo(DisplayColors.GreenPhosphor), "(0,1)");
        Assert.That(pixels[BufferWidth + 1], Is.EqualTo(DisplayColors.GreenPhosphor), "(1,1)");
    }

    /// <summary>
    /// Verifies that RenderCharacter works at non-zero destination.
    /// </summary>
    [Test]
    public void RenderCharacter_NonZeroDestination_RendersAtCorrectPosition()
    {
        var pixels = new uint[BufferSize];
        Array.Fill(pixels, DisplayColors.Black);

        var simpleRom = new byte[8];
        simpleRom[0] = 0b01000000;

        CharacterRenderer.RenderCharacter(
            pixels,
            BufferWidth,
            simpleRom,
            charCode: 0,
            romOffset: 0,
            destX: 100,
            destY: 50,
            foregroundColor: DisplayColors.GreenPhosphor);

        Assert.That(pixels[(50 * BufferWidth) + 100], Is.EqualTo(DisplayColors.GreenPhosphor));
    }

    /// <summary>
    /// Verifies that RenderScanline correctly interprets bit patterns.
    /// </summary>
    [Test]
    public void RenderScanline_AllBitsSet_RendersAllPixels()
    {
        var pixels = new uint[BufferSize];
        Array.Fill(pixels, DisplayColors.Black);

        CharacterRenderer.RenderScanline(
            pixels,
            BufferWidth,
            scanlineData: 0x7F,
            destX: 0,
            destY: 0,
            foregroundColor: DisplayColors.GreenPhosphor,
            scale: 1);

        for (int i = 0; i < 7; i++)
        {
            Assert.That(pixels[i], Is.EqualTo(DisplayColors.GreenPhosphor), $"Pixel {i} should be set");
        }
    }

    /// <summary>
    /// Verifies that RenderScanline ignores bit 7 (high bit).
    /// </summary>
    [Test]
    public void RenderScanline_HighBitSet_IgnoresHighBit()
    {
        var pixels = new uint[BufferSize];
        Array.Fill(pixels, DisplayColors.Black);

        CharacterRenderer.RenderScanline(
            pixels,
            BufferWidth,
            scanlineData: 0x80,
            destX: 0,
            destY: 0,
            foregroundColor: DisplayColors.GreenPhosphor,
            scale: 1);

        for (int i = 0; i < 7; i++)
        {
            Assert.That(pixels[i], Is.EqualTo(DisplayColors.Black), $"Pixel {i} should not be set");
        }
    }

    /// <summary>
    /// Verifies that GetCharacterDisplayString returns correct string for inverse character.
    /// </summary>
    [Test]
    public void GetCharacterDisplayString_InverseAt_ReturnsCorrectString()
    {
        string result = CharacterRenderer.GetCharacterDisplayString(0x00);

        Assert.That(result, Does.Contain("@"));
        Assert.That(result, Does.Contain("Inverse"));
    }

    /// <summary>
    /// Verifies that GetCharacterDisplayString returns correct string for normal 'A'.
    /// </summary>
    [Test]
    public void GetCharacterDisplayString_NormalA_ReturnsCorrectString()
    {
        string result = CharacterRenderer.GetCharacterDisplayString(0xC1);

        Assert.That(result, Does.Contain("A"));
    }

    /// <summary>
    /// Verifies that GetCharacterDisplayString handles flashing characters.
    /// </summary>
    [Test]
    public void GetCharacterDisplayString_FlashingRange_ContainsFlashing()
    {
        string result = CharacterRenderer.GetCharacterDisplayString(0x41);

        Assert.That(result, Does.Contain("Flashing"));
    }

    /// <summary>
    /// Verifies that RenderCharacter handles empty ROM gracefully.
    /// </summary>
    [Test]
    public void RenderCharacter_EmptyRom_ReturnsFalse()
    {
        var emptyRom = Array.Empty<byte>();
        var pixels = new uint[BufferSize];

        bool result = CharacterRenderer.RenderCharacter(
            pixels,
            BufferWidth,
            emptyRom,
            charCode: 0,
            romOffset: 0,
            destX: 0,
            destY: 0,
            foregroundColor: DisplayColors.GreenPhosphor);

        Assert.That(result, Is.False);
    }

    /// <summary>
    /// Verifies that multiple characters can be rendered to the same buffer.
    /// </summary>
    [Test]
    public void RenderCharacter_MultipleCharacters_AllRendered()
    {
        var pixels = new uint[BufferSize];
        Array.Fill(pixels, DisplayColors.Black);

        for (int i = 0; i < 40; i++)
        {
            bool result = CharacterRenderer.RenderCharacter(
                pixels,
                BufferWidth,
                testRomData,
                charCode: i,
                romOffset: 0,
                destX: i * 7,
                destY: 0,
                foregroundColor: DisplayColors.GreenPhosphor);

            Assert.That(result, Is.True, $"Character {i} should render successfully");
        }
    }

    /// <summary>
    /// Verifies boundary condition for ROM offset at exact end of valid range.
    /// </summary>
    [Test]
    public void RenderCharacter_RomOffsetAtBoundary_ReturnsCorrectly()
    {
        var exactRom = new byte[8];
        Array.Fill(exactRom, (byte)0xFF);

        var pixels = new uint[BufferSize];

        bool result1 = CharacterRenderer.RenderCharacter(
            pixels,
            BufferWidth,
            exactRom,
            charCode: 0,
            romOffset: 0,
            destX: 0,
            destY: 0,
            foregroundColor: DisplayColors.GreenPhosphor);

        bool result2 = CharacterRenderer.RenderCharacter(
            pixels,
            BufferWidth,
            exactRom,
            charCode: 1,
            romOffset: 0,
            destX: 0,
            destY: 0,
            foregroundColor: DisplayColors.GreenPhosphor);

        Assert.That(result1, Is.True, "Character 0 should render");
        Assert.That(result2, Is.False, "Character 1 should fail - out of bounds");
    }
}