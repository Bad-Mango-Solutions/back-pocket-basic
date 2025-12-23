// <copyright file="BusResult.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus;

/// <summary>
/// Represents the result of a bus write operation, containing fault information if any.
/// </summary>
/// <remarks>
/// <para>
/// This non-generic version is used for write operations that don't return a value,
/// but still need to report faults and timing information.
/// </para>
/// <para>
/// Using try-style APIs with <see cref="BusResult"/> keeps faults cheap and predictable
/// in the hot path. A page permission check becomes a couple of branches,
/// not a thrown exception that murders performance and obscures control flow.
/// </para>
/// </remarks>
/// <param name="Fault">The fault information; <see cref="FaultKind.None"/> if successful.</param>
/// <param name="Cycles">The number of cycles consumed by this operation.</param>
public readonly record struct BusResult(BusFault Fault, ulong Cycles = 0)
{
    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if no fault occurred; otherwise, <see langword="false"/>.
    /// </value>
    public bool Ok => Fault.Kind == FaultKind.None;

    /// <summary>
    /// Gets a value indicating whether the operation faulted.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if a fault occurred; otherwise, <see langword="false"/>.
    /// </value>
    public bool Failed => Fault.Kind != FaultKind.None;

    /// <summary>
    /// Implicitly converts a fault to a result.
    /// </summary>
    /// <param name="fault">The fault that occurred.</param>
    public static implicit operator BusResult(BusFault fault) => FromFault(fault);

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="cycles">The number of cycles consumed.</param>
    /// <returns>A successful <see cref="BusResult"/>.</returns>
    public static BusResult Success(ulong cycles = 0) => new(BusFault.Success(), cycles);

    /// <summary>
    /// Creates a successful result with context.
    /// </summary>
    /// <param name="access">The bus access that succeeded.</param>
    /// <param name="deviceId">The device ID that handled the access.</param>
    /// <param name="regionTag">The region tag of the accessed page.</param>
    /// <param name="cycles">The number of cycles consumed.</param>
    /// <returns>A successful <see cref="BusResult"/>.</returns>
    public static BusResult Success(in BusAccess access, int deviceId, RegionTag regionTag, ulong cycles = 0) =>
        new(BusFault.Success(in access, deviceId, regionTag), cycles);

    /// <summary>
    /// Creates a faulted result.
    /// </summary>
    /// <param name="fault">The fault that occurred.</param>
    /// <param name="cycles">The number of cycles consumed before the fault.</param>
    /// <returns>A faulted <see cref="BusResult"/>.</returns>
    public static BusResult FromFault(BusFault fault, ulong cycles = 0) => new(fault, cycles);
}