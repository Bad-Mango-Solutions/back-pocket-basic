// <copyright file="BasicValueTests.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Tests;

using Interpreter.Runtime;

/// <summary>
/// Contains unit tests for the <see cref="BasicValue"/> struct,
/// ensuring its functionality and behavior are correct for various operations
/// such as arithmetic, equality, and type conversions.
/// </summary>
/// <remarks>
/// This test class verifies the correctness of the <see cref="BasicValue"/> struct
/// when handling numeric and string values, performing arithmetic operations,
/// and evaluating logical conditions.
/// </remarks>
[TestFixture]
public class BasicValueTests
{
    /// <summary>
    /// Verifies that the <see cref="BasicValue.FromNumber(double)"/> method correctly creates
    /// a numeric <see cref="BasicValue"/> instance.
    /// </summary>
    /// <remarks>
    /// This test ensures that the <see cref="BasicValue"/> instance created from a numeric value:
    /// <list type="bullet">
    /// <item><description>Is identified as numeric.</description></item>
    /// <item><description>Is not identified as a string.</description></item>
    /// <item><description>Returns the correct numeric value when <see cref="BasicValue.AsNumber"/> is called.</description></item>
    /// </list>
    /// </remarks>
    [Test]
    public void FromNumber_CreatesNumericValue()
    {
        var value = BasicValue.FromNumber(42);

        Assert.That(value.IsNumeric, Is.True);
        Assert.That(value.IsString, Is.False);
        Assert.That(value.AsNumber(), Is.EqualTo(42));
    }

    /// <summary>
    /// Verifies that the <see cref="BasicValue.FromString(string)"/> method correctly creates
    /// a <see cref="BasicValue"/> instance representing a string value.
    /// </summary>
    /// <remarks>
    /// This test ensures that the created <see cref="BasicValue"/> instance has its
    /// <see cref="BasicValue.IsString"/> property set to <c>true</c>, its
    /// <see cref="BasicValue.IsNumeric"/> property set to <c>false</c>, and its
    /// string representation matches the input string.
    /// </remarks>
    [Test]
    public void FromString_CreatesStringValue()
    {
        var value = BasicValue.FromString("HELLO");

        Assert.That(value.IsString, Is.True);
        Assert.That(value.IsNumeric, Is.False);
        Assert.That(value.AsString(), Is.EqualTo("HELLO"));
    }

    /// <summary>
    /// Tests the addition operation for numeric values using the <see cref="BasicValue"/> struct.
    /// </summary>
    /// <remarks>
    /// This test verifies that the addition operator correctly adds two numeric
    /// <see cref="BasicValue"/> instances and produces the expected result.
    /// </remarks>
    /// <example>
    /// Given two numeric values, 10 and 20, represented as <see cref="BasicValue"/> instances:
    /// <code>
    /// var a = BasicValue.FromNumber(10);
    /// var b = BasicValue.FromNumber(20);
    /// var result = a + b;
    /// Assert.That(result.AsNumber(), Is.EqualTo(30));
    /// </code>
    /// The result of the addition is expected to be 30.
    /// </example>
    [Test]
    public void Addition_Numbers_AddsCorrectly()
    {
        var a = BasicValue.FromNumber(10);
        var b = BasicValue.FromNumber(20);

        var result = a + b;

        Assert.That(result.AsNumber(), Is.EqualTo(30));
    }

    /// <summary>
    /// Verifies that the addition operator for <see cref="BasicValue"/> correctly concatenates
    /// two string values.
    /// </summary>
    /// <remarks>
    /// This test ensures that when two <see cref="BasicValue"/> instances containing string values
    /// are added using the <c>+</c> operator, the resulting <see cref="BasicValue"/> contains the
    /// concatenated string.
    /// </remarks>
    /// <example>
    /// For example, adding "HELLO" and " WORLD" results in "HELLO WORLD".
    /// </example>
    [Test]
    public void Addition_Strings_Concatenates()
    {
        var a = BasicValue.FromString("HELLO");
        var b = BasicValue.FromString(" WORLD");

        var result = a + b;

        Assert.That(result.AsString(), Is.EqualTo("HELLO WORLD"));
    }

