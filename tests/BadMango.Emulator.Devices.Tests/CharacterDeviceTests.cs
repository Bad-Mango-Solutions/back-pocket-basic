// <copyright file="CharacterDeviceTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Devices.Tests;

using BadMango.Emulator.Bus;
using BadMango.Emulator.Bus.Interfaces;

using Moq;

/// <summary>
/// Unit tests for the <see cref="CharacterDevice"/> class.
/// </summary>
[TestFixture]
public class CharacterDeviceTests
{
    private CharacterDevice device = null!;
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
    public void Name_ReturnsCharacterGenerator()
    {
        Assert.That(device.Name, Is.EqualTo("Character Generator"));
    }

    /// <summary>
    /// Verifies that DeviceType returns the correct value.
    /// </summary>
    [Test]
    public void DeviceType_ReturnsCharacter()
    {
        Assert.That(device.DeviceType, Is.EqualTo("Character"));
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
    /// Verifies that ProviderName returns Character.
    /// </summary>
    [Test]
    public void ProviderName_ReturnsCharacter()
    {
        Assert.That(device.ProviderName, Is.EqualTo("Character"));
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
        var romData = new byte[CharacterDevice.CharacterRomSize];

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
    /// Verifies that LoadCharacterRom throws for null data.
    /// </summary>
    [Test]
    public void LoadCharacterRom_NullData_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => device.LoadCharacterRom(null!));
    }

