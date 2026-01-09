// <copyright file="LanguageCardDeviceTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Devices.Tests;

using BadMango.Emulator.Bus;
using BadMango.Emulator.Bus.Interfaces;

/// <summary>
/// Unit tests for the <see cref="LanguageCardDevice"/> class.
/// </summary>
[TestFixture]
public class LanguageCardDeviceTests
{
    private const int PageSize = 4096;

    /// <summary>
    /// Verifies that the Language Card has correct initial state.
    /// </summary>
    [Test]
    public void Constructor_InitialState_IsCorrect()
    {
        var device = new LanguageCardDevice();

        Assert.Multiple(() =>
        {
            Assert.That(device.IsRamReadEnabled, Is.False, "RAM read should be disabled initially");
            Assert.That(device.IsRamWriteEnabled, Is.False, "RAM write should be disabled initially");
            Assert.That(device.SelectedBank, Is.EqualTo(1), "Bank 1 should be selected initially");
            Assert.That(device.TotalRamSize, Is.EqualTo(16 * 1024u), "Total RAM should be 16KB");
        });
    }

    /// <summary>
    /// Verifies that the Language Card implements the expected interfaces.
    /// </summary>
    [Test]
    public void Device_ImplementsExpectedInterfaces()
    {
        var device = new LanguageCardDevice();

        Assert.Multiple(() =>
        {
            Assert.That(device, Is.InstanceOf<IMotherboardDevice>());
            Assert.That(device, Is.InstanceOf<ISoftSwitchProvider>());
            Assert.That(device, Is.InstanceOf<IScheduledDevice>());
            Assert.That(device, Is.InstanceOf<IPeripheral>());
        });
    }

    /// <summary>
    /// Verifies peripheral properties are set correctly.
    /// </summary>
    [Test]
    public void PeripheralProperties_AreCorrect()
    {
        var device = new LanguageCardDevice();

        Assert.Multiple(() =>
        {
            Assert.That(device.Name, Is.EqualTo("Language Card"));
            Assert.That(device.DeviceType, Is.EqualTo("LanguageCard"));
            Assert.That(device.Kind, Is.EqualTo(PeripheralKind.Motherboard));
            Assert.That(device.ProviderName, Is.EqualTo("Language Card"));
        });
    }

    /// <summary>
    /// Verifies that soft switch states are reported correctly.
    /// </summary>
    [Test]
    public void GetSoftSwitchStates_ReturnsExpectedStates()
    {
        var device = new LanguageCardDevice();

        var states = device.GetSoftSwitchStates();

        // Should return all 8 Language Card soft switches from the truth table
        Assert.That(states, Has.Count.EqualTo(8));
        Assert.Multiple(() =>
        {
            // Bank 2 switches
            Assert.That(states[0].Name, Is.EqualTo("LCBANK2RD"));
            Assert.That(states[0].Address, Is.EqualTo(0xC080));
            Assert.That(states[0].Description, Is.EqualTo("Bank 2 RAM read, write-protected"));
            Assert.That(states[1].Name, Is.EqualTo("LCBANK2WR"));
            Assert.That(states[1].Address, Is.EqualTo(0xC081));
            Assert.That(states[1].Description, Is.EqualTo("ROM read, Bank 2 RAM write (R�2)"));
            Assert.That(states[2].Name, Is.EqualTo("LCBANK2ROM"));
            Assert.That(states[2].Address, Is.EqualTo(0xC082));
            Assert.That(states[2].Description, Is.EqualTo("ROM read, RAM write-protected"));
            Assert.That(states[3].Name, Is.EqualTo("LCBANK2RW"));
            Assert.That(states[3].Address, Is.EqualTo(0xC083));
            Assert.That(states[3].Description, Is.EqualTo("Bank 2 RAM read/write (R�2)"));

            // Bank 1 switches
            Assert.That(states[4].Name, Is.EqualTo("LCBANK1RD"));
            Assert.That(states[4].Address, Is.EqualTo(0xC088));
            Assert.That(states[4].Description, Is.EqualTo("Bank 1 RAM read, write-protected"));
            Assert.That(states[5].Name, Is.EqualTo("LCBANK1WR"));
            Assert.That(states[5].Address, Is.EqualTo(0xC089));
            Assert.That(states[5].Description, Is.EqualTo("ROM read, Bank 1 RAM write (R�2)"));
            Assert.That(states[6].Name, Is.EqualTo("LCBANK1ROM"));
            Assert.That(states[6].Address, Is.EqualTo(0xC08A));
            Assert.That(states[6].Description, Is.EqualTo("ROM read, RAM write-protected"));
            Assert.That(states[7].Name, Is.EqualTo("LCBANK1RW"));
            Assert.That(states[7].Address, Is.EqualTo(0xC08B));
            Assert.That(states[7].Description, Is.EqualTo("Bank 1 RAM read/write (R�2)"));
        });
    }

