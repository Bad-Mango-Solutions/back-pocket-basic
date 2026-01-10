// <copyright file="BootRom.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Devices;

/// <summary>
/// Minimal 2KB boot ROM for the emulator providing proper CPU vectors and an idle loop.
/// </summary>
/// <remarks>
/// <para>
/// This boot ROM provides the essential CPU vectors required for proper 65C02 operation:
/// </para>
/// <list type="bullet">
/// <item><description><b>NMI vector</b> at $FFFA-$FFFB - Points to RTI handler</description></item>
/// <item><description><b>RESET vector</b> at $FFFC-$FFFD - Points to initialization code</description></item>
/// <item><description><b>IRQ/BRK vector</b> at $FFFE-$FFFF - Points to RTI handler</description></item>
/// </list>
/// <para>
/// The RESET handler initializes CPU state and enters a WAI (Wait for Interrupt) idle loop.
/// This allows the scheduler to continue dispatching events while waiting for interrupts.
/// </para>
/// <para>
/// Memory layout ($F800-$FFFF, 2KB):
/// </para>
/// <list type="bullet">
/// <item><description>$F800-$FFF9 - NOP fill (available for future expansion)</description></item>
/// <item><description>$FFFA-$FFFF - CPU vectors</description></item>
/// </list>
/// </remarks>
public static class BootRom
{
    /// <summary>
    /// The start address where the boot ROM is loaded ($F800).
    /// </summary>
    public const ushort LoadAddress = 0xF800;

    /// <summary>
    /// The size of the boot ROM in bytes (2KB = 2048 bytes).
    /// </summary>
    public const int Size = 0x0800;

    /// <summary>
    /// Offset of the NMI vector within the ROM (at $FFFA - $F800 = $07FA).
    /// </summary>
    private const int NmiVectorOffset = 0x07FA;

    /// <summary>
    /// Offset of the RESET vector within the ROM (at $FFFC - $F800 = $07FC).
    /// </summary>
    private const int ResetVectorOffset = 0x07FC;

    /// <summary>
    /// Offset of the IRQ vector within the ROM (at $FFFE - $F800 = $07FE).
    /// </summary>
    private const int IrqVectorOffset = 0x07FE;

    /// <summary>
    /// Address of the RESET handler code.
    /// </summary>
    private const ushort ResetHandlerAddress = 0xF800;

    /// <summary>
    /// Address of the interrupt handler (RTI).
    /// </summary>
    private const ushort InterruptHandlerAddress = 0xF810;

    // CPU Instruction opcodes
    private const byte CldInstruction = 0xD8; // CLD - Clear Decimal mode
    private const byte SeiInstruction = 0x78; // SEI - Set Interrupt disable
    private const byte CliInstruction = 0x58; // CLI - Clear Interrupt disable
    private const byte LdxImmInstruction = 0xA2; // LDX #imm
    private const byte TxsInstruction = 0x9A; // TXS - Transfer X to Stack pointer
    private const byte WaiInstruction = 0xCB; // WAI - Wait for Interrupt
    private const byte JmpAbsInstruction = 0x4C; // JMP abs
    private const byte RtiInstruction = 0x40; // RTI - Return from Interrupt
    private const byte NopInstruction = 0xEA; // NOP - No Operation

    private static byte[]? cachedRom;

    /// <summary>
    /// Gets the boot ROM data as a byte array.
    /// </summary>
    /// <returns>A 2KB byte array containing the boot ROM image.</returns>
    /// <remarks>
    /// <para>
    /// The RESET handler performs the following initialization:
    /// </para>
    /// <list type="number">
    /// <item><description>CLD - Clear decimal mode</description></item>
    /// <item><description>SEI - Disable interrupts during init</description></item>
    /// <item><description>LDX #$FF - Initialize stack pointer</description></item>
    /// <item><description>TXS - Set stack pointer to $01FF</description></item>
    /// <item><description>CLI - Enable interrupts</description></item>
    /// <item><description>WAI - Wait for interrupt</description></item>
    /// <item><description>JMP to WAI loop (infinite loop)</description></item>
    /// </list>
    /// </remarks>
    public static byte[] GetRomData()
    {
        if (cachedRom != null)
        {
            return cachedRom;
        }

        cachedRom = new byte[Size];

        // Fill with NOP instructions (safe default)
        Array.Fill(cachedRom, NopInstruction);

        // Write RESET handler at $F800 (offset 0)
        // This initializes CPU state and enters an idle loop
        int offset = 0;

        // CLD - Clear decimal mode
        cachedRom[offset++] = CldInstruction;

        // SEI - Disable interrupts during initialization
        cachedRom[offset++] = SeiInstruction;

        // LDX #$FF - Initialize stack pointer high
        cachedRom[offset++] = LdxImmInstruction;
        cachedRom[offset++] = 0xFF;

        // TXS - Transfer X to stack pointer (SP = $FF, stack at $01FF)
        cachedRom[offset++] = TxsInstruction;

        // CLI - Clear interrupt disable (enable interrupts)
        cachedRom[offset++] = CliInstruction;

        // WAI loop start (at $F806)
        int waiLoopOffset = offset;
        ushort waiLoopAddress = (ushort)(LoadAddress + waiLoopOffset);

        // WAI - Wait for interrupt (efficient idle)
        cachedRom[offset++] = WaiInstruction;

        // JMP back to WAI (infinite loop)
        cachedRom[offset++] = JmpAbsInstruction;
        cachedRom[offset++] = (byte)(waiLoopAddress & 0xFF);        // Low byte
        cachedRom[offset++] = (byte)((waiLoopAddress >> 8) & 0xFF); // High byte

        // Write interrupt handler at $F810 (simple RTI)
        int interruptOffset = InterruptHandlerAddress - LoadAddress;
        cachedRom[interruptOffset] = RtiInstruction;

        // Write CPU vectors at end of ROM
        // NMI vector ($FFFA-$FFFB) - points to RTI handler
        cachedRom[NmiVectorOffset] = (byte)(InterruptHandlerAddress & 0xFF);
        cachedRom[NmiVectorOffset + 1] = (byte)((InterruptHandlerAddress >> 8) & 0xFF);

        // RESET vector ($FFFC-$FFFD) - points to RESET handler
        cachedRom[ResetVectorOffset] = (byte)(ResetHandlerAddress & 0xFF);
        cachedRom[ResetVectorOffset + 1] = (byte)((ResetHandlerAddress >> 8) & 0xFF);

        // IRQ/BRK vector ($FFFE-$FFFF) - points to RTI handler
        cachedRom[IrqVectorOffset] = (byte)(InterruptHandlerAddress & 0xFF);
        cachedRom[IrqVectorOffset + 1] = (byte)((InterruptHandlerAddress >> 8) & 0xFF);

        return cachedRom;
    }

    /// <summary>
    /// Clears the cached ROM data, forcing regeneration on next access.
    /// </summary>
    /// <remarks>
    /// This is primarily useful for testing scenarios where the ROM
    /// needs to be regenerated.
    /// </remarks>
    internal static void ClearCache()
    {
        cachedRom = null;
    }
}