// <copyright file="IClockDeviceTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Devices.Tests;

using BadMango.Emulator.Bus.Interfaces;

/// <summary>
/// Unit tests for the <see cref="IClockDevice"/> interface contract.
/// </summary>
[TestFixture]
public class IClockDeviceTests
{
    /// <summary>
    /// Verifies that IClockDevice interface inherits from ISlotCard.
    /// </summary>
    [Test]
    public void Interface_InheritsFromISlotCard()
    {
        Assert.That(typeof(ISlotCard).IsAssignableFrom(typeof(IClockDevice)), Is.True);
    }

    /// <summary>
    /// Verifies that IClockDevice interface defines CurrentTime property.
    /// </summary>
    [Test]
    public void Interface_HasCurrentTimeProperty()
    {
        var property = typeof(IClockDevice).GetProperty(nameof(IClockDevice.CurrentTime));
        Assert.That(property, Is.Not.Null);
        Assert.That(property.PropertyType, Is.EqualTo(typeof(DateTime)));
    }

    /// <summary>
    /// Verifies that IClockDevice interface defines UseHostTime property with getter and setter.
    /// </summary>
    [Test]
    public void Interface_HasUseHostTimePropertyWithGetAndSet()
    {
        var property = typeof(IClockDevice).GetProperty(nameof(IClockDevice.UseHostTime));
        Assert.That(property, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(property.PropertyType, Is.EqualTo(typeof(bool)));
            Assert.That(property.CanRead, Is.True);
            Assert.That(property.CanWrite, Is.True);
        });
    }

    /// <summary>
    /// Verifies that IClockDevice interface defines SetFixedTime method.
    /// </summary>
    [Test]
    public void Interface_HasSetFixedTimeMethod()
    {
        var method = typeof(IClockDevice).GetMethod(nameof(IClockDevice.SetFixedTime));
        Assert.That(method, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(method.ReturnType, Is.EqualTo(typeof(void)));
            var parameters = method.GetParameters();
            Assert.That(parameters, Has.Length.EqualTo(1));
            Assert.That(parameters[0].ParameterType, Is.EqualTo(typeof(DateTime)));
        });
    }
}