// <copyright file="FacConverterTests.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Tests;

using ApplesoftBasic.Interpreter.Emulation;
using Microsoft.Extensions.Logging;
using Moq;

/// <summary>
/// Contains comprehensive unit tests for the <see cref="FacConverter"/> class,
/// which handles conversions between .NET floating-point types and Apple II
/// Floating-point ACcumulator (FAC) memory format.
/// </summary>
[TestFixture]
public class FacConverterTests
{
    #region DoubleToFacBytes Tests

    /// <summary>
    /// Verifies that converting zero produces the expected byte array.
    /// </summary>
    [Test]
    public void DoubleToFacBytes_Zero_ReturnsCorrectBytes()
    {
        byte[] result = FacConverter.DoubleToFacBytes(0.0);

        Assert.That(result.Length, Is.EqualTo(5));
        Assert.That(result[4], Is.EqualTo(0x00), "Sign byte should be 0x00 for zero");

        // IEEE 754 representation of 0.0f is all zeros
        double converted = BitConverter.ToSingle(result, 0);
        Assert.That(converted, Is.EqualTo(0.0));
    }

    /// <summary>
    /// Verifies that converting negative zero produces the expected byte array with sign byte 0xFF.
    /// </summary>
    [Test]
    public void DoubleToFacBytes_NegativeZero_ReturnsCorrectBytes()
    {
        byte[] result = FacConverter.DoubleToFacBytes(-0.0);

        Assert.That(result.Length, Is.EqualTo(5));
        Assert.That(result[4], Is.EqualTo(0xFF), "Sign byte should be 0xFF for negative zero");

        // IEEE 754 representation of -0.0f has sign bit set
        double converted = BitConverter.ToSingle(result, 0);
        Assert.That(converted, Is.EqualTo(0.0)); // Value is still zero
    }

    /// <summary>
    /// Verifies that converting a positive integer produces correct results.
    /// </summary>
    [Test]
    public void DoubleToFacBytes_PositiveInteger_ReturnsCorrectBytes()
    {
        byte[] result = FacConverter.DoubleToFacBytes(42.0);

        Assert.That(result.Length, Is.EqualTo(5));
        Assert.That(result[4], Is.EqualTo(0x00), "Sign byte should be 0x00 for positive");

        double converted = BitConverter.ToSingle(result, 0);
        Assert.That(converted, Is.EqualTo(42.0f));
    }

    /// <summary>
    /// Verifies that converting a negative integer produces correct results with sign byte.
    /// </summary>
    [Test]
    public void DoubleToFacBytes_NegativeInteger_ReturnsCorrectBytesWithSignByte()
    {
        byte[] result = FacConverter.DoubleToFacBytes(-42.0);

        Assert.That(result.Length, Is.EqualTo(5));
        Assert.That(result[4], Is.EqualTo(0xFF), "Sign byte should be 0xFF for negative");

        double converted = BitConverter.ToSingle(result, 0);
        Assert.That(converted, Is.EqualTo(-42.0f));
    }

    /// <summary>
    /// Verifies that converting Pi produces correct results.
    /// </summary>
    [Test]
    public void DoubleToFacBytes_Pi_ReturnsCorrectBytes()
    {
        byte[] result = FacConverter.DoubleToFacBytes(Math.PI);

        Assert.That(result.Length, Is.EqualTo(5));
        Assert.That(result[4], Is.EqualTo(0x00), "Sign byte should be 0x00 for positive");

        double converted = BitConverter.ToSingle(result, 0);
        Assert.That(converted, Is.EqualTo((float)Math.PI).Within(1e-6));
    }

    /// <summary>
    /// Verifies that converting a very small positive number works correctly.
    /// </summary>
    [Test]
    public void DoubleToFacBytes_VerySmallPositive_ReturnsCorrectBytes()
    {
        double smallValue = 1e-30;
        byte[] result = FacConverter.DoubleToFacBytes(smallValue);

        Assert.That(result[4], Is.EqualTo(0x00), "Sign byte should be 0x00 for positive");

        double converted = BitConverter.ToSingle(result, 0);
        Assert.That(converted, Is.EqualTo((float)smallValue).Within(1e-35));
    }