    /// <summary>
    /// Verifies that the subtraction operation between two numeric <see cref="BasicValue"/> instances
    /// produces the correct result.
    /// </summary>
    /// <remarks>
    /// This test ensures that the subtraction operator for <see cref="BasicValue"/> correctly computes
    /// the difference between two numeric values and returns the expected result.
    /// </remarks>
    /// <example>
    /// For example, subtracting 10 from 30 using <see cref="BasicValue"/> should yield 20.
    /// </example>
    [Test]
    public void Subtraction_Numbers_SubtractsCorrectly()
    {
        var a = BasicValue.FromNumber(30);
        var b = BasicValue.FromNumber(10);

        var result = a - b;

        Assert.That(result.AsNumber(), Is.EqualTo(20));
    }

    /// <summary>
    /// Verifies that the multiplication operation between two numeric <see cref="BasicValue"/> instances
    /// produces the correct result.
    /// </summary>
    /// <remarks>
    /// This test ensures that the <c>*</c> operator correctly multiplies two numeric values
    /// represented by <see cref="BasicValue"/> instances and returns the expected numeric result.
    /// </remarks>
    /// <example>
    /// Given two numeric <see cref="BasicValue"/> instances, <c>a</c> and <c>b</c>,
    /// where <c>a = 5</c> and <c>b = 4</c>, the result of <c>a * b</c> should be <c>20</c>.
    /// </example>
    [Test]
    public void Multiplication_Numbers_MultipliesCorrectly()
    {
        var a = BasicValue.FromNumber(5);
        var b = BasicValue.FromNumber(4);

        var result = a * b;

        Assert.That(result.AsNumber(), Is.EqualTo(20));
    }

    /// <summary>
    /// Verifies that the division operation between two numeric <see cref="BasicValue"/> instances
    /// produces the correct result.
    /// </summary>
    /// <remarks>
    /// This test ensures that dividing one numeric <see cref="BasicValue"/> by another
    /// yields the expected numeric result. For example, dividing 20 by 4 should return 5.
    /// </remarks>
    /// <example>
    /// <code>
    /// var a = BasicValue.FromNumber(20);
    /// var b = BasicValue.FromNumber(4);
    /// var result = a / b;
    /// Assert.That(result.AsNumber(), Is.EqualTo(5));
    /// </code>
    /// </example>
    [Test]
    public void Division_Numbers_DividesCorrectly()
    {
        var a = BasicValue.FromNumber(20);
        var b = BasicValue.FromNumber(4);

        var result = a / b;

        Assert.That(result.AsNumber(), Is.EqualTo(5));
    }

    /// <summary>
    /// Verifies that dividing a <see cref="BasicValue"/> instance by zero
    /// throws a <see cref="BasicRuntimeException"/>.
    /// </summary>
    /// <remarks>
    /// This test ensures that the division operation in the <see cref="BasicValue"/> struct
    /// correctly handles division by zero by throwing the appropriate exception.
    /// </remarks>
    /// <exception cref="BasicRuntimeException">
    /// Thrown when attempting to divide a <see cref="BasicValue"/> by zero.
    /// </exception>
    [Test]
    public void Division_ByZero_ThrowsException()
    {
        var a = BasicValue.FromNumber(10);
        var b = BasicValue.FromNumber(0);

        Assert.Throws<BasicRuntimeException>(() => _ = a / b);
    }

    /// <summary>
    /// Tests the exponentiation operation for numeric values in the <see cref="BasicValue"/> struct.
    /// </summary>
    /// <remarks>
    /// This test verifies that the power operator (<c>^</c>) correctly computes the result
    /// of raising one numeric <see cref="BasicValue"/> to the power of another.
    /// </remarks>
    /// <example>
    /// For example, raising 2 to the power of 3 should result in 8.
    /// </example>
    [Test]
    public void Power_Numbers_ComputesCorrectly()
    {
        var a = BasicValue.FromNumber(2);
        var b = BasicValue.FromNumber(3);

        var result = a ^ b;

        Assert.That(result.AsNumber(), Is.EqualTo(8));
    }

