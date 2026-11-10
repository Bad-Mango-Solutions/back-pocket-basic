// <copyright file="MemoryContext.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus;

/// <summary>
/// Identifies a memory context for trap registration and execution.
/// </summary>
/// <remarks>
/// <para>
/// Memory contexts allow different trap handlers to be registered at the same address
/// for different memory configurations. This is essential for Apple II emulation where
/// the same address can map to different physical memory depending on soft switch states:
/// </para>
/// <list type="bullet">
/// <item><description><b>ROM vs Language Card RAM</b> ($D000-$FFFF)</description></item>
/// <item><description><b>Main RAM vs Auxiliary RAM</b> ($0000-$BFFF)</description></item>
/// <item><description><b>Different expansion ROM cards</b> ($C800-$CFFF)</description></item>
/// <item><description><b>ProDOS /RAM disk banks</b></description></item>
/// </list>
/// <para>
/// The trap registry uses the memory context as part of the trap key, allowing
/// multiple traps to coexist at the same address for different contexts.
/// </para>
/// </remarks>
/// <param name="Id">
/// A unique identifier for the memory context. Use predefined constants from
/// <see cref="MemoryContexts"/> for standard contexts, or define custom IDs for
/// application-specific memory areas.
/// </param>
public readonly record struct MemoryContext(string Id)
{
    /// <summary>
    /// Gets a value indicating whether this is the default (ROM) context.
    /// </summary>
    public bool IsDefault => Id == MemoryContexts.Rom.Id;

    /// <inheritdoc />
    public override string ToString() => Id;
}