// <copyright file="LanguageCardSplitTarget.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Devices;

using BadMango.Emulator.Bus;
using BadMango.Emulator.Bus.Interfaces;

/// <summary>
/// A composite bus target used by the Language Card to implement the hardware
/// "split" read/write mode at $D000-$FFFF: reads come from ROM, writes go to LC RAM.
/// </summary>
/// <remarks>
/// <para>
/// On a real Apple II, soft switches $C081/$C089 (and the write-enabled state of
/// $C083/$C08B before the second R*2 read promotes them) select a mode where the
/// CPU reads the underlying system ROM but writes to the Language Card RAM for the
/// same virtual address. This is the mode DOS 3.3's $BFCB sequence uses to copy
/// ROM contents into the LC RAM banks during boot.
/// </para>
/// <para>
/// The bus's page table only stores a single target per page, so this composite
/// target acts as the page target for the affected pages while the LC is in
/// split mode. It implements <see cref="ICompositeTarget"/> so the bus's read /
/// write hot paths dispatch through <see cref="ResolveTarget"/>; this target
/// returns itself, and then forwards the access to either the read or write
/// underlying target according to <see cref="BusAccess.Intent"/> as observed in
/// <see cref="Read8"/> and <see cref="Write8"/>.
/// </para>
/// <para>
/// Read routing is backed by one or more <see cref="RomPageBinding"/> descriptors,
/// one per 4 KB page in the covered region, allowing $E000-$EFFF and $F000-$FFFF
/// to be independently routed to different ROM targets when the system ROM is
/// composed from multiple page-level overlays. If a page has no <see cref="RomPageBinding"/>,
/// reads return <c>0xFF</c> (floating-bus value).
/// </para>
/// <para>
/// Address translation is handled internally: callers configure the split target
/// with the virtual base of the region it covers plus the per-target physical
/// base offsets, and accesses are routed using the original
/// <see cref="BusAccess.Address"/> (ignoring the bus-computed
/// <c>physicalAddress</c> parameter, which is necessarily a single value and
/// cannot satisfy both the ROM and RAM targets simultaneously).
/// </para>
/// </remarks>
internal sealed class LanguageCardSplitTarget : ICompositeTarget
{
    /// <summary>Size of a single bus page in bytes (4 KB).</summary>
    private const Addr BusPageSize = 0x1000;

    private readonly RomPageBinding[] romBindings;
    private readonly IBusTarget writeTarget;
    private readonly Addr writePhysBaseAtRegion;
    private readonly Addr regionVirtualBase;
    private readonly Addr regionSize;
    private readonly string name;

    /// <summary>
    /// Initializes a new instance of the <see cref="LanguageCardSplitTarget"/> class.
    /// </summary>
    /// <param name="romBindings">
    /// One or more <see cref="RomPageBinding"/> entries that describe the ROM pages that
    /// service reads. Each entry covers one 4 KB page. Pages within <paramref name="regionVirtualBase"/>
    /// to <paramref name="regionVirtualBase"/> + <paramref name="regionSize"/> that have no
    /// corresponding binding return <c>0xFF</c> for split-mode reads.
    /// </param>
    /// <param name="writeTarget">The target that services writes (typically Language Card RAM).</param>
    /// <param name="writePhysBaseAtRegion">
    /// The physical address within <paramref name="writeTarget"/> that corresponds to
    /// <paramref name="regionVirtualBase"/>. Pass <c>0</c> when the LC RAM target is
    /// sized exactly to the covered region.
    /// </param>
    /// <param name="regionVirtualBase">
    /// The starting virtual address of the region this split target covers (for example, $E000).
    /// </param>
    /// <param name="regionSize">
    /// The size in bytes of the region this split target covers (for example, $2000 for $E000-$FFFF).
    /// </param>
    /// <param name="name">A human-readable name for diagnostics.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="romBindings"/>, <paramref name="writeTarget"/>, or <paramref name="name"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="romBindings"/> is empty.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="regionSize"/> is zero.
    /// </exception>
    public LanguageCardSplitTarget(
        IReadOnlyList<RomPageBinding> romBindings,
        IBusTarget writeTarget,
        Addr writePhysBaseAtRegion,
        Addr regionVirtualBase,
        Addr regionSize,
        string name)
    {
        ArgumentNullException.ThrowIfNull(romBindings);
        ArgumentNullException.ThrowIfNull(writeTarget);
        ArgumentNullException.ThrowIfNull(name);
        if (romBindings.Count == 0)
        {
            throw new ArgumentException("At least one ROM page binding is required.", nameof(romBindings));
        }

        if (regionSize == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(regionSize), regionSize, "Region size must be greater than zero.");
        }

