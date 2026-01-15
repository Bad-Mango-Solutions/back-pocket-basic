// <copyright file="Extended80ColumnPage0TargetTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Devices.Tests;

using BadMango.Emulator.Bus;
using BadMango.Emulator.Bus.Interfaces;

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
        var auxRam = new PhysicalMemory(0x1000, "AUX");
        mainRam.AsSpan()[0x0500] = 0x41;
        auxRam.AsSpan()[0x0500] = 0x42;

        var mainTarget = new RamTarget(mainRam.Slice(0, 0x1000), "MAIN");
        var auxTarget = new RamTarget(auxRam.Slice(0, 0x1000), "AUX");

        var target = new Extended80ColumnPage0Target(mainTarget, auxTarget);

        // Initialize routing with all switches off
        target.UpdateRouting(altZp: false, store80: false, page2: false, ramRd: false, ramWrt: false);

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
        var auxRam = new PhysicalMemory(0x1000, "AUX");
        mainRam.AsSpan()[0x0500] = 0x41;
        auxRam.AsSpan()[0x0500] = 0x42;

        var mainTarget = new RamTarget(mainRam.Slice(0, 0x1000), "MAIN");
        var auxTarget = new RamTarget(auxRam.Slice(0, 0x1000), "AUX");

        var target = new Extended80ColumnPage0Target(mainTarget, auxTarget);

        // Update routing with 80STORE and PAGE2 enabled
        target.UpdateRouting(altZp: false, store80: true, page2: true, ramRd: false, ramWrt: false);

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
        var auxRam = new PhysicalMemory(0x1000, "AUX");

        var mainTarget = new RamTarget(mainRam.Slice(0, 0x1000), "MAIN");
        var auxTarget = new RamTarget(auxRam.Slice(0, 0x1000), "AUX");

        var target = new Extended80ColumnPage0Target(mainTarget, auxTarget);

        // Update routing with 80STORE and PAGE2 enabled
        target.UpdateRouting(altZp: false, store80: true, page2: true, ramRd: false, ramWrt: false);

        var writeAccess = CreateAccess(0x0500, AccessIntent.DataWrite);

        // Act
        target.Write8(0x0500, 0x42, writeAccess);

        // Assert
        Assert.That(auxRam.AsSpan()[0x0500], Is.EqualTo(0x42), "Should write to auxiliary text page");
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
        var auxRam = new PhysicalMemory(0x1000, "AUX");
        mainRam.AsSpan()[0x0500] = 0x41;
        auxRam.AsSpan()[0x0500] = 0x42;

        var mainTarget = new RamTarget(mainRam.Slice(0, 0x1000), "MAIN");
        var auxTarget = new RamTarget(auxRam.Slice(0, 0x1000), "AUX");

        var target = new Extended80ColumnPage0Target(mainTarget, auxTarget);

        // Update routing with RAMRD enabled (80STORE off)
        target.UpdateRouting(altZp: false, store80: false, page2: false, ramRd: true, ramWrt: false);

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
        var auxRam = new PhysicalMemory(0x1000, "AUX");
        mainRam.AsSpan()[0x0050] = 0x41;
        auxRam.AsSpan()[0x0050] = 0x42;

        var mainTarget = new RamTarget(mainRam.Slice(0, 0x1000), "MAIN");
        var auxTarget = new RamTarget(auxRam.Slice(0, 0x1000), "AUX");

        var target = new Extended80ColumnPage0Target(mainTarget, auxTarget);

        // Update routing with ALTZP enabled
        target.UpdateRouting(altZp: true, store80: false, page2: false, ramRd: false, ramWrt: false);

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
        var auxRam = new PhysicalMemory(0x1000, "AUX");
        mainRam.AsSpan()[0x0150] = 0x41;
        auxRam.AsSpan()[0x0150] = 0x42;

        var mainTarget = new RamTarget(mainRam.Slice(0, 0x1000), "MAIN");
        var auxTarget = new RamTarget(auxRam.Slice(0, 0x1000), "AUX");

        var target = new Extended80ColumnPage0Target(mainTarget, auxTarget);

        // Update routing with ALTZP enabled
        target.UpdateRouting(altZp: true, store80: false, page2: false, ramRd: false, ramWrt: false);

        var readAccess = CreateAccess(0x0150, AccessIntent.DataRead);

        // Act
        byte result = target.Read8(0x0150, readAccess);

        // Assert
        Assert.That(result, Is.EqualTo(0x42), "Stack should read from aux when ALTZP is on");
    }

    /// <summary>
    /// Verifies that toggling PAGE2 correctly switches text page access via UpdateRouting.
    /// </summary>
    [Test]
    public void ReadWrite_Page2Toggle_SwitchesTextPageAccess()
    {
        // Arrange
        var mainRam = new PhysicalMemory(0x1000, "MAIN");
        var auxRam = new PhysicalMemory(0x1000, "AUX");

        var mainTarget = new RamTarget(mainRam.Slice(0, 0x1000), "MAIN");
        var auxTarget = new RamTarget(auxRam.Slice(0, 0x1000), "AUX");

        var target = new Extended80ColumnPage0Target(mainTarget, auxTarget);

        var access = CreateAccess(0x0500, AccessIntent.DataWrite);

        // Enable 80STORE, PAGE2 off
        target.UpdateRouting(altZp: false, store80: true, page2: false, ramRd: false, ramWrt: false);

        // Write to main (PAGE2 off)
        target.Write8(0x0500, 0x41, access);
        Assert.That(mainRam.AsSpan()[0x0500], Is.EqualTo(0x41));

        // Switch to PAGE2
        target.UpdateRouting(altZp: false, store80: true, page2: true, ramRd: false, ramWrt: false);

        // Write to aux
        target.Write8(0x0500, 0x42, access);
        Assert.That(auxRam.AsSpan()[0x0500], Is.EqualTo(0x42));
        Assert.That(mainRam.AsSpan()[0x0500], Is.EqualTo(0x41), "Main memory should be unchanged");

        // Switch back to PAGE1
        target.UpdateRouting(altZp: false, store80: true, page2: false, ramRd: false, ramWrt: false);

        // Verify main memory is visible again
        var readAccess = CreateAccess(0x0500, AccessIntent.DataRead);
        byte result = target.Read8(0x0500, readAccess);
        Assert.That(result, Is.EqualTo(0x41));
    }

    /// <summary>
    /// Verifies that asymmetric RAMRD/RAMWRT routing works correctly.
    /// </summary>
    [Test]
    public void ReadWrite_AsymmetricRamRdRamWrt_RoutesCorrectly()
    {
        // Arrange
        var mainRam = new PhysicalMemory(0x1000, "MAIN");
        var auxRam = new PhysicalMemory(0x1000, "AUX");
        mainRam.AsSpan()[0x0300] = 0x41;
        auxRam.AsSpan()[0x0300] = 0x42;

        var mainTarget = new RamTarget(mainRam.Slice(0, 0x1000), "MAIN");
        var auxTarget = new RamTarget(auxRam.Slice(0, 0x1000), "AUX");

        var target = new Extended80ColumnPage0Target(mainTarget, auxTarget);

        // Enable RAMRD but not RAMWRT - reads from aux, writes to main
        target.UpdateRouting(altZp: false, store80: false, page2: false, ramRd: true, ramWrt: false);

        var readAccess = CreateAccess(0x0300, AccessIntent.DataRead);
        var writeAccess = CreateAccess(0x0300, AccessIntent.DataWrite);

        // Act - read from aux
        byte result = target.Read8(0x0300, readAccess);

        // Act - write to main
        target.Write8(0x0300, 0x99, writeAccess);

        // Assert
        Assert.That(result, Is.EqualTo(0x42), "Should read from aux (RAMRD on)");
        Assert.That(mainRam.AsSpan()[0x0300], Is.EqualTo(0x99), "Should write to main (RAMWRT off)");
        Assert.That(auxRam.AsSpan()[0x0300], Is.EqualTo(0x42), "Aux should be unchanged");
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