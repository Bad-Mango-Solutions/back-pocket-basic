// <copyright file="HexParser.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Core.Configuration;

using System.Globalization;

/// <summary>
/// Provides utility methods for parsing hexadecimal strings commonly used in machine profiles.
/// </summary>
/// <remarks>
/// This class handles parsing of hex values with or without the "0x" prefix,
/// which is the standard format used in machine profile JSON files for addresses
/// and sizes.
/// </remarks>
public static class HexParser
{
    /// <summary>
    /// Parses a hexadecimal string to an unsigned 32-bit integer.
    /// </summary>
    /// <param name="value">The hex string to parse (e.g., "0x1000", "FFFF", "0xC000").</param>
    /// <returns>The parsed unsigned 32-bit integer value.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is null, empty, or whitespace.</exception>
    /// <exception cref="FormatException">Thrown when <paramref name="value"/> is not a valid hex string.</exception>
    public static uint ParseUInt32(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        ReadOnlySpan<char> span = value.AsSpan().Trim();

        if (span.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            span = span[2..];
        }

        if (!uint.TryParse(span, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint result))
        {
            throw new FormatException($"Invalid hex value: '{value}'");
        }

        return result;
    }

    /// <summary>
    /// Parses a hexadecimal string to a byte value.
    /// </summary>
    /// <param name="value">The hex string to parse (e.g., "0x00", "FF", "0xAA").</param>
    /// <returns>The parsed byte value.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is null, empty, or whitespace.</exception>
    /// <exception cref="FormatException">
    /// Thrown when <paramref name="value"/> is not a valid hex string or exceeds the byte range (0x00-0xFF).
    /// </exception>
    public static byte ParseByte(string value)
    {
        uint parsed = ParseUInt32(value);

        if (parsed > 0xFF)
        {
            throw new FormatException($"Value '{value}' exceeds byte range (0x00-0xFF).");
        }

        return (byte)parsed;
    }

    /// <summary>
    /// Tries to parse a hexadecimal string to an unsigned 32-bit integer.
    /// </summary>
    /// <param name="value">The hex string to parse.</param>
    /// <param name="result">When this method returns, contains the parsed value if successful; otherwise, zero.</param>
    /// <returns><see langword="true"/> if parsing was successful; otherwise, <see langword="false"/>.</returns>
    public static bool TryParseUInt32(string? value, out uint result)
    {
        result = 0;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        ReadOnlySpan<char> span = value.AsSpan().Trim();

        if (span.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            span = span[2..];
        }

        return uint.TryParse(span, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out result);
    }

    /// <summary>
    /// Tries to parse a hexadecimal string to a byte value.
    /// </summary>
    /// <param name="value">The hex string to parse.</param>
    /// <param name="result">When this method returns, contains the parsed value if successful; otherwise, zero.</param>
    /// <returns><see langword="true"/> if parsing was successful and value is in byte range; otherwise, <see langword="false"/>.</returns>
    public static bool TryParseByte(string? value, out byte result)
    {
        result = 0;

        if (!TryParseUInt32(value, out uint parsed))
        {
            return false;
        }

        if (parsed > 0xFF)
        {
            return false;
        }

        result = (byte)parsed;
        return true;
    }
}