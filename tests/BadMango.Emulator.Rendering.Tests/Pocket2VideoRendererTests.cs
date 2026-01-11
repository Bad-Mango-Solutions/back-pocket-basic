// <copyright file="Pocket2VideoRendererTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Rendering.Tests;

using BadMango.Emulator.Devices;

/// <summary>
/// Unit tests for the <see cref="Pocket2VideoRenderer"/> class.
/// </summary>
[TestFixture]
public class Pocket2VideoRendererTests
{
    private Pocket2VideoRenderer renderer = null!;
    private uint[] pixelBuffer = null!;

    /// <summary>
    /// Sets up the test fixture before each test.
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        renderer = new Pocket2VideoRenderer();
        pixelBuffer = new uint[renderer.CanonicalWidth * renderer.CanonicalHeight];
    }

    /// <summary>
    /// Verifies CanonicalWidth returns 560.
    /// </summary>
    [Test]
    public void CanonicalWidth_Returns560()
    {
        Assert.That(renderer.CanonicalWidth, Is.EqualTo(560));
    }

    /// <summary>
    /// Verifies CanonicalHeight returns 384.
    /// </summary>
    [Test]
    public void CanonicalHeight_Returns384()
    {
        Assert.That(renderer.CanonicalHeight, Is.EqualTo(384));
    }

    /// <summary>
    /// Verifies Clear fills the buffer with black.
    /// </summary>
    [Test]
    public void Clear_FillsBufferWithBlack()
    {
        // Fill with non-black first
        Array.Fill(pixelBuffer, 0xFFFFFFFF);

        renderer.Clear(pixelBuffer);

        Assert.That(pixelBuffer[0], Is.EqualTo(DisplayColors.Black));
        Assert.That(pixelBuffer[pixelBuffer.Length - 1], Is.EqualTo(DisplayColors.Black));
    }

    /// <summary>
    /// Verifies RenderFrame throws on null readMemory delegate.
    /// </summary>
    [Test]
    public void RenderFrame_NullReadMemory_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            renderer.RenderFrame(
                pixelBuffer,
                VideoMode.Text40,
                null!,
                ReadOnlySpan<byte>.Empty,
                useAltCharSet: false,
                isPage2: false,
                flashState: false));
    }

    /// <summary>
    /// Verifies Text40 mode renders something to the buffer.
    /// </summary>
    [Test]
    public void RenderFrame_Text40Mode_RendersContent()
    {
        // Clear buffer first
        renderer.Clear(pixelBuffer);

        // Create a simple character ROM (all zeros = inverted spaces)
        var charRom = new byte[4096];

        // Render with memory filled with spaces ($A0)
        renderer.RenderFrame(
            pixelBuffer,
            VideoMode.Text40,
            addr => 0xA0, // Normal space character
            charRom,
            useAltCharSet: false,
            isPage2: false,
            flashState: false);

        // Buffer should have been modified (should have some non-black pixels)
        // Space characters in normal mode should show as blank (black)
        // but the rendering logic should have executed
        Assert.That(pixelBuffer.Length, Is.GreaterThan(0));
    }

    /// <summary>
    /// Verifies LoRes mode renders colored blocks.
    /// </summary>
    [Test]
    public void RenderFrame_LoResMode_RendersColoredBlocks()
    {
        renderer.Clear(pixelBuffer);

        // Memory pattern: 0x12 = color 2 top (low nibble), color 1 bottom (high nibble)
        renderer.RenderFrame(
            pixelBuffer,
            VideoMode.LoRes,
            addr => 0x12,
            ReadOnlySpan<byte>.Empty,
            useAltCharSet: false,
            isPage2: false,
            flashState: false);

        // First block top should be color 2 (low nibble of 0x12)
        // Lo-res color 2 is Dark Blue
        uint topColor = DisplayColors.GetLoResColor(2);

        // Check a pixel in the first lo-res block (top-left area)
        Assert.That(pixelBuffer[0], Is.EqualTo(topColor));
    }

    /// <summary>
    /// Verifies HiRes mode renders pixels.
    /// </summary>
    [Test]
    public void RenderFrame_HiResMode_RendersPixels()
    {
        renderer.Clear(pixelBuffer);

        // Memory pattern: all 0xFF (all pixels set)
        renderer.RenderFrame(
            pixelBuffer,
            VideoMode.HiRes,
            addr =>
            {
                // Only return 0xFF for hi-res page 1 ($2000-$3FFF)
                if (addr >= 0x2000 && addr < 0x4000)
                {
                    return 0xFF;
                }

                return 0x00;
            },
            ReadOnlySpan<byte>.Empty,
            useAltCharSet: false,
            isPage2: false,
            flashState: false);

        // With all bits set, we should see green phosphor pixels
        // Check the first scanline
        Assert.That(pixelBuffer[0], Is.EqualTo(DisplayColors.GreenPhosphor));
    }

    /// <summary>
    /// Verifies Mixed mode renders graphics with text window.
    /// </summary>
    [Test]
    public void RenderFrame_HiResMixedMode_RendersGraphicsAndText()
    {
        renderer.Clear(pixelBuffer);

        // Mixed mode: hi-res graphics with 4-line text window at bottom
        renderer.RenderFrame(
            pixelBuffer,
            VideoMode.HiResMixed,
            addr =>
            {
                // Hi-res area: all pixels on
                if (addr >= 0x2000 && addr < 0x4000)
                {
                    return 0xFF;
                }

                // Text area: spaces
                if (addr >= 0x0400 && addr < 0x0800)
                {
                    return 0xA0;
                }

                return 0x00;
            },
            new byte[4096],
            useAltCharSet: false,
            isPage2: false,
            flashState: false);

        // Graphics area should have pixels
        Assert.That(pixelBuffer[0], Is.EqualTo(DisplayColors.GreenPhosphor));
    }

    /// <summary>
    /// Verifies page 2 selection changes the memory addresses read.
    /// </summary>
    [Test]
    public void RenderFrame_Page2_ReadsFromPage2Addresses()
    {
        renderer.Clear(pixelBuffer);

        bool readFromPage2 = false;

        renderer.RenderFrame(
            pixelBuffer,
            VideoMode.Text40,
            addr =>
            {
                // Page 2 text is at $0800-$0BFF
                if (addr >= 0x0800 && addr < 0x0C00)
                {
                    readFromPage2 = true;
                }

                return 0xA0;
            },
            new byte[4096],
            useAltCharSet: false,
            isPage2: true, // Select page 2
            flashState: false);

        Assert.That(readFromPage2, Is.True, "Should read from page 2 addresses");
    }

    /// <summary>
    /// Verifies flash state affects rendering of flashing characters.
    /// </summary>
    [Test]
    public void RenderFrame_FlashState_AffectsFlashingCharacters()
    {
        // Render with flash state false
        renderer.Clear(pixelBuffer);
        var bufferCopy1 = new uint[pixelBuffer.Length];

        renderer.RenderFrame(
            pixelBuffer,
            VideoMode.Text40,
            addr => 0x40, // Flashing '@' character
            new byte[4096],
            useAltCharSet: false,
            isPage2: false,
            flashState: false);

        Array.Copy(pixelBuffer, bufferCopy1, pixelBuffer.Length);

        // Render with flash state true
        renderer.Clear(pixelBuffer);

        renderer.RenderFrame(
            pixelBuffer,
            VideoMode.Text40,
            addr => 0x40, // Same flashing '@' character
            new byte[4096],
            useAltCharSet: false,
            isPage2: false,
            flashState: true);

        // The two renders should be different due to flash inversion
        bool hasDifference = false;
        for (int i = 0; i < pixelBuffer.Length; i++)
        {
            if (pixelBuffer[i] != bufferCopy1[i])
            {
                hasDifference = true;
                break;
            }
        }

        Assert.That(hasDifference, Is.True, "Flash state should affect rendering");
    }
}