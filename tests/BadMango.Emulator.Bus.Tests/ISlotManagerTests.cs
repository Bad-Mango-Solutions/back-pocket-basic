// <copyright file="ISlotManagerTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus.Tests;

using Interfaces;

/// <summary>
/// Unit tests for the <see cref="ISlotManager"/> interface contract.
/// </summary>
[TestFixture]
public class ISlotManagerTests
{
    /// <summary>
    /// Verifies that ISlotManager interface defines ActiveExpansionSlot property.
    /// </summary>
    [Test]
    public void Interface_HasActiveExpansionSlotProperty()
    {
        var property = typeof(ISlotManager).GetProperty(nameof(ISlotManager.ActiveExpansionSlot));
        Assert.That(property, Is.Not.Null);
        Assert.That(property.PropertyType, Is.EqualTo(typeof(int?)));
    }

    /// <summary>
    /// Verifies that ISlotManager interface defines GetSlotRomRegion method.
    /// </summary>
    /// <remarks>
    /// Note: At runtime, nullable reference types (IBusTarget?) are the same CLR type as IBusTarget.
    /// The nullability is compile-time metadata only.
    /// </remarks>
    [Test]
    public void Interface_HasGetSlotRomRegionMethod()
    {
        var method = typeof(ISlotManager).GetMethod(nameof(ISlotManager.GetSlotRomRegion));
        Assert.That(method, Is.Not.Null);
        Assert.That(method.ReturnType, Is.EqualTo(typeof(IBusTarget)));
    }

    /// <summary>
    /// Verifies that ISlotManager interface defines GetExpansionRomRegion method.
    /// </summary>
    /// <remarks>
    /// Note: At runtime, nullable reference types (IBusTarget?) are the same CLR type as IBusTarget.
    /// The nullability is compile-time metadata only.
    /// </remarks>
    [Test]
    public void Interface_HasGetExpansionRomRegionMethod()
    {
        var method = typeof(ISlotManager).GetMethod(nameof(ISlotManager.GetExpansionRomRegion));
        Assert.That(method, Is.Not.Null);
        Assert.That(method.ReturnType, Is.EqualTo(typeof(IBusTarget)));
    }

    /// <summary>
    /// Verifies that ISlotManager interface defines SelectExpansionSlot method.
    /// </summary>
    [Test]
    public void Interface_HasSelectExpansionSlotMethod()
    {
        var method = typeof(ISlotManager).GetMethod(nameof(ISlotManager.SelectExpansionSlot));
        Assert.That(method, Is.Not.Null);
        Assert.That(method.ReturnType, Is.EqualTo(typeof(void)));
    }

    /// <summary>
    /// Verifies that ISlotManager interface defines DeselectExpansionSlot method.
    /// </summary>
    [Test]
    public void Interface_HasDeselectExpansionSlotMethod()
    {
        var method = typeof(ISlotManager).GetMethod(nameof(ISlotManager.DeselectExpansionSlot));
        Assert.That(method, Is.Not.Null);
        Assert.That(method.ReturnType, Is.EqualTo(typeof(void)));
    }
}