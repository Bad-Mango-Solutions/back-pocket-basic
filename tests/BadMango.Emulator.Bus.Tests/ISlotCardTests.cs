// <copyright file="ISlotCardTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus.Tests;

using Interfaces;

/// <summary>
/// Unit tests for the <see cref="ISlotCard"/> interface contract.
/// </summary>
[TestFixture]
public class ISlotCardTests
{
    /// <summary>
    /// Verifies that ISlotCard interface inherits from IPeripheral.
    /// </summary>
    [Test]
    public void Interface_InheritsFromIPeripheral()
    {
        Assert.That(typeof(IPeripheral).IsAssignableFrom(typeof(ISlotCard)), Is.True);
    }

    /// <summary>
    /// Verifies that ISlotCard interface defines SlotNumber property with getter and setter.
    /// </summary>
    [Test]
    public void Interface_HasSlotNumberPropertyWithGetAndSet()
    {
        var property = typeof(ISlotCard).GetProperty(nameof(ISlotCard.SlotNumber));
        Assert.That(property, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(property.PropertyType, Is.EqualTo(typeof(int)));
            Assert.That(property.CanRead, Is.True);
            Assert.That(property.CanWrite, Is.True);
        });
    }

    /// <summary>
    /// Verifies that ISlotCard interface defines IOHandlers property.
    /// </summary>
    [Test]
    public void Interface_HasIOHandlersProperty()
    {
        var property = typeof(ISlotCard).GetProperty(nameof(ISlotCard.IOHandlers));
        Assert.That(property, Is.Not.Null);
        Assert.That(property.PropertyType, Is.EqualTo(typeof(SlotIOHandlers)));
    }

    /// <summary>
    /// Verifies that ISlotCard interface defines ROMRegion property.
    /// </summary>
    /// <remarks>
    /// Note: At runtime, nullable reference types (IBusTarget?) are the same CLR type as IBusTarget.
    /// </remarks>
    [Test]
    public void Interface_HasROMRegionProperty()
    {
        var property = typeof(ISlotCard).GetProperty(nameof(ISlotCard.ROMRegion));
        Assert.That(property, Is.Not.Null);
        Assert.That(property.PropertyType, Is.EqualTo(typeof(IBusTarget)));
    }

    /// <summary>
    /// Verifies that ISlotCard interface defines ExpansionROMRegion property.
    /// </summary>
    /// <remarks>
    /// Note: At runtime, nullable reference types (IBusTarget?) are the same CLR type as IBusTarget.
    /// </remarks>
    [Test]
    public void Interface_HasExpansionROMRegionProperty()
    {
        var property = typeof(ISlotCard).GetProperty(nameof(ISlotCard.ExpansionROMRegion));
        Assert.That(property, Is.Not.Null);
        Assert.That(property.PropertyType, Is.EqualTo(typeof(IBusTarget)));
    }

    /// <summary>
    /// Verifies that ISlotCard interface defines OnExpansionROMSelected method.
    /// </summary>
    [Test]
    public void Interface_HasOnExpansionROMSelectedMethod()
    {
        var method = typeof(ISlotCard).GetMethod(nameof(ISlotCard.OnExpansionROMSelected));
        Assert.That(method, Is.Not.Null);
        Assert.That(method.ReturnType, Is.EqualTo(typeof(void)));
    }

    /// <summary>
    /// Verifies that ISlotCard interface defines OnExpansionROMDeselected method.
    /// </summary>
    [Test]
    public void Interface_HasOnExpansionROMDeselectedMethod()
    {
        var method = typeof(ISlotCard).GetMethod(nameof(ISlotCard.OnExpansionROMDeselected));
        Assert.That(method, Is.Not.Null);
        Assert.That(method.ReturnType, Is.EqualTo(typeof(void)));
    }
}