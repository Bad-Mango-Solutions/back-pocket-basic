// <copyright file="IPeripheralTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus.Tests;

using Interfaces;

/// <summary>
/// Unit tests for the <see cref="IPeripheral"/> interface contract.
/// </summary>
[TestFixture]
public class IPeripheralTests
{
    /// <summary>
    /// Verifies that IPeripheral interface inherits from IScheduledDevice.
    /// </summary>
    [Test]
    public void Interface_InheritsFromIScheduledDevice()
    {
        Assert.That(typeof(IScheduledDevice).IsAssignableFrom(typeof(IPeripheral)), Is.True);
    }

    /// <summary>
    /// Verifies that IPeripheral interface defines Name property from IScheduledDevice.
    /// </summary>
    [Test]
    public void Interface_HasNameProperty()
    {
        // Name is inherited from IScheduledDevice, so we check the base interface
        var baseProperty = typeof(IScheduledDevice).GetProperty(nameof(IScheduledDevice.Name));
        Assert.That(baseProperty, Is.Not.Null);
        Assert.That(baseProperty.PropertyType, Is.EqualTo(typeof(string)));
    }

    /// <summary>
    /// Verifies that IPeripheral interface defines DeviceType property.
    /// </summary>
    [Test]
    public void Interface_HasDeviceTypeProperty()
    {
        var property = typeof(IPeripheral).GetProperty(nameof(IPeripheral.DeviceType));
        Assert.That(property, Is.Not.Null);
        Assert.That(property.PropertyType, Is.EqualTo(typeof(string)));
    }

    /// <summary>
    /// Verifies that IPeripheral interface defines Kind property.
    /// </summary>
    [Test]
    public void Interface_HasKindProperty()
    {
        var property = typeof(IPeripheral).GetProperty(nameof(IPeripheral.Kind));
        Assert.That(property, Is.Not.Null);
        Assert.That(property.PropertyType, Is.EqualTo(typeof(PeripheralKind)));
    }

    /// <summary>
    /// Verifies that IPeripheral interface defines Reset method.
    /// </summary>
    [Test]
    public void Interface_HasResetMethod()
    {
        var method = typeof(IPeripheral).GetMethod(nameof(IPeripheral.Reset));
        Assert.That(method, Is.Not.Null);
        Assert.That(method.ReturnType, Is.EqualTo(typeof(void)));
    }

    /// <summary>
    /// Verifies that IPeripheral interface defines Initialize method from IScheduledDevice.
    /// </summary>
    [Test]
    public void Interface_HasInitializeMethod()
    {
        var method = typeof(IScheduledDevice).GetMethod(nameof(IScheduledDevice.Initialize));
        Assert.That(method, Is.Not.Null);
        Assert.That(method.ReturnType, Is.EqualTo(typeof(void)));
    }
}