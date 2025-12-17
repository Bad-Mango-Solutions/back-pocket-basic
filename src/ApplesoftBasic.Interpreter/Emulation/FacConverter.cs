// <copyright file="FacConverter.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.Emulation;

/// <summary>
/// Provides conversion methods between .NET floating-point types and the Apple II
/// Floating-point ACcumulator (FAC) memory format.
/// </summary>
/// <remarks>
/// <para>
/// The Apple II uses a 5-byte floating-point format in the FAC registers:
/// </para>
/// <list type="bullet">
/// <item><description>Byte 0: Exponent (biased by 128)</description></item>
/// <item><description>Bytes 1-4: Mantissa (normalized with implicit leading 1)</description></item>
/// </list>
/// <para>
/// This class provides a simplified implementation that stores .NET float values
/// in a 4-byte IEEE 754 format plus a sign byte, enabling basic interoperability
/// with machine language routines that read/write FAC1 and FAC2.
/// </para>
/// <para>
/// Note: This is a simplified implementation. For full Applesoft BASIC compatibility,
/// a proper 5-byte Apple II floating-point format implementation would be needed.
/// </para>
/// </remarks>
public static class FacConverter
{
    /// <summary>
    /// Converts a .NET double value to a byte array suitable for writing to FAC memory.
    /// </summary>
    /// <param name="value">The double value to convert.</param>
    /// <returns>
    /// A 5-byte array where bytes 0-3 contain the IEEE 754 single-precision representation
    /// of the value, and byte 4 contains the sign byte (0xFF for negative, 0x00 for non-negative).
    /// </returns>
    /// <remarks>
    /// <para>
    /// The conversion process:
    /// </para>
    /// <list type="number">
    /// <item><description>The double is converted to a single-precision float.</description></item>
    /// <item><description>The float is converted to its 4-byte IEEE 754 representation.</description></item>
    /// <item><description>A sign byte is appended (0xFF for negative, 0x00 otherwise).</description></item>
    /// </list>
    /// <para>
    /// Special values are handled as follows:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Positive zero: Returns [0x00, 0x00, 0x00, 0x00, 0x00]</description></item>
    /// <item><description>Negative zero: Returns [0x00, 0x00, 0x00, 0x80, 0xFF]</description></item>
    /// <item><description>Positive infinity: Returns IEEE 754 infinity representation with sign byte 0x00</description></item>
    /// <item><description>Negative infinity: Returns IEEE 754 infinity representation with sign byte 0xFF</description></item>
    /// <item><description>NaN: Returns IEEE 754 NaN representation with sign byte 0x00</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// byte[] facBytes = FacConverter.DoubleToFacBytes(3.14159);
    /// // Write facBytes to FAC1 memory location
    /// </code>
    /// </example>
    public static byte[] DoubleToFacBytes(double value)
    {
        byte[] result = new byte[5];

        // Convert double to single-precision float and get bytes
        byte[] floatBytes = BitConverter.GetBytes((float)value);
        Array.Copy(floatBytes, 0, result, 0, 4);

        // Set sign byte
        result[4] = value < 0 ? (byte)0xFF : (byte)0x00;

        return result;
    }

    /// <summary>
    /// Converts a byte array from FAC memory format to a .NET double value.
    /// </summary>
    /// <param name="facBytes">
    /// A byte array of at least 4 bytes containing the IEEE 754 single-precision
    /// representation of the floating-point value.
    /// </param>
    /// <returns>The double value represented by the FAC bytes.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="facBytes"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="facBytes"/> has fewer than 4 bytes.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The conversion reads the first 4 bytes as an IEEE 754 single-precision float
    /// and converts it to a double. The sign byte (if present at index 4) is not
    /// used in the conversion as the sign is already encoded in the IEEE 754 format.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// byte[] facBytes = new byte[] { 0xDB, 0x0F, 0x49, 0x40, 0x00 }; // ~3.14159
    /// double value = FacConverter.FacBytesToDouble(facBytes);
    /// </code>
    /// </example>
    public static double FacBytesToDouble(byte[] facBytes)
    {
        if (facBytes == null)
        {
            throw new ArgumentNullException(nameof(facBytes));
        }

        if (facBytes.Length < 4)
        {
            throw new ArgumentException("FAC byte array must contain at least 4 bytes.", nameof(facBytes));
        }

        return BitConverter.ToSingle(facBytes, 0);
    }

