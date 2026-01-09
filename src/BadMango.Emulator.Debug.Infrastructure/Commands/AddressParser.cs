// <copyright file="AddressParser.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Commands;

using System.Globalization;

using BadMango.Emulator.Bus;
using BadMango.Emulator.Bus.Interfaces;

/// <summary>
/// Provides address parsing utilities for debug commands.
/// </summary>
/// <remarks>
/// <para>
/// Supports multiple address formats:
/// <list type="bullet">
/// <item><description>Hex with $ prefix: <c>$C000</c>, <c>$300</c></description></item>
/// <item><description>Hex with 0x prefix: <c>0xC000</c>, <c>0x300</c></description></item>
/// <item><description>Decimal: <c>49152</c>, <c>768</c></description></item>
/// <item><description>Soft switch names: <c>SPEAKER</c>, <c>KBD</c>, <c>KBDSTRB</c> (when machine is available)</description></item>
/// </list>
/// </para>
/// <para>
/// Soft switch names are resolved dynamically from components that implement
/// <see cref="ISoftSwitchProvider"/>. This allows commands to use switch names
/// that are registered by the actual devices in the current machine configuration.
/// </para>
/// </remarks>
public static class AddressParser
{
    /// <summary>
    /// Attempts to parse an address from a string value.
    /// </summary>
    /// <param name="value">The string value to parse.</param>
    /// <param name="address">When this method returns, contains the parsed address if successful.</param>
    /// <returns><see langword="true"/> if parsing succeeded; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// <para>
    /// This overload does not support soft switch name resolution. Use
    /// <see cref="TryParse(string, IMachine?, out uint)"/> when a machine is available.
    /// </para>
    /// <para>
    /// The parser attempts to resolve the value in the following order:
    /// <list type="number">
    /// <item><description>If the value starts with <c>$</c>, parse as hexadecimal.</description></item>
    /// <item><description>If the value starts with <c>0x</c>, parse as hexadecimal.</description></item>
    /// <item><description>Otherwise, try to parse as decimal.</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public static bool TryParse(string value, out uint address)
    {
        return TryParse(value, null, out address);
    }

    /// <summary>
    /// Attempts to parse an address from a string value, with soft switch name resolution.
    /// </summary>
    /// <param name="value">The string value to parse.</param>
    /// <param name="machine">The machine to query for soft switch providers, or <see langword="null"/>.</param>
    /// <param name="address">When this method returns, contains the parsed address if successful.</param>
    /// <returns><see langword="true"/> if parsing succeeded; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// <para>
    /// The parser attempts to resolve the value in the following order:
    /// <list type="number">
    /// <item><description>If the value starts with <c>$</c>, parse as hexadecimal.</description></item>
    /// <item><description>If the value starts with <c>0x</c>, parse as hexadecimal.</description></item>
    /// <item><description>If a machine is provided, try to resolve as a registered soft switch name.</description></item>
    /// <item><description>Otherwise, try to parse as decimal.</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public static bool TryParse(string value, IMachine? machine, out uint address)
    {
        address = 0;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        // Dollar-prefixed hex
        if (value.StartsWith("$", StringComparison.Ordinal))
        {
            return uint.TryParse(value[1..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out address);
        }

        // 0x-prefixed hex
        if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            return uint.TryParse(value[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out address);
        }

        // Try as soft switch name if machine is available
        if (machine is not null && TryResolveSoftSwitchName(value, machine, out ushort switchAddress))
        {
            address = switchAddress;
            return true;
        }

        // Try as decimal
        return uint.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out address);
    }

    /// <summary>
    /// Attempts to parse a count value from a string.
    /// </summary>
    /// <param name="value">The string value to parse.</param>
    /// <param name="count">When this method returns, contains the parsed count if successful.</param>
    /// <returns><see langword="true"/> if parsing succeeded; otherwise, <see langword="false"/>.</returns>
    public static bool TryParseCount(string value, out int count)
    {
        count = 0;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        // Dollar-prefixed hex
        if (value.StartsWith("$", StringComparison.Ordinal))
        {
            return int.TryParse(value[1..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out count);
        }

        // 0x-prefixed hex
        if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            return int.TryParse(value[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out count);
        }

        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out count);
    }

    /// <summary>
    /// Gets a description of accepted address formats for help text.
    /// </summary>
    /// <returns>A string describing accepted address formats.</returns>
    public static string GetFormatDescription()
    {
        return "hex ($1234 or 0x1234), decimal, or soft switch name (e.g., SPEAKER, KBD)";
    }

    /// <summary>
    /// Attempts to resolve a soft switch name to its address using registered providers.
    /// </summary>
    /// <param name="name">The soft switch name (case-insensitive).</param>
    /// <param name="machine">The machine to query for providers.</param>
    /// <param name="address">When this method returns, contains the address if found.</param>
    /// <returns><see langword="true"/> if the name was found; otherwise, <see langword="false"/>.</returns>
    private static bool TryResolveSoftSwitchName(string name, IMachine machine, out ushort address)
    {
        address = 0;

        var providers = machine.GetComponents<ISoftSwitchProvider>();
        foreach (var provider in providers)
        {
            var switches = provider.GetSoftSwitchStates();
            foreach (var sw in switches)
            {
                if (sw.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    address = sw.Address;
                    return true;
                }
            }
        }

        return false;
    }
}