// <copyright file="BusTraceEventTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus.Tests;

/// <summary>
/// Unit tests for the <see cref="BusTraceEvent"/> struct.
/// </summary>
[TestFixture]
public class BusTraceEventTests
{
    /// <summary>
    /// Verifies that the constructor sets all properties.
    /// </summary>
    [Test]
    public void Constructor_SetsAllProperties()
    {
        var evt = new BusTraceEvent(
            cycle: 12345,
            address: 0x1000,
            value: 0x42,
            widthBits: 8,
            intent: AccessIntent.DataRead,
            flags: AccessFlags.None,
            sourceId: 0,
            deviceId: 1,
            regionTag: RegionTag.Ram);

        Assert.Multiple(() =>
        {
            Assert.That(evt.Cycle, Is.EqualTo(12345ul));
            Assert.That(evt.Address, Is.EqualTo(0x1000u));
            Assert.That(evt.Value, Is.EqualTo(0x42u));
            Assert.That(evt.WidthBits, Is.EqualTo((byte)8));
            Assert.That(evt.Intent, Is.EqualTo(AccessIntent.DataRead));
            Assert.That(evt.Flags, Is.EqualTo(AccessFlags.None));
            Assert.That(evt.SourceId, Is.EqualTo(0));
            Assert.That(evt.DeviceId, Is.EqualTo(1));
            Assert.That(evt.RegionTag, Is.EqualTo(RegionTag.Ram));
        });
    }

    /// <summary>
    /// Verifies that FromAccess creates correct event from bus access.
    /// </summary>
    [Test]
    public void FromAccess_CreatesCorrectEvent()
    {
        var access = new BusAccess(
            Address: 0x2000,
            Value: 0,
            WidthBits: 16,
            Mode: CpuMode.Compat,
            EmulationFlag: true,
            Intent: AccessIntent.DataWrite,
            SourceId: 2,
            Cycle: 5000,
            Flags: AccessFlags.Decompose);

        var evt = BusTraceEvent.FromAccess(access, value: 0x1234, deviceId: 5, regionTag: RegionTag.Io);

        Assert.Multiple(() =>
        {
            Assert.That(evt.Cycle, Is.EqualTo(5000ul));
            Assert.That(evt.Address, Is.EqualTo(0x2000u));
            Assert.That(evt.Value, Is.EqualTo(0x1234u));
            Assert.That(evt.WidthBits, Is.EqualTo((byte)16));
            Assert.That(evt.Intent, Is.EqualTo(AccessIntent.DataWrite));
            Assert.That(evt.Flags, Is.EqualTo(AccessFlags.Decompose));
            Assert.That(evt.SourceId, Is.EqualTo(2));
            Assert.That(evt.DeviceId, Is.EqualTo(5));
            Assert.That(evt.RegionTag, Is.EqualTo(RegionTag.Io));
        });
    }

    /// <summary>
    /// Verifies that trace events support 32-bit values.
    /// </summary>
    [Test]
    public void TraceEvent_Supports32BitValues()
    {
        var evt = new BusTraceEvent(
            cycle: 0,
            address: 0xFFFF0000,
            value: 0xDEADBEEF,
            widthBits: 32,
            intent: AccessIntent.DataRead,
            flags: AccessFlags.Atomic,
            sourceId: 0,
            deviceId: 0,
            regionTag: RegionTag.Ram);

        Assert.Multiple(() =>
        {
            Assert.That(evt.Address, Is.EqualTo(0xFFFF0000u));
            Assert.That(evt.Value, Is.EqualTo(0xDEADBEEFu));
            Assert.That(evt.WidthBits, Is.EqualTo((byte)32));
        });
    }
}