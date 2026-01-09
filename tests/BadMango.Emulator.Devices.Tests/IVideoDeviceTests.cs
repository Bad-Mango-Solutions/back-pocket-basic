// <copyright file="IVideoDeviceTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Devices.Tests;

using BadMango.Emulator.Bus.Interfaces;

/// <summary>
/// Unit tests for the <see cref="IVideoDevice"/> interface contract.
/// </summary>
[TestFixture]
public class IVideoDeviceTests
{
    /// <summary>
    /// Verifies that IVideoDevice interface inherits from IMotherboardDevice.
    /// </summary>
    [Test]
    public void Interface_InheritsFromIMotherboardDevice()
    {
        Assert.That(typeof(IMotherboardDevice).IsAssignableFrom(typeof(IVideoDevice)), Is.True);
    }

    /// <summary>
    /// Verifies that IVideoDevice interface defines CurrentMode property.
    /// </summary>
    [Test]
    public void Interface_HasCurrentModeProperty()
    {
        var property = typeof(IVideoDevice).GetProperty(nameof(IVideoDevice.CurrentMode));
        Assert.That(property, Is.Not.Null);
        Assert.That(property.PropertyType, Is.EqualTo(typeof(VideoMode)));
    }

    /// <summary>
    /// Verifies that IVideoDevice interface defines IsTextMode property.
    /// </summary>
    [Test]
    public void Interface_HasIsTextModeProperty()
    {
        var property = typeof(IVideoDevice).GetProperty(nameof(IVideoDevice.IsTextMode));
        Assert.That(property, Is.Not.Null);
        Assert.That(property.PropertyType, Is.EqualTo(typeof(bool)));
    }

    /// <summary>
    /// Verifies that IVideoDevice interface defines IsMixedMode property.
    /// </summary>
    [Test]
    public void Interface_HasIsMixedModeProperty()
    {
        var property = typeof(IVideoDevice).GetProperty(nameof(IVideoDevice.IsMixedMode));
        Assert.That(property, Is.Not.Null);
        Assert.That(property.PropertyType, Is.EqualTo(typeof(bool)));
    }

    /// <summary>
    /// Verifies that IVideoDevice interface defines IsPage2 property.
    /// </summary>
    [Test]
    public void Interface_HasIsPage2Property()
    {
        var property = typeof(IVideoDevice).GetProperty(nameof(IVideoDevice.IsPage2));
        Assert.That(property, Is.Not.Null);
        Assert.That(property.PropertyType, Is.EqualTo(typeof(bool)));
    }

    /// <summary>
    /// Verifies that IVideoDevice interface defines IsHiRes property.
    /// </summary>
    [Test]
    public void Interface_HasIsHiResProperty()
    {
        var property = typeof(IVideoDevice).GetProperty(nameof(IVideoDevice.IsHiRes));
        Assert.That(property, Is.Not.Null);
        Assert.That(property.PropertyType, Is.EqualTo(typeof(bool)));
    }

    /// <summary>
    /// Verifies that IVideoDevice interface defines Is80Column property.
    /// </summary>
    [Test]
    public void Interface_HasIs80ColumnProperty()
    {
        var property = typeof(IVideoDevice).GetProperty(nameof(IVideoDevice.Is80Column));
        Assert.That(property, Is.Not.Null);
        Assert.That(property.PropertyType, Is.EqualTo(typeof(bool)));
    }

    /// <summary>
    /// Verifies that IVideoDevice interface defines IsDoubleHiRes property.
    /// </summary>
    [Test]
    public void Interface_HasIsDoubleHiResProperty()
    {
        var property = typeof(IVideoDevice).GetProperty(nameof(IVideoDevice.IsDoubleHiRes));
        Assert.That(property, Is.Not.Null);
        Assert.That(property.PropertyType, Is.EqualTo(typeof(bool)));
    }

    /// <summary>
    /// Verifies that IVideoDevice interface defines IsAltCharSet property.
    /// </summary>
    [Test]
    public void Interface_HasIsAltCharSetProperty()
    {
        var property = typeof(IVideoDevice).GetProperty(nameof(IVideoDevice.IsAltCharSet));
        Assert.That(property, Is.Not.Null);
        Assert.That(property.PropertyType, Is.EqualTo(typeof(bool)));
    }

    /// <summary>
    /// Verifies that IVideoDevice interface defines Annunciators property.
    /// </summary>
    [Test]
    public void Interface_HasAnnunciatorsProperty()
    {
        var property = typeof(IVideoDevice).GetProperty(nameof(IVideoDevice.Annunciators));
        Assert.That(property, Is.Not.Null);
        Assert.That(property.PropertyType, Is.EqualTo(typeof(IReadOnlyList<bool>)));
    }

    /// <summary>
    /// Verifies that IVideoDevice interface defines ModeChanged event.
    /// </summary>
    [Test]
    public void Interface_HasModeChangedEvent()
    {
        var eventInfo = typeof(IVideoDevice).GetEvent(nameof(IVideoDevice.ModeChanged));
        Assert.That(eventInfo, Is.Not.Null);
    }
}