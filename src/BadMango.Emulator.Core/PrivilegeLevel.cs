// <copyright file="PrivilegeLevel.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Core;

/// <summary>
/// Defines the privilege levels for the 65832 processor.
/// </summary>
/// <remarks>
/// Privilege levels are encoded in bits 0-1 of the CR0 (Control Register 0).
/// Higher numeric values indicate higher privilege.
/// </remarks>
public enum PrivilegeLevel : byte
{
    /// <summary>User mode - lowest privilege, memory protection enforced.</summary>
    User = 0,

    /// <summary>Kernel mode - full access to system resources and privileged instructions.</summary>
    Kernel = 1,

    /// <summary>Hypervisor mode - manages virtual machines and guest kernels (reserved).</summary>
    /// <remarks>
    /// Reserved for future virtualization support. Systems without H mode
    /// treat this as equivalent to Kernel mode.
    /// </remarks>
    Hypervisor = 2,

    /// <summary>Reserved for future use.</summary>
    Reserved = 3,
}