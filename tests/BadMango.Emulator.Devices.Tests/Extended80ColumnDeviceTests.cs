// <copyright file="Extended80ColumnDeviceTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Devices.Tests;

using BadMango.Emulator.Bus;
using BadMango.Emulator.Bus.Interfaces;

/// <summary>
/// Unit tests for the <see cref="Extended80ColumnDevice"/> class.
/// </summary>
[TestFixture]
public class Extended80ColumnDeviceTests
{
    /// <summary>
    /// Verifies that the Extended 80-Column Card has correct initial state.
    /// </summary>
    [Test]
    public void Constructor_InitialState_IsCorrect()
    {
        var device = new Extended80ColumnDevice();

        Assert.Multiple(() =>
        {
            Assert.That(device.Is80StoreEnabled, Is.False, "80STORE should be disabled initially");
            Assert.That(device.IsRamRdEnabled, Is.False, "RAMRD should be disabled initially");
            Assert.That(device.IsRamWrtEnabled, Is.False, "RAMWRT should be disabled initially");
            Assert.That(device.IsIntCXRomEnabled, Is.False, "INTCXROM should be disabled initially");
            Assert.That(device.IsAltZpEnabled, Is.False, "ALTZP should be disabled initially");
            Assert.That(device.IsSlotC3RomEnabled, Is.False, "SLOTC3ROM should be disabled initially");
            Assert.That(device.Is80ColumnEnabled, Is.False, "80COL should be disabled initially");
            Assert.That(device.IsPage2Selected, Is.False, "PAGE2 should not be selected initially");
            Assert.That(device.IsHiResEnabled, Is.False, "HIRES should be disabled initially");
            Assert.That(device.TotalAuxRamSize, Is.EqualTo(64 * 1024u), "Total aux RAM should be 64KB");
        });
    }

    /// <summary>
    /// Verifies that the Extended 80-Column Card implements the expected interfaces.
    /// </summary>
    [Test]
    public void Device_ImplementsExpectedInterfaces()
    {
        var device = new Extended80ColumnDevice();

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
        var device = new Extended80ColumnDevice();

        Assert.Multiple(() =>
        {
            Assert.That(device.Name, Is.EqualTo("Extended 80-Column Card"));
            Assert.That(device.DeviceType, Is.EqualTo("Extended80Column"));
            Assert.That(device.Kind, Is.EqualTo(PeripheralKind.Motherboard));
            Assert.That(device.ProviderName, Is.EqualTo("Extended 80-Column Card"));
        });
    }

    /// <summary>
    /// Verifies that soft switch states are reported correctly.
    /// </summary>
    [Test]
    public void GetSoftSwitchStates_ReturnsExpectedStates()
    {
        var device = new Extended80ColumnDevice();

        var states = device.GetSoftSwitchStates();

        Assert.That(states, Has.Count.EqualTo(9));
        Assert.Multiple(() =>
        {
            Assert.That(states[0].Name, Is.EqualTo("80STORE"));
            Assert.That(states[0].Address, Is.EqualTo(0xC000));
            Assert.That(states[1].Name, Is.EqualTo("RAMRD"));
            Assert.That(states[1].Address, Is.EqualTo(0xC002));
            Assert.That(states[2].Name, Is.EqualTo("RAMWRT"));
            Assert.That(states[2].Address, Is.EqualTo(0xC004));
            Assert.That(states[3].Name, Is.EqualTo("INTCXROM"));
            Assert.That(states[3].Address, Is.EqualTo(0xC006));
            Assert.That(states[4].Name, Is.EqualTo("ALTZP"));
            Assert.That(states[4].Address, Is.EqualTo(0xC008));
            Assert.That(states[5].Name, Is.EqualTo("SLOTC3ROM"));
            Assert.That(states[5].Address, Is.EqualTo(0xC00A));
            Assert.That(states[6].Name, Is.EqualTo("80COL"));
            Assert.That(states[6].Address, Is.EqualTo(0xC00C));
            Assert.That(states[7].Name, Is.EqualTo("PAGE2"));
            Assert.That(states[7].Address, Is.EqualTo(0xC054));
            Assert.That(states[8].Name, Is.EqualTo("HIRES"));
            Assert.That(states[8].Address, Is.EqualTo(0xC056));
        });
    }

