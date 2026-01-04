// <copyright file="IVideoModeDeviceTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Devices.Tests;

using BadMango.Emulator.Bus.Interfaces;

/// <summary>
/// Unit tests for the <see cref="IVideoModeDevice"/> interface contract.
/// </summary>
[TestFixture]
public class IVideoModeDeviceTests
{
    /// <summary>
    /// Verifies that IVideoModeDevice interface inherits from IMotherboardDevice.
    /// </summary>
    [Test]
    public void Interface_InheritsFromIMotherboardDevice()
    {
        Assert.That(typeof(IMotherboardDevice).IsAssignableFrom(typeof(IVideoModeDevice)), Is.True);
    }

    /// <summary>
    /// Verifies that IVideoModeDevice interface defines CurrentMode property.
    /// </summary>
    [Test]
    public void Interface_HasCurrentModeProperty()
    {
        var property = typeof(IVideoModeDevice).GetProperty(nameof(IVideoModeDevice.CurrentMode));
        Assert.That(property, Is.Not.Null);
        Assert.That(property.PropertyType, Is.EqualTo(typeof(VideoMode)));
    }

    /// <summary>
    /// Verifies that IVideoModeDevice interface defines IsTextMode property.
    /// </summary>
    [Test]
    public void Interface_HasIsTextModeProperty()
    {
        var property = typeof(IVideoModeDevice).GetProperty(nameof(IVideoModeDevice.IsTextMode));
        Assert.That(property, Is.Not.Null);
        Assert.That(property.PropertyType, Is.EqualTo(typeof(bool)));
    }

    /// <summary>
    /// Verifies that IVideoModeDevice interface defines IsMixedMode property.
    /// </summary>
    [Test]
    public void Interface_HasIsMixedModeProperty()
    {
        var property = typeof(IVideoModeDevice).GetProperty(nameof(IVideoModeDevice.IsMixedMode));
        Assert.That(property, Is.Not.Null);
        Assert.That(property.PropertyType, Is.EqualTo(typeof(bool)));
    }

    /// <summary>
    /// Verifies that IVideoModeDevice interface defines IsPage2 property.
    /// </summary>
    [Test]
    public void Interface_HasIsPage2Property()
    {
        var property = typeof(IVideoModeDevice).GetProperty(nameof(IVideoModeDevice.IsPage2));
        Assert.That(property, Is.Not.Null);
        Assert.That(property.PropertyType, Is.EqualTo(typeof(bool)));
    }

    /// <summary>
    /// Verifies that IVideoModeDevice interface defines IsHiRes property.
    /// </summary>
    [Test]
    public void Interface_HasIsHiResProperty()
    {
        var property = typeof(IVideoModeDevice).GetProperty(nameof(IVideoModeDevice.IsHiRes));
        Assert.That(property, Is.Not.Null);
        Assert.That(property.PropertyType, Is.EqualTo(typeof(bool)));
    }

    /// <summary>
    /// Verifies that IVideoModeDevice interface defines Is80Column property.
    /// </summary>
    [Test]
    public void Interface_HasIs80ColumnProperty()
    {
        var property = typeof(IVideoModeDevice).GetProperty(nameof(IVideoModeDevice.Is80Column));
        Assert.That(property, Is.Not.Null);
        Assert.That(property.PropertyType, Is.EqualTo(typeof(bool)));
    }

    /// <summary>
    /// Verifies that IVideoModeDevice interface defines IsDoubleHiRes property.
    /// </summary>
    [Test]
    public void Interface_HasIsDoubleHiResProperty()
    {
        var property = typeof(IVideoModeDevice).GetProperty(nameof(IVideoModeDevice.IsDoubleHiRes));
        Assert.That(property, Is.Not.Null);
        Assert.That(property.PropertyType, Is.EqualTo(typeof(bool)));
    }

    /// <summary>
    /// Verifies that IVideoModeDevice interface defines IsAltCharSet property.
    /// </summary>
    [Test]
    public void Interface_HasIsAltCharSetProperty()
    {
        var property = typeof(IVideoModeDevice).GetProperty(nameof(IVideoModeDevice.IsAltCharSet));
        Assert.That(property, Is.Not.Null);
        Assert.That(property.PropertyType, Is.EqualTo(typeof(bool)));
    }

    /// <summary>
    /// Verifies that IVideoModeDevice interface defines Annunciators property.
    /// </summary>
    [Test]
    public void Interface_HasAnnunciatorsProperty()
    {
        var property = typeof(IVideoModeDevice).GetProperty(nameof(IVideoModeDevice.Annunciators));
        Assert.That(property, Is.Not.Null);
        Assert.That(property.PropertyType, Is.EqualTo(typeof(IReadOnlyList<bool>)));
    }

    /// <summary>
    /// Verifies that IVideoModeDevice interface defines ModeChanged event.
    /// </summary>
    [Test]
    public void Interface_HasModeChangedEvent()
    {
        var eventInfo = typeof(IVideoModeDevice).GetEvent(nameof(IVideoModeDevice.ModeChanged));
        Assert.That(eventInfo, Is.Not.Null);
    }
}