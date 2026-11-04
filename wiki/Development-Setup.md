# Development Setup

Complete guide for setting up your development environment to contribute to BackPocket Emulator.

## Overview

This guide covers everything you need to start developing on the project. For general contribution guidelines, see [CONTRIBUTING.md](https://github.com/Bad-Mango-Solutions/back-pocket-emulator/blob/main/CONTRIBUTING.md).

## Prerequisites

### Required Software

1. **[.NET 10.0 SDK](https://dotnet.microsoft.com/download)** or later
2. **Git** - [Download](https://git-scm.com/downloads)
3. **Code Editor** (choose one):
   - [Visual Studio 2022](https://visualstudio.microsoft.com/) (Windows/Mac)
   - [Visual Studio Code](https://code.visualstudio.com/) (All platforms)
   - [JetBrains Rider](https://www.jetbrains.com/rider/) (All platforms)

### Optional Tools

- **GitHub CLI** (`gh`) - [Download](https://cli.github.com/)
- **.NET Global Tools**:
  ```bash
  dotnet tool install -g dotnet-format
  dotnet tool install -g dotnet-coverage
  ```

## Initial Setup

### 1. Fork the Repository

1. Visit [https://github.com/Bad-Mango-Solutions/back-pocket-emulator](https://github.com/Bad-Mango-Solutions/back-pocket-emulator)
2. Click **Fork** button (top-right)
3. Create fork in your GitHub account

### 2. Clone Your Fork

```bash
# Clone your fork
git clone https://github.com/YOUR_USERNAME/back-pocket-emulator.git
cd back-pocket-emulator

# Add upstream remote
git remote add upstream https://github.com/Bad-Mango-Solutions/back-pocket-emulator.git

# Verify remotes
git remote -v
```

**Expected output:**
```
origin    https://github.com/YOUR_USERNAME/back-pocket-emulator.git (fetch)
origin    https://github.com/YOUR_USERNAME/back-pocket-emulator.git (push)
upstream  https://github.com/Bad-Mango-Solutions/back-pocket-emulator.git (fetch)
upstream  https://github.com/Bad-Mango-Solutions/back-pocket-emulator.git (push)
```

### 3. Restore Dependencies

```bash
dotnet restore BackPocketEmulator.slnx
```

### 4. Build the Solution

```bash
dotnet build BackPocketEmulator.slnx
```

**Expected:**
```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

### 5. Run Tests

```bash
dotnet test BackPocketEmulator.slnx --verbosity normal
```

The run should finish with no failed tests.

### 6. Verify Installation

```bash
dotnet run --project src/BadMango.Emulator.Debug/BadMango.Emulator.Debug.csproj
```

That starts the `emudbg` console debugger.

## IDE Configuration

### Visual Studio 2022

#### Setup

1. Open `BackPocketEmulator.slnx`
2. Visual Studio will restore NuGet packages automatically
3. Build: **Ctrl+Shift+B**
4. Run tests: **Test → Run All Tests**

#### Recommended Extensions

- **ReSharper** (optional, paid) - Enhanced C# tools
- **CodeMaid** - Code cleanup utilities
- **GitHub Extension** - GitHub integration

#### Settings

**EditorConfig:**
Project includes `.editorconfig` - Visual Studio will automatically apply settings.

**Code Analysis:**
- Enable: **Tools → Options → Text Editor → C# → Code Style**
- Use built-in analyzers

### Visual Studio Code

#### Setup

1. Install **C# Extension** by Microsoft
2. Open folder: `File → Open Folder` → Select `applesoft-basic`
3. Build: **Ctrl+Shift+B** or **Terminal → Run Build Task**
4. Run tests: Use terminal commands

#### Recommended Extensions

- **C#** (ms-dotnettools.csharp) - Language support
- **C# Dev Kit** (ms-dotnettools.csdevkit) - Enhanced features
- **.NET Core Test Explorer** - Test running
- **EditorConfig** - Code style
- **GitLens** - Enhanced Git integration

#### Settings (.vscode/settings.json)

```json
{
  "omnisharp.enableRoslynAnalyzers": true,
  "omnisharp.enableEditorConfigSupport": true,
  "editor.formatOnSave": true,
  "files.trimTrailingWhitespace": true
}
```

#### Tasks (.vscode/tasks.json)

```json
{
  "version": "2.0.0",
  "tasks": [
    {
      "label": "build",
      "command": "dotnet",
      "type": "process",
      "args": ["build", "BackPocketEmulator.slnx"],
      "problemMatcher": "$msCompile"
    },
    {
      "label": "test",
      "command": "dotnet",
      "type": "process",
      "args": ["test", "BackPocketEmulator.slnx"],
      "problemMatcher": "$msCompile"
    }
  ]
}
```

### JetBrains Rider

#### Setup

1. Open `BackPocketEmulator.slnx`
2. Rider will restore packages automatically
3. Build: **Ctrl+F9** (Windows/Linux) or **Cmd+F9** (Mac)
4. Run tests: **Alt+Shift+T** or use Test Explorer

#### Settings

- **Settings → Editor → Code Style**: Uses .editorconfig automatically
- **Settings → Editor → Inspections**: Enable all C# inspections

## Project Structure

Every project in `BackPocketEmulator.slnx` is listed in [CONTRIBUTING.md](https://github.com/Bad-Mango-Solutions/back-pocket-emulator/blob/main/CONTRIBUTING.md#project-structure). `BadMango.Basic`, `BadMango.Basic.Console`, and `BadMango.Basic.Tests` moved to [back-pocket-basic-legacy](https://github.com/Bad-Mango-Solutions/back-pocket-basic-legacy).

## Development Workflow

### Creating a Feature Branch

```bash
# Update main branch
git checkout main
git pull upstream main

# Create feature branch
git checkout -b feature/your-feature-name
```

### Making Changes

1. **Write code** following [Code Style](Code-Style)
2. **Add/update tests** - See [Testing Guide](Testing-Guide)
3. **Build** to check for errors:
   ```bash
   dotnet build BackPocketEmulator.slnx
   ```
4. **Run tests** to verify:
   ```bash
   dotnet test BackPocketEmulator.slnx
   ```

### Committing Changes

```bash
# Stage changes
git add .

# Commit with descriptive message
git commit -m "Add support for XYZ feature"

# Push to your fork
git push origin feature/your-feature-name
```

### Creating a Pull Request

1. Go to your fork on GitHub
2. Click **Pull Request**
3. Select base: `Bad-Mango-Solutions/back-pocket-emulator` main
4. Select compare: `your-fork` feature/your-feature-name
5. Fill out PR template
6. Submit pull request

## Building and Testing

### Build Commands

```bash
# Debug build
dotnet build BackPocketEmulator.slnx

# Release build
dotnet build BackPocketEmulator.slnx --configuration Release

# Clean build
dotnet clean BackPocketEmulator.slnx
dotnet build BackPocketEmulator.slnx

# Build specific project
dotnet build src/BadMango.Emulator.Core/BadMango.Emulator.Core.csproj
```

### Test Commands

```bash
# Run all tests
dotnet test BackPocketEmulator.slnx

# Run with detailed output
dotnet test BackPocketEmulator.slnx --verbosity detailed

# Run specific test class
dotnet test --filter FullyQualifiedName~InterruptAndHaltTests

# Run tests matching a name pattern
dotnet test --filter Name~Cycle

# Run tests with coverage (if dotnet-coverage installed)
dotnet test BackPocketEmulator.slnx --collect:"XPlat Code Coverage"
```

### Running the Console App

```bash
# Run with dotnet run
dotnet run --project src/BadMango.Emulator.Debug/BadMango.Emulator.Debug.csproj

# Or run the built executable
./src/BadMango.Emulator.Debug/bin/Debug/net10.0/emudbg
```

## Code Quality Tools

### Formatting

```bash
# Format code
dotnet format BackPocketEmulator.slnx

# Check formatting without changes
dotnet format BackPocketEmulator.slnx --verify-no-changes
```

### Static Analysis

Built-in analyzers run during build. Check **Error List** in IDE.

### EditorConfig

Project includes `.editorconfig` with code style rules:
- Indentation: 4 spaces
- Line endings: LF
- Trailing whitespace: removed
- Final newline: required

## Debugging

### Console Application

**Visual Studio:**
1. Set `BadMango.Emulator.Debug` as startup project
2. Right-click → **Properties → Debug**
3. Press **F5** to debug

**VS Code:**
1. Create `.vscode/launch.json`:
```json
{
  "version": "0.2.0",
  "configurations": [
    {
      "name": "Debug Console App",
      "type": "coreclr",
      "request": "launch",
      "program": "${workspaceFolder}/src/BadMango.Emulator.Debug/bin/Debug/net10.0/emudbg.dll",
      "args": [],
      "cwd": "${workspaceFolder}",
      "stopAtEntry": false
    }
  ]
}
```
2. Press **F5**

### Tests

**Visual Studio:**
- **Test → Windows → Test Explorer**
- Right-click test → **Debug**

**VS Code:**
- Use .NET Core Test Explorer extension
- Click debug icon next to test

**Rider:**
- Right-click test → **Debug**

## Troubleshooting

### Build Errors

**"SDK not found":**
```bash
# Check .NET version
dotnet --version

# Should be 10.0 or higher
```

**"Unable to restore packages":**
```bash
# Clear NuGet cache
dotnet nuget locals all --clear
dotnet restore BackPocketEmulator.slnx
```

### Test Failures

**Tests pass locally but fail in CI:**
- Check line endings (should be LF, not CRLF)
- Verify tests don't depend on local environment

### IDE Issues

**Intellisense not working (VS Code):**
```bash
# Restart OmniSharp
Ctrl+Shift+P → "OmniSharp: Restart OmniSharp"
```

**Solution won't load (Visual Studio):**
- Close Visual Studio
- Delete `.vs` folder
- Delete `bin` and `obj` folders
- Reopen solution

## Keeping Your Fork Updated

```bash
# Fetch upstream changes
git fetch upstream

# Update your main branch
git checkout main
git merge upstream/main
git push origin main

# Rebase feature branch on latest main (if needed)
git checkout feature/your-feature
git rebase main
```

## Additional Resources

### Documentation

- [CONTRIBUTING.md](https://github.com/Bad-Mango-Solutions/back-pocket-emulator/blob/main/CONTRIBUTING.md) - Contribution guidelines
- [SETUP_GUIDE.md](https://github.com/Bad-Mango-Solutions/back-pocket-emulator/blob/main/SETUP_GUIDE.md) - Repository setup
- [Testing Guide](Testing-Guide) - Writing tests
- [Code Style](Code-Style) - Coding standards

### External Resources

- [.NET Documentation](https://docs.microsoft.com/en-us/dotnet/)
- [C# Programming Guide](https://docs.microsoft.com/en-us/dotnet/csharp/)
- [NUnit Documentation](https://docs.nunit.org/)
- [Moq Documentation](https://github.com/moq/moq4/wiki/Quickstart)

## Getting Help

- **Questions?** Open a [Discussion](https://github.com/Bad-Mango-Solutions/back-pocket-emulator/discussions)
- **Found a bug?** Open an [Issue](https://github.com/Bad-Mango-Solutions/back-pocket-emulator/issues)
- **Need clarification?** Comment on the relevant issue or PR

## Next Steps

1. ✅ Complete this setup guide
2. 📖 Read [Code Style](Code-Style)
3. 🧪 Review [Testing Guide](Testing-Guide)
4. 💡 Pick an issue to work on
5. 🚀 Submit your first PR!

---

**Happy Coding!** 🎉