    /// <summary>
    /// Verifies that GetCharacterScanline returns correct data from primary set.
    /// </summary>
    [Test]
    public void GetCharacterScanline_PrimarySet_ReturnsCorrectData()
    {
        var romData = new byte[CharacterDevice.CharacterRomSize];

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
        var romData = new byte[CharacterDevice.CharacterRomSize];

        // Set different patterns in primary and alternate sets
        int primaryOffset = 0x40 * 8;
        int altOffset = CharacterDevice.CharacterSetSize + (0x40 * 8);
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
        var romData = new byte[CharacterDevice.CharacterRomSize];

        // Fill character 0x00 with sequential values
        for (int i = 0; i < 8; i++)
        {
            romData[i] = (byte)i;
        }

        device.LoadCharacterRom(romData);

        var bitmap = device.GetCharacterBitmap(0x00, useAltCharSet: false);

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
    /// Verifies that GetCharacterScanline throws for invalid scanline.
    /// </summary>
    [Test]
    public void GetCharacterScanline_InvalidScanline_ThrowsArgumentOutOfRangeException()
    {
        var romData = new byte[CharacterDevice.CharacterRomSize];
        device.LoadCharacterRom(romData);

        Assert.Throws<ArgumentOutOfRangeException>(() => device.GetCharacterScanline(0x00, 8, useAltCharSet: false));
    }

    /// <summary>
    /// Verifies that CharacterDevice implements ICharacterDevice.
    /// </summary>
    [Test]
    public void CharacterDevice_ImplementsICharacterDevice()
    {
        Assert.That(device, Is.InstanceOf<Interfaces.ICharacterDevice>());
    }

    /// <summary>
    /// Verifies that CharacterDevice implements ISoftSwitchProvider.
    /// </summary>
    [Test]
    public void CharacterDevice_ImplementsISoftSwitchProvider()
    {
        Assert.That(device, Is.InstanceOf<ISoftSwitchProvider>());
    }

    /// <summary>
    /// Verifies initial state of AltGlyph1.
    /// </summary>
    [Test]
    public void IsAltGlyph1Enabled_InitiallyFalse()
    {
        Assert.That(device.IsAltGlyph1Enabled, Is.False);
    }

    /// <summary>
    /// Verifies initial state of AltGlyph2.
    /// </summary>
    [Test]
    public void IsAltGlyph2Enabled_InitiallyFalse()
    {
        Assert.That(device.IsAltGlyph2Enabled, Is.False);
    }

    /// <summary>
    /// Verifies initial state of NoFlash1.
    /// </summary>
    [Test]
    public void IsNoFlash1Enabled_InitiallyFalse()
    {
        Assert.That(device.IsNoFlash1Enabled, Is.False);
    }

    /// <summary>
    /// Verifies that NoFlash2 defaults to true (bank 2 defaults to NOFLASH).
    /// </summary>
    [Test]
    public void IsNoFlash2Enabled_DefaultsToTrue()
    {
        Assert.That(device.IsNoFlash2Enabled, Is.True);
    }

    /// <summary>
    /// Verifies initial state of GlyphRead.
    /// </summary>
    [Test]
    public void IsGlyphReadEnabled_InitiallyFalse()
    {
        Assert.That(device.IsGlyphReadEnabled, Is.False);
    }

    /// <summary>
    /// Verifies initial state of GlyphWrite.
    /// </summary>
    [Test]
    public void IsGlyphWriteEnabled_InitiallyFalse()
    {
        Assert.That(device.IsGlyphWriteEnabled, Is.False);
    }

    /// <summary>
    /// Verifies that reading $C069 enables AltGlyph1.
    /// </summary>
    [Test]
    public void ReadC069_EnablesAltGlyph1()
    {
        var context = CreateTestContext();

        _ = dispatcher.Read(0x69, in context);

        Assert.That(device.IsAltGlyph1Enabled, Is.True);
    }

    /// <summary>
    /// Verifies that reading $C068 disables AltGlyph1.
    /// </summary>
    [Test]
    public void ReadC068_DisablesAltGlyph1()
    {
        var context = CreateTestContext();

        // First enable
        _ = dispatcher.Read(0x69, in context);
        Assert.That(device.IsAltGlyph1Enabled, Is.True);

        // Then disable
        _ = dispatcher.Read(0x68, in context);
        Assert.That(device.IsAltGlyph1Enabled, Is.False);
    }

    /// <summary>
    /// Verifies that reading $C06B enables AltGlyph2.
    /// </summary>
    [Test]
    public void ReadC06B_EnablesAltGlyph2()
    {
        var context = CreateTestContext();

        _ = dispatcher.Read(0x6B, in context);

        Assert.That(device.IsAltGlyph2Enabled, Is.True);
    }

    /// <summary>
    /// Verifies that reading $C06A disables AltGlyph2.
    /// </summary>
    [Test]
    public void ReadC06A_DisablesAltGlyph2()
    {
        var context = CreateTestContext();

        // First enable
        _ = dispatcher.Read(0x6B, in context);
        Assert.That(device.IsAltGlyph2Enabled, Is.True);

        // Then disable
        _ = dispatcher.Read(0x6A, in context);
        Assert.That(device.IsAltGlyph2Enabled, Is.False);
    }

    /// <summary>
    /// Verifies that reading $C065 enables NoFlash1.
    /// </summary>
    [Test]
    public void ReadC065_EnablesNoFlash1()
    {
        var context = CreateTestContext();

        _ = dispatcher.Read(0x65, in context);

        Assert.That(device.IsNoFlash1Enabled, Is.True);
    }

    /// <summary>
    /// Verifies that reading $C064 disables NoFlash1.
    /// </summary>
    [Test]
    public void ReadC064_DisablesNoFlash1()
    {
        var context = CreateTestContext();

        // First enable
        _ = dispatcher.Read(0x65, in context);
        Assert.That(device.IsNoFlash1Enabled, Is.True);

        // Then disable
        _ = dispatcher.Read(0x64, in context);
        Assert.That(device.IsNoFlash1Enabled, Is.False);
    }

    /// <summary>
    /// Verifies that reading $C067 enables NoFlash2.
    /// </summary>
    [Test]
    public void ReadC067_EnablesNoFlash2()
    {
        var context = CreateTestContext();

        // First disable (it defaults to true)
        _ = dispatcher.Read(0x66, in context);
        Assert.That(device.IsNoFlash2Enabled, Is.False);

        // Then enable
        _ = dispatcher.Read(0x67, in context);
        Assert.That(device.IsNoFlash2Enabled, Is.True);
    }

    /// <summary>
    /// Verifies that reading $C066 disables NoFlash2.
    /// </summary>
    [Test]
    public void ReadC066_DisablesNoFlash2()
    {
        var context = CreateTestContext();

        _ = dispatcher.Read(0x66, in context);

        Assert.That(device.IsNoFlash2Enabled, Is.False);
    }

    /// <summary>
    /// Verifies that reading $C061 enables GlyphRead.
    /// </summary>
    [Test]
    public void ReadC061_EnablesGlyphRead()
    {
        var context = CreateTestContext();

        _ = dispatcher.Read(0x61, in context);

        Assert.That(device.IsGlyphReadEnabled, Is.True);
    }

    /// <summary>
    /// Verifies that reading $C060 disables GlyphRead.
    /// </summary>
    [Test]
    public void ReadC060_DisablesGlyphRead()
    {
        var context = CreateTestContext();

        // First enable
        _ = dispatcher.Read(0x61, in context);
        Assert.That(device.IsGlyphReadEnabled, Is.True);

        // Then disable
        _ = dispatcher.Read(0x60, in context);
        Assert.That(device.IsGlyphReadEnabled, Is.False);
    }

    /// <summary>
    /// Verifies that reading $C063 enables GlyphWrite.
    /// </summary>
    [Test]
    public void ReadC063_EnablesGlyphWrite()
    {
        var context = CreateTestContext();

        _ = dispatcher.Read(0x63, in context);

        Assert.That(device.IsGlyphWriteEnabled, Is.True);
    }

    /// <summary>
    /// Verifies that reading $C062 disables GlyphWrite.
    /// </summary>
    [Test]
    public void ReadC062_DisablesGlyphWrite()
    {
        var context = CreateTestContext();

        // First enable
        _ = dispatcher.Read(0x63, in context);
        Assert.That(device.IsGlyphWriteEnabled, Is.True);

        // Then disable
        _ = dispatcher.Read(0x62, in context);
        Assert.That(device.IsGlyphWriteEnabled, Is.False);
    }

    /// <summary>
    /// Verifies that reading $C024 returns AltGlyph1 status.
    /// </summary>
    [Test]
    public void ReadC024_ReturnsAltGlyph1Status()
    {
        var context = CreateTestContext();

        // Initially off - bit 7 should be clear
        byte statusOff = dispatcher.Read(0x24, in context);
        Assert.That(statusOff & 0x80, Is.EqualTo(0x00), "Bit 7 should be clear when AltGlyph1 off");

        // Enable it
        _ = dispatcher.Read(0x69, in context);
        byte statusOn = dispatcher.Read(0x24, in context);
        Assert.That(statusOn & 0x80, Is.EqualTo(0x80), "Bit 7 should be set when AltGlyph1 on");
    }

    /// <summary>
    /// Verifies that reading $C025 returns AltGlyph2 status.
    /// </summary>
    [Test]
    public void ReadC025_ReturnsAltGlyph2Status()
    {
        var context = CreateTestContext();

        // Initially off - bit 7 should be clear
        byte statusOff = dispatcher.Read(0x25, in context);
        Assert.That(statusOff & 0x80, Is.EqualTo(0x00), "Bit 7 should be clear when AltGlyph2 off");

        // Enable it
        _ = dispatcher.Read(0x6B, in context);
        byte statusOn = dispatcher.Read(0x25, in context);
        Assert.That(statusOn & 0x80, Is.EqualTo(0x80), "Bit 7 should be set when AltGlyph2 on");
    }

    /// <summary>
    /// Verifies that reading $C026 returns NoFlash1 status.
    /// </summary>
    [Test]
    public void ReadC026_ReturnsNoFlash1Status()
    {
        var context = CreateTestContext();

        // Initially off - bit 7 should be clear
        byte statusOff = dispatcher.Read(0x26, in context);
        Assert.That(statusOff & 0x80, Is.EqualTo(0x00), "Bit 7 should be clear when NoFlash1 off");

        // Enable it
        _ = dispatcher.Read(0x65, in context);
        byte statusOn = dispatcher.Read(0x26, in context);
        Assert.That(statusOn & 0x80, Is.EqualTo(0x80), "Bit 7 should be set when NoFlash1 on");
    }

    /// <summary>
    /// Verifies that reading $C027 returns NoFlash2 status (defaults to on).
    /// </summary>
    [Test]
    public void ReadC027_ReturnsNoFlash2Status()
    {
        var context = CreateTestContext();

        // Initially on (bank 2 defaults to NOFLASH) - bit 7 should be set
        byte statusOn = dispatcher.Read(0x27, in context);
        Assert.That(statusOn & 0x80, Is.EqualTo(0x80), "Bit 7 should be set when NoFlash2 on");

        // Disable it
        _ = dispatcher.Read(0x66, in context);
        byte statusOff = dispatcher.Read(0x27, in context);
        Assert.That(statusOff & 0x80, Is.EqualTo(0x00), "Bit 7 should be clear when NoFlash2 off");
    }

    /// <summary>
    /// Verifies that Reset restores default state.
    /// </summary>
    [Test]
    public void Reset_RestoresDefaultState()
    {
        var context = CreateTestContext();

        // Change state
        _ = dispatcher.Read(0x69, in context); // AltGlyph1 on
        _ = dispatcher.Read(0x6B, in context); // AltGlyph2 on
        _ = dispatcher.Read(0x65, in context); // NoFlash1 on
        _ = dispatcher.Read(0x66, in context); // NoFlash2 off
        _ = dispatcher.Read(0x61, in context); // GlyphRead on
        _ = dispatcher.Read(0x63, in context); // GlyphWrite on

        device.Reset();

        Assert.Multiple(() =>
        {
            Assert.That(device.IsAltGlyph1Enabled, Is.False);
            Assert.That(device.IsAltGlyph2Enabled, Is.False);
            Assert.That(device.IsNoFlash1Enabled, Is.False);
            Assert.That(device.IsNoFlash2Enabled, Is.True); // Defaults to true
            Assert.That(device.IsGlyphReadEnabled, Is.False);
            Assert.That(device.IsGlyphWriteEnabled, Is.False);
        });
    }

    /// <summary>
    /// Verifies that side-effect-free reads don't change state.
    /// </summary>
    [Test]
    public void Read_WithNoSideEffects_DoesNotChangeState()
    {
        var context = CreateTestContextWithNoSideEffects();

        _ = dispatcher.Read(0x69, in context); // Try to enable AltGlyph1

        Assert.That(device.IsAltGlyph1Enabled, Is.False); // Should remain false
    }

    /// <summary>
    /// Verifies that writes also affect soft switch state.
    /// </summary>
    [Test]
    public void WriteC069_AlsoEnablesAltGlyph1()
    {
        var context = CreateTestContext();

        dispatcher.Write(0x69, 0x00, in context);

        Assert.That(device.IsAltGlyph1Enabled, Is.True);
    }

    /// <summary>
    /// Verifies that GetSoftSwitchStates returns expected switches.
    /// </summary>
    [Test]
    public void GetSoftSwitchStates_ReturnsExpectedSwitches()
    {
        var states = device.GetSoftSwitchStates();

        Assert.Multiple(() =>
        {
            // Check for glyph control switches
            Assert.That(states.Any(s => s.Name == "ALTGLYPH1OFF" && s.Address == 0xC068), Is.True);
            Assert.That(states.Any(s => s.Name == "ALTGLYPH1ON" && s.Address == 0xC069), Is.True);
            Assert.That(states.Any(s => s.Name == "ALTGLYPH2OFF" && s.Address == 0xC06A), Is.True);
            Assert.That(states.Any(s => s.Name == "ALTGLYPH2ON" && s.Address == 0xC06B), Is.True);

            // Check for flash control switches
            Assert.That(states.Any(s => s.Name == "NOFLASH1OFF" && s.Address == 0xC064), Is.True);
            Assert.That(states.Any(s => s.Name == "NOFLASH1ON" && s.Address == 0xC065), Is.True);
            Assert.That(states.Any(s => s.Name == "NOFLASH2OFF" && s.Address == 0xC066), Is.True);
            Assert.That(states.Any(s => s.Name == "NOFLASH2ON" && s.Address == 0xC067), Is.True);

            // Check for status reads
            Assert.That(states.Any(s => s.Name == "RDALTGLYPH1" && s.Address == 0xC024), Is.True);
            Assert.That(states.Any(s => s.Name == "RDALTGLYPH2" && s.Address == 0xC025), Is.True);
        });
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
    /// Verifies that CharacterRomSize constant is 4096 bytes.
    /// </summary>
    [Test]
    public void CharacterRomSize_Is4096Bytes()
    {
        Assert.That(CharacterDevice.CharacterRomSize, Is.EqualTo(4096));
    }

    /// <summary>
    /// Verifies that CharacterSetSize constant is 2048 bytes.
    /// </summary>
    [Test]
    public void CharacterSetSize_Is2048Bytes()
    {
        Assert.That(CharacterDevice.CharacterSetSize, Is.EqualTo(2048));
    }

    /// <summary>
    /// Verifies that GlyphBankSize constant is 4096 bytes.
    /// </summary>
    [Test]
    public void GlyphBankSize_Is4096Bytes()
    {
        Assert.That(CharacterDevice.GlyphBankSize, Is.EqualTo(4096));
    }

    /// <summary>
    /// Verifies that glyph bank 1 overlay works correctly.
    /// </summary>
    [Test]
    public void GetCharacterScanline_WithAltGlyph1Enabled_UsesGlyphBank1()
    {
        var romData = new byte[CharacterDevice.CharacterRomSize];
        romData[0x40 * 8] = 0xAA; // Pattern in ROM

        device.LoadCharacterRom(romData);

        // Write different pattern to glyph bank 1
        var glyphData = new byte[] { 0x55 };
        device.WriteGlyphBank1(0x40 * 8, glyphData);

        // Enable glyph bank 1 overlay
        var context = CreateTestContext();
        _ = dispatcher.Read(0x69, in context);

        // Should now read from glyph bank 1
        byte result = device.GetCharacterScanline(0x40, 0, useAltCharSet: false);
        Assert.That(result, Is.EqualTo(0x55));
    }

    /// <summary>
    /// Verifies that glyph bank 2 overlay works correctly.
    /// </summary>
    [Test]
    public void GetCharacterScanline_WithAltGlyph2Enabled_UsesGlyphBank2()
    {
        var romData = new byte[CharacterDevice.CharacterRomSize];
        int altOffset = CharacterDevice.CharacterSetSize + (0x40 * 8);
        romData[altOffset] = 0xAA; // Pattern in ROM (alternate set)

        device.LoadCharacterRom(romData);

        // Write different pattern to glyph bank 2
        var glyphData = new byte[] { 0x55 };
        device.WriteGlyphBank2(0x40 * 8, glyphData);

        // Enable glyph bank 2 overlay
        var context = CreateTestContext();
        _ = dispatcher.Read(0x6B, in context);

        // Should now read from glyph bank 2
        byte result = device.GetCharacterScanline(0x40, 0, useAltCharSet: true);
        Assert.That(result, Is.EqualTo(0x55));
    }

    /// <summary>
    /// Verifies GetCharacterScanlineWithEffects applies flash effect correctly.
    /// </summary>
    [Test]
    public void GetCharacterScanlineWithEffects_FlashingCharacter_InvertsWhenFlashStateTrue()
    {
        var romData = new byte[CharacterDevice.CharacterRomSize];
        romData[0x40 * 8] = 0x55; // Pattern for flashing character $40

        device.LoadCharacterRom(romData);

        // Flashing characters are $40-$7F
        // When flash state is true and NoFlash1 is false, should invert
        byte normalResult = device.GetCharacterScanlineWithEffects(0x40, 0, false, flashState: false);
        byte flashedResult = device.GetCharacterScanlineWithEffects(0x40, 0, false, flashState: true);

        Assert.That(normalResult, Is.EqualTo(0x55));
        Assert.That(flashedResult, Is.EqualTo(0x2A)); // ~0x55 & 0x7F = 0x2A
    }

    /// <summary>
    /// Verifies GetCharacterScanlineWithEffects respects NoFlash setting.
    /// </summary>
    [Test]
    public void GetCharacterScanlineWithEffects_WithNoFlash1Enabled_DoesNotInvert()
    {
        var romData = new byte[CharacterDevice.CharacterRomSize];
        romData[0x40 * 8] = 0x55;

        device.LoadCharacterRom(romData);

        // Enable NoFlash1
        var context = CreateTestContext();
        _ = dispatcher.Read(0x65, in context);

        // Should not invert even with flash state true
        byte result = device.GetCharacterScanlineWithEffects(0x40, 0, false, flashState: true);
        Assert.That(result, Is.EqualTo(0x55));
    }

    /// <summary>
    /// Verifies GetScanlineRow generates correct output.
    /// </summary>
    [Test]
    public void GetScanlineRow_GeneratesCorrectOutput()
    {
        var romData = new byte[CharacterDevice.CharacterRomSize];
        romData[0x41 * 8] = 0x18; // 'A' pattern
        romData[0x42 * 8] = 0x24; // 'B' pattern

        device.LoadCharacterRom(romData);

        var characterCodes = new byte[] { 0x41, 0x42 };
        var outputBuffer = new byte[2];

        device.GetScanlineRow(characterCodes, 0, false, false, outputBuffer);

        Assert.That(outputBuffer[0], Is.EqualTo(0x18));
        Assert.That(outputBuffer[1], Is.EqualTo(0x24));
    }

    /// <summary>
    /// Verifies that DefaultCharacterRom can be loaded into CharacterDevice.
    /// </summary>
    [Test]
    public void DefaultCharacterRom_LoadIntoCharacterDevice_LoadsSuccessfully()
    {
        DefaultCharacterRom.LoadIntoCharacterDevice(device);

        Assert.That(device.IsCharacterRomLoaded, Is.True);
    }

    private static BusAccess CreateTestContext()
    {
        return new(
            Address: 0xC060,
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
            Address: 0xC060,
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