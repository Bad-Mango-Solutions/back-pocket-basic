// <copyright file="ISystemContext.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Basic.Runtime;

using Emulation;
using IO;

/// <summary>
/// Provides an interface for managing system-level services including hardware emulation
/// and I/O operations.
/// </summary>
/// <remarks>
/// This context aggregates all hardware and system-level services, separating them from
/// BASIC language runtime concerns. It provides expansion points for future hardware
/// emulation features such as disk controllers, expansion cards, and file systems.
/// </remarks>
public interface ISystemContext
{
    /// <summary>
    /// Gets the Apple II system emulator providing access to CPU, memory, and hardware.
    /// </summary>
    /// <value>
    /// An instance of <see cref="IAppleSystem"/> that emulates the Apple II hardware.
    /// </value>
    IAppleSystem System { get; }

    /// <summary>
    /// Gets the I/O handler for console and screen operations.
    /// </summary>
    /// <value>
    /// An instance of <see cref="IBasicIO"/> that manages input/output operations.
    /// </value>
    IBasicIO IO { get; }
}