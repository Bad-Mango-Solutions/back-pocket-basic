# BackPocket Emulator

<p align="center">
  <img src="https://raw.githubusercontent.com/Bad-Mango-Solutions/back-pocket-emulator/main/back-pocket-logo.png" alt="BackPocket Emulator Logo" width="200">
</p>

A .NET emulator for the WDC 65C02 (Apple IIe-class Pocket2e machine), built to grow into the 65816 and a theoretical 32-bit 65832.

![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)
![License](https://img.shields.io/badge/license-MIT-green)

The Applesoft BASIC interpreter (`BadMango.Basic`, `BadMango.Basic.Console`, and `BadMango.Basic.Tests`) moved to [back-pocket-basic-legacy](https://github.com/Bad-Mango-Solutions/back-pocket-basic-legacy). Language, sample, and extension pages still in this wiki describe that interpreter.

## Getting started

1. **[Installation](Installation)** - Clone this repository and build `BackPocketEmulator.slnx`
2. **[Development Setup](Development-Setup)** - Fork, restore, and run tests
3. **[Contributing Guide](https://github.com/Bad-Mango-Solutions/back-pocket-emulator/blob/main/CONTRIBUTING.md)** - Project structure and contribution workflow

## Emulator documentation

- **[Architecture Overview](Architecture-Overview)** - Where the old interpreter writeup stopped, and where the current layout lives
- **[6502 Emulation](6502-Emulation)** - CPU notes, including the current `BadMango.Emulator` projects
- **[Memory Map](Memory-Map)** - Apple II memory layout
- **[Testing Guide](Testing-Guide)** - NUnit layout for this repository

## Projects

Every project in `BackPocketEmulator.slnx` is listed in the [project structure section of CONTRIBUTING.md](https://github.com/Bad-Mango-Solutions/back-pocket-emulator/blob/main/CONTRIBUTING.md#project-structure). The two hosts are:

| Project | Assembly | Role |
| --- | --- | --- |
| `BadMango.Emulator.Debug` | `emudbg` | Console debugger |
| `BadMango.Emulator.UI` | `bpui` | Avalonia GUI |

## Resources

- **[GitHub Repository](https://github.com/Bad-Mango-Solutions/back-pocket-emulator)**
- **[Issue Tracker](https://github.com/Bad-Mango-Solutions/back-pocket-emulator/issues)**
- **[Applesoft BASIC interpreter](https://github.com/Bad-Mango-Solutions/back-pocket-basic-legacy)**

**License**: MIT. See [LICENSE](https://github.com/Bad-Mango-Solutions/back-pocket-emulator/blob/main/LICENSE).
