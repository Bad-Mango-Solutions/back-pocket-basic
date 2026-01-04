// <copyright file="IGamePortDeviceTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Devices.Tests;

using BadMango.Emulator.Bus.Interfaces;

/// <summary>
/// Unit tests for the <see cref="IGamePortDevice"/> interface contract.
/// </summary>
[TestFixture]
public class IGamePortDeviceTests
{
    /// <summary>
    /// Verifies that IGamePortDevice interface inherits from IMotherboardDevice.
    /// </summary>
    [Test]
    public void Interface_InheritsFromIMotherboardDevice()
    {
        Assert.That(typeof(IMotherboardDevice).IsAssignableFrom(typeof(IGamePortDevice)), Is.True);
    }

    /// <summary>
    /// Verifies that IGamePortDevice interface defines Buttons property.
    /// </summary>
    [Test]
    public void Interface_HasButtonsProperty()
    {
        var property = typeof(IGamePortDevice).GetProperty(nameof(IGamePortDevice.Buttons));
        Assert.That(property, Is.Not.Null);
        Assert.That(property.PropertyType, Is.EqualTo(typeof(IReadOnlyList<bool>)));
    }

    /// <summary>
    /// Verifies that IGamePortDevice interface defines Paddles property.
    /// </summary>
    [Test]
    public void Interface_HasPaddlesProperty()
    {
        var property = typeof(IGamePortDevice).GetProperty(nameof(IGamePortDevice.Paddles));
        Assert.That(property, Is.Not.Null);
        Assert.That(property.PropertyType, Is.EqualTo(typeof(IReadOnlyList<byte>)));
    }

    /// <summary>
    /// Verifies that IGamePortDevice interface defines SetButton method.
    /// </summary>
    [Test]
    public void Interface_HasSetButtonMethod()
    {
        var method = typeof(IGamePortDevice).GetMethod(nameof(IGamePortDevice.SetButton));
        Assert.That(method, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(method.ReturnType, Is.EqualTo(typeof(void)));
            var parameters = method.GetParameters();
            Assert.That(parameters, Has.Length.EqualTo(2));
            Assert.That(parameters[0].ParameterType, Is.EqualTo(typeof(int)));
            Assert.That(parameters[1].ParameterType, Is.EqualTo(typeof(bool)));
        });
    }

    /// <summary>
    /// Verifies that IGamePortDevice interface defines SetPaddle method.
    /// </summary>
    [Test]
    public void Interface_HasSetPaddleMethod()
    {
        var method = typeof(IGamePortDevice).GetMethod(nameof(IGamePortDevice.SetPaddle));
        Assert.That(method, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(method.ReturnType, Is.EqualTo(typeof(void)));
            var parameters = method.GetParameters();
            Assert.That(parameters, Has.Length.EqualTo(2));
            Assert.That(parameters[0].ParameterType, Is.EqualTo(typeof(int)));
            Assert.That(parameters[1].ParameterType, Is.EqualTo(typeof(byte)));
        });
    }

    /// <summary>
    /// Verifies that IGamePortDevice interface defines SetJoystick method.
    /// </summary>
    [Test]
    public void Interface_HasSetJoystickMethod()
    {
        var method = typeof(IGamePortDevice).GetMethod(nameof(IGamePortDevice.SetJoystick));
        Assert.That(method, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(method.ReturnType, Is.EqualTo(typeof(void)));
            var parameters = method.GetParameters();
            Assert.That(parameters, Has.Length.EqualTo(2));
            Assert.That(parameters[0].ParameterType, Is.EqualTo(typeof(float)));
            Assert.That(parameters[1].ParameterType, Is.EqualTo(typeof(float)));
        });
    }
}