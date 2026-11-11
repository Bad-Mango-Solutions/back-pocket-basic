// <copyright file="IOPageDispatcher.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus;

/// <summary>
/// Flat dispatch table for the $C000-$C0FF soft switch region.
/// </summary>
/// <remarks>
/// <para>
/// The Apple IIe I/O page ($C000-$C0FF) contains 256 bytes of soft switches
/// from multiple devices: keyboard, auxiliary memory, speaker, video modes,
/// game I/O, Language Card, and slot device I/O.
/// </para>
/// <para>
/// Rather than complex interface hierarchies, this class uses a simple flat
/// dispatch table: two 256-entry arrays of delegates for read and write
/// handlers. Devices register their handlers at specific offsets during
/// initialization.
/// </para>
/// <para>
/// Unregistered addresses return $FF (floating bus) on read and no-op on write,
/// consistent with real Apple II hardware behavior.
/// </para>
/// <para>
/// Slot I/O Layout ($C080-$C0FF):
/// </para>
/// <list type="bullet">
/// <item><description>$C080-$C08F (slot 0): Language Card</description></item>
/// <item><description>$C090-$C09F (slot 1): 16 bytes</description></item>
/// <item><description>$C0A0-$C0AF (slot 2): 16 bytes</description></item>
/// <item><description>$C0B0-$C0BF (slot 3): 16 bytes</description></item>
/// <item><description>$C0C0-$C0CF (slot 4): 16 bytes</description></item>
/// <item><description>$C0D0-$C0DF (slot 5): 16 bytes</description></item>
/// <item><description>$C0E0-$C0EF (slot 6): 16 bytes</description></item>
/// <item><description>$C0F0-$C0FF (slot 7): 16 bytes</description></item>
/// </list>
/// </remarks>
public sealed class IOPageDispatcher
{
    /// <summary>
    /// The value returned when reading from an unregistered address (floating bus).
    /// </summary>
    private const byte FloatingBusValue = 0xFF;

    /// <summary>
    /// The number of I/O addresses in the page.
    /// </summary>
    private const int IOPageSize = 256;

    /// <summary>
    /// The number of I/O bytes per slot.
    /// </summary>
    private const int SlotIOSize = 16;

    /// <summary>
    /// The number of peripheral slots (0-7).
    /// </summary>
    private const int SlotCount = 8;

    /// <summary>
    /// The base offset for slot I/O in the $C0xx page.
    /// </summary>
    private const byte SlotIOBaseOffset = 0x80;

    private readonly SoftSwitchReadHandler?[] readHandlers;
    private readonly SoftSwitchWriteHandler?[] writeHandlers;

    /// <summary>
    /// Initializes a new instance of the <see cref="IOPageDispatcher"/> class.
    /// </summary>
    public IOPageDispatcher()
    {
        readHandlers = new SoftSwitchReadHandler?[IOPageSize];
        writeHandlers = new SoftSwitchWriteHandler?[IOPageSize];
    }

    /// <summary>
    /// Reads from a soft switch address.
    /// </summary>
    /// <param name="offset">Offset within $C000-$C0FF (0x00-0xFF).</param>
    /// <param name="context">Bus access context.</param>
    /// <returns>
    /// The value returned by the registered handler, or $FF (floating bus)
    /// if no handler is registered.
    /// </returns>
    public byte Read(byte offset, in BusAccess context)
    {
        var handler = readHandlers[offset];
        return handler is not null ? handler(offset, in context) : FloatingBusValue;
    }

    /// <summary>
    /// Writes to a soft switch address.
    /// </summary>
    /// <param name="offset">Offset within $C000-$C0FF (0x00-0xFF).</param>
    /// <param name="value">Value being written.</param>
    /// <param name="context">Bus access context.</param>
    /// <remarks>
    /// If no handler is registered for the offset, the write is silently ignored (no-op).
    /// </remarks>
    public void Write(byte offset, byte value, in BusAccess context)
    {
        var handler = writeHandlers[offset];
        handler?.Invoke(offset, value, in context);
    }

    /// <summary>
    /// Registers a read handler for a specific offset.
    /// </summary>
    /// <param name="offset">Offset within $C000-$C0FF (0x00-0xFF).</param>
    /// <param name="handler">The read handler to register.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="handler"/> is <see langword="null"/>.
    /// </exception>
    public void RegisterRead(byte offset, SoftSwitchReadHandler handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        readHandlers[offset] = handler;
    }

