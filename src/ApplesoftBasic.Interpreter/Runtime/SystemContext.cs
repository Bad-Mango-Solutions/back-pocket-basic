// <copyright file="SystemContext.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.Runtime;

using Emulation;
using IO;
using Microsoft.Extensions.Logging;

/// <summary>
/// Provides a concrete implementation of <see cref="ISystemContext"/> that aggregates
/// system-level services.
/// </summary>
/// <remarks>
/// This class serves as a container for hardware emulation, I/O, and logging services,
/// separating system concerns from BASIC language runtime state.
/// </remarks>
public sealed class SystemContext : ISystemContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SystemContext"/> class.
    /// </summary>
    /// <param name="system">The Apple II system emulator.</param>
    /// <param name="io">The I/O handler for console operations.</param>
    /// <param name="logger">The logger for diagnostic output.</param>
    public SystemContext(
        IAppleSystem system,
        IBasicIO io,
        ILogger logger)
    {
        System = system;
        IO = io;
        Logger = logger;
    }

    /// <inheritdoc/>
    public IAppleSystem System { get; }

    /// <inheritdoc/>
    public IBasicIO IO { get; }

    /// <inheritdoc/>
    public ILogger Logger { get; }
}