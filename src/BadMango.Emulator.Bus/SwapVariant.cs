// <copyright file="SwapVariant.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus;

using Interfaces;

/// <summary>
/// Represents a variant within a swap group for bank switching.
/// </summary>
/// <remarks>
/// <para>
/// Swap variants define mutually exclusive memory configurations within a swap group.
/// At any time, only one variant can be active per swap group. When a variant is selected,
/// the page table entries for the swap group's address range are updated to point to
/// the variant's target and physical base.
/// </para>
/// <para>
/// This is used for scenarios like the Apple II Language Card, which has two 4KB banks
/// for the D000-DFFF range; only one bank is active at a time.
/// </para>
/// </remarks>
/// <param name="Name">The unique name identifying this variant within its swap group.</param>
/// <param name="Target">The bus target for this variant.</param>
/// <param name="PhysBase">The physical base address within the target's address space.</param>
/// <param name="Perms">Permission flags for this variant's pages.</param>
public readonly record struct SwapVariant(
    string Name,
    IBusTarget Target,
    Addr PhysBase,
    PagePerms Perms);