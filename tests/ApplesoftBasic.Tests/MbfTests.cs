// <copyright file="MbfTests.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Tests;

using ApplesoftBasic.Interpreter.Emulation;

/// <summary>
/// Contains comprehensive unit tests for the <see cref="MBF"/> struct,
/// which represents Microsoft Binary Format floating-point numbers as used
/// in Applesoft BASIC and the Apple II.
/// </summary>
[TestFixture]
public class MbfTests
{
    #region Zero Value Tests

    /// <summary>
    /// Verifies that MBF.Zero returns a zero value.
    /// </summary>
    [Test]
    public void Zero_ReturnsZeroValue()
    {
        MBF zero = MBF.Zero;

        Assert.That(zero.IsZero, Is.True);
        Assert.That(zero.Exponent, Is.EqualTo(0));
        Assert.That((double)zero, Is.EqualTo(0.0));
    }

    /// <summary>
    /// Verifies that converting 0.0 to MBF produces a zero value.
    /// </summary>
    [Test]
    public void FromDouble_Zero_ReturnsZeroMbf()
    {
        MBF result = MBF.FromDouble(0.0);

        Assert.That(result.IsZero, Is.True);
        Assert.That(result.Exponent, Is.EqualTo(0));
    }

    /// <summary>
    /// Verifies that converting negative zero to MBF produces a zero value.
    /// </summary>
    [Test]
    public void FromDouble_NegativeZero_ReturnsZeroMbf()
    {
        MBF result = MBF.FromDouble(-0.0);

        Assert.That(result.IsZero, Is.True);
        Assert.That(result.Exponent, Is.EqualTo(0));
    }

    #endregion

    #region Positive Value Conversion Tests

    /// <summary>
    /// Verifies round-trip conversion of positive integers.
    /// </summary>
    /// <param name="value">The test value.</param>
    [TestCase(1.0)]
    [TestCase(2.0)]
    [TestCase(42.0)]
    [TestCase(100.0)]
    [TestCase(1000.0)]
    [TestCase(1000000.0)]
    public void FromDouble_PositiveInteger_RoundTripsCorrectly(double value)
    {
        MBF mbf = MBF.FromDouble(value);
        double result = mbf.ToDouble();

        Assert.That(result, Is.EqualTo(value).Within(1e-7 * Math.Abs(value)));
        Assert.That(mbf.IsNegative, Is.False);
    }

    /// <summary>
    /// Verifies round-trip conversion of positive fractions.
    /// </summary>
    /// <param name="value">The test value.</param>
    [TestCase(0.5)]
    [TestCase(0.25)]
    [TestCase(0.125)]
    [TestCase(0.1)]
    [TestCase(0.01)]
    [TestCase(0.001)]
    public void FromDouble_PositiveFraction_RoundTripsCorrectly(double value)
    {
        MBF mbf = MBF.FromDouble(value);
        double result = mbf.ToDouble();

        Assert.That(result, Is.EqualTo(value).Within(1e-7 * Math.Abs(value)));
        Assert.That(mbf.IsNegative, Is.False);
    }

    /// <summary>
    /// Verifies round-trip conversion of Pi.
    /// </summary>
    [Test]
    public void FromDouble_Pi_RoundTripsCorrectly()
    {
        MBF mbf = MBF.FromDouble(Math.PI);
        double result = mbf.ToDouble();

        Assert.That(result, Is.EqualTo(Math.PI).Within(1e-7));
        Assert.That(mbf.IsNegative, Is.False);
    }

    /// <summary>
    /// Verifies round-trip conversion of E.
    /// </summary>
    [Test]
    public void FromDouble_E_RoundTripsCorrectly()
    {
        MBF mbf = MBF.FromDouble(Math.E);
        double result = mbf.ToDouble();

        Assert.That(result, Is.EqualTo(Math.E).Within(1e-7));
        Assert.That(mbf.IsNegative, Is.False);
    }

    #endregion

    #region Negative Value Conversion Tests