    /// <summary>
    /// Verifies that initial state shows LCBANK1ROM ($C08A) as active.
    /// </summary>
    /// <remarks>
    /// On power-up/reset: Bank 1 selected, RAM read disabled, RAM write disabled.
    /// This matches the $C08A state in the truth table.
    /// </remarks>
    [Test]
    public void GetSoftSwitchStates_InitialState_ShowsBank1RomActive()
    {
        var device = new LanguageCardDevice();

        var states = device.GetSoftSwitchStates();

        Assert.Multiple(() =>
        {
            // Only LCBANK1ROM ($C08A) should be active in initial state
            Assert.That(states[0].Value, Is.False, "LCBANK2RD should not be active");
            Assert.That(states[1].Value, Is.False, "LCBANK2WR should not be active");
            Assert.That(states[2].Value, Is.False, "LCBANK2ROM should not be active");
            Assert.That(states[3].Value, Is.False, "LCBANK2RW should not be active");
            Assert.That(states[4].Value, Is.False, "LCBANK1RD should not be active");
            Assert.That(states[5].Value, Is.False, "LCBANK1WR should not be active");
            Assert.That(states[6].Value, Is.True, "LCBANK1ROM should be active (Bank 1, ROM, no write)");
            Assert.That(states[7].Value, Is.False, "LCBANK1RW should not be active");
        });
    }

    /// <summary>
    /// Verifies soft switch state after reading $C080 (Bank 2 RAM read, write-protected).
    /// </summary>
    [Test]
    public void GetSoftSwitchStates_AfterC080_ShowsBank2RdActive()
    {
        var device = new LanguageCardDevice();
        var dispatcher = new IOPageDispatcher();
        device.RegisterHandlers(dispatcher);

        var context = CreateTestContext();
        dispatcher.Read(0x80, in context);

        var states = device.GetSoftSwitchStates();

        Assert.Multiple(() =>
        {
            Assert.That(states[0].Value, Is.True, "LCBANK2RD should be active");
            Assert.That(states[1].Value, Is.False, "LCBANK2WR should not be active");
            Assert.That(states[2].Value, Is.False, "LCBANK2ROM should not be active");
            Assert.That(states[3].Value, Is.False, "LCBANK2RW should not be active");
            Assert.That(states[4].Value, Is.False, "LCBANK1RD should not be active");
            Assert.That(states[5].Value, Is.False, "LCBANK1WR should not be active");
            Assert.That(states[6].Value, Is.False, "LCBANK1ROM should not be active");
            Assert.That(states[7].Value, Is.False, "LCBANK1RW should not be active");
        });
    }

    /// <summary>
    /// Verifies soft switch state after reading $C083 twice (Bank 2 RAM read/write).
    /// </summary>
    [Test]
    public void GetSoftSwitchStates_AfterC083Twice_ShowsBank2RwActive()
    {
        var device = new LanguageCardDevice();
        var dispatcher = new IOPageDispatcher();
        device.RegisterHandlers(dispatcher);

        var context = CreateTestContext();
        dispatcher.Read(0x83, in context);
        dispatcher.Read(0x83, in context); // R�2 to enable write

        var states = device.GetSoftSwitchStates();

        Assert.Multiple(() =>
        {
            Assert.That(states[0].Value, Is.False, "LCBANK2RD should not be active");
            Assert.That(states[1].Value, Is.False, "LCBANK2WR should not be active");
            Assert.That(states[2].Value, Is.False, "LCBANK2ROM should not be active");
            Assert.That(states[3].Value, Is.True, "LCBANK2RW should be active");
            Assert.That(states[4].Value, Is.False, "LCBANK1RD should not be active");
            Assert.That(states[5].Value, Is.False, "LCBANK1WR should not be active");
            Assert.That(states[6].Value, Is.False, "LCBANK1ROM should not be active");
            Assert.That(states[7].Value, Is.False, "LCBANK1RW should not be active");
        });
    }

