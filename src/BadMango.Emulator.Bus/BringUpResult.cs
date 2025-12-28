// <copyright file="BringUpResult.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus;

using Interfaces;

/// <summary>
/// A concrete implementation of <see cref="IBringUpResult"/> for machine bring-up results.
/// </summary>
public sealed class BringUpResult : IBringUpResult
{
    private BringUpResult(
        bool success,
        string? errorMessage,
        IRegionManager? regionManager,
        IReadOnlyDictionary<string, IPhysicalMemory>? physicalMemoryPools,
        IDeviceRegistry? deviceRegistry,
        Addr entryPoint,
        IMachineConstants? constants)
    {
        Success = success;
        ErrorMessage = errorMessage;
        RegionManager = regionManager;
        PhysicalMemoryPools = physicalMemoryPools;
        DeviceRegistry = deviceRegistry;
        EntryPoint = entryPoint;
        Constants = constants;
    }

    /// <inheritdoc />
    public bool Success { get; }

    /// <inheritdoc />
    public string? ErrorMessage { get; }

    /// <inheritdoc />
    public IRegionManager? RegionManager { get; }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, IPhysicalMemory>? PhysicalMemoryPools { get; }

    /// <inheritdoc />
    public IDeviceRegistry? DeviceRegistry { get; }

    /// <inheritdoc />
    public Addr EntryPoint { get; }

    /// <inheritdoc />
    public IMachineConstants? Constants { get; }

    /// <summary>
    /// Creates a successful bring-up result.
    /// </summary>
    /// <param name="regionManager">The configured region manager.</param>
    /// <param name="physicalMemoryPools">The allocated physical memory pools.</param>
    /// <param name="deviceRegistry">The device registry.</param>
    /// <param name="entryPoint">The execution entry point.</param>
    /// <param name="constants">The machine constants.</param>
    /// <returns>A successful result.</returns>
    public static BringUpResult Succeeded(
        IRegionManager regionManager,
        IReadOnlyDictionary<string, IPhysicalMemory> physicalMemoryPools,
        IDeviceRegistry deviceRegistry,
        Addr entryPoint,
        IMachineConstants constants)
    {
        ArgumentNullException.ThrowIfNull(regionManager);
        ArgumentNullException.ThrowIfNull(physicalMemoryPools);
        ArgumentNullException.ThrowIfNull(deviceRegistry);
        ArgumentNullException.ThrowIfNull(constants);

        return new BringUpResult(
            success: true,
            errorMessage: null,
            regionManager: regionManager,
            physicalMemoryPools: physicalMemoryPools,
            deviceRegistry: deviceRegistry,
            entryPoint: entryPoint,
            constants: constants);
    }

    /// <summary>
    /// Creates a failed bring-up result.
    /// </summary>
    /// <param name="errorMessage">The error message describing the failure.</param>
    /// <returns>A failed result.</returns>
    public static BringUpResult Failed(string errorMessage)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage);

        return new BringUpResult(
            success: false,
            errorMessage: errorMessage,
            regionManager: null,
            physicalMemoryPools: null,
            deviceRegistry: null,
            entryPoint: 0,
            constants: null);
    }
}