    /// <summary>
    /// Verifies round-trip conversion of negative integers.
    /// </summary>
    /// <param name="value">The test value.</param>
    [TestCase(-1.0)]
    [TestCase(-2.0)]
    [TestCase(-42.0)]
    [TestCase(-100.0)]
    [TestCase(-1000.0)]
    [TestCase(-1000000.0)]
    public void FromDouble_NegativeInteger_RoundTripsCorrectly(double value)
    {
        MBF mbf = MBF.FromDouble(value);
        double result = mbf.ToDouble();

        Assert.That(result, Is.EqualTo(value).Within(1e-7 * Math.Abs(value)));
        Assert.That(mbf.IsNegative, Is.True);
    }

    /// <summary>
    /// Verifies round-trip conversion of negative fractions.
    /// </summary>
    /// <param name="value">The test value.</param>
    [TestCase(-0.5)]
    [TestCase(-0.25)]
    [TestCase(-0.125)]
    [TestCase(-0.1)]
    [TestCase(-0.01)]
    [TestCase(-0.001)]
    public void FromDouble_NegativeFraction_RoundTripsCorrectly(double value)
    {
        MBF mbf = MBF.FromDouble(value);
        double result = mbf.ToDouble();

        Assert.That(result, Is.EqualTo(value).Within(1e-7 * Math.Abs(value)));
        Assert.That(mbf.IsNegative, Is.True);
    }

    /// <summary>
    /// Verifies round-trip conversion of negative Pi.
    /// </summary>
    [Test]
    public void FromDouble_NegativePi_RoundTripsCorrectly()
    {
        MBF mbf = MBF.FromDouble(-Math.PI);
        double result = mbf.ToDouble();

        Assert.That(result, Is.EqualTo(-Math.PI).Within(1e-7));
        Assert.That(mbf.IsNegative, Is.True);
    }

    #endregion

    #region Special Value Tests

    /// <summary>
    /// Verifies that positive infinity throws OverflowException.
    /// </summary>
    [Test]
    public void FromDouble_PositiveInfinity_ThrowsOverflowException()
    {
        Assert.Throws<OverflowException>(() => MBF.FromDouble(double.PositiveInfinity));
    }

    /// <summary>
    /// Verifies that negative infinity throws OverflowException.
    /// </summary>
    [Test]
    public void FromDouble_NegativeInfinity_ThrowsOverflowException()
    {
        Assert.Throws<OverflowException>(() => MBF.FromDouble(double.NegativeInfinity));
    }

    /// <summary>
    /// Verifies that NaN throws OverflowException.
    /// </summary>
    [Test]
    public void FromDouble_NaN_ThrowsOverflowException()
    {
        Assert.Throws<OverflowException>(() => MBF.FromDouble(double.NaN));
    }

    /// <summary>
    /// Verifies that very large values throw OverflowException.
    /// </summary>
    [Test]
    public void FromDouble_VeryLargeValue_ThrowsOverflowException()
    {
        // MBF exponent max is 255, which is about 2^127
        double veryLarge = Math.Pow(2, 128);
        Assert.Throws<OverflowException>(() => MBF.FromDouble(veryLarge));
    }

    /// <summary>
    /// Verifies that very small values underflow to zero.
    /// </summary>
    [Test]
    public void FromDouble_VerySmallValue_UnderflowsToZero()
    {
        // MBF exponent min is 1, which is about 2^(-127)
        double verySmall = Math.Pow(2, -150);
        MBF result = MBF.FromDouble(verySmall);

        Assert.That(result.IsZero, Is.True);
    }

    #endregion

    #region Implicit Conversion Tests

    /// <summary>
    /// Verifies implicit conversion from double to MBF.
    /// </summary>
    [Test]
    public void ImplicitConversion_DoubleToMbf_Works()
    {
        MBF mbf = 3.14159;

        Assert.That(mbf.IsZero, Is.False);
        Assert.That(mbf.ToDouble(), Is.EqualTo(3.14159).Within(1e-7));
    }