    /// <summary>
    /// Verifies that I/O handlers are registered at slot 0.
    /// </summary>
    [Test]
    public void RegisterHandlers_RegistersAtSlot0()
    {
        var device = new LanguageCardDevice();
        var dispatcher = new IOPageDispatcher();

        device.RegisterHandlers(dispatcher);

        // Verify by reading from slot 0's offset ($80)
        var context = CreateTestContext(isSideEffectFree: true);
        var result = dispatcher.Read(0x80, in context);

        // Should return floating bus value (0xFF) rather than default/error
        Assert.That(result, Is.EqualTo(0xFF));
    }

    /// <summary>
    /// Verifies that RegisterHandlers throws on null dispatcher.
    /// </summary>
    [Test]
    public void RegisterHandlers_NullDispatcher_ThrowsArgumentNullException()
    {
        var device = new LanguageCardDevice();

        Assert.Throws<ArgumentNullException>(() => device.RegisterHandlers(null!));
    }

    /// <summary>
    /// Verifies that soft switch $C080 enables RAM read with Bank 2.
    /// </summary>
    [Test]
    public void SoftSwitch_C080_EnablesRamReadBank2()
    {
        var device = new LanguageCardDevice();
        var dispatcher = new IOPageDispatcher();
        device.RegisterHandlers(dispatcher);

        // Read $C080 (offset 0 from $C080)
        var context = CreateTestContext();
        dispatcher.Read(0x80, in context);

        Assert.Multiple(() =>
        {
            Assert.That(device.IsRamReadEnabled, Is.True, "RAM read should be enabled");
            Assert.That(device.SelectedBank, Is.EqualTo(2), "Bank 2 should be selected");
            Assert.That(device.IsRamWriteEnabled, Is.False, "Write should remain disabled");
        });
    }

    /// <summary>
    /// Verifies that soft switch $C081 disables RAM read.
    /// </summary>
    [Test]
    public void SoftSwitch_C081_DisablesRamRead()
    {
        var device = new LanguageCardDevice();
        var dispatcher = new IOPageDispatcher();
        device.RegisterHandlers(dispatcher);

        // First enable RAM read
        var context = CreateTestContext();
        dispatcher.Read(0x80, in context);

        // Then read $C081 (offset 1)
        dispatcher.Read(0x81, in context);

        Assert.Multiple(() =>
        {
            Assert.That(device.IsRamReadEnabled, Is.False, "RAM read should be disabled");
            Assert.That(device.SelectedBank, Is.EqualTo(2), "Bank 2 should still be selected");
        });
    }

    /// <summary>
    /// Verifies that soft switch $C088 enables RAM read with Bank 1.
    /// </summary>
    [Test]
    public void SoftSwitch_C088_EnablesRamReadBank1()
    {
        var device = new LanguageCardDevice();
        var dispatcher = new IOPageDispatcher();
        device.RegisterHandlers(dispatcher);

        // Read $C088 (offset 8 from $C080)
        var context = CreateTestContext();
        dispatcher.Read(0x88, in context);

        Assert.Multiple(() =>
        {
            Assert.That(device.IsRamReadEnabled, Is.True, "RAM read should be enabled");
            Assert.That(device.SelectedBank, Is.EqualTo(1), "Bank 1 should be selected");
        });
    }

    /// <summary>
    /// Verifies that R�2 protocol requires two consecutive reads of same odd address.
    /// </summary>
    [Test]
    public void RxProtocol_TwoReadsOfSameOddAddress_EnablesWrite()
    {
        var device = new LanguageCardDevice();
        var dispatcher = new IOPageDispatcher();
        device.RegisterHandlers(dispatcher);

        var context = CreateTestContext();

        // First read of $C083 (offset 3)
        dispatcher.Read(0x83, in context);
        Assert.That(device.IsRamWriteEnabled, Is.False, "Write should not be enabled after first read");

        // Second consecutive read of same address
        dispatcher.Read(0x83, in context);
        Assert.Multiple(() =>
        {
            Assert.That(device.IsRamWriteEnabled, Is.True, "Write should be enabled after second read");
            Assert.That(device.IsRamReadEnabled, Is.True, "RAM read should also be enabled");
        });
    }

