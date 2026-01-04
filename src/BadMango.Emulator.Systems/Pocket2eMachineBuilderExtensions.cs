// <copyright file="Pocket2eMachineBuilderExtensions.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Systems;

using BadMango.Emulator.Bus;
using BadMango.Emulator.Bus.Interfaces;
using BadMango.Emulator.Core.Cpu;
using BadMango.Emulator.Core.Interfaces.Cpu;
using BadMango.Emulator.Emulation.Cpu;

/// <summary>
/// Extension methods for configuring <see cref="MachineBuilder"/> as a Pocket2e system.
/// </summary>
/// <remarks>
/// <para>
/// Pocket2e is an Apple IIe-compatible configuration using a 65C02 CPU with
/// a 64KB address space and standard IIe memory layout.
/// </para>
/// <para>
/// The standard configuration includes:
/// <list type="bullet">
/// <item><description>65C02 CPU at 1.023 MHz (emulated)</description></item>
/// <item><description>64KB RAM (48KB base + 16KB auxiliary)</description></item>
/// <item><description>Standard IIe ROM vectors at $FFFA-$FFFF</description></item>
/// <item><description>High-priority vector table layer for NMI, RESET, and IRQ/BRK</description></item>
/// </list>
/// </para>
/// </remarks>
public static class Pocket2eMachineBuilderExtensions
{
    /// <summary>
    /// The standard IIe base RAM size (48KB).
    /// </summary>
    public const uint BaseRamSize = 48 * 1024;

    /// <summary>
    /// The standard IIe full RAM size including auxiliary memory (64KB).
    /// </summary>
    public const uint FullRamSize = 64 * 1024;

    /// <summary>
    /// The vector table layer name.
    /// </summary>
    public const string VectorTableLayerName = "VectorTable";

    /// <summary>
    /// The vector table layer priority (high to override ROM).
    /// </summary>
    public const int VectorTableLayerPriority = 1000;

    /// <summary>
    /// Address of the NMI vector ($FFFA).
    /// </summary>
    public const Addr NmiVectorAddress = 0xFFFA;

    /// <summary>
    /// Address of the RESET vector ($FFFC).
    /// </summary>
    public const Addr ResetVectorAddress = 0xFFFC;

    /// <summary>
    /// Address of the IRQ/BRK vector ($FFFE).
    /// </summary>
    public const Addr IrqVectorAddress = 0xFFFE;

    /// <summary>
    /// Sets up a known good basic Pocket2e (Apple IIe clone) configuration.
    /// </summary>
    /// <param name="builder">The machine builder to configure.</param>
    /// <returns>The configured builder for method chaining.</returns>
    /// <remarks>
    /// <para>
    /// This configures:
    /// <list type="bullet">
    /// <item><description>16-bit address space (64KB)</description></item>
    /// <item><description>65C02 CPU</description></item>
    /// <item><description>64KB RAM mapped from $0000-$FFFF</description></item>
    /// <item><description>High-priority vector table layer for the 6502 vectors</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// After calling this method, you should add ROM images using
    /// <see cref="MachineBuilder.WithRom(RomDescriptor)"/> or
    /// <see cref="MachineBuilder.WithRom(byte[], uint, string?)"/>.
    /// </para>
    /// </remarks>
    public static MachineBuilder AsPocket2e(this MachineBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder, nameof(builder));