    /// <summary>
    /// Verifies that the negation operator correctly negates a numeric <see cref="BasicValue"/>.
    /// </summary>
    /// <remarks>
    /// This test ensures that applying the unary negation operator to a numeric <see cref="BasicValue"/>
    /// produces the expected negative value.
    /// </remarks>
    [Test]
    public void Negation_Number_NegatesCorrectly()
    {
        var value = BasicValue.FromNumber(5);

        var result = -value;

        Assert.That(result.AsNumber(), Is.EqualTo(-5));
    }

    /// <summary>
    /// Tests the equality operator of the <see cref="BasicValue"/> struct
    /// to ensure that two instances representing the same numeric value are considered equal.
    /// </summary>
    /// <remarks>
    /// This test verifies that the equality operator (==) correctly identifies two
    /// <see cref="BasicValue"/> instances initialized with the same numeric value as equal.
    /// </remarks>
    [Test]
    public void Equality_SameNumbers_ReturnsTrue()
    {
        var a = BasicValue.FromNumber(42);
        var b = BasicValue.FromNumber(42);

        Assert.That(a == b, Is.True);
    }

    /// <summary>
    /// Tests the equality operator (<c>==</c>) for <see cref="BasicValue"/> instances
    /// with different numeric values, ensuring it returns <c>false</c>.
    /// </summary>
    /// <remarks>
    /// This test verifies that the equality operator correctly identifies two
    /// <see cref="BasicValue"/> instances with different numeric values as not equal.
    /// </remarks>
    [Test]
    public void Equality_DifferentNumbers_ReturnsFalse()
    {
        var a = BasicValue.FromNumber(42);
        var b = BasicValue.FromNumber(43);

        Assert.That(a == b, Is.False);
    }

    /// <summary>
    /// Verifies that two <see cref="BasicValue"/> instances created from identical string values
    /// are considered equal using the equality operator.
    /// </summary>
    /// <remarks>
    /// This test ensures that the equality operator correctly identifies two <see cref="BasicValue"/>
    /// instances with the same string value as equal.
    /// </remarks>
    [Test]
    public void Equality_SameStrings_ReturnsTrue()
    {
        var a = BasicValue.FromString("HELLO");
        var b = BasicValue.FromString("HELLO");

        Assert.That(a == b, Is.True);
    }

    /// <summary>
    /// Verifies that the less-than comparison operator (<c>&lt;</c>) for <see cref="BasicValue"/> instances
    /// correctly evaluates numeric values.
    /// </summary>
    /// <remarks>
    /// This test ensures that the less-than operator behaves as expected when comparing two numeric
    /// <see cref="BasicValue"/> instances. It checks both true and false outcomes for the comparison.
    /// </remarks>
    /// <example>
    /// For example:
    /// <code>
    /// var a = BasicValue.FromNumber(5);
    /// var b = BasicValue.FromNumber(10);
    /// Assert.That(a &lt; b, Is.True);
    /// Assert.That(b &lt; a, Is.False);
    /// </code>
    /// </example>
    [Test]
    public void LessThan_Numbers_ComparesCorrectly()
    {
        var a = BasicValue.FromNumber(5);
        var b = BasicValue.FromNumber(10);

        Assert.That(a < b, Is.True);
        Assert.That(b < a, Is.False);
    }

    /// <summary>
    /// Tests the greater-than comparison operator for numeric values in the <see cref="BasicValue"/> struct.
    /// </summary>
    /// <remarks>
    /// This test verifies that the greater-than operator (<c>&gt;</c>) correctly evaluates the relationship
    /// between two numeric <see cref="BasicValue"/> instances. It ensures that the operator returns <see langword="true"/>
    /// when the left-hand operand is greater than the right-hand operand, and <see langword="false"/> otherwise.
    /// </remarks>
    /// <example>
    /// <code>
    /// var a = BasicValue.FromNumber(10);
    /// var b = BasicValue.FromNumber(5);
    /// Assert.That(a &gt; b, Is.True);
    /// Assert.That(b &gt; a, Is.False);
    /// </code>
    /// </example>
    [Test]
    public void GreaterThan_Numbers_ComparesCorrectly()
    {
        var a = BasicValue.FromNumber(10);
        var b = BasicValue.FromNumber(5);

        Assert.That(a > b, Is.True);
        Assert.That(b > a, Is.False);
    }

