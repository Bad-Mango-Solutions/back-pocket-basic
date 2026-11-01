# GitHub Repository Copilot Instructions

## Project Overview

This repository contains code to emulate an Applesoft BASIC interpreter, enhancing its capabilities and performance. The project is structured to facilitate contributions, maintainability, and adherence to coding standards.

### Target Framework

**This project uses .NET 10.0** as its target framework. All new code and projects must target `net10.0`. Do not question or suggest changes to the framework version - .NET 10.0 is the established standard for this repository as documented in the README and all project files.

### 65xxx Emulation

- The core of the project is a cycle-accurate emulator for the 65C02 and 65816 microprocessors, which are central to the Apple II architecture.
- It must accurately model the memory management, instruction set, and timing of these processors to ensure compatibility with existing software and to enable detailed diagnostics.
- It must also accurately model I/O devices such as the keyboard, video output, audio output, and disk drives to provide a complete emulation environment.
- The future speculative 65832 emulator will build on this foundation while offering capabilities expected of a 32-bit successor, including memory management, protected memory regions, execution privilege (kernel vs. user), hypervisor support, and an expanded instruction set.
- In order to reach this goal, we will have to continually improve the performance of the core emulation loop, optimize memory access patterns, and ensure that our implementation is efficient enough to run in real time on modern hardware.

### New OS Development

- Beginning with the 65C02-based Apple II, we will develop a new operating system that runs on the emulator. This OS will be designed to be compatible with existing Apple II software while also introducing new features and capabilities.
- We will use this OS as a testbed for the emulator's accuracy and performance, as well as a platform for exploring new ideas in OS design and user experience.
- The OS will be developed in parallel with the emulator, with a focus on iterative development and continuous integration to ensure that both components evolve together and remain compatible.
- Further, we will expand its functionality as we introduce the 65816 emulator and eventually our speculative 32-bit "65832" processor, ensuring that it can take advantage of new capabilities while maintaining backward compatibility.

### Debug Console

- The existing `emudbg` debug console is designed for interactive human use.
- It exposes a REPL interface for issuing commands and inspecting emulator state in real time.
- It also exposes the local keyboard, video, and audio devices for direct interaction with the emulated machine.
- It will include debugging tools such as register/memory inspection, breakpoints, watchpoints, tracing, and disk state management.
- The ultimate goal is to reuse this infrastructure for a user-friendly application that can host multiple instances of an emulator.
- We will use this debug console as we move towards the 65816 emulator and, eventually, our speculative 32-bit "65832" processor, so it must be designed with future extensibility in mind.
- **For AI agents and tools**: See `AGENTS.md` (root) for the exact recommended patterns to launch and drive the REPL (batching via stdin piping, core commands, limitations, and examples). Always test changes with realistic command sequences.

### Emulator Host Agent (emudbg-agent)

- The `emudbg-agent` is a new host mode for `emudbg` that allows an LLM agent to control the emulator as a local tool.
- It will reuse the existing debug infrastructure and command surface while exposing a machine-friendly transport suitable for local tooling integrations such as MCP clients.
- Use headless Avalonia for video output when implementing the emudbg agent host.
- Use a fake speaker controller for audio in the emudbg agent host on Linux/Ubuntu diagnostic hosts where Windows-only implementations are unavailable.
- Practical interaction guidance for the current REPL (while the dedicated agent host is being built) lives in `AGENTS.md`.

## Development Guidelines

### Code Standards

- **Do not suppress warnings.** Disabling a warning is not the same as fixing it. Resolve the underlying issue or provide the required documentation instead of turning analyzers off.
- **XML documentation completeness.** Write well-formed XML docs that include summaries plus documentation of parameters, type parameters, and return values when applicable.
- **Use inheritdoc when appropriate.** If a class or member implements an already documented interface or inherited member, prefer `<inheritdoc cref="FullyQualifiedMember" />` to avoid duplication while keeping documentation intact.
- **StyleCop compliance.** Follow repository code-style rules (including newline expectations) to keep StyleCop analyzers clean without suppressions.
- **Unit tests required.** Every new feature or bug fix must include unit tests that cover the relevant code paths. Ensure tests are comprehensive and validate expected behavior.
- **Adhere to SOLID principles.** Design classes and modules following SOLID principles to ensure a clean and maintainable codebase.
- **Code formatting.** Maintain consistent code formatting as per the project's style guidelines to enhance readability and collaboration.
- **Important:** SA1518 must not be suppressed. Always fix the underlying issue rather than suppressing this warning. **There must not be any newlines at the end of a .cs file; it must *always* end with `}`, `]` (in AssemblyInfo.cs), or `;` (in GlobalUsings.cs).**
  - Grammatical Note: In the warning text, "File may not end with a newline character", "may not" means "must not". It is a prohibition, not a permission.
  - Additional note: This rule only applies to C# (`.cs`) files. Only C# files are checked for this rule.
