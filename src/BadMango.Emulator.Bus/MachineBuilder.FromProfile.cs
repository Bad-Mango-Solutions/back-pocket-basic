// <copyright file="MachineBuilder.FromProfile.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus;

using BadMango.Emulator.Core.Configuration;
using BadMango.Emulator.Core.Cpu;

using Interfaces;

/// <summary>
/// Profile processing methods for <see cref="MachineBuilder"/>.
/// </summary>
public sealed partial class MachineBuilder
{
    /// <summary>
    /// Configures the builder from a machine profile.
    /// </summary>
    /// <param name="profile">The machine profile to use for configuration.</param>
    /// <param name="pathResolver">
    /// Optional path resolver for loading ROM files. If <see langword="null"/>, uses the resolver
    /// provided to <see cref="Create"/>, or creates a default resolver with no library root.
    /// </param>
    /// <returns>This builder instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="profile"/> is null.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the profile memory configuration is invalid or uses unsupported region types.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method configures the builder based on a machine profile, including:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Address space size from <see cref="MachineProfile.AddressSpace"/></description></item>
    /// <item><description>CPU family from <see cref="CpuProfileSection.Type"/></description></item>
    /// <item><description>Physical memory from <see cref="MemoryProfileSection.Physical"/></description></item>
    /// <item><description>Memory regions from <see cref="MemoryProfileSection.Regions"/></description></item>
    /// </list>
    /// <para>
    /// The profile must use the regions-based memory configuration format with physical memory blocks.
    /// </para>
    /// <para>
    /// When using the builder created via <see cref="Create"/>, you typically don't need
    /// to pass a path resolver to this method:
    /// </para>
    /// <code>
    /// var machine = MachineBuilder.Create(pathResolver, cpuFactory)
    ///     .FromProfile(profile)  // Uses pre-configured resolver
    ///     .Build();
    /// </code>
    /// </remarks>
    public MachineBuilder FromProfile(MachineProfile profile, ProfilePathResolver? pathResolver = null)
    {
        ArgumentNullException.ThrowIfNull(profile);

        // Use provided resolver, or fall back to pre-configured resolver, or create default
        var effectiveResolver = pathResolver ?? profilePathResolver ?? new ProfilePathResolver(null);
        profilePathResolver = effectiveResolver;

        // Configure address space
        addressSpaceBits = profile.AddressSpace;

        // Configure CPU family
        cpuFamily = profile.Cpu.Type.ToUpperInvariant() switch
        {
            "65C02" => CpuFamily.Cpu65C02,
            "65816" => CpuFamily.Cpu65C816,
            "65832" => CpuFamily.Cpu65832,
            _ => throw new NotSupportedException($"CPU type '{profile.Cpu.Type}' is not supported."),
        };

        // Configure memory regions
        var memoryConfig = profile.Memory;
        if (!memoryConfig.UsesRegions)
        {
            throw new InvalidOperationException(
                "Memory configuration must use the 'regions' array format. " +
                "The legacy 'size' and 'type' properties are no longer supported.");
        }

        // Build ROM image lookup
        var romImages = BuildRomImageLookup(memoryConfig, effectiveResolver);

        // Create physical memory blocks
        if (memoryConfig.UsesPhysicalMemory)
        {
            foreach (var physical in memoryConfig.Physical!)
            {
                CreatePhysicalMemory(physical, romImages, effectiveResolver);
            }
        }

        // Configure regions
        foreach (var region in memoryConfig.Regions!)
        {
            ConfigureRegion(region, effectiveResolver);
        }

        // Configure devices (motherboard devices from the devices.motherboard section)
        if (profile.Devices?.Motherboard is not null)
        {
            foreach (var deviceEntry in profile.Devices.Motherboard)
            {
                ConfigureMotherboardDevice(deviceEntry);
            }
        }

        // Configure slot cards (from the devices.slots.cards section)
        if (profile.Devices?.Slots?.Cards is not null && profile.Devices.Slots.Enabled)
        {
            foreach (var cardEntry in profile.Devices.Slots.Cards)
            {
                ConfigureSlotCard(cardEntry);
            }
        }

        return this;
    }

