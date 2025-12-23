// <copyright file="BusResult{T}.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus;

/// <summary>
/// Represents the result of a bus read operation, containing either the value or a fault.
/// </summary>
/// <typeparam name="T">The type of the value (byte, ushort, or uint).</typeparam>
/// <remarks>
/// <para>
/// Using try-style APIs with <see cref="BusResult{T}"/> keeps faults cheap and predictable
/// in the hot path. A page permission check (including NX) becomes a couple of branches,
/// not a thrown exception that murders performance and obscures control flow.
/// </para>
/// <para>
/// The bus never silently fixes faults; the CPU receives full information about what
/// happened and can translate it into its architecture's exception/abort mechanism.
/// </para>
/// </remarks>
/// <param name="Value">The read value if successful; undefined if a fault occurred.</param>
/// <param name="Fault">The fault information; <see cref="FaultKind.None"/> if successful.</param>
/// <param name="Cycles">The number of cycles consumed by this operation.</param>
public readonly record struct BusResult<T>(T Value, BusFault Fault, ulong Cycles = 0)
    where T : struct
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
    /// Implicitly converts a fault to a faulted result.
    /// </summary>
    /// <param name="fault">The fault that occurred.</param>
    public static implicit operator BusResult<T>(BusFault fault) => FromFault(fault);

    /// <summary>
    /// Creates a successful result with the specified value.
    /// </summary>
    /// <param name="value">The value that was read.</param>
    /// <param name="cycles">The number of cycles consumed.</param>
    /// <returns>A successful <see cref="BusResult{T}"/>.</returns>
    public static BusResult<T> Success(T value, ulong cycles = 0) => new(value, BusFault.Success(), cycles);

    /// <summary>
    /// Creates a successful result with context.
    /// </summary>
    /// <param name="value">The value that was read.</param>
    /// <param name="access">The bus access that succeeded.</param>
    /// <param name="deviceId">The device ID that handled the access.</param>
    /// <param name="regionTag">The region tag of the accessed page.</param>
    /// <param name="cycles">The number of cycles consumed.</param>
    /// <returns>A successful <see cref="BusResult{T}"/>.</returns>
    public static BusResult<T> Success(T value, in BusAccess access, int deviceId, RegionTag regionTag, ulong cycles = 0) =>
        new(value, BusFault.Success(in access, deviceId, regionTag), cycles);

    /// <summary>
    /// Creates a faulted result.
    /// </summary>
    /// <param name="fault">The fault that occurred.</param>
    /// <param name="cycles">The number of cycles consumed before the fault.</param>
    /// <returns>A faulted <see cref="BusResult{T}"/>.</returns>
    public static BusResult<T> FromFault(BusFault fault, ulong cycles = 0) => new(default!, fault, cycles);
}