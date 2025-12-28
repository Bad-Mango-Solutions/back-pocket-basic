// <copyright file="MemoryRegion.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus;

using Interfaces;

/// <summary>
/// A concrete implementation of <see cref="IMemoryRegion"/> for defining mappable memory blocks.
/// </summary>
/// <remarks>
/// <para>
/// Memory regions are typically created during machine definition to describe the
/// physical memory pools and their preferred locations in the address space.
/// </para>
/// </remarks>
public sealed class MemoryRegion : IMemoryRegion
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryRegion"/> class.
    /// </summary>
    /// <param name="id">Unique identifier for the region.</param>
    /// <param name="name">Human-readable name for the region.</param>
    /// <param name="preferredBase">Suggested base address for mapping.</param>
    /// <param name="target">The bus target handling accesses to this region.</param>
    /// <param name="size">Size of the region in bytes.</param>
    /// <param name="tag">Region classification tag.</param>
    /// <param name="defaultPermissions">Default page permissions.</param>
    /// <param name="capabilities">Target capability flags.</param>
    /// <param name="physicalMemory">Optional underlying physical memory.</param>
    /// <param name="isRelocatable">Whether the region can be mapped elsewhere.</param>
    /// <param name="supportsOverlay">Whether the region can participate in stacks.</param>
    /// <param name="priority">Mapping priority (lower = higher priority).</param>
    public MemoryRegion(
        string id,
        string name,
        Addr preferredBase,
        IBusTarget target,
        uint size,
        RegionTag tag,
        PagePerms defaultPermissions,
        TargetCaps capabilities,
        IPhysicalMemory? physicalMemory = null,
        bool isRelocatable = true,
        bool supportsOverlay = true,
        int priority = 100)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentOutOfRangeException.ThrowIfZero(size);

        Id = id;
        Name = name;
        PreferredBase = preferredBase;
        Target = target;
        Size = size;
        Tag = tag;
        DefaultPermissions = defaultPermissions;
        Capabilities = capabilities;
        PhysicalMemory = physicalMemory;
        IsRelocatable = isRelocatable;
        SupportsOverlay = supportsOverlay;
        Priority = priority;
    }

    /// <inheritdoc />
    public string Id { get; }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public Addr PreferredBase { get; }

    /// <inheritdoc />
    public uint Size { get; }

    /// <inheritdoc />
    public RegionTag Tag { get; }

    /// <inheritdoc />
    public PagePerms DefaultPermissions { get; }

    /// <inheritdoc />
    public TargetCaps Capabilities { get; }

    /// <inheritdoc />
    public IBusTarget Target { get; }

    /// <inheritdoc />
    public IPhysicalMemory? PhysicalMemory { get; }

    /// <inheritdoc />
    public bool IsRelocatable { get; }

    /// <inheritdoc />
    public bool SupportsOverlay { get; }

    /// <inheritdoc />
    public int Priority { get; }

    /// <summary>
    /// Creates a RAM region backed by physical memory.
    /// </summary>
    /// <param name="id">Unique identifier for the region.</param>
    /// <param name="name">Human-readable name for the region.</param>
    /// <param name="preferredBase">Suggested base address for mapping.</param>
    /// <param name="physicalMemory">The physical memory backing this region.</param>
    /// <param name="priority">Mapping priority (lower = higher priority).</param>
    /// <returns>A configured RAM region.</returns>
    public static MemoryRegion CreateRam(
        string id,
        string name,
        Addr preferredBase,
        IPhysicalMemory physicalMemory,
        int priority = 100)
    {
        ArgumentNullException.ThrowIfNull(physicalMemory);

        var target = new RamTarget(physicalMemory.Slice(0, physicalMemory.Size));
        return new MemoryRegion(
            id: id,
            name: name,
            preferredBase: preferredBase,
            target: target,
            size: physicalMemory.Size,
            tag: RegionTag.Ram,
            defaultPermissions: PagePerms.Read | PagePerms.Write | PagePerms.Execute,
            capabilities: TargetCaps.SupportsPeek | TargetCaps.SupportsPoke | TargetCaps.SupportsWide,
            physicalMemory: physicalMemory,
            priority: priority);
    }

    /// <summary>
    /// Creates a ROM region backed by physical memory.
    /// </summary>
    /// <param name="id">Unique identifier for the region.</param>
    /// <param name="name">Human-readable name for the region.</param>
    /// <param name="preferredBase">Suggested base address for mapping.</param>
    /// <param name="physicalMemory">The physical memory backing this region.</param>
    /// <param name="priority">Mapping priority (lower = higher priority).</param>
    /// <returns>A configured ROM region.</returns>
    public static MemoryRegion CreateRom(
        string id,
        string name,
        Addr preferredBase,
        IPhysicalMemory physicalMemory,
        int priority = 0)
    {
        ArgumentNullException.ThrowIfNull(physicalMemory);

        var target = new RomTarget(physicalMemory.ReadOnlySlice(0, physicalMemory.Size));
        return new MemoryRegion(
            id: id,
            name: name,
            preferredBase: preferredBase,
            target: target,
            size: physicalMemory.Size,
            tag: RegionTag.Rom,
            defaultPermissions: PagePerms.Read | PagePerms.Execute,
            capabilities: TargetCaps.SupportsPeek | TargetCaps.SupportsWide,
            physicalMemory: physicalMemory,
            isRelocatable: false,
            supportsOverlay: true,
            priority: priority);
    }
}