    private static Dictionary<string, (string ResolvedPath, uint Size)> BuildRomImageLookup(
        MemoryProfileSection memoryConfig,
        ProfilePathResolver pathResolver)
    {
        var lookup = new Dictionary<string, (string ResolvedPath, uint Size)>(StringComparer.OrdinalIgnoreCase);

        if (memoryConfig.RomImages is null)
        {
            return lookup;
        }

        foreach (var romImage in memoryConfig.RomImages)
        {
            if (string.IsNullOrEmpty(romImage.Name))
            {
                throw new InvalidOperationException("ROM image must have a name.");
            }

            if (lookup.ContainsKey(romImage.Name))
            {
                throw new InvalidOperationException($"Duplicate ROM image name: '{romImage.Name}'.");
            }

            string resolvedPath = pathResolver.Resolve(romImage.Source);
            uint size = HexParser.ParseUInt32(romImage.Size);

            // Validate that the ROM file exists
            if (!File.Exists(resolvedPath))
            {
                throw new FileNotFoundException(
                    $"ROM image '{romImage.Name}' not found at '{resolvedPath}'. " +
                    $"Source path: '{romImage.Source}'.",
                    resolvedPath);
            }

            lookup[romImage.Name] = (resolvedPath, size);
        }

        return lookup;
    }

    private static void LoadSourceIntoPhysicalMemory(
        PhysicalMemory physical,
        PhysicalMemorySourceProfile source,
        Dictionary<string, (string ResolvedPath, uint Size)> romImages)
    {
        if (!string.Equals(source.Type, "rom-image", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Unsupported physical memory source type: '{source.Type}'. Only 'rom-image' is supported.");
        }

        if (string.IsNullOrEmpty(source.RomImage))
        {
            throw new InvalidOperationException(
                $"Physical memory source '{source.Name}' must specify a 'rom-image' reference.");
        }

        if (!romImages.TryGetValue(source.RomImage, out var romInfo))
        {
            throw new InvalidOperationException(
                $"ROM image '{source.RomImage}' referenced by source '{source.Name}' is not defined in rom-images.");
        }

        uint offset = HexParser.ParseUInt32(source.Offset);

        // Validate bounds
        if (offset + romInfo.Size > physical.Size)
        {
            throw new InvalidOperationException(
                $"ROM image '{source.RomImage}' (size 0x{romInfo.Size:X}) at offset 0x{offset:X} " +
                $"would exceed physical memory '{physical.Name}' (size 0x{physical.Size:X}).");
        }

        // Load the ROM data (file existence already validated in BuildRomImageLookup)
        byte[] romData = File.ReadAllBytes(romInfo.ResolvedPath);

        // Validate size
        if (romData.Length < romInfo.Size)
        {
            throw new InvalidOperationException(
                $"ROM file '{romInfo.ResolvedPath}' is too small ({romData.Length} bytes). " +
                $"Expected {romInfo.Size} bytes for ROM image '{source.RomImage}'.");
        }

        // Truncate if larger
        if (romData.Length > romInfo.Size)
        {
            romData = romData.Take((int)romInfo.Size).ToArray();
        }

        // Write ROM data into physical memory at the specified offset
        physical.WriteSpan(offset, romData);
    }

    private void CreatePhysicalMemory(
        PhysicalMemoryProfile physicalProfile,
        Dictionary<string, (string ResolvedPath, uint Size)> romImages,
        ProfilePathResolver pathResolver)
    {
        if (string.IsNullOrEmpty(physicalProfile.Name))
        {
            throw new InvalidOperationException("Physical memory must have a name.");
        }

        if (physicalMemoryBlocks.ContainsKey(physicalProfile.Name))
        {
            throw new InvalidOperationException($"Duplicate physical memory name: '{physicalProfile.Name}'.");
        }

        uint size = HexParser.ParseUInt32(physicalProfile.Size);

        // Create the physical memory instance
        var physical = new PhysicalMemory(size, physicalProfile.Name);

        // Apply fill pattern if specified
        if (!string.IsNullOrEmpty(physicalProfile.Fill))
        {
            byte fillValue = HexParser.ParseByte(physicalProfile.Fill);
            physical.Fill(fillValue);
        }

        // Load sources (ROM images) into the physical memory
        if (physicalProfile.Sources is not null)
        {
            foreach (var source in physicalProfile.Sources)
            {
                LoadSourceIntoPhysicalMemory(physical, source, romImages);
            }
        }

        physicalMemoryBlocks[physicalProfile.Name] = physical;
    }

    private void ConfigureRegion(MemoryRegionProfile region, ProfilePathResolver pathResolver)
    {
        uint start = HexParser.ParseUInt32(region.Start);
        uint size = HexParser.ParseUInt32(region.Size);
        var perms = PermissionParser.Parse(region.Permissions);

        // Validate page alignment (4KB = 0x1000)
        const uint pageSize = 0x1000;
        if ((start % pageSize) != 0)
        {
            throw new InvalidOperationException(
                $"Region '{region.Name}' start address 0x{start:X} is not page-aligned (must be multiple of 0x1000).");
        }

        if ((size % pageSize) != 0)
        {
            throw new InvalidOperationException(
                $"Region '{region.Name}' size 0x{size:X} is not page-aligned (must be multiple of 0x1000).");
        }

        string regionType = region.Type.ToLowerInvariant();

        switch (regionType)
        {
            case "ram":
                ConfigureRamRegion(region, start, size, perms);
                break;

            case "rom":
                ConfigureRomRegion(region, start, size, perms);
                break;

            case "composite":
                ConfigureCompositeRegion(region, start, size, perms);
                break;

            case "io":
                throw new NotSupportedException($"I/O region type is not yet supported for region '{region.Name}'.");

            default:
                throw new NotSupportedException($"Unknown memory region type '{region.Type}' for region '{region.Name}'.");
        }
    }

    private void ConfigureCompositeRegion(MemoryRegionProfile region, uint start, uint size, PagePerms perms)
    {
        // Determine the target factory based on the handler
        Func<MachineBuilder, IBusTarget>? factory = null;

        if (string.IsNullOrEmpty(region.Handler) ||
            string.Equals(region.Handler, "default", StringComparison.OrdinalIgnoreCase))
        {
            // Use a new DefaultCompositeTarget with the region name
            // (captures region.Name in the closure)
            var regionName = region.Name;
            factory = _ => new DefaultCompositeTarget(regionName);
        }
        else if (!compositeHandlerFactories.TryGetValue(region.Handler, out factory))
        {
            throw new InvalidOperationException(
                $"No handler registered for composite region '{region.Name}' with handler '{region.Handler}'. " +
                $"Use RegisterCompositeHandler to register the handler before calling FromProfile, " +
                $"or use 'default' (or omit the handler) for open-bus behavior.");
        }

        // Capture factory for the closure
        var targetFactory = factory;

        // Defer the handler creation to the memory configuration phase
        // so that required components are available
        memoryConfigurations.Add((bus, registry) =>
        {
            var target = targetFactory(this);

            int deviceId = registry.GenerateId();
            registry.Register(deviceId, "Composite", region.Name, $"Memory/{start:X4}");

            int pageCount = (int)(size / 0x1000);
            int startPage = (int)(start / 0x1000);

            bus.MapPageRange(
                startPage: startPage,
                pageCount: pageCount,
                deviceId: deviceId,
                regionTag: RegionTag.Composite,
                perms: perms,
                caps: target.Capabilities,
                target: target,
                physicalBase: 0);
        });
    }

    private void ConfigureRamRegion(MemoryRegionProfile region, uint start, uint size, PagePerms perms)
    {
        // Get physical memory backing
        var (physical, sourceOffset) = GetPhysicalMemoryForRegion(region, size);

        var target = new RamTarget(physical.Slice(sourceOffset, size), physical.Name);

        // Add memory configuration callback
        memoryConfigurations.Add((bus, registry) =>
        {
            int deviceId = registry.GenerateId();
            registry.Register(deviceId, "RAM", region.Name, $"Memory/{start:X4}");

            int pageCount = (int)(size / 0x1000);
            int startPage = (int)(start / 0x1000);

            bus.MapPageRange(
                startPage: startPage,
                pageCount: pageCount,
                deviceId: deviceId,
                regionTag: RegionTag.Ram,
                perms: perms,
                caps: target.Capabilities,
                target: target,
                physicalBase: 0);
        });
    }

    private void ConfigureRomRegion(MemoryRegionProfile region, uint start, uint size, PagePerms perms)
    {
        // Get physical memory backing
        var (physical, sourceOffset) = GetPhysicalMemoryForRegion(region, size);

        var target = new RomTarget(physical.ReadOnlySlice(sourceOffset, size), physical.Name);

        // Add memory configuration callback
        memoryConfigurations.Add((bus, registry) =>
        {
            int deviceId = registry.GenerateId();
            registry.Register(deviceId, "ROM", region.Name, $"Memory/{start:X4}");

            int pageCount = (int)(size / 0x1000);
            int startPage = (int)(start / 0x1000);

            bus.MapPageRange(
                startPage: startPage,
                pageCount: pageCount,
                deviceId: deviceId,
                regionTag: RegionTag.Rom,
                perms: perms,
                caps: target.Capabilities,
                target: target,
                physicalBase: 0);
        });
    }

    private (PhysicalMemory Physical, uint SourceOffset) GetPhysicalMemoryForRegion(
        MemoryRegionProfile region,
        uint regionSize)
    {
        if (string.IsNullOrEmpty(region.Source))
        {
            throw new InvalidOperationException(
                $"Region '{region.Name}' of type '{region.Type}' must specify a 'source' " +
                $"referencing a physical memory block.");
        }

        if (!physicalMemoryBlocks.TryGetValue(region.Source, out var physical))
        {
            throw new InvalidOperationException(
                $"Physical memory '{region.Source}' referenced by region '{region.Name}' " +
                $"is not defined in the physical memory array.");
        }

        uint sourceOffset = 0;
        if (!string.IsNullOrEmpty(region.SourceOffset))
        {
            sourceOffset = HexParser.ParseUInt32(region.SourceOffset);

            // Validate page alignment
            const uint pageSize = 0x1000;
            if ((sourceOffset % pageSize) != 0)
            {
                throw new InvalidOperationException(
                    $"Region '{region.Name}' source-offset 0x{sourceOffset:X} is not page-aligned " +
                    $"(must be multiple of 0x1000).");
            }
        }

        // Validate bounds
        if (sourceOffset + regionSize > physical.Size)
        {
            throw new InvalidOperationException(
                $"Region '{region.Name}' (size 0x{regionSize:X}) at source-offset 0x{sourceOffset:X} " +
                $"would exceed physical memory '{region.Source}' (size 0x{physical.Size:X}).");
        }

        return (physical, sourceOffset);
    }

    private void ConfigureMotherboardDevice(MotherboardDeviceEntry deviceEntry)
    {
        // Skip disabled devices
        if (!deviceEntry.Enabled)
        {
            return;
        }

        // Look up the device factory
        if (!motherboardDeviceFactories.TryGetValue(deviceEntry.Type, out var factory))
        {
            // No factory registered - silently skip
            // This allows profiles to declare devices that may not be supported by all hosts
            return;
        }

        // Create the device instance
        var device = factory(this);

        // Add the device as both a component and a scheduled device
        AddComponent(device);
        AddDevice(device);

        // Register the device in the device registry during build
        var capturedName = deviceEntry.Name ?? device.Name;
        var capturedType = device.DeviceType;
        memoryConfigurations.Add((bus, registry) =>
        {
            int deviceId = registry.GenerateId();
            registry.Register(deviceId, capturedType, capturedName, $"Motherboard/{capturedType}");
        });
    }

    private void ConfigureSlotCard(SlotCardProfile cardEntry)
    {
        // Validate slot number
        if (cardEntry.Slot < 1 || cardEntry.Slot > 7)
        {
            throw new InvalidOperationException(
                $"Invalid slot number {cardEntry.Slot} for card type '{cardEntry.Type}'. " +
                $"Slot must be between 1 and 7.");
        }

        // Look up the card factory
        if (!slotCardFactories.TryGetValue(cardEntry.Type, out var factory))
        {
            // No factory registered - silently skip
            // This allows profiles to declare cards that may not be supported by all hosts
            return;
        }

        // Create the card instance
        var card = factory(this);

        // Add as a pending slot card (will be installed during build when SlotManager is available)
        AddComponent(new PendingSlotCardFromProfile(cardEntry.Slot, card));

        // Also add the card as a device so it gets initialized
        AddDevice(card);

        // Register the card in the device registry during build
        var capturedSlot = cardEntry.Slot;
        var capturedName = card.Name;
        var capturedType = card.DeviceType;
        memoryConfigurations.Add((bus, registry) =>
        {
            int deviceId = registry.GenerateId();
            registry.Register(deviceId, capturedType, capturedName, $"Slot/{capturedSlot}/{capturedType}");
        });
    }

    /// <summary>
    /// Represents a slot card pending installation during profile-based machine build.
    /// </summary>
    /// <param name="Slot">The slot number (1-7).</param>
    /// <param name="Card">The slot card to install.</param>
    private sealed record PendingSlotCardFromProfile(int Slot, ISlotCard Card) : IPendingSlotCard;
}