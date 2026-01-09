// <copyright file="ILanguageCardState.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus.Interfaces;

/// <summary>
/// Interface for querying Language Card state.
/// </summary>
/// <remarks>
/// <para>
/// This interface exposes the read-only state of a Language Card implementation,
/// allowing other components (such as the <see cref="TrapRegistry"/>) to query
/// whether Language Card RAM is currently enabled without depending on a specific
/// implementation class.
/// </para>
/// </remarks>
public interface ILanguageCardState
{
    /// <summary>
    /// Gets a value indicating whether RAM read is enabled.
    /// </summary>
    /// <value><see langword="true"/> if RAM read is enabled; otherwise, <see langword="false"/>.</value>
    /// <remarks>
    /// When RAM read is enabled, reads from $D000-$FFFF return Language Card RAM.
    /// When disabled, reads return ROM.
    /// </remarks>
    bool IsRamReadEnabled { get; }

    /// <summary>
    /// Gets a value indicating whether RAM write is enabled.
    /// </summary>
    /// <value><see langword="true"/> if RAM write is enabled; otherwise, <see langword="false"/>.</value>
    /// <remarks>
    /// When RAM write is enabled, writes to $D000-$FFFF go to Language Card RAM.
    /// When disabled, writes are ignored.
    /// </remarks>
    bool IsRamWriteEnabled { get; }

    /// <summary>
    /// Gets the currently selected bank (1 or 2) for the $D000-$DFFF region.
    /// </summary>
    /// <value>1 for Bank 1, 2 for Bank 2.</value>
    int SelectedBank { get; }
}