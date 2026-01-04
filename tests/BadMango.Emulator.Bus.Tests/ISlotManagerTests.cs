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
    /// Verifies that ISlotManager interface defines Slots property.
    /// </summary>
    [Test]
    public void Interface_HasSlotsProperty()
    {
        var property = typeof(ISlotManager).GetProperty(nameof(ISlotManager.Slots));
        Assert.That(property, Is.Not.Null);
        Assert.That(property.PropertyType, Is.EqualTo(typeof(IReadOnlyDictionary<int, ISlotCard>)));
    }

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
    /// Verifies that ISlotManager interface defines Install method.
    /// </summary>
    [Test]
    public void Interface_HasInstallMethod()
    {
        var method = typeof(ISlotManager).GetMethod(nameof(ISlotManager.Install));
        Assert.That(method, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(method.ReturnType, Is.EqualTo(typeof(void)));
            var parameters = method.GetParameters();
            Assert.That(parameters, Has.Length.EqualTo(2));
            Assert.That(parameters[0].ParameterType, Is.EqualTo(typeof(int)));
            Assert.That(parameters[1].ParameterType, Is.EqualTo(typeof(ISlotCard)));
        });
    }

    /// <summary>
    /// Verifies that ISlotManager interface defines Remove method.
    /// </summary>
    [Test]
    public void Interface_HasRemoveMethod()
    {
        var method = typeof(ISlotManager).GetMethod(nameof(ISlotManager.Remove));
        Assert.That(method, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(method.ReturnType, Is.EqualTo(typeof(void)));
            var parameters = method.GetParameters();
            Assert.That(parameters, Has.Length.EqualTo(1));
            Assert.That(parameters[0].ParameterType, Is.EqualTo(typeof(int)));
        });
    }

    /// <summary>
    /// Verifies that ISlotManager interface defines GetCard method.
    /// </summary>
    /// <remarks>
    /// Note: At runtime, nullable reference types (ISlotCard?) are the same CLR type as ISlotCard.
    /// The nullability is compile-time metadata only.
    /// </remarks>
    [Test]
    public void Interface_HasGetCardMethod()
    {
        var method = typeof(ISlotManager).GetMethod(nameof(ISlotManager.GetCard));
        Assert.That(method, Is.Not.Null);
        Assert.That(method.ReturnType, Is.EqualTo(typeof(ISlotCard)));
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

    /// <summary>
    /// Verifies that ISlotManager interface defines HandleSlotROMAccess method.
    /// </summary>
    [Test]
    public void Interface_HasHandleSlotROMAccessMethod()
    {
        var method = typeof(ISlotManager).GetMethod(nameof(ISlotManager.HandleSlotROMAccess));
        Assert.That(method, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(method.ReturnType, Is.EqualTo(typeof(void)));
            var parameters = method.GetParameters();
            Assert.That(parameters, Has.Length.EqualTo(1));
            Assert.That(parameters[0].ParameterType, Is.EqualTo(typeof(Addr)));
        });
    }

    /// <summary>
    /// Verifies that ISlotManager interface defines Reset method.
    /// </summary>
    [Test]
    public void Interface_HasResetMethod()
    {
        var method = typeof(ISlotManager).GetMethod(nameof(ISlotManager.Reset));
        Assert.That(method, Is.Not.Null);
        Assert.That(method.ReturnType, Is.EqualTo(typeof(void)));
    }
}