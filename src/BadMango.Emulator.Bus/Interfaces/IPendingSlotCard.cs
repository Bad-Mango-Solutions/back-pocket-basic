// <copyright file="IPendingSlotCard.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus.Interfaces;

/// <summary>
/// Represents a slot card pending installation during machine build.
/// </summary>
/// <remarks>
/// <para>
/// This interface allows the machine builder to install pending slot cards
/// without tight coupling to system-specific implementations. Cards are
/// stored as pending components during builder configuration and installed
/// when the machine is built and the <see cref="ISlotManager"/> is available.
/// </para>
/// </remarks>
public interface IPendingSlotCard
{
    /// <summary>
    /// Gets the slot number (1-7) where the card should be installed.
    /// </summary>
    int Slot { get; }

    /// <summary>
    /// Gets the slot card to install.
    /// </summary>
    ISlotCard Card { get; }
}