// <copyright file="BusFault.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus;

/// <summary>
/// Represents a fault that occurred during a bus operation.
/// </summary>
/// <remarks>
/// <para>
/// The fault payload carries exactly what tooling and exception logic need
/// without allocations. The CPU can record the fault address, kind, and intent
/// into its fault registers without the bus knowing anything about the CPU's
/// exception model.
/// </para>
/// <para>
/// Faults are first-class return values from bus operations, not exceptions.
/// This keeps fault handling cheap and predictable in the hot path.
/// </para>
/// </remarks>
/// <param name="Kind">The type of fault that occurred.</param>
/// <param name="Address">The address where the fault occurred.</param>
/// <param name="WidthBits">The width of the attempted access (8, 16, or 32).</param>
/// <param name="Intent">The intent of the access that faulted.</param>
/// <param name="Mode">The CPU mode at the time of the fault.</param>
/// <param name="SourceId">The structural ID of the access initiator.</param>
/// <param name="DeviceId">The device ID from the page entry, or -1 if unmapped.</param>
/// <param name="RegionTag">The region tag from the page entry, if available.</param>
/// <param name="Cycle">The machine cycle when the fault occurred.</param>
public readonly record struct BusFault(
    FaultKind Kind,
    Addr Address,
    byte WidthBits,
    AccessIntent Intent,
    CpuMode Mode,
    int SourceId,
    int DeviceId,
    RegionTag RegionTag,
    ulong Cycle)
{
    /// <summary>
    /// Gets a value indicating whether this represents no fault (success).
    /// </summary>
    /// <value>
    /// <see langword="true"/> if <see cref="Kind"/> is <see cref="FaultKind.None"/>;
    /// otherwise, <see langword="false"/>.
    /// </value>
    public bool IsSuccess => Kind == FaultKind.None;

    /// <summary>
    /// Gets a value indicating whether a fault occurred.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if <see cref="Kind"/> is not <see cref="FaultKind.None"/>;
    /// otherwise, <see langword="false"/>.
    /// </value>
    public bool IsFault => Kind != FaultKind.None;

    /// <summary>
    /// Gets a value indicating whether this is an NX fault.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if <see cref="Kind"/> is <see cref="FaultKind.Nx"/>;
    /// otherwise, <see langword="false"/>.
    /// </value>
    public bool IsNxFault => Kind == FaultKind.Nx;

    /// <summary>
    /// Gets a value indicating whether this is a permission fault.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if <see cref="Kind"/> is <see cref="FaultKind.Permission"/>;
    /// otherwise, <see langword="false"/>.
    /// </value>
    public bool IsPermissionFault => Kind == FaultKind.Permission;

    /// <summary>
    /// Gets a value indicating whether this is an unmapped fault.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if <see cref="Kind"/> is <see cref="FaultKind.Unmapped"/>;
    /// otherwise, <see langword="false"/>.
    /// </value>
    public bool IsUnmappedFault => Kind == FaultKind.Unmapped;

    /// <summary>
    /// Gets a value indicating whether this is a device fault.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if <see cref="Kind"/> is <see cref="FaultKind.DeviceFault"/>;
    /// otherwise, <see langword="false"/>.
    /// </value>
    public bool IsDeviceFault => Kind == FaultKind.DeviceFault;

    /// <summary>
    /// Creates a success (no fault) result.
    /// </summary>
    /// <returns>A <see cref="BusFault"/> with <see cref="Kind"/> set to <see cref="FaultKind.None"/>.</returns>
    public static BusFault Success() => new(FaultKind.None, 0, 0, AccessIntent.DataRead, CpuMode.Native, 0, 0, RegionTag.Unknown, 0);

    /// <summary>
    /// Creates a success (no fault) result with context from an access.
    /// </summary>
    /// <param name="access">The bus access that succeeded.</param>
    /// <param name="deviceId">The device ID that handled the access.</param>
    /// <param name="regionTag">The region tag of the accessed page.</param>
    /// <returns>A <see cref="BusFault"/> with <see cref="Kind"/> set to <see cref="FaultKind.None"/>.</returns>
    public static BusFault Success(in BusAccess access, int deviceId, RegionTag regionTag) =>
        new(FaultKind.None, access.Address, access.WidthBits, access.Intent, access.Mode, access.SourceId, deviceId, regionTag, access.Cycle);

    /// <summary>
    /// Creates an unmapped fault.
    /// </summary>
    /// <param name="access">The bus access that faulted.</param>
    /// <returns>A <see cref="BusFault"/> with <see cref="Kind"/> set to <see cref="FaultKind.Unmapped"/>.</returns>
    public static BusFault Unmapped(in BusAccess access) =>
        new(FaultKind.Unmapped, access.Address, access.WidthBits, access.Intent, access.Mode, access.SourceId, -1, RegionTag.Unmapped, access.Cycle);

    /// <summary>
    /// Creates a permission fault.
    /// </summary>
    /// <param name="access">The bus access that faulted.</param>
    /// <param name="deviceId">The device ID from the page entry.</param>
    /// <param name="regionTag">The region tag from the page entry.</param>
    /// <returns>A <see cref="BusFault"/> with <see cref="Kind"/> set to <see cref="FaultKind.Permission"/>.</returns>
    public static BusFault PermissionDenied(in BusAccess access, int deviceId, RegionTag regionTag) =>
        new(FaultKind.Permission, access.Address, access.WidthBits, access.Intent, access.Mode, access.SourceId, deviceId, regionTag, access.Cycle);

    /// <summary>
    /// Creates an NX (no execute) fault.
    /// </summary>
    /// <param name="access">The bus access that faulted.</param>
    /// <param name="deviceId">The device ID from the page entry.</param>
    /// <param name="regionTag">The region tag from the page entry.</param>
    /// <returns>A <see cref="BusFault"/> with <see cref="Kind"/> set to <see cref="FaultKind.Nx"/>.</returns>
    public static BusFault NoExecute(in BusAccess access, int deviceId, RegionTag regionTag) =>
        new(FaultKind.Nx, access.Address, access.WidthBits, access.Intent, access.Mode, access.SourceId, deviceId, regionTag, access.Cycle);

    /// <summary>
    /// Creates a misaligned access fault.
    /// </summary>
    /// <param name="access">The bus access that faulted.</param>
    /// <param name="deviceId">The device ID from the page entry.</param>
    /// <param name="regionTag">The region tag from the page entry.</param>
    /// <returns>A <see cref="BusFault"/> with <see cref="Kind"/> set to <see cref="FaultKind.Misaligned"/>.</returns>
    public static BusFault Misaligned(in BusAccess access, int deviceId, RegionTag regionTag) =>
        new(FaultKind.Misaligned, access.Address, access.WidthBits, access.Intent, access.Mode, access.SourceId, deviceId, regionTag, access.Cycle);

    /// <summary>
    /// Creates a device fault.
    /// </summary>
    /// <param name="access">The bus access that faulted.</param>
    /// <param name="deviceId">The device ID that reported the fault.</param>
    /// <param name="regionTag">The region tag from the page entry.</param>
    /// <returns>A <see cref="BusFault"/> with <see cref="Kind"/> set to <see cref="FaultKind.DeviceFault"/>.</returns>
    public static BusFault Device(in BusAccess access, int deviceId, RegionTag regionTag) =>
        new(FaultKind.DeviceFault, access.Address, access.WidthBits, access.Intent, access.Mode, access.SourceId, deviceId, regionTag, access.Cycle);
}