    /// <summary>
    /// Verifies implicit conversion from MBF to double.
    /// </summary>
    [Test]
    public void ImplicitConversion_MbfToDouble_Works()
    {
        MBF mbf = MBF.FromDouble(3.14159);
        double result = mbf;

        Assert.That(result, Is.EqualTo(3.14159).Within(1e-7));
    }

    /// <summary>
    /// Verifies implicit conversion from float to MBF.
    /// </summary>
    [Test]
    public void ImplicitConversion_FloatToMbf_Works()
    {
        MBF mbf = 3.14159f;

        Assert.That(mbf.IsZero, Is.False);
        Assert.That(mbf.ToFloat(), Is.EqualTo(3.14159f).Within(1e-5f));
    }

    /// <summary>
    /// Verifies implicit conversion from MBF to float.
    /// </summary>
    [Test]
    public void ImplicitConversion_MbfToFloat_Works()
    {
        MBF mbf = MBF.FromFloat(3.14159f);
        float result = mbf;

        Assert.That(result, Is.EqualTo(3.14159f).Within(1e-5f));
    }

    #endregion

    #region Byte Array Tests

    /// <summary>
    /// Verifies ToBytes returns correct byte count.
    /// </summary>
    [Test]
    public void ToBytes_ReturnsCorrectLength()
    {
        MBF mbf = MBF.FromDouble(42.0);
        byte[] bytes = mbf.ToBytes();

        Assert.That(bytes.Length, Is.EqualTo(MBF.ByteSize));
        Assert.That(bytes.Length, Is.EqualTo(5));
    }

