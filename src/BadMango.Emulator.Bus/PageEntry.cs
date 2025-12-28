// <copyright file="PageEntry.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus;

using Interfaces;

/// <summary>
/// Represents a single entry in the page table for address routing.
/// </summary>
/// <remarks>
/// <para>
/// The page table is the "spine" of the emulator's memory system. Each page entry
/// maps a 4KB address page to a target device and contains metadata for fast routing
/// and capability checking.
/// </para>
/// <para>
/// The page lookup is O(1): <c>pageTable[address >> 12]</c> gives the entry for any address.
/// All routing decisions (device dispatch, atomic vs decomposed, tracing) flow from this lookup.
/// </para>
/// <para>
/// Permissions are checked before touching the device to prevent side effects when an
/// access would fault. NX is checked only for instruction fetch intent and is ignored
/// in Compat mode.
/// </para>
/// <para>
/// Privilege levels control access based on the requestor's ring. In compat mode,
/// all pages default to Ring 0 access (no restrictions). Native mode machines may
/// enforce privilege checking.
/// </para>
/// </remarks>
/// <param name="DeviceId">Structural identifier of the device handling this page.</param>
/// <param name="RegionTag">Classification of the memory region type.</param>
/// <param name="Perms">Permission flags controlling read, write, and execute access.</param>
/// <param name="Caps">Capability flags for the target device.</param>
/// <param name="Target">The bus target implementation for this page.</param>
/// <param name="PhysicalBase">The physical base address within the target's address space.</param>
/// <param name="MinReadPrivilege">
/// Minimum privilege level required for read accesses to this page.
/// The default value, <see cref="PrivilegeLevel.Ring0"/>, allows reads from the most privileged ring only;
/// set this to a less-privileged ring (for example, user mode) to permit reads from code running at that level or higher.
/// </param>
/// <param name="MinWritePrivilege">
/// Minimum privilege level required for write accesses to this page.
/// The default value, <see cref="PrivilegeLevel.Ring0"/>, restricts writes to the most privileged ring;
/// override this to a less-privileged ring when user-mode or guest code must be able to write to the page.
/// </param>
/// <param name="MinExecutePrivilege">
/// Minimum privilege level required for instruction fetch (execute) accesses to this page.
/// The default value, <see cref="PrivilegeLevel.Ring0"/>, limits execution to the most privileged ring;
/// increase this to a less-privileged ring when mapping user-executable code or shared executable regions.
/// </param>
/// <param name="IsSealed">
/// When set to <see langword="true"/>, prevents modification of this page entry from callers running at a lower privilege level,
/// ensuring that critical mappings (such as kernel or hypervisor pages) cannot be altered by less-privileged code.
/// </param>
public readonly record struct PageEntry(
    int DeviceId,
    RegionTag RegionTag,
    PagePerms Perms,
    TargetCaps Caps,
    IBusTarget Target,
    Addr PhysicalBase,
    PrivilegeLevel MinReadPrivilege = PrivilegeLevel.Ring0,
    PrivilegeLevel MinWritePrivilege = PrivilegeLevel.Ring0,
    PrivilegeLevel MinExecutePrivilege = PrivilegeLevel.Ring0,
    bool IsSealed = false)
{
    /// <summary>
    /// Gets a value indicating whether this page supports Peek (read without side effects).
    /// </summary>
    /// <value>
    /// <see langword="true"/> if the target supports Peek operations;
    /// otherwise, <see langword="false"/>.
    /// </value>
    public bool SupportsPeek => (Caps & TargetCaps.SupportsPeek) != 0;

    /// <summary>
    /// Gets a value indicating whether this page supports Poke (write without side effects).
    /// </summary>
    /// <value>
    /// <see langword="true"/> if the target supports Poke operations;
    /// otherwise, <see langword="false"/>.
    /// </value>
    public bool SupportsPoke => (Caps & TargetCaps.SupportsPoke) != 0;

    /// <summary>
    /// Gets a value indicating whether this page supports atomic wide access.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if the target supports atomic 16-bit and 32-bit operations;
    /// otherwise, <see langword="false"/>.
    /// </value>
    public bool SupportsWide => (Caps & TargetCaps.SupportsWide) != 0;

    /// <summary>
    /// Gets a value indicating whether this page has side effects on access.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if accessing this page may cause observable state changes;
    /// otherwise, <see langword="false"/>.
    /// </value>
    public bool HasSideEffects => (Caps & TargetCaps.HasSideEffects) != 0;

    /// <summary>
    /// Gets a value indicating whether this page is timing-sensitive.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if the target behavior depends on access timing;
    /// otherwise, <see langword="false"/>.
    /// </value>
    public bool IsTimingSensitive => (Caps & TargetCaps.TimingSensitive) != 0;

    /// <summary>
    /// Gets a value indicating whether this page allows read access.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if read permission is granted;
    /// otherwise, <see langword="false"/>.
    /// </value>
    public bool CanRead => (Perms & PagePerms.Read) != 0;

    /// <summary>
    /// Gets a value indicating whether this page allows write access.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if write permission is granted;
    /// otherwise, <see langword="false"/>.
    /// </value>
    public bool CanWrite => (Perms & PagePerms.Write) != 0;

    /// <summary>
    /// Gets a value indicating whether this page allows execute access (instruction fetch).
    /// </summary>
    /// <value>
    /// <see langword="true"/> if execute permission is granted;
    /// otherwise, <see langword="false"/>.
    /// </value>
    /// <remarks>
    /// NX enforcement (when this is <see langword="false"/>) applies only to instruction
    /// fetch intent and is ignored in Compat mode.
    /// </remarks>
    public bool CanExecute => (Perms & PagePerms.Execute) != 0;

    /// <summary>
    /// Gets a value indicating whether this page is NX (No Execute).
    /// </summary>
    /// <value>
    /// <see langword="true"/> if execute permission is denied;
    /// otherwise, <see langword="false"/>.
    /// </value>
    public bool IsNx => (Perms & PagePerms.Execute) == 0;
}