// <copyright file="ControlRegisterHelpers.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Core;

using System.Runtime.CompilerServices;

/// <summary>
/// Helper methods for Control Register 0 (CR0) manipulation.
/// </summary>
public static class ControlRegisterHelpers
{
    // Privilege Level (bits 0-1)
    private const uint PrivilegeMask = 0x0000_0003;

    // Control flags (bits 2, 4)
    private const uint PagingEnableBit = 0x0000_0004;      // Bit 2

    // Bit 3 is reserved
    private const uint NoExecuteEnableBit = 0x0000_0010;   // Bit 4

    // Interrupt Priority Level (bits 5-7)
    private const uint InterruptPriorityMask = 0x0000_00E0;
    private const int InterruptPriorityShift = 5;

    // Address Space ID (bits 8-15)
    private const uint AsidMask = 0x0000_FF00;
    private const int AsidShift = 8;

    #region Privilege Level

    /// <param name="cr0">The CR0 register value.</param>
    extension(DWord cr0)
    {
        /// <summary>Gets the current privilege level from CR0.</summary>
        /// <returns>The current privilege level.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PrivilegeLevel GetPrivilegeLevel()
            => (PrivilegeLevel)(cr0 & PrivilegeMask);

        /// <summary>Sets the privilege level in CR0.</summary>
        /// <param name="level">The privilege level to set.</param>
        /// <returns>The updated CR0 value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DWord SetPrivilegeLevel(PrivilegeLevel level)
            => (cr0 & ~PrivilegeMask) | (uint)level;

        /// <summary>Gets whether the CPU is currently in user mode.</summary>
        /// <returns>True if in user mode, false otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsUserMode()
            => cr0.GetPrivilegeLevel() == PrivilegeLevel.User;

        /// <summary>Gets whether the CPU is currently in kernel mode.</summary>
        /// <returns>True if in kernel mode, false otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsKernelMode()
            => cr0.GetPrivilegeLevel() == PrivilegeLevel.Kernel;

        /// <summary>Gets whether the CPU is currently in hypervisor mode.</summary>
        /// <returns>True if hypervisor mode, false otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsHypervisorMode()
            => cr0.GetPrivilegeLevel() == PrivilegeLevel.Hypervisor;
    }

    #endregion

    #region Paging Enable (PG)

    /// <param name="cr0">The CR0 register value.</param>
    extension(DWord cr0)
    {
        /// <summary>Gets whether paging is enabled.</summary>
        /// <returns>True if paging is enabled, false otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsPagingEnabled()
            => (cr0 & PagingEnableBit) != 0;

        /// <summary>Sets the paging enable flag in CR0.</summary>
        /// <param name="enabled">Whether to enable paging.</param>
        /// <returns>The updated CR0 value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DWord SetPagingEnabled(bool enabled)
            => enabled ? cr0 | PagingEnableBit : cr0 & ~PagingEnableBit;
    }

    #endregion

    #region No-Execute Enforcement (NXE)

    /// <param name="cr0">The CR0 register value.</param>
    extension(DWord cr0)
    {
        /// <summary>Gets whether no-execute enforcement is enabled.</summary>
        /// <returns>True if NX enforcement is enabled, false otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsNoExecuteEnabled()
            => (cr0 & NoExecuteEnableBit) != 0;

        /// <summary>Sets the no-execute enforcement flag in CR0.</summary>
        /// <param name="enabled">Whether to enable NX enforcement.</param>
        /// <returns>The updated CR0 value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DWord SetNoExecuteEnabled(bool enabled)
            => enabled ? cr0 | NoExecuteEnableBit : cr0 & ~NoExecuteEnableBit;
    }

    #endregion

    #region Interrupt Priority Level

    /// <param name="cr0">The CR0 register value.</param>
    extension(DWord cr0)
    {
        /// <summary>Gets the interrupt priority level (0-7) from CR0.</summary>
        /// <returns>The interrupt priority level (0-7).</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte GetInterruptPriority()
            => (byte)((cr0 & InterruptPriorityMask) >> InterruptPriorityShift);

        /// <summary>Sets the interrupt priority level in CR0.</summary>
        /// <param name="priority">The priority level (0-7).</param>
        /// <returns>The updated CR0 value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DWord SetInterruptPriority(byte priority)
            => (cr0 & ~InterruptPriorityMask) | ((uint)(priority & 0x07) << InterruptPriorityShift);
    }

    #endregion

    #region Address Space ID (ASID)

    /// <param name="cr0">The CR0 register value.</param>
    extension(DWord cr0)
    {
        /// <summary>Gets the Address Space ID (ASID) from CR0.</summary>
        /// <returns>The ASID value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte GetAsid()
            => (byte)((cr0 & AsidMask) >> AsidShift);

        /// <summary>Sets the ASID in CR0.</summary>
        /// <param name="asid">The ASID to set.</param>
        /// <returns>The updated CR0 value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DWord SetAsid(byte asid)
            => (cr0 & ~AsidMask) | ((uint)asid << AsidShift);
    }

    #endregion
}