// <copyright file="CpuCapabilities.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Core.Cpu;

/// <summary>Represents the capabilities of a CPU in the Back Pocket emulator.</summary>
[Flags]
public enum CpuCapabilities : uint
{
    /// <summary>Represents the absence of any CPU capabilities.</summary>
    /// <remarks>
    /// This value indicates that no specific features or instruction sets are supported.
    /// It can be used as a default or placeholder value when no capabilities are applicable.
    /// </remarks>
    None = 0,

    /// <summary>Represents the base capability of the 6502 CPU architecture.</summary>
    /// <remarks>
    /// The <see cref="CpuCapabilities.Base6502"/> flag indicates support for the original 6502 instruction set
    /// and architecture. This serves as the foundational capability for all CPUs in the 6502 family,
    /// including extended variants such as 65C02, 65816, and 65832.
    /// Decomposed bus access, 16-bit addressing, 8-bit registers.
    /// </remarks>
    Base6502 = 1 << 0,

    /// <summary>Indicates support for the emulation flag capability in the CPU.</summary>
    /// <remarks>
    /// When clear, writes to the E flag are ignored (65C02 behavior).
    /// The 65C816 and 65832 set this flag.
    /// </remarks>
    SupportsEmulationFlag = 1 << 1,

    /// <summary>Supports the CP (Compatibility) flag for mode switching.</summary>
    /// <remarks>
    /// When clear, writes to the CP flag are ignored (65C02 and 65C816 behavior).
    /// Only the 65832 sets this flag.
    /// </remarks>
    SupportsCompatibilityFlag = 1 << 2,         // Reserved for 65832

    /// <summary>Supports 16-bit accumulator and index registers (M/X flags meaningful).</summary>
    Supports16BitRegisters = 1 << 3,

    /// <summary>Supports 32-bit accumulator, index registers, and addressing.</summary>
    Supports32BitRegisters = 1 << 4,            // Reserved for 65832

    /// <summary>Supports 64-bit accumulator, index registers, and addressing.</summary>
    Supports64BitRegisters = 1 << 5,            // Reserved for hypothetical future CPUs

    /// <summary>Indicates support for the 65C02 instruction set extensions.</summary>
    /// <remarks>
    /// The <see cref="CpuCapabilities.Supports65C02Instructions"/> flag signifies that the CPU supports
    /// the enhanced instruction set introduced with the 65C02 processor. These enhancements include
    /// additional addressing modes, new instructions, and improvements over the original 6502 architecture.
    /// This capability is essential for emulating or implementing systems based on the 65C02 CPU.
    /// </remarks>
    Supports65C02Instructions = 1 << 6,

    /// <summary>
    /// Represents the capability to support the 65816 instruction set.
    /// </summary>
    /// <remarks>
    /// The <see cref="CpuCapabilities.Supports65816Instructions"/> flag indicates that the CPU
    /// supports the extended 65816 instruction set, which builds upon the 6502 architecture.
    /// This includes features such as 16-bit registers, enhanced addressing modes, and additional
    /// instructions for improved performance and functionality.
    /// </remarks>
    Supports65816Instructions = 1 << 7,

    /// <summary>
    /// Indicates support for the 65832 instruction set.
    /// </summary>
    /// <remarks>
    /// The 65832 instruction set extends the capabilities of the 65816 instruction set,
    /// introducing additional features and enhancements. This flag can be used to determine
    /// whether a CPU implementation supports these advanced instructions.
    /// </remarks>
    Supports65832Instructions = 1 << 8,         // Reserved for 65832

    /// <summary>
    /// Indicates whether the CPU supports atomic bus operations.
    /// </summary>
    /// <remarks>
    /// Atomic bus operations ensure that specific read-modify-write sequences are
    /// performed without interruption, which is critical for maintaining data
    /// integrity in multithreaded or multiprocessor environments.
    /// </remarks>
    SupportsAtomicBusOperations = 1 << 9,       // Reserved for 65832

    /// <summary>
    /// Indicates that the CPU supports general-purpose registers.
    /// </summary>
    /// <remarks>
    /// General-purpose registers are versatile registers that can be used for various purposes,
    /// such as arithmetic operations, data storage, and addressing. This capability is essential
    /// for CPUs that implement modern instruction sets requiring flexible register usage.
    /// </remarks>
    SupportsGeneralPurposeRegisters = 1 << 10,  // Reserved for 65832

    /// <summary>
    /// Indicates support for system registers in the CPU architecture.
    /// </summary>
    /// <remarks>
    /// The <see cref="CpuCapabilities.SupportsSystemRegisters"/> flag signifies that the CPU includes
    /// specialized system registers, which may be used for managing control, status, or other
    /// system-level operations. This capability is typically found in advanced CPU architectures
    /// and enables enhanced control over low-level system behavior.
    /// </remarks>
    SupportsSystemRegisters = 1 << 11,          // Reserved for 65832
}