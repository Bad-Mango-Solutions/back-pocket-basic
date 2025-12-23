// <copyright file="DeviceRegistryTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus.Tests;

/// <summary>
/// Unit tests for the <see cref="DeviceRegistry"/> class.
/// </summary>
[TestFixture]
public class DeviceRegistryTests
{
    /// <summary>
    /// Verifies that a new DeviceRegistry has zero Count.
    /// </summary>
    [Test]
    public void DeviceRegistry_NewInstance_HasZeroCount()
    {
        var registry = new DeviceRegistry();
        Assert.That(registry.Count, Is.EqualTo(0));
    }

    /// <summary>
    /// Verifies that Register adds a device and increments Count.
    /// </summary>
    [Test]
    public void DeviceRegistry_Register_AddsDevice()
    {
        var registry = new DeviceRegistry();
        registry.Register(1, "Ram", "Main RAM", "main/ram");

        Assert.That(registry.Count, Is.EqualTo(1));
    }

    /// <summary>
    /// Verifies that Register throws ArgumentException for duplicate ID.
    /// </summary>
    [Test]
    public void DeviceRegistry_Register_ThrowsOnDuplicateId()
    {
        var registry = new DeviceRegistry();
        registry.Register(1, "Ram", "Main RAM", "main/ram");

        Assert.Throws<ArgumentException>(() =>
            registry.Register(1, "Rom", "Boot ROM", "main/rom"));
    }

    /// <summary>
    /// Verifies that Register throws ArgumentNullException for null kind.
    /// </summary>
    [Test]
    public void DeviceRegistry_Register_ThrowsOnNullKind()
    {
        var registry = new DeviceRegistry();

        Assert.Throws<ArgumentNullException>(() =>
            registry.Register(1, null!, "Main RAM", "main/ram"));
    }

    /// <summary>
    /// Verifies that Register throws ArgumentNullException for null name.
    /// </summary>
    [Test]
    public void DeviceRegistry_Register_ThrowsOnNullName()
    {
        var registry = new DeviceRegistry();

        Assert.Throws<ArgumentNullException>(() =>
            registry.Register(1, "Ram", null!, "main/ram"));
    }

    /// <summary>
    /// Verifies that Register throws ArgumentNullException for null wiringPath.
    /// </summary>
    [Test]
    public void DeviceRegistry_Register_ThrowsOnNullWiringPath()
    {
        var registry = new DeviceRegistry();

        Assert.Throws<ArgumentNullException>(() =>
            registry.Register(1, "Ram", "Main RAM", null!));
    }

    /// <summary>
    /// Verifies that TryGet returns true and provides DeviceInfo for registered device.
    /// </summary>
    [Test]
    public void DeviceRegistry_TryGet_ReturnsTrueForRegisteredDevice()
    {
        var registry = new DeviceRegistry();
        registry.Register(42, "SlotCard", "Disk II", "main/slots/6/disk2");

        bool found = registry.TryGet(42, out var info);

        Assert.Multiple(() =>
        {
            Assert.That(found, Is.True);
            Assert.That(info.Id, Is.EqualTo(42));
            Assert.That(info.Kind, Is.EqualTo("SlotCard"));
            Assert.That(info.Name, Is.EqualTo("Disk II"));
            Assert.That(info.WiringPath, Is.EqualTo("main/slots/6/disk2"));
        });
    }

    /// <summary>
    /// Verifies that TryGet returns false for unregistered device.
    /// </summary>
    [Test]
    public void DeviceRegistry_TryGet_ReturnsFalseForUnregisteredDevice()
    {
        var registry = new DeviceRegistry();

        bool found = registry.TryGet(999, out _);

        Assert.That(found, Is.False);
    }

    /// <summary>
    /// Verifies that Get returns DeviceInfo for registered device.
    /// </summary>
    [Test]
    public void DeviceRegistry_Get_ReturnsDeviceInfoForRegisteredDevice()
    {
        var registry = new DeviceRegistry();
        registry.Register(10, "MegaII", "Mega II Chip", "main/megaii");

        var info = registry.Get(10);

        Assert.Multiple(() =>
        {
            Assert.That(info.Id, Is.EqualTo(10));
            Assert.That(info.Kind, Is.EqualTo("MegaII"));
            Assert.That(info.Name, Is.EqualTo("Mega II Chip"));
            Assert.That(info.WiringPath, Is.EqualTo("main/megaii"));
        });
    }

