// <copyright file="CyclesTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus.Tests;

/// <summary>
/// Unit tests for the <see cref="Cycles"/> record struct.
/// </summary>
[TestFixture]
public class CyclesTests
{
    /// <summary>
    /// Verifies that Cycles.Zero returns a zero value.
    /// </summary>
    [Test]
    public void Cycles_Zero_ReturnsZeroValue()
    {
        var cycles = Cycles.Zero;

        Assert.That(cycles.Value, Is.EqualTo(0ul));
    }

    /// <summary>
    /// Verifies that Cycles.One returns a one value.
    /// </summary>
    [Test]
    public void Cycles_One_ReturnsOneValue()
    {
        var cycles = Cycles.One;

        Assert.That(cycles.Value, Is.EqualTo(1ul));
    }

    /// <summary>
    /// Verifies that Cycles can be created with a value.
    /// </summary>
    [Test]
    public void Cycles_CanBeCreatedWithValue()
    {
        var cycles = new Cycles(100ul);

        Assert.That(cycles.Value, Is.EqualTo(100ul));
    }

    /// <summary>
    /// Verifies implicit conversion from ulong to Cycles.
    /// </summary>
    [Test]
    public void Cycles_ImplicitConversion_FromUlong()
    {
        Cycles cycles = 500ul;

        Assert.That(cycles.Value, Is.EqualTo(500ul));
    }

    /// <summary>
    /// Verifies explicit conversion from Cycles to ulong.
    /// </summary>
    [Test]
    public void Cycles_ExplicitConversion_ToUlong()
    {
        var cycles = new Cycles(250ul);
        ulong value = (ulong)cycles;

        Assert.That(value, Is.EqualTo(250ul));
    }

    /// <summary>
    /// Verifies addition of two Cycles values.
    /// </summary>
    [Test]
    public void Cycles_Addition_ReturnsCorrectSum()
    {
        var a = new Cycles(100ul);
        var b = new Cycles(50ul);

        var result = a + b;

        Assert.That(result.Value, Is.EqualTo(150ul));
    }

    /// <summary>
    /// Verifies subtraction of two Cycles values.
    /// </summary>
    [Test]
    public void Cycles_Subtraction_ReturnsCorrectDifference()
    {
        var a = new Cycles(100ul);
        var b = new Cycles(30ul);

        var result = a - b;

        Assert.That(result.Value, Is.EqualTo(70ul));
    }

    /// <summary>
    /// Verifies less-than comparison.
    /// </summary>
    [Test]
    public void Cycles_LessThan_ReturnsCorrectResult()
    {
        var a = new Cycles(50ul);
        var b = new Cycles(100ul);
        var c = new Cycles(50ul);

        Assert.Multiple(() =>
        {
            Assert.That(a < b, Is.True);
            Assert.That(b < a, Is.False);
            Assert.That(c < c, Is.False); // Test same value comparison
        });
    }

    /// <summary>
    /// Verifies greater-than comparison.
    /// </summary>
    [Test]
    public void Cycles_GreaterThan_ReturnsCorrectResult()
    {
        var a = new Cycles(100ul);
        var b = new Cycles(50ul);
        var c = new Cycles(100ul);

        Assert.Multiple(() =>
        {
            Assert.That(a > b, Is.True);
            Assert.That(b > a, Is.False);
            Assert.That(c > c, Is.False); // Test same value comparison
        });
    }

    /// <summary>
    /// Verifies less-than-or-equal comparison.
    /// </summary>
    [Test]
    public void Cycles_LessThanOrEqual_ReturnsCorrectResult()
    {
        var a = new Cycles(50ul);
        var b = new Cycles(100ul);
        var c = new Cycles(50ul);

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
    public void Cycles_GreaterThanOrEqual_ReturnsCorrectResult()
    {
        var a = new Cycles(100ul);
        var b = new Cycles(50ul);
        var c = new Cycles(100ul);

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
    public void Cycles_CompareTo_ReturnsCorrectOrdering()
    {
        var a = new Cycles(50ul);
        var b = new Cycles(100ul);
        var c = new Cycles(50ul);

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
    public void Cycles_RecordEquality_Works()
    {
        var a = new Cycles(100ul);
        var b = new Cycles(100ul);
        var c = new Cycles(50ul);

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
    public void Cycles_ToString_ReturnsDescriptiveString()
    {
        var cycles = new Cycles(123ul);

        Assert.That(cycles.ToString(), Is.EqualTo("123 cycles"));
    }
}