    /// <summary>
    /// Verifies FromBytes with null throws ArgumentNullException.
    /// </summary>
    [Test]
    public void FromBytes_NullArray_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => MBF.FromBytes(null!));
    }

    /// <summary>
    /// Verifies FromBytes with too few bytes throws ArgumentException.
    /// </summary>
    [Test]
    public void FromBytes_TooFewBytes_ThrowsArgumentException()
    {
        byte[] shortArray = new byte[] { 0x00, 0x00, 0x00, 0x00 };
        Assert.Throws<ArgumentException>(() => MBF.FromBytes(shortArray));
    }

    /// <summary>
    /// Verifies round-trip through bytes.
    /// </summary>
    /// <param name="value">The test value.</param>
    [TestCase(0.0)]
    [TestCase(1.0)]
    [TestCase(-1.0)]
    [TestCase(42.0)]
    [TestCase(-42.0)]
    [TestCase(0.5)]
    [TestCase(-0.5)]
    public void BytesRoundTrip_PreservesValue(double value)
    {
        MBF original = MBF.FromDouble(value);
        byte[] bytes = original.ToBytes();
        MBF restored = MBF.FromBytes(bytes);

        Assert.That(restored.ToDouble(), Is.EqualTo(original.ToDouble()));
    }

    #endregion

    #region Equality Tests

    /// <summary>
    /// Verifies that equal MBF values are equal.
    /// </summary>
    [Test]
    public void Equals_EqualValues_ReturnsTrue()
    {
        MBF a = MBF.FromDouble(42.0);
        MBF b = MBF.FromDouble(42.0);

        Assert.That(a.Equals(b), Is.True);
        Assert.That(a == b, Is.True);
        Assert.That(a != b, Is.False);
    }

    /// <summary>
    /// Verifies that unequal MBF values are not equal.
    /// </summary>
    [Test]
    public void Equals_UnequalValues_ReturnsFalse()
    {
        MBF a = MBF.FromDouble(42.0);
        MBF b = MBF.FromDouble(43.0);

        Assert.That(a.Equals(b), Is.False);
        Assert.That(a == b, Is.False);
        Assert.That(a != b, Is.True);
    }

    /// <summary>
    /// Verifies that two zero values are equal regardless of mantissa bytes.
    /// </summary>
    [Test]
    public void Equals_TwoZeros_ReturnsTrue()
    {
        MBF a = MBF.Zero;
        MBF b = MBF.FromDouble(0.0);

        Assert.That(a.Equals(b), Is.True);
        Assert.That(a == b, Is.True);
    }

    /// <summary>
    /// Verifies that Equals with object works correctly.
    /// </summary>
    [Test]
    public void Equals_WithObject_WorksCorrectly()
    {
        MBF mbf = MBF.FromDouble(42.0);
        object obj = MBF.FromDouble(42.0);
        object notMbf = "not an MBF";
        object? nullObj = null;

        Assert.That(mbf.Equals(obj), Is.True);
        Assert.That(mbf.Equals(notMbf), Is.False);
        Assert.That(mbf.Equals(nullObj), Is.False);
    }

    /// <summary>
    /// Verifies that GetHashCode returns same value for equal MBF values.
    /// </summary>
    [Test]
    public void GetHashCode_EqualValues_ReturnsSameHashCode()
    {
        MBF a = MBF.FromDouble(42.0);
        MBF b = MBF.FromDouble(42.0);

        Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
    }

    /// <summary>
    /// Verifies that GetHashCode returns 0 for zero values.
    /// </summary>
    [Test]
    public void GetHashCode_Zero_ReturnsZero()
    {
        MBF zero = MBF.Zero;

        Assert.That(zero.GetHashCode(), Is.EqualTo(0));
    }

    #endregion

    #region Properties Tests

    /// <summary>
    /// Verifies IsZero property for various values.
    /// </summary>
    /// <param name="value">The test value.</param>
    /// <param name="expectedIsZero">Expected IsZero result.</param>
    [TestCase(0.0, true)]
    [TestCase(1.0, false)]
    [TestCase(-1.0, false)]
    [TestCase(0.001, false)]
    [TestCase(-0.001, false)]
    public void IsZero_ReturnsCorrectValue(double value, bool expectedIsZero)
    {
        MBF mbf = MBF.FromDouble(value);

        Assert.That(mbf.IsZero, Is.EqualTo(expectedIsZero));
    }

    /// <summary>
    /// Verifies IsNegative property for various values.
    /// </summary>
    /// <param name="value">The test value.</param>
    /// <param name="expectedIsNegative">Expected IsNegative result.</param>
    [TestCase(0.0, false)]
    [TestCase(1.0, false)]
    [TestCase(-1.0, true)]
    [TestCase(0.001, false)]
    [TestCase(-0.001, true)]
    public void IsNegative_ReturnsCorrectValue(double value, bool expectedIsNegative)
    {
        MBF mbf = MBF.FromDouble(value);

        Assert.That(mbf.IsNegative, Is.EqualTo(expectedIsNegative));
    }

    /// <summary>
    /// Verifies that zero is not considered negative.
    /// </summary>
    [Test]
    public void IsNegative_Zero_ReturnsFalse()
    {
        MBF zero = MBF.Zero;

        Assert.That(zero.IsNegative, Is.False);
    }

    /// <summary>
    /// Verifies ExponentBias constant value.
    /// </summary>
    [Test]
    public void ExponentBias_HasCorrectValue()
    {
        Assert.That(MBF.ExponentBias, Is.EqualTo(128));
    }

    /// <summary>
    /// Verifies ByteSize constant value.
    /// </summary>
    [Test]
    public void ByteSize_HasCorrectValue()
    {
        Assert.That(MBF.ByteSize, Is.EqualTo(5));
    }

    #endregion

    #region ToString Tests

    /// <summary>
    /// Verifies ToString returns a meaningful string.
    /// </summary>
    [Test]
    public void ToString_ReturnsNonEmptyString()
    {
        MBF mbf = MBF.FromDouble(42.0);
        string result = mbf.ToString();

        Assert.That(result, Is.Not.Empty);
        Assert.That(result, Does.Contain("MBF"));
    }

    /// <summary>
    /// Verifies ToString for zero value.
    /// </summary>
    [Test]
    public void ToString_Zero_ReturnsZeroString()
    {
        MBF zero = MBF.Zero;
        string result = zero.ToString();

        Assert.That(result, Does.Contain("0"));
    }

    #endregion

    #region Constructor Tests

    /// <summary>
    /// Verifies constructor sets all bytes correctly.
    /// </summary>
    [Test]
    public void Constructor_SetsAllBytesCorrectly()
    {
        MBF mbf = new MBF(0x81, 0x00, 0x00, 0x00, 0x00);

        Assert.That(mbf.Exponent, Is.EqualTo(0x81));
        Assert.That(mbf.Mantissa1, Is.EqualTo(0x00));
        Assert.That(mbf.Mantissa2, Is.EqualTo(0x00));
        Assert.That(mbf.Mantissa3, Is.EqualTo(0x00));
        Assert.That(mbf.Mantissa4, Is.EqualTo(0x00));
    }

    #endregion

    #region Edge Case Tests

    /// <summary>
    /// Verifies handling of powers of 2.
    /// </summary>
    /// <param name="exponent">The power of 2 to test.</param>
    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(10)]
    [TestCase(20)]
    [TestCase(-1)]
    [TestCase(-2)]
    [TestCase(-10)]
    [TestCase(-20)]
    public void FromDouble_PowersOfTwo_RoundTripsCorrectly(int exponent)
    {
        double value = Math.Pow(2, exponent);
        MBF mbf = MBF.FromDouble(value);
        double result = mbf.ToDouble();

        Assert.That(result, Is.EqualTo(value).Within(1e-10 * Math.Abs(value)));
    }

    /// <summary>
    /// Verifies handling of maximum representable value (approximately).
    /// </summary>
    [Test]
    public void FromDouble_MaxRepresentableValue_DoesNotThrow()
    {
        // MBF can represent approximately 2^127
        double maxValue = Math.Pow(2, 126);
        MBF mbf = MBF.FromDouble(maxValue);

        Assert.That(mbf.IsZero, Is.False);
    }

    /// <summary>
    /// Verifies handling of minimum representable positive value.
    /// </summary>
    [Test]
    public void FromDouble_MinRepresentableValue_DoesNotThrow()
    {
        // MBF can represent approximately 2^(-127)
        double minValue = Math.Pow(2, -126);
        MBF mbf = MBF.FromDouble(minValue);

        Assert.That(mbf.IsZero, Is.False);
    }

    #endregion

    #region FromFloat Tests

    /// <summary>
    /// Verifies FromFloat works correctly.
    /// </summary>
    [Test]
    public void FromFloat_PositiveValue_RoundTripsCorrectly()
    {
        float value = 42.5f;
        MBF mbf = MBF.FromFloat(value);
        float result = mbf.ToFloat();

        Assert.That(result, Is.EqualTo(value).Within(1e-5f));
    }

    /// <summary>
    /// Verifies FromFloat works correctly with negative values.
    /// </summary>
    [Test]
    public void FromFloat_NegativeValue_RoundTripsCorrectly()
    {
        float value = -42.5f;
        MBF mbf = MBF.FromFloat(value);
        float result = mbf.ToFloat();

        Assert.That(result, Is.EqualTo(value).Within(1e-5f));
    }

    /// <summary>
    /// Verifies FromFloat with float infinity throws OverflowException.
    /// </summary>
    [Test]
    public void FromFloat_Infinity_ThrowsOverflowException()
    {
        Assert.Throws<OverflowException>(() => MBF.FromFloat(float.PositiveInfinity));
        Assert.Throws<OverflowException>(() => MBF.FromFloat(float.NegativeInfinity));
    }

    /// <summary>
    /// Verifies FromFloat with float NaN throws OverflowException.
    /// </summary>
    [Test]
    public void FromFloat_NaN_ThrowsOverflowException()
    {
        Assert.Throws<OverflowException>(() => MBF.FromFloat(float.NaN));
    }

    #endregion
}