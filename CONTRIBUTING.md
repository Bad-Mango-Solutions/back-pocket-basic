# Contributing to BackPocketBASIC

Thank you for your interest in contributing to BackPocketBASIC! This document provides guidelines and instructions for contributing.

## Table of Contents

- [Code of Conduct](#code-of-conduct)
- [How Can I Contribute?](#how-can-i-contribute)
- [Development Setup](#development-setup)
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
- Consider Applesoft BASIC compatibility

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
   git clone https://github.com/YOUR_USERNAME/back-pocket-basic.git
   cd back-pocket-basic
   ```

3. **Add upstream remote**:
   ```bash
   git remote add upstream https://github.com/Bad-Mango-Solutions/back-pocket-basic.git
   ```

4. **Restore dependencies**:
   ```bash
   dotnet restore BackPocketBasic.slnx
   ```

5. **Build the solution**:
   ```bash
   dotnet build BackPocketBasic.slnx
   ```

6. **Run tests**:
   ```bash
   dotnet test BackPocketBasic.slnx
   ```

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
dotnet test BackPocketBasic.slnx --verbosity normal

# Build in Release mode
dotnet build BackPocketBasic.slnx --configuration Release

# Test manually with sample programs
dotnet run --project src/BadMango.Basic.Console/BadMango.Basic.Console.csproj -- samples/demo.bas
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
Add support for FLASH command in text mode

Fix off-by-one error in array bounds checking

Improve performance of lexer tokenization

Fixes #123
```

### 5. Push to Your Fork

```bash
git push origin feature/your-feature-name
```

### 6. Create a Pull Request

1. Go to the [repository](https://github.com/Bad-Mango-Solutions/back-pocket-basic)
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
/// Evaluates a BASIC expression and returns the result.
/// </summary>
/// <param name="expression">The expression to evaluate.</param>
/// <returns>The numeric or string result of the expression.</returns>
public object EvaluateExpression(Expression expression)
{
    // Implementation
}
```

### Project Structure

- **AST/**: Abstract Syntax Tree node definitions
- **Emulation/**: 6502 CPU and Apple II emulation
- **Execution/**: Interpreter implementation
- **Lexer/**: Tokenization logic
- **Parser/**: Parsing logic
- **Runtime/**: Runtime environment and state

### Naming Conventions

- Classes: `PascalCase`
- Methods: `PascalCase`
- Properties: `PascalCase`
- Private fields: `_camelCase` with underscore prefix
- Constants: `PascalCase` or `UPPER_CASE`
- Interfaces: `IPascalCase` with I prefix

## Testing Guidelines

### Unit Tests

- Tests are in `tests/BadMango.Basic.Tests/`
- Use NUnit framework
- Follow Arrange-Act-Assert pattern
- One logical assertion per test when possible

Example:
```csharp
[Test]
public void Print_Statement_Outputs_Text()
{
    // Arrange
    var program = "10 PRINT \"HELLO\"";
    var interpreter = CreateInterpreter();
    
    // Act
    interpreter.Run(program);
    var output = GetOutput();
    
    // Assert
    Assert.That(output, Is.EqualTo("HELLO\n"));
}
```

### Integration Tests

- Test complete BASIC programs
- Verify end-to-end functionality
- Use sample programs for testing

### Test Coverage

- Aim for high test coverage on core components
- Test both success and error cases
- Include edge cases and boundary conditions

### Running Specific Tests

```bash
# Run tests in a specific class
dotnet test --filter FullyQualifiedName~BasicInterpreterTests

# Run tests matching a name pattern
dotnet test --filter Name~Print

# Run tests in a category
dotnet test --filter Category=Parser
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
- BASIC code that triggers the bug
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
- Applesoft BASIC compatibility notes

## Additional Resources

### Documentation

- [README.md](README.md) - Project overview and usage
- [SETUP_GUIDE.md](SETUP_GUIDE.md) - Repository setup instructions
- [BRANCH_MIGRATION.md](BRANCH_MIGRATION.md) - Branch synchronization
- [SECURITY.md](.github/SECURITY.md) - Security policy

### Applesoft BASIC References

- [Applesoft BASIC Quick Reference](http://www.landsnail.com/a2ref.htm)
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
