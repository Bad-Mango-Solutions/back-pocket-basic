// <copyright file="BasicValueTests.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Tests;

using Interpreter.Runtime;

[TestFixture]
public class BasicValueTests
{
    [Test]
    public void FromNumber_CreatesNumericValue()
    {
        var value = BasicValue.FromNumber(42);

        Assert.That(value.IsNumeric, Is.True);
        Assert.That(value.IsString, Is.False);
        Assert.That(value.AsNumber(), Is.EqualTo(42));
    }

    [Test]
    public void FromString_CreatesStringValue()
    {
        var value = BasicValue.FromString("HELLO");

        Assert.That(value.IsString, Is.True);
        Assert.That(value.IsNumeric, Is.False);
        Assert.That(value.AsString(), Is.EqualTo("HELLO"));
    }

    [Test]
    public void Addition_Numbers_AddsCorrectly()
    {
        var a = BasicValue.FromNumber(10);
        var b = BasicValue.FromNumber(20);

        var result = a + b;

        Assert.That(result.AsNumber(), Is.EqualTo(30));
    }

    [Test]
    public void Addition_Strings_Concatenates()
    {
        var a = BasicValue.FromString("HELLO");
        var b = BasicValue.FromString(" WORLD");

        var result = a + b;

        Assert.That(result.AsString(), Is.EqualTo("HELLO WORLD"));
    }

    [Test]
    public void Subtraction_Numbers_SubtractsCorrectly()
    {
        var a = BasicValue.FromNumber(30);
        var b = BasicValue.FromNumber(10);

        var result = a - b;

        Assert.That(result.AsNumber(), Is.EqualTo(20));
    }

    [Test]
    public void Multiplication_Numbers_MultipliesCorrectly()
    {
        var a = BasicValue.FromNumber(5);
        var b = BasicValue.FromNumber(4);

        var result = a * b;

        Assert.That(result.AsNumber(), Is.EqualTo(20));
    }

    [Test]
    public void Division_Numbers_DividesCorrectly()
    {
        var a = BasicValue.FromNumber(20);
        var b = BasicValue.FromNumber(4);

        var result = a / b;

        Assert.That(result.AsNumber(), Is.EqualTo(5));
    }

    [Test]
    public void Division_ByZero_ThrowsException()
    {
        var a = BasicValue.FromNumber(10);
        var b = BasicValue.FromNumber(0);

        Assert.Throws<BasicRuntimeException>(() => _ = a / b);
    }

    [Test]
    public void Power_Numbers_ComputesCorrectly()
    {
        var a = BasicValue.FromNumber(2);
        var b = BasicValue.FromNumber(3);

        var result = a ^ b;

        Assert.That(result.AsNumber(), Is.EqualTo(8));
    }

    [Test]
    public void Negation_Number_NegatesCorrectly()
    {
        var value = BasicValue.FromNumber(5);

        var result = -value;

        Assert.That(result.AsNumber(), Is.EqualTo(-5));
    }

    [Test]
    public void Equality_SameNumbers_ReturnsTrue()
    {
        var a = BasicValue.FromNumber(42);
        var b = BasicValue.FromNumber(42);

        Assert.That(a == b, Is.True);
    }

    [Test]
    public void Equality_DifferentNumbers_ReturnsFalse()
    {
        var a = BasicValue.FromNumber(42);
        var b = BasicValue.FromNumber(43);

        Assert.That(a == b, Is.False);
    }

    [Test]
    public void Equality_SameStrings_ReturnsTrue()
    {
        var a = BasicValue.FromString("HELLO");
        var b = BasicValue.FromString("HELLO");

        Assert.That(a == b, Is.True);
    }

    [Test]
    public void LessThan_Numbers_ComparesCorrectly()
    {
        var a = BasicValue.FromNumber(5);
        var b = BasicValue.FromNumber(10);

        Assert.That(a < b, Is.True);
        Assert.That(b < a, Is.False);
    }

    [Test]
    public void GreaterThan_Numbers_ComparesCorrectly()
    {
        var a = BasicValue.FromNumber(10);
        var b = BasicValue.FromNumber(5);

        Assert.That(a > b, Is.True);
        Assert.That(b > a, Is.False);
    }

    [Test]
    public void IsTrue_NonZeroNumber_ReturnsTrue()
    {
        var value = BasicValue.FromNumber(1);
        Assert.That(value.IsTrue(), Is.True);
    }

    [Test]
    public void IsTrue_ZeroNumber_ReturnsFalse()
    {
        var value = BasicValue.FromNumber(0);
        Assert.That(value.IsTrue(), Is.False);
    }

    [Test]
    public void IsTrue_NonEmptyString_ReturnsTrue()
    {
        var value = BasicValue.FromString("X");
        Assert.That(value.IsTrue(), Is.True);
    }

    [Test]
    public void IsTrue_EmptyString_ReturnsFalse()
    {
        var value = BasicValue.FromString("");
        Assert.That(value.IsTrue(), Is.False);
    }

    [Test]
    public void AsInteger_TruncatesTowardZero()
    {
        Assert.That(BasicValue.FromNumber(3.7).AsInteger(), Is.EqualTo(3));
        Assert.That(BasicValue.FromNumber(-3.7).AsInteger(), Is.EqualTo(-3));
    }

    [Test]
    public void AsString_FormatsIntegersWithoutDecimal()
    {
        var value = BasicValue.FromNumber(42);
        Assert.That(value.AsString(), Is.EqualTo("42"));
    }
}