    /// <summary>
    /// Verifies that RegisterHandlers throws on null dispatcher.
    /// </summary>
    [Test]
    public void RegisterHandlers_NullDispatcher_ThrowsArgumentNullException()
    {
        var device = new Extended80ColumnDevice();

        Assert.Throws<ArgumentNullException>(() => device.RegisterHandlers(null!));
    }

    /// <summary>
    /// Verifies that soft switch $C001 enables 80STORE mode.
    /// </summary>
    [Test]
    public void SoftSwitch_C001_Enables80Store()
    {
        var device = new Extended80ColumnDevice();
        var dispatcher = new IOPageDispatcher();
        device.RegisterHandlers(dispatcher);

        var context = CreateTestContext();
        dispatcher.Write(0x01, 0x00, in context);

        Assert.That(device.Is80StoreEnabled, Is.True, "80STORE should be enabled");
    }

    /// <summary>
    /// Verifies that soft switch $C000 disables 80STORE mode.
    /// </summary>
    [Test]
    public void SoftSwitch_C000_Disables80Store()
    {
        var device = new Extended80ColumnDevice();
        var dispatcher = new IOPageDispatcher();
        device.RegisterHandlers(dispatcher);

        var context = CreateTestContext();
        dispatcher.Write(0x01, 0x00, in context); // Enable first
        dispatcher.Write(0x00, 0x00, in context); // Disable

        Assert.That(device.Is80StoreEnabled, Is.False, "80STORE should be disabled");
    }

    /// <summary>
    /// Verifies that soft switch $C003 enables RAMRD.
    /// </summary>
    [Test]
    public void SoftSwitch_C003_EnablesRamRd()
    {
        var device = new Extended80ColumnDevice();
        var dispatcher = new IOPageDispatcher();
        device.RegisterHandlers(dispatcher);

        var context = CreateTestContext();
        dispatcher.Write(0x03, 0x00, in context);

        Assert.That(device.IsRamRdEnabled, Is.True, "RAMRD should be enabled");
    }

    /// <summary>
    /// Verifies that soft switch $C002 disables RAMRD.
    /// </summary>
    [Test]
    public void SoftSwitch_C002_DisablesRamRd()
    {
        var device = new Extended80ColumnDevice();
        var dispatcher = new IOPageDispatcher();
        device.RegisterHandlers(dispatcher);

        var context = CreateTestContext();
        dispatcher.Write(0x03, 0x00, in context); // Enable first
        dispatcher.Write(0x02, 0x00, in context); // Disable

        Assert.That(device.IsRamRdEnabled, Is.False, "RAMRD should be disabled");
    }

    /// <summary>
    /// Verifies that soft switch $C005 enables RAMWRT.
    /// </summary>
    [Test]
    public void SoftSwitch_C005_EnablesRamWrt()
    {
        var device = new Extended80ColumnDevice();
        var dispatcher = new IOPageDispatcher();
        device.RegisterHandlers(dispatcher);

        var context = CreateTestContext();
        dispatcher.Write(0x05, 0x00, in context);

        Assert.That(device.IsRamWrtEnabled, Is.True, "RAMWRT should be enabled");
    }

    /// <summary>
    /// Verifies that soft switch $C007 enables INTCXROM.
    /// </summary>
    [Test]
    public void SoftSwitch_C007_EnablesIntCXRom()
    {
        var device = new Extended80ColumnDevice();
        var dispatcher = new IOPageDispatcher();
        device.RegisterHandlers(dispatcher);

        var context = CreateTestContext();
        dispatcher.Write(0x07, 0x00, in context);

        Assert.That(device.IsIntCXRomEnabled, Is.True, "INTCXROM should be enabled");
    }

    /// <summary>
    /// Verifies that soft switch $C009 enables ALTZP.
    /// </summary>
    [Test]
    public void SoftSwitch_C009_EnablesAltZp()
    {
        var device = new Extended80ColumnDevice();
        var dispatcher = new IOPageDispatcher();
        device.RegisterHandlers(dispatcher);

        var context = CreateTestContext();
        dispatcher.Write(0x09, 0x00, in context);

        Assert.That(device.IsAltZpEnabled, Is.True, "ALTZP should be enabled");
    }

    /// <summary>
    /// Verifies that soft switch $C00D enables 80-column mode.
    /// </summary>
    [Test]
    public void SoftSwitch_C00D_Enables80Col()
    {
        var device = new Extended80ColumnDevice();
        var dispatcher = new IOPageDispatcher();
        device.RegisterHandlers(dispatcher);

        var context = CreateTestContext();
        dispatcher.Write(0x0D, 0x00, in context);

        Assert.That(device.Is80ColumnEnabled, Is.True, "80COL should be enabled");
    }

