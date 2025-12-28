// <copyright file="CycleTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus.Tests;

using Core;

/// <summary>
/// Unit tests for the <see cref="Cycle"/> record struct.
/// </summary>
[TestFixture]
public class CycleTests
{
    /// <summary>
    /// Verifies that Cycle.Zero returns a zero value.
    /// </summary>
    [Test]
    public void Cycle_Zero_ReturnsZeroValue()
    {
        var cycle = Cycle.Zero;

        Assert.That(cycle.Value, Is.EqualTo(0ul));
    }

    /// <summary>
    /// Verifies that Cycle.One returns a one value.
    /// </summary>
    [Test]
    public void Cycle_One_ReturnsOneValue()
    {
        var cycle = Cycle.One;

        Assert.That(cycle.Value, Is.EqualTo(1ul));
    }

    /// <summary>
    /// Verifies that Cycle can be created with a value.
    /// </summary>
    [Test]
    public void Cycle_CanBeCreatedWithValue()
    {
        var cycle = new Cycle(100ul);

        Assert.That(cycle.Value, Is.EqualTo(100ul));
    }

    /// <summary>
    /// Verifies implicit conversion from ulong to Cycle.
    /// </summary>
    [Test]
    public void Cycle_ImplicitConversion_FromUlong()
    {
        Cycle cycle = 500ul;

        Assert.That(cycle.Value, Is.EqualTo(500ul));
    }

    /// <summary>
    /// Verifies implicit conversion from Cycle to ulong.
    /// </summary>
    [Test]
    public void Cycle_ImplicitConversion_ToUlong()
    {
        var cycle = new Cycle(250ul);
        ulong value = cycle;

        Assert.That(value, Is.EqualTo(250ul));
    }

    /// <summary>
    /// Verifies addition of two Cycle values.
    /// </summary>
    [Test]
    public void Cycle_Addition_ReturnsCorrectSum()
    {
        var a = new Cycle(100ul);
        var b = new Cycle(50ul);

        var result = a + b;

        Assert.That(result.Value, Is.EqualTo(150ul));
    }

    /// <summary>
    /// Verifies subtraction of two Cycle values.
    /// </summary>
    [Test]
    public void Cycle_Subtraction_ReturnsCorrectDifference()
    {
        var a = new Cycle(100ul);
        var b = new Cycle(30ul);

        var result = a - b;

        Assert.That(result.Value, Is.EqualTo(70ul));
    }

    /// <summary>
    /// Verifies less-than comparison.
    /// </summary>
    [Test]
    public void Cycle_LessThan_ReturnsCorrectResult()
    {
        var a = new Cycle(50ul);
        var b = new Cycle(100ul);
        var c = new Cycle(50ul);

        Assert.Multiple(() =>
        {
            Assert.That(a < b, Is.True);
            Assert.That(b < a, Is.False);
            Assert.That(c < a, Is.False); // Test same value comparison
        });
    }

    /// <summary>
    /// Verifies greater-than comparison.
    /// </summary>
    [Test]
    public void Cycle_GreaterThan_ReturnsCorrectResult()
    {
        var a = new Cycle(100ul);
        var b = new Cycle(50ul);
        var c = new Cycle(100ul);

        Assert.Multiple(() =>
        {
            Assert.That(a > b, Is.True);
            Assert.That(b > a, Is.False);
            Assert.That(c > a, Is.False); // Test same value comparison
        });
    }

    /// <summary>
    /// Verifies less-than-or-equal comparison.
    /// </summary>
    [Test]
    public void Cycle_LessThanOrEqual_ReturnsCorrectResult()
    {
        var a = new Cycle(50ul);
        var b = new Cycle(100ul);
        var c = new Cycle(50ul);

        Assert.Multiple(() =>
        {
            Assert.That(a <= b, Is.True);
            Assert.That(b <= a, Is.False);
            Assert.That(a <= c, Is.True);
        });
    }