- **Important:** SA1600 and related XML doc warnings must not be suppressed. Always provide XML documentation for all public members and types to ensure clarity and maintainability.
- **Important:** Just because the above warnings must not be suppressed does not mean that *any* warning can be suppressed. Use judgment and strive to write clean, warning-free code.

### Coding Best Practices
- **Consistent naming conventions.** Use clear and consistent naming for variables, methods, classes, and other identifiers to enhance code readability.
- **Modular design.** Structure code into small, reusable modules or functions to promote maintainability and ease of testing.
- **Error handling.** Implement robust error handling to manage exceptions and edge cases gracefully.
- **Code reviews.** All code changes should undergo peer review to ensure quality and adherence to project standards.
- **Documentation.** Maintain up-to-date documentation for all major components and functionalities to assist future developers and users.
- **Version control.** Use meaningful commit messages and follow branching strategies to manage code changes effectively.
- **Performance optimization.** Regularly profile and optimize code to ensure efficient performance without sacrificing readability.

### Testing
- **Automated testing.** Implement automated tests for all new features and bug fixes to ensure code reliability.
- **Continuous integration.** Set up continuous integration pipelines to run tests automatically on code changes.
- **Code coverage.** Aim for high code coverage with tests to minimize the risk of undetected bugs.
- **Regression testing.** Regularly run regression tests to ensure new changes do not break existing functionality.
- **Test documentation.** Document test cases and scenarios to provide clarity on what is being tested and the expected outcomes.
- **Performance testing.** Include performance tests to ensure that new features do not degrade the system's performance.
- **User acceptance testing.** Involve end-users in testing to validate that the software meets their needs and expectations.
- **Bug tracking.** Use a bug tracking system (GitHub Issues) to log, prioritize, and manage bugs effectively.

## GitHub Project and Copilot Workflow

