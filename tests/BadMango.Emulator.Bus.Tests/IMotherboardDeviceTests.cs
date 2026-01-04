// <copyright file="IMotherboardDeviceTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus.Tests;

using Interfaces;

/// <summary>
/// Unit tests for the <see cref="IMotherboardDevice"/> interface contract.
/// </summary>
[TestFixture]
public class IMotherboardDeviceTests
{
    /// <summary>
    /// Verifies that IMotherboardDevice interface inherits from IPeripheral.
    /// </summary>
    [Test]
    public void Interface_InheritsFromIPeripheral()
    {
        Assert.That(typeof(IPeripheral).IsAssignableFrom(typeof(IMotherboardDevice)), Is.True);
    }

    /// <summary>
    /// Verifies that IMotherboardDevice interface defines RegisterHandlers method.
    /// </summary>
    [Test]
    public void Interface_HasRegisterHandlersMethod()
    {
        var method = typeof(IMotherboardDevice).GetMethod(nameof(IMotherboardDevice.RegisterHandlers));
        Assert.That(method, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(method.ReturnType, Is.EqualTo(typeof(void)));
            var parameters = method.GetParameters();
            Assert.That(parameters, Has.Length.EqualTo(1));
            Assert.That(parameters[0].ParameterType, Is.EqualTo(typeof(IOPageDispatcher)));
        });
    }
}