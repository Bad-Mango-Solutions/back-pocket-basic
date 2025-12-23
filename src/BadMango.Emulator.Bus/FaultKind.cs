// <copyright file="FaultKind.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus;

/// <summary>
/// Defines the types of faults that can occur during bus operations.
/// </summary>
/// <remarks>
/// <para>
/// The fault model provides first-class return values from bus operations,
/// allowing the CPU to react correctly to access failures without relying
/// on exceptions. The bus never silently fixes faults and the CPU never
/// guesses why an access failed.
/// </para>
/// <para>
/// NX faults are enforced only on instruction fetch intent. Data reads
/// on NX pages are allowed unless other permissions forbid it.
/// </para>
/// </remarks>
public enum FaultKind : byte
{
    /// <summary>
    /// No fault occurred; the operation completed successfully.
    /// </summary>
    None = 0,

    /// <summary>
    /// The address has no page entry or maps to a memory hole.
    /// </summary>
    /// <remarks>
    /// Corresponds to a "translation fault" or "bus error" in the CPU's
    /// exception model.
    /// </remarks>
    Unmapped,

    /// <summary>
    /// The requested operation violates page permissions (read/write not allowed).
    /// </summary>
    /// <remarks>
    /// Corresponds to a "data access fault" in the CPU's exception model.
    /// </remarks>
    Permission,

    /// <summary>
    /// Execute permission denied on instruction fetch.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This fault triggers only when <see cref="AccessIntent.InstructionFetch"/>
    /// is attempted on a page without execute permission. Data reads on the
    /// same page are allowed.
    /// </para>
    /// <para>
    /// Corresponds to an "instruction abort" or "execute access fault" in the
    /// CPU's exception model.
    /// </para>
    /// <para>
    /// In Compat mode, NX enforcement is ignored per policy.
    /// </para>
    /// </remarks>
    Nx,

    /// <summary>
    /// The access violates alignment requirements for atomic wide operations.
    /// </summary>
    /// <remarks>
    /// Applies only when Native mode enforces alignment rules for atomic
    /// 16-bit or 32-bit operations.
    /// </remarks>
    Misaligned,

    /// <summary>
    /// A device signaled a fault during the access.
    /// </summary>
    /// <remarks>
    /// Used for "bus error"-style conditions where the device itself reports
    /// an error, such as accessing an unimplemented register or an illegal
    /// DMA operation.
    /// </remarks>
    DeviceFault,
}