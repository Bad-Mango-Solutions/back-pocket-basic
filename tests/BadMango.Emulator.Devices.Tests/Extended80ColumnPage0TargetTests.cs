// <copyright file="Extended80ColumnPage0TargetTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Devices.Tests;

using BadMango.Emulator.Bus;
using BadMango.Emulator.Bus.Interfaces;

using Moq;

/// <summary>
/// Unit tests for the <see cref="Extended80ColumnPage0Target"/> class.
/// </summary>
[TestFixture]
public class Extended80ColumnPage0TargetTests
{
    /// <summary>
    /// Verifies that the target reads from main memory when all switches are off.
    /// </summary>
    [Test]
    public void Read8_AllSwitchesOff_ReadsFromMainMemory()
    {
        // Arrange
        var mainRam = new PhysicalMemory(0x1000, "MAIN");
        mainRam.AsSpan()[0x0500] = 0x41;

        var mainTarget = new RamTarget(mainRam.Slice(0, 0x1000), "MAIN");
        var device = new Extended80ColumnDevice();

        var target = new Extended80ColumnPage0Target(mainTarget, device);
        var access = CreateAccess(0x0500, AccessIntent.DataRead);

        // Act
        byte result = target.Read8(0x0500, access);

        // Assert
        Assert.That(result, Is.EqualTo(0x41));
    }

    /// <summary>
    /// Verifies that the target reads from auxiliary memory for text page when 80STORE+PAGE2 is set.
    /// </summary>
    [Test]
    public void Read8_80StoreAndPage2_ReadsFromAuxTextPage()
    {
        // Arrange
        var mainRam = new PhysicalMemory(0x1000, "MAIN");
        mainRam.AsSpan()[0x0500] = 0x41; // Main text page

        var mainTarget = new RamTarget(mainRam.Slice(0, 0x1000), "MAIN");
        var device = new Extended80ColumnDevice();

        // Write different value to aux text page
        device.WriteAuxRam(0x0500, 0x42);

        var target = new Extended80ColumnPage0Target(mainTarget, device);

        // Enable 80STORE and PAGE2 via internal state manipulation
        // We need to simulate the soft switch being triggered
        var dispatcher = new IOPageDispatcher();
        device.RegisterHandlers(dispatcher);

        // Trigger 80STORE on
        var storeOnAccess = CreateAccess(0xC001, AccessIntent.DataWrite);
        dispatcher.Write(0x01, 0, storeOnAccess);

        // Set PAGE2
        device.SetPage2(true);

        var readAccess = CreateAccess(0x0500, AccessIntent.DataRead);

        // Act
        byte result = target.Read8(0x0500, readAccess);

        // Assert
        Assert.That(result, Is.EqualTo(0x42), "Should read from auxiliary text page");
    }

    /// <summary>
    /// Verifies that writes go to auxiliary memory for text page when 80STORE+PAGE2 is set.
    /// </summary>
    [Test]
    public void Write8_80StoreAndPage2_WritesToAuxTextPage()
    {
        // Arrange
        var mainRam = new PhysicalMemory(0x1000, "MAIN");
        var mainTarget = new RamTarget(mainRam.Slice(0, 0x1000), "MAIN");
        var device = new Extended80ColumnDevice();

        var target = new Extended80ColumnPage0Target(mainTarget, device);

        // Enable 80STORE and PAGE2
        var dispatcher = new IOPageDispatcher();
        device.RegisterHandlers(dispatcher);
        var storeOnAccess = CreateAccess(0xC001, AccessIntent.DataWrite);
        dispatcher.Write(0x01, 0, storeOnAccess);
        device.SetPage2(true);

        var writeAccess = CreateAccess(0x0500, AccessIntent.DataWrite);

        // Act
        target.Write8(0x0500, 0x42, writeAccess);

        // Assert
        Assert.That(device.ReadAuxRam(0x0500), Is.EqualTo(0x42), "Should write to auxiliary text page");
        Assert.That(mainRam.AsSpan()[0x0500], Is.EqualTo(0x00), "Main memory should be unchanged");
    }

    /// <summary>
    /// Verifies that text page follows RAMRD when 80STORE is disabled.
    /// </summary>
    [Test]
    public void Read8_80StoreOff_TextPageFollowsRamRd()
    {
        // Arrange
        var mainRam = new PhysicalMemory(0x1000, "MAIN");
        mainRam.AsSpan()[0x0500] = 0x41;

        var mainTarget = new RamTarget(mainRam.Slice(0, 0x1000), "MAIN");
        var device = new Extended80ColumnDevice();
        device.WriteAuxRam(0x0500, 0x42);

        var target = new Extended80ColumnPage0Target(mainTarget, device);

        // Enable RAMRD (80STORE stays off by default)
        var dispatcher = new IOPageDispatcher();
        device.RegisterHandlers(dispatcher);
        var ramRdOnAccess = CreateAccess(0xC003, AccessIntent.DataWrite);
        dispatcher.Write(0x03, 0, ramRdOnAccess);

        var readAccess = CreateAccess(0x0500, AccessIntent.DataRead);

        // Act
        byte result = target.Read8(0x0500, readAccess);

        // Assert
        Assert.That(result, Is.EqualTo(0x42), "With 80STORE off, text page should follow RAMRD");
    }