    /// <summary>
    /// Verifies greater-than-or-equal comparison.
    /// </summary>
    [Test]
    public void Cycle_GreaterThanOrEqual_ReturnsCorrectResult()
    {
        var a = new Cycle(100ul);
        var b = new Cycle(50ul);
        var c = new Cycle(100ul);

        Assert.Multiple(() =>
        {
            Assert.That(a >= b, Is.True);
            Assert.That(b >= a, Is.False);
            Assert.That(a >= c, Is.True);
        });
    }

    /// <summary>
    /// Verifies CompareTo returns correct ordering.
    /// </summary>
    [Test]
    public void Cycle_CompareTo_ReturnsCorrectOrdering()
    {
        var a = new Cycle(50ul);
        var b = new Cycle(100ul);
        var c = new Cycle(50ul);

        Assert.Multiple(() =>
        {
            Assert.That(a.CompareTo(b), Is.LessThan(0));
            Assert.That(b.CompareTo(a), Is.GreaterThan(0));
            Assert.That(a.CompareTo(c), Is.EqualTo(0));
        });
    }

    /// <summary>
    /// Verifies record equality.
    /// </summary>
    [Test]
    public void Cycle_RecordEquality_Works()
    {
        var a = new Cycle(100ul);
        var b = new Cycle(100ul);
        var c = new Cycle(50ul);

        Assert.Multiple(() =>
        {
            Assert.That(a, Is.EqualTo(b));
            Assert.That(a, Is.Not.EqualTo(c));
        });
    }

    /// <summary>
    /// Verifies ToString returns descriptive string.
    /// </summary>
    [Test]
    public void Cycle_ToString_ReturnsDescriptiveString()
    {
        var cycle = new Cycle(123ul);

        Assert.That(cycle.ToString(), Is.EqualTo("123 cycles"));
    }

    /// <summary>
    /// Verifies the prefix increment operator returns incremented value.
    /// </summary>
    [Test]
    public void Cycle_PrefixIncrement_ReturnsIncrementedValue()
    {
        var cycle = new Cycle(100ul);

        var result = ++cycle;

        Assert.Multiple(() =>
        {
            Assert.That(result.Value, Is.EqualTo(101ul));
            Assert.That(cycle.Value, Is.EqualTo(101ul));
        });
    }

    /// <summary>
    /// Verifies the postfix increment operator returns original value and increments.
    /// </summary>
    [Test]
    public void Cycle_PostfixIncrement_ReturnsOriginalValueThenIncrements()
    {
        var cycle = new Cycle(100ul);

        var result = cycle++;

        Assert.Multiple(() =>
        {
            Assert.That(result.Value, Is.EqualTo(100ul));
            Assert.That(cycle.Value, Is.EqualTo(101ul));
        });
    }

    /// <summary>
    /// Verifies the compound assignment operator (+=) works correctly.
    /// </summary>
    [Test]
    public void Cycle_CompoundAddition_ReturnsCorrectSum()
    {
        var cycle = new Cycle(100ul);

        cycle += new Cycle(50ul);

        Assert.That(cycle.Value, Is.EqualTo(150ul));
    }

    /// <summary>
    /// Verifies the compound subtraction operator (-=) works correctly.
    /// </summary>
    [Test]
    public void Cycle_CompoundSubtraction_ReturnsCorrectDifference()
    {
        var cycle = new Cycle(100ul);

        cycle -= new Cycle(30ul);

        Assert.That(cycle.Value, Is.EqualTo(70ul));
    }

    /// <summary>
    /// Verifies increment from zero works correctly.
    /// </summary>
    [Test]
    public void Cycle_IncrementFromZero_ReturnsOne()
    {
        var cycle = Cycle.Zero;

        cycle++;

        Assert.That(cycle.Value, Is.EqualTo(1ul));
    }

    /// <summary>
    /// Verifies increment from One constant works correctly.
    /// </summary>
    [Test]
    public void Cycle_IncrementFromOne_ReturnsTwo()
    {
        var cycle = Cycle.One;

        cycle++;

        Assert.That(cycle.Value, Is.EqualTo(2ul));
    }
}