    /// <summary>
    /// Verifies that R�2 protocol resets when reading a different address.
    /// </summary>
    [Test]
    public void RxProtocol_DifferentOddAddress_DoesNotEnableWrite()
    {
        var device = new LanguageCardDevice();
        var dispatcher = new IOPageDispatcher();
        device.RegisterHandlers(dispatcher);

        var context = CreateTestContext();

        // First read of $C083
        dispatcher.Read(0x83, in context);

        // Read different odd address $C081
        dispatcher.Read(0x81, in context);
        Assert.That(device.IsRamWriteEnabled, Is.False, "Write should not be enabled");
    }

    /// <summary>
    /// Verifies that reading an even address clears R�2 state and disables write.
    /// </summary>
    [Test]
    public void EvenAddressRead_ClearsRxAndDisablesWrite()
    {
        var device = new LanguageCardDevice();
        var dispatcher = new IOPageDispatcher();
        device.RegisterHandlers(dispatcher);

        var context = CreateTestContext();

        // Enable write via R�2
        dispatcher.Read(0x83, in context);
        dispatcher.Read(0x83, in context);
        Assert.That(device.IsRamWriteEnabled, Is.True, "Write should be enabled");

        // Read even address $C080
        dispatcher.Read(0x80, in context);
        Assert.That(device.IsRamWriteEnabled, Is.False, "Write should be disabled after even address read");
    }

    /// <summary>
    /// Verifies that writes to soft switches have no effect on state.
    /// </summary>
    /// <remarks>
    /// Only reads trigger Language Card switch effects. Writes are ignored.
    /// This is a common emulator misconception.
    /// </remarks>
    [Test]
    public void Write_HasNoEffect()
    {
        var device = new LanguageCardDevice();
        var dispatcher = new IOPageDispatcher();
        device.RegisterHandlers(dispatcher);

        var context = CreateTestContext();

        // Prime the R�2 protocol with first read
        dispatcher.Read(0x83, in context);

        // Write to any switch - should have NO effect
        dispatcher.Write(0x80, 0x00, in context);

        // Second read should still enable write (R�2 state was NOT cleared by write)
        dispatcher.Read(0x83, in context);

        Assert.That(device.IsRamWriteEnabled, Is.True, "R�2 state should not be affected by writes");
    }

    /// <summary>
    /// Verifies that side-effect-free reads don't change state.
    /// </summary>
    [Test]
    public void SideEffectFreeRead_DoesNotChangeState()
    {
        var device = new LanguageCardDevice();
        var dispatcher = new IOPageDispatcher();
        device.RegisterHandlers(dispatcher);

        // Create side-effect-free context (used by debugger)
        var context = CreateTestContext(isSideEffectFree: true);

        // Read $C080 with side effects disabled
        dispatcher.Read(0x80, in context);

        Assert.That(device.IsRamReadEnabled, Is.False, "State should not change with side-effect-free read");
    }

    /// <summary>
    /// Verifies that Reset returns device to power-on state.
    /// </summary>
    [Test]
    public void Reset_ReturnsToPowerOnState()
    {
        var device = new LanguageCardDevice();
        var dispatcher = new IOPageDispatcher();
        device.RegisterHandlers(dispatcher);

        var context = CreateTestContext();

        // Change state
        dispatcher.Read(0x83, in context);
        dispatcher.Read(0x83, in context);
        dispatcher.Read(0x88, in context);
        dispatcher.Read(0x8B, in context);
        dispatcher.Read(0x8B, in context);

        // Reset
        device.Reset();

        Assert.Multiple(() =>
        {
            Assert.That(device.IsRamReadEnabled, Is.False, "RAM read should be disabled after reset");
            Assert.That(device.IsRamWriteEnabled, Is.False, "RAM write should be disabled after reset");
            Assert.That(device.SelectedBank, Is.EqualTo(1), "Bank 1 should be selected after reset");
        });
    }

