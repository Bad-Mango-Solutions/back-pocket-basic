// <copyright file="ISpeakerDeviceTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Devices.Tests;

using BadMango.Emulator.Bus.Interfaces;

/// <summary>
/// Unit tests for the <see cref="ISpeakerDevice"/> interface contract.
/// </summary>
[TestFixture]
public class ISpeakerDeviceTests
{
    /// <summary>
    /// Verifies that ISpeakerDevice interface inherits from IMotherboardDevice.
    /// </summary>
    [Test]
    public void Interface_InheritsFromIMotherboardDevice()
    {
        Assert.That(typeof(IMotherboardDevice).IsAssignableFrom(typeof(ISpeakerDevice)), Is.True);
    }

    /// <summary>
    /// Verifies that ISpeakerDevice interface defines State property.
    /// </summary>
    [Test]
    public void Interface_HasStateProperty()
    {
        var property = typeof(ISpeakerDevice).GetProperty(nameof(ISpeakerDevice.State));
        Assert.That(property, Is.Not.Null);
        Assert.That(property.PropertyType, Is.EqualTo(typeof(bool)));
    }

    /// <summary>
    /// Verifies that ISpeakerDevice interface defines PendingToggles property.
    /// </summary>
    [Test]
    public void Interface_HasPendingTogglesProperty()
    {
        var property = typeof(ISpeakerDevice).GetProperty(nameof(ISpeakerDevice.PendingToggles));
        Assert.That(property, Is.Not.Null);
        Assert.That(property.PropertyType, Is.EqualTo(typeof(IReadOnlyList<(ulong Cycle, bool State)>)));
    }

    /// <summary>
    /// Verifies that ISpeakerDevice interface defines DrainToggles method.
    /// </summary>
    [Test]
    public void Interface_HasDrainTogglesMethod()
    {
        var method = typeof(ISpeakerDevice).GetMethod(nameof(ISpeakerDevice.DrainToggles));
        Assert.That(method, Is.Not.Null);
        Assert.That(method.ReturnType, Is.EqualTo(typeof(IList<(ulong Cycle, bool State)>)));
    }

    /// <summary>
    /// Verifies that ISpeakerDevice interface defines Toggled event.
    /// </summary>
    [Test]
    public void Interface_HasToggledEvent()
    {
        var eventInfo = typeof(ISpeakerDevice).GetEvent(nameof(ISpeakerDevice.Toggled));
        Assert.That(eventInfo, Is.Not.Null);
    }
}