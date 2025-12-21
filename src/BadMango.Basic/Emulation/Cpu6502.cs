// <copyright file="Cpu6502.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Basic.Emulation;

using System.Diagnostics.CodeAnalysis;

using Microsoft.Extensions.Logging;

/// <summary>
/// 6502 CPU emulator.
/// </summary>
[ExcludeFromCodeCoverage]
public class Cpu6502 : ICpu
{
    private readonly ILogger<Cpu6502> logger;
    private bool halted;
    private int cycles;

    /// <summary>
    /// Initializes a new instance of the <see cref="Cpu6502"/> class.
    /// </summary>
    /// <param name="memory">The memory interface used by the CPU emulator.</param>
    /// <param name="logger">The logger instance for logging CPU-related operations.</param>
    public Cpu6502(IMemory memory, ILogger<Cpu6502> logger)
    {
        Memory = memory;
        this.logger = logger;
        Registers = new();
    }

    /// <summary>
    /// Gets the CPU registers for the 6502 CPU emulator.
    /// </summary>
    /// <remarks>
    /// The <see cref="Cpu6502Registers"/> class represents the internal registers of the 6502 CPU,
    /// including the accumulator, index registers, stack pointer, program counter, and processor status.
    /// </remarks>
    public Cpu6502Registers Registers { get; }

    /// <summary>
    /// Gets the memory interface used by the 6502 CPU for reading and writing data.
    /// </summary>
    /// <remarks>
    /// The <see cref="IMemory"/> implementation provides access to the memory space
    /// required for the CPU's operation, including fetching instructions and accessing data.
    /// </remarks>
    public IMemory Memory { get; }

    /// <summary>
    /// Gets a value indicating whether the CPU is currently halted.
    /// </summary>
    /// <remarks>
    /// When the CPU is halted, it ceases execution of instructions until it is reset or resumed.
    /// This property reflects the internal state of the CPU emulator.
    /// </remarks>
    public bool Halted => halted;

    /// <summary>
    /// Resets the CPU to its initial state.
    /// </summary>
    /// <remarks>
    /// This method resets the CPU registers to their default values and sets the program counter (PC)
    /// to the address specified by the reset vector located at memory address <c>0xFFFC</c>.
    /// Additionally, it clears the halted state of the CPU.
    /// </remarks>
    /// <example>
    /// The following example demonstrates how to reset the CPU:
    /// <code>
    /// var cpu = new Cpu6502(memory, logger);
    /// cpu.Reset();
    /// </code>
    /// </example>
    public void Reset()
    {
        Registers.Reset();

        // Load reset vector
        Registers.PC = Memory.ReadWord(0xFFFC);
        halted = false;
        logger.LogDebug("CPU reset, PC=${PC:X4}", Registers.PC);
    }

    /// <summary>
    /// Executes instructions starting from the specified memory address.
    /// </summary>
    /// <param name="startAddress">The memory address from which execution begins.</param>
    /// <remarks>
    /// This method sets the program counter to the specified start address and begins
    /// executing instructions until the CPU is halted. It logs the starting address
    /// and processes instructions in a loop by repeatedly invoking the <see cref="Step"/> method.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the CPU is in an invalid state that prevents execution.
    /// </exception>
    public void Execute(int startAddress)
    {
        Registers.PC = (ushort)startAddress;
        halted = false;

        logger.LogDebug("Starting execution at ${Address:X4}", startAddress);

        while (!halted)
        {
            Step();
        }
    }

    /// <summary>
    /// Executes a single instruction at the current program counter and updates the CPU state.
    /// </summary>
    /// <returns>
    /// The number of cycles taken to execute the instruction.
    /// </returns>
    /// <remarks>
    /// This method fetches the opcode at the current program counter, decodes it, and executes
    /// the corresponding instruction. It also increments the program counter and updates the
    /// cycle count based on the executed instruction.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the CPU is in a halted state when this method is called.
    /// </exception>
    public int Step()
    {
        if (halted)
        {
            return 0;
        }

        cycles = 0;
        byte opcode = FetchByte();

        ExecuteOpcode(opcode);

        return cycles;
    }