        this.romBindings = [.. romBindings];
        this.writeTarget = writeTarget;
        this.writePhysBaseAtRegion = writePhysBaseAtRegion;
        this.regionVirtualBase = regionVirtualBase;
        this.regionSize = regionSize;
        this.name = name;
    }

    /// <inheritdoc />
    public string Name => name;

    /// <inheritdoc />
    public TargetCaps Capabilities
    {
        get
        {
            TargetCaps caps = writeTarget.Capabilities;
            foreach (var binding in romBindings)
            {
                caps |= binding.Target.Capabilities;
            }

            return caps;
        }
    }

    /// <summary>
    /// Gets the write target (Language Card RAM).
    /// </summary>
    /// <value>The bus target servicing writes in split mode.</value>
    public IBusTarget WriteTarget => writeTarget;

    /// <inheritdoc />
    /// <remarks>
    /// Dispatches the read to the <see cref="RomPageBinding"/> whose
    /// <see cref="RomPageBinding.VirtualPageBase"/> covers <see cref="BusAccess.Address"/>.
    /// Each binding covers exactly one 4 KB page so at most one binding matches.
    /// Pages with no binding (not a typical configuration but structurally possible)
    /// return <c>0xFF</c>, the floating-bus value. The bus-supplied
    /// <paramref name="physicalAddress"/> is ignored because it reflects only the
    /// write target's physical base.
    /// </remarks>
    public byte Read8(Addr physicalAddress, in BusAccess access)
    {
        Addr addr = access.Address;
        foreach (var binding in romBindings)
        {
            if (addr >= binding.VirtualPageBase && addr < binding.VirtualPageBase + BusPageSize)
            {
                return binding.Target.Read8(binding.PhysicalBase + (addr - binding.VirtualPageBase), access);
            }
        }

        // No ROM binding for this address: return floating-bus value.
        return 0xFF;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Forwards the write to <see cref="WriteTarget"/> at the translated RAM physical
    /// address. The bus-supplied <paramref name="physicalAddress"/> is ignored in
    /// favor of an explicit translation from <see cref="BusAccess.Address"/> so the
    /// behavior is symmetric with <see cref="Read8"/>.
    /// </remarks>
    public void Write8(Addr physicalAddress, byte value, in BusAccess access)
    {
        Addr offset = (Addr)(access.Address - regionVirtualBase);
        writeTarget.Write8(writePhysBaseAtRegion + offset, value, access);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Always returns <see langword="this"/>; routing between the read and write
    /// underlying targets is performed inside <see cref="Read8"/> and
    /// <see cref="Write8"/> rather than by returning different sub-targets here.
    /// Returning a different target per intent would not work because the bus calls
    /// <c>subTarget.Read8(physicalAddress, ...)</c> with the page's single
    /// <c>PhysicalBase</c>, which cannot simultaneously address both ROM and RAM.
    /// </remarks>
    public IBusTarget? ResolveTarget(Addr offset, AccessIntent intent) => this;

    /// <inheritdoc />
    /// <remarks>
    /// Returns <see cref="RegionTag.Ram"/> because the split target is, from the bus
    /// page table's perspective, a single overlay region. The underlying read target's
    /// ROM nature is observable through the <see cref="RomPageBinding"/> list for
    /// tooling that needs it; the page-level tag tracks the overlay (LC RAM) rather
    /// than the read pass-through.
    /// </remarks>
    public RegionTag GetSubRegionTag(Addr offset) => RegionTag.Ram;

    /// <inheritdoc />
    public IEnumerable<(Addr StartOffset, Addr Size, RegionTag Tag, string TargetName)> EnumerateSubRegions()
    {
        // The split target is logically a single sub-region from the bus's perspective.
        // Report it once so observability tools can describe it.
        yield return (0, regionSize, RegionTag.Ram, name);
    }
}