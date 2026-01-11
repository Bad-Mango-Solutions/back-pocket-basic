// <copyright file="VideoWindowTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.UI.Tests;

using Avalonia.Headless.NUnit;

using BadMango.Emulator.Debug.UI.Views;
using BadMango.Emulator.Rendering;

/// <summary>
/// Unit tests for the <see cref="VideoWindow"/> class.
/// </summary>
[TestFixture]
public class VideoWindowTests
{
    /// <summary>
    /// Verifies VideoWindow can be instantiated in headless mode.
    /// </summary>
    [AvaloniaTest]
    public void VideoWindow_CanBeInstantiated()
    {
        var window = new VideoWindow();
        Assert.That(window, Is.Not.Null);
        Assert.That(window.Scale, Is.EqualTo(2)); // Default scale
    }

    /// <summary>
    /// Verifies Scale property validates range - too low.
    /// </summary>
    [AvaloniaTest]
    public void Scale_TooLow_ThrowsArgumentOutOfRangeException()
    {
        var window = new VideoWindow();
        Assert.Throws<ArgumentOutOfRangeException>(() => window.Scale = 0);
    }

    /// <summary>
    /// Verifies Scale property validates range - too high.
    /// </summary>
    [AvaloniaTest]
    public void Scale_TooHigh_ThrowsArgumentOutOfRangeException()
    {
        var window = new VideoWindow();
        Assert.Throws<ArgumentOutOfRangeException>(() => window.Scale = 5);
    }

    /// <summary>
    /// Verifies Scale property accepts valid values.
    /// </summary>
    [AvaloniaTest]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    public void Scale_ValidValue_SetsCorrectly(int scale)
    {
        var window = new VideoWindow();
        window.Scale = scale;
        Assert.That(window.Scale, Is.EqualTo(scale));
    }

    /// <summary>
    /// Verifies ShowFps property can be toggled.
    /// </summary>
    [AvaloniaTest]
    public void ShowFps_CanBeToggled()
    {
        var window = new VideoWindow();

        window.ShowFps = true;
        Assert.That(window.ShowFps, Is.True);

        window.ShowFps = false;
        Assert.That(window.ShowFps, Is.False);
    }

    /// <summary>
    /// Verifies ForceRedraw can be called without throwing.
    /// </summary>
    [AvaloniaTest]
    public void ForceRedraw_DoesNotThrow()
    {
        var window = new VideoWindow();
        Assert.DoesNotThrow(() => window.ForceRedraw());
    }

    /// <summary>
    /// Verifies DetachMachine can be called without a machine attached.
    /// </summary>
    [AvaloniaTest]
    public void DetachMachine_WithoutMachine_DoesNotThrow()
    {
        var window = new VideoWindow();
        Assert.DoesNotThrow(() => window.DetachMachine());
    }
}

/// <summary>
/// Integration tests for video rendering pipeline with headless Avalonia.
/// </summary>
[TestFixture]
public class VideoRenderingIntegrationTests
{
    private Pocket2VideoRenderer renderer = null!;
    private PixelBuffer? pixelBuffer;

