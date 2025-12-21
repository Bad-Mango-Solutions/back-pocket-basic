// <copyright file="SystemContext.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Basic.Runtime;

using Emulation;
using IO;

/// <summary>
/// Provides a concrete implementation of <see cref="ISystemContext"/> that aggregates
/// system-level services.
/// </summary>
/// <remarks>
/// This class serves as a container for hardware emulation and I/O services,
/// separating system concerns from BASIC language runtime state.
/// </remarks>
public sealed class SystemContext : ISystemContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SystemContext"/> class.
    /// </summary>
    /// <param name="system">The Apple II system emulator.</param>
    /// <param name="io">The I/O handler for console operations.</param>
    public SystemContext(
        IAppleSystem system,
        IBasicIO io)
    {
        System = system;
        IO = io;
    }

    /// <inheritdoc/>
    public IAppleSystem System { get; }

    /// <inheritdoc/>
    public IBasicIO IO { get; }
}