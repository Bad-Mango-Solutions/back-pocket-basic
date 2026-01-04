// <copyright file="PeripheralKindTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus.Tests;

/// <summary>
/// Unit tests for the <see cref="PeripheralKind"/> enumeration.
/// </summary>
[TestFixture]
public class PeripheralKindTests
{
    /// <summary>
    /// Verifies that Motherboard value is defined.
    /// </summary>
    [Test]
    public void PeripheralKind_HasMotherboardValue()
    {
        Assert.That(Enum.IsDefined(typeof(PeripheralKind), PeripheralKind.Motherboard), Is.True);
    }

    /// <summary>
    /// Verifies that SlotCard value is defined.
    /// </summary>
    [Test]
    public void PeripheralKind_HasSlotCardValue()
    {
        Assert.That(Enum.IsDefined(typeof(PeripheralKind), PeripheralKind.SlotCard), Is.True);
    }

    /// <summary>
    /// Verifies that Internal value is defined.
    /// </summary>
    [Test]
    public void PeripheralKind_HasInternalValue()
    {
        Assert.That(Enum.IsDefined(typeof(PeripheralKind), PeripheralKind.Internal), Is.True);
    }

    /// <summary>
    /// Verifies that the enum has exactly 3 values.
    /// </summary>
    [Test]
    public void PeripheralKind_HasExpectedValueCount()
    {
        var values = Enum.GetValues<PeripheralKind>();
        Assert.That(values, Has.Length.EqualTo(3));
    }
}