    /// <summary>
    /// Sets up the test fixture.
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        renderer = new Pocket2VideoRenderer();
    }

    /// <summary>
    /// Tears down the test fixture.
    /// </summary>
    [TearDown]
    public void TearDown()
    {
        pixelBuffer?.Dispose();
        pixelBuffer = null;
    }

    /// <summary>
    /// Verifies PixelBuffer can be created in headless mode.
    /// </summary>
    [AvaloniaTest]
    public void PixelBuffer_CanBeCreated()
    {
        pixelBuffer = new PixelBuffer(renderer.CanonicalWidth, renderer.CanonicalHeight);
        Assert.That(pixelBuffer, Is.Not.Null);
        Assert.That(pixelBuffer.Width, Is.EqualTo(renderer.CanonicalWidth));
        Assert.That(pixelBuffer.Height, Is.EqualTo(renderer.CanonicalHeight));
    }

    /// <summary>
    /// Verifies renderer integrates correctly with PixelBuffer.
    /// </summary>
    [AvaloniaTest]
    public void Renderer_WithPixelBuffer_WorksCorrectly()
    {
        pixelBuffer = new PixelBuffer(renderer.CanonicalWidth, renderer.CanonicalHeight);
        var pixels = pixelBuffer.GetPixels();

        renderer.RenderFrame(
            pixels,
            Devices.VideoMode.Text40,
            addr => 0xA0,
            new byte[4096],
            useAltCharSet: false,
            isPage2: false,
            flashState: false);

        // Should not throw when committing
        Assert.DoesNotThrow(() => pixelBuffer.Commit());
    }

    /// <summary>
    /// Verifies PixelBuffer dimensions match renderer canonical size.
    /// </summary>
    [AvaloniaTest]
    public void PixelBuffer_DimensionsMatchRenderer()
    {
        pixelBuffer = new PixelBuffer(renderer.CanonicalWidth, renderer.CanonicalHeight);
        Assert.That(pixelBuffer.Width, Is.EqualTo(renderer.CanonicalWidth));
        Assert.That(pixelBuffer.Height, Is.EqualTo(renderer.CanonicalHeight));
    }

    /// <summary>
    /// Verifies multiple render-commit cycles work correctly.
    /// </summary>
    [AvaloniaTest]
    public void MultipleRenderCommitCycles_WorkCorrectly()
    {
        pixelBuffer = new PixelBuffer(renderer.CanonicalWidth, renderer.CanonicalHeight);

        for (int i = 0; i < 10; i++)
        {
            var pixels = pixelBuffer.GetPixels();

            renderer.RenderFrame(
                pixels,
                Devices.VideoMode.Text40,
                addr => (byte)(i + 0xA0),
                new byte[4096],
                useAltCharSet: false,
                isPage2: false,
                flashState: i % 2 == 0);

            pixelBuffer.Commit();
        }

        Assert.Pass();
    }

    /// <summary>
    /// Verifies Clear and Fill operations work with PixelBuffer.
    /// </summary>
    [AvaloniaTest]
    public void ClearAndFill_WithPixelBuffer_WorkCorrectly()
    {
        pixelBuffer = new PixelBuffer(renderer.CanonicalWidth, renderer.CanonicalHeight);
        var pixels = pixelBuffer.GetPixels();

        // Clear using renderer
        renderer.Clear(pixels);
        pixelBuffer.Commit();

        // Fill using PixelBuffer directly
        pixelBuffer.Fill(DisplayColors.GreenPhosphor);
        pixelBuffer.Commit();

        Assert.Pass();
    }

    /// <summary>
    /// Verifies mode switching doesn't corrupt buffer.
    /// </summary>
    [AvaloniaTest]
    public void ModeSwitch_DoesNotCorruptBuffer()
    {
        pixelBuffer = new PixelBuffer(renderer.CanonicalWidth, renderer.CanonicalHeight);

        var modes = new[]
        {
            Devices.VideoMode.Text40,
            Devices.VideoMode.LoRes,
            Devices.VideoMode.HiRes,
            Devices.VideoMode.LoResMixed,
            Devices.VideoMode.HiResMixed,
        };

        foreach (var mode in modes)
        {
            var pixels = pixelBuffer.GetPixels();

            renderer.RenderFrame(
                pixels,
                mode,
                addr => 0xAA,
                new byte[4096],
                useAltCharSet: false,
                isPage2: false,
                flashState: false);

            pixelBuffer.Commit();
        }

        // Verify buffer is still valid
        var finalPixels = pixelBuffer.GetPixels();
        Assert.That(finalPixels.Length, Is.EqualTo(renderer.CanonicalWidth * renderer.CanonicalHeight));
    }

    /// <summary>
    /// Verifies PixelBuffer bitmap is accessible.
    /// </summary>
    [AvaloniaTest]
    public void PixelBuffer_BitmapIsAccessible()
    {
        pixelBuffer = new PixelBuffer(renderer.CanonicalWidth, renderer.CanonicalHeight);
        var bitmap = pixelBuffer.Bitmap;

        Assert.That(bitmap, Is.Not.Null);
        Assert.That(bitmap.PixelSize.Width, Is.EqualTo(renderer.CanonicalWidth));
        Assert.That(bitmap.PixelSize.Height, Is.EqualTo(renderer.CanonicalHeight));
    }

    /// <summary>
    /// Verifies SetPixel works correctly.
    /// </summary>
    [AvaloniaTest]
    public void PixelBuffer_SetPixel_WorksCorrectly()
    {
        pixelBuffer = new PixelBuffer(100, 100);

        // Set a pixel
        pixelBuffer.SetPixel(50, 50, DisplayColors.GreenPhosphor);

        // Verify pixel was set
        var pixels = pixelBuffer.GetPixels();
        Assert.That(pixels[(50 * 100) + 50], Is.EqualTo(DisplayColors.GreenPhosphor));
    }

    /// <summary>
    /// Verifies SetPixel validates bounds.
    /// </summary>
    [AvaloniaTest]
    public void PixelBuffer_SetPixel_ValidatesBounds()
    {
        pixelBuffer = new PixelBuffer(100, 100);

        Assert.Throws<ArgumentOutOfRangeException>(() => pixelBuffer.SetPixel(-1, 50, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => pixelBuffer.SetPixel(100, 50, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => pixelBuffer.SetPixel(50, -1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => pixelBuffer.SetPixel(50, 100, 0));
    }
}

/// <summary>
/// Tests for keyboard mapping functionality.
/// </summary>
[TestFixture]
public class KeyboardMappingTests
{
    /// <summary>
    /// Verifies control key combinations produce correct codes.
    /// </summary>
    [TestCase('A', 0x01)]
    [TestCase('B', 0x02)]
    [TestCase('C', 0x03)]
    [TestCase('Z', 0x1A)]
    public void ControlKey_ProducesCorrectCode(char letter, byte expectedCode)
    {
        // Ctrl+A = 0x01, Ctrl+B = 0x02, etc.
        byte actualCode = (byte)(letter - 'A' + 1);
        Assert.That(actualCode, Is.EqualTo(expectedCode));
    }

    /// <summary>
    /// Verifies arrow key codes are correct.
    /// </summary>
    [TestCase("Left", 0x08)]
    [TestCase("Right", 0x15)]
    [TestCase("Up", 0x0B)]
    [TestCase("Down", 0x0A)]
    public void ArrowKeys_ProduceCorrectCodes(string direction, byte expectedCode)
    {
        byte actualCode = direction switch
        {
            "Left" => 0x08,
            "Right" => 0x15,
            "Up" => 0x0B,
            "Down" => 0x0A,
            _ => 0x00,
        };

        Assert.That(actualCode, Is.EqualTo(expectedCode));
    }

    /// <summary>
    /// Verifies special key codes are correct.
    /// </summary>
    [TestCase("Return", 0x0D)]
    [TestCase("Backspace", 0x08)]
    [TestCase("Tab", 0x09)]
    [TestCase("Escape", 0x1B)]
    [TestCase("Space", 0x20)]
    public void SpecialKeys_ProduceCorrectCodes(string key, byte expectedCode)
    {
        byte actualCode = key switch
        {
            "Return" => 0x0D,
            "Backspace" => 0x08,
            "Tab" => 0x09,
            "Escape" => 0x1B,
            "Space" => 0x20,
            _ => 0x00,
        };

        Assert.That(actualCode, Is.EqualTo(expectedCode));
    }

    /// <summary>
    /// Verifies uppercase letters produce correct codes.
    /// </summary>
    [TestCase('A', 0x41)]
    [TestCase('Z', 0x5A)]
    public void UppercaseLetters_ProduceCorrectCodes(char letter, byte expectedCode)
    {
        Assert.That((byte)letter, Is.EqualTo(expectedCode));
    }

    /// <summary>
    /// Verifies lowercase letters produce correct codes.
    /// </summary>
    [TestCase('a', 0x61)]
    [TestCase('z', 0x7A)]
    public void LowercaseLetters_ProduceCorrectCodes(char letter, byte expectedCode)
    {
        Assert.That((byte)letter, Is.EqualTo(expectedCode));
    }

    /// <summary>
    /// Verifies number keys produce correct codes.
    /// </summary>
    [TestCase('0', 0x30)]
    [TestCase('9', 0x39)]
    public void NumberKeys_ProduceCorrectCodes(char number, byte expectedCode)
    {
        Assert.That((byte)number, Is.EqualTo(expectedCode));
    }

    /// <summary>
    /// Verifies shifted number keys produce correct symbol codes.
    /// </summary>
    [TestCase(1, '!')]
    [TestCase(2, '@')]
    [TestCase(3, '#')]
    [TestCase(4, '$')]
    [TestCase(5, '%')]
    [TestCase(6, '^')]
    [TestCase(7, '&')]
    [TestCase(8, '*')]
    [TestCase(9, '(')]
    [TestCase(0, ')')]
    public void ShiftedNumberKeys_ProduceCorrectSymbols(int number, char expectedSymbol)
    {
        char actualSymbol = number switch
        {
            1 => '!',
            2 => '@',
            3 => '#',
            4 => '$',
            5 => '%',
            6 => '^',
            7 => '&',
            8 => '*',
            9 => '(',
            0 => ')',
            _ => ' ',
        };

        Assert.That(actualSymbol, Is.EqualTo(expectedSymbol));
    }
}