    /// <summary>
    /// Verifies that converting a very large positive number works correctly.
    /// </summary>
    [Test]
    public void DoubleToFacBytes_VeryLargePositive_ReturnsCorrectBytes()
    {
        double largeValue = 1e30;
        byte[] result = FacConverter.DoubleToFacBytes(largeValue);

        Assert.That(result[4], Is.EqualTo(0x00), "Sign byte should be 0x00 for positive");

        double converted = BitConverter.ToSingle(result, 0);
        Assert.That(converted, Is.EqualTo((float)largeValue).Within(1e25));
    }

    /// <summary>
    /// Verifies that positive infinity is handled correctly.
    /// </summary>
    [Test]
    public void DoubleToFacBytes_PositiveInfinity_ReturnsInfinityBytes()
    {
        byte[] result = FacConverter.DoubleToFacBytes(double.PositiveInfinity);

        Assert.That(result[4], Is.EqualTo(0x00), "Sign byte should be 0x00 for positive infinity");

        double converted = BitConverter.ToSingle(result, 0);
        Assert.That(float.IsPositiveInfinity((float)converted), Is.True);
    }

    /// <summary>
    /// Verifies that negative infinity is handled correctly.
    /// </summary>
    [Test]
    public void DoubleToFacBytes_NegativeInfinity_ReturnsInfinityBytesWithSignByte()
    {
        byte[] result = FacConverter.DoubleToFacBytes(double.NegativeInfinity);

        Assert.That(result[4], Is.EqualTo(0xFF), "Sign byte should be 0xFF for negative infinity");

        double converted = BitConverter.ToSingle(result, 0);
        Assert.That(float.IsNegativeInfinity((float)converted), Is.True);
    }

    /// <summary>
    /// Verifies that NaN is handled correctly.
    /// </summary>
    [Test]
    public void DoubleToFacBytes_NaN_ReturnsNaNBytes()
    {
        byte[] result = FacConverter.DoubleToFacBytes(double.NaN);

        double converted = BitConverter.ToSingle(result, 0);
        Assert.That(float.IsNaN((float)converted), Is.True);
    }

    /// <summary>
    /// Verifies that a fractional number is converted correctly.
    /// </summary>
    [Test]
    public void DoubleToFacBytes_FractionalNumber_ReturnsCorrectBytes()
    {
        byte[] result = FacConverter.DoubleToFacBytes(0.5);

        Assert.That(result[4], Is.EqualTo(0x00), "Sign byte should be 0x00 for positive");

        double converted = BitConverter.ToSingle(result, 0);
        Assert.That(converted, Is.EqualTo(0.5f));
    }

    /// <summary>
    /// Verifies that negative fractional numbers are converted correctly.
    /// </summary>
    [Test]
    public void DoubleToFacBytes_NegativeFractional_ReturnsCorrectBytesWithSignByte()
    {
        byte[] result = FacConverter.DoubleToFacBytes(-0.125);

        Assert.That(result[4], Is.EqualTo(0xFF), "Sign byte should be 0xFF for negative");

        double converted = BitConverter.ToSingle(result, 0);
        Assert.That(converted, Is.EqualTo(-0.125f));
    }

    #endregion

    #region FacBytesToDouble Tests

    /// <summary>
    /// Verifies that converting FAC bytes for zero back to double returns zero.
    /// </summary>
    [Test]
    public void FacBytesToDouble_ZeroBytes_ReturnsZero()
    {
        byte[] facBytes = BitConverter.GetBytes(0.0f);

        double result = FacConverter.FacBytesToDouble(facBytes);

        Assert.That(result, Is.EqualTo(0.0));
    }

    /// <summary>
    /// Verifies that converting FAC bytes for a positive integer returns correct value.
    /// </summary>
    [Test]
    public void FacBytesToDouble_PositiveIntegerBytes_ReturnsCorrectValue()
    {
        byte[] facBytes = BitConverter.GetBytes(42.0f);

        double result = FacConverter.FacBytesToDouble(facBytes);

        Assert.That(result, Is.EqualTo(42.0));
    }

    /// <summary>
    /// Verifies that converting FAC bytes for a negative integer returns correct value.
    /// </summary>
    [Test]
    public void FacBytesToDouble_NegativeIntegerBytes_ReturnsCorrectValue()
    {
        byte[] facBytes = BitConverter.GetBytes(-42.0f);

        double result = FacConverter.FacBytesToDouble(facBytes);

        Assert.That(result, Is.EqualTo(-42.0));
    }

