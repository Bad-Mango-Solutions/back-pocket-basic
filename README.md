# BackPocket Emulator

<p align="center">
  <img src="back-pocket-logo.png" alt="BackPocket Emulator Logo" width="200">
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
git clone https://github.com/Bad-Mango-Solutions/back-pocket-emulator.git
cd back-pocket-emulator

# Build the solution
dotnet build BackPocketEmulator.slnx

# Run tests
dotnet test BackPocketEmulator.slnx

# Run tests with statement coverage
dotnet test BackPocketEmulator.slnx --collect "XPlat Code Coverage"
```

## Architecture

### Project Structure

```
back-pocket-emulator/
├── src/
│   ├── BadMango.Emulator.Core/                 # CPU contracts, registers, signals, machine profiles
│   ├── BadMango.Emulator.Bus/                  # Main bus, memory map, device registry, machine builder
│   ├── BadMango.Emulator.Emulation/            # 65C02 CPU; 65816 and 65832 placeholders
│   │   └── Cpu/
│   ├── BadMango.Emulator.Devices/              # Peripherals (video, keyboard, speaker, Disk II, cards)
│   ├── BadMango.Emulator.Storage/              # Disk images, block media, GCR nibblizing
│   ├── BadMango.Emulator.Rendering/            # Pixel buffers and character rendering
│   ├── BadMango.Emulator.Systems/              # Pocket2e machine composition
│   ├── BadMango.Emulator.Infrastructure/       # Event aggregator
│   ├── BadMango.Emulator.Configuration/        # App settings and path validation
│   ├── BadMango.Emulator.UI.Abstractions/      # UI contracts
│   ├── BadMango.Emulator.UI/                   # Avalonia host (bpui)
│   ├── BadMango.Emulator.TextEditor/           # Avalonia editor
│   ├── BadMango.Emulator.Debug.Infrastructure/ # REPL, commands, breakpoints, trace
│   ├── BadMango.Emulator.Debug.UI/             # Avalonia windows for the console debugger
│   ├── BadMango.Emulator.Debug/                # Console host (emudbg)
│   └── BadMango.Emulator.Interop/              # Reserved for native and Rust interop
│
├── tests/
│   ├── BadMango.Unit.Components/               # Shared test helpers
│   ├── BadMango.Emulator.Tests/                # CPU, machine, and cross-project tests
│   ├── BadMango.Emulator.Bus.Tests/
│   ├── BadMango.Emulator.Configuration.Tests/
│   ├── BadMango.Emulator.Debug.Infrastructure.Tests/
│   ├── BadMango.Emulator.Debug.Tests/
│   ├── BadMango.Emulator.Debug.UI.Tests/
│   ├── BadMango.Emulator.Devices.Tests/
│   ├── BadMango.Emulator.Infrastructure.Tests/
│   ├── BadMango.Emulator.Rendering.Tests/
│   ├── BadMango.Emulator.Storage.Tests/
│   ├── BadMango.Emulator.TextEditor.Tests/
│   └── BadMango.Emulator.UI.Tests/
│
├── profiles/                          # Machine profile JSON
├── schemas/                           # machine-profile.schema.json
├── roms/                              # Apple II ROM images
├── disks/                             # Sample disk images
├── samples/                           # Sample machine-code programs
├── specs/                             # Architecture and device specifications
├── reference/                         # Design notes
├── references/                        # Additional reference notes
├── wiki/                              # GitHub Wiki documentation
└── BackPocketEmulator.slnx            # Solution file
```

How these projects reference each other is described in [CONTRIBUTING.md](CONTRIBUTING.md#project-structure).

### Technologies Used

- **.NET 10.0** - Runtime and SDK
- **Avalonia UI 12** - Cross-platform GUI framework (Emulator UI)
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

Third-party components distributed with the application, and their licenses, are listed in [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).

## Acknowledgments

- Apple II and Applesoft BASIC are trademarks of Apple Inc.
- This is an educational project and is not affiliated with Apple Inc.
- Thanks to the retro computing community for preserving Apple II documentation
