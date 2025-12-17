// <copyright file="BasicInteger.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.Emulation;

using System.Runtime.InteropServices;

/// <summary>
/// Represents an Applesoft BASIC integer value as stored on the Apple II.
/// </summary>
/// <remarks>
/// <para>
/// Applesoft BASIC integers (variables with % suffix) are stored as 16-bit signed values
/// in two's complement format. The valid range is -32768 to 32767.
/// </para>
/// <para>
/// In Apple II memory, integers are stored in little-endian format (low byte first).
/// </para>
/// <para>
/// Key characteristics:
/// </para>
/// <list type="bullet">
/// <item><description>16-bit signed two's complement</description></item>
/// <item><description>Range: -32768 to 32767</description></item>
/// <item><description>Little-endian byte order in memory</description></item>
/// <item><description>2 bytes storage</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// // Create from int
/// BasicInteger value = 42;
///
/// // Convert to int
/// int result = value;
///
/// // Get raw bytes (little-endian)
/// byte[] bytes = value.ToBytes();
///
/// // Create from bytes
/// BasicInteger fromBytes = BasicInteger.FromBytes(new byte[] { 0x2A, 0x00 }); // 42
/// </code>
/// </example>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct BasicInteger : IEquatable<BasicInteger>
{
    /// <summary>
    /// The size in bytes of the BasicInteger representation.
    /// </summary>
    public const int ByteSize = 2;

    /// <summary>
    /// The minimum value that can be represented.
    /// </summary>
    public const short MinValue = short.MinValue; // -32768

    /// <summary>
    /// The maximum value that can be represented.
    /// </summary>
    public const short MaxValue = short.MaxValue; // 32767

    private readonly short value;

    /// <summary>
    /// Initializes a new instance of the <see cref="BasicInteger"/> struct.
    /// </summary>
    /// <param name="value">The 16-bit signed integer value.</param>
    public BasicInteger(short value)
    {
        this.value = value;
    }

    /// <summary>
    /// Gets the BasicInteger representation of zero.
    /// </summary>
    public static BasicInteger Zero => new(0);

    /// <summary>
    /// Gets the underlying 16-bit signed value.
    /// </summary>
    public short Value => value;

    /// <summary>
    /// Gets a value indicating whether this BasicInteger is zero.
    /// </summary>
    public bool IsZero => value == 0;

    /// <summary>
    /// Gets a value indicating whether this BasicInteger is negative.
    /// </summary>
    public bool IsNegative => value < 0;

    /// <summary>
    /// Implicitly converts an int to a BasicInteger.
    /// </summary>
    /// <param name="value">The integer value to convert.</param>
    /// <returns>A BasicInteger representing the value.</returns>
    /// <exception cref="OverflowException">
    /// Thrown when the value is outside the range -32768 to 32767.
    /// </exception>
    public static implicit operator BasicInteger(int value)
    {
        if (value < MinValue || value > MaxValue)
        {
            throw new OverflowException($"Value {value} is outside the valid BasicInteger range ({MinValue} to {MaxValue}).");
        }

        return new BasicInteger((short)value);
    }

    /// <summary>
    /// Implicitly converts a short to a BasicInteger.
    /// </summary>
    /// <param name="value">The short value to convert.</param>
    /// <returns>A BasicInteger representing the value.</returns>
    public static implicit operator BasicInteger(short value) => new(value);

    /// <summary>
    /// Implicitly converts a BasicInteger to an int.
    /// </summary>
    /// <param name="basicInt">The BasicInteger to convert.</param>
    /// <returns>The integer value.</returns>
    public static implicit operator int(BasicInteger basicInt) => basicInt.value;

    /// <summary>
    /// Implicitly converts a BasicInteger to a short.
    /// </summary>
    /// <param name="basicInt">The BasicInteger to convert.</param>
    /// <returns>The short value.</returns>
    public static implicit operator short(BasicInteger basicInt) => basicInt.value;

    /// <summary>
    /// Determines whether two BasicInteger values are equal.
    /// </summary>
    /// <param name="left">The first BasicInteger value to compare.</param>
    /// <param name="right">The second BasicInteger value to compare.</param>
    /// <returns><c>true</c> if the values are equal; otherwise, <c>false</c>.</returns>
    public static bool operator ==(BasicInteger left, BasicInteger right) => left.Equals(right);

    /// <summary>
    /// Determines whether two BasicInteger values are not equal.
    /// </summary>
    /// <param name="left">The first BasicInteger value to compare.</param>
    /// <param name="right">The second BasicInteger value to compare.</param>
    /// <returns><c>true</c> if the values are not equal; otherwise, <c>false</c>.</returns>
    public static bool operator !=(BasicInteger left, BasicInteger right) => !left.Equals(right);

    /// <summary>
    /// Creates a BasicInteger from an integer value.
    /// </summary>
    /// <param name="value">The integer value.</param>
    /// <returns>A BasicInteger representing the value.</returns>
    /// <exception cref="OverflowException">
    /// Thrown when the value is outside the valid range.
    /// </exception>
    public static BasicInteger FromInt(int value)
    {
        if (value < MinValue || value > MaxValue)
        {
            throw new OverflowException($"Value {value} is outside the valid BasicInteger range ({MinValue} to {MaxValue}).");
        }

        return new BasicInteger((short)value);
    }

    /// <summary>
    /// Creates a BasicInteger from a double value by truncating toward zero.
    /// </summary>
    /// <param name="value">The double value to convert.</param>
    /// <returns>A BasicInteger representing the truncated value.</returns>
    /// <exception cref="OverflowException">
    /// Thrown when the truncated value is outside the valid range.
    /// </exception>
    /// <remarks>
    /// This method matches Applesoft BASIC's behavior of truncating floating-point
    /// values toward zero when converting to integers.
    /// </remarks>
    public static BasicInteger FromDouble(double value)
    {
        // Truncate toward zero (Applesoft behavior)
        int truncated = value >= 0 ? (int)Math.Floor(value) : (int)Math.Ceiling(value);

        if (truncated < MinValue || truncated > MaxValue)
        {
            throw new OverflowException($"Value {value} (truncated to {truncated}) is outside the valid BasicInteger range.");
        }

        return new BasicInteger((short)truncated);
    }

    /// <summary>
    /// Creates a BasicInteger from a byte array (little-endian format).
    /// </summary>
    /// <param name="bytes">A byte array containing at least 2 bytes.</param>
    /// <returns>A BasicInteger created from the bytes.</returns>
    /// <exception cref="ArgumentNullException">Thrown when bytes is null.</exception>
    /// <exception cref="ArgumentException">Thrown when bytes has fewer than 2 elements.</exception>
    public static BasicInteger FromBytes(byte[] bytes)
    {
        if (bytes == null)
        {
            throw new ArgumentNullException(nameof(bytes));
        }

        if (bytes.Length < ByteSize)
        {
            throw new ArgumentException($"Byte array must contain at least {ByteSize} bytes.", nameof(bytes));
        }

        // Little-endian: low byte first
        short value = (short)(bytes[0] | (bytes[1] << 8));
        return new BasicInteger(value);
    }

    /// <summary>
    /// Converts this BasicInteger to a byte array (little-endian format).
    /// </summary>
    /// <returns>A 2-byte array in little-endian format.</returns>
    public byte[] ToBytes()
    {
        return new byte[]
        {
            (byte)(value & 0xFF),        // Low byte
            (byte)((value >> 8) & 0xFF), // High byte
        };
    }

    /// <summary>
    /// Converts this BasicInteger to a double.
    /// </summary>
    /// <returns>The double representation of this integer.</returns>
    public double ToDouble() => value;

    /// <summary>
    /// Converts this BasicInteger to an MBF value.
    /// </summary>
    /// <returns>An MBF representation of this integer.</returns>
    public MBF ToMbf() => MBF.FromDouble(value);

    /// <inheritdoc/>
    public bool Equals(BasicInteger other) => value == other.value;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is BasicInteger other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => value;

    /// <inheritdoc/>
    public override string ToString() => value.ToString();
}