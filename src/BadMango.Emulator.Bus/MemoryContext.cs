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

/// <summary>
/// Predefined memory contexts for common Apple II memory configurations.
/// </summary>
public static class MemoryContexts
{
    /// <summary>
    /// The default ROM context. Traps registered without a specific context use this.
    /// </summary>
    public static readonly MemoryContext Rom = new("ROM");

    /// <summary>
    /// Language Card RAM context for $D000-$FFFF when LC RAM is enabled.
    /// </summary>
    public static readonly MemoryContext LanguageCardRam = new("LC_RAM");

    /// <summary>
    /// Language Card RAM Bank 1 context for $D000-$DFFF.
    /// </summary>
    public static readonly MemoryContext LanguageCardBank1 = new("LC_BANK1");

    /// <summary>
    /// Language Card RAM Bank 2 context for $D000-$DFFF.
    /// </summary>
    public static readonly MemoryContext LanguageCardBank2 = new("LC_BANK2");

    /// <summary>
    /// Main RAM context for $0000-$BFFF.
    /// </summary>
    public static readonly MemoryContext MainRam = new("MAIN_RAM");

    /// <summary>
    /// Auxiliary RAM context for $0000-$BFFF (Apple IIe/IIc).
    /// </summary>
    public static readonly MemoryContext AuxiliaryRam = new("AUX_RAM");

    /// <summary>
    /// ProDOS /RAM disk context.
    /// </summary>
    public static readonly MemoryContext ProDosRamDisk = new("PRODOS_RAM");

    /// <summary>
    /// Creates a custom memory context with the specified identifier.
    /// </summary>
    /// <param name="id">The unique identifier for the memory context.</param>
    /// <returns>A new <see cref="MemoryContext"/> with the specified ID.</returns>
    public static MemoryContext Custom(string id) => new(id);
}