I'll analyze the codebase thoroughly to create a comprehensive, staged implementation plan for evolving the Applesoft BASIC interpreter into a more capable Apple II/IIgs emulation platform.
Now let me look at the IAppleSystem interface and examine the existing test structure:
Now I have a comprehensive understanding of the codebase. Let me create a detailed, staged implementation plan:
Now let me provide a detailed breakdown of each stage as GitHub issues with comprehensive descriptions:

---

# Detailed Implementation Plan for Applesoft BASIC Interpreter Evolution

## Executive Summary

This plan transforms the Applesoft BASIC interpreter into a comprehensive Apple II/IIgs emulation platform through 11 logical stages. Each stage is designed to be a standalone GitHub issue that GitHub Copilot can act upon, with clear acceptance criteria and dependencies.

---

## Stage 1: Foundation Refactoring - SystemContext Pattern

**Issue Title:** `Refactor: Introduce SystemContext to reduce constructor bloat`

### Problem Statement
The current `BasicInterpreter` constructor has 9 parameters, and `AppleSystem` has 4. As we add more features (disk controllers, expansion cards, file managers), this will grow unmanageably.

### Current State
```csharp
public BasicInterpreter(
    IParser parser,
    IBasicIO io,
    IVariableManager variables,
    IFunctionManager functions,
    IDataManager data,
    ILoopManager loops,
    IGosubManager gosub,
    IAppleSystem appleSystem,
    ILogger<BasicInterpreter> logger)
```

### Proposed Solution
Create a `SystemContext` class that aggregates all system-level dependencies:

```csharp
public interface ISystemContext
{
    IAppleSystem System { get; }
    IBasicIO IO { get; }
    IVariableManager Variables { get; }
    IFunctionManager Functions { get; }
    IDataManager Data { get; }
    ILoopManager Loops { get; }
    IGosubManager Gosub { get; }
    ILogger Logger { get; }
}
```

### Files to Create/Modify
- `src/ApplesoftBasic.Interpreter/Runtime/ISystemContext.cs` (new)
- `src/ApplesoftBasic.Interpreter/Runtime/SystemContext.cs` (new)
- `src/ApplesoftBasic.Interpreter/Execution/BasicInterpreter.cs` (modify)
- `src/ApplesoftBasic.Interpreter/InterpreterModule.cs` (modify)
- `tests/ApplesoftBasic.Tests/InterpreterTests.cs` (modify)

### Acceptance Criteria
- [ ] `ISystemContext` interface created with all current dependencies
- [ ] `SystemContext` implementation created
- [ ] `BasicInterpreter` constructor simplified to accept `ISystemContext` + `IParser`
- [ ] DI registration updated in `InterpreterModule`
- [ ] All existing tests pass
- [ ] Build succeeds with no warnings

---

## Stage 2: Command-Line Infrastructure

**Issue Title:** `Feature: Add extensible command-line argument processing`

### Problem Statement
The current `Program.cs` uses basic `args` array processing. We need a robust CLI framework to support:
- CPU mode selection (6502/65816)
- Memory size configuration
- Disk image mounting
- Debug/verbose output control

### Proposed Solution
Use `System.CommandLine` package for professional CLI handling:

```csharp
public class CommandLineOptions
{
    public FileInfo? SourceFile { get; set; }
    public CpuMode CpuMode { get; set; } = CpuMode.Cpu6502;
    public int MemorySize { get; set; } = 65536; // 64KB default
    public LogLevel LogLevel { get; set; } = LogLevel.Warning;
    public List<DiskMount> DiskMounts { get; set; } = [];
}

public enum CpuMode
{
    Cpu6502,           // Classic Apple II (default)
    Cpu65816Emulation, // Apple IIgs in emulation mode
    Cpu65816Native     // Apple IIgs in native mode
}
```

### Example Usage
```sh
# Current (maintained for compatibility)
ApplesoftBasic.Console demo.bas

# New options
ApplesoftBasic.Console demo.bas --cpu-mode 6502
ApplesoftBasic.Console demo.bas --cpu-mode 65816-emulation --memory 1MB
ApplesoftBasic.Console demo.bas --disk-slot6 games.dsk --verbose
ApplesoftBasic.Console --help
```

### Files to Create/Modify
- `src/ApplesoftBasic.Console/CommandLineOptions.cs` (new)
- `src/ApplesoftBasic.Console/CpuMode.cs` (new)
- `src/ApplesoftBasic.Console/Program.cs` (modify)
- `src/ApplesoftBasic.Console/ApplesoftBasic.Console.csproj` (add package)

### Dependencies
- `System.CommandLine` NuGet package

### Acceptance Criteria
- [ ] `System.CommandLine` package added
- [ ] `CommandLineOptions` class with all options
- [ ] `CpuMode` enum with 3 modes
- [ ] `--help` displays usage information
- [ ] Default mode is `Cpu6502` with 64KB RAM
- [ ] Existing single-argument usage still works
- [ ] Memory sizes accept KB/MB suffixes (e.g., `--memory 128KB`)

---

## Stage 3: 65816 CPU Foundation

**Issue Title:** `Feature: Implement 65816 CPU with 6502 emulation mode`

### Problem Statement
The Apple IIgs uses a 65816 CPU that can run in 6502 emulation mode. We want to support this for future Apple IIgs emulation while defaulting to pure 6502 mode.

### Current State
- `Cpu6502.cs` implements full 6502 instruction set
- `Cpu6502Registers.cs` has 6502 registers
- `Cpu65816Registers.cs` extends with 65816 registers (exists but unused)

### Proposed Architecture
```
ICpu (interface)
├── Cpu6502 (existing, refactored)
└── Cpu65816 
    ├── Emulation Mode (behaves like 6502)
    └── Native Mode (16-bit operations)
```

### Key 65816 Features to Implement
1. **Emulation Mode (E=1)**: Behaves like 6502 with some enhancements
2. **16-bit Registers**: A/X/Y can be 8 or 16 bits
3. **24-bit Addressing**: Program Bank (PBR) + Data Bank (DBR)
4. **New Instructions**: XBA, XCE, REP, SEP, PEA, PEI, PER, MVN, MVP, etc.
5. **New Addressing Modes**: Stack relative, direct page indirect long

### Files to Create/Modify
- `src/ApplesoftBasic.Interpreter/Emulation/ICpuRegisters.cs` (new)
- `src/ApplesoftBasic.Interpreter/Emulation/Cpu6502.cs` (modify)
- `src/ApplesoftBasic.Interpreter/Emulation/Cpu65816.cs` (new)
- `src/ApplesoftBasic.Interpreter/Emulation/CpuFactory.cs` (new)
- `src/ApplesoftBasic.Interpreter/Emulation/ICpu.cs` (modify)
- `tests/ApplesoftBasic.Tests/Cpu65816Tests.cs` (new)

### Acceptance Criteria
- [ ] `Cpu65816` class created extending `Cpu6502`
- [ ] Emulation mode works identically to `Cpu6502`
- [ ] XCE instruction toggles emulation/native mode
- [ ] REP/SEP instructions control register widths
- [ ] Bank registers (DBR, PBR) accessible
- [ ] `CpuFactory` creates correct CPU based on mode
- [ ] All 6502 tests pass with `Cpu65816` in emulation mode
- [ ] New 65816-specific tests added

---

## Stage 4: Memory Architecture Enhancement

**Issue Title:** `Feature: Implement banked memory supporting up to 16MB`

### Problem Statement
Current `AppleMemory` is fixed at 64KB. For Apple IIgs emulation and larger programs, we need:
- Configurable memory sizes (64KB to 8MB typical)
- Bank-switched memory architecture
- Memory mapping for expansion cards

### Proposed Architecture
```
IMemory (interface)
├── AppleMemory (64KB, current - refactored)
└── BankedMemory (up to 16MB)
    ├── Bank 0: $00/0000 - $00/FFFF (64KB)
    ├── Bank 1: $01/0000 - $01/FFFF (64KB)
    └── Bank N: ...
```

### Memory Configuration
```csharp
public class MemoryConfiguration
{
    public int TotalSize { get; init; } = 65536;
    public int BankSize { get; init; } = 65536;
    public bool HasLanguageCard { get; init; } = true;
    public bool HasAuxMemory { get; init; } = false; // 128KB Apple IIe
}
```

### Files to Create/Modify
- `src/ApplesoftBasic.Interpreter/Emulation/IMemoryBank.cs` (new)
- `src/ApplesoftBasic.Interpreter/Emulation/MemoryConfiguration.cs` (new)
- `src/ApplesoftBasic.Interpreter/Emulation/BankedMemory.cs` (new)
- `src/ApplesoftBasic.Interpreter/Emulation/MemoryMap.cs` (new)
- `src/ApplesoftBasic.Interpreter/Emulation/AppleMemory.cs` (modify)
- `tests/ApplesoftBasic.Tests/BankedMemoryTests.cs` (new)

### Acceptance Criteria
- [ ] `MemoryConfiguration` class created
- [ ] `BankedMemory` supports configurable size up to 16MB
- [ ] Memory bank switching via soft switches
- [ ] Backward compatible - default 64KB works unchanged
- [ ] `--memory` CLI option wired up
- [ ] Memory configuration tests pass

---

## Stage 5: Expansion Slot Architecture

**Issue Title:** `Feature: Implement Apple II expansion slot architecture`

### Problem Statement
Apple II has 7 expansion slots for peripherals (disk controllers, memory cards, etc.). We need an architecture to support pluggable expansion cards.

### Apple II Slot Memory Map
```
$C0n0-$C0nF  - Slot n I/O (soft switches)
$CnXX        - Slot n ROM (256 bytes)
$C800-$CFFF  - Shared expansion ROM (2KB, one slot at a time)
```

### Proposed Architecture
```csharp
public interface IExpansionCard
{
    string Name { get; }
    byte ReadIO(int offset);        // $C0n0-$C0nF
    void WriteIO(int offset, byte value);
    byte ReadRom(int offset);       // $CnXX
    byte ReadExpansionRom(int offset); // $C800-$CFFF
    void Reset();
}

public class SlotManager
{
    IExpansionCard?[] Slots { get; } = new IExpansionCard?[8]; // 0-7
    void InsertCard(int slot, IExpansionCard card);
    void RemoveCard(int slot);
}
```

### Standard Slot Assignments
- Slot 0: Reserved (motherboard)
- Slot 1: Printer interface
- Slot 2: Serial/modem
- Slot 3: 80-column card
- Slot 4: Mouse/clock
- Slot 5: SmartPort/hard disk
- Slot 6: Disk II controller (5.25" floppy)
- Slot 7: SmartPort/hard disk (alternate)

### Files to Create/Modify
- `src/ApplesoftBasic.Interpreter/Emulation/Cards/IExpansionCard.cs` (new)
- `src/ApplesoftBasic.Interpreter/Emulation/Cards/ExpansionSlot.cs` (new)
- `src/ApplesoftBasic.Interpreter/Emulation/Cards/SlotManager.cs` (new)
- `src/ApplesoftBasic.Interpreter/Emulation/Cards/NullCard.cs` (new)
- `src/ApplesoftBasic.Interpreter/Emulation/AppleMemory.cs` (modify)
- `src/ApplesoftBasic.Interpreter/Emulation/AppleSystem.cs` (modify)
- `tests/ApplesoftBasic.Tests/ExpansionSlotTests.cs` (new)

### Acceptance Criteria
- [ ] `IExpansionCard` interface defined
- [ ] `SlotManager` manages 7 slots
- [ ] Memory reads/writes to $C0nX routed to cards
- [ ] ROM area $CnXX routed correctly
- [ ] Expansion ROM bank switching works
- [ ] `NullCard` as default for empty slots
- [ ] Slot manager tests pass

---

## Stage 6: Disk Controller Foundation

**Issue Title:** `Feature: Implement Disk II controller for 5.25" floppy emulation`

### Problem Statement
File I/O requires disk emulation. The Disk II controller is the foundation for ProDOS file access.

### Disk II Controller
- 6-phase stepper motor control
- Read/write head positioning
- Nibble-based encoding (6-and-2)
- Support for .dsk and .po image formats

### Proposed Architecture
```csharp
public interface IDiskController : IExpansionCard
{
    void InsertDisk(int drive, IDiskImage image);
    void EjectDisk(int drive);
    bool IsMotorOn { get; }
    int CurrentTrack { get; }
}

public interface IDiskImage
{
    string FilePath { get; }
    int Tracks { get; }
    int SectorsPerTrack { get; }
    byte[] ReadSector(int track, int sector);
    void WriteSector(int track, int sector, byte[] data);
}
```

### Files to Create/Modify
- `src/ApplesoftBasic.Interpreter/Emulation/Disk/IDiskController.cs` (new)
- `src/ApplesoftBasic.Interpreter/Emulation/Disk/IDiskImage.cs` (new)
- `src/ApplesoftBasic.Interpreter/Emulation/Disk/DiskImage.cs` (new)
- `src/ApplesoftBasic.Interpreter/Emulation/Disk/Disk525.cs` (new)
- `src/ApplesoftBasic.Interpreter/Emulation/Cards/DiskIICard.cs` (new)
- `tests/ApplesoftBasic.Tests/DiskControllerTests.cs` (new)

### Supported Image Formats
- `.dsk` - DOS-order sector images (140KB)
- `.po` - ProDOS-order sector images (140KB)
- `.nib` - Nibble images (future)

### Acceptance Criteria
- [ ] `DiskIICard` implements `IExpansionCard`
- [ ] `.dsk` and `.po` format reading works
- [ ] Sector read operations functional
- [ ] Motor on/off state tracked
- [ ] Track stepping implemented
- [ ] Card installs in slot 6 by default
- [ ] Disk controller tests pass

---

## Stage 7: ProDOS File System

**Issue Title:** `Feature: Implement ProDOS file system for disk access`

### Problem Statement
The `ProDosEmulator.cs` exists but only has stubs. We need real file system operations.

### ProDOS Structure
```
Block 0-1: Boot blocks
Block 2: Volume directory header
Block 3-5: Volume directory (continued)
Block 6+: Files and subdirectories
```

### Proposed Architecture
```csharp
public interface IFileSystem
{
    IEnumerable<FileEntry> ListDirectory(string path);
    FileEntry? GetFileInfo(string path);
    Stream OpenFile(string path, FileMode mode);
    void CreateFile(string path, FileType type);
    void DeleteFile(string path);
}

public class ProDosFileSystem : IFileSystem
{
    // ProDOS-specific implementation
}
```

### Files to Create/Modify
- `src/ApplesoftBasic.Interpreter/Emulation/FileSystem/IFileSystem.cs` (new)
- `src/ApplesoftBasic.Interpreter/Emulation/FileSystem/FileEntry.cs` (new)
- `src/ApplesoftBasic.Interpreter/Emulation/FileSystem/FileType.cs` (new)
- `src/ApplesoftBasic.Interpreter/Emulation/FileSystem/ProDosFileSystem.cs` (new)
- `src/ApplesoftBasic.Interpreter/Emulation/ProDosEmulator.cs` (modify)
- `tests/ApplesoftBasic.Tests/ProDosFileSystemTests.cs` (new)

### Acceptance Criteria
- [ ] Read volume directory
- [ ] Navigate subdirectories
- [ ] Open files for reading
- [ ] Open files for writing
- [ ] Create new files
- [ ] Delete files
- [ ] File type detection (TXT, BAS, BIN, etc.)
- [ ] File system tests with sample disk image

---

## Stage 8: BASIC File I/O Commands

**Issue Title:** `Feature: Implement BASIC file I/O commands (OPEN, CLOSE, PRINT#, INPUT#, GET#)`

### Problem Statement
The tokens exist in `TokenType.cs` but file I/O is not implemented in the parser or interpreter.

### Syntax
```
10 D$ = CHR$(4)
20 PRINT D$;"OPEN MYFILE.TXT"
30 PRINT D$;"READ MYFILE.TXT"
40 INPUT A$
50 PRINT D$;"CLOSE MYFILE.TXT"
```

Or using modern extension syntax:
```
10 OPEN "MYFILE.TXT", 1
20 INPUT# 1, A$
30 PRINT# 1, "HELLO"
40 CLOSE 1
```

### AST Nodes to Create
```csharp
public class OpenStatement : IStatement { ... }
public class CloseStatement : IStatement { ... }
public class PrintFileStatement : IStatement { ... }
public class InputFileStatement : IStatement { ... }
public class GetFileStatement : IStatement { ... }
```

### Files to Create/Modify
- `src/ApplesoftBasic.Interpreter/AST/OpenStatement.cs` (new)
- `src/ApplesoftBasic.Interpreter/AST/CloseStatement.cs` (new)
- `src/ApplesoftBasic.Interpreter/AST/PrintFileStatement.cs` (new)
- `src/ApplesoftBasic.Interpreter/AST/InputFileStatement.cs` (new)
- `src/ApplesoftBasic.Interpreter/AST/GetFileStatement.cs` (new)
- `src/ApplesoftBasic.Interpreter/AST/IAstVisitor.cs` (modify)
- `src/ApplesoftBasic.Interpreter/Lexer/BasicLexer.cs` (modify)
- `src/ApplesoftBasic.Interpreter/Parser/BasicParser.cs` (modify)
- `src/ApplesoftBasic.Interpreter/Execution/BasicInterpreter.cs` (modify)
- `src/ApplesoftBasic.Interpreter/Runtime/FileManager.cs` (new)
- `tests/ApplesoftBasic.Tests/FileIOTests.cs` (new)
- `samples/fileio.bas` (new)

### Acceptance Criteria
- [ ] `OPEN filename, channel` opens file
- [ ] `CLOSE channel` closes file
- [ ] `PRINT# channel, data` writes to file
- [ ] `INPUT# channel, var` reads from file
- [ ] `GET# channel, var$` reads single character
- [ ] Error handling for missing files
- [ ] Maximum 16 open files supported
- [ ] File I/O tests pass
- [ ] Sample program demonstrates file I/O

---

## Stage 9: Hard Disk Support

**Issue Title:** `Feature: Implement SmartPort hard disk emulation`

### Problem Statement
5.25" floppies are limited to 140KB. Hard disk support enables larger programs and data.

### SmartPort Protocol
SmartPort is Apple's block-level device protocol for hard disks, 3.5" drives, etc.

### Supported Image Formats
- `.hdv` - Raw block image
- `.2mg` - Universal disk image with header
- `.po` - ProDOS-order (reused from floppy)

### Files to Create/Modify
- `src/ApplesoftBasic.Interpreter/Emulation/Disk/IBlockDevice.cs` (new)
- `src/ApplesoftBasic.Interpreter/Emulation/Disk/HardDiskImage.cs` (new)
- `src/ApplesoftBasic.Interpreter/Emulation/Cards/SmartPortCard.cs` (new)
- `tests/ApplesoftBasic.Tests/HardDiskTests.cs` (new)

### Acceptance Criteria
- [ ] SmartPort protocol implemented
- [ ] `.hdv` format support
- [ ] `.2mg` format support (with header parsing)
- [ ] Multiple partition support
- [ ] Default installation in slot 7
- [ ] Hard disk tests pass

---

## Stage 10: Error Handling (ONERR/RESUME)

**Issue Title:** `Feature: Implement ONERR GOTO and RESUME error handling`

### Problem Statement
The `ONERR` and `RESUME` tokens exist but aren't implemented. Error handling is essential for robust programs.

### Syntax
```
10 ONERR GOTO 1000
20 PRINT 1/0        : REM This will error
30 PRINT "DONE"
40 END
1000 PRINT "ERROR "; PEEK(222)
1010 RESUME         : REM Continue at line 30
```

### Error Codes (PEEK 222)
- 0: Next without FOR
- 16: Syntax error
- 22: RETURN without GOSUB
- 42: Out of memory
- 53: Illegal quantity
- 69: Overflow
- 77: Out of data
- 90: Undefined statement
- 133: Division by zero

### Files to Create/Modify
- `src/ApplesoftBasic.Interpreter/AST/OnErrStatement.cs` (new)
- `src/ApplesoftBasic.Interpreter/AST/ResumeStatement.cs` (new)
- `src/ApplesoftBasic.Interpreter/Runtime/ErrorState.cs` (new)
- `src/ApplesoftBasic.Interpreter/Runtime/BasicErrorCode.cs` (new)
- `src/ApplesoftBasic.Interpreter/Lexer/BasicLexer.cs` (modify)
- `src/ApplesoftBasic.Interpreter/Parser/BasicParser.cs` (modify)
- `src/ApplesoftBasic.Interpreter/Execution/BasicInterpreter.cs` (modify)
- `tests/ApplesoftBasic.Tests/ErrorHandlingTests.cs` (new)
- `samples/error_handling.bas` (new)

### Acceptance Criteria
- [ ] `ONERR GOTO linenum` sets error handler
- [ ] Errors jump to handler instead of stopping
- [ ] Error code stored at memory location 222
- [ ] `RESUME` continues after error line
- [ ] `RESUME linenum` jumps to specific line
- [ ] `POKE 216,0` clears error handler
- [ ] Error handling tests pass

---

## Stage 11: Integration and Polish

**Issue Title:** `Chore: Integration testing, documentation, and polish`

### Tasks
1. **Sample Programs**
   - `samples/fileio.bas` - File I/O demonstration
   - `samples/disk_catalog.bas` - List disk directory
   - `samples/error_demo.bas` - Error handling demo

2. **Documentation Updates**
   - Update `README.md` with new capabilities
   - Update `wiki/Custom-Extensions.md` with file I/O
   - Create `wiki/CPU-Modes.md` for 6502/65816
   - Create `wiki/Disk-Emulation.md` for disk operations
   - Create `wiki/Command-Line-Reference.md`

3. **Integration Testing**
   - End-to-end file I/O test
   - Disk boot test
   - Multi-slot configuration test

4. **Performance**
   - Profile CPU emulation
   - Optimize hot paths
   - Memory usage analysis

### Files to Create/Modify
- `samples/fileio.bas` (new)
- `samples/disk_catalog.bas` (new)
- `samples/error_demo.bas` (new)
- `README.md` (modify)
- `wiki/Custom-Extensions.md` (modify)
- `wiki/CPU-Modes.md` (new)
- `wiki/Disk-Emulation.md` (new)
- `wiki/Command-Line-Reference.md` (new)
- `tests/ApplesoftBasic.Tests/IntegrationTests.cs` (new)

### Acceptance Criteria
- [ ] All sample programs run successfully
- [ ] Documentation complete and accurate
- [ ] Integration tests pass
- [ ] No performance regressions
- [ ] Build produces no warnings

---

## Dependency Graph

```
Stage 1 (SystemContext)
    └── Stage 2 (CLI)
            └── Stage 3 (65816 CPU)
                    ├── Stage 4 (Memory)
                    │       └── Stage 5 (Expansion Slots)
                    │               ├── Stage 6 (Disk Controller)
                    │               │       └── Stage 7 (ProDOS)
                    │               │               └── Stage 8 (File I/O)
                    │               └── Stage 9 (Hard Disk)
                    └── Stage 10 (Error Handling)
                            └── Stage 11 (Integration)
```

---

## Estimated Timeline

| Stage | Title               | Estimated Effort | Dependencies |
| ----- | ------------------- | ---------------- | ------------ |
| 1     | SystemContext       | 2-4 hours        | None         |
| 2     | Command-Line        | 4-6 hours        | Stage 1      |
| 3     | 65816 CPU           | 16-24 hours      | Stage 2      |
| 4     | Memory Architecture | 8-12 hours       | Stage 3      |
| 5     | Expansion Slots     | 8-12 hours       | Stage 4      |
| 6     | Disk Controller     | 12-16 hours      | Stage 5      |
| 7     | ProDOS File System  | 12-16 hours      | Stage 6      |
| 8     | BASIC File I/O      | 8-12 hours       | Stage 7      |
| 9     | Hard Disk           | 8-12 hours       | Stage 5      |
| 10    | Error Handling      | 6-8 hours        | Stage 3      |
| 11    | Integration         | 8-12 hours       | All          |

**Total Estimated: 92-130 hours**

---

## Issue Labels Recommendation

- `enhancement` - New feature
- `refactor` - Code improvement without new features
- `cpu` - CPU emulation related
- `memory` - Memory subsystem
- `disk` - Disk/storage related
- `basic` - BASIC language features
- `breaking-change` - May affect existing behavior
- `documentation` - Documentation updates

---

This plan provides a clear roadmap for evolving the Applesoft BASIC interpreter into a comprehensive Apple II/IIgs emulation platform while maintaining backward compatibility and following best practices for extensibility.