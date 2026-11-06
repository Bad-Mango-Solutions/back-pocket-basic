# Contributing to BackPocket Emulator

Thank you for your interest in contributing to BackPocket Emulator! This document provides guidelines and instructions for contributing.

## Table of Contents

- [Code of Conduct](#code-of-conduct)
- [How Can I Contribute?](#how-can-i-contribute)
- [Development Setup](#development-setup)
- [Project Structure](#project-structure)
- [Development Workflow](#development-workflow)
- [Coding Guidelines](#coding-guidelines)
- [Testing Guidelines](#testing-guidelines)
- [Submitting Changes](#submitting-changes)
- [Reporting Bugs](#reporting-bugs)
- [Suggesting Features](#suggesting-features)

## Code of Conduct

This project is committed to providing a welcoming and inclusive environment for all contributors. Please be respectful and constructive in all interactions.

## How Can I Contribute?

### Reporting Bugs
- Use the [Bug Report template](.github/ISSUE_TEMPLATE/bug_report.md)
- Search existing issues to avoid duplicates
- Include reproduction steps and environment details

### Suggesting Features
- Use the [Feature Request template](.github/ISSUE_TEMPLATE/feature_request.md)
- Explain the use case and benefits
- Consider WDC 65C02 hardware accuracy (and future 65816 compatibility)

### Improving Documentation
- Fix typos or unclear explanations
- Add examples or clarifications
- Update outdated information

### Contributing Code
- Fix bugs
- Implement new features
- Improve performance
- Add test coverage
- Refactor code

## Development Setup

### Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download) or later
- Git
- A code editor (Visual Studio, VS Code, Rider, etc.)

### Getting Started

1. **Fork the repository** on GitHub

2. **Clone your fork**:
   ```bash
   git clone https://github.com/YOUR_USERNAME/back-pocket-emulator.git
   cd back-pocket-emulator
   ```

3. **Add upstream remote**:
   ```bash
   git remote add upstream https://github.com/Bad-Mango-Solutions/back-pocket-emulator.git
   ```

4. **Restore dependencies**:
   ```bash
   dotnet restore BackPocketEmulator.slnx
   ```

5. **Build the solution**:
   ```bash
   dotnet build BackPocketEmulator.slnx
   ```

6. **Run tests**:
   ```bash
   dotnet test BackPocketEmulator.slnx
   ```

## Project Structure

`BackPocketEmulator.slnx` is the solution (.NET 10, NUnit, StyleCop). On disk, every project is a direct child of `src/` or `tests/`. The solution also groups those projects into virtual folders for the IDE (`/src/emulator/`, `/src/emulator/infrastructure/`, `/src/emulator/ui/`, and `/tests/`).

The Applesoft BASIC interpreter (`BadMango.Basic`, `BadMango.Basic.Console`, and `BadMango.Basic.Tests`) moved to [back-pocket-basic-legacy](https://github.com/Bad-Mango-Solutions/back-pocket-basic-legacy).

```
back-pocket-emulator/
├── BackPocketEmulator.slnx          # Solution
├── src/
│   ├── BadMango.Emulator.Core/                 # CPU contracts, registers, signals, machine profiles
│   ├── BadMango.Emulator.Bus/                  # Main bus, memory map, device registry, machine builder
│   ├── BadMango.Emulator.Emulation/            # 65C02 CPU; 65816 and 65832 placeholders
│   ├── BadMango.Emulator.Devices/              # Peripherals (video, keyboard, speaker, Disk II, cards)
│   ├── BadMango.Emulator.Storage/              # Disk images, block media, GCR nibblizing
│   ├── BadMango.Emulator.Rendering/            # Pixel buffers and character rendering
│   ├── BadMango.Emulator.Systems/              # Pocket2e machine composition
│   ├── BadMango.Emulator.Infrastructure/       # Event aggregator (no emulator dependencies)
│   ├── BadMango.Emulator.Configuration/        # App settings models and path validation
│   ├── BadMango.Emulator.UI.Abstractions/      # UI contracts without the Avalonia app
│   ├── BadMango.Emulator.UI/                   # Avalonia host, assembly name bpui
│   ├── BadMango.Emulator.TextEditor/           # Avalonia editor with syntax highlighting
│   ├── BadMango.Emulator.Debug.Infrastructure/ # REPL, commands, breakpoints, trace
│   ├── BadMango.Emulator.Debug.UI/             # Avalonia windows for the console debugger
│   ├── BadMango.Emulator.Debug/                # Console host, assembly name emudbg
│   └── BadMango.Emulator.Interop/              # Reserved for native and Rust interop
├── tests/
│   ├── BadMango.Unit.Components/                        # Shared NUnit and Serilog test helpers
│   ├── BadMango.Emulator.Tests/                         # CPU, machine, and cross-project tests
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
├── profiles/                          # Machine profile JSON
├── schemas/                           # machine-profile.schema.json
├── roms/                              # Apple II ROM images
├── disks/                             # Sample disk images
├── samples/                           # Sample machine-code programs
├── specs/                             # Architecture and device specifications
├── reference/                         # Design notes
├── references/                        # Additional reference notes
├── wiki/                              # Wiki markdown (synced to the GitHub wiki)
└── .github/                           # CI workflows, issue templates, Copilot instructions
```

Editor and analyzer settings live in `.editorconfig`, `StyleCop.json`, and `BackPocketEmulator.slnx.DotSettings`. Longer guides (`README.md`, this file, `SETUP_GUIDE.md`, and the implementation notes) sit at the repository root next to those folders. CI workflows are under `.github/workflows/`.

### What each project does

| Project | Role | Project references |
| --- | --- | --- |
| `BadMango.Emulator.Core` | CPU contracts and state (registers, opcode table, status flags), signal bus, machine-profile models and loader, debug-step listener contracts | none |
| `BadMango.Emulator.Infrastructure` | Event aggregator and Autofac registration. Safe for any component to reference | none |
| `BadMango.Emulator.Configuration` | Application settings, settings migration, and path validation | none |
| `BadMango.Emulator.Storage` | Storage backends, sector and block media, disk-image factory, GCR 6-and-2 nibblizer | none |
| `BadMango.Emulator.TextEditor` | Reusable Avalonia text editor (assembly source and other file types) | none |
| `BadMango.Emulator.Bus` | `MainBus`, layered memory mapping, I/O page dispatch, device registry, `Machine` and `MachineBuilder`, peripheral contracts | Core |
| `BadMango.Emulator.Emulation` | `Cpu65C02` and shared instruction and addressing-mode implementation. `Cpu65816` and `Cpu65832` are placeholders in the same project | Core, Bus |
| `BadMango.Emulator.Devices` | Keyboard, video, speaker, language card, 80-column card, game I/O, Disk II, Thunderclock, and PocketWatch | Bus, Storage |
| `BadMango.Emulator.Rendering` | Pixel buffers, character renderer, and Pocket2e video renderer | Devices |
| `BadMango.Emulator.Systems` | Pocket2e (Apple IIe-class 65C02) machine builder extensions | Bus, Core, Emulation, Devices |
| `BadMango.Emulator.Debug.Infrastructure` | Debug REPL, command modules, breakpoints, watchpoints, and tracing | Bus, Core, Devices, Emulation, Rendering, Storage, Systems |
| `BadMango.Emulator.Debug.UI` | Avalonia popup windows (video, glyph editor, status monitors) used by the console debugger. Library, not an executable | Bus, Debug.Infrastructure, Devices, Rendering, TextEditor |
| `BadMango.Emulator.Debug` | `emudbg` console host. Configures Serilog and starts the REPL | Core, Bus, Emulation, Debug.Infrastructure, Debug.UI |
| `BadMango.Emulator.UI.Abstractions` | Theme, settings, and pop-out contracts shared without taking a dependency on the GUI executable | Configuration, Infrastructure |
| `BadMango.Emulator.UI` | `bpui` Avalonia application (MVVM, Autofac, generic host). Configures Serilog at startup | Core, Infrastructure, UI.Abstractions |
| `BadMango.Emulator.Interop` | Leaf project reserved for native and Rust interop | Core |
| `BadMango.Unit.Components` | Test support only: `Generator` (including `Generator.Log()`), NUnit Serilog sink, performance-test helper. Every test project references it | none |
| `BadMango.Emulator.Tests` | CPU, disassembler, machine-builder, profile, device, storage, and debug-infrastructure tests. This is the test project for Emulation, Systems, and Core | Unit.Components, Bus, Core, Debug.Infrastructure, Devices, Emulation, Storage, Systems |
| `BadMango.Emulator.Bus.Tests` | Bus, memory map, and device-registry tests | Unit.Components, Bus |
| `BadMango.Emulator.Configuration.Tests` | Settings and path-validation tests | Unit.Components, Configuration |
| `BadMango.Emulator.Debug.Infrastructure.Tests` | REPL command tests | Unit.Components, Bus, Debug.Infrastructure, Storage |
| `BadMango.Emulator.Debug.Tests` | Console host module tests | Unit.Components, Debug |
| `BadMango.Emulator.Debug.UI.Tests` | Debug window and video-window tests | Unit.Components, Debug.Infrastructure, Debug.UI |
| `BadMango.Emulator.Devices.Tests` | Peripheral tests | Unit.Components, Core, Devices, Storage |
| `BadMango.Emulator.Infrastructure.Tests` | Event aggregator tests | Unit.Components, Infrastructure |
| `BadMango.Emulator.Rendering.Tests` | Pixel buffer and character renderer tests | Unit.Components, Rendering |
| `BadMango.Emulator.Storage.Tests` | Disk image and GCR tests | Unit.Components, Storage |
| `BadMango.Emulator.TextEditor.Tests` | Editor and syntax-highlighting tests | Unit.Components, Debug.Infrastructure, TextEditor |
| `BadMango.Emulator.UI.Tests` | Avalonia headless tests for the GUI host | Unit.Components, Configuration, Infrastructure, UI.Abstractions, UI |

`BadMango.Emulator.Emulation`, `BadMango.Emulator.Systems`, and `BadMango.Emulator.Core` are covered by `BadMango.Emulator.Tests`. `BadMango.Emulator.Interop` is the library project without a paired test project.

### How the projects relate

Machine construction runs from the contracts outward:

1. `Core` defines CPU state, signaling, and machine profiles. `Bus` implements the memory map and builds a `Machine` from a profile.
2. `Emulation` executes instructions against that bus. `Devices` plugs peripherals into the bus and reads media through `Storage`. `Rendering` turns device video state into pixels.
3. `Systems` is the Pocket2e recipe: it wires `Cpu65C02`, the bus, and the devices into one machine.
4. `Debug.Infrastructure` drives that machine from a REPL. `Debug.UI` and `TextEditor` add Avalonia windows. `Debug` is the executable that hosts both (`dotnet run --project src/BadMango.Emulator.Debug/BadMango.Emulator.Debug.csproj`).
5. `UI.Abstractions` and `UI` are the graphical host (`dotnet run --project src/BadMango.Emulator.UI/BadMango.Emulator.UI.csproj`). That host references `Core`, `Infrastructure`, and `UI.Abstractions`.
6. `Infrastructure` and `Configuration` sit beside the machine. The GUI abstractions use both. The machine projects do not.

Solution virtual folders, for orientation in an IDE:

- `/src/emulator/`: Bus, Devices, Interop, Systems
- `/src/emulator/infrastructure/`: Configuration, Core, Debug.Infrastructure, Emulation, Infrastructure, Rendering, Storage, TextEditor, UI.Abstractions
- `/src/emulator/ui/`: Debug, Debug.UI, UI
- `/tests/`: every test project, including `BadMango.Unit.Components`

## Development Workflow

### 1. Create a Feature Branch

```bash
# Ensure you're on main and up to date
git checkout main
git pull upstream main

# Create a new branch
git checkout -b feature/your-feature-name
# or
git checkout -b fix/bug-description
```

### 2. Make Your Changes

- Write clear, focused commits
- Follow coding guidelines
- Add or update tests
- Update documentation if needed

### 3. Test Your Changes

```bash
# Run all tests
dotnet test BackPocketEmulator.slnx --verbosity normal

# Build in Release mode
dotnet build BackPocketEmulator.slnx --configuration Release

# Test manually with sample programs
dotnet run --project src/BadMango.Emulator.Debug/BadMango.Emulator.Debug.csproj
```

### 4. Commit Your Changes

```bash
git add .
git commit -m "Brief description of changes"
```

**Commit Message Guidelines:**
- Use present tense ("Add feature" not "Added feature")
- Use imperative mood ("Move cursor to..." not "Moves cursor to...")
- First line should be 50 characters or less
- Reference issues and PRs when applicable

Examples:
```
Add 65C02 interrupt sampling after CLI

Fix off-by-one error in bus page mapping

Record a bus fault when a device read is unmapped

Fixes #123
```

### 5. Push to Your Fork

```bash
git push origin feature/your-feature-name
```

### 6. Create a Pull Request

1. Go to the [repository](https://github.com/Bad-Mango-Solutions/back-pocket-emulator)
2. Click "New Pull Request"
3. Select your fork and branch
4. Fill out the PR template
5. Submit the pull request

## Coding Guidelines

### C# Style

- Follow [Microsoft C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- Use meaningful variable and method names
- Keep methods focused and concise
- Add XML documentation comments for public APIs

Example:
```csharp
/// <summary>
/// Executes a single CPU instruction and returns the cycles consumed.
/// </summary>
/// <returns>The cycle count consumed by the instruction.</returns>
public int Step()
{
    // Implementation
}
```

### Exception Handling

- Never use a bare `catch` or `catch (Exception)`; catch only the specific exceptions the called code is documented or known to throw. Combine types that share handling with `catch (Exception ex) when (ex is A or B)`
- No empty catch blocks: log the exception (with the injected Serilog `ILogger`) or comment why ignoring it is correct. Prefer not catching at all
- Don't wrap calls in `try`/`catch` for exceptions they can't throw. APIs that report errors through return codes or error state (e.g. native interop such as OpenAL's `AL.GetError()`) must have that result checked instead
- Best-effort teardown (`Dispose`, `finally`) may continue past failures but should still log them
- When translating an exception, keep the original as the `InnerException`; rethrow unchanged with `throw;`
- Use guard helpers such as `ArgumentNullException.ThrowIfNull` instead of hand-written argument checks

### Naming Conventions

- Classes: `PascalCase`
- Methods: `PascalCase`
- Properties: `PascalCase`
- Private fields: `_camelCase` with underscore prefix
- Constants: `PascalCase` or `UPPER_CASE`
- Interfaces: `IPascalCase` with I prefix

## Testing Guidelines

### Unit Tests

- Tests live in `tests/`. Shared helpers are `tests/BadMango.Unit.Components/`. See [Project Structure](#project-structure) for which test project covers which library.
- Use NUnit framework
- Follow Arrange-Act-Assert pattern
- One logical assertion per test when possible

Example:
```csharp
[Test]
public void Cycle_Zero_HasValueZero()
{
    // Arrange
    var cycle = Cycle.Zero;

    // Act
    var value = cycle.Value;

    // Assert
    Assert.That(value, Is.EqualTo(0ul));
}
```

### Integration Tests

- Test complete machine-code programs and boot paths
- Verify end-to-end functionality
- Use sample programs for testing

### Test Coverage

- Aim for high test coverage on core components
- Test both success and error cases
- Include edge cases and boundary conditions

### Running Specific Tests

```bash
# Run tests in a specific class
dotnet test --filter FullyQualifiedName~InterruptAndHaltTests

# Run tests matching a name pattern
dotnet test --filter Name~Cycle

# Run one test project
dotnet test tests/BadMango.Emulator.Bus.Tests/BadMango.Emulator.Bus.Tests.csproj
```

## Submitting Changes

### Before Submitting

- [ ] All tests pass
- [ ] Code follows style guidelines
- [ ] Documentation is updated
- [ ] Commit messages are clear
- [ ] Branch is up to date with main

### Pull Request Process

1. **Fill out the PR template** completely
2. **Ensure CI checks pass** (build, tests)
3. **Request review** from maintainers
4. **Address feedback** promptly
5. **Keep PR updated** with main branch if needed

### Review Process

- Maintainers will review your PR
- Feedback may be provided through comments
- Changes may be requested
- Approved PRs will be merged by maintainers

### After Your PR is Merged

1. Delete your feature branch:
   ```bash
   git branch -d feature/your-feature-name
   git push origin --delete feature/your-feature-name
   ```

2. Update your main branch:
   ```bash
   git checkout main
   git pull upstream main
   git push origin main
   ```

## Reporting Bugs

### Before Reporting

- Search existing issues for duplicates
- Verify the bug exists in the latest version
- Collect reproduction information

### Creating a Bug Report

Use the bug report template and include:

- Clear description of the bug
- Steps to reproduce
- Expected vs. actual behavior
- Machine code, disk image, or debugger commands that trigger the bug
- Environment details (OS, .NET version)
- Error messages or stack traces

## Suggesting Features

### Before Suggesting

- Search existing issues and PRs
- Consider if it fits the project scope
- Think about implementation complexity

### Creating a Feature Request

Use the feature request template and include:

- Clear description of the feature
- Use case or problem it solves
- Proposed solution
- Examples of how it would be used
- 65C02 hardware-behavior notes

## Additional Resources

### Documentation

- [README.md](README.md) - Project overview and usage
- [SETUP_GUIDE.md](SETUP_GUIDE.md) - Repository setup instructions
- [BRANCH_MIGRATION.md](BRANCH_MIGRATION.md) - Branch synchronization
- [SECURITY.md](.github/SECURITY.md) - Security policy

### Apple II / 6502 References

- [Applesoft BASIC (legacy interpreter)](https://github.com/Bad-Mango-Solutions/back-pocket-basic-legacy)
- [Apple II Documentation Project](https://www.apple2.org/)
- [6502 Instruction Reference](http://www.6502.org/tutorials/6502opcodes.html)

### .NET Resources

- [.NET Documentation](https://docs.microsoft.com/en-us/dotnet/)
- [C# Language Reference](https://docs.microsoft.com/en-us/dotnet/csharp/)

## Questions?

If you have questions about contributing:
- Open an issue with the question
- Check existing documentation
- Review closed issues and PRs for similar discussions

## Thank You!

Your contributions help make this project better. Whether you're fixing a typo, reporting a bug, or implementing a new feature, your effort is appreciated!

---

**Happy Coding!** 🚀
