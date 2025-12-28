// <copyright file="SystemRegisters.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Core.Cpu;

using System.Runtime.InteropServices;

/// <summary>
/// Represents privileged system registers for the 65832 processor.
/// </summary>
/// <remarks>
/// These registers are only accessible in Kernel or Hypervisor modes.
/// Attempting to access them from User mode will generate a privilege violation exception.
/// </remarks>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct SystemRegisters
{
    /// <summary>Control Register 0 - global control bits and privilege level.</summary>
    /// <remarks>
    /// <para>Bit layout:</para>
    /// <list type="bullet">
    /// <item><description>Bits 0-1: Privilege Level (PrivilegeLevel enum: User=0, Kernel=1, Hypervisor=2)</description></item>
    /// <item><description>Bit 2: PG (Paging Enable)</description></item>
    /// <item><description>Bit 3: Reserved</description></item>
    /// <item><description>Bit 4: NXE (No-Execute Enforcement Enable)</description></item>
    /// <item><description>Bits 5-7: Interrupt Priority Level (0-7)</description></item>
    /// <item><description>Bits 8-15: ASID (Address Space ID)</description></item>
    /// <item><description>Bits 16-31: Reserved</description></item>
    /// </list>
    /// </remarks>
    public DWord CR0;

    /// <summary>Page Table Base Register - physical address of L1 page table.</summary>
    public DWord PTBR;

    /// <summary>Vector Base Address Register - base address of exception vector table.</summary>
    /// <remarks>Must be 4KB aligned.</remarks>
    public DWord VBAR;

    /// <summary>Fault Address Register - virtual address that caused the last page fault.</summary>
    public DWord FAR;

    /// <summary>Fault Status Code - reason for the last page fault.</summary>
    public DWord FSC;

    /// <summary>Thread-Local Storage pointer.</summary>
    public DWord TLS;

    /// <summary>
    /// Initializes a new instance of the <see cref="SystemRegisters"/> struct with default values.
    /// </summary>
    public SystemRegisters()
    {
        CR0 = 0;  // Default to User mode, all features disabled
        PTBR = 0;
        VBAR = 0;
        FAR = 0;
        FSC = 0;
        TLS = 0;
    }
}