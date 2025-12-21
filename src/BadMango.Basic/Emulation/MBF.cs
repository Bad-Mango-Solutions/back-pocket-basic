// <copyright file="MBF.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace BadMango.Basic.Emulation;

using System.Runtime.InteropServices;

/// <summary>
/// Represents a Microsoft Binary Format (MBF) floating-point number as used
/// in Applesoft BASIC and the Apple II.
/// </summary>
/// <remarks>
/// <para>
/// The MBF format uses 5 bytes:
/// </para>
/// <list type="bullet">
/// <item><description>Byte 0: Exponent (biased by 128)</description></item>
/// <item><description>Bytes 1-4: Mantissa (normalized with implicit leading 1)</description></item>
/// <item><description>Sign: Stored in MSB of byte 1 (mantissa high byte)</description></item>
/// </list>
/// <para>
/// Key characteristics:
/// </para>
/// <list type="bullet">
/// <item><description>Zero is represented by an exponent byte of 0</description></item>
/// <item><description>Non-zero values have an implicit leading 1 bit in mantissa</description></item>
/// <item><description>Sign bit is stored in the MSB of the first mantissa byte</description></item>
/// <item><description>Exponent bias is 128 (0x80)</description></item>
/// </list>
/// <para>
/// MBF does not support infinity or NaN. These special values will throw
/// <see cref="OverflowException"/> when converted to MBF.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Create MBF value from double using implicit conversion
/// MBF value = 3.14159;
///
/// // Convert back to double using implicit conversion
/// double result = value;
///
/// // Create MBF value using explicit method
/// MBF explicitValue = MBF.FromDouble(2.71828);
///
/// // Get the raw bytes
/// byte[] bytes = explicitValue.ToBytes();
/// </code>
/// </example>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct MBF : IEquatable<MBF>
{
    /// <summary>
    /// The exponent bias used in MBF format.
    /// </summary>
    /// <remarks>
    /// The actual exponent is stored value minus 128.
    /// For example, a stored exponent of 129 represents an actual exponent of 1.
    /// </remarks>
    public const int ExponentBias = 128;

    /// <summary>
    /// The size in bytes of the MBF representation.
    /// </summary>
    public const int ByteSize = 5;

    /// <summary>
    /// The number of bits in the mantissa (32 bits = 4 bytes).
    /// </summary>
    private const int MantissaBits = 32;

    /// <summary>
    /// The conversion factor for normalizing the mantissa (2^32).
    /// Used to convert between normalized double mantissa and 32-bit integer representation.
    /// </summary>
    private const double MantissaScale = 4294967296.0; // 2^32 = 0x100000000

    private readonly byte exponent;
    private readonly byte mantissa1; // MSB contains sign bit
    private readonly byte mantissa2;
    private readonly byte mantissa3;
    private readonly byte mantissa4; // LSB

    /// <summary>
    /// Initializes a new instance of the <see cref="MBF"/> struct from raw bytes.
    /// </summary>
    /// <param name="exponent">The exponent byte (biased by 128).</param>
    /// <param name="mantissa1">The first mantissa byte (MSB, contains sign in bit 7).</param>
    /// <param name="mantissa2">The second mantissa byte.</param>
    /// <param name="mantissa3">The third mantissa byte.</param>
    /// <param name="mantissa4">The fourth mantissa byte (LSB).</param>
    public MBF(byte exponent, byte mantissa1, byte mantissa2, byte mantissa3, byte mantissa4)
    {
        this.exponent = exponent;
        this.mantissa1 = mantissa1;
        this.mantissa2 = mantissa2;
        this.mantissa3 = mantissa3;
        this.mantissa4 = mantissa4;
    }

    /// <summary>
    /// Gets the MBF representation of zero.
    /// </summary>
    /// <value>An MBF struct representing zero.</value>
    public static MBF Zero => new MBF(0, 0, 0, 0, 0);

    /// <summary>
    /// Gets the exponent byte (biased by 128).
    /// </summary>
    /// <value>The raw exponent byte value.</value>
    public byte Exponent => exponent;

    /// <summary>
    /// Gets the first mantissa byte (MSB, contains sign bit in bit 7).
    /// </summary>
    /// <value>The first mantissa byte.</value>
    public byte Mantissa1 => mantissa1;

    /// <summary>
    /// Gets the second mantissa byte.
    /// </summary>
    /// <value>The second mantissa byte.</value>
    public byte Mantissa2 => mantissa2;

    /// <summary>
    /// Gets the third mantissa byte.
    /// </summary>
    /// <value>The third mantissa byte.</value>
    public byte Mantissa3 => mantissa3;

    /// <summary>
    /// Gets the fourth mantissa byte (LSB).
    /// </summary>
    /// <value>The fourth mantissa byte.</value>
    public byte Mantissa4 => mantissa4;

    /// <summary>
    /// Gets a value indicating whether this MBF value represents zero.
    /// </summary>
    /// <value><c>true</c> if this value is zero; otherwise, <c>false</c>.</value>
    /// <remarks>
    /// In MBF format, zero is represented by an exponent byte of 0.
    /// The mantissa bytes are ignored when exponent is 0.
    /// </remarks>
    public bool IsZero => exponent == 0;

    /// <summary>
    /// Gets a value indicating whether this MBF value is negative.
    /// </summary>
    /// <value><c>true</c> if this value is negative; otherwise, <c>false</c>.</value>
    /// <remarks>
    /// The sign is stored in the MSB of the first mantissa byte.
    /// Zero is considered positive (not negative) regardless of sign bit.
    /// </remarks>
    public bool IsNegative => !IsZero && (mantissa1 & 0x80) != 0;

    /// <summary>
    /// Implicitly converts a double to an MBF value.
    /// </summary>
    /// <param name="value">The double value to convert.</param>
    /// <returns>An MBF struct representing the converted value.</returns>
    /// <exception cref="OverflowException">
    /// Thrown when the value is infinity or NaN, which are not supported by MBF.
    /// </exception>
    public static implicit operator MBF(double value) => FromDouble(value);

    /// <summary>
    /// Implicitly converts an MBF value to a double.
    /// </summary>
    /// <param name="mbf">The MBF value to convert.</param>
    /// <returns>The double value represented by the MBF.</returns>
    public static implicit operator double(MBF mbf) => mbf.ToDouble();

    /// <summary>
    /// Implicitly converts a float to an MBF value.
    /// </summary>
    /// <param name="value">The float value to convert.</param>
    /// <returns>An MBF struct representing the converted value.</returns>
    /// <exception cref="OverflowException">
    /// Thrown when the value is infinity or NaN, which are not supported by MBF.
    /// </exception>
    public static implicit operator MBF(float value) => FromFloat(value);

    /// <summary>
    /// Implicitly converts an MBF value to a float.
    /// </summary>
    /// <param name="mbf">The MBF value to convert.</param>
    /// <returns>The float value represented by the MBF.</returns>
    public static implicit operator float(MBF mbf) => mbf.ToFloat();

    /// <summary>
    /// Determines whether two MBF values are equal.
    /// </summary>
    /// <param name="left">The first MBF value.</param>
    /// <param name="right">The second MBF value.</param>
    /// <returns><c>true</c> if the values are equal; otherwise, <c>false</c>.</returns>
    public static bool operator ==(MBF left, MBF right) => left.Equals(right);

    /// <summary>
    /// Determines whether two MBF values are not equal.
    /// </summary>
    /// <param name="left">The first MBF value.</param>
    /// <param name="right">The second MBF value.</param>
    /// <returns><c>true</c> if the values are not equal; otherwise, <c>false</c>.</returns>
    public static bool operator !=(MBF left, MBF right) => !left.Equals(right);

    /// <summary>
    /// Creates an MBF value from a double-precision floating-point number.
    /// </summary>
    /// <param name="value">The double value to convert.</param>
    /// <returns>An MBF struct representing the converted value.</returns>
    /// <exception cref="OverflowException">
    /// Thrown when the value is infinity or NaN, which are not supported by MBF.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Special cases are handled as follows:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Zero (including negative zero): Returns MBF zero (exponent = 0)</description></item>
    /// <item><description>Infinity: Throws <see cref="OverflowException"/></description></item>
    /// <item><description>NaN: Throws <see cref="OverflowException"/></description></item>
    /// <item><description>Values too large: Throws <see cref="OverflowException"/></description></item>
    /// <item><description>Values too small (underflow): Returns zero</description></item>
    /// </list>
    /// </remarks>
    public static MBF FromDouble(double value)
    {
        // Handle special IEEE 754 values that MBF doesn't support
        if (double.IsNaN(value))
        {
            throw new OverflowException("MBF format does not support NaN values.");
        }

        if (double.IsInfinity(value))
        {
            throw new OverflowException("MBF format does not support infinity values.");
        }

        // Handle zero (including negative zero)
        if (value == 0.0)
        {
            return Zero;
        }

        // Extract sign
        bool negative = value < 0;
        double absValue = Math.Abs(value);

        // Calculate exponent (find power of 2)
        // IEEE double: 1.f * 2^exp, where exp = floor(log2(value))
        int exp = (int)Math.Floor(Math.Log2(absValue));

        // MBF exponent is biased by 128, and the mantissa is stored as 0.1xxxx (normalized).
        // The +1 offset converts from IEEE-style 1.xxxx to MBF-style 0.1xxxx representation.
        // In MBF, a value like 1.0 has exponent 129 (128 bias + 1) because it's stored as 0.5 * 2^1.
        int biasedExp = exp + ExponentBias + 1;

        // Check for overflow (exponent > 255) or underflow (exponent < 1)
        if (biasedExp > 255)
        {
            throw new OverflowException($"Value {value} is too large to represent in MBF format.");
        }

        if (biasedExp < 1)
        {
            // Underflow to zero
            return Zero;
        }

        // Calculate mantissa
        // Normalize to get fractional part: absValue / 2^(exp+1) gives 0.5 <= mantissa < 1.0
        double mantissa = absValue / Math.Pow(2, exp + 1);

        // Convert mantissa to 32-bit integer (4 bytes)
        // Multiply by 2^32 (MantissaScale) to get the 32-bit mantissa value
        // The implicit leading 1 is replaced by the sign bit
        uint mantissaBits = (uint)(mantissa * MantissaScale);

        // Extract mantissa bytes (MSB first)
        byte m1 = (byte)((mantissaBits >> 24) & 0xFF);
        byte m2 = (byte)((mantissaBits >> 16) & 0xFF);
        byte m3 = (byte)((mantissaBits >> 8) & 0xFF);
        byte m4 = (byte)(mantissaBits & 0xFF);

        // Clear the MSB of m1 (which would be the implicit 1) and set sign bit if negative
        m1 = (byte)((m1 & 0x7F) | (negative ? 0x80 : 0x00));

        return new MBF((byte)biasedExp, m1, m2, m3, m4);
    }

    /// <summary>
    /// Creates an MBF value from a single-precision floating-point number.
    /// </summary>
    /// <param name="value">The float value to convert.</param>
    /// <returns>An MBF struct representing the converted value.</returns>
    /// <exception cref="OverflowException">
    /// Thrown when the value is infinity or NaN, which are not supported by MBF.
    /// </exception>
    public static MBF FromFloat(float value)
    {
        return FromDouble(value);
    }

    /// <summary>
    /// Creates an MBF value from a byte array.
    /// </summary>
    /// <param name="bytes">A byte array containing at least 5 bytes.</param>
    /// <returns>An MBF struct created from the byte array.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="bytes"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="bytes"/> has fewer than 5 bytes.
    /// </exception>
    public static MBF FromBytes(byte[] bytes)
    {
        if (bytes == null)
        {
            throw new ArgumentNullException(nameof(bytes));
        }

        if (bytes.Length < ByteSize)
        {
            throw new ArgumentException($"Byte array must contain at least {ByteSize} bytes.", nameof(bytes));
        }

        return new MBF(bytes[0], bytes[1], bytes[2], bytes[3], bytes[4]);
    }

    /// <summary>
    /// Converts this MBF value to a double-precision floating-point number.
    /// </summary>
    /// <returns>The double value represented by this MBF.</returns>
    public double ToDouble()
    {
        // Zero is represented by exponent = 0
        if (exponent == 0)
        {
            return 0.0;
        }

        // Extract sign
        bool negative = (mantissa1 & 0x80) != 0;

        // Calculate actual exponent
        int exp = exponent - ExponentBias - 1;

        // Build mantissa from bytes
        // Restore the implicit 1 bit in the MSB position
        uint mantissaBits = ((uint)(mantissa1 | 0x80) << 24)
                          | ((uint)mantissa2 << 16)
                          | ((uint)mantissa3 << 8)
                          | mantissa4;

        // Convert to normalized double mantissa (0.5 <= mantissa < 1.0)
        // Divide by 2^32 (MantissaScale) to convert from 32-bit integer to fractional
        double mantissa = mantissaBits / MantissaScale;

        // Calculate final value: mantissa * 2^(exp+1)
        double result = mantissa * Math.Pow(2, exp + 1);

        return negative ? -result : result;
    }

    /// <summary>
    /// Converts this MBF value to a single-precision floating-point number.
    /// </summary>
    /// <returns>The float value represented by this MBF.</returns>
    public float ToFloat()
    {
        return (float)ToDouble();
    }

    /// <summary>
    /// Returns the byte representation of this MBF value.
    /// </summary>
    /// <returns>A 5-byte array containing the MBF representation.</returns>
    public byte[] ToBytes()
    {
        return new byte[] { exponent, mantissa1, mantissa2, mantissa3, mantissa4 };
    }

    /// <inheritdoc/>
    public bool Equals(MBF other)
    {
        // Zero values are equal regardless of mantissa bytes
        if (exponent == 0 && other.exponent == 0)
        {
            return true;
        }

        return exponent == other.exponent
            && mantissa1 == other.mantissa1
            && mantissa2 == other.mantissa2
            && mantissa3 == other.mantissa3
            && mantissa4 == other.mantissa4;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is MBF other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        if (exponent == 0)
        {
            return 0;
        }

        return HashCode.Combine(exponent, mantissa1, mantissa2, mantissa3, mantissa4);
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        double value = ToDouble();
        return $"MBF({value:G}, Exp={exponent:X2}, M=[{mantissa1:X2},{mantissa2:X2},{mantissa3:X2},{mantissa4:X2}])";
    }
}