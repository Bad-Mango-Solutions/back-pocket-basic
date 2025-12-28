// <copyright file="IMachineConstants.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus.Interfaces;

/// <summary>
/// Defines machine-specific constants and limits.
/// </summary>
/// <remarks>
/// <para>
/// Machine constants are interface-required values that ensure every machine handler
/// explicitly declares its constraints. This avoids inline magic numbers and makes
/// validation logic reference named constants.
/// </para>
/// <para>
/// These constants define what a machine type supports, not what a specific instance
/// is configured with. For example, an Apple IIgs supports up to 8MB RAM, but a
/// particular machine instance might be configured with only 1MB.
/// </para>
/// </remarks>
public interface IMachineConstants
{
    /// <summary>
    /// Gets the minimum RAM size supported by this machine type.
    /// </summary>
    /// <value>The minimum RAM in bytes.</value>
    uint MinRamSize { get; }

    /// <summary>
    /// Gets the maximum RAM size supported by this machine type.
    /// </summary>
    /// <value>The maximum RAM in bytes.</value>
    uint MaxRamSize { get; }

    /// <summary>
    /// Gets the default RAM size for this machine type.
    /// </summary>
    /// <value>The default RAM in bytes.</value>
    uint DefaultRamSize { get; }

    /// <summary>
    /// Gets the page size used by this machine's MMU.
    /// </summary>
    /// <value>The page size in bytes (typically 4096).</value>
    uint PageSize { get; }

    /// <summary>
    /// Gets the base address where the boot ROM is mapped.
    /// </summary>
    /// <value>The boot ROM base address.</value>
    Addr BootRomBase { get; }

    /// <summary>
    /// Gets the expected size of the boot ROM.
    /// </summary>
    /// <value>The boot ROM size in bytes.</value>
    uint BootRomSize { get; }

    /// <summary>
    /// Gets the base address where main RAM starts.
    /// </summary>
    /// <value>The RAM base address.</value>
    Addr RamBase { get; }

    /// <summary>
    /// Gets a human-readable name for this machine type.
    /// </summary>
    /// <value>The machine type name.</value>
    string MachineName { get; }

    /// <summary>
    /// Gets a unique identifier for this machine type.
    /// </summary>
    /// <value>The machine type identifier.</value>
    string MachineTypeId { get; }
}