    /// <summary>
    /// Verifies that the <see cref="BasicValue.IsTrue"/> method correctly evaluates
    /// a non-zero numeric value as <c>true</c>.
    /// </summary>
    /// <remarks>
    /// This test ensures that numeric values other than zero are treated as logically true
    /// when evaluated using the <see cref="BasicValue.IsTrue"/> method.
    /// </remarks>
    [Test]
    public void IsTrue_NonZeroNumber_ReturnsTrue()
    {
        var value = BasicValue.FromNumber(1);
        Assert.That(value.IsTrue(), Is.True);
    }

    /// <summary>
    /// Verifies that the <see cref="BasicValue.IsTrue"/> method correctly returns <c>false</c>
    /// when the value is a numeric zero.
    /// </summary>
    /// <remarks>
    /// This test ensures that the <see cref="BasicValue"/> struct's logical evaluation
    /// treats a numeric zero as a "false" value, consistent with expected behavior.
    /// </remarks>
    [Test]
    public void IsTrue_ZeroNumber_ReturnsFalse()
    {
        var value = BasicValue.FromNumber(0);
        Assert.That(value.IsTrue(), Is.False);
    }

    /// <summary>
    /// Verifies that the <see cref="BasicValue.IsTrue"/> method correctly evaluates
    /// a non-empty string as <c>true</c>.
    /// </summary>
    /// <remarks>
    /// This test ensures that when a <see cref="BasicValue"/> instance is initialized
    /// with a non-empty string, the <see cref="BasicValue.IsTrue"/> method returns <c>true</c>.
    /// </remarks>
    [Test]
    public void IsTrue_NonEmptyString_ReturnsTrue()
    {
        var value = BasicValue.FromString("X");
        Assert.That(value.IsTrue(), Is.True);
    }

    /// <summary>
    /// Verifies that the <see cref="BasicValue.IsTrue"/> method correctly evaluates an empty string
    /// and returns <c>false</c>.
    /// </summary>
    /// <remarks>
    /// This test ensures that an instance of <see cref="BasicValue"/> created from an empty string
    /// is evaluated as logically false by the <see cref="BasicValue.IsTrue"/> method.
    /// </remarks>
    [Test]
    public void IsTrue_EmptyString_ReturnsFalse()
    {
        var value = BasicValue.FromString(string.Empty);
        Assert.That(value.IsTrue(), Is.False);
    }

    /// <summary>
    /// Verifies that the <see cref="BasicValue.AsInteger"/> method correctly truncates
    /// numeric values toward zero when converting them to integers.
    /// </summary>
    /// <remarks>
    /// This test ensures that positive and negative numeric values are truncated
    /// toward zero, aligning with the behavior of Applesoft BASIC.
    /// </remarks>
    /// <example>
    /// For example:
    /// <code>
    /// Assert.That(BasicValue.FromNumber(3.7).AsInteger(), Is.EqualTo(3));
    /// Assert.That(BasicValue.FromNumber(-3.7).AsInteger(), Is.EqualTo(-3));
    /// </code>
    /// </example>
    [Test]
    public void AsInteger_TruncatesTowardZero()
    {
        Assert.That(BasicValue.FromNumber(3.7).AsInteger(), Is.EqualTo(3));
        Assert.That(BasicValue.FromNumber(-3.7).AsInteger(), Is.EqualTo(-3));
    }

    /// <summary>
    /// Verifies that the <see cref="BasicValue.AsString"/> method formats integer values
    /// without including a decimal point or fractional part.
    /// </summary>
    /// <remarks>
    /// This test ensures that when a numeric <see cref="BasicValue"/> instance is converted
    /// to a string using the <see cref="BasicValue.AsString"/> method, the result is a
    /// string representation of the integer without any decimal places.
    /// </remarks>
    [Test]
    public void AsString_FormatsIntegersWithoutDecimal()
    {
        var value = BasicValue.FromNumber(42);
        Assert.That(value.AsString(), Is.EqualTo("42"));
    }

    /// <summary>
    /// Verifies that <see cref="BasicValue.ApproximatelyEquals"/> returns true for identical values.
    /// </summary>
    [Test]
    public void ApproximatelyEquals_IdenticalValues_ReturnsTrue()
    {
        var a = BasicValue.FromNumber(42.0);
        var b = BasicValue.FromNumber(42.0);

        Assert.That(a.ApproximatelyEquals(b), Is.True);
    }

