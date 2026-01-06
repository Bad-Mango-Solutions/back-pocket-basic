# Welcome to the BackPocketBASIC Wiki

<p align="center">
  <img src="https://raw.githubusercontent.com/Bad-Mango-Solutions/back-pocket-basic/main/back-pocket-logo.png" alt="BackPocketBASIC Logo" width="200">
</p>

A fully-featured Applesoft BASIC interpreter and Apple II emulator framework written in .NET, featuring multi-CPU emulation (65C02/65816/65832) and a modular architecture.

![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)
![License](https://img.shields.io/badge/license-MIT-green)

## What is BackPocketBASIC?

BackPocketBASIC is a comprehensive project with two main components:

1. **Applesoft BASIC Interpreter** - A complete implementation of the BASIC programming language supplied with Apple II computers, allowing you to run vintage programs on modern hardware.

2. **Apple II Emulator Framework** - A modular, extensible emulator supporting multiple CPU variants (65C02, 65816, 65832) with accurate peripheral emulation.

## Key Features

### BASIC Interpreter
- **Complete Language Support**: Full implementation of Applesoft BASIC commands and syntax
- **Legacy 6502 Emulation**: Integrated 6502 CPU for PEEK, POKE, and CALL operations
- **Memory Space Emulation**: 64KB emulated memory matching the Apple II memory map
- **Custom Extensions**: Modern additions like the SLEEP command

### Emulator Framework
- **Multi-CPU Architecture**: Unified codebase supporting 65C02 (65816/65832 stubs available)
- **Modular Design**: Separate projects for CPU, bus, devices, and systems
- **Peripheral Support**: Keyboard, speaker, video modes, clock cards
- **Cross-Platform UI**: Avalonia-based GUI for machine management
- **Extensible**: Easy to add new devices and system configurations

## Getting Started

New to this project? Start here:

1. **[Installation](Installation)** - Get the prerequisites and build the project
2. **[Quick Start](Quick-Start)** - Run your first BASIC program
3. **[Language Reference](Language-Reference)** - Learn the Applesoft BASIC commands
4. **[Sample Programs](Sample-Programs)** - Explore example programs

## User Guide

- **[Language Reference](Language-Reference)** - Complete reference for all commands
- **[Built-in Functions](Built-in-Functions)** - Math, string, and utility functions
- **[Custom Extensions](Custom-Extensions)** - Modern additions like the SLEEP command
- **[Sample Programs](Sample-Programs)** - Guided walkthroughs

## Technical Documentation

For developers and those interested in the internals:

- **[Architecture Overview](Architecture-Overview)** - Project structure and components
- **[6502 Emulation](6502-Emulation)** - CPU emulation details and multi-CPU architecture
- **[Memory Map](Memory-Map)** - Detailed memory layout ($0000-$FFFF)
- **[API Reference](API-Reference)** - Integrating the interpreter library

## Contributing

Want to contribute? We'd love your help!

- **[Development Setup](Development-Setup)** - Set up your environment
- **[Testing Guide](Testing-Guide)** - Running and writing tests
- **[Code Style](Code-Style)** - Coding standards and guidelines

## Project Modules

| Module | Description |
|--------|-------------|
| `BadMango.Basic` | Applesoft BASIC interpreter with integrated 6502 |
| `BadMango.Basic.Console` | Command-line interface (bpbasic) |
| `BadMango.Emulator.Core` | CPU abstractions and interfaces |
| `BadMango.Emulator.Emulation` | 65C02 implementation (65816/65832 stubs) |
| `BadMango.Emulator.Bus` | System bus and memory management |
| `BadMango.Emulator.Devices` | Peripheral device implementations |
| `BadMango.Emulator.UI` | Avalonia-based graphical interface |

## Resources

- **[GitHub Repository](https://github.com/Bad-Mango-Solutions/back-pocket-basic)**
- **[Issue Tracker](https://github.com/Bad-Mango-Solutions/back-pocket-basic/issues)**
- **[Contributing Guide](https://github.com/Bad-Mango-Solutions/back-pocket-basic/blob/main/CONTRIBUTING.md)**

## About

This project is an educational endeavor to preserve and modernize Applesoft BASIC and Apple II emulation. Apple II and Applesoft BASIC are trademarks of Apple Inc. This project is not affiliated with Apple Inc.

**License**: MIT License - see the [LICENSE](https://github.com/Bad-Mango-Solutions/back-pocket-basic/blob/main/LICENSE) file for details.
