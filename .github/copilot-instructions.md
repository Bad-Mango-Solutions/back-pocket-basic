# GitHub Repository Copilot Instructions

## Project Overview

This repository contains code to emulate an Applesoft BASIC interpreter, enhancing its capabilities and performance. The project is structured to facilitate contributions, maintainability, and adherence to coding standards.

### Target Framework

**This project uses .NET 10.0** as its target framework. All new code and projects must target `net10.0`. Do not question or suggest changes to the framework version - .NET 10.0 is the established standard for this repository as documented in the README and all project files.

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
- **Important:** SA1600 and related XML doc warnings must not be suppressed. Always provide XML documentation for all public members and types to ensure clarity and maintainability.

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
