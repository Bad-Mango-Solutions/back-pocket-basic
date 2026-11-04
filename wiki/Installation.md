# Installation

This guide builds BackPocket Emulator (`BackPocketEmulator.slnx`) on your system.

## Prerequisites

### Required Software

- **[.NET 10.0 SDK](https://dotnet.microsoft.com/download)** or later
- **Git** (for cloning the repository)

### Supported Platforms

The emulator runs on any platform that supports .NET 10.0:

- **Windows** 10/11 (x64, ARM64)
- **macOS** 10.15+ (x64, ARM64/Apple Silicon)
- **Linux** (various distributions, x64, ARM64)

## Installation Steps

### 1. Clone the Repository

```bash
git clone https://github.com/Bad-Mango-Solutions/back-pocket-emulator.git
cd back-pocket-emulator
```

### 2. Restore Dependencies

```bash
dotnet restore BackPocketEmulator.slnx
```

This will download all required NuGet packages:
- Microsoft.Extensions.Hosting
- Serilog
- Autofac
- NUnit (for tests)
- Moq (for tests)

### 3. Build the Solution

```bash
dotnet build BackPocketEmulator.slnx
```

**Expected Output:**
```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

### 4. Run Tests (Optional)

Verify the installation by running the test suite:

```bash
dotnet test BackPocketEmulator.slnx
```

The run should finish with no failed tests.

### 5. Build Release Version (Optional)

For better performance, build the release version:

```bash
dotnet build BackPocketEmulator.slnx --configuration Release
```

## Verifying Installation

Start the console debugger:

```bash
dotnet run --project src/BadMango.Emulator.Debug/BadMango.Emulator.Debug.csproj
```

That command starts the `emudbg` console debugger.

## Directory Structure

`src/` and `tests/` hold one directory per project in `BackPocketEmulator.slnx`. The full list, including `profiles/`, `schemas/`, `roms/`, `disks/`, `samples/`, `specs/`, and `wiki/`, is in [CONTRIBUTING.md](https://github.com/Bad-Mango-Solutions/back-pocket-emulator/blob/main/CONTRIBUTING.md#project-structure).

## Building Individual Projects

You can also build individual projects:

```bash
# Build the CPU contracts library
dotnet build src/BadMango.Emulator.Core/BadMango.Emulator.Core.csproj

# Build the console debugger
dotnet build src/BadMango.Emulator.Debug/BadMango.Emulator.Debug.csproj

# Build and run the console debugger
dotnet run --project src/BadMango.Emulator.Debug/BadMango.Emulator.Debug.csproj
```

## IDE Support

### Visual Studio

1. Open `BackPocketEmulator.slnx` in Visual Studio 2022 or later
2. Ensure .NET 10.0 SDK is installed
3. Build the solution (Ctrl+Shift+B)
4. Run tests from Test Explorer

### Visual Studio Code

1. Install the C# extension
2. Open the `applesoft-basic` folder
3. Use `Ctrl+Shift+B` to build
4. Run tests with `dotnet test` in the terminal

### JetBrains Rider

1. Open `BackPocketEmulator.slnx` in Rider
2. Rider will automatically restore dependencies
3. Build and run using the UI or Ctrl+F9

## Troubleshooting

### "SDK not found" Error

**Problem**: `The command could not be loaded, possibly because: You intended to execute a .NET application...`

**Solution**: Install the .NET 10.0 SDK from https://dotnet.microsoft.com/download

### Build Warnings About Console.Beep

**Problem**: Warning about `Console.Beep` not being supported on all platforms

**Solution**: This is expected and can be ignored. The code includes platform checks to handle this gracefully.

### "Project file does not exist" Error

**Problem**: `The project file does not exist`

**Solution**: Ensure you're in the correct directory. Run `pwd` (Unix) or `cd` (Windows) to check your location.

### NuGet Package Restore Fails

**Problem**: Cannot restore NuGet packages

**Solution**:
```bash
# Clear NuGet cache
dotnet nuget locals all --clear

# Retry restore
dotnet restore BackPocketEmulator.slnx
```

### Tests Fail

**Problem**: Some tests fail during `dotnet test`

**Solution**: This may indicate an environment issue. Check:
- Correct .NET version is installed (`dotnet --version`)
- All dependencies were restored
- Try a clean build: `dotnet clean && dotnet build`

## Next Steps

Now that you have the interpreter installed:

1. **[Quick Start](Quick-Start)** - Learn how to run BASIC programs
2. **[Language Reference](Language-Reference)** - Explore Applesoft BASIC commands
3. **[Sample Programs](Sample-Programs)** - Try the included examples

## Uninstalling

To remove the interpreter, simply delete the `applesoft-basic` directory:

```bash
rm -rf applesoft-basic
```

## Getting Help

If you encounter issues:

1. Check the [Troubleshooting](#troubleshooting) section above
2. Review [closed issues](https://github.com/Bad-Mango-Solutions/back-pocket-emulator/issues?q=is%3Aissue+is%3Aclosed) on GitHub
3. Open a [new issue](https://github.com/Bad-Mango-Solutions/back-pocket-emulator/issues/new) with details about your problem
