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
    /// Verifies initial state of AltCharSet.
    /// </summary>
    [Test]
    public void IsAltCharSet_InitiallyFalse()
    {
        Assert.That(device.IsAltCharSet, Is.False);
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
    /// Verifies that writing $C00F (SETALTCHAR) enables AltCharSet.
    /// </summary>
    [Test]
    public void WriteC00F_EnablesAltCharSet()
    {
        var context = CreateTestContext();

        dispatcher.Write(0x0F, 0x00, in context);

        Assert.That(device.IsAltCharSet, Is.True);
    }

    /// <summary>
    /// Verifies that writing $C00E (CLRALTCHAR) disables AltCharSet.
    /// </summary>
    [Test]
    public void WriteC00E_DisablesAltCharSet()
    {
        var context = CreateTestContext();

        // First enable
        dispatcher.Write(0x0F, 0x00, in context);
        Assert.That(device.IsAltCharSet, Is.True);

        // Then disable
        dispatcher.Write(0x0E, 0x00, in context);
        Assert.That(device.IsAltCharSet, Is.False);
    }

    /// <summary>
    /// Verifies that writing $C04B (SETALTGLYPH1) enables AltGlyph1.
    /// </summary>
    [Test]
    public void WriteC04B_EnablesAltGlyph1()
    {
        var context = CreateTestContext();

        dispatcher.Write(0x4B, 0x00, in context);

        Assert.That(device.IsAltGlyph1Enabled, Is.True);
    }

    /// <summary>
    /// Verifies that writing $C04A (CLRALTGLYPH1) disables AltGlyph1.
    /// </summary>
    [Test]
    public void WriteC04A_DisablesAltGlyph1()
    {
        var context = CreateTestContext();

        // First enable
        dispatcher.Write(0x4B, 0x00, in context);
        Assert.That(device.IsAltGlyph1Enabled, Is.True);

        // Then disable
        dispatcher.Write(0x4A, 0x00, in context);
        Assert.That(device.IsAltGlyph1Enabled, Is.False);
    }

    /// <summary>
    /// Verifies that writing $C04D (SETALTGLYPH2) enables AltGlyph2.
    /// </summary>
    [Test]
    public void WriteC04D_EnablesAltGlyph2()
    {
        var context = CreateTestContext();

        dispatcher.Write(0x4D, 0x00, in context);

        Assert.That(device.IsAltGlyph2Enabled, Is.True);
    }

    /// <summary>
    /// Verifies that writing $C04C (CLRALTGLYPH2) disables AltGlyph2.
    /// </summary>
    [Test]
    public void WriteC04C_DisablesAltGlyph2()
    {
        var context = CreateTestContext();

        // First enable
        dispatcher.Write(0x4D, 0x00, in context);
        Assert.That(device.IsAltGlyph2Enabled, Is.True);

        // Then disable
        dispatcher.Write(0x4C, 0x00, in context);
        Assert.That(device.IsAltGlyph2Enabled, Is.False);
    }

    /// <summary>
    /// Verifies that writing $C047 (SETNOFLASH1) enables NoFlash1.
    /// </summary>
    [Test]
    public void WriteC047_EnablesNoFlash1()
    {
        var context = CreateTestContext();

        dispatcher.Write(0x47, 0x00, in context);

        Assert.That(device.IsNoFlash1Enabled, Is.True);
    }

    /// <summary>
    /// Verifies that writing $C046 (CLRNOFLASH1) disables NoFlash1.
    /// </summary>
    [Test]
    public void WriteC046_DisablesNoFlash1()
    {
        var context = CreateTestContext();

        // First enable
        dispatcher.Write(0x47, 0x00, in context);
        Assert.That(device.IsNoFlash1Enabled, Is.True);

        // Then disable
        dispatcher.Write(0x46, 0x00, in context);
        Assert.That(device.IsNoFlash1Enabled, Is.False);
    }

    /// <summary>
    /// Verifies that writing $C049 (SETNOFLASH2) enables NoFlash2.
    /// </summary>
    [Test]
    public void WriteC049_EnablesNoFlash2()
    {
        var context = CreateTestContext();

        // First disable (it defaults to true)
        dispatcher.Write(0x48, 0x00, in context);
        Assert.That(device.IsNoFlash2Enabled, Is.False);

        // Then enable
        dispatcher.Write(0x49, 0x00, in context);
        Assert.That(device.IsNoFlash2Enabled, Is.True);
    }

    /// <summary>
    /// Verifies that writing $C048 (CLRNOFLASH2) disables NoFlash2.
    /// </summary>
    [Test]
    public void WriteC048_DisablesNoFlash2()
    {
        var context = CreateTestContext();

        dispatcher.Write(0x48, 0x00, in context);

        Assert.That(device.IsNoFlash2Enabled, Is.False);
    }

    /// <summary>
    /// Verifies that writing $C043 (SETGLYPHRD) enables GlyphRead.
    /// </summary>
    [Test]
    public void WriteC043_EnablesGlyphRead()
    {
        var context = CreateTestContext();

        dispatcher.Write(0x43, 0x00, in context);

        Assert.That(device.IsGlyphReadEnabled, Is.True);
    }

    /// <summary>
    /// Verifies that writing $C042 (CLRGLYPHRD) disables GlyphRead.
    /// </summary>
    [Test]
    public void WriteC042_DisablesGlyphRead()
    {
        var context = CreateTestContext();

        // First enable
        dispatcher.Write(0x43, 0x00, in context);
        Assert.That(device.IsGlyphReadEnabled, Is.True);

        // Then disable
        dispatcher.Write(0x42, 0x00, in context);
        Assert.That(device.IsGlyphReadEnabled, Is.False);
    }

    /// <summary>
    /// Verifies that writing $C045 (SETGLYPHWRT) enables GlyphWrite.
    /// </summary>
    [Test]
    public void WriteC045_EnablesGlyphWrite()
    {
        var context = CreateTestContext();

        dispatcher.Write(0x45, 0x00, in context);

        Assert.That(device.IsGlyphWriteEnabled, Is.True);
    }

    /// <summary>
    /// Verifies that writing $C044 (CLRGLYPHWRT) disables GlyphWrite.
    /// </summary>
    [Test]
    public void WriteC044_DisablesGlyphWrite()
    {
        var context = CreateTestContext();

        // First enable
        dispatcher.Write(0x45, 0x00, in context);
        Assert.That(device.IsGlyphWriteEnabled, Is.True);

        // Then disable
        dispatcher.Write(0x44, 0x00, in context);
        Assert.That(device.IsGlyphWriteEnabled, Is.False);
    }

    /// <summary>
    /// Verifies that reading $C01E returns AltCharSet status.
    /// </summary>
    [Test]
    public void ReadC01E_ReturnsAltCharSetStatus()
    {
        var context = CreateTestContext();

        // Initially off - bit 7 should be clear
        byte statusOff = dispatcher.Read(0x1E, in context);
        Assert.That(statusOff & 0x80, Is.EqualTo(0x00), "Bit 7 should be clear when AltCharSet off");

        // Enable it
        dispatcher.Write(0x0F, 0x00, in context);
        byte statusOn = dispatcher.Read(0x1E, in context);
        Assert.That(statusOn & 0x80, Is.EqualTo(0x80), "Bit 7 should be set when AltCharSet on");
    }

    /// <summary>
    /// Verifies that reading $C038 returns AltGlyph1 status.
    /// </summary>
    [Test]
    public void ReadC038_ReturnsAltGlyph1Status()
    {
        var context = CreateTestContext();

        // Initially off - bit 7 should be clear
        byte statusOff = dispatcher.Read(0x38, in context);
        Assert.That(statusOff & 0x80, Is.EqualTo(0x00), "Bit 7 should be clear when AltGlyph1 off");

        // Enable it
        dispatcher.Write(0x4B, 0x00, in context);
        byte statusOn = dispatcher.Read(0x38, in context);
        Assert.That(statusOn & 0x80, Is.EqualTo(0x80), "Bit 7 should be set when AltGlyph1 on");
    }

    /// <summary>
    /// Verifies that reading $C039 returns AltGlyph2 status.
    /// </summary>
    [Test]
    public void ReadC039_ReturnsAltGlyph2Status()
    {
        var context = CreateTestContext();

        // Initially off - bit 7 should be clear
        byte statusOff = dispatcher.Read(0x39, in context);
        Assert.That(statusOff & 0x80, Is.EqualTo(0x00), "Bit 7 should be clear when AltGlyph2 off");

        // Enable it
        dispatcher.Write(0x4D, 0x00, in context);
        byte statusOn = dispatcher.Read(0x39, in context);
        Assert.That(statusOn & 0x80, Is.EqualTo(0x80), "Bit 7 should be set when AltGlyph2 on");
    }

    /// <summary>
    /// Verifies that reading $C036 returns NoFlash1 status.
    /// </summary>
    [Test]
    public void ReadC036_ReturnsNoFlash1Status()
    {
        var context = CreateTestContext();

        // Initially off - bit 7 should be clear
        byte statusOff = dispatcher.Read(0x36, in context);
        Assert.That(statusOff & 0x80, Is.EqualTo(0x00), "Bit 7 should be clear when NoFlash1 off");

        // Enable it
        dispatcher.Write(0x47, 0x00, in context);
        byte statusOn = dispatcher.Read(0x36, in context);
        Assert.That(statusOn & 0x80, Is.EqualTo(0x80), "Bit 7 should be set when NoFlash1 on");
    }

    /// <summary>
    /// Verifies that reading $C037 returns NoFlash2 status (defaults to on).
    /// </summary>
    [Test]
    public void ReadC037_ReturnsNoFlash2Status()
    {
        var context = CreateTestContext();

        // Initially on (bank 2 defaults to NOFLASH) - bit 7 should be set
        byte statusOn = dispatcher.Read(0x37, in context);
        Assert.That(statusOn & 0x80, Is.EqualTo(0x80), "Bit 7 should be set when NoFlash2 on");

        // Disable it
        dispatcher.Write(0x48, 0x00, in context);
        byte statusOff = dispatcher.Read(0x37, in context);
        Assert.That(statusOff & 0x80, Is.EqualTo(0x00), "Bit 7 should be clear when NoFlash2 off");
    }

    /// <summary>
    /// Verifies that Reset restores default state.
    /// </summary>
    [Test]
    public void Reset_RestoresDefaultState()
    {
        var context = CreateTestContext();

        // Change state using writes
        dispatcher.Write(0x0F, 0x00, in context); // AltCharSet on
        dispatcher.Write(0x4B, 0x00, in context); // AltGlyph1 on
        dispatcher.Write(0x4D, 0x00, in context); // AltGlyph2 on
        dispatcher.Write(0x47, 0x00, in context); // NoFlash1 on
        dispatcher.Write(0x48, 0x00, in context); // NoFlash2 off
        dispatcher.Write(0x43, 0x00, in context); // GlyphRead on
        dispatcher.Write(0x45, 0x00, in context); // GlyphWrite on

        device.Reset();

        Assert.Multiple(() =>
        {
            Assert.That(device.IsAltCharSet, Is.False);
            Assert.That(device.IsAltGlyph1Enabled, Is.False);
            Assert.That(device.IsAltGlyph2Enabled, Is.False);
            Assert.That(device.IsNoFlash1Enabled, Is.False);
            Assert.That(device.IsNoFlash2Enabled, Is.True); // Defaults to true
            Assert.That(device.IsGlyphReadEnabled, Is.False);
            Assert.That(device.IsGlyphWriteEnabled, Is.False);
        });
    }

    /// <summary>
    /// Verifies that side-effect-free writes don't change state.
    /// </summary>
    [Test]
    public void Write_WithNoSideEffects_DoesNotChangeState()
    {
        var context = CreateTestContextWithNoSideEffects();

        dispatcher.Write(0x4B, 0x00, in context); // Try to enable AltGlyph1

        Assert.That(device.IsAltGlyph1Enabled, Is.False); // Should remain false
    }

    /// <summary>
    /// Verifies that reads to toggle switch addresses do NOT affect state (write-only).
    /// </summary>
    [Test]
    public void Read_ToggleSwitchAddresses_DoNotChangeState()
    {
        var context = CreateTestContext();

        // Reading toggle switches should NOT change state (they're write-only now)
        _ = dispatcher.Read(0x4B, in context); // Read SETALTGLYPH1 address

        Assert.That(device.IsAltGlyph1Enabled, Is.False); // Should remain false
    }

    /// <summary>
    /// Verifies that GetSoftSwitchStates returns expected switches with correct addresses.
    /// </summary>
    [Test]
    public void GetSoftSwitchStates_ReturnsExpectedSwitches()
    {
        var states = device.GetSoftSwitchStates();

        Assert.Multiple(() =>
        {
            // Check for ALTCHAR switches
            Assert.That(states.Any(s => s.Name == "CLRALTCHAR" && s.Address == 0xC00E), Is.True);
            Assert.That(states.Any(s => s.Name == "SETALTCHAR" && s.Address == 0xC00F), Is.True);

            // Check for glyph control switches at new addresses ($C04A-$C04D)
            Assert.That(states.Any(s => s.Name == "CLRALTGLYPH1" && s.Address == 0xC04A), Is.True);
            Assert.That(states.Any(s => s.Name == "SETALTGLYPH1" && s.Address == 0xC04B), Is.True);
            Assert.That(states.Any(s => s.Name == "CLRALTGLYPH2" && s.Address == 0xC04C), Is.True);
            Assert.That(states.Any(s => s.Name == "SETALTGLYPH2" && s.Address == 0xC04D), Is.True);

            // Check for flash control switches at new addresses ($C046-$C049)
            Assert.That(states.Any(s => s.Name == "CLRNOFLASH1" && s.Address == 0xC046), Is.True);
            Assert.That(states.Any(s => s.Name == "SETNOFLASH1" && s.Address == 0xC047), Is.True);
            Assert.That(states.Any(s => s.Name == "CLRNOFLASH2" && s.Address == 0xC048), Is.True);
            Assert.That(states.Any(s => s.Name == "SETNOFLASH2" && s.Address == 0xC049), Is.True);

            // Check for glyph read/write switches at new addresses ($C042-$C045)
            Assert.That(states.Any(s => s.Name == "CLRGLYPHRD" && s.Address == 0xC042), Is.True);
            Assert.That(states.Any(s => s.Name == "SETGLYPHRD" && s.Address == 0xC043), Is.True);
            Assert.That(states.Any(s => s.Name == "CLRGLYPHWRT" && s.Address == 0xC044), Is.True);
            Assert.That(states.Any(s => s.Name == "SETGLYPHWRT" && s.Address == 0xC045), Is.True);

            // Check for status reads at new addresses ($C034-$C039)
            Assert.That(states.Any(s => s.Name == "RDGLYPHRD" && s.Address == 0xC034), Is.True);
            Assert.That(states.Any(s => s.Name == "RDGLYPHWRT" && s.Address == 0xC035), Is.True);
            Assert.That(states.Any(s => s.Name == "RDNOFLASH1" && s.Address == 0xC036), Is.True);
            Assert.That(states.Any(s => s.Name == "RDNOFLASH2" && s.Address == 0xC037), Is.True);
            Assert.That(states.Any(s => s.Name == "RDALTGLYPH1" && s.Address == 0xC038), Is.True);
            Assert.That(states.Any(s => s.Name == "RDALTGLYPH2" && s.Address == 0xC039), Is.True);
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
    /// Verifies that GlyphRamSize constant is 4096 bytes.
    /// </summary>
    [Test]
    public void GlyphRamSize_Is4096Bytes()
    {
        Assert.That(CharacterDevice.GlyphRamSize, Is.EqualTo(4096));
    }

    /// <summary>
    /// Verifies that glyph RAM overlay works correctly for bank 1.
    /// </summary>
    [Test]
    public void GetCharacterScanline_WithAltGlyph1Enabled_UsesGlyphRam()
    {
        var romData = new byte[CharacterDevice.CharacterRomSize];
        romData[0x40 * 8] = 0xAA; // Pattern in ROM

        device.LoadCharacterRom(romData);

        // Write different pattern to glyph RAM
        var glyphData = new byte[] { 0x55 };
        device.WriteGlyphRam(0x40 * 8, glyphData);

        // Enable glyph bank 1 overlay
        var context = CreateTestContext();
        dispatcher.Write(0x4B, 0x00, in context);

        // Should now read from glyph RAM
        byte result = device.GetCharacterScanline(0x40, 0, useAltCharSet: false);
        Assert.That(result, Is.EqualTo(0x55));
    }

    /// <summary>
    /// Verifies that glyph RAM overlay works correctly for bank 2.
    /// </summary>
    [Test]
    public void GetCharacterScanline_WithAltGlyph2Enabled_UsesGlyphRam()
    {
        var romData = new byte[CharacterDevice.CharacterRomSize];
        int altOffset = CharacterDevice.CharacterSetSize + (0x40 * 8);
        romData[altOffset] = 0xAA; // Pattern in ROM (alternate set)

        device.LoadCharacterRom(romData);

        // Write different pattern to glyph RAM (bank 2 = upper half)
        var glyphData = new byte[] { 0x55 };
        device.WriteGlyphRam(altOffset, glyphData);

        // Enable glyph bank 2 overlay
        var context = CreateTestContext();
        dispatcher.Write(0x4D, 0x00, in context);

        // Should now read from glyph RAM
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
        dispatcher.Write(0x47, 0x00, in context);

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

    /// <summary>
    /// Verifies that OnVBlank triggers CharacterRomChanged event when pending.
    /// </summary>
    [Test]
    public void OnVBlank_WithPendingChange_TriggersEvent()
    {
        var eventRaised = false;
        device.CharacterRomChanged += () => eventRaised = true;

        // Trigger a change (this sets pending flag)
        var context = CreateTestContext();
        dispatcher.Write(0x0F, 0x00, in context); // SETALTCHAR

        // Process pending changes at VBLANK
        device.OnVBlank();

        Assert.That(eventRaised, Is.True);
    }

    private static BusAccess CreateTestContext()
    {
        return new(
            Address: 0xC040,
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
            Address: 0xC040,
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