    /// <summary>
    /// Verifies that zero page is controlled by ALTZP.
    /// </summary>
    [Test]
    public void Read8_AltZp_ZeroPageReadsFromAux()
    {
        // Arrange
        var mainRam = new PhysicalMemory(0x1000, "MAIN");
        mainRam.AsSpan()[0x0050] = 0x41;

        var mainTarget = new RamTarget(mainRam.Slice(0, 0x1000), "MAIN");
        var device = new Extended80ColumnDevice();
        device.WriteAuxRam(0x0050, 0x42);

        var target = new Extended80ColumnPage0Target(mainTarget, device);

        // Enable ALTZP
        var dispatcher = new IOPageDispatcher();
        device.RegisterHandlers(dispatcher);
        var altZpOnAccess = CreateAccess(0xC009, AccessIntent.DataWrite);
        dispatcher.Write(0x09, 0, altZpOnAccess);

        var readAccess = CreateAccess(0x0050, AccessIntent.DataRead);

        // Act
        byte result = target.Read8(0x0050, readAccess);

        // Assert
        Assert.That(result, Is.EqualTo(0x42), "Zero page should read from aux when ALTZP is on");
    }

    /// <summary>
    /// Verifies that stack is controlled by ALTZP.
    /// </summary>
    [Test]
    public void Read8_AltZp_StackReadsFromAux()
    {
        // Arrange
        var mainRam = new PhysicalMemory(0x1000, "MAIN");
        mainRam.AsSpan()[0x0150] = 0x41;

        var mainTarget = new RamTarget(mainRam.Slice(0, 0x1000), "MAIN");
        var device = new Extended80ColumnDevice();
        device.WriteAuxRam(0x0150, 0x42);

        var target = new Extended80ColumnPage0Target(mainTarget, device);

        // Enable ALTZP
        var dispatcher = new IOPageDispatcher();
        device.RegisterHandlers(dispatcher);
        var altZpOnAccess = CreateAccess(0xC009, AccessIntent.DataWrite);
        dispatcher.Write(0x09, 0, altZpOnAccess);

        var readAccess = CreateAccess(0x0150, AccessIntent.DataRead);

        // Act
        byte result = target.Read8(0x0150, readAccess);

        // Assert
        Assert.That(result, Is.EqualTo(0x42), "Stack should read from aux when ALTZP is on");
    }

    /// <summary>
    /// Verifies that toggling PAGE2 correctly switches text page access.
    /// </summary>
    [Test]
    public void ReadWrite_Page2Toggle_SwitchesTextPageAccess()
    {
        // Arrange
        var mainRam = new PhysicalMemory(0x1000, "MAIN");
        var mainTarget = new RamTarget(mainRam.Slice(0, 0x1000), "MAIN");
        var device = new Extended80ColumnDevice();

        var target = new Extended80ColumnPage0Target(mainTarget, device);

        var dispatcher = new IOPageDispatcher();
        device.RegisterHandlers(dispatcher);

        // Enable 80STORE
        var storeOnAccess = CreateAccess(0xC001, AccessIntent.DataWrite);
        dispatcher.Write(0x01, 0, storeOnAccess);

        var access = CreateAccess(0x0500, AccessIntent.DataWrite);

        // Write to main (PAGE2 off)
        target.Write8(0x0500, 0x41, access);
        Assert.That(mainRam.AsSpan()[0x0500], Is.EqualTo(0x41));

        // Switch to PAGE2
        device.SetPage2(true);

        // Write to aux
        target.Write8(0x0500, 0x42, access);
        Assert.That(device.ReadAuxRam(0x0500), Is.EqualTo(0x42));
        Assert.That(mainRam.AsSpan()[0x0500], Is.EqualTo(0x41), "Main memory should be unchanged");

        // Switch back to PAGE1
        device.SetPage2(false);

        // Verify main memory is visible again
        var readAccess = CreateAccess(0x0500, AccessIntent.DataRead);
        byte result = target.Read8(0x0500, readAccess);
        Assert.That(result, Is.EqualTo(0x41));
    }

    private static BusAccess CreateAccess(ushort address, AccessIntent intent)
    {
        return new BusAccess(
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
