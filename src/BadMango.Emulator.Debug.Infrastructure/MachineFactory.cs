// <copyright file="MachineFactory.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure;

using BadMango.Emulator.Core;
using BadMango.Emulator.Core.Configuration;
using BadMango.Emulator.Emulation.Cpu;
using BadMango.Emulator.Emulation.Debugging;
using BadMango.Emulator.Emulation.Memory;

using Core.Cpu;
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
    /// <returns>A tuple containing the CPU, memory, disassembler, and machine info.</returns>
    /// <exception cref="NotSupportedException">Thrown when the CPU type is not supported.</exception>
    public static (ICpu Cpu, IMemory Memory, IDisassembler Disassembler, MachineInfo Info) CreateSystem(MachineProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        var memory = CreateMemory(profile.Memory);
        (ICpu cpu, OpcodeTable opcodeTable) = CreateCpu(profile.Cpu, memory);
        cpu.Reset();
        var disassembler = new Disassembler(opcodeTable, memory);
        var info = MachineInfo.FromProfile(profile);

        return (cpu, memory, disassembler, info);
    }

    private static IMemory CreateMemory(MemoryProfileSection memoryConfig)
    {
        return memoryConfig.Type.ToLowerInvariant() switch
        {
            "basic" => new BasicMemory(memoryConfig.Size),
            _ => throw new NotSupportedException($"Memory type '{memoryConfig.Type}' is not supported."),
        };
    }

    private static (ICpu Cpu, OpcodeTable OpcodeTable) CreateCpu(CpuProfileSection cpuConfig, IMemory memory)
    {
        return cpuConfig.Type.ToUpperInvariant() switch
        {
            "65C02" => CreateCpu65C02(memory),
            "6502" => throw new NotSupportedException("6502 CPU is not yet implemented in the emulator core."),
            "65816" => throw new NotSupportedException("65816 CPU is not yet implemented."),
            "65832" => throw new NotSupportedException("65832 CPU is not yet implemented."),
            _ => throw new NotSupportedException($"CPU type '{cpuConfig.Type}' is not supported."),
        };
    }

    private static (Cpu65C02 Cpu, OpcodeTable Opcodes) CreateCpu65C02(IMemory memory)
    {
        var opcodeTable = Cpu65C02OpcodeTableBuilder.Build();
        var cpu = new Cpu65C02(memory);
        return (cpu, opcodeTable);
    }
}