    /// <summary>
    /// Verifies that Reset returns device to power-on state.
    /// </summary>
    [Test]
    public void Reset_ReturnsToPowerOnState()
    {
        var device = new Extended80ColumnDevice();
        var dispatcher = new IOPageDispatcher();
        device.RegisterHandlers(dispatcher);

        var context = CreateTestContext();
        dispatcher.Write(0x01, 0x00, in context); // Enable 80STORE
        dispatcher.Write(0x03, 0x00, in context); // Enable RAMRD
        dispatcher.Write(0x05, 0x00, in context); // Enable RAMWRT
        dispatcher.Write(0x07, 0x00, in context); // Enable INTCXROM
        dispatcher.Write(0x09, 0x00, in context); // Enable ALTZP
        dispatcher.Write(0x0D, 0x00, in context); // Enable 80COL

        device.Reset();

        Assert.Multiple(() =>
        {
            Assert.That(device.Is80StoreEnabled, Is.False, "80STORE should be disabled after reset");
            Assert.That(device.IsRamRdEnabled, Is.False, "RAMRD should be disabled after reset");
            Assert.That(device.IsRamWrtEnabled, Is.False, "RAMWRT should be disabled after reset");
            Assert.That(device.IsIntCXRomEnabled, Is.False, "INTCXROM should be disabled after reset");
            Assert.That(device.IsAltZpEnabled, Is.False, "ALTZP should be disabled after reset");
            Assert.That(device.Is80ColumnEnabled, Is.False, "80COL should be disabled after reset");
        });
    }

    /// <summary>
    /// Verifies that side-effect-free writes don't change state.
    /// </summary>
    [Test]
    public void SideEffectFreeWrite_DoesNotChangeState()
    {
        var device = new Extended80ColumnDevice();
        var dispatcher = new IOPageDispatcher();
        device.RegisterHandlers(dispatcher);

        var context = CreateTestContext(isSideEffectFree: true);
        dispatcher.Write(0x01, 0x00, in context);

        Assert.That(device.Is80StoreEnabled, Is.False, "State should not change with side-effect-free write");
    }

    /// <summary>
    /// Verifies that status read $C013 returns RAMRD state.
    /// </summary>
    [Test]
    public void StatusRead_C013_ReturnsRamRdState()
    {
        var device = new Extended80ColumnDevice();
        var dispatcher = new IOPageDispatcher();
        device.RegisterHandlers(dispatcher);

        var context = CreateTestContext();
        byte resultOff = dispatcher.Read(0x13, in context);
        Assert.That(resultOff & 0x80, Is.EqualTo(0x00), "RAMRD status should be off");

        dispatcher.Write(0x03, 0x00, in context); // Enable RAMRD
        byte resultOn = dispatcher.Read(0x13, in context);
        Assert.That(resultOn & 0x80, Is.EqualTo(0x80), "RAMRD status should be on");
    }

    /// <summary>
    /// Verifies that status read $C018 returns 80STORE state.
    /// </summary>
    [Test]
    public void StatusRead_C018_Returns80StoreState()
    {
        var device = new Extended80ColumnDevice();
        var dispatcher = new IOPageDispatcher();
        device.RegisterHandlers(dispatcher);

        var context = CreateTestContext();
        byte resultOff = dispatcher.Read(0x18, in context);
        Assert.That(resultOff & 0x80, Is.EqualTo(0x00), "80STORE status should be off");

        dispatcher.Write(0x01, 0x00, in context); // Enable 80STORE
        byte resultOn = dispatcher.Read(0x18, in context);
        Assert.That(resultOn & 0x80, Is.EqualTo(0x80), "80STORE status should be on");
    }

    /// <summary>
    /// Verifies auxiliary RAM read/write operations.
    /// </summary>
    [Test]
    public void AuxRam_ReadWrite_Works()
    {
        var device = new Extended80ColumnDevice();

        device.WriteAuxRam(0x0400, 0x41); // Write 'A' to text page
        byte result = device.ReadAuxRam(0x0400);

        Assert.That(result, Is.EqualTo(0x41), "Should read back written value");
    }