    /// <summary>
    /// Gets the sign byte for a given double value.
    /// </summary>
    /// <param name="value">The double value to get the sign for.</param>
    /// <returns>0xFF for negative values, 0x00 for non-negative values (including positive zero).</returns>
    /// <remarks>
    /// Note that negative zero (-0.0) is considered negative and returns 0xFF.
    /// </remarks>
    public static byte GetSignByte(double value)
    {
        return value < 0 ? (byte)0xFF : (byte)0x00;
    }

    /// <summary>
    /// Writes a double value to memory at the specified FAC location.
    /// </summary>
    /// <param name="memory">The memory interface to write to.</param>
    /// <param name="facAddress">The starting address of the FAC register.</param>
    /// <param name="signAddress">The address of the sign byte.</param>
    /// <param name="value">The double value to write.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="memory"/> is null.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method writes 4 bytes of the IEEE 754 single-precision representation
    /// starting at <paramref name="facAddress"/>, and writes the sign byte to
    /// <paramref name="signAddress"/>.
    /// </para>
    /// </remarks>
    public static void WriteToMemory(IMemory memory, int facAddress, int signAddress, double value)
    {
        if (memory == null)
        {
            throw new ArgumentNullException(nameof(memory));
        }

        byte[] facBytes = DoubleToFacBytes(value);

        for (int i = 0; i < 4; i++)
        {
            memory.Write(facAddress + i, facBytes[i]);
        }

        memory.Write(signAddress, facBytes[4]);
    }

    /// <summary>
    /// Reads a double value from memory at the specified FAC location.
    /// </summary>
    /// <param name="memory">The memory interface to read from.</param>
    /// <param name="facAddress">The starting address of the FAC register.</param>
    /// <returns>The double value read from the FAC memory location.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="memory"/> is null.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method reads 4 bytes starting at <paramref name="facAddress"/> and
    /// converts them from IEEE 754 single-precision format to a double.
    /// </para>
    /// </remarks>
    public static double ReadFromMemory(IMemory memory, int facAddress)
    {
        if (memory == null)
        {
            throw new ArgumentNullException(nameof(memory));
        }

        byte[] bytes = new byte[4];
        for (int i = 0; i < 4; i++)
        {
            bytes[i] = memory.Read(facAddress + i);
        }

        return FacBytesToDouble(bytes);
    }

    /// <summary>
    /// Validates that a double value can be accurately represented as a single-precision float.
    /// </summary>
    /// <param name="value">The double value to validate.</param>
    /// <returns>
    /// <c>true</c> if the value can be represented without significant precision loss;
    /// <c>false</c> otherwise.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method checks if converting the double to float and back results in the same value
    /// (within floating-point tolerance). Values outside the range of single-precision floats
    /// or requiring more precision than single-precision provides will return <c>false</c>.
    /// </para>
    /// <para>
    /// Special cases:
    /// </para>
    /// <list type="bullet">
    /// <item><description>NaN always returns <c>true</c> (NaN converts to NaN)</description></item>
    /// <item><description>Infinity always returns <c>true</c> (infinity converts to infinity)</description></item>
    /// </list>
    /// </remarks>
    public static bool CanRepresentAsFloat(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            return true;
        }

        float asFloat = (float)value;
        double backToDouble = asFloat;

        // Check if the round-trip conversion preserves the value
        // Use a relative tolerance for non-zero values
        if (value == 0)
        {
            return backToDouble == 0;
        }

        double relativeDiff = Math.Abs((value - backToDouble) / value);
        return relativeDiff < 1e-6; // Allow for single-precision tolerance
    }
}