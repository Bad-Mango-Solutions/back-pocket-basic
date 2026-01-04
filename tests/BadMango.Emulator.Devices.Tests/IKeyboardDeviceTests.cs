// <copyright file="IKeyboardDeviceTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Devices.Tests;

using BadMango.Emulator.Bus.Interfaces;

/// <summary>
/// Unit tests for the <see cref="IKeyboardDevice"/> interface contract.
/// </summary>
[TestFixture]
public class IKeyboardDeviceTests
{
    /// <summary>
    /// Verifies that IKeyboardDevice interface inherits from IMotherboardDevice.
    /// </summary>
    [Test]
    public void Interface_InheritsFromIMotherboardDevice()
    {
        Assert.That(typeof(IMotherboardDevice).IsAssignableFrom(typeof(IKeyboardDevice)), Is.True);
    }

    /// <summary>
    /// Verifies that IKeyboardDevice interface defines HasKeyDown property.
    /// </summary>
    [Test]
    public void Interface_HasHasKeyDownProperty()
    {
        var property = typeof(IKeyboardDevice).GetProperty(nameof(IKeyboardDevice.HasKeyDown));
        Assert.That(property, Is.Not.Null);
        Assert.That(property.PropertyType, Is.EqualTo(typeof(bool)));
    }

    /// <summary>
    /// Verifies that IKeyboardDevice interface defines KeyData property.
    /// </summary>
    [Test]
    public void Interface_HasKeyDataProperty()
    {
        var property = typeof(IKeyboardDevice).GetProperty(nameof(IKeyboardDevice.KeyData));
        Assert.That(property, Is.Not.Null);
        Assert.That(property.PropertyType, Is.EqualTo(typeof(byte)));
    }

    /// <summary>
    /// Verifies that IKeyboardDevice interface defines Modifiers property.
    /// </summary>
    [Test]
    public void Interface_HasModifiersProperty()
    {
        var property = typeof(IKeyboardDevice).GetProperty(nameof(IKeyboardDevice.Modifiers));
        Assert.That(property, Is.Not.Null);
        Assert.That(property.PropertyType, Is.EqualTo(typeof(KeyboardModifiers)));
    }

    /// <summary>
    /// Verifies that IKeyboardDevice interface defines KeyDown method.
    /// </summary>
    [Test]
    public void Interface_HasKeyDownMethod()
    {
        var method = typeof(IKeyboardDevice).GetMethod(nameof(IKeyboardDevice.KeyDown));
        Assert.That(method, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(method.ReturnType, Is.EqualTo(typeof(void)));
            var parameters = method.GetParameters();
            Assert.That(parameters, Has.Length.EqualTo(1));
            Assert.That(parameters[0].ParameterType, Is.EqualTo(typeof(byte)));
        });
    }

    /// <summary>
    /// Verifies that IKeyboardDevice interface defines KeyUp method.
    /// </summary>
    [Test]
    public void Interface_HasKeyUpMethod()
    {
        var method = typeof(IKeyboardDevice).GetMethod(nameof(IKeyboardDevice.KeyUp));
        Assert.That(method, Is.Not.Null);
        Assert.That(method.ReturnType, Is.EqualTo(typeof(void)));
    }

    /// <summary>
    /// Verifies that IKeyboardDevice interface defines SetModifiers method.
    /// </summary>
    [Test]
    public void Interface_HasSetModifiersMethod()
    {
        var method = typeof(IKeyboardDevice).GetMethod(nameof(IKeyboardDevice.SetModifiers));
        Assert.That(method, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(method.ReturnType, Is.EqualTo(typeof(void)));
            var parameters = method.GetParameters();
            Assert.That(parameters, Has.Length.EqualTo(1));
            Assert.That(parameters[0].ParameterType, Is.EqualTo(typeof(KeyboardModifiers)));
        });
    }

    /// <summary>
    /// Verifies that IKeyboardDevice interface defines TypeString method.
    /// </summary>
    [Test]
    public void Interface_HasTypeStringMethod()
    {
        var method = typeof(IKeyboardDevice).GetMethod(nameof(IKeyboardDevice.TypeString));
        Assert.That(method, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(method.ReturnType, Is.EqualTo(typeof(void)));
            var parameters = method.GetParameters();
            Assert.That(parameters, Has.Length.EqualTo(2));
            Assert.That(parameters[0].ParameterType, Is.EqualTo(typeof(string)));
            Assert.That(parameters[1].ParameterType, Is.EqualTo(typeof(int)));
        });
    }
}