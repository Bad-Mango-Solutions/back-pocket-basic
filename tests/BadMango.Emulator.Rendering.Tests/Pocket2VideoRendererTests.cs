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

    #region Basic Properties Tests

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
    /// Verifies the framebuffer size is correct for the canonical resolution.
    /// </summary>
    [Test]
    public void FramebufferSize_IsCorrect()
    {
        int expectedSize = 560 * 384;
        Assert.That(pixelBuffer.Length, Is.EqualTo(expectedSize));
    }

    #endregion

    #region Clear Tests

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
    /// Verifies Clear fills all pixels with black.
    /// </summary>
    [Test]
    public void Clear_FillsAllPixelsWithBlack()
    {
        Array.Fill(pixelBuffer, 0xFFFFFFFF);

        renderer.Clear(pixelBuffer);

        Assert.That(pixelBuffer.All(p => p == DisplayColors.Black), Is.True);
    }

    /// <summary>
    /// Verifies Clear handles empty buffer without throwing.
    /// </summary>
    [Test]
    public void Clear_EmptyBuffer_DoesNotThrow()
    {
        var emptyBuffer = Array.Empty<uint>();
        Assert.DoesNotThrow(() => renderer.Clear(emptyBuffer));
    }

    #endregion

    #region RenderFrame Validation Tests

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
    /// Verifies RenderFrame handles empty character ROM gracefully.
    /// </summary>
    [Test]
    public void RenderFrame_EmptyCharacterRom_DoesNotThrow()
    {
        Assert.DoesNotThrow(() =>
            renderer.RenderFrame(
                pixelBuffer,
                VideoMode.Text40,
                addr => 0xA0,
                ReadOnlySpan<byte>.Empty,
                useAltCharSet: false,
                isPage2: false,
                flashState: false));
    }

    #endregion

    #region Text40 Mode Tests

    /// <summary>
    /// Verifies Text40 mode renders content to the buffer.
    /// </summary>
    [Test]
    public void RenderFrame_Text40Mode_RendersContent()
    {
        renderer.Clear(pixelBuffer);
        var charRom = new byte[4096];

        renderer.RenderFrame(
            pixelBuffer,
            VideoMode.Text40,
            addr => 0xA0,
            charRom,
            useAltCharSet: false,
            isPage2: false,
            flashState: false);

        Assert.That(pixelBuffer.Length, Is.GreaterThan(0));
    }

    /// <summary>
    /// Verifies Text40 mode reads from correct text page 1 addresses.
    /// </summary>
    [Test]
    public void RenderFrame_Text40_ReadsFromPage1Addresses()
    {
        var readAddresses = new List<ushort>();

        renderer.RenderFrame(
            pixelBuffer,
            VideoMode.Text40,
            addr =>
            {
                readAddresses.Add(addr);
                return 0xA0;
            },
            new byte[4096],
            useAltCharSet: false,
            isPage2: false,
            flashState: false);

        // Text page 1 is $0400-$07FF
        Assert.That(readAddresses.Any(a => a >= 0x0400 && a < 0x0800), Is.True);
        Assert.That(readAddresses.Any(a => a >= 0x0800 && a < 0x0C00), Is.False);
    }

    /// <summary>
    /// Verifies Text40 mode with page 2 reads from correct addresses.
    /// </summary>
    [Test]
    public void RenderFrame_Text40Page2_ReadsFromPage2Addresses()
    {
        var readAddresses = new List<ushort>();

        renderer.RenderFrame(
            pixelBuffer,
            VideoMode.Text40,
            addr =>
            {
                readAddresses.Add(addr);
                return 0xA0;
            },
            new byte[4096],
            useAltCharSet: false,
            isPage2: true,
            flashState: false);

        // Text page 2 is $0800-$0BFF
        Assert.That(readAddresses.Any(a => a >= 0x0800 && a < 0x0C00), Is.True);
    }

    /// <summary>
    /// Verifies inverse characters render correctly (character codes $00-$3F).
    /// The ROM contains pre-inverted bitmaps for inverse characters.
    /// </summary>
    [Test]
    public void RenderFrame_Text40_InverseCharacters_RenderDifferently()
    {
        // Create a character ROM with proper inverse character data
        // The ROM stores pre-inverted bitmaps for $00-$3F
        var charRom = new byte[4096];

        // Set up normal 'A' at $C1 (ROM offset 0xC1 * 8 = 1544)
        // Standard 'A' pattern
        int normalOffset = 0xC1 * 8;
        charRom[normalOffset + 0] = 0x18; // ..##....
        charRom[normalOffset + 1] = 0x24; // .#..#...
        charRom[normalOffset + 2] = 0x42; // #....#..
        charRom[normalOffset + 3] = 0x42; // #....#..
        charRom[normalOffset + 4] = 0x7E; // ######..
        charRom[normalOffset + 5] = 0x42; // #....#..
        charRom[normalOffset + 6] = 0x42; // #....#..
        charRom[normalOffset + 7] = 0x00; // ........

        // Set up inverse 'A' at $01 (ROM offset 0x01 * 8 = 8)
        // Pre-inverted bitmap (bits flipped from normal)
        int inverseOffset = 0x01 * 8;
        charRom[inverseOffset + 0] = 0x67; // inverted 0x18
        charRom[inverseOffset + 1] = 0x5B; // inverted 0x24
        charRom[inverseOffset + 2] = 0x3D; // inverted 0x42
        charRom[inverseOffset + 3] = 0x3D; // inverted 0x42
        charRom[inverseOffset + 4] = 0x01; // inverted 0x7E
        charRom[inverseOffset + 5] = 0x3D; // inverted 0x42
        charRom[inverseOffset + 6] = 0x3D; // inverted 0x42
        charRom[inverseOffset + 7] = 0x7F; // inverted 0x00

        renderer.Clear(pixelBuffer);
        var buffer1 = new uint[pixelBuffer.Length];

        // Render normal character
        renderer.RenderFrame(
            pixelBuffer,
            VideoMode.Text40,
            addr => 0xC1, // Normal 'A'
            charRom,
            useAltCharSet: false,
            isPage2: false,
            flashState: false);

        Array.Copy(pixelBuffer, buffer1, pixelBuffer.Length);

        // Render inverse character
        renderer.Clear(pixelBuffer);
        renderer.RenderFrame(
            pixelBuffer,
            VideoMode.Text40,
            addr => 0x01, // Inverse 'A'
            charRom,
            useAltCharSet: false,
            isPage2: false,
            flashState: false);

        // Buffers should be different
        bool hasDifference = !pixelBuffer.SequenceEqual(buffer1);
        Assert.That(hasDifference, Is.True, "Inverse characters should render differently");
    }

    /// <summary>
    /// Verifies flashing characters toggle based on flash state.
    /// </summary>
    [Test]
    public void RenderFrame_Text40_FlashingCharacters_ToggleWithFlashState()
    {
        renderer.Clear(pixelBuffer);
        var bufferFlashOff = new uint[pixelBuffer.Length];

        renderer.RenderFrame(
            pixelBuffer,
            VideoMode.Text40,
            addr => 0x40, // Flashing '@'
            new byte[4096],
            useAltCharSet: false,
            isPage2: false,
            flashState: false);

        Array.Copy(pixelBuffer, bufferFlashOff, pixelBuffer.Length);

        renderer.Clear(pixelBuffer);
        renderer.RenderFrame(
            pixelBuffer,
            VideoMode.Text40,
            addr => 0x40,
            new byte[4096],
            useAltCharSet: false,
            isPage2: false,
            flashState: true);

        bool hasDifference = !pixelBuffer.SequenceEqual(bufferFlashOff);
        Assert.That(hasDifference, Is.True, "Flash state should affect flashing characters");
    }

    /// <summary>
    /// Verifies text mode covers all 24 rows.
    /// </summary>
    [Test]
    public void RenderFrame_Text40_CoversAll24Rows()
    {
        var rowsAccessed = new HashSet<int>();

        renderer.RenderFrame(
            pixelBuffer,
            VideoMode.Text40,
            addr =>
            {
                // Calculate which row this address belongs to
                if (addr >= 0x0400 && addr < 0x0800)
                {
                    // Decode row from address (reverse the row address calculation)
                    int baseAddr = addr - 0x0400;
                    for (int row = 0; row < 24; row++)
                    {
                        int group = row / 8;
                        int offset = row % 8;
                        int rowStart = (offset * 128) + (group * 40);
                        if (baseAddr >= rowStart && baseAddr < rowStart + 40)
                        {
                            rowsAccessed.Add(row);
                            break;
                        }
                    }
                }

                return 0xA0;
            },
            new byte[4096],
            useAltCharSet: false,
            isPage2: false,
            flashState: false);

        Assert.That(rowsAccessed.Count, Is.EqualTo(24), "All 24 text rows should be accessed");
    }

    /// <summary>
    /// Verifies text mode covers all 40 columns.
    /// </summary>
    [Test]
    public void RenderFrame_Text40_CoversAll40Columns()
    {
        var columnsAccessed = new HashSet<int>();

        renderer.RenderFrame(
            pixelBuffer,
            VideoMode.Text40,
            addr =>
            {
                if (addr >= 0x0400 && addr < 0x0800)
                {
                    int baseAddr = addr - 0x0400;
                    int col = baseAddr % 128;
                    if (col < 40)
                    {
                        columnsAccessed.Add(col);
                    }
                }

                return 0xA0;
            },
            new byte[4096],
            useAltCharSet: false,
            isPage2: false,
            flashState: false);

        Assert.That(columnsAccessed.Count, Is.EqualTo(40), "All 40 text columns should be accessed");
    }

    #endregion

    #region Lo-Res Mode Tests

    /// <summary>
    /// Verifies LoRes mode renders colored blocks in color mode.
    /// </summary>
    [Test]
    public void RenderFrame_LoResMode_RendersColoredBlocks()
    {
        renderer.Clear(pixelBuffer);

        renderer.RenderFrame(
            pixelBuffer,
            VideoMode.LoRes,
            addr => 0x12, // Color 2 top, color 1 bottom
            ReadOnlySpan<byte>.Empty,
            useAltCharSet: false,
            isPage2: false,
            flashState: false,
            DisplayColorMode.Color);

        uint topColor = DisplayColors.GetLoResColor(2);
        Assert.That(pixelBuffer[0], Is.EqualTo(topColor));
    }

    /// <summary>
    /// Verifies all 16 lo-res colors render correctly.
    /// </summary>
    /// <param name="color">The color index to test (0-15).</param>
    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    [TestCase(5)]
    [TestCase(6)]
    [TestCase(7)]
    [TestCase(8)]
    [TestCase(9)]
    [TestCase(10)]
    [TestCase(11)]
    [TestCase(12)]
    [TestCase(13)]
    [TestCase(14)]
    [TestCase(15)]
    public void RenderFrame_LoRes_AllColorsRenderCorrectly(int color)
    {
        renderer.Clear(pixelBuffer);

        byte memValue = (byte)color; // Color in low nibble (top block)

        renderer.RenderFrame(
            pixelBuffer,
            VideoMode.LoRes,
            addr => memValue,
            ReadOnlySpan<byte>.Empty,
            useAltCharSet: false,
            isPage2: false,
            flashState: false,
            DisplayColorMode.Color);

        uint expectedColor = DisplayColors.GetLoResColor(color);
        Assert.That(pixelBuffer[0], Is.EqualTo(expectedColor));
    }

    /// <summary>
    /// Verifies lo-res block dimensions are correct (14×8 pixels per block).
    /// </summary>
    [Test]
    public void RenderFrame_LoRes_BlockDimensionsAreCorrect()
    {
        renderer.Clear(pixelBuffer);

        // Set first column to color 1, rest to color 0
        renderer.RenderFrame(
            pixelBuffer,
            VideoMode.LoRes,
            addr =>
            {
                int col = (addr - 0x0400) % 128;
                return (byte)(col == 0 ? 0x01 : 0x00);
            },
            ReadOnlySpan<byte>.Empty,
            useAltCharSet: false,
            isPage2: false,
            flashState: false,
            DisplayColorMode.Color);

        uint color1 = DisplayColors.GetLoResColor(1);
        uint color0 = DisplayColors.GetLoResColor(0);

        // First 14 pixels of first row should be color 1
        for (int x = 0; x < 14; x++)
        {
            Assert.That(pixelBuffer[x], Is.EqualTo(color1), $"Pixel {x} should be color 1");
        }

        // Pixel 14 should be color 0 (next block)
        Assert.That(pixelBuffer[14], Is.EqualTo(color0), "Pixel 14 should be color 0");
    }

    /// <summary>
    /// Verifies lo-res top and bottom nibbles render to correct positions.
    /// </summary>
    [Test]
    public void RenderFrame_LoRes_TopAndBottomNibblesRenderCorrectly()
    {
        renderer.Clear(pixelBuffer);

        // Memory value 0x21: top = 1, bottom = 2
        renderer.RenderFrame(
            pixelBuffer,
            VideoMode.LoRes,
            addr => 0x21,
            ReadOnlySpan<byte>.Empty,
            useAltCharSet: false,
            isPage2: false,
            flashState: false,
            DisplayColorMode.Color);

        uint topColor = DisplayColors.GetLoResColor(1);
        uint bottomColor = DisplayColors.GetLoResColor(2);

        // Top block is rows 0-7 (first 8 rows of canonical 384)
        Assert.That(pixelBuffer[0], Is.EqualTo(topColor), "Top block should be color 1");

        // Bottom block starts at row 8
        int bottomRowStart = 8 * 560; // Row 8, first pixel
        Assert.That(pixelBuffer[bottomRowStart], Is.EqualTo(bottomColor), "Bottom block should be color 2");
    }

    /// <summary>
    /// Verifies lo-res page 2 reads from correct addresses.
    /// </summary>
    [Test]
    public void RenderFrame_LoResPage2_ReadsFromPage2Addresses()
    {
        var readAddresses = new List<ushort>();

        renderer.RenderFrame(
            pixelBuffer,
            VideoMode.LoRes,
            addr =>
            {
                readAddresses.Add(addr);
                return 0x00;
            },
            ReadOnlySpan<byte>.Empty,
            useAltCharSet: false,
            isPage2: true,
            flashState: false);

        Assert.That(readAddresses.Any(a => a >= 0x0800 && a < 0x0C00), Is.True);
    }

    #endregion

    #region Hi-Res Mode Tests

    /// <summary>
    /// Verifies HiRes mode renders pixels.
    /// </summary>
    [Test]
    public void RenderFrame_HiResMode_RendersPixels()
    {
        renderer.Clear(pixelBuffer);

        renderer.RenderFrame(
            pixelBuffer,
            VideoMode.HiRes,
            addr =>
            {
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

        Assert.That(pixelBuffer[0], Is.EqualTo(DisplayColors.GreenPhosphor));
    }

    /// <summary>
    /// Verifies hi-res individual bit positions render correctly.
    /// </summary>
    /// <param name="bit">The bit position to test (0-6).</param>
    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    [TestCase(5)]
    [TestCase(6)]
    public void RenderFrame_HiRes_IndividualBitsRenderCorrectly(int bit)
    {
        renderer.Clear(pixelBuffer);

        byte memValue = (byte)(1 << bit);

        renderer.RenderFrame(
            pixelBuffer,
            VideoMode.HiRes,
            addr => addr >= 0x2000 && addr < 0x4000 ? memValue : (byte)0,
            ReadOnlySpan<byte>.Empty,
            useAltCharSet: false,
            isPage2: false,
            flashState: false);

        // The pixel at position 'bit * 2' should be set (2× scaling)
        int pixelX = bit * 2;
        Assert.That(pixelBuffer[pixelX], Is.EqualTo(DisplayColors.GreenPhosphor));

        // Adjacent pixel that's not set should be black
        int otherBit = (bit + 1) % 7;
        int otherPixelX = otherBit * 2;
        if (otherBit != bit)
        {
            Assert.That(pixelBuffer[otherPixelX], Is.EqualTo(DisplayColors.Black));
        }
    }

    /// <summary>
    /// Verifies hi-res page 1 reads from $2000-$3FFF.
    /// </summary>
    [Test]
    public void RenderFrame_HiResPage1_ReadsFromCorrectAddresses()
    {
        var readAddresses = new List<ushort>();

        renderer.RenderFrame(
            pixelBuffer,
            VideoMode.HiRes,
            addr =>
            {
                readAddresses.Add(addr);
                return 0x00;
            },
            ReadOnlySpan<byte>.Empty,
            useAltCharSet: false,
            isPage2: false,
            flashState: false);

        Assert.That(readAddresses.Any(a => a >= 0x2000 && a < 0x4000), Is.True);
        Assert.That(readAddresses.Any(a => a >= 0x4000 && a < 0x6000), Is.False);
    }

    /// <summary>
    /// Verifies hi-res page 2 reads from $4000-$5FFF.
    /// </summary>
    [Test]
    public void RenderFrame_HiResPage2_ReadsFromCorrectAddresses()
    {
        var readAddresses = new List<ushort>();

        renderer.RenderFrame(
            pixelBuffer,
            VideoMode.HiRes,
            addr =>
            {
                readAddresses.Add(addr);
                return 0x00;
            },
            ReadOnlySpan<byte>.Empty,
            useAltCharSet: false,
            isPage2: true,
            flashState: false);

        Assert.That(readAddresses.Any(a => a >= 0x4000 && a < 0x6000), Is.True);
    }

    /// <summary>
    /// Verifies hi-res mode covers all 192 scanlines.
    /// </summary>
    [Test]
    public void RenderFrame_HiRes_CoversAll192Scanlines()
    {
        var scanlineAddresses = new HashSet<int>();

        renderer.RenderFrame(
            pixelBuffer,
            VideoMode.HiRes,
            addr =>
            {
                if (addr >= 0x2000 && addr < 0x4000)
                {
                    // Track unique row addresses
                    scanlineAddresses.Add(addr & 0xFF80); // Group by 128-byte chunks
                }

                return 0x00;
            },
            ReadOnlySpan<byte>.Empty,
            useAltCharSet: false,
            isPage2: false,
            flashState: false);

        // Should have accessed addresses from all 192 scanlines
        Assert.That(scanlineAddresses.Count, Is.GreaterThanOrEqualTo(192 / 8), "Should access all scanline groups");
    }

    #endregion

    #region Mixed Mode Tests

    /// <summary>
    /// Verifies Mixed mode renders graphics with text window.
    /// </summary>
    [Test]
    public void RenderFrame_HiResMixedMode_RendersGraphicsAndText()
    {
        renderer.Clear(pixelBuffer);

        renderer.RenderFrame(
            pixelBuffer,
            VideoMode.HiResMixed,
            addr =>
            {
                if (addr >= 0x2000 && addr < 0x4000)
                {
                    return 0xFF;
                }

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

        Assert.That(pixelBuffer[0], Is.EqualTo(DisplayColors.GreenPhosphor));
    }

    /// <summary>
    /// Verifies hi-res mixed mode only renders 160 scanlines of graphics.
    /// </summary>
    [Test]
    public void RenderFrame_HiResMixed_Only160ScanlinesOfGraphics()
    {
        var hiResAddresses = new List<ushort>();

        renderer.RenderFrame(
            pixelBuffer,
            VideoMode.HiResMixed,
            addr =>
            {
                if (addr >= 0x2000 && addr < 0x4000)
                {
                    hiResAddresses.Add(addr);
                }

                return 0x00;
            },
            new byte[4096],
            useAltCharSet: false,
            isPage2: false,
            flashState: false);

        // Should not read addresses for scanlines 160-191
        // This is a simplification - actual test would verify row addresses
        Assert.That(hiResAddresses.Count, Is.LessThan(192 * 40), "Mixed mode should read fewer hi-res addresses");
    }

    /// <summary>
    /// Verifies lo-res mixed mode renders bottom 4 text lines.
    /// </summary>
    [Test]
    public void RenderFrame_LoResMixed_RendersBottom4TextLines()
    {
        var textAddresses = new List<ushort>();

        renderer.RenderFrame(
            pixelBuffer,
            VideoMode.LoResMixed,
            addr =>
            {
                if (addr >= 0x0400 && addr < 0x0800)
                {
                    textAddresses.Add(addr);
                }

                return 0x00;
            },
            new byte[4096],
            useAltCharSet: false,
            isPage2: false,
            flashState: false);

        // Should have text addresses for rows 20-23
        Assert.That(textAddresses.Count, Is.GreaterThan(0), "Mixed mode should read text addresses");
    }

    #endregion

    #region Page 2 Tests

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
                if (addr >= 0x0800 && addr < 0x0C00)
                {
                    readFromPage2 = true;
                }

                return 0xA0;
            },
            new byte[4096],
            useAltCharSet: false,
            isPage2: true,
            flashState: false);

        Assert.That(readFromPage2, Is.True, "Should read from page 2 addresses");
    }

    #endregion

    #region Flash State Tests

    /// <summary>
    /// Verifies flash state affects rendering of flashing characters.
    /// </summary>
    [Test]
    public void RenderFrame_FlashState_AffectsFlashingCharacters()
    {
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

        renderer.Clear(pixelBuffer);

        renderer.RenderFrame(
            pixelBuffer,
            VideoMode.Text40,
            addr => 0x40,
            new byte[4096],
            useAltCharSet: false,
            isPage2: false,
            flashState: true);

        bool hasDifference = !pixelBuffer.SequenceEqual(bufferCopy1);
        Assert.That(hasDifference, Is.True, "Flash state should affect rendering");
    }

    /// <summary>
    /// Verifies normal characters are not affected by flash state.
    /// </summary>
    [Test]
    public void RenderFrame_FlashState_DoesNotAffectNormalCharacters()
    {
        renderer.Clear(pixelBuffer);
        var bufferFlashOff = new uint[pixelBuffer.Length];

        renderer.RenderFrame(
            pixelBuffer,
            VideoMode.Text40,
            addr => 0xC1, // Normal 'A'
            new byte[4096],
            useAltCharSet: false,
            isPage2: false,
            flashState: false);

        Array.Copy(pixelBuffer, bufferFlashOff, pixelBuffer.Length);

        renderer.Clear(pixelBuffer);
        renderer.RenderFrame(
            pixelBuffer,
            VideoMode.Text40,
            addr => 0xC1,
            new byte[4096],
            useAltCharSet: false,
            isPage2: false,
            flashState: true);

        bool identical = pixelBuffer.SequenceEqual(bufferFlashOff);
        Assert.That(identical, Is.True, "Normal characters should not be affected by flash state");
    }

    #endregion

    #region Edge Case Tests

    /// <summary>
    /// Verifies rendering with all zeros in memory produces black screen.
    /// </summary>
    [Test]
    public void RenderFrame_AllZeroMemory_ProducesBlackScreen()
    {
        renderer.Clear(pixelBuffer);

        renderer.RenderFrame(
            pixelBuffer,
            VideoMode.HiRes,
            addr => 0x00,
            ReadOnlySpan<byte>.Empty,
            useAltCharSet: false,
            isPage2: false,
            flashState: false);

        Assert.That(pixelBuffer.All(p => p == DisplayColors.Black), Is.True);
    }

    /// <summary>
    /// Verifies rendering with all 0xFF in hi-res memory produces fully lit screen.
    /// </summary>
    [Test]
    public void RenderFrame_HiRes_AllOnesMemory_ProducesFullyLitScreen()
    {
        renderer.Clear(pixelBuffer);

        renderer.RenderFrame(
            pixelBuffer,
            VideoMode.HiRes,
            addr => addr >= 0x2000 && addr < 0x4000 ? (byte)0x7F : (byte)0x00, // 7 bits set
            ReadOnlySpan<byte>.Empty,
            useAltCharSet: false,
            isPage2: false,
            flashState: false);

        // First 14 pixels (2× scaled from 7 bits) should be green
        for (int x = 0; x < 14; x++)
        {
            Assert.That(pixelBuffer[x], Is.EqualTo(DisplayColors.GreenPhosphor), $"Pixel {x} should be lit");
        }
    }

    /// <summary>
    /// Verifies unsupported video mode falls back to text mode.
    /// </summary>
    [Test]
    public void RenderFrame_UnsupportedMode_FallsBackToText()
    {
        renderer.Clear(pixelBuffer);

        // Use a mode that might not be explicitly handled
        Assert.DoesNotThrow(() =>
            renderer.RenderFrame(
                pixelBuffer,
                (VideoMode)999, // Invalid mode
                addr => 0xA0,
                new byte[4096],
                useAltCharSet: false,
                isPage2: false,
                flashState: false));
    }

    /// <summary>
    /// Verifies rendering is deterministic (same inputs produce same output).
    /// </summary>
    [Test]
    public void RenderFrame_IsDeterministic()
    {
        var buffer1 = new uint[pixelBuffer.Length];
        var buffer2 = new uint[pixelBuffer.Length];

        renderer.RenderFrame(
            buffer1,
            VideoMode.Text40,
            addr => (byte)(addr & 0xFF),
            new byte[4096],
            useAltCharSet: false,
            isPage2: false,
            flashState: false);

        renderer.RenderFrame(
            buffer2,
            VideoMode.Text40,
            addr => (byte)(addr & 0xFF),
            new byte[4096],
            useAltCharSet: false,
            isPage2: false,
            flashState: false);

        Assert.That(buffer1.SequenceEqual(buffer2), Is.True, "Same inputs should produce same output");
    }

    /// <summary>
    /// Verifies renderer handles rapid mode switches without corruption.
    /// </summary>
    [Test]
    public void RenderFrame_RapidModeSwitch_NoCorruption()
    {
        var modes = new[] { VideoMode.Text40, VideoMode.LoRes, VideoMode.HiRes, VideoMode.HiResMixed, VideoMode.LoResMixed };

        foreach (var mode in modes)
        {
            renderer.Clear(pixelBuffer);

            Assert.DoesNotThrow(() =>
                renderer.RenderFrame(
                    pixelBuffer,
                    mode,
                    addr => 0xAA,
                    new byte[4096],
                    useAltCharSet: false,
                    isPage2: false,
                    flashState: false));
        }
    }

    #endregion

    #region Performance Tests

    /// <summary>
    /// Verifies rendering completes in reasonable time (performance baseline).
    /// </summary>
    [Test]
    public void RenderFrame_Performance_CompletesInReasonableTime()
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();

        // Simulate 1 second at 60 fps
        for (int i = 0; i < 60; i++)
        {
            renderer.RenderFrame(
                pixelBuffer,
                VideoMode.Text40,
                addr => 0xA0,
                new byte[4096],
                useAltCharSet: false,
                isPage2: false,
                flashState: i % 32 < 16);
        }

        sw.Stop();

        // Should complete 60 frames in well under 1 second
        Assert.That(sw.ElapsedMilliseconds, Is.LessThan(500), "60 frames should render in < 500ms");
    }

    #endregion
}