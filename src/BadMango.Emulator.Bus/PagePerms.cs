// <copyright file="PagePerms.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus;

/// <summary>
/// Permission flags for page-level access control.
/// </summary>
/// <remarks>
/// <para>
/// Page permissions are checked before touching the device, preventing
/// side effects when an access would fault. NX is checked only for
/// instruction fetch intent.
/// </para>
/// <para>
/// In Compat mode, NX (X) permission is ignored per policy.
/// </para>
/// </remarks>
[Flags]
public enum PagePerms : byte
{
    /// <summary>
    /// No permissions granted; all access denied.
    /// </summary>
    None = 0,

    /// <summary>
    /// Read permission granted.
    /// </summary>
    Read = 1 << 0,

    /// <summary>
    /// Write permission granted.
    /// </summary>
    Write = 1 << 1,

    /// <summary>
    /// Execute permission granted.
    /// </summary>
    /// <remarks>
    /// When this flag is absent (NX), instruction fetch will fault.
    /// Data reads are still allowed if <see cref="Read"/> is set.
    /// NX is ignored in Compat mode.
    /// </remarks>
    Execute = 1 << 2,

    /// <summary>
    /// Read and write permissions (typical for RAM).
    /// </summary>
    ReadWrite = Read | Write,

    /// <summary>
    /// Read and execute permissions (typical for ROM).
    /// </summary>
    ReadExecute = Read | Execute,

    /// <summary>
    /// Full permissions: read, write, and execute.
    /// </summary>
    All = Read | Write | Execute,
}