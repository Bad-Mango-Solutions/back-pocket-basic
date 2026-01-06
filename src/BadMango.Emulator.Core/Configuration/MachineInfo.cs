// <copyright file="MachineInfo.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Core.Configuration;

/// <summary>
/// Provides display-friendly information about a machine configuration.
/// </summary>
/// <remarks>
/// This record encapsulates the key details of a machine profile in a format
/// suitable for display in user interfaces, banners, and status messages.
/// </remarks>
/// <param name="Name">The unique identifier for the machine profile.</param>
/// <param name="DisplayName">The human-readable name of the machine.</param>
/// <param name="CpuType">The CPU type identifier (e.g., "65C02", "6502").</param>
/// <param name="MemorySize">The total memory size in bytes.</param>
/// <param name="Description">An optional description of the machine configuration.</param>
public sealed record MachineInfo(
    string Name,
    string DisplayName,
    string CpuType,
    uint MemorySize,
    string? Description = null)
{
    /// <summary>
    /// Gets the memory size formatted as a human-readable string (e.g., "64KB", "128KB").
    /// </summary>
    public string FormattedMemorySize => FormatMemorySize(MemorySize);

    /// <summary>
    /// Gets a short summary suitable for display in a banner or status line.
    /// </summary>
    /// <remarks>
    /// Returns a string in the format "DisplayName (CpuType + FormattedMemorySize RAM)".
    /// For example: "Simple 65C02 System (65C02 + 64KB RAM)".
    /// </remarks>
    public string Summary => $"{DisplayName} ({CpuType} + {FormattedMemorySize} RAM)";

    /// <summary>
    /// Creates a <see cref="MachineInfo"/> from a <see cref="MachineProfile"/>.
    /// </summary>
    /// <param name="profile">The machine profile to convert.</param>
    /// <returns>A new <see cref="MachineInfo"/> containing the profile's display information.</returns>
    public static MachineInfo FromProfile(MachineProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        uint memorySize = CalculateMemorySize(profile);

        return new MachineInfo(
            Name: profile.Name,
            DisplayName: profile.DisplayName ?? profile.Name,
            CpuType: profile.Cpu.Type,
            MemorySize: memorySize,
            Description: profile.Description);
    }

    /// <summary>
    /// Calculates the total memory size from the profile's memory configuration.
    /// </summary>
    /// <param name="profile">The machine profile.</param>
    /// <returns>The total memory size in bytes.</returns>
    private static uint CalculateMemorySize(MachineProfile profile)
    {
        var memoryConfig = profile.Memory;

        if (memoryConfig.UsesRegions)
        {
            uint totalSize = 0;
            foreach (var region in memoryConfig.Regions!)
            {
                if (HexParser.TryParseUInt32(region.Size, out uint regionSize))
                {
                    totalSize += regionSize;
                }
            }

            return totalSize;
        }

        // Default to address space size if no regions defined
        return (uint)(1 << profile.AddressSpace);
    }

    /// <summary>
    /// Formats a memory size in bytes to a human-readable string.
    /// </summary>
    /// <param name="bytes">The memory size in bytes.</param>
    /// <returns>A formatted string (e.g., "64KB", "1MB", "16MB").</returns>
    private static string FormatMemorySize(uint bytes)
    {
        return bytes switch
        {
            >= 1024 * 1024 when bytes % (1024 * 1024) == 0 => $"{bytes / (1024 * 1024)}MB",
            >= 1024 when bytes % 1024 == 0 => $"{bytes / 1024}KB",
            _ => $"{bytes} bytes",
        };
    }
}