    /// <summary>
    /// Verifies that auxiliary RAM span is 64KB.
    /// </summary>
    [Test]
    public void AuxiliaryRam_Is64KB()
    {
        var device = new Extended80ColumnDevice();

        Assert.That(device.AuxiliaryRam.Length, Is.EqualTo(65536), "Auxiliary RAM should be 64KB");
    }

    /// <summary>
    /// Verifies that expansion ROM can be loaded.
    /// </summary>
    [Test]
    public void LoadExpansionRom_LoadsData()
    {
        var device = new Extended80ColumnDevice();
        var romData = new byte[] { 0xEA, 0xEA, 0x60 }; // NOP, NOP, RTS

        device.LoadExpansionRom(romData);

        // Verify the ROM data was loaded (access through the target)
        Assert.That(device.ExpansionRomTarget, Is.Not.Null);
    }

    /// <summary>
    /// Verifies SetPage2 method updates state.
    /// </summary>
    [Test]
    public void SetPage2_UpdatesState()
    {
        var device = new Extended80ColumnDevice();

        device.SetPage2(true);
        Assert.That(device.IsPage2Selected, Is.True);

        device.SetPage2(false);
        Assert.That(device.IsPage2Selected, Is.False);
    }

    /// <summary>
    /// Verifies SetHiRes method updates state.
    /// </summary>
    [Test]
    public void SetHiRes_UpdatesState()
    {
        var device = new Extended80ColumnDevice();

        device.SetHiRes(true);
        Assert.That(device.IsHiResEnabled, Is.True);

        device.SetHiRes(false);
        Assert.That(device.IsHiResEnabled, Is.False);
    }

    /// <summary>
    /// Verifies that GetAuxPage0Target returns a valid target.
    /// </summary>
    [Test]
    public void GetAuxPage0Target_ReturnsValidTarget()
    {
        var device = new Extended80ColumnDevice();

        var target = device.GetAuxPage0Target();

        Assert.That(target, Is.Not.Null);
        Assert.That(target.Name, Is.EqualTo("AUX_PAGE0"));
    }

    /// <summary>
    /// Verifies that SetPage0Target can be called and updates routing.
    /// </summary>
    [Test]
    public void SetPage0Target_UpdatesRoutingOnSwitchChange()
    {
        var device = new Extended80ColumnDevice();
        var dispatcher = new IOPageDispatcher();
        device.RegisterHandlers(dispatcher);

        // Create a mock page 0 target
        var mainRam = new PhysicalMemory(0x1000, "MAIN");
        var auxRam = new PhysicalMemory(0x1000, "AUX");
        var mainTarget = new RamTarget(mainRam.Slice(0, 0x1000), "MAIN");
        var auxTarget = new RamTarget(auxRam.Slice(0, 0x1000), "AUX");
        var page0Target = new Extended80ColumnPage0Target(mainTarget, auxTarget);

        // Wire up the device to the page 0 target
        device.SetPage0Target(page0Target);

        // Write a value to main memory zero page
        mainRam.AsSpan()[0x50] = 0xAA;

        // With ALTZP off, should read from main
        var readAccess = CreateBusAccess(0x50, AccessIntent.DataRead);
        byte resultMain = page0Target.Read8(0x50, in readAccess);
        Assert.That(resultMain, Is.EqualTo(0xAA), "Should read from main with ALTZP off");

        // Enable ALTZP via soft switch
        var context = CreateTestContext();
        dispatcher.Write(0x09, 0x00, in context);

        // Now should read from aux (which is 0x00 since uninitialized)
        byte resultAux = page0Target.Read8(0x50, in readAccess);
        Assert.That(resultAux, Is.EqualTo(0x00), "Should read from aux with ALTZP on");
    }

    private static BusAccess CreateTestContext(bool isSideEffectFree = false)
    {
        var flags = isSideEffectFree ? AccessFlags.NoSideEffects : AccessFlags.None;

        return new(
            Address: 0xC000,
            Value: 0,
            WidthBits: 8,
            Mode: BusAccessMode.Decomposed,
            EmulationFlag: true,
            Intent: AccessIntent.DataRead,
            SourceId: 0,
            Cycle: 0,
            Flags: flags);
    }

    private static BusAccess CreateBusAccess(ushort address, AccessIntent intent)
    {
        return new(
            Address: address,
            Value: 0,
            WidthBits: 8,
            Mode: BusAccessMode.Decomposed,
            EmulationFlag: true,
            Intent: intent,
            SourceId: 0,
            Cycle: 0,
            Flags: AccessFlags.None);
    }
}