        return builder
            .WithAddressSpace(16)
            .WithCpu(CpuFamily.Cpu65C02)
            .WithCpuFactory(CreatePocket2eCpu)
            .ConfigureMemory(ConfigurePocket2eMemory)
            .CreateLayer(VectorTableLayerName, VectorTableLayerPriority);
    }

    /// <summary>
    /// Adds standard IIe vector table ROM with specified vectors.
    /// </summary>
    /// <param name="builder">The machine builder to configure.</param>
    /// <param name="nmiHandler">The address of the NMI handler routine.</param>
    /// <param name="resetHandler">The address of the RESET handler routine.</param>
    /// <param name="irqHandler">The address of the IRQ/BRK handler routine.</param>
    /// <returns>The configured builder for method chaining.</returns>
    /// <remarks>
    /// <para>
    /// This creates a small 6-byte ROM containing the vector table and maps it
    /// as a high-priority layer at $FFFA-$FFFF. The vectors are stored in
    /// little-endian format.
    /// </para>
    /// <para>
    /// The vector table layer has higher priority than the base ROM, ensuring
    /// these vectors are always used regardless of other ROM mappings.
    /// </para>
    /// </remarks>
    public static MachineBuilder WithVectorTable(
        this MachineBuilder builder,
        Addr nmiHandler,
        Addr resetHandler,
        Addr irqHandler)
    {
        ArgumentNullException.ThrowIfNull(builder, nameof(builder));

        // Create vector table data (6 bytes: NMI, RESET, IRQ)
        byte[] vectorData =
        [
            (byte)(nmiHandler & 0xFF), (byte)((nmiHandler >> 8) & 0xFF),       // NMI vector at $FFFA
            (byte)(resetHandler & 0xFF), (byte)((resetHandler >> 8) & 0xFF),   // RESET vector at $FFFC
            (byte)(irqHandler & 0xFF), (byte)((irqHandler >> 8) & 0xFF),       // IRQ/BRK vector at $FFFE
        ];

        var physical = new PhysicalMemory(vectorData, "VectorTable");
        var target = new RomTarget(physical.Slice(0, 6));

        return builder.AddLayeredMapping(
            VectorTableLayerName,
            NmiVectorAddress,
            0x1000, // Page-aligned size (vectors are at end of page)
            target,
            RegionTag.Rom,
            PagePerms.ReadExecute);
    }

    /// <summary>
    /// Adds standard IIe vector table with default monitor entry points.
    /// </summary>
    /// <param name="builder">The machine builder to configure.</param>
    /// <returns>The configured builder for method chaining.</returns>
    /// <remarks>
    /// <para>
    /// Uses common IIe-compatible vector addresses:
    /// <list type="bullet">
    /// <item><description>NMI: $FA40 (standard NMI handler)</description></item>
    /// <item><description>RESET: $FA62 (standard RESET handler)</description></item>
    /// <item><description>IRQ/BRK: $FA40 (standard IRQ/BRK handler)</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// These are placeholder values; actual values depend on the ROM image being used.
    /// For accurate emulation, use <see cref="WithVectorTable(MachineBuilder, uint, uint, uint)"/>
    /// with vectors from your specific ROM.
    /// </para>
    /// </remarks>
    public static MachineBuilder WithStandardVectorTable(this MachineBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder, nameof(builder));

        // Standard placeholder vectors pointing to common entry points
        // These are typical IIe-compatible addresses
        return builder.WithVectorTable(
            nmiHandler: 0xFA40,   // Typical NMI handler
            resetHandler: 0xFA62, // Typical RESET handler
            irqHandler: 0xFA40);  // Typical IRQ/BRK handler
    }

    private static ICpu CreatePocket2eCpu(IEventContext context)
    {
        return new Cpu65C02(context);
    }

    private static void ConfigurePocket2eMemory(IMemoryBus bus, IDeviceRegistry devices)
    {
        // Create 64KB RAM
        var ram = new PhysicalMemory(FullRamSize, "MainRAM");
        var ramTarget = new RamTarget(ram.Slice(0, FullRamSize));

        // Register the RAM device
        int ramDeviceId = devices.GenerateId();
        devices.Register(ramDeviceId, "RAM", "Main RAM", "Memory/MainRAM");

        // Map full 64KB as RAM (will be overlaid by ROM later)
        bus.MapRegion(
            0x0000,
            FullRamSize,
            ramDeviceId,
            RegionTag.Ram,
            PagePerms.All,
            ramTarget.Capabilities,
            ramTarget,
            0);
    }
}