    /// <summary>
    /// Verifies that Get throws KeyNotFoundException for unregistered device.
    /// </summary>
    [Test]
    public void DeviceRegistry_Get_ThrowsForUnregisteredDevice()
    {
        var registry = new DeviceRegistry();

        Assert.Throws<KeyNotFoundException>(() => registry.Get(999));
    }

    /// <summary>
    /// Verifies that Contains returns true for registered device.
    /// </summary>
    [Test]
    public void DeviceRegistry_Contains_ReturnsTrueForRegisteredDevice()
    {
        var registry = new DeviceRegistry();
        registry.Register(5, "Ram", "Extended RAM", "main/aux/ram");

        Assert.That(registry.Contains(5), Is.True);
    }

    /// <summary>
    /// Verifies that Contains returns false for unregistered device.
    /// </summary>
    [Test]
    public void DeviceRegistry_Contains_ReturnsFalseForUnregisteredDevice()
    {
        var registry = new DeviceRegistry();

        Assert.That(registry.Contains(5), Is.False);
    }

    /// <summary>
    /// Verifies that GetAll returns all registered devices.
    /// </summary>
    [Test]
    public void DeviceRegistry_GetAll_ReturnsAllRegisteredDevices()
    {
        var registry = new DeviceRegistry();
        registry.Register(1, "Ram", "Main RAM", "main/ram");
        registry.Register(2, "Rom", "Boot ROM", "main/rom");
        registry.Register(3, "SlotCard", "Disk II", "main/slots/6/disk2");

        var all = registry.GetAll().ToList();

        Assert.That(all, Has.Count.EqualTo(3));
    }

    /// <summary>
    /// Verifies that GetAll returns empty enumerable for empty registry.
    /// </summary>
    [Test]
    public void DeviceRegistry_GetAll_ReturnsEmptyForEmptyRegistry()
    {
        var registry = new DeviceRegistry();

        var all = registry.GetAll();

        Assert.That(all, Is.Empty);
    }

    /// <summary>
    /// Verifies that GenerateId returns sequential IDs.
    /// </summary>
    [Test]
    public void DeviceRegistry_GenerateId_ReturnsSequentialIds()
    {
        var registry = new DeviceRegistry();

        int id1 = registry.GenerateId();
        int id2 = registry.GenerateId();
        int id3 = registry.GenerateId();

        Assert.Multiple(() =>
        {
            Assert.That(id1, Is.EqualTo(0));
            Assert.That(id2, Is.EqualTo(1));
            Assert.That(id3, Is.EqualTo(2));
        });
    }

    /// <summary>
    /// Verifies that GenerateId returns IDs beyond registered IDs.
    /// </summary>
    [Test]
    public void DeviceRegistry_GenerateId_ReturnsBeyondRegisteredIds()
    {
        var registry = new DeviceRegistry();
        registry.Register(100, "Ram", "Main RAM", "main/ram");

        int id = registry.GenerateId();

        Assert.That(id, Is.EqualTo(101));
    }

    /// <summary>
    /// Verifies that GenerateId stays ahead after multiple registrations.
    /// </summary>
    [Test]
    public void DeviceRegistry_GenerateId_StaysAheadAfterRegistrations()
    {
        var registry = new DeviceRegistry();
        registry.Register(10, "Ram", "RAM 1", "path1");
        registry.Register(5, "Ram", "RAM 2", "path2");
        registry.Register(20, "Ram", "RAM 3", "path3");

        int id = registry.GenerateId();

        Assert.That(id, Is.EqualTo(21));
    }

    /// <summary>
    /// Verifies DeviceInfo ToString returns expected format.
    /// </summary>
    [Test]
    public void DeviceRegistry_DeviceInfoToString_ReturnsExpectedFormat()
    {
        var registry = new DeviceRegistry();
        registry.Register(1, "SlotCard", "Disk II", "main/slots/6/disk2");

        var info = registry.Get(1);

        Assert.That(info.ToString(), Is.EqualTo("Disk II (SlotCard)"));
    }
}