    /// <summary>
    /// Registers a read handler for a specific offset only when no handler is currently registered.
    /// </summary>
    /// <param name="offset">Offset within $C000-$C0FF (0x00-0xFF).</param>
    /// <param name="handler">The read handler to register.</param>
    /// <returns>
    /// <see langword="true"/> if the handler was registered; <see langword="false"/> if the offset
    /// already had a read handler.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="handler"/> is <see langword="null"/>.
    /// </exception>
    public bool RegisterReadIfUnregistered(byte offset, SoftSwitchReadHandler handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        if (readHandlers[offset] is not null)
        {
            return false;
        }

        readHandlers[offset] = handler;
        return true;
    }

    /// <summary>
    /// Registers a write handler for a specific offset.
    /// </summary>
    /// <param name="offset">Offset within $C000-$C0FF (0x00-0xFF).</param>
    /// <param name="handler">The write handler to register.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="handler"/> is <see langword="null"/>.
    /// </exception>
    public void RegisterWrite(byte offset, SoftSwitchWriteHandler handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        writeHandlers[offset] = handler;
    }

    /// <summary>
    /// Registers read and/or write handlers for a specific offset.
    /// </summary>
    /// <param name="offset">Offset within $C000-$C0FF (0x00-0xFF).</param>
    /// <param name="read">
    /// The read handler, or <see langword="null"/> to leave unhandled.
    /// </param>
    /// <param name="write">
    /// The write handler, or <see langword="null"/> to leave unhandled.
    /// </param>
    /// <remarks>
    /// Unlike <see cref="RegisterRead"/> and <see cref="RegisterWrite"/>,
    /// this method allows <see langword="null"/> handlers for either or both
    /// operations. A <see langword="null"/> handler will clear any previously
    /// registered handler at that offset.
    /// </remarks>
    public void Register(byte offset, SoftSwitchReadHandler? read, SoftSwitchWriteHandler? write)
    {
        readHandlers[offset] = read;
        writeHandlers[offset] = write;
    }

    /// <summary>
    /// Installs a slot card's handlers into the dispatcher.
    /// </summary>
    /// <param name="slot">The slot number (0-7).</param>
    /// <param name="handlers">The slot's I/O handlers.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="slot"/> is not in the range 0-7.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="handlers"/> is <see langword="null"/>.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method copies the card's 16 handlers into positions
    /// <c>0x80 + (slot &lt;&lt; 4)</c> through <c>0x8F + (slot &lt;&lt; 4)</c>.
    /// </para>
    /// <para>
    /// For example, slot 6 handlers are installed at offsets 0xE0-0xEF,
    /// corresponding to addresses $C0E0-$C0EF.
    /// </para>
    /// </remarks>
    public void InstallSlotHandlers(int slot, SlotIOHandlers handlers)
    {
        ValidateSlotNumber(slot);
        ArgumentNullException.ThrowIfNull(handlers);

        int baseOffset = SlotIOBaseOffset + (slot * SlotIOSize);

        for (int i = 0; i < SlotIOSize; i++)
        {
            readHandlers[baseOffset + i] = handlers.ReadHandlers[i];
            writeHandlers[baseOffset + i] = handlers.WriteHandlers[i];
        }
    }

    /// <summary>
    /// Removes all handlers for a slot.
    /// </summary>
    /// <param name="slot">The slot number (0-7).</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="slot"/> is not in the range 0-7.
    /// </exception>
    /// <remarks>
    /// After removal, reads from the slot's I/O region return $FF (floating bus)
    /// and writes are ignored.
    /// </remarks>
    public void RemoveSlotHandlers(int slot)
    {
        ValidateSlotNumber(slot);

        int baseOffset = SlotIOBaseOffset + (slot * SlotIOSize);

        for (int i = 0; i < SlotIOSize; i++)
        {
            readHandlers[baseOffset + i] = null;
            writeHandlers[baseOffset + i] = null;
        }
    }

    private static void ValidateSlotNumber(int slot)
    {
        if (slot < 0 || slot >= SlotCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(slot),
                slot,
                $"Slot number must be 0-7, was {slot}.");
        }
    }
}