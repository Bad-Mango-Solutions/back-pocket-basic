// <copyright file="DeviceInfoTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus.Tests;

/// <summary>
/// Unit tests for the <see cref="DeviceInfo"/> struct.
/// </summary>
[TestFixture]
public class DeviceInfoTests
{
    /// <summary>
    /// Verifies that DeviceInfo can be created with all properties.
    /// </summary>
    [Test]
    public void DeviceInfo_CanBeCreatedWithProperties()
    {
        var info = new DeviceInfo(
            Id: 42,
            Kind: "SlotCard",
            Name: "Disk II Controller",
            WiringPath: "main/slots/6/disk2");

        Assert.Multiple(() =>
        {
            Assert.That(info.Id, Is.EqualTo(42));
            Assert.That(info.Kind, Is.EqualTo("SlotCard"));
            Assert.That(info.Name, Is.EqualTo("Disk II Controller"));
            Assert.That(info.WiringPath, Is.EqualTo("main/slots/6/disk2"));
        });
    }

    /// <summary>
    /// Verifies ToString returns formatted display string.
    /// </summary>
    [Test]
    public void DeviceInfo_ToString_ReturnsFormattedString()
    {
        var info = new DeviceInfo(1, "Ram", "Main Memory", "main/ram");
        var result = info.ToString();

        Assert.That(result, Is.EqualTo("Main Memory (Ram)"));
    }

    /// <summary>
    /// Verifies record equality works correctly.
    /// </summary>
    [Test]
    public void DeviceInfo_RecordEquality_Works()
    {
        var info1 = new DeviceInfo(1, "Ram", "Main Memory", "main/ram");
        var info2 = new DeviceInfo(1, "Ram", "Main Memory", "main/ram");
        var info3 = new DeviceInfo(2, "Ram", "Main Memory", "main/ram");

        Assert.Multiple(() =>
        {
            Assert.That(info1, Is.EqualTo(info2));
            Assert.That(info1, Is.Not.EqualTo(info3));
        });
    }

    /// <summary>
    /// Verifies DeviceInfo can represent various device types.
    /// </summary>
    [Test]
    public void DeviceInfo_VariousDeviceTypes()
    {
        var ramDevice = new DeviceInfo(1, "Ram", "48KB Main RAM", "main/ram/48k");
        var romDevice = new DeviceInfo(2, "Rom", "Applesoft BASIC ROM", "main/rom/applesoft");
        var slotDevice = new DeviceInfo(3, "SlotCard", "Super Serial Card", "main/slots/2/ssc");
        var megaDevice = new DeviceInfo(4, "MegaII", "Mega II Integration", "main/megaii");

        Assert.Multiple(() =>
        {
            Assert.That(ramDevice.Kind, Is.EqualTo("Ram"));
            Assert.That(romDevice.Kind, Is.EqualTo("Rom"));
            Assert.That(slotDevice.Kind, Is.EqualTo("SlotCard"));
            Assert.That(megaDevice.Kind, Is.EqualTo("MegaII"));
        });
    }
}