    /// <summary>
    /// Verifies that a null byte array throws ArgumentNullException.
    /// </summary>
    [Test]
    public void FacBytesToDouble_NullArray_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => FacConverter.FacBytesToDouble(null!));
    }

    /// <summary>
    /// Verifies that an array with fewer than 4 bytes throws ArgumentException.
    /// </summary>
    [Test]
    public void FacBytesToDouble_TooFewBytes_ThrowsArgumentException()
    {
        byte[] shortArray = new byte[] { 0x00, 0x00, 0x00 };

        Assert.Throws<ArgumentException>(() => FacConverter.FacBytesToDouble(shortArray));
    }

    /// <summary>
    /// Verifies that an array with exactly 4 bytes works correctly.
    /// </summary>
    [Test]
    public void FacBytesToDouble_ExactlyFourBytes_ReturnsCorrectValue()
    {
        byte[] facBytes = BitConverter.GetBytes(123.456f);

        double result = FacConverter.FacBytesToDouble(facBytes);

        Assert.That(result, Is.EqualTo(123.456f).Within(1e-3));
    }

    /// <summary>
    /// Verifies that an array with more than 4 bytes uses only the first 4.
    /// </summary>
    [Test]
    public void FacBytesToDouble_MoreThanFourBytes_UsesFirstFour()
    {
        byte[] facBytes = new byte[] { 0x00, 0x00, 0x28, 0x42, 0xFF, 0xAB, 0xCD }; // 42.0f followed by extra bytes

        double result = FacConverter.FacBytesToDouble(facBytes);

        Assert.That(result, Is.EqualTo(42.0));
    }

    /// <summary>
    /// Verifies round-trip conversion of Pi maintains precision within float tolerance.
    /// </summary>
    [Test]
    public void FacBytesToDouble_PiRoundTrip_MaintainsPrecision()
    {
        byte[] facBytes = FacConverter.DoubleToFacBytes(Math.PI);

        double result = FacConverter.FacBytesToDouble(facBytes);

        Assert.That(result, Is.EqualTo((float)Math.PI).Within(1e-6));
    }

    #endregion

    #region GetSignByte Tests

    /// <summary>
    /// Verifies that positive values return sign byte 0x00.
    /// </summary>
    [Test]
    public void GetSignByte_PositiveValue_ReturnsZero()
    {
        Assert.That(FacConverter.GetSignByte(42.0), Is.EqualTo(0x00));
        Assert.That(FacConverter.GetSignByte(0.001), Is.EqualTo(0x00));
        Assert.That(FacConverter.GetSignByte(double.MaxValue), Is.EqualTo(0x00));
    }

    /// <summary>
    /// Verifies that negative values return sign byte 0xFF.
    /// </summary>
    [Test]
    public void GetSignByte_NegativeValue_ReturnsFF()
    {
        Assert.That(FacConverter.GetSignByte(-42.0), Is.EqualTo(0xFF));
        Assert.That(FacConverter.GetSignByte(-0.001), Is.EqualTo(0xFF));
        Assert.That(FacConverter.GetSignByte(double.MinValue), Is.EqualTo(0xFF));
    }

    /// <summary>
    /// Verifies that positive zero returns sign byte 0x00.
    /// </summary>
    [Test]
    public void GetSignByte_PositiveZero_ReturnsZero()
    {
        Assert.That(FacConverter.GetSignByte(0.0), Is.EqualTo(0x00));
    }

    /// <summary>
    /// Verifies that negative zero returns sign byte 0xFF.
    /// </summary>
    [Test]
    public void GetSignByte_NegativeZero_ReturnsFF()
    {
        Assert.That(FacConverter.GetSignByte(-0.0), Is.EqualTo(0xFF));
    }

    /// <summary>
    /// Verifies that positive infinity returns sign byte 0x00.
    /// </summary>
    [Test]
    public void GetSignByte_PositiveInfinity_ReturnsZero()
    {
        Assert.That(FacConverter.GetSignByte(double.PositiveInfinity), Is.EqualTo(0x00));
    }

    /// <summary>
    /// Verifies that negative infinity returns sign byte 0xFF.
    /// </summary>
    [Test]
    public void GetSignByte_NegativeInfinity_ReturnsFF()
    {
        Assert.That(FacConverter.GetSignByte(double.NegativeInfinity), Is.EqualTo(0xFF));
    }

    #endregion

    #region WriteToMemory Tests

    /// <summary>
    /// Verifies that WriteToMemory writes the correct bytes to memory.
    /// </summary>
    [Test]
    public void WriteToMemory_PositiveValue_WritesCorrectBytes()
    {
        var memoryLogger = new Mock<ILogger<AppleMemory>>();
        var memory = new AppleMemory(memoryLogger.Object);
        int facAddress = 0x009D;
        int signAddress = 0x00A2;

        FacConverter.WriteToMemory(memory, facAddress, signAddress, 42.0);

        // Read back and verify
        byte[] expectedBytes = BitConverter.GetBytes(42.0f);
        for (int i = 0; i < 4; i++)
        {
            Assert.That(memory.Read(facAddress + i), Is.EqualTo(expectedBytes[i]));
        }

        Assert.That(memory.Read(signAddress), Is.EqualTo(0x00));
    }

    /// <summary>
    /// Verifies that WriteToMemory writes correct sign byte for negative values.
    /// </summary>
    [Test]
    public void WriteToMemory_NegativeValue_WritesCorrectSignByte()
    {
        var memoryLogger = new Mock<ILogger<AppleMemory>>();
        var memory = new AppleMemory(memoryLogger.Object);
        int facAddress = 0x009D;
        int signAddress = 0x00A2;

        FacConverter.WriteToMemory(memory, facAddress, signAddress, -123.456);

        Assert.That(memory.Read(signAddress), Is.EqualTo(0xFF));
    }

    /// <summary>
    /// Verifies that WriteToMemory throws ArgumentNullException for null memory.
    /// </summary>
    [Test]
    public void WriteToMemory_NullMemory_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            FacConverter.WriteToMemory(null!, 0x009D, 0x00A2, 42.0));
    }

    #endregion

    #region ReadFromMemory Tests

    /// <summary>
    /// Verifies that ReadFromMemory reads the correct value from memory.
    /// </summary>
    [Test]
    public void ReadFromMemory_ValidBytes_ReturnsCorrectValue()
    {
        var memoryLogger = new Mock<ILogger<AppleMemory>>();
        var memory = new AppleMemory(memoryLogger.Object);
        int facAddress = 0x009D;

        // Write 42.0f to memory
        byte[] bytes = BitConverter.GetBytes(42.0f);
        for (int i = 0; i < 4; i++)
        {
            memory.Write(facAddress + i, bytes[i]);
        }

        double result = FacConverter.ReadFromMemory(memory, facAddress);

        Assert.That(result, Is.EqualTo(42.0));
    }

    /// <summary>
    /// Verifies that ReadFromMemory throws ArgumentNullException for null memory.
    /// </summary>
    [Test]
    public void ReadFromMemory_NullMemory_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            FacConverter.ReadFromMemory(null!, 0x009D));
    }

    #endregion

    #region Round-Trip Tests

    /// <summary>
    /// Verifies that write-then-read round trip produces the original value.
    /// </summary>
    [Test]
    public void RoundTrip_WriteAndRead_ReturnsOriginalValue()
    {
        var memoryLogger = new Mock<ILogger<AppleMemory>>();
        var memory = new AppleMemory(memoryLogger.Object);
        int facAddress = 0x009D;
        int signAddress = 0x00A2;

        double originalValue = 3.14159;
        FacConverter.WriteToMemory(memory, facAddress, signAddress, originalValue);
        double result = FacConverter.ReadFromMemory(memory, facAddress);

        // Note: precision is limited to single-precision float
        Assert.That(result, Is.EqualTo((float)originalValue).Within(1e-6));
    }

    /// <summary>
    /// Verifies round-trip conversion for various test values.
    /// </summary>
    /// <param name="value">The test value for round-trip conversion.</param>
    [TestCase(0.0)]
    [TestCase(1.0)]
    [TestCase(-1.0)]
    [TestCase(42.0)]
    [TestCase(-42.0)]
    [TestCase(0.5)]
    [TestCase(-0.5)]
    [TestCase(100.0)]
    [TestCase(-100.0)]
    [TestCase(1000000.0)]
    [TestCase(-1000000.0)]
    public void RoundTrip_DoubleToFacAndBack_PreservesValue(double value)
    {
        byte[] facBytes = FacConverter.DoubleToFacBytes(value);
        double result = FacConverter.FacBytesToDouble(facBytes);

        // Convert to float for comparison since we're using single precision
        Assert.That(result, Is.EqualTo((float)value).Within(1e-6));
    }

    /// <summary>
    /// Verifies round-trip for small fractional values.
    /// </summary>
    /// <param name="value">The small fractional value to test.</param>
    [TestCase(0.1)]
    [TestCase(0.01)]
    [TestCase(0.001)]
    [TestCase(-0.1)]
    [TestCase(-0.01)]
    [TestCase(-0.001)]
    public void RoundTrip_SmallFractionalValues_PreservesValueWithinTolerance(double value)
    {
        byte[] facBytes = FacConverter.DoubleToFacBytes(value);
        double result = FacConverter.FacBytesToDouble(facBytes);

        Assert.That(result, Is.EqualTo((float)value).Within(Math.Abs(value) * 1e-5));
    }

    #endregion

    #region CanRepresentAsFloat Tests

    /// <summary>
    /// Verifies that normal float-representable values return true.
    /// </summary>
    /// <param name="value">The value to test for float representation.</param>
    [TestCase(0.0)]
    [TestCase(1.0)]
    [TestCase(-1.0)]
    [TestCase(42.0)]
    [TestCase(0.5)]
    [TestCase(1000000.0)]
    public void CanRepresentAsFloat_NormalValues_ReturnsTrue(double value)
    {
        Assert.That(FacConverter.CanRepresentAsFloat(value), Is.True);
    }

    /// <summary>
    /// Verifies that NaN returns true (NaN can be represented in float).
    /// </summary>
    [Test]
    public void CanRepresentAsFloat_NaN_ReturnsTrue()
    {
        Assert.That(FacConverter.CanRepresentAsFloat(double.NaN), Is.True);
    }

    /// <summary>
    /// Verifies that infinity values return true.
    /// </summary>
    [Test]
    public void CanRepresentAsFloat_Infinity_ReturnsTrue()
    {
        Assert.That(FacConverter.CanRepresentAsFloat(double.PositiveInfinity), Is.True);
        Assert.That(FacConverter.CanRepresentAsFloat(double.NegativeInfinity), Is.True);
    }

    #endregion

    #region Edge Cases

    /// <summary>
    /// Verifies handling of float.MaxValue.
    /// </summary>
    [Test]
    public void DoubleToFacBytes_FloatMaxValue_ReturnsCorrectBytes()
    {
        byte[] result = FacConverter.DoubleToFacBytes(float.MaxValue);

        double converted = BitConverter.ToSingle(result, 0);
        Assert.That(converted, Is.EqualTo(float.MaxValue));
    }

    /// <summary>
    /// Verifies handling of float.MinValue.
    /// </summary>
    [Test]
    public void DoubleToFacBytes_FloatMinValue_ReturnsCorrectBytes()
    {
        byte[] result = FacConverter.DoubleToFacBytes(float.MinValue);

        double converted = BitConverter.ToSingle(result, 0);
        Assert.That(converted, Is.EqualTo(float.MinValue));
        Assert.That(result[4], Is.EqualTo(0xFF), "Sign byte should be 0xFF for negative");
    }

    /// <summary>
    /// Verifies handling of float.Epsilon (smallest positive float).
    /// </summary>
    [Test]
    public void DoubleToFacBytes_FloatEpsilon_ReturnsCorrectBytes()
    {
        byte[] result = FacConverter.DoubleToFacBytes(float.Epsilon);

        double converted = BitConverter.ToSingle(result, 0);
        Assert.That(converted, Is.EqualTo(float.Epsilon));
    }

    /// <summary>
    /// Verifies that values larger than float.MaxValue become infinity.
    /// </summary>
    [Test]
    public void DoubleToFacBytes_OverflowToInfinity_HandledCorrectly()
    {
        double overflowValue = (double)float.MaxValue * 2;
        byte[] result = FacConverter.DoubleToFacBytes(overflowValue);

        double converted = BitConverter.ToSingle(result, 0);
        Assert.That(float.IsPositiveInfinity((float)converted), Is.True);
    }

    /// <summary>
    /// Verifies that very small values that underflow to zero are handled.
    /// </summary>
    [Test]
    public void DoubleToFacBytes_UnderflowToZero_HandledCorrectly()
    {
        double underflowValue = double.Epsilon; // Smaller than float.Epsilon
        byte[] result = FacConverter.DoubleToFacBytes(underflowValue);

        double converted = BitConverter.ToSingle(result, 0);
        Assert.That(converted, Is.EqualTo(0.0)); // Underflows to zero in float
    }

    #endregion
}