This repository is tracked on a **GitHub Project** board under the [Bad-Mango-Solutions](https://github.com/Bad-Mango-Solutions) organization. See [`.github/GITHUB_PROJECT.md`](GITHUB_PROJECT.md) for board setup, automation variables, and verification commands.

### When Copilot (or any agent) picks up work

1. **Start from the GitHub Issue** linked on the project card — use its reproduction steps and acceptance criteria.
2. **Read `AGENTS.md`** (repo root) for `emudbg` batch verification when touching emulator, boot, disk, or debug commands.
3. **Branch naming:** `feature/<issue>-short-description` or `fix/<issue>-short-description`.
4. **Commit messages:** reference issues (`Fixes #123`, `Relates to #456`).
5. **Before finishing:** run `dotnet build BackPocketBasic.slnx` and `dotnet test BackPocketBasic.slnx`; for boot-path changes, run the DOS/ProDOS smoke sequences in `AGENTS.md` or `.github/GITHUB_PROJECT.md`.
6. **PR template:** fill Related Issues, Testing, and link the project card.

### Repository automation

| Workflow | Purpose |
|----------|---------|
| [`ci.yml`](workflows/ci.yml) | Build and test on push/PR to `main` |
| [`copilot-setup-steps.yml`](workflows/copilot-setup-steps.yml) | Copilot coding agent environment setup (restore, build, test, emudbg smoke) |
| [`project-automation.yml`](workflows/project-automation.yml) | Auto-add new issues/PRs to [org project #3](https://github.com/orgs/Bad-Mango-Solutions/projects/3) (requires `ADD_TO_PROJECT_PAT` secret) |
| [`.github/project.config.json`](project.config.json) | Committed project id/URL for Copilot and agents |

### Key planning documents

| Document | Scope |
|----------|-------|
| `PRD-emudbg-agent.md` | Agent mode product requirements |
| `emudbg-agent-plan.md` | MCP / structured tool implementation plan |
| `AGENTS.md` | Current `emudbg` command patterns for humans and AI |
| `specs/` and `profiles/` | Machine profiles and hardware specifications |

## Issue and PR Guidelines

### When Creating Issues
- **Clear titles and descriptions.** Provide concise and descriptive titles along with detailed descriptions of the issue or feature request.
- **Reproduction steps.** Include steps to reproduce the issue, if applicable, to facilitate debugging.
- **Expectations.** Clearly state the expected behavior versus the actual behavior observed.
- **Environment details.** Provide relevant environment details (e.g., OS, version, dependencies) that may affect the issue.
- **Error messages and stack traces.** Include any relevant error messages or stack traces to aid in diagnosis.
- **Labels and milestones.** Use appropriate labels and milestones to categorize and prioritize issues effectively.

### When Working on Tasks
- **Assign yourself.** Assign the issue to yourself when you start working on it to indicate ownership.
- **Link related issues.** Reference any related issues or pull requests in your commits and PR descriptions.
- **Focused and minimal changes.** Keep changes focused on the specific issue or feature being addressed to facilitate review.
- **Descriptive commit messages.** Write clear and descriptive commit messages that explain the purpose of the changes.
- **Update Tests.** Ensure that any new functionality or bug fixes are accompanied by appropriate tests.
- **Documentation updates.** Update relevant documentation to reflect changes made in the codebase (in the README.md, inline comments, and wiki pages).
- **Ensure backward compatibility.** Avoid breaking changes unless absolutely necessary, and document any such changes clearly.
- **Test with edge cases.** Consider edge cases and test accordingly to ensure robustness.
- **Test with full sample code.** Ensure that any new features or changes are tested with complete sample code to validate functionality.

## Dependencies and Libraries
- **Well-maintained libraries.** Use libraries that are actively maintained and have a strong community support.
- **Compatible with the target framework.** Ensure that all dependencies are compatible with .NET 10.0 (net10.0). The project may also accept packages compatible with net6.0 or netstandard2.0 as they are compatible with net10.0, but prefer net10.0-specific packages when available.
- **Properly licensed.** Verify that all third-party libraries comply with the project's licensing requirements. The repository in general should only use libraries that are licensed under permissive licenses (e.g., MIT, Apache 2.0). We use the MIT License for this repository, so any compatible license is acceptable.
- **Necessary and not redundant.** Avoid adding unnecessary dependencies that bloat the project or duplicate existing functionality.
- **Preferred libraries.** When introducing a new dependency in one of these areas, prefer the established choice already used in the codebase:
  - **Logging:** [Serilog](https://serilog.net/) (`Serilog.ILogger`).
  - **Dependency injection:** [Autofac](https://autofac.org/).
  - **Mocking in tests:** [Moq](https://github.com/devlooped/moq).

## Logging
- **Use Serilog `ILogger`.** All new code that needs to log MUST take `Serilog.ILogger` as a constructor parameter and store it in a `private readonly` field. Call `logger.ForContext<TThis>()` once in the constructor when a per-class context is desired.
- **Do not use `System.Diagnostics.Trace` or `System.Diagnostics.Debug` for application logging.** They are not testable, are not configurable, and bypass the configured Serilog sinks. Reserve `Debug.Assert`-style invariants for genuine programmer-error checks only.
- **Do not call `Log.Logger` or any other static / ambient Serilog facade in library code.** Always inject the logger so it can be mocked, scoped, and enriched per call site. The frontend application projects (e.g. `BadMango.Emulator.UI`, `BadMango.Basic.Console`) are responsible for configuring `Log.Logger` and registering an `ILogger` instance with Autofac at startup.
- **Make classes DI-friendly.** Constructors should accept their collaborators (including the `ILogger`) as parameters; avoid `new`-ing services or loggers internally so consumers can substitute test doubles.
- **Log outside hot loops.** Logging belongs at well-defined boundaries (configuration, mount/eject, error paths, lifecycle transitions). Do not log per CPU cycle, per scan-line, per emitted nibble, or inside any other timing-sensitive inner loop. Lift logging out of the loop or guard it with `logger.IsEnabled(LogEventLevel.Verbose)` when verbose detail is genuinely needed.
- **Use structured logging.** Prefer `logger.Warning("Foo {Bar} failed because {Reason}.", bar, reason)` over interpolated or concatenated strings so messages remain queryable.

## Testing helpers
- **Use `BadMango.Unit.Components` for shared test utilities.** All new test projects should reference `tests/BadMango.Unit.Components/BadMango.Unit.Components.csproj`.
  - **`Generator`** — random data generation. Use `Generator.Log()` to obtain a preconfigured `Mock<ILogger>` whose `ForContext` chain returns the same mock, which is the canonical way to inject a logger into a class under test.
  - **`UnitExtensions`** — Serilog sink extension that pipes log events to NUnit's `TestContext.Out`. Configure with `new LoggerConfiguration().WriteTo.NUnit()` when a real Serilog pipeline is needed inside a test.
  - **`NUnitContextSink`** — the underlying Serilog sink used by `WriteTo.NUnit()`.
  - **`UnitTest.InconclusiveIfCI`** — marks a test inconclusive when running in CI; pair with `PerformanceTestAttribute` for timing-sensitive cases.