    private byte FetchByte()
    {
        byte value = Memory.Read(Registers.PC);
        Registers.PC++;
        cycles++;
        return value;
    }

    private ushort FetchWord()
    {
        ushort low = FetchByte();
        ushort high = FetchByte();
        return (ushort)(low | (high << 8));
    }

    private void ExecuteOpcode(byte opcode)
    {
        switch (opcode)
        {
            // BRK - Break
            case 0x00: BRK(); break;

            // ORA - OR Memory with Accumulator
            case 0x01: ORA(IndirectX()); break;
            case 0x05: ORA(ZeroPage()); break;
            case 0x09: ORA(Immediate()); break;
            case 0x0D: ORA(Absolute()); break;
            case 0x11: ORA(IndirectY()); break;
            case 0x15: ORA(ZeroPageX()); break;
            case 0x19: ORA(AbsoluteY()); break;
            case 0x1D: ORA(AbsoluteX()); break;

            // ASL - Arithmetic Shift Left
            case 0x06: ASL_Memory(ZeroPage()); break;
            case 0x0A: ASL_Accumulator(); break;
            case 0x0E: ASL_Memory(Absolute()); break;
            case 0x16: ASL_Memory(ZeroPageX()); break;
            case 0x1E: ASL_Memory(AbsoluteX()); break;

            // PHP - Push Processor Status
            case 0x08: PHP(); break;

            // BPL - Branch if Positive
            case 0x10: Branch(!Registers.Negative); break;

            // CLC - Clear Carry Flag
            case 0x18: Registers.Carry = false; break;

            // JSR - Jump to Subroutine
            case 0x20: JSR(); break;

            // AND - AND Memory with Accumulator
            case 0x21: AND(IndirectX()); break;
            case 0x25: AND(ZeroPage()); break;
            case 0x29: AND(Immediate()); break;
            case 0x2D: AND(Absolute()); break;
            case 0x31: AND(IndirectY()); break;
            case 0x35: AND(ZeroPageX()); break;
            case 0x39: AND(AbsoluteY()); break;
            case 0x3D: AND(AbsoluteX()); break;

            // BIT - Test Bits in Memory
            case 0x24: BIT(ZeroPage()); break;
            case 0x2C: BIT(Absolute()); break;

            // ROL - Rotate Left
            case 0x26: ROL_Memory(ZeroPage()); break;
            case 0x2A: ROL_Accumulator(); break;
            case 0x2E: ROL_Memory(Absolute()); break;
            case 0x36: ROL_Memory(ZeroPageX()); break;
            case 0x3E: ROL_Memory(AbsoluteX()); break;

            // PLP - Pull Processor Status
            case 0x28: PLP(); break;

            // BMI - Branch if Minus
            case 0x30: Branch(Registers.Negative); break;

            // SEC - Set Carry Flag
            case 0x38: Registers.Carry = true; break;

            // RTI - Return from Interrupt
            case 0x40: RTI(); break;

            // EOR - XOR Memory with Accumulator
            case 0x41: EOR(IndirectX()); break;
            case 0x45: EOR(ZeroPage()); break;
            case 0x49: EOR(Immediate()); break;
            case 0x4D: EOR(Absolute()); break;
            case 0x51: EOR(IndirectY()); break;
            case 0x55: EOR(ZeroPageX()); break;
            case 0x59: EOR(AbsoluteY()); break;
            case 0x5D: EOR(AbsoluteX()); break;

            // LSR - Logical Shift Right
            case 0x46: LSR_Memory(ZeroPage()); break;
            case 0x4A: LSR_Accumulator(); break;
            case 0x4E: LSR_Memory(Absolute()); break;
            case 0x56: LSR_Memory(ZeroPageX()); break;
            case 0x5E: LSR_Memory(AbsoluteX()); break;

            // PHA - Push Accumulator
            case 0x48: PushByte(Registers.A); break;

            // JMP - Jump
            case 0x4C: JMP(Absolute()); break;
            case 0x6C: JMP(Indirect()); break;

            // BVC - Branch if Overflow Clear
            case 0x50: Branch(!Registers.Overflow); break;

            // CLI - Clear Interrupt Disable
            case 0x58: Registers.InterruptDisabled = false; break;

            // RTS - Return from Subroutine
            case 0x60: RTS(); break;

            // ADC - Add with Carry
            case 0x61: ADC(IndirectX()); break;
            case 0x65: ADC(ZeroPage()); break;
            case 0x69: ADC(Immediate()); break;
            case 0x6D: ADC(Absolute()); break;
            case 0x71: ADC(IndirectY()); break;
            case 0x75: ADC(ZeroPageX()); break;
            case 0x79: ADC(AbsoluteY()); break;
            case 0x7D: ADC(AbsoluteX()); break;

            // ROR - Rotate Right
            case 0x66: ROR_Memory(ZeroPage()); break;
            case 0x6A: ROR_Accumulator(); break;
            case 0x6E: ROR_Memory(Absolute()); break;
            case 0x76: ROR_Memory(ZeroPageX()); break;
            case 0x7E: ROR_Memory(AbsoluteX()); break;

            // PLA - Pull Accumulator
            case 0x68: Registers.A = PopByte(); Registers.SetNZ(Registers.A); break;

            // BVS - Branch if Overflow Set
            case 0x70: Branch(Registers.Overflow); break;

            // SEI - Set Interrupt Disable
            case 0x78: Registers.InterruptDisabled = true; break;

            // STA - Store Accumulator
            case 0x81: Memory.Write(IndirectX(), Registers.A); break;
            case 0x85: Memory.Write(ZeroPage(), Registers.A); break;
            case 0x8D: Memory.Write(Absolute(), Registers.A); break;
            case 0x91: Memory.Write(IndirectY(), Registers.A); break;
            case 0x95: Memory.Write(ZeroPageX(), Registers.A); break;
            case 0x99: Memory.Write(AbsoluteY(), Registers.A); break;
            case 0x9D: Memory.Write(AbsoluteX(), Registers.A); break;

            // STY - Store Y Register
            case 0x84: Memory.Write(ZeroPage(), Registers.Y); break;
            case 0x8C: Memory.Write(Absolute(), Registers.Y); break;
            case 0x94: Memory.Write(ZeroPageX(), Registers.Y); break;

            // STX - Store X Register
            case 0x86: Memory.Write(ZeroPage(), Registers.X); break;
            case 0x8E: Memory.Write(Absolute(), Registers.X); break;
            case 0x96: Memory.Write(ZeroPageY(), Registers.X); break;

            // DEY - Decrement Y
            case 0x88: Registers.Y--; Registers.SetNZ(Registers.Y); break;

            // TXA - Transfer X to A
            case 0x8A: Registers.A = Registers.X; Registers.SetNZ(Registers.A); break;

            // BCC - Branch if Carry Clear
            case 0x90: Branch(!Registers.Carry); break;

            // TYA - Transfer Y to A
            case 0x98: Registers.A = Registers.Y; Registers.SetNZ(Registers.A); break;

            // TXS - Transfer X to Stack Pointer
            case 0x9A: Registers.SP = Registers.X; break;

            // LDY - Load Y Register
            case 0xA0: Registers.Y = Memory.Read(Immediate()); Registers.SetNZ(Registers.Y); break;
            case 0xA4: Registers.Y = Memory.Read(ZeroPage()); Registers.SetNZ(Registers.Y); break;
            case 0xAC: Registers.Y = Memory.Read(Absolute()); Registers.SetNZ(Registers.Y); break;
            case 0xB4: Registers.Y = Memory.Read(ZeroPageX()); Registers.SetNZ(Registers.Y); break;
            case 0xBC: Registers.Y = Memory.Read(AbsoluteX()); Registers.SetNZ(Registers.Y); break;

            // LDA - Load Accumulator
            case 0xA1: Registers.A = Memory.Read(IndirectX()); Registers.SetNZ(Registers.A); break;
            case 0xA5: Registers.A = Memory.Read(ZeroPage()); Registers.SetNZ(Registers.A); break;
            case 0xA9: Registers.A = Memory.Read(Immediate()); Registers.SetNZ(Registers.A); break;
            case 0xAD: Registers.A = Memory.Read(Absolute()); Registers.SetNZ(Registers.A); break;
            case 0xB1: Registers.A = Memory.Read(IndirectY()); Registers.SetNZ(Registers.A); break;
            case 0xB5: Registers.A = Memory.Read(ZeroPageX()); Registers.SetNZ(Registers.A); break;
            case 0xB9: Registers.A = Memory.Read(AbsoluteY()); Registers.SetNZ(Registers.A); break;
            case 0xBD: Registers.A = Memory.Read(AbsoluteX()); Registers.SetNZ(Registers.A); break;

            // LDX - Load X Register
            case 0xA2: Registers.X = Memory.Read(Immediate()); Registers.SetNZ(Registers.X); break;
            case 0xA6: Registers.X = Memory.Read(ZeroPage()); Registers.SetNZ(Registers.X); break;
            case 0xAE: Registers.X = Memory.Read(Absolute()); Registers.SetNZ(Registers.X); break;
            case 0xB6: Registers.X = Memory.Read(ZeroPageY()); Registers.SetNZ(Registers.X); break;
            case 0xBE: Registers.X = Memory.Read(AbsoluteY()); Registers.SetNZ(Registers.X); break;

            // TAY - Transfer A to Y
            case 0xA8: Registers.Y = Registers.A; Registers.SetNZ(Registers.Y); break;

            // TAX - Transfer A to X
            case 0xAA: Registers.X = Registers.A; Registers.SetNZ(Registers.X); break;

            // BCS - Branch if Carry Set
            case 0xB0: Branch(Registers.Carry); break;

            // CLV - Clear Overflow Flag
            case 0xB8: Registers.Overflow = false; break;

            // TSX - Transfer Stack Pointer to X
            case 0xBA: Registers.X = Registers.SP; Registers.SetNZ(Registers.X); break;

            // CPY - Compare Y Register
            case 0xC0: Compare(Registers.Y, Immediate()); break;
            case 0xC4: Compare(Registers.Y, ZeroPage()); break;
            case 0xCC: Compare(Registers.Y, Absolute()); break;

            // CMP - Compare Accumulator
            case 0xC1: Compare(Registers.A, IndirectX()); break;
            case 0xC5: Compare(Registers.A, ZeroPage()); break;
            case 0xC9: Compare(Registers.A, Immediate()); break;
            case 0xCD: Compare(Registers.A, Absolute()); break;
            case 0xD1: Compare(Registers.A, IndirectY()); break;
            case 0xD5: Compare(Registers.A, ZeroPageX()); break;
            case 0xD9: Compare(Registers.A, AbsoluteY()); break;
            case 0xDD: Compare(Registers.A, AbsoluteX()); break;

            // DEC - Decrement Memory
            case 0xC6: DEC(ZeroPage()); break;
            case 0xCE: DEC(Absolute()); break;
            case 0xD6: DEC(ZeroPageX()); break;
            case 0xDE: DEC(AbsoluteX()); break;

            // INY - Increment Y
            case 0xC8: Registers.Y++; Registers.SetNZ(Registers.Y); break;

            // DEX - Decrement X
            case 0xCA: Registers.X--; Registers.SetNZ(Registers.X); break;

            // BNE - Branch if Not Equal
            case 0xD0: Branch(!Registers.Zero); break;

            // CLD - Clear Decimal Mode
            case 0xD8: Registers.Decimal = false; break;

            // CPX - Compare X Register
            case 0xE0: Compare(Registers.X, Immediate()); break;
            case 0xE4: Compare(Registers.X, ZeroPage()); break;
            case 0xEC: Compare(Registers.X, Absolute()); break;

            // SBC - Subtract with Carry
            case 0xE1: SBC(IndirectX()); break;
            case 0xE5: SBC(ZeroPage()); break;
            case 0xE9: SBC(Immediate()); break;
            case 0xED: SBC(Absolute()); break;
            case 0xF1: SBC(IndirectY()); break;
            case 0xF5: SBC(ZeroPageX()); break;
            case 0xF9: SBC(AbsoluteY()); break;
            case 0xFD: SBC(AbsoluteX()); break;

            // INC - Increment Memory
            case 0xE6: INC(ZeroPage()); break;
            case 0xEE: INC(Absolute()); break;
            case 0xF6: INC(ZeroPageX()); break;
            case 0xFE: INC(AbsoluteX()); break;

            // INX - Increment X
            case 0xE8: Registers.X++; Registers.SetNZ(Registers.X); break;

            // NOP - No Operation
            case 0xEA: break;

            // BEQ - Branch if Equal
            case 0xF0: Branch(Registers.Zero); break;

            // SED - Set Decimal Flag
            case 0xF8: Registers.Decimal = true; break;

            default:
                logger.LogWarning("Unknown opcode ${Opcode:X2} at ${PC:X4}", opcode, Registers.PC - 1);
                break;
        }
    }

    #region Addressing Modes

    private int Immediate()
    {
        return Registers.PC++;
    }

    private int ZeroPage()
    {
        return FetchByte();
    }

    private int ZeroPageX()
    {
        return (FetchByte() + Registers.X) & 0xFF;
    }

    private int ZeroPageY()
    {
        return (FetchByte() + Registers.Y) & 0xFF;
    }

    private int Absolute()
    {
        return FetchWord();
    }

    private int AbsoluteX()
    {
        int addr = FetchWord();
        return (addr + Registers.X) & 0xFFFF;
    }

    private int AbsoluteY()
    {
        int addr = FetchWord();
        return (addr + Registers.Y) & 0xFFFF;
    }

    private int IndirectX()
    {
        int zpAddr = (FetchByte() + Registers.X) & 0xFF;
        return Memory.ReadWord(zpAddr);
    }

    private int IndirectY()
    {
        int zpAddr = FetchByte();
        int addr = Memory.ReadWord(zpAddr);
        return (addr + Registers.Y) & 0xFFFF;
    }

    private int Indirect()
    {
        int addr = FetchWord();

        // 6502 indirect jump bug - doesn't cross page boundary
        if ((addr & 0xFF) == 0xFF)
        {
            byte low = Memory.Read(addr);
            byte high = Memory.Read(addr & 0xFF00);
            return low | (high << 8);
        }

        return Memory.ReadWord(addr);
    }

    #endregion

    #region Instructions

    private void BRK()
    {
        Registers.PC++;
        PushWord(Registers.PC);
        PushByte((byte)(Registers.P | 0x10)); // Set break flag
        Registers.InterruptDisabled = true;
        Registers.PC = Memory.ReadWord(0xFFFE);
        halted = true; // Stop execution for BASIC interpreter
    }

    private void ORA(int address)
    {
        Registers.A |= Memory.Read(address);
        Registers.SetNZ(Registers.A);
    }

    private void AND(int address)
    {
        Registers.A &= Memory.Read(address);
        Registers.SetNZ(Registers.A);
    }

    private void EOR(int address)
    {
        Registers.A ^= Memory.Read(address);
        Registers.SetNZ(Registers.A);
    }

    private void ADC(int address)
    {
        byte value = Memory.Read(address);
        int result = Registers.A + value + (Registers.Carry ? 1 : 0);

        Registers.Overflow = ((Registers.A ^ result) & (value ^ result) & 0x80) != 0;
        Registers.Carry = result > 0xFF;
        Registers.A = (byte)result;
        Registers.SetNZ(Registers.A);
    }

    private void SBC(int address)
    {
        byte value = Memory.Read(address);
        int result = Registers.A - value - (Registers.Carry ? 0 : 1);

        Registers.Overflow = ((Registers.A ^ result) & (Registers.A ^ value) & 0x80) != 0;
        Registers.Carry = result >= 0;
        Registers.A = (byte)result;
        Registers.SetNZ(Registers.A);
    }

    private void Compare(byte register, int address)
    {
        byte value = Memory.Read(address);
        int result = register - value;
        Registers.Carry = register >= value;
        Registers.Zero = register == value;
        Registers.Negative = (result & 0x80) != 0;
    }

    private void BIT(int address)
    {
        byte value = Memory.Read(address);
        Registers.Zero = (Registers.A & value) == 0;
        Registers.Overflow = (value & 0x40) != 0;
        Registers.Negative = (value & 0x80) != 0;
    }

    private void ASL_Accumulator()
    {
        Registers.Carry = (Registers.A & 0x80) != 0;
        Registers.A <<= 1;
        Registers.SetNZ(Registers.A);
    }

    private void ASL_Memory(int address)
    {
        byte value = Memory.Read(address);
        Registers.Carry = (value & 0x80) != 0;
        value <<= 1;
        Memory.Write(address, value);
        Registers.SetNZ(value);
    }

    private void LSR_Accumulator()
    {
        Registers.Carry = (Registers.A & 0x01) != 0;
        Registers.A >>= 1;
        Registers.SetNZ(Registers.A);
    }

    private void LSR_Memory(int address)
    {
        byte value = Memory.Read(address);
        Registers.Carry = (value & 0x01) != 0;
        value >>= 1;
        Memory.Write(address, value);
        Registers.SetNZ(value);
    }

    private void ROL_Accumulator()
    {
        bool newCarry = (Registers.A & 0x80) != 0;
        Registers.A = (byte)((Registers.A << 1) | (Registers.Carry ? 1 : 0));
        Registers.Carry = newCarry;
        Registers.SetNZ(Registers.A);
    }

    private void ROL_Memory(int address)
    {
        byte value = Memory.Read(address);
        bool newCarry = (value & 0x80) != 0;
        value = (byte)((value << 1) | (Registers.Carry ? 1 : 0));
        Registers.Carry = newCarry;
        Memory.Write(address, value);
        Registers.SetNZ(value);
    }

    private void ROR_Accumulator()
    {
        bool newCarry = (Registers.A & 0x01) != 0;
        Registers.A = (byte)((Registers.A >> 1) | (Registers.Carry ? 0x80 : 0));
        Registers.Carry = newCarry;
        Registers.SetNZ(Registers.A);
    }

    private void ROR_Memory(int address)
    {
        byte value = Memory.Read(address);
        bool newCarry = (value & 0x01) != 0;
        value = (byte)((value >> 1) | (Registers.Carry ? 0x80 : 0));
        Registers.Carry = newCarry;
        Memory.Write(address, value);
        Registers.SetNZ(value);
    }

    private void INC(int address)
    {
        byte value = (byte)(Memory.Read(address) + 1);
        Memory.Write(address, value);
        Registers.SetNZ(value);
    }

    private void DEC(int address)
    {
        byte value = (byte)(Memory.Read(address) - 1);
        Memory.Write(address, value);
        Registers.SetNZ(value);
    }

    private void Branch(bool condition)
    {
        sbyte offset = (sbyte)FetchByte();
        if (condition)
        {
            Registers.PC = (ushort)(Registers.PC + offset);
            cycles++;
        }
    }

    private void JMP(int address)
    {
        Registers.PC = (ushort)address;
    }

    private void JSR()
    {
        ushort addr = FetchWord();
        PushWord((ushort)(Registers.PC - 1));
        Registers.PC = addr;
    }

    private void RTS()
    {
        Registers.PC = (ushort)(PopWord() + 1);
        halted = true; // Stop execution for BASIC interpreter
    }

    private void RTI()
    {
        Registers.P = PopByte();
        Registers.PC = PopWord();
    }

    private void PHP()
    {
        PushByte((byte)(Registers.P | 0x10)); // Break flag set when pushed
    }

    private void PLP()
    {
        Registers.P = (byte)((PopByte() & 0xEF) | 0x20); // Clear break, set bit 5
    }

    private void PushByte(byte value)
    {
        Memory.Write(0x100 + Registers.SP, value);
        Registers.SP--;
    }

    private byte PopByte()
    {
        Registers.SP++;
        return Memory.Read(0x100 + Registers.SP);
    }

    private void PushWord(ushort value)
    {
        PushByte((byte)(value >> 8));
        PushByte((byte)(value & 0xFF));
    }

    private ushort PopWord()
    {
        byte low = PopByte();
        byte high = PopByte();
        return (ushort)(low | (high << 8));
    }

    #endregion
}