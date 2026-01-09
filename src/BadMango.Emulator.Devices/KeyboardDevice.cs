// <copyright file="KeyboardDevice.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Devices;

using BadMango.Emulator.Bus;
using BadMango.Emulator.Bus.Interfaces;

using Interfaces;

/// <summary>
/// Keyboard device handling $C000 (KBD) and $C010 (KBDSTRB).
/// </summary>
/// <remarks>
/// <para>
/// The Apple II keyboard presents a simple interface to software:
/// </para>
/// <list type="bullet">
/// <item><description>$C000: Keyboard data (bit 7 = strobe, bits 6-0 = ASCII)</description></item>
/// <item><description>$C010: Any key down flag (read) / clear strobe (read or write)</description></item>
/// </list>
/// <para>
/// When a key is pressed, the ASCII code is latched into the keyboard data register
/// with the high bit (strobe) set. The strobe remains set until cleared by accessing $C010.
/// </para>
/// </remarks>
[DeviceType("keyboard")]
public sealed class KeyboardDevice : IKeyboardDevice, ISoftSwitchProvider
{
    private const ushort KeyboardDataAddress = 0xC000;
    private const ushort KeyboardStrobeAddress = 0xC010;

    private const byte KeyboardDataOffset = 0x00;
    private const byte KeyboardStrobeOffset = 0x10;
    private const byte StrobeBit = 0x80;
    private const byte KeyDownBit = 0x80;

    /// <summary>
    /// CPU cycles per millisecond at approximately 1.02 MHz.
    /// </summary>
    private const int CyclesPerMillisecond = 1020;

    private readonly Queue<(byte Key, int DelayMs)> typeQueue = new();
    private byte lastKey;
    private bool strobe;
    private bool keyDown;
    private KeyboardModifiers modifiers;
    private IScheduler? scheduler;

    /// <inheritdoc />
    public string Name => "Keyboard";

    /// <inheritdoc />
    public string DeviceType => "Keyboard";

    /// <inheritdoc />
    public PeripheralKind Kind => PeripheralKind.Motherboard;

    /// <inheritdoc />
    public bool HasKeyDown => keyDown;

    /// <inheritdoc />
    public byte KeyData => strobe ? (byte)(lastKey | StrobeBit) : lastKey;

    /// <inheritdoc />
    public KeyboardModifiers Modifiers => modifiers;

    /// <inheritdoc />
    public string ProviderName => "Keyboard";

    /// <inheritdoc />
    public IReadOnlyList<SoftSwitchState> GetSoftSwitchStates()
    {
        return
        [
            new("KBD", KeyboardDataAddress, strobe, "Keyboard data register (strobe set when key available)"),
            new("KBDSTRB", KeyboardStrobeAddress, keyDown, "Any key down flag"),
        ];
    }

    /// <inheritdoc />
    public void Initialize(IEventContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        scheduler = context.Scheduler;
    }

    /// <inheritdoc />
    public void RegisterHandlers(IOPageDispatcher dispatcher)
    {
        ArgumentNullException.ThrowIfNull(dispatcher);

        // $C000: Keyboard data (read-only for keyboard, write-only for 80STORE)
        dispatcher.RegisterRead(KeyboardDataOffset, ReadKeyboardData);

        // $C010: Clear strobe (read and write)
        dispatcher.Register(KeyboardStrobeOffset, ClearStrobeRead, ClearStrobeWrite);
    }

    /// <inheritdoc />
    public void Reset()
    {
        lastKey = 0;
        strobe = false;
        keyDown = false;
        modifiers = KeyboardModifiers.None;
        typeQueue.Clear();
    }

    /// <inheritdoc />
    public void KeyDown(byte asciiCode)
    {
        lastKey = (byte)(asciiCode & 0x7F); // Clear high bit
        strobe = true;
        keyDown = true;
    }

    /// <inheritdoc />
    public void KeyUp()
    {
        keyDown = false;

        // Note: lastKey and strobe remain unchanged until strobe is cleared
    }

    /// <inheritdoc />
    public void SetModifiers(KeyboardModifiers modifiers)
    {
        this.modifiers = modifiers;
    }

    /// <inheritdoc />
    public void TypeString(string text, int delayMs = 50)
    {
        ArgumentNullException.ThrowIfNull(text);

        // Convert characters to Apple II ASCII and enqueue non-zero values
        var keysToEnqueue = text
            .Select(c => ConvertToAppleAscii(c))
            .Where(ascii => ascii != 0)
            .Select(ascii => (ascii, delayMs));

        foreach (var entry in keysToEnqueue)
        {
            typeQueue.Enqueue(entry);
        }

        // Start processing the queue if not already in progress
        ProcessTypeQueue();
    }

    private static byte ConvertToAppleAscii(char c)
    {
        // Handle common character conversions
        return c switch
        {
            '\n' or '\r' => 0x0D, // Carriage return
            '\b' => 0x08,         // Backspace
            '\t' => 0x09,         // Tab (not common on Apple II, but supported)
            '\x1B' => 0x1B,       // Escape
            >= ' ' and <= '~' => (byte)c, // Printable ASCII
            _ => 0,               // Unsupported characters
        };
    }

    private byte ReadKeyboardData(byte offset, in BusAccess context)
    {
        return KeyData;
    }

    private byte ClearStrobeRead(byte offset, in BusAccess context)
    {
        byte result = keyDown ? KeyDownBit : (byte)0x00;

        if (!context.IsSideEffectFree)
        {
            strobe = false;
        }

        return result;
    }

    private void ClearStrobeWrite(byte offset, byte value, in BusAccess context)
    {
        if (!context.IsSideEffectFree)
        {
            strobe = false;
        }
    }

    private void ProcessTypeQueue()
    {
        if (typeQueue.Count == 0 || scheduler is null)
        {
            return;
        }

        // Only inject a key if strobe is clear
        if (!strobe && typeQueue.TryDequeue(out var entry))
        {
            KeyDown(entry.Key);

            // Schedule key release and next key
            var delayCycles = (ulong)(entry.DelayMs * CyclesPerMillisecond);

            scheduler.ScheduleAt(
                scheduler.Now + delayCycles,
                ScheduledEventKind.DeviceTimer,
                0,
                _ =>
                {
                    KeyUp();
                    ProcessTypeQueue();
                });
        }
    }
}