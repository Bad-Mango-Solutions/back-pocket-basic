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
using BadMango.Emulator.Devices;
using BadMango.Emulator.Emulation.Cpu;
using BadMango.Emulator.Emulation.Debugging;

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

        pathResolver ??= new(null);

        // Build the machine using FromProfile as the primary configuration source
        var builder = new MachineBuilder();

        // Register composite handler factories for handlers used by the profile.
        // These only register factories - they don't create components or memory layouts.
        // The actual components are created on-demand when FromProfile processes the regions.
        if (RequiresCompositeIOHandler(profile))
        {
            builder.RegisterCompositeIOHandler();
        }

        // Register standard motherboard device factories for profile-based device loading.
        // This enables profiles to declare devices like speaker, keyboard, video, etc.
        builder.RegisterStandardDeviceFactories();

        var machine = builder
            .FromProfile(profile, pathResolver)
            .WithCpuFactory(CreateCpuFactory(profile.Cpu))
            .Build();

        // Wire the trap registry to the CPU so that traps fire during execution.
        // The MachineBuilder creates and registers the TrapRegistry as a component;
        // we assign it to the CPU here since the TrapRegistry property is on the
        // concrete Cpu65C02 type, not the ICpu interface.
        WireTrapRegistry(machine);

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

        pathResolver ??= new(null);

        // Build the machine using FromProfile as the primary configuration source
        var builder = new MachineBuilder();

        // Register composite handler factories for handlers used by the profile.
        // These only register factories - they don't create components or memory layouts.
        // The actual components are created on-demand when FromProfile processes the regions.
        if (RequiresCompositeIOHandler(profile))
        {
            builder.RegisterCompositeIOHandler();
        }

        // Register standard motherboard device factories for profile-based device loading.
        // This enables profiles to declare devices like speaker, keyboard, video, etc.
        builder.RegisterStandardDeviceFactories();

        var machine = builder
            .FromProfile(profile, pathResolver)
            .WithCpuFactory(CreateCpuFactory(profile.Cpu))
            .Build();

        // Wire the trap registry to the CPU so that traps fire during execution.
        WireTrapRegistry(machine);

        return machine;
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

    /// <summary>
    /// Wires the trap registry from the machine's component bag to the CPU.
    /// </summary>
    /// <param name="machine">The machine whose CPU should receive the trap registry.</param>
    /// <remarks>
    /// <para>
    /// The <see cref="MachineBuilder"/> creates and registers a <see cref="TrapRegistry"/>
    /// as a machine component during <see cref="MachineBuilder.Build"/>. This method
    /// retrieves that registry and assigns it to the CPU's <see cref="Cpu65C02.TrapRegistry"/>
    /// property so that traps fire during instruction execution.
    /// </para>
    /// <para>
    /// The assignment is done here rather than in the builder because the
    /// <c>TrapRegistry</c> property is on the concrete <see cref="Cpu65C02"/> type,
    /// which is not accessible from the Bus layer where <see cref="MachineBuilder"/> lives.
    /// </para>
    /// </remarks>
    private static void WireTrapRegistry(IMachine machine)
    {
        var trapRegistry = machine.GetComponent<ITrapRegistry>();
        if (trapRegistry is not null && machine.Cpu is Cpu65C02 cpu)
        {
            cpu.TrapRegistry = trapRegistry;
        }
    }
}