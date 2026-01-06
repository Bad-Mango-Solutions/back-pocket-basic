// <copyright file="MachineFactory.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure;

using BadMango.Emulator.Core.Configuration;
using BadMango.Emulator.Emulation.Cpu;
using BadMango.Emulator.Emulation.Debugging;

using Bus;
using Bus.Interfaces;

using Core.Cpu;
using Core.Interfaces;
using Core.Interfaces.Cpu;

/// <summary>
/// Factory for creating emulator components from machine profiles.
/// </summary>
public static class MachineFactory
{
    /// <summary>
    /// Creates a complete debug system from a machine profile.
    /// </summary>
    /// <param name="profile">The machine profile to instantiate.</param>
    /// <param name="pathResolver">Optional path resolver for loading ROM files. If null, a default resolver is used.</param>
    /// <returns>A tuple containing the CPU, memory bus, disassembler, and machine info.</returns>
    /// <exception cref="NotSupportedException">Thrown when the CPU type is not supported.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the memory configuration is invalid.</exception>
    public static (ICpu Cpu, IMemoryBus Bus, IDisassembler Disassembler, MachineInfo Info) CreateSystem(
        MachineProfile profile,
        ProfilePathResolver? pathResolver = null)
    {
        ArgumentNullException.ThrowIfNull(profile);

        pathResolver ??= new ProfilePathResolver(null);

        var bus = CreateMemoryBus(profile, pathResolver);
        (ICpu cpu, OpcodeTable opcodeTable) = CreateCpu(profile.Cpu, bus);
        cpu.Reset();
        var disassembler = new Disassembler(opcodeTable, bus);
        var info = MachineInfo.FromProfile(profile);

        return (cpu, bus, disassembler, info);
    }

    private static IMemoryBus CreateMemoryBus(MachineProfile profile, ProfilePathResolver pathResolver)
    {
        var memoryConfig = profile.Memory;

        if (memoryConfig.UsesRegions)
        {
            return CreateBusFromRegions(profile.AddressSpace, memoryConfig.Regions!, pathResolver);
        }

        // No regions defined - throw an error since the old format is no longer supported
        throw new InvalidOperationException(
            "Memory configuration must use the 'regions' array format. " +
            "The legacy 'size' and 'type' properties are no longer supported.");
    }

    private static IMemoryBus CreateBusFromRegions(
        int addressSpaceBits,
        List<MemoryRegionProfile> regions,
        ProfilePathResolver pathResolver)
    {
        var bus = new MainBus(addressSpaceBits);
        var registry = new DeviceRegistry();

        foreach (var region in regions)
        {
            MapRegion(bus, registry, region, pathResolver);
        }

        return bus;
    }

    private static void MapRegion(
        MainBus bus,
        DeviceRegistry registry,
        MemoryRegionProfile region,
        ProfilePathResolver pathResolver)
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

        int deviceId = registry.GenerateId();
        string regionType = region.Type.ToLowerInvariant();

        IBusTarget target;
        RegionTag tag;

        switch (regionType)
        {
            case "ram":
                (target, tag) = CreateRamTarget(region, size);
                break;

            case "rom":
                (target, tag) = CreateRomTarget(region, size, pathResolver);
                break;

            case "io":
                // I/O regions are not yet implemented - throw for now
                throw new NotSupportedException($"I/O region type is not yet supported for region '{region.Name}'.");

            default:
                throw new NotSupportedException($"Unknown memory region type '{region.Type}' for region '{region.Name}'.");
        }

        registry.Register(deviceId, regionType.ToUpperInvariant(), region.Name, $"Memory/{start:X4}");

        int pageCount = (int)(size / pageSize);
        int startPage = (int)(start / pageSize);

        bus.MapPageRange(
            startPage: startPage,
            pageCount: pageCount,
            deviceId: deviceId,
            regionTag: tag,
            perms: perms,
            caps: target.Capabilities,
            target: target,
            physicalBase: 0);
    }

    private static (IBusTarget Target, RegionTag Tag) CreateRamTarget(MemoryRegionProfile region, uint size)
    {
        var physical = new PhysicalMemory(size, region.Name);

        // Handle fill pattern if specified
        if (!string.IsNullOrEmpty(region.Fill))
        {
            byte fillValue = HexParser.ParseByte(region.Fill);
            physical.Fill(fillValue);
        }

        // TODO: Handle source loading for RAM regions (pre-initialized RAM)

        var target = new RamTarget(physical.Slice(0, size));
        return (target, RegionTag.Ram);
    }

    private static (IBusTarget Target, RegionTag Tag) CreateRomTarget(
        MemoryRegionProfile region,
        uint size,
        ProfilePathResolver pathResolver)
    {
        byte[] romData;

        if (!string.IsNullOrEmpty(region.Source))
        {
            // Load ROM from file
            string resolvedPath = pathResolver.Resolve(region.Source);

            if (!File.Exists(resolvedPath))
            {
                throw new FileNotFoundException(
                    $"ROM file not found for region '{region.Name}': {resolvedPath}",
                    resolvedPath);
            }

            romData = File.ReadAllBytes(resolvedPath);

            // Handle source offset if specified
            if (!string.IsNullOrEmpty(region.SourceOffset))
            {
                uint offset = HexParser.ParseUInt32(region.SourceOffset);
                if (offset >= romData.Length)
                {
                    throw new InvalidOperationException(
                        $"Source offset 0x{offset:X} exceeds ROM file size ({romData.Length} bytes) for region '{region.Name}'.");
                }

                romData = romData.Skip((int)offset).ToArray();
            }

            // Validate size
            if (romData.Length < size)
            {
                throw new InvalidOperationException(
                    $"ROM file is too small ({romData.Length} bytes) for region '{region.Name}' which requires {size} bytes.");
            }

            // Truncate if needed
            if (romData.Length > size)
            {
                romData = romData.Take((int)size).ToArray();
            }
        }
        else
        {
            // Create empty ROM (all zeros or fill pattern)
            romData = new byte[size];

            if (!string.IsNullOrEmpty(region.Fill))
            {
                byte fillValue = HexParser.ParseByte(region.Fill);
                Array.Fill(romData, fillValue);
            }
        }

        var physical = new PhysicalMemory(romData, region.Name);
        var target = new RomTarget(physical.Slice(0, size));
        return (target, RegionTag.Rom);
    }

    private static (ICpu Cpu, OpcodeTable OpcodeTable) CreateCpu(CpuProfileSection cpuConfig, IMemoryBus bus)
    {
        return cpuConfig.Type.ToUpperInvariant() switch
        {
            "65C02" => CreateCpu65C02(bus),
            "6502" => throw new NotSupportedException("6502 CPU is not yet implemented in the emulator core."),
            "65816" => throw new NotSupportedException("65816 CPU is not yet implemented."),
            "65832" => throw new NotSupportedException("65832 CPU is not yet implemented."),
            _ => throw new NotSupportedException($"CPU type '{cpuConfig.Type}' is not supported."),
        };
    }

    private static (Cpu65C02 Cpu, OpcodeTable Opcodes) CreateCpu65C02(IMemoryBus bus)
    {
        var opcodeTable = Cpu65C02OpcodeTableBuilder.Build();

        // Create a MemoryBusAdapter for the CPU (it still needs IMemory internally)
        // This is temporary until the CPU is fully migrated to use IEventContext
#pragma warning disable CS0618 // Type or member is obsolete
        var memoryAdapter = new MemoryBusAdapter(bus);
        var cpu = new Cpu65C02(memoryAdapter);
#pragma warning restore CS0618 // Type or member is obsolete

        return (cpu, opcodeTable);
    }
}