    /// <summary>
    /// Verifies that <see cref="BasicValue.ApproximatelyEquals"/> returns true for values
    /// within epsilon tolerance (1e-10).
    /// </summary>
    [Test]
    public void ApproximatelyEquals_WithinEpsilon_ReturnsTrue()
    {
        var a = BasicValue.FromNumber(1.0);
        var b = BasicValue.FromNumber(1.0 + 5e-11); // Well within epsilon

        Assert.That(a.ApproximatelyEquals(b), Is.True);
    }

    /// <summary>
    /// Verifies that <see cref="BasicValue.ApproximatelyEquals"/> returns false for values
    /// that differ by more than epsilon tolerance.
    /// </summary>
    [Test]
    public void ApproximatelyEquals_BeyondEpsilon_ReturnsFalse()
    {
        var a = BasicValue.FromNumber(1.0);
        var b = BasicValue.FromNumber(1.0 + 2e-10); // Beyond epsilon (1e-10)

        Assert.That(a.ApproximatelyEquals(b), Is.False);
    }

    /// <summary>
    /// Verifies that <see cref="BasicValue.ApproximatelyEquals"/> correctly handles values
    /// at the epsilon boundary (1e-10).
    /// </summary>
    [Test]
    public void ApproximatelyEquals_AtEpsilonBoundary_ReturnsTrue()
    {
        var a = BasicValue.FromNumber(1.0);
        var b = BasicValue.FromNumber(1.0 + 9e-11); // Just within epsilon tolerance

        Assert.That(a.ApproximatelyEquals(b), Is.True);
    }

    /// <summary>
    /// Verifies that <see cref="BasicValue.ApproximatelyEquals"/> works correctly with
    /// very large numbers using relative epsilon.
    /// </summary>
    [Test]
    public void ApproximatelyEquals_LargeNumbers_UsesRelativeEpsilon()
    {
        var a = BasicValue.FromNumber(1e15);
        var b = BasicValue.FromNumber(1e15 + 1e5); // Within relative epsilon

        Assert.That(a.ApproximatelyEquals(b), Is.True);
    }

    /// <summary>
    /// Verifies that <see cref="BasicValue.ApproximatelyEquals"/> returns false for
    /// large numbers that differ beyond relative epsilon.
    /// </summary>
    [Test]
    public void ApproximatelyEquals_LargeNumbersBeyondRelativeEpsilon_ReturnsFalse()
    {
        var a = BasicValue.FromNumber(1e15);
        var b = BasicValue.FromNumber(1e15 + 2e6); // Beyond relative epsilon

        Assert.That(a.ApproximatelyEquals(b), Is.False);
    }

    /// <summary>
    /// Verifies that <see cref="BasicValue.ApproximatelyEquals"/> correctly handles
    /// very small numbers near zero using absolute epsilon.
    /// </summary>
    [Test]
    public void ApproximatelyEquals_VerySmallNumbers_UsesAbsoluteEpsilon()
    {
        var a = BasicValue.FromNumber(1e-11);
        var b = BasicValue.FromNumber(2e-11);

        Assert.That(a.ApproximatelyEquals(b), Is.True); // Both within epsilon of 0
    }

    /// <summary>
    /// Verifies that <see cref="BasicValue.ApproximatelyEquals"/> returns true for
    /// mixed magnitude comparisons within tolerance.
    /// </summary>
    [Test]
    public void ApproximatelyEquals_MixedMagnitudes_WithinTolerance()
    {
        var a = BasicValue.FromNumber(1000.0);
        var b = BasicValue.FromNumber(1000.0 + 5e-8); // Within relative epsilon

        Assert.That(a.ApproximatelyEquals(b), Is.True);
    }

    /// <summary>
    /// Verifies that <see cref="BasicValue.ApproximatelyEquals"/> returns true when
    /// comparing identical string values.
    /// </summary>
    [Test]
    public void ApproximatelyEquals_IdenticalStrings_ReturnsTrue()
    {
        var a = BasicValue.FromString("HELLO");
        var b = BasicValue.FromString("HELLO");

        Assert.That(a.ApproximatelyEquals(b), Is.True);
    }

