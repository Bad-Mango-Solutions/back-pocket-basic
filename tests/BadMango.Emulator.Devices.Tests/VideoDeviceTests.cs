// <copyright file="VideoDeviceTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Devices.Tests;

using BadMango.Emulator.Bus;
using BadMango.Emulator.Bus.Interfaces;

using Moq;

/// <summary>
/// Unit tests for the <see cref="VideoDevice"/> class.
/// </summary>
[TestFixture]
public class VideoDeviceTests
{
    private VideoDevice device = null!;
    private IOPageDispatcher dispatcher = null!;

    /// <summary>
    /// Sets up test fixtures before each test.
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        device = new();
        dispatcher = new();
        device.RegisterHandlers(dispatcher);
    }

    /// <summary>
    /// Verifies that Name returns the correct value.
    /// </summary>
    [Test]
    public void Name_ReturnsVideoDevice()
    {
        Assert.That(device.Name, Is.EqualTo("Video Device"));
    }

    /// <summary>
    /// Verifies that DeviceType returns the correct value.
    /// </summary>
    [Test]
    public void DeviceType_ReturnsVideo()
    {
        Assert.That(device.DeviceType, Is.EqualTo("Video"));
    }

    /// <summary>
    /// Verifies that Kind returns Motherboard.
    /// </summary>
    [Test]
    public void Kind_ReturnsMotherboard()
    {
        Assert.That(device.Kind, Is.EqualTo(PeripheralKind.Motherboard));
    }

    /// <summary>
    /// Verifies that IsTextMode is initially true.
    /// </summary>
    [Test]
    public void IsTextMode_InitiallyTrue()
    {
        Assert.That(device.IsTextMode, Is.True);
    }

    /// <summary>
    /// Verifies that reading $C050 sets graphics mode.
    /// </summary>
    [Test]
    public void ReadC050_SetsGraphicsMode()
    {
        var context = CreateTestContext();

        _ = dispatcher.Read(0x50, in context);

        Assert.That(device.IsTextMode, Is.False);
    }

    /// <summary>
    /// Verifies that reading $C051 sets text mode.
    /// </summary>
    [Test]
    public void ReadC051_SetsTextMode()
    {
        var context = CreateTestContext();

        // First set to graphics mode
        _ = dispatcher.Read(0x50, in context);
        Assert.That(device.IsTextMode, Is.False);

        // Then set back to text mode
        _ = dispatcher.Read(0x51, in context);
        Assert.That(device.IsTextMode, Is.True);
    }

    /// <summary>
    /// Verifies that reading $C052 clears mixed mode.
    /// </summary>
    [Test]
    public void ReadC052_ClearsMixedMode()
    {
        var context = CreateTestContext();

        _ = dispatcher.Read(0x53, in context); // First enable mixed
        Assert.That(device.IsMixedMode, Is.True);

        _ = dispatcher.Read(0x52, in context); // Then disable
        Assert.That(device.IsMixedMode, Is.False);
    }

    /// <summary>
    /// Verifies that reading $C053 sets mixed mode.
    /// </summary>
    [Test]
    public void ReadC053_SetsMixedMode()
    {
        var context = CreateTestContext();

        _ = dispatcher.Read(0x53, in context);

        Assert.That(device.IsMixedMode, Is.True);
    }

    /// <summary>
    /// Verifies that annunciator switches work correctly.
    /// </summary>
    [Test]
    public void Annunciator_SwitchesWorkCorrectly()
    {
        var context = CreateTestContext();

        // $C058 = Annunciator 0 off
        // $C059 = Annunciator 0 on
        _ = dispatcher.Read(0x59, in context); // ANN0 on
        Assert.That(device.Annunciators[0], Is.True);

        _ = dispatcher.Read(0x58, in context); // ANN0 off
        Assert.That(device.Annunciators[0], Is.False);

        // $C05B = Annunciator 1 on
        _ = dispatcher.Read(0x5B, in context);
        Assert.That(device.Annunciators[1], Is.True);
    }

    /// <summary>
    /// Verifies that CurrentMode returns correct mode based on state.
    /// </summary>
    [Test]
    public void CurrentMode_ReturnsCorrectMode()
    {
        Assert.That(device.CurrentMode, Is.EqualTo(VideoMode.Text40));

        device.Set80ColumnMode(true);
        Assert.That(device.CurrentMode, Is.EqualTo(VideoMode.Text80));
    }

    /// <summary>
    /// Verifies that ModeChanged event is raised when mode changes.
    /// </summary>
    [Test]
    public void ModeChanged_RaisedWhenModeChanges()
    {
        VideoMode? changedMode = null;
        device.ModeChanged += mode => changedMode = mode;

        var context = CreateTestContext();
        _ = dispatcher.Read(0x50, in context); // Switch to graphics

        Assert.That(changedMode, Is.EqualTo(VideoMode.LoRes));
    }

    /// <summary>
    /// Verifies that Reset restores default state.
    /// </summary>
    [Test]
    public void Reset_RestoresDefaultState()
    {
        var context = CreateTestContext();
        _ = dispatcher.Read(0x50, in context); // Graphics
        _ = dispatcher.Read(0x53, in context); // Mixed
        _ = dispatcher.Read(0x59, in context); // ANN0 on

        device.Reset();

        Assert.Multiple(() =>
        {
            Assert.That(device.IsTextMode, Is.True);
            Assert.That(device.IsMixedMode, Is.False);
            Assert.That(device.Annunciators[0], Is.False);
        });
    }

    /// <summary>
    /// Verifies that side-effect-free reads don't change state.
    /// </summary>
    [Test]
    public void Read_WithNoSideEffects_DoesNotChangeState()
    {
        var context = CreateTestContextWithNoSideEffects();

        _ = dispatcher.Read(0x50, in context); // Try to switch to graphics

        Assert.That(device.IsTextMode, Is.True); // Should remain in text mode
    }

    /// <summary>
    /// Verifies that writes also affect video mode.
    /// </summary>
    [Test]
    public void WriteC050_AlsoSetsGraphicsMode()
    {
        var context = CreateTestContext();

        dispatcher.Write(0x50, 0x00, in context);

        Assert.That(device.IsTextMode, Is.False);
    }

    /// <summary>
    /// Verifies that Initialize does not throw.
    /// </summary>
    [Test]
    public void Initialize_DoesNotThrow()
    {
        var mockContext = new Mock<IEventContext>();
        Assert.DoesNotThrow(() => device.Initialize(mockContext.Object));
    }

    /// <summary>
    /// Verifies that VideoDevice implements ISoftSwitchProvider.
    /// </summary>
    [Test]
    public void VideoDevice_ImplementsISoftSwitchProvider()
    {
        Assert.That(device, Is.InstanceOf<ISoftSwitchProvider>());
    }

    /// <summary>
    /// Verifies that ProviderName returns Video.
    /// </summary>
    [Test]
    public void ProviderName_ReturnsVideo()
    {
        var provider = (ISoftSwitchProvider)device;
        Assert.That(provider.ProviderName, Is.EqualTo("Video"));
    }

    /// <summary>
    /// Verifies that GetSoftSwitchStates returns video mode switches.
    /// </summary>
    [Test]
    public void GetSoftSwitchStates_ReturnsVideoModeSwitches()
    {
        var provider = (ISoftSwitchProvider)device;

        var states = provider.GetSoftSwitchStates();

        Assert.Multiple(() =>
        {
            // Check for core video mode switches
            Assert.That(states.Any(s => s.Name == "TXTCLR" && s.Address == 0xC050), Is.True);
            Assert.That(states.Any(s => s.Name == "TXTSET" && s.Address == 0xC051), Is.True);
            Assert.That(states.Any(s => s.Name == "MIXCLR" && s.Address == 0xC052), Is.True);
            Assert.That(states.Any(s => s.Name == "MIXSET" && s.Address == 0xC053), Is.True);
            Assert.That(states.Any(s => s.Name == "LOWSCR" && s.Address == 0xC054), Is.True);
            Assert.That(states.Any(s => s.Name == "HISCR" && s.Address == 0xC055), Is.True);
            Assert.That(states.Any(s => s.Name == "LORES" && s.Address == 0xC056), Is.True);
            Assert.That(states.Any(s => s.Name == "HIRES" && s.Address == 0xC057), Is.True);
        });
    }

    /// <summary>
    /// Verifies that GetSoftSwitchStates returns annunciator switches.
    /// </summary>
    [Test]
    public void GetSoftSwitchStates_ReturnsAnnunciatorSwitches()
    {
        var provider = (ISoftSwitchProvider)device;

        var states = provider.GetSoftSwitchStates();

        Assert.Multiple(() =>
        {
            // Check for annunciator switches (off and on for each)
            Assert.That(states.Count(s => s.Name == "AN0"), Is.EqualTo(2));
            Assert.That(states.Count(s => s.Name == "AN1"), Is.EqualTo(2));
            Assert.That(states.Count(s => s.Name == "AN2"), Is.EqualTo(2));
            Assert.That(states.Count(s => s.Name == "AN3"), Is.EqualTo(2));
        });
    }

    /// <summary>
    /// Verifies that GetSoftSwitchStates returns status read switches.
    /// </summary>
    [Test]
    public void GetSoftSwitchStates_ReturnsStatusReadSwitches()
    {
        var provider = (ISoftSwitchProvider)device;

        var states = provider.GetSoftSwitchStates();

        Assert.Multiple(() =>
        {
            Assert.That(states.Any(s => s.Name == "RDVBL" && s.Address == 0xC019), Is.True);
            Assert.That(states.Any(s => s.Name == "RDTEXT" && s.Address == 0xC01A), Is.True);
            Assert.That(states.Any(s => s.Name == "RDMIXED" && s.Address == 0xC01B), Is.True);
            Assert.That(states.Any(s => s.Name == "RDPAGE2" && s.Address == 0xC01C), Is.True);
            Assert.That(states.Any(s => s.Name == "RDHIRES" && s.Address == 0xC01D), Is.True);
            Assert.That(states.Any(s => s.Name == "RDALTCHAR" && s.Address == 0xC01E), Is.True);
            Assert.That(states.Any(s => s.Name == "RD80COL" && s.Address == 0xC01F), Is.True);
        });
    }

    /// <summary>
    /// Verifies that reading $C019 returns correct vertical blanking status.
    /// </summary>
    [Test]
    public void ReadC019_ReturnsVerticalBlankingStatus()
    {
        var context = CreateTestContext();

        // Not in VBL - bit 7 should be set
        device.IsVerticalBlanking = false;
        byte notInVbl = dispatcher.Read(0x19, in context);
        Assert.That(notInVbl & 0x80, Is.EqualTo(0x80), "Bit 7 should be set when NOT in VBL");

        // In VBL - bit 7 should be clear (inverted from other status reads)
        device.IsVerticalBlanking = true;
        byte inVbl = dispatcher.Read(0x19, in context);
        Assert.That(inVbl & 0x80, Is.EqualTo(0x00), "Bit 7 should be clear when IN VBL");
    }

    /// <summary>
    /// Verifies that reading $C01A returns correct text mode status.
    /// </summary>
    [Test]
    public void ReadC01A_ReturnsTextModeStatus()
    {
        var context = CreateTestContext();

        // Initially in text mode - bit 7 should be set
        byte textModeOn = dispatcher.Read(0x1A, in context);
        Assert.That(textModeOn & 0x80, Is.EqualTo(0x80), "Bit 7 should be set in text mode");

        // Switch to graphics mode - bit 7 should be clear
        _ = dispatcher.Read(0x50, in context);
        byte textModeOff = dispatcher.Read(0x1A, in context);
        Assert.That(textModeOff & 0x80, Is.EqualTo(0x00), "Bit 7 should be clear in graphics mode");
    }

    /// <summary>
    /// Verifies that reading $C01B returns correct mixed mode status.
    /// </summary>
    [Test]
    public void ReadC01B_ReturnsMixedModeStatus()
    {
        var context = CreateTestContext();

        // Initially not in mixed mode - bit 7 should be clear
        byte mixedOff = dispatcher.Read(0x1B, in context);
        Assert.That(mixedOff & 0x80, Is.EqualTo(0x00), "Bit 7 should be clear when mixed mode off");

        // Enable mixed mode - bit 7 should be set
        _ = dispatcher.Read(0x53, in context);
        byte mixedOn = dispatcher.Read(0x1B, in context);
        Assert.That(mixedOn & 0x80, Is.EqualTo(0x80), "Bit 7 should be set when mixed mode on");
    }

    /// <summary>
    /// Verifies that reading $C01C returns correct page 2 status.
    /// </summary>
    [Test]
    public void ReadC01C_ReturnsPage2Status()
    {
        var context = CreateTestContext();

        // Initially page 1 - bit 7 should be clear
        byte page1 = dispatcher.Read(0x1C, in context);
        Assert.That(page1 & 0x80, Is.EqualTo(0x00), "Bit 7 should be clear for page 1");

        // Note: SetPage2 is internal, set directly for testing
        device.GetType().GetMethod("SetPage2", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.Invoke(device, [true]);

        byte page2 = dispatcher.Read(0x1C, in context);
        Assert.That(page2 & 0x80, Is.EqualTo(0x80), "Bit 7 should be set for page 2");
    }

    /// <summary>
    /// Verifies that reading $C01D returns correct hi-res mode status.
    /// </summary>
    [Test]
    public void ReadC01D_ReturnsHiResModeStatus()
    {
        var context = CreateTestContext();

        // Initially lo-res - bit 7 should be clear
        byte lores = dispatcher.Read(0x1D, in context);
        Assert.That(lores & 0x80, Is.EqualTo(0x00), "Bit 7 should be clear in lo-res mode");

        // Note: SetHiRes is internal, set directly for testing
        device.GetType().GetMethod("SetHiRes", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.Invoke(device, [true]);

        byte hires = dispatcher.Read(0x1D, in context);
        Assert.That(hires & 0x80, Is.EqualTo(0x80), "Bit 7 should be set in hi-res mode");
    }

    /// <summary>
    /// Verifies that reading $C01E returns correct alternate character set status.
    /// </summary>
    [Test]
    public void ReadC01E_ReturnsAltCharSetStatus()
    {
        var context = CreateTestContext();

        // Initially primary character set - bit 7 should be clear
        byte primarySet = dispatcher.Read(0x1E, in context);
        Assert.That(primarySet & 0x80, Is.EqualTo(0x00), "Bit 7 should be clear for primary char set");

        // Enable alternate character set
        device.SetAltCharSet(true);
        byte altSet = dispatcher.Read(0x1E, in context);
        Assert.That(altSet & 0x80, Is.EqualTo(0x80), "Bit 7 should be set for alternate char set");
    }

    /// <summary>
    /// Verifies that reading $C01F returns correct 80-column mode status.
    /// </summary>
    [Test]
    public void ReadC01F_Returns80ColumnModeStatus()
    {
        var context = CreateTestContext();

        // Initially 40-column - bit 7 should be clear
        byte col40 = dispatcher.Read(0x1F, in context);
        Assert.That(col40 & 0x80, Is.EqualTo(0x00), "Bit 7 should be clear for 40-column mode");

        // Enable 80-column mode
        device.Set80ColumnMode(true);
        byte col80 = dispatcher.Read(0x1F, in context);
        Assert.That(col80 & 0x80, Is.EqualTo(0x80), "Bit 7 should be set for 80-column mode");
    }

    /// <summary>
    /// Verifies that status reads do not have side effects.
    /// </summary>
    [Test]
    public void StatusReads_HaveNoSideEffects()
    {
        var context = CreateTestContext();

        // Ensure initial state
        bool initialText = device.IsTextMode;
        bool initialMixed = device.IsMixedMode;

        // Read all status registers
        _ = dispatcher.Read(0x19, in context);
        _ = dispatcher.Read(0x1A, in context);
        _ = dispatcher.Read(0x1B, in context);
        _ = dispatcher.Read(0x1C, in context);
        _ = dispatcher.Read(0x1D, in context);
        _ = dispatcher.Read(0x1E, in context);
        _ = dispatcher.Read(0x1F, in context);

        // State should be unchanged
        Assert.Multiple(() =>
        {
            Assert.That(device.IsTextMode, Is.EqualTo(initialText));
            Assert.That(device.IsMixedMode, Is.EqualTo(initialMixed));
        });
    }

    /// <summary>
    /// Verifies that IsCharacterRomLoaded is false when no ROM is loaded.
    /// </summary>
    [Test]
    public void IsCharacterRomLoaded_WhenNoRomLoaded_ReturnsFalse()
    {
        Assert.That(device.IsCharacterRomLoaded, Is.False);
    }

    /// <summary>
    /// Verifies that LoadCharacterRom accepts valid 4KB ROM data.
    /// </summary>
    [Test]
    public void LoadCharacterRom_ValidData_SetsRomLoaded()
    {
        var romData = new byte[VideoDevice.CharacterRomSize];

        device.LoadCharacterRom(romData);

        Assert.That(device.IsCharacterRomLoaded, Is.True);
    }

    /// <summary>
    /// Verifies that LoadCharacterRom throws for invalid size.
    /// </summary>
    [Test]
    public void LoadCharacterRom_InvalidSize_ThrowsArgumentException()
    {
        var romData = new byte[1024]; // Wrong size

        Assert.Throws<ArgumentException>(() => device.LoadCharacterRom(romData));
    }

    /// <summary>
    /// Verifies that LoadCharacterRom throws for zero-length data.
    /// </summary>
    [Test]
    public void LoadCharacterRom_EmptyData_ThrowsArgumentException()
    {
        var romData = Array.Empty<byte>();

        Assert.Throws<ArgumentException>(() => device.LoadCharacterRom(romData));
    }

    /// <summary>
    /// Verifies that GetCharacterScanline returns correct data from primary set.
    /// </summary>
    [Test]
    public void GetCharacterScanline_PrimarySet_ReturnsCorrectData()
    {
        var romData = new byte[VideoDevice.CharacterRomSize];

        // Set up test pattern at character 'A' (0xC1), scanline 0
        int offset = 0xC1 * 8;
        romData[offset] = 0x18; // Expected pattern

        device.LoadCharacterRom(romData);

        byte result = device.GetCharacterScanline(0xC1, 0, useAltCharSet: false);

        Assert.That(result, Is.EqualTo(0x18));
    }

    /// <summary>
    /// Verifies that GetCharacterScanline uses the alternate character set.
    /// </summary>
    [Test]
    public void GetCharacterScanline_AlternateSet_UsesSecondHalf()
    {
        var romData = new byte[VideoDevice.CharacterRomSize];

        // Set different patterns in primary and alternate sets
        int primaryOffset = 0x40 * 8;
        int altOffset = VideoDevice.CharacterSetSize + (0x40 * 8);
        romData[primaryOffset] = 0xAA;
        romData[altOffset] = 0x55;

        device.LoadCharacterRom(romData);

        byte primary = device.GetCharacterScanline(0x40, 0, useAltCharSet: false);
        byte alternate = device.GetCharacterScanline(0x40, 0, useAltCharSet: true);

        Assert.Multiple(() =>
        {
            Assert.That(primary, Is.EqualTo(0xAA));
            Assert.That(alternate, Is.EqualTo(0x55));
        });
    }

    /// <summary>
    /// Verifies that GetCharacterBitmap returns all 8 scanlines.
    /// </summary>
    [Test]
    public void GetCharacterBitmap_ReturnsAll8Scanlines()
    {
        var romData = new byte[VideoDevice.CharacterRomSize];

        // Fill character 0x00 with sequential values
        for (int i = 0; i < 8; i++)
        {
            romData[i] = (byte)i;
        }

        device.LoadCharacterRom(romData);

        var bitmap = device.GetCharacterBitmap(0x00, useAltCharSet: false);

        // Memory<T> can be used with Assert.Multiple via Span property
        Assert.That(bitmap.Length, Is.EqualTo(8));
        Assert.That(bitmap.Span[0], Is.EqualTo(0));
        Assert.That(bitmap.Span[7], Is.EqualTo(7));
    }

    /// <summary>
    /// Verifies that GetCharacterScanline returns zero when no ROM is loaded.
    /// </summary>
    [Test]
    public void GetCharacterScanline_NoRomLoaded_ReturnsZero()
    {
        byte result = device.GetCharacterScanline(0xC1, 0, useAltCharSet: false);

        Assert.That(result, Is.EqualTo(0x00));
    }

    /// <summary>
    /// Verifies that GetCharacterBitmap returns empty memory when no ROM is loaded.
    /// </summary>
    [Test]
    public void GetCharacterBitmap_NoRomLoaded_ReturnsEmptyMemory()
    {
        var result = device.GetCharacterBitmap(0xC1, useAltCharSet: false);

        Assert.That(result.IsEmpty, Is.True);
    }

    /// <summary>
    /// Verifies that GetCharacterScanline throws for invalid scanline.
    /// </summary>
    [Test]
    public void GetCharacterScanline_InvalidScanline_ThrowsArgumentOutOfRangeException()
    {
        var romData = new byte[VideoDevice.CharacterRomSize];
        device.LoadCharacterRom(romData);

        Assert.Throws<ArgumentOutOfRangeException>(() => device.GetCharacterScanline(0x00, 8, useAltCharSet: false));
    }

    /// <summary>
    /// Verifies that VideoDevice implements ICharacterRomProvider.
    /// </summary>
    [Test]
    public void VideoDevice_ImplementsICharacterRomProvider()
    {
        Assert.That(device, Is.InstanceOf<Interfaces.ICharacterRomProvider>());
    }

    /// <summary>
    /// Verifies that CharacterRomSize constant is 4096 bytes.
    /// </summary>
    [Test]
    public void CharacterRomSize_Is4096Bytes()
    {
        Assert.That(VideoDevice.CharacterRomSize, Is.EqualTo(4096));
    }

    /// <summary>
    /// Verifies that CharacterSetSize constant is 2048 bytes.
    /// </summary>
    [Test]
    public void CharacterSetSize_Is2048Bytes()
    {
        Assert.That(VideoDevice.CharacterSetSize, Is.EqualTo(2048));
    }

    private static BusAccess CreateTestContext()
    {
        return new(
            Address: 0xC050,
            Value: 0,
            WidthBits: 8,
            Mode: BusAccessMode.Decomposed,
            EmulationFlag: true,
            Intent: AccessIntent.DataRead,
            SourceId: 0,
            Cycle: 0,
            Flags: AccessFlags.None);
    }

    private static BusAccess CreateTestContextWithNoSideEffects()
    {
        return new(
            Address: 0xC050,
            Value: 0,
            WidthBits: 8,
            Mode: BusAccessMode.Decomposed,
            EmulationFlag: true,
            Intent: AccessIntent.DebugRead,
            SourceId: 0,
            Cycle: 0,
            Flags: AccessFlags.NoSideEffects);
    }
}