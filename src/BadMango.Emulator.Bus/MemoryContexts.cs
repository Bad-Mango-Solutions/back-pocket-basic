// <copyright file="MemoryContexts.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus;

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