    /// <summary>
    /// Verifies that <see cref="BasicValue.ApproximatelyEquals"/> returns false when
    /// comparing different string values.
    /// </summary>
    [Test]
    public void ApproximatelyEquals_DifferentStrings_ReturnsFalse()
    {
        var a = BasicValue.FromString("HELLO");
        var b = BasicValue.FromString("WORLD");

        Assert.That(a.ApproximatelyEquals(b), Is.False);
    }

    /// <summary>
    /// Verifies that <see cref="BasicValue.ApproximatelyEquals"/> handles mixed string/numeric
    /// comparisons by converting strings to numbers.
    /// </summary>
    [Test]
    public void ApproximatelyEquals_StringAndNumeric_ConvertsAndCompares()
    {
        var a = BasicValue.FromString("42");
        var b = BasicValue.FromNumber(42.0);

        Assert.That(a.ApproximatelyEquals(b), Is.True);
    }

    /// <summary>
    /// Verifies that <see cref="BasicValue.ApproximatelyEquals"/> treats non-numeric strings
    /// as zero when compared with numeric values.
    /// </summary>
    [Test]
    public void ApproximatelyEquals_NonNumericStringAndZero_ReturnsTrue()
    {
        var a = BasicValue.FromString("ABC");
        var b = BasicValue.FromNumber(0.0);

        Assert.That(a.ApproximatelyEquals(b), Is.True); // "ABC" converts to 0
    }

    /// <summary>
    /// Verifies that <see cref="BasicValue.ApproximatelyEquals"/> is symmetric:
    /// a.ApproximatelyEquals(b) == b.ApproximatelyEquals(a).
    /// </summary>
    [Test]
    public void ApproximatelyEquals_IsSymmetric()
    {
        var a = BasicValue.FromNumber(1.0);
        var b = BasicValue.FromNumber(1.0 + 5e-11);

        Assert.That(a.ApproximatelyEquals(b), Is.EqualTo(b.ApproximatelyEquals(a)));
    }

    /// <summary>
    /// Verifies that <see cref="BasicValue.ApproximatelyEquals"/> maintains consistency
    /// with exact equality for values that are exactly equal.
    /// </summary>
    [Test]
    public void ApproximatelyEquals_ExactlyEqualValues_ConsistentWithOperator()
    {
        var a = BasicValue.FromNumber(42.0);
        var b = BasicValue.FromNumber(42.0);

        Assert.That(a.ApproximatelyEquals(b), Is.True);
        Assert.That(a == b, Is.True);
    }

    /// <summary>
    /// Verifies that <see cref="BasicValue.ApproximatelyEquals"/> and the == operator
    /// can differ for values within epsilon tolerance.
    /// </summary>
    [Test]
    public void ApproximatelyEquals_DiffersFromOperator_ForEpsilonDifferences()
    {
        var a = BasicValue.FromNumber(1.0);
        var b = BasicValue.FromNumber(1.0 + 5e-11);

        Assert.That(a.ApproximatelyEquals(b), Is.True);
        Assert.That(a == b, Is.False); // Exact equality is false
    }

    /// <summary>
    /// Verifies that <see cref="BasicValue.ApproximatelyEquals"/> correctly handles
    /// negative numbers within epsilon tolerance.
    /// </summary>
    [Test]
    public void ApproximatelyEquals_NegativeNumbers_WithinEpsilon()
    {
        var a = BasicValue.FromNumber(-1.0);
        var b = BasicValue.FromNumber(-1.0 - 5e-11);

        Assert.That(a.ApproximatelyEquals(b), Is.True);
    }

    /// <summary>
    /// Verifies that <see cref="BasicValue.ApproximatelyEquals"/> correctly handles
    /// comparisons across zero.
    /// </summary>
    [Test]
    public void ApproximatelyEquals_AcrossZero_WithinEpsilon()
    {
        var a = BasicValue.FromNumber(5e-11);
        var b = BasicValue.FromNumber(-5e-11);

        Assert.That(a.ApproximatelyEquals(b), Is.True); // Both within epsilon of 0
    }
}