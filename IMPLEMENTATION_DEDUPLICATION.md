# Unified CPU Architecture - Implementation Summary

## Overview
This implementation provides a unified, reusable infrastructure for multi-CPU emulation (65C02, 65816, 65832) through runtime mode detection instead of compile-time generics. The architecture eliminates code duplication while maintaining static methods and aggressive inlining for performance.

## Type System

### Type Aliases (GlobalUsings.cs)
For semantic clarity and future-proofing, the codebase uses type aliases:

- **Addr** (uint) - 32-bit addresses, preparing for 65832 flat addressing
- **Word** (ushort) - 16-bit words
- **DWord** (uint) - 32-bit double words (semantic clarity vs Addr)

These aliases appear in `GlobalUsings.cs` in Core, Emulation, and Tests projects.

### Unified State Structure

**CpuState**
- Single state structure for all CPU variants (65C02, 65816, 65832)
- Contains universal `Registers` structure with all registers
- No generic type parameters needed
- Supports runtime mode detection via CPU flags

```csharp
public struct CpuState : ICpuState
{
    public Registers Registers;  // Universal register set
    public ulong Cycles;
    public HaltState HaltReason;
}
```

**Registers**
- Unified register structure containing all registers for all CPU variants
- Each register type has multiple "views" (byte/word/dword)
- Extension methods provide size-aware access based on CPU mode

```csharp
public struct Registers
{
    public RegisterAccumulator A;      // Supports 8/16/32-bit views
    public RegisterIndex X, Y;          // Supports 8/16/32-bit views
    public RegisterStackPointer SP;     // Supports 8/16/32-bit views
    public RegisterProgramCounter PC;   // Supports 16/32-bit views
    public ProcessorStatusFlags P;      // Flag register
    public RegisterDirectPage D;        // 65816+ direct page
    public byte DBR, PBR;              // 65816+ bank registers
    public DWord R0-R7;                // 65832 general purpose registers
    // ... additional registers
}
```

## Architecture

### Extension Methods for Size-Aware Access

**RegisterHelpers.cs (511 lines)**
Provides extension methods for accessing registers at different sizes:

```csharp
extension(ref RegisterAccumulator a)
{
    public byte GetByte() => (byte)(a.acc & 0xFF);
    public Word GetWord() => (Word)(a.acc & 0xFFFF);
    public DWord GetDWord() => a.acc;
    
    public RegisterAccumulator SetByte(byte value) { ... }
    public RegisterAccumulator SetWord(Word value) { ... }
    public RegisterAccumulator SetDWord(DWord value) { ... }
    
    public RegisterAccumulator SetValue(uint value, byte size) { ... }
}
```

**ProcessorStatusFlagsHelpers.cs (273 lines)**
Provides extension methods for flag manipulation:

```csharp
extension(ProcessorStatusFlags p)
{
    public bool IsCarrySet() => ((byte)p & CarryBit) != 0;
    public ProcessorStatusFlags SetCarry(bool value) { ... }
    public ProcessorStatusFlags SetZeroAndNegative(uint value, byte size) { ... }
    // ... more flag operations
}
```

### AddressingModes.cs
A static class providing reusable, mode-aware addressing mode implementations:

**Delegate Type:**
```csharp
public delegate Addr AddressingMode<TState>(IMemory memory, ref TState state)
    where TState : struct;
```

**Addressing Modes (15 total):**
- `Implied` - Register-only instructions
- `ImmediateByte` - Immediate addressing
- `ZeroPage` - Zero Page/Direct Page addressing (mode-aware)
- `ZeroPageX` / `ZeroPageY` - Indexed Zero Page
- `Absolute` - Absolute addressing
- `AbsoluteX` / `AbsoluteY` - Indexed Absolute
- `IndirectX` / `IndirectY` - Indirect addressing
- `AbsoluteXWrite` / `AbsoluteYWrite` / `IndirectYWrite` - Write variants
- `Accumulator` - Accumulator addressing
- `Relative` - Relative addressing for branches
- `Indirect` - Indirect jump

**Mode-Aware Behavior:**
- In 65C02 mode: Standard behavior with direct page at $0000
- In 65816 emulation mode: Compatible with 6502, stack at page 1
- In 65816 native mode: Direct page relocatable via D register, 16-bit operations possible

```csharp
public static Addr ZeroPage(IMemory memory, ref CpuState state)
{
    byte zpOffset = memory.Read(state.Registers.PC.addr++);
    Word directPage = state.Registers.D.GetWord();  // Runtime mode detection
    
    // Add cycle penalty if direct page not page-aligned (65816 native mode)
    if (!state.Registers.E && (directPage & 0xFF) != 0)
        state.Cycles++;
        
    return directPage + zpOffset;
}
```

### Instructions.cs (11 Partial Class Files)
Static instruction methods organized by category, using runtime mode detection:

**Design Pattern:**
```csharp
public static OpcodeHandler LDA(AddressingMode<CpuState> mode)
{
    return (memory, ref state) =>
    {
        Addr address = mode(memory, ref state);
        byte size = state.Registers.GetAccumulatorSize();  // Runtime check
        var value = memory.ReadValue(address, size);
        state.Cycles++;
        
        state.Registers.P.SetZeroAndNegative(value, size);
        state.Registers.A.SetValue(value, size);
    };
}
```

**Instruction Organization (11 files, 70 instructions total):**
1. **Instructions.cs** - Load/Store (LDA, LDX, LDY, STA, STX, STY), NOP, BRK
2. **Instructions.Flags.cs** - Flag manipulation (CLC, SEC, CLI, SEI, CLD, SED, CLV)
3. **Instructions.Transfer.cs** - Register transfers (TAX, TAY, TXA, TYA, TXS, TSX)
4. **Instructions.Stack.cs** - Stack operations (PHA, PHP, PLA, PLP, PHX, PLX, PHY, PLY)
5. **Instructions.Jump.cs** - Jumps and subroutines (JMP, JSR, RTS, RTI)
6. **Instructions.Branch.cs** - Branches (BCC, BCS, BEQ, BNE, BMI, BPL, BVC, BVS, BRA)
7. **Instructions.Arithmetic.cs** - Arithmetic (ADC, SBC, INC, DEC, INX, INY, DEX, DEY)
8. **Instructions.Logical.cs** - Logical operations (AND, ORA, EOR, BIT)
9. **Instructions.Shift.cs** - Shifts and rotates (ASL, LSR, ROL, ROR)
10. **Instructions.Compare.cs** - Comparisons (CMP, CPX, CPY)
11. **Instructions.65C02.cs** - 65C02-specific (STZ, TSB, TRB, WAI, STP)

**Key Features:**
- Runtime mode detection based on CPU flags (E, M, X)
- Size-aware operations via extension methods
- Single implementation adapts to all CPU variants
- Aggressive inlining for performance
- Organized by instruction category for maintainability

## Usage Example - Unified State Pattern

### Before (CPU-Specific Code)
```csharp
internal void LDA_Immediate()
{
    a = FetchByte();
    SetZN(a);
}

internal void LDA_ZeroPage()
{
    a = ReadZeroPage();
    SetZN(a);
}

internal void LDA_Absolute()
{
    a = ReadAbsolute();
    SetZN(a);
}
// ... 6 more LDA variants ...

internal void CLC()
{
    p &= unchecked((byte)~FlagC);
}
```

### After (Unified State Pattern)
```csharp
// In Cpu65C02OpcodeTableBuilder.cs - ONE LINE PER OPCODE
handlers[0xA9] = Instructions.LDA(AddressingModes.ImmediateByte);
handlers[0xA5] = Instructions.LDA(AddressingModes.ZeroPage);
handlers[0xAD] = Instructions.LDA(AddressingModes.Absolute);
handlers[0xB5] = Instructions.LDA(AddressingModes.ZeroPageX);
handlers[0xBD] = Instructions.LDA(AddressingModes.AbsoluteX);
// ... same LDA implementation adapts to ALL CPU modes

handlers[0x18] = Instructions.CLC(AddressingModes.Implied);
handlers[0x38] = Instructions.SEC(AddressingModes.Implied);
handlers[0xEA] = Instructions.NOP(AddressingModes.Implied);
```

**Runtime Adaptation:**
```csharp
// Same LDA instruction adapts to CPU mode at runtime
public static OpcodeHandler LDA(AddressingMode<CpuState> mode)
{
    return (memory, ref state) =>
    {
        Addr address = mode(memory, ref state);
        
        // Adapts to 8-bit (65C02) or 16-bit (65816 native) automatically
        byte size = state.Registers.GetAccumulatorSize();
        var value = memory.ReadValue(address, size);
        
        state.Registers.P.SetZeroAndNegative(value, size);
        state.Registers.A.SetValue(value, size);
    };
}
```

**Benefits:**
- No combinatorial explosion - single implementation for all modes
- Runtime mode detection enables multi-CPU support
- Size-aware operations via extension methods
- Same addressing modes used by all instructions

## Benefits

1. **Multi-CPU Support**: Single codebase for 65C02, 65816, and 65832
2. **Runtime Flexibility**: Instructions adapt to CPU mode at runtime
3. **Code Reuse**: Addressing mode logic is centralized and reusable
4. **Maintainability**: Organized into 11 partial classes by category
5. **Type Safety**: Extension methods provide compile-time checking
6. **Performance**: Aggressive inlining maintains hot-path performance
7. **Extensibility**: Easy to add new CPU modes and register sizes
8. **Testability**: Instructions and addressing modes tested independently
9. **Semantic Clarity**: Addr/Word/DWord types convey intent clearly
10. **No Duplication**: Single instruction implementation for all variants

## Testing

### Test Coverage
- **AddressingModesTests**: Mode-aware addressing mode tests
  - Implied mode (returns 0, no side effects)
  - ImmediateByte mode (PC increment)
  - ZeroPage modes with cycle counting and mode detection
  - ZeroPageX/Y with zero page wrapping validation
  - Absolute modes with 16-bit addressing
  - AbsoluteX/Y with page boundary cycle detection
  - IndirectX/Y with pointer indirection
  - Write variants always taking maximum cycles
  - Direct page relocation (65816 native mode)
  
- **InstructionsTests**: 153 emulator tests covering all 70 instructions
  - Load/Store: LDA, LDX, LDY, STA, STX, STY, STZ
  - Flag operations: CLC, SEC, CLI, SEI, CLD, SED, CLV
  - Register transfers: TAX, TAY, TXA, TYA, TXS, TSX
  - Stack operations: PHA, PHP, PLA, PLP, PHX, PLX, PHY, PLY
  - Jumps: JMP, JSR, RTS, RTI
  - Branches: BCC, BCS, BEQ, BNE, BMI, BPL, BVC, BVS, BRA
  - Arithmetic: ADC, SBC, INC, DEC, INX, INY, DEX, DEY
  - Logical: AND, ORA, EOR, BIT
  - Shifts: ASL, LSR, ROL, ROR
  - Comparisons: CMP, CPX, CPY
  - 65C02-specific: TSB, TRB, WAI, STP
    
- **Cpu65C02Tests**: Existing tests verify backward compatibility

### Test Results
- ✅ 153 emulator tests passing (unified architecture)
- ✅ 429 BASIC interpreter tests passing
- ✅ Total: 582 tests, 0 failures
- ✅ Build succeeds with 0 errors, 1 minor StyleCop warning
- ✅ Backward compatible with all existing functionality
- ✅ All processor flag manipulation validated
- ✅ Runtime mode detection working correctly

## Architecture Evolution

### From Generic Types to Unified State
The initial approach used generic type parameters but was abandoned in favor of a simpler unified state pattern:

**Previous Approach (Abandoned):**
- Complex generic type parameters on every method
- Required `CreateTruncating()` conversions everywhere
- Type inference challenges with 5+ type parameters
- Verbose signatures like `InstructionsFor<TCpu, TRegisters, TAccumulator, TIndex, TStack, TProgram, TState>`

**Current Approach (Unified State Pattern):**
- Single `CpuState` structure for all CPU variants
- Extension methods for size-aware register access
- Runtime mode detection via CPU flags
- Clean signatures like `OpcodeHandler LDA(AddressingMode<CpuState>)`

### Key Design Decisions
1. **Runtime over Compile-Time**: Mode detection at runtime provides flexibility without complexity
2. **Extension Methods**: Size-aware access patterns similar to x86-64 RAX/EAX/AX/AL
3. **Partial Classes**: Instructions organized into 11 files by category for maintainability
4. **Helper Methods**: Extensive helper classes (RegisterHelpers, ProcessorStatusFlagsHelpers, ControlRegisterHelpers)

## Future Work

### 65816 Support (Ready to Implement)
The unified architecture is ready for 65816 implementation:

```csharp
// 65816 native mode with 16-bit accumulator (M flag = 0)
byte size = state.Registers.GetAccumulatorSize();  // Returns 2 in native mode
Word value16 = state.Registers.A.GetWord();        // Access as 16-bit

// Addressing modes adapt automatically
Word directPage = state.Registers.D.GetWord();     // Relocatable direct page
bool emulationMode = state.Registers.E;            // Emulation mode flag
```

### 65832 Support (Foundation in Place)
The 32-bit register support is already in the Registers structure:

```csharp
// 65832 with 32-bit registers
DWord value32 = state.Registers.A.GetDWord();      // Access as 32-bit
DWord r0 = state.Registers.R0;                     // General purpose registers
```

### Potential Enhancements
1. Complete 65816 instruction set (new opcodes: XBA, XCE, REP, SEP, etc.)
2. 65832 privileged architecture (control registers, paging)
3. Performance profiling and optimization
4. Enhanced cycle-accurate timing
5. Additional CPU modes and variants

## Technical Notes

### OpcodeTable Architecture
- **OpcodeTable** uses unified `CpuState` parameter
- Handlers are **OpcodeHandler** delegates: `(memory, ref state) => { }`
- State passed by reference for direct manipulation
- Simplified from generic approach

### Unified State Pattern
- **Addressing modes** return `Addr` and operate on `CpuState`
- **Instructions** are higher-order functions returning opcode handlers
- Pattern: `Instructions.LDA(AddressingModes.ImmediateByte)` composes both
- Runtime mode detection eliminates combinatorial explosion

### Cycle Counting
- Addressing modes update cycles counter via ref parameter
- Page boundary detection adds extra cycles appropriately
- Write variants always take maximum cycles
- Mode-aware cycle penalties (e.g., direct page not page-aligned)

### Memory and State Access
- Memory access uses `Addr` (uint) for 32-bit addressing
- `CpuState` structure contains universal `Registers`
- Extension methods provide size-aware access
- All state manipulated through structure passed by reference

### Type System
- **Addr** (uint) for addresses - supports 32-bit addressing
- **Word** (ushort) for 16-bit values
- **DWord** (uint) for 32-bit values
- **Extension methods** provide byte/word/dword views of registers

### Performance Considerations
- All hot-path methods use `MethodImpl(MethodImplOptions.AggressiveInlining)`
- ref parameters avoid copying large structures
- Static methods avoid virtual dispatch
- Extension methods inline effectively
- Runtime mode checks are minimal overhead

## Files Changed

### New Files (Unified Architecture)
- **src/BadMango.Emulator.Core/CpuState.cs** - Unified CPU state structure
- **src/BadMango.Emulator.Core/Registers.cs** - Universal register set
- **src/BadMango.Emulator.Core/ProcessorStatusFlags.cs** - Flag enumeration
- **src/BadMango.Emulator.Core/RegisterHelpers.cs** - Size-aware register access (511 lines)
- **src/BadMango.Emulator.Core/ProcessorStatusFlagsHelpers.cs** - Flag operations (273 lines)
- **src/BadMango.Emulator.Core/ControlRegisterHelpers.cs** - Control register helpers (153 lines)
- **src/BadMango.Emulator.Emulation/Cpu/Instructions.*.cs** - 11 partial class files by category
- **tests/BadMango.Emulator.Tests/*Tests.cs** - Comprehensive test coverage

### Modified Files
- **src/BadMango.Emulator.Emulation/Cpu/AddressingModes.cs** - Mode-aware implementations
- **src/BadMango.Emulator.Emulation/Cpu/Cpu65C02.cs** - Uses unified state
- **src/BadMango.Emulator.Emulation/Cpu/Cpu65C02OpcodeTableBuilder.cs** - Simplified
- **src/BadMango.Emulator.Core/IMemory.cs** - Enhanced with ReadValue/WriteValue
- **README.md** - Updated architecture documentation
- **65C02-IMPLEMENTATION-ROADMAP.md** - Reflects unified architecture

### Removed Files (From Generic Approach)
- **AddressingModesFor.cs** - Replaced by unified AddressingModes
- **InstructionsFor.cs** - Replaced by partial Instructions classes
- **GenericOpcodeTableBuilder.cs** - No longer needed
- **OpcodeTableBuilders.cs** - No longer needed
- **AddressingModesHelpers.cs** - Functionality integrated

## Conclusion
This implementation successfully achieves multi-CPU support through a unified state architecture with runtime mode detection. The approach is simpler, more maintainable, and more flexible than the generic type parameter approach, while providing the same multi-CPU capabilities with better code organization and developer experience.