    /// <summary>
    /// Verifies that RAM targets are created and accessible.
    /// </summary>
    [Test]
    public void RamTargets_AreCreated()
    {
        var device = new LanguageCardDevice();

        Assert.Multiple(() =>
        {
            Assert.That(device.Bank1Target, Is.Not.Null, "Bank1Target should be created");
            Assert.That(device.Bank2Target, Is.Not.Null, "Bank2Target should be created");
            Assert.That(device.HighTarget, Is.Not.Null, "HighTarget should be created");
        });
    }

    /// <summary>
    /// Verifies that I/O handlers span all 16 soft switch addresses.
    /// </summary>
    [Test]
    public void IOHandlers_CoverAll16Addresses()
    {
        var device = new LanguageCardDevice();
        var dispatcher = new IOPageDispatcher();
        device.RegisterHandlers(dispatcher);

        var context = CreateTestContext(isSideEffectFree: true);

        // Verify all 16 offsets (0x80-0x8F) are handled
        for (byte offset = 0x80; offset <= 0x8F; offset++)
        {
            var result = dispatcher.Read(offset, in context);
            Assert.That(result, Is.EqualTo(0xFF), $"Offset ${offset:X2} should return floating bus");
        }
    }

    /// <summary>
    /// Verifies soft switch bit 3 bank selection logic.
    /// </summary>
    /// <param name="offset">The soft switch offset.</param>
    /// <param name="expectedBank">The expected bank selection.</param>
    [TestCase(0x00, 2)] // $C080: bit 3 = 0 -> Bank 2
    [TestCase(0x01, 2)] // $C081: bit 3 = 0 -> Bank 2
    [TestCase(0x07, 2)] // $C087: bit 3 = 0 -> Bank 2
    [TestCase(0x08, 1)] // $C088: bit 3 = 1 -> Bank 1
    [TestCase(0x09, 1)] // $C089: bit 3 = 1 -> Bank 1
    [TestCase(0x0F, 1)] // $C08F: bit 3 = 1 -> Bank 1
    public void BankSelection_BasedOnBit3(byte offset, int expectedBank)
    {
        var device = new LanguageCardDevice();
        var dispatcher = new IOPageDispatcher();
        device.RegisterHandlers(dispatcher);

        var context = CreateTestContext();
        dispatcher.Read((byte)(0x80 + offset), in context);

        Assert.That(device.SelectedBank, Is.EqualTo(expectedBank));
    }

    /// <summary>
    /// Verifies RAM read enable based on bits 0 and 1.
    /// </summary>
    /// <param name="offset">The soft switch offset.</param>
    /// <param name="expectedRamRead">Whether RAM read should be enabled.</param>
    [TestCase(0x00, true)] // $C080: bits 0,1 = 00 -> RAM
    [TestCase(0x01, false)] // $C081: bits 0,1 = 01 -> ROM
    [TestCase(0x02, false)] // $C082: bits 0,1 = 10 -> ROM
    [TestCase(0x03, true)] // $C083: bits 0,1 = 11 -> RAM
    [TestCase(0x08, true)] // $C088: bits 0,1 = 00 -> RAM
    [TestCase(0x09, false)] // $C089: bits 0,1 = 01 -> ROM
    [TestCase(0x0A, false)] // $C08A: bits 0,1 = 10 -> ROM
    [TestCase(0x0B, true)] // $C08B: bits 0,1 = 11 -> RAM
    public void RamReadEnable_BasedOnBits0And1(byte offset, bool expectedRamRead)
    {
        var device = new LanguageCardDevice();
        var dispatcher = new IOPageDispatcher();
        device.RegisterHandlers(dispatcher);

        var context = CreateTestContext();
        dispatcher.Read((byte)(0x80 + offset), in context);

        Assert.That(device.IsRamReadEnabled, Is.EqualTo(expectedRamRead));
    }

    private static BusAccess CreateTestContext(bool isSideEffectFree = false)
    {
        var flags = isSideEffectFree ? AccessFlags.NoSideEffects : AccessFlags.None;

        return new(
            Address: 0xC080,
            Value: 0,
            WidthBits: 8,
            Mode: BusAccessMode.Decomposed,
            EmulationFlag: true,
            Intent: AccessIntent.DataRead,
            SourceId: 0,
            Cycle: 0,
            Flags: flags);
    }
}