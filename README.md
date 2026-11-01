# BackPocket Emulator

<p align="center">
  <img src="back-pocket-logo.png" alt="BackPocketBASIC Logo" width="200">
</p>

A .NET emulator for the WDC 65C02 (Apple IIe-class "Pocket2e" machine), with an architecture built to grow into the 65816 and a theoretical 32-bit 65832.

![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)
![License](https://img.shields.io/badge/license-MIT-green)

> **Looking for the Applesoft BASIC interpreter?** It now lives in its own repository, with history preserved: [back-pocket-basic-legacy](https://github.com/Bad-Mango-Solutions/back-pocket-basic-legacy).

## Features

### Multi-CPU Emulation (65C02/65816/65832)

- **Unified CPU Architecture**: Single codebase supports multiple CPU variants through runtime mode detection
- **65C02 Support**: Full WDC 65C02 instruction set (70 instructions, 15 addressing modes)
- **65816-Ready**: Architecture prepared for Apple IIgs 65816 emulation
- **65832-Ready**: Foundation for future 32-bit 65832 support
- **Extension Methods**: Size-aware register access (byte/word/dword views) adapts to CPU mode
- **Runtime Mode Detection**: Instructions adapt behavior based on CPU flags (E, M, X)

## Getting Started

### Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download) or later

### Building

```bash
# Clone the repository
git clone https://github.com/Bad-Mango-Solutions/back-pocket-basic.git
cd back-pocket-basic

# Build the solution
dotnet build BackPocketBasic.slnx

# Run tests
dotnet test BackPocketBasic.slnx

# Run tests with statement coverage
dotnet test BackPocketBasic.slnx --collect "XPlat Code Coverage"
```

## Architecture

### Project Structure

```
back-pocket-basic/
├── src/
│   ├── BadMango.Emulator.Core/        # Core emulator abstractions
│   │   ├── Cpu/                       # CPU state, registers, instructions
│   │   ├── Interfaces/                # Core interfaces (IMemory, ICpu)
│   │   └── Signaling/                 # Interrupt and signal handling
│   ├── BadMango.Emulator.Emulation/   # CPU implementations
│   │   └── Cpu/                       # 65C02, 65816, 65832 implementations
│   ├── BadMango.Emulator.Bus/         # System bus and memory management
│   ├── BadMango.Emulator.Devices/     # Peripheral device implementations
│   ├── BadMango.Emulator.Systems/     # Complete system configurations
│   ├── BadMango.Emulator.Debug/       # emudbg debug console + infrastructure (see emudbg.md)
│   ├── BadMango.Emulator.Infrastructure/ # Event and registration utilities
│   ├── BadMango.Emulator.Interop/     # Interoperability layer
│   ├── BadMango.Emulator.Configuration/ # Configuration management
│   ├── BadMango.Emulator.UI/          # Avalonia-based GUI (optional)
│   └── BadMango.Emulator.UI.Abstractions/ # UI abstraction interfaces
│
├── tests/
│   ├── BadMango.Emulator.Tests/       # Emulator core tests
│   ├── BadMango.Emulator.Bus.Tests/   # Bus and memory tests
│   ├── BadMango.Emulator.Devices.Tests/ # Device tests
│   └── ... (additional test projects)
│
├── wiki/                              # GitHub Wiki documentation
└── BackPocketBasic.slnx               # Solution file
```

### Technologies Used

- **.NET 10.0** - Runtime and SDK
- **Avalonia UI 11.2+** - Cross-platform GUI framework (Emulator UI)
- **Microsoft.Extensions.Hosting** - Application hosting model
- **Serilog** - Structured logging
- **Autofac** - Dependency injection
- **CommunityToolkit.Mvvm** - MVVM implementation (Emulator UI)
- **NUnit** - Unit testing framework
- **Moq** - Mocking framework

### Key Components

#### Advanced Emulator Framework

1. **CPU Core** (`BadMango.Emulator.Core`) - Abstract CPU definitions and interfaces
   - `Registers` - Universal register set supporting 8/16/32-bit modes
   - `ProcessorStatusFlags` - Status flag management
   - `OpcodeTable` - Instruction dispatch infrastructure
2. **CPU Implementations** (`BadMango.Emulator.Emulation`)
   - `Cpu65C02` - Full WDC 65C02 implementation
   - `Cpu65816` - Apple IIgs 65816 support (stub)
   - `Cpu65832` - Hypothetical 32-bit 65832 support (stub)
   - `AddressingModes` - Shared addressing mode implementations
   - `Instructions` - Organized by category (Arithmetic, Branch, Compare, etc.)
3. **System Bus** (`BadMango.Emulator.Bus`) - Memory management and device routing
   - `MainBus` - System bus with layered memory mapping
   - `PhysicalMemory` - RAM/ROM management
   - `DeviceRegistry` - Peripheral device registration
4. **Devices** (`BadMango.Emulator.Devices`) - Peripheral implementations
   - Keyboard, Speaker, Video Mode controllers
   - Disk II controller stubs
   - Thunderclock and PocketWatch cards
5. **UI** (`BadMango.Emulator.UI`) - Avalonia-based graphical interface

## Logging

Logs are written to:
- **Console**: Warnings and errors only
- **File**: `logs/backpocket-<date>.log` with full debug information

## Future Enhancements

- [ ] 65816 extended processor mode
- [ ] Disk image support

## Contributing

Contributions are welcome! Please see our [Contributing Guide](CONTRIBUTING.md) for details on:

- How to set up your development environment
- Coding standards and guidelines
- How to submit pull requests
- Reporting bugs and requesting features

### Quick Links

- **[Contributing Guide](CONTRIBUTING.md)** - Comprehensive contribution guidelines
- **[Setup Guide](SETUP_GUIDE.md)** - Repository configuration and branch protection setup
- **[Security Policy](.github/SECURITY.md)** - How to report security vulnerabilities

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

- Apple II and Applesoft BASIC are trademarks of Apple Inc.
- This is an educational project and is not affiliated with Apple Inc.
- Thanks to the retro computing community for preserving Apple II documentation
