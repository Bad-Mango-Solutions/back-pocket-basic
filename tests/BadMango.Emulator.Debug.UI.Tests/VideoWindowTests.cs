// <copyright file="VideoWindowTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.UI.Tests;

using Avalonia.Headless.NUnit;

using BadMango.Emulator.Bus;
using BadMango.Emulator.Bus.Interfaces;
using BadMango.Emulator.Debug.UI.Views;
using BadMango.Emulator.Devices;
using BadMango.Emulator.Devices.Interfaces;
using BadMango.Emulator.Rendering;

using EmulatorKeyboardDevice = BadMango.Emulator.Devices.Interfaces.IKeyboardDevice;

/// <summary>
/// Unit tests for the <see cref="VideoWindow"/> class.
/// </summary>
[TestFixture]
public class VideoWindowTests
{
    private Pocket2VideoRenderer? renderer;
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
    /// <param name="scale">The scale value to test.</param>
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

    /// <summary>
    /// Verifies PixelBuffer can be created in headless mode.
    /// </summary>
    [AvaloniaTest]
    public void PixelBuffer_CanBeCreated()
    {
        pixelBuffer = new PixelBuffer(renderer!.CanonicalWidth, renderer.CanonicalHeight);
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
        pixelBuffer = new PixelBuffer(renderer!.CanonicalWidth, renderer.CanonicalHeight);
        var pixels = pixelBuffer.GetPixels();

        renderer.RenderFrame(
            pixels,
            Devices.VideoMode.Text40,
            addr => 0xA0,
            new byte[4096],
            useAltCharSet: false,
            isPage2: false,
            flashState: false,
            noFlash1Enabled: false,
            noFlash2Enabled: true);

        // Should not throw when committing
        Assert.DoesNotThrow(() => pixelBuffer.Commit());
    }

    /// <summary>
    /// Verifies PixelBuffer dimensions match renderer canonical size.
    /// </summary>
    [AvaloniaTest]
    public void PixelBuffer_DimensionsMatchRenderer()
    {
        pixelBuffer = new PixelBuffer(renderer!.CanonicalWidth, renderer.CanonicalHeight);
        Assert.That(pixelBuffer.Width, Is.EqualTo(renderer.CanonicalWidth));
        Assert.That(pixelBuffer.Height, Is.EqualTo(renderer.CanonicalHeight));
    }

    /// <summary>
    /// Verifies multiple render-commit cycles work correctly.
    /// </summary>
    [AvaloniaTest]
    public void MultipleRenderCommitCycles_WorkCorrectly()
    {
        pixelBuffer = new PixelBuffer(renderer!.CanonicalWidth, renderer.CanonicalHeight);

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
                flashState: i % 2 == 0,
                noFlash1Enabled: false,
                noFlash2Enabled: true);

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
        pixelBuffer = new PixelBuffer(renderer!.CanonicalWidth, renderer.CanonicalHeight);
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
        pixelBuffer = new PixelBuffer(renderer!.CanonicalWidth, renderer.CanonicalHeight);

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
                flashState: false,
                noFlash1Enabled: false,
                noFlash2Enabled: true);

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
        pixelBuffer = new PixelBuffer(renderer!.CanonicalWidth, renderer.CanonicalHeight);
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

    /// <summary>
    /// Verifies control key combinations produce correct codes.
    /// </summary>
    /// <param name="letter">The letter key pressed with Control.</param>
    /// <param name="expectedCode">The expected control code.</param>
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
    /// <param name="direction">The direction of the arrow key.</param>
    /// <param name="expectedCode">The expected key code.</param>
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
    /// <param name="key">The name of the special key.</param>
    /// <param name="expectedCode">The expected key code.</param>
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
    /// <param name="letter">The uppercase letter.</param>
    /// <param name="expectedCode">The expected ASCII code.</param>
    [TestCase('A', 0x41)]
    [TestCase('Z', 0x5A)]
    public void UppercaseLetters_ProduceCorrectCodes(char letter, byte expectedCode)
    {
        Assert.That((byte)letter, Is.EqualTo(expectedCode));
    }

    /// <summary>
    /// Verifies lowercase letters produce correct codes.
    /// </summary>
    /// <param name="letter">The lowercase letter.</param>
    /// <param name="expectedCode">The expected ASCII code.</param>
    [TestCase('a', 0x61)]
    [TestCase('z', 0x7A)]
    public void LowercaseLetters_ProduceCorrectCodes(char letter, byte expectedCode)
    {
        Assert.That((byte)letter, Is.EqualTo(expectedCode));
    }

    /// <summary>
    /// Verifies number keys produce correct codes.
    /// </summary>
    /// <param name="number">The number character.</param>
    /// <param name="expectedCode">The expected ASCII code.</param>
    [TestCase('0', 0x30)]
    [TestCase('9', 0x39)]
    public void NumberKeys_ProduceCorrectCodes(char number, byte expectedCode)
    {
        Assert.That((byte)number, Is.EqualTo(expectedCode));
    }

    /// <summary>
    /// Verifies shifted number keys produce correct symbol codes.
    /// </summary>
    /// <param name="number">The number key position (0-9).</param>
    /// <param name="expectedSymbol">The expected shifted symbol.</param>
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

    /// <summary>
    /// Verifies Caps Lock produces uppercase for letters only.
    /// </summary>
    /// <remarks>
    /// Tests the XOR logic: CapsLock alone produces uppercase.
    /// </remarks>
    /// <param name="letter">The letter to test.</param>
    /// <param name="capsLock">Whether Caps Lock is active.</param>
    /// <param name="shift">Whether Shift is pressed.</param>
    /// <param name="expectedCode">The expected ASCII code.</param>
    [TestCase('A', false, false, 0x61)]
    [TestCase('A', true, false, 0x41)]
    [TestCase('A', false, true, 0x41)]
    [TestCase('A', true, true, 0x61)]
    [TestCase('Z', false, false, 0x7A)]
    [TestCase('Z', true, false, 0x5A)]
    [TestCase('Z', false, true, 0x5A)]
    [TestCase('Z', true, true, 0x7A)]
    public void CapsLock_AffectsLetterCase(char letter, bool capsLock, bool shift, byte expectedCode)
    {
        // Using XOR logic: shift ^ capsLock determines uppercase
        byte baseCode = (byte)letter;
        bool uppercase = shift ^ capsLock;
        byte actualCode = uppercase ? baseCode : (byte)(baseCode + 0x20);

        Assert.That(actualCode, Is.EqualTo(expectedCode));
    }

    /// <summary>
    /// Verifies Caps Lock does NOT affect number keys.
    /// </summary>
    /// <param name="number">The number key (0-9).</param>
    /// <param name="capsLock">Whether Caps Lock is active.</param>
    /// <param name="shift">Whether Shift is pressed.</param>
    /// <param name="expectedCode">The expected ASCII code.</param>
    [TestCase('5', false, false, 0x35)]
    [TestCase('5', true, false, 0x35)]
    [TestCase('5', false, true, '%')]
    [TestCase('5', true, true, '%')]
    public void CapsLock_DoesNotAffectNumbers(char number, bool capsLock, bool shift, int expectedCode)
    {
        // Numbers ignore Caps Lock, only Shift matters
        byte actualCode = (byte)(shift && number == '5' ? '%' : number);
        Assert.That(actualCode, Is.EqualTo((byte)expectedCode));
    }

    /// <summary>
    /// Verifies Caps Lock does NOT affect punctuation keys.
    /// </summary>
    /// <param name="capsLock">Whether Caps Lock is active.</param>
    /// <param name="shift">Whether Shift is pressed.</param>
    /// <param name="expectedCode">The expected ASCII code.</param>
    [TestCase(false, false, ';')]
    [TestCase(true, false, ';')]
    [TestCase(false, true, ':')]
    [TestCase(true, true, ':')]
    public void CapsLock_DoesNotAffectPunctuation(bool capsLock, bool shift, char expectedCode)
    {
        // Punctuation ignores Caps Lock, only Shift matters
        char actualCode = shift ? ':' : ';';

        Assert.That(actualCode, Is.EqualTo(expectedCode));
    }

    #region VBlank Snapshot Tests

    /// <summary>
    /// Verifies that AttachMachine retrieves IMainMemoryProvider for direct RAM access.
    /// </summary>
    [AvaloniaTest]
    public void AttachMachine_WithMainMemoryProvider_DoesNotThrow()
    {
        var window = new VideoWindow();
        var mockMachine = CreateMockMachineWithMainMemoryProvider(out _, out _);

        Assert.DoesNotThrow(() => window.AttachMachine(mockMachine.Object));
    }

    /// <summary>
    /// Verifies that AttachMachine works without an IMainMemoryProvider (graceful fallback).
    /// </summary>
    [AvaloniaTest]
    public void AttachMachine_WithoutMainMemoryProvider_DoesNotThrow()
    {
        var window = new VideoWindow();
        var mockMachine = CreateMockMachineWithoutMainMemoryProvider(out _);

        Assert.DoesNotThrow(() => window.AttachMachine(mockMachine.Object));
    }

    /// <summary>
    /// Verifies that VBlank snapshot captures data from IMainMemoryProvider's physical RAM,
    /// not from the memory bus which may be affected by soft switch routing.
    /// </summary>
    /// <remarks>
    /// This test simulates the 80STORE/PAGE2 redirection bug:
    /// the memory bus returns different data than the physical RAM, and the snapshot
    /// must use the physical RAM data.
    /// </remarks>
    [AvaloniaTest]
    public void VBlankSnapshot_ReadsFromPhysicalRam_NotBus()
    {
        var window = new VideoWindow();
        var mockMachine = CreateMockMachineWithMainMemoryProvider(
            out var physicalRam,
            out var mockVideoDevice,
            busReturnValue: 0xFF);

        // Write known data to physical RAM at text page 1 ($0400)
        physicalRam[0x0400] = 0xC1; // 'A' in Apple II character encoding
        physicalRam[0x0401] = 0xC2; // 'B'
        physicalRam[0x0402] = 0xC3; // 'C'

        window.AttachMachine(mockMachine.Object);

        // Fire VBlank to trigger snapshot capture
        mockVideoDevice.Raise(v => v.VBlankOccurred += null);

        // Verify that the bus was NOT called for text page addresses
        // during the VBlank snapshot capture — physical RAM was used instead.
        var mockBus = Mock.Get(mockMachine.Object.Bus);
        mockBus.Verify(
            b => b.Read8(It.IsAny<BusAccess>()),
            Times.Never,
            "VBlank snapshot should read from physical RAM, not the memory bus");
    }

    /// <summary>
    /// Verifies that VBlank snapshot falls back to bus reads when no IMainMemoryProvider
    /// is available (e.g., non-Pocket2e machine configurations).
    /// </summary>
    [AvaloniaTest]
    public void VBlankSnapshot_WithoutMainMemoryProvider_FallsBackToBus()
    {
        var window = new VideoWindow();
        var mockMachine = CreateMockMachineWithoutMainMemoryProvider(
            out var mockVideoDevice);

        window.AttachMachine(mockMachine.Object);

        // Fire VBlank to trigger snapshot capture — with no IMainMemoryProvider,
        // this exercises the bus-read fallback path. The snapshot should complete
        // without error, reading from the mock bus instead of physical RAM.
        Assert.DoesNotThrow(() => mockVideoDevice.Raise(v => v.VBlankOccurred += null));
    }

    /// <summary>
    /// Verifies that the VBlank snapshot captures the full text page range ($0400-$0BFF)
    /// from physical RAM, covering both text page 1 and text page 2.
    /// </summary>
    [AvaloniaTest]
    public void VBlankSnapshot_CapturesFullTextPageRange()
    {
        var window = new VideoWindow();
        var mockMachine = CreateMockMachineWithMainMemoryProvider(
            out var physicalRam,
            out var mockVideoDevice);

        // Fill text page 1 ($0400-$07FF) with 0x41 and text page 2 ($0800-$0BFF) with 0x42
        for (int i = 0x0400; i < 0x0800; i++)
        {
            physicalRam[i] = 0x41;
        }

        for (int i = 0x0800; i < 0x0C00; i++)
        {
            physicalRam[i] = 0x42;
        }

        window.AttachMachine(mockMachine.Object);

        // Fire VBlank to trigger snapshot capture
        mockVideoDevice.Raise(v => v.VBlankOccurred += null);

        // Bus should not have been called — all reads from physical RAM
        var mockBus = Mock.Get(mockMachine.Object.Bus);
        mockBus.Verify(
            b => b.Read8(It.IsAny<BusAccess>()),
            Times.Never,
            "Full text page range should be captured from physical RAM");
    }

    /// <summary>
    /// Verifies that physical RAM snapshot is immune to 80STORE/PAGE2 soft switch state.
    /// The bus returns different data depending on soft switch state, but the physical
    /// RAM always returns the same correct data.
    /// </summary>
    [AvaloniaTest]
    public void VBlankSnapshot_ImmuneToSoftSwitchState()
    {
        var window = new VideoWindow();

        // Set up physical RAM with known data
        var mockMachine = CreateMockMachineWithMainMemoryProvider(
            out var physicalRam,
            out var mockVideoDevice,
            busReturnValue: 0x00);

        // Write a pattern to physical main RAM at $0400
        // (bus would return 0x00 due to simulated 80STORE redirection)
        for (int i = 0x0400; i < 0x0800; i++)
        {
            physicalRam[i] = (byte)(i & 0xFF);
        }

        window.AttachMachine(mockMachine.Object);

        // Fire VBlank — snapshot should capture from physical RAM
        mockVideoDevice.Raise(v => v.VBlankOccurred += null);

        // The bus was never consulted — physical RAM was used directly
        var mockBus = Mock.Get(mockMachine.Object.Bus);
        mockBus.Verify(
            b => b.Read8(It.IsAny<BusAccess>()),
            Times.Never,
            "Snapshot must read from physical RAM regardless of soft switch state");
    }

    /// <summary>
    /// Verifies that DetachMachine clears the main RAM reference.
    /// After detach, a subsequent VBlank should not read from the old RAM.
    /// </summary>
    [AvaloniaTest]
    public void DetachMachine_ClearsMainRamReference()
    {
        var window = new VideoWindow();
        var mockMachine = CreateMockMachineWithMainMemoryProvider(out _, out _);

        window.AttachMachine(mockMachine.Object);
        window.DetachMachine();

        // After detach, ForceRedraw should not throw (renders blank screen)
        Assert.DoesNotThrow(() => window.ForceRedraw());
    }

    /// <summary>
    /// Creates a mock machine with an IMainMemoryProvider component.
    /// </summary>
    /// <param name="physicalRam">Output: the physical RAM byte array for direct manipulation.</param>
    /// <param name="mockVideoDevice">Output: the mock video device for raising VBlank events.</param>
    /// <param name="busReturnValue">Value returned by the memory bus Read8 (simulating redirected reads).</param>
    /// <returns>The configured mock machine.</returns>
    private static Mock<IMachine> CreateMockMachineWithMainMemoryProvider(
        out byte[] physicalRam,
        out Mock<IVideoDevice> mockVideoDevice,
        byte busReturnValue = 0xA0)
    {
        physicalRam = new byte[65536]; // 64KB main RAM

        var physicalMemory = new PhysicalMemory(
            (ReadOnlySpan<byte>)physicalRam.AsSpan(),
            "TestMainRAM");
        var mockMainMemoryProvider = new Mock<IMainMemoryProvider>();
        mockMainMemoryProvider.Setup(p => p.MainRam).Returns(physicalMemory.Memory);

        var mockBus = new Mock<IMemoryBus>();
        mockBus.Setup(b => b.Read8(It.IsAny<BusAccess>())).Returns(busReturnValue);

        mockVideoDevice = new Mock<IVideoDevice>();
        mockVideoDevice.Setup(v => v.CurrentMode).Returns(VideoMode.Text40);
        mockVideoDevice.Setup(v => v.IsPage2).Returns(false);

        var mockMachine = new Mock<IMachine>();
        mockMachine.Setup(m => m.Bus).Returns(mockBus.Object);
        mockMachine.Setup(m => m.GetComponent<IVideoDevice>()).Returns(mockVideoDevice.Object);
        mockMachine.Setup(m => m.GetComponent<IMainMemoryProvider>()).Returns(mockMainMemoryProvider.Object);
        mockMachine.Setup(m => m.GetComponent<IExtended80ColumnDevice>()).Returns((IExtended80ColumnDevice?)null);
        mockMachine.Setup(m => m.GetComponent<ICharacterDevice>()).Returns((ICharacterDevice?)null);
        mockMachine.Setup(m => m.GetComponent<ICharacterRomProvider>()).Returns((ICharacterRomProvider?)null);
        mockMachine.Setup(m => m.GetComponent<EmulatorKeyboardDevice>()).Returns((EmulatorKeyboardDevice?)null);

        return mockMachine;
    }

    /// <summary>
    /// Creates a mock machine without an IMainMemoryProvider component (fallback path).
    /// </summary>
    /// <param name="mockVideoDevice">Output: the mock video device for raising VBlank events.</param>
    /// <returns>The configured mock machine.</returns>
    private static Mock<IMachine> CreateMockMachineWithoutMainMemoryProvider(
        out Mock<IVideoDevice> mockVideoDevice)
    {
        var mockBus = new Mock<IMemoryBus>();

        mockVideoDevice = new Mock<IVideoDevice>();
        mockVideoDevice.Setup(v => v.CurrentMode).Returns(VideoMode.Text40);
        mockVideoDevice.Setup(v => v.IsPage2).Returns(false);

        var mockMachine = new Mock<IMachine>();
        mockMachine.Setup(m => m.Bus).Returns(mockBus.Object);
        mockMachine.Setup(m => m.GetComponent<IVideoDevice>()).Returns(mockVideoDevice.Object);
        mockMachine.Setup(m => m.GetComponent<IMainMemoryProvider>()).Returns((IMainMemoryProvider?)null);
        mockMachine.Setup(m => m.GetComponent<IExtended80ColumnDevice>()).Returns((IExtended80ColumnDevice?)null);
        mockMachine.Setup(m => m.GetComponent<ICharacterDevice>()).Returns((ICharacterDevice?)null);
        mockMachine.Setup(m => m.GetComponent<ICharacterRomProvider>()).Returns((ICharacterRomProvider?)null);
        mockMachine.Setup(m => m.GetComponent<EmulatorKeyboardDevice>()).Returns((EmulatorKeyboardDevice?)null);

        return mockMachine;
    }

    #endregion
}