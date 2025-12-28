// <copyright file="IBringUpResult.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus.Interfaces;

/// <summary>
/// Represents the result of machine bring-up, containing all configured components.
/// </summary>
/// <remarks>
/// <para>
/// After bring-up completes, this result provides access to:
/// <list type="bullet">
/// <item><description>The configured region manager with all memory regions</description></item>
/// <item><description>The physical memory pools allocated for this machine</description></item>
/// <item><description>The device registry with all attached devices</description></item>
/// <item><description>Entry point information for starting execution</description></item>
/// </list>
/// </para>
/// </remarks>
public interface IBringUpResult
{
    /// <summary>
    /// Gets a value indicating whether bring-up was successful.
    /// </summary>
    /// <value><see langword="true"/> if bring-up succeeded.</value>
    bool Success { get; }

    /// <summary>
    /// Gets the error message if bring-up failed.
    /// </summary>
    /// <value>The error message, or <see langword="null"/> if successful.</value>
    string? ErrorMessage { get; }

    /// <summary>
    /// Gets the configured region manager.
    /// </summary>
    /// <value>The region manager with all memory regions mapped.</value>
    IRegionManager? RegionManager { get; }

    /// <summary>
    /// Gets the physical memory pools allocated for this machine.
    /// </summary>
    /// <value>A dictionary mapping pool names to physical memory instances.</value>
    IReadOnlyDictionary<string, IPhysicalMemory>? PhysicalMemoryPools { get; }

    /// <summary>
    /// Gets the device registry with all attached devices.
    /// </summary>
    /// <value>The device registry.</value>
    IDeviceRegistry? DeviceRegistry { get; }

    /// <summary>
    /// Gets the entry point address for starting execution.
    /// </summary>
    /// <value>The address where execution should begin.</value>
    Addr EntryPoint { get; }

    /// <summary>
    /// Gets the machine constants used during bring-up.
    /// </summary>
    /// <value>The machine constants.</value>
    IMachineConstants? Constants { get; }
}