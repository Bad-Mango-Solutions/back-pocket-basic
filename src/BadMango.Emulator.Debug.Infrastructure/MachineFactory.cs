// <copyright file="MachineFactory.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure;

using BadMango.Emulator.Bus;
using BadMango.Emulator.Bus.Interfaces;
using BadMango.Emulator.Core.Configuration;
using BadMango.Emulator.Core.Cpu;
using BadMango.Emulator.Core.Interfaces;
using BadMango.Emulator.Core.Interfaces.Cpu;
using BadMango.Emulator.Emulation.Cpu;
using BadMango.Emulator.Emulation.Debugging;
using BadMango.Emulator.Systems;

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
        var (machine, disassembler, info) = CreateDebugSystem(profile, pathResolver);
        return (machine.Cpu, machine.Bus, disassembler, info);
    }

    /// <summary>
    /// Creates a complete debug system from a machine profile, including the full machine instance.
    /// </summary>
    /// <param name="profile">The machine profile to instantiate.</param>
    /// <param name="pathResolver">Optional path resolver for loading ROM files. If null, a default resolver is used.</param>
    /// <returns>A tuple containing the machine, disassembler, and machine info.</returns>
    /// <exception cref="NotSupportedException">Thrown when the CPU type is not supported.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the memory configuration is invalid.</exception>
    public static (IMachine Machine, IDisassembler Disassembler, MachineInfo Info) CreateDebugSystem(
        MachineProfile profile,
        ProfilePathResolver? pathResolver = null)
    {
        ArgumentNullException.ThrowIfNull(profile);

        pathResolver ??= new ProfilePathResolver(null);

        // Build the machine using FromProfile as the primary configuration source
        var builder = new MachineBuilder();

        // Register composite handler factories for handlers used by the profile.
        // These only register factories - they don't create components or memory layouts.
        // The actual components are created on-demand when FromProfile processes the regions.
        if (RequiresCompositeIOHandler(profile))
        {
            builder.RegisterCompositeIOHandler();
        }

        var machine = builder
            .FromProfile(profile, pathResolver)
            .WithCpuFactory(CreateCpuFactory(profile.Cpu))
            .Build();

        // Create opcode table and disassembler
        var opcodeTable = GetOpcodeTable(profile.Cpu);
        var disassembler = new Disassembler(opcodeTable, machine.Bus);
        var info = MachineInfo.FromProfile(profile);

        // Reset the CPU to initialize it
        machine.Reset();

        return (machine, disassembler, info);
    }

    /// <summary>
    /// Creates a complete machine from a profile using MachineBuilder.
    /// </summary>
    /// <param name="profile">The machine profile to instantiate.</param>
    /// <param name="pathResolver">Optional path resolver for loading ROM files. If null, a default resolver is used.</param>
    /// <returns>A fully assembled and initialized machine.</returns>
    /// <exception cref="NotSupportedException">Thrown when the CPU type is not supported.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the memory configuration is invalid.</exception>
    public static IMachine CreateMachine(
        MachineProfile profile,
        ProfilePathResolver? pathResolver = null)
    {
        ArgumentNullException.ThrowIfNull(profile);

        pathResolver ??= new ProfilePathResolver(null);

        // Build the machine using FromProfile as the primary configuration source
        var builder = new MachineBuilder();

        // Register composite handler factories for handlers used by the profile.
        // These only register factories - they don't create components or memory layouts.
        // The actual components are created on-demand when FromProfile processes the regions.
        if (RequiresCompositeIOHandler(profile))
        {
            builder.RegisterCompositeIOHandler();
        }

        return builder
            .FromProfile(profile, pathResolver)
            .WithCpuFactory(CreateCpuFactory(profile.Cpu))
            .Build();
    }

    /// <summary>
    /// Determines if a profile uses the composite-io handler.
    /// </summary>
    /// <param name="profile">The profile to check.</param>
    /// <returns>True if the profile uses composite-io handler; otherwise, false.</returns>
    private static bool RequiresCompositeIOHandler(MachineProfile profile)
    {
        if (profile.Memory?.Regions is null)
        {
            return false;
        }

        return profile.Memory.Regions.Any(region =>
            string.Equals(region.Type, "composite", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(region.Handler, "composite-io", StringComparison.OrdinalIgnoreCase));
    }

    private static Func<IEventContext, ICpu> CreateCpuFactory(CpuProfileSection cpuConfig)
    {
        return cpuConfig.Type.ToUpperInvariant() switch
        {
            "65C02" => context => new Cpu65C02(context),
            "6502" => throw new NotSupportedException("6502 CPU is not yet implemented in the emulator core."),
            "65816" => throw new NotSupportedException("65816 CPU is not yet implemented."),
            "65832" => throw new NotSupportedException("65832 CPU is not yet implemented."),
            _ => throw new NotSupportedException($"CPU type '{cpuConfig.Type}' is not supported."),
        };
    }

    private static OpcodeTable GetOpcodeTable(CpuProfileSection cpuConfig)
    {
        return cpuConfig.Type.ToUpperInvariant() switch
        {
            "65C02" => Cpu65C02OpcodeTableBuilder.Build(),
            "6502" => throw new NotSupportedException("6502 CPU is not yet implemented in the emulator core."),
            "65816" => throw new NotSupportedException("65816 CPU is not yet implemented."),
            "65832" => throw new NotSupportedException("65832 CPU is not yet implemented."),
            _ => throw new NotSupportedException($"CPU type '{cpuConfig.Type}' is not supported."),
        };
    }
}