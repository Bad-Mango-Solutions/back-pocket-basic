// <copyright file="PrivilegeLevel.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus;

/// <summary>
/// Defines the privilege levels for memory access control.
/// </summary>
/// <remarks>
/// <para>
/// Privilege levels follow a ring model where lower values indicate higher privilege.
/// Ring 0 (Hypervisor) has the highest privilege, Ring 3 (User) has the lowest.
/// </para>
/// <para>
/// In compatibility mode (emulating Apple II), all operations run at Ring 0
/// with no privilege enforcement. Native mode machines may enforce privilege
/// levels to implement protected memory and system calls.
/// </para>
/// </remarks>
public enum PrivilegeLevel : byte
{
    /// <summary>
    /// Ring 0: Hypervisor level. Full access to all resources. Can modify any page entry.
    /// </summary>
    Ring0 = 0,

    /// <summary>
    /// Ring 1: Kernel level. Can modify most page permissions and access kernel memory.
    /// </summary>
    Ring1 = 1,

    /// <summary>
    /// Ring 2: Reserved for future use. Typically used for device drivers.
    /// </summary>
    Ring2 = 2,

    /// <summary>
    /// Ring 3: User level. Lowest privilege, restricted by page permissions.
    /// </summary>
    Ring3 = 3,
}