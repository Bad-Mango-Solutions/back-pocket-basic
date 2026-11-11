// <copyright file="AppleIIeKeyboard.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Devices;

using BadMango.Emulator.Bus;
using BadMango.Emulator.Bus.Interfaces;
using BadMango.Emulator.Core;
using BadMango.Emulator.Core.Interfaces.Signaling;
using BadMango.Emulator.Core.Signaling;

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
/// <para>
/// The keyboard tracks the modifier inputs that map to the Apple II pushbuttons:
/// </para>
/// <list type="bullet">
/// <item><description>$C061 (PB0): Open Apple / Button 0 - bit 7 set when pressed</description></item>
/// <item><description>$C062 (PB1): Closed Apple / Button 1 - bit 7 set when pressed</description></item>
/// <item><description>$C063 (PB2): Shift key modifier - bit 7 set when pressed</description></item>
/// </list>
/// <para>
/// Threading: the keyboard is fed from host threads (UI, REPL) while the machine thread reads
/// it. When the keyboard is initialized by a machine that implements
/// <see cref="IMachineDispatcher"/>, the host-facing mutators (<see cref="KeyDown"/>,
/// <see cref="KeyUp"/>, <see cref="SetModifiers"/>, <see cref="PressResetKey"/>,
/// <see cref="ReleaseResetKey"/> and <see cref="TypeString"/>) are marshalled onto the machine
/// thread with <see cref="IMachineDispatcher.Post"/>, so they never race the CPU, the scheduler or
/// the RESET line. While the machine is quiescent they take effect immediately.
/// </para>
/// </remarks>
[DeviceType("keyboard")]
public sealed class AppleIIeKeyboard : IAppleIIeKeyboard, ISoftSwitchProvider, IResetReceiver
{
    private const ushort KeyboardDataAddress = 0xC000;
    private const ushort KeyboardStrobeAddress = 0xC010;
    private const ushort PushButton0Address = 0xC061;
    private const ushort PushButton1Address = 0xC062;
    private const ushort PushButton2Address = 0xC063;

    private const byte KeyboardDataOffset = 0x00;
    private const byte KeyboardStrobeOffset = 0x10;
    private const byte PushButton0Offset = 0x61;
    private const byte PushButton1Offset = 0x62;
    private const byte PushButton2Offset = 0x63;
    private const byte StrobeBit = 0x80;
    private const byte KeyDownBit = 0x80;
    private const byte ButtonPressedBit = 0x80;

    /// <summary>
    /// CPU cycles per millisecond at approximately 1.02 MHz.
    /// </summary>
    private const int CyclesPerMillisecond = 1020;
    private const int ResetSourceDeviceId = int.MinValue + 2;

    /// <summary>
    /// Minimum gap between a typed key's release and the next typed key, in milliseconds.
    /// </summary>
    private const int MinimumTypeGapMs = 1;

    /// <summary>
    /// Divisor applied to the hold time to derive the inter-key gap (hold 50 ms gives a 10 ms gap).
    /// </summary>
    private const int TypeGapDivisor = 5;

    /// <summary>
    /// How often a pending typed key re-checks whether software has cleared the strobe, in milliseconds.
    /// </summary>
    private const int StrobeWaitPollMs = 1;

    private const string TypeEventTag = "keyboard-type";

    private readonly Queue<(byte Key, int DelayMs)> typeQueue = new();
    private readonly IKeyTranslator translator;
    private IMachineDispatcher? dispatcher;
    private int typeGeneration;
    private bool typingActive;
    private byte lastKey;
    private bool strobe;
    private bool keyDown;
    private bool resetKeyPressed;
    private bool resetAsserted;
    private KeyboardModifiers modifiers;
    private IScheduler? scheduler;
    private ISignalBus? signals;

    /// <summary>
    /// Initializes a new instance of the <see cref="AppleIIeKeyboard"/> class with the US-layout
    /// Apple IIe key translator.
    /// </summary>
    public AppleIIeKeyboard()
        : this(new AppleIIeKeyTranslator())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AppleIIeKeyboard"/> class with the specified
    /// key translator.
    /// </summary>
    /// <param name="translator">The translator that turns host keys into this keyboard's codes.</param>
    /// <exception cref="ArgumentNullException"><paramref name="translator"/> is <see langword="null"/>.</exception>
    public AppleIIeKeyboard(IKeyTranslator translator)
    {
        ArgumentNullException.ThrowIfNull(translator);
        this.translator = translator;
    }

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
    public IKeyTranslator Translator => translator;

    /// <inheritdoc />
    public bool RequireControlForReset { get; set; } = true;

    /// <inheritdoc />
    public string ProviderName => "Keyboard";

    /// <inheritdoc />
    public IReadOnlyList<SoftSwitchState> GetSoftSwitchStates()
    {
        bool openApplePressed = modifiers.HasFlag(KeyboardModifiers.OpenApple);
        bool closedApplePressed = modifiers.HasFlag(KeyboardModifiers.ClosedApple);
        bool shiftPressed = modifiers.HasFlag(KeyboardModifiers.Shift);

        return
        [
            new("KBD", KeyboardDataAddress, strobe, "Keyboard data register (strobe set when key available)"),
            new("KBDSTRB", KeyboardStrobeAddress, keyDown, "Any key down flag"),
            new("PB0", PushButton0Address, openApplePressed, "Pushbutton 0 / Open Apple"),
            new("PB1", PushButton1Address, closedApplePressed, "Pushbutton 1 / Closed Apple"),
            new("PB2", PushButton2Address, shiftPressed, "Pushbutton 2 / Shift key"),
        ];
    }

    /// <inheritdoc />
    public void Initialize(IEventContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        scheduler = context.Scheduler;
        signals = context.Signals;
        dispatcher = context as IMachineDispatcher;
    }

    /// <inheritdoc />
    public void RegisterHandlers(IOPageDispatcher dispatcher)
    {
        ArgumentNullException.ThrowIfNull(dispatcher);

        // $C000: Keyboard data (read-only for keyboard, write-only for 80STORE)
        dispatcher.RegisterRead(KeyboardDataOffset, ReadKeyboardData);

        // $C010: Clear strobe (read and write)
        dispatcher.Register(KeyboardStrobeOffset, ClearStrobeRead, ClearStrobeWrite);

        // $C061-$C063: Pushbutton inputs (read-only).
        // Register only when unclaimed so GameIOController can remain the single owner
        // when both devices are present, independent of registration order.
        _ = dispatcher.RegisterReadIfUnregistered(PushButton0Offset, ReadPushButton0);
        _ = dispatcher.RegisterReadIfUnregistered(PushButton1Offset, ReadPushButton1);
        _ = dispatcher.RegisterReadIfUnregistered(PushButton2Offset, ReadPushButton2);
    }

    /// <inheritdoc />
    public void Reset()
    {
        lastKey = 0;
        strobe = false;
        keyDown = false;
        resetKeyPressed = false;
        SetResetLineState(asserted: false);
        CancelTyping();
    }

    /// <inheritdoc />
    public void KeyDown(byte asciiCode)
    {
        OnMachineThread(() =>
        {
            lastKey = (byte)(asciiCode & 0x7F); // Clear high bit
            strobe = true;
            keyDown = true;
        });
    }

    /// <inheritdoc />
    public void KeyUp()
    {
        // Note: lastKey and strobe remain unchanged until strobe is cleared
        OnMachineThread(() => keyDown = false);
    }

    /// <inheritdoc />
    public void SetModifiers(KeyboardModifiers modifiers)
    {
        OnMachineThread(() =>
        {
            this.modifiers = modifiers;
            UpdateResetLineState();
        });
    }

    /// <inheritdoc />
    public void PressResetKey()
    {
        OnMachineThread(() =>
        {
            resetKeyPressed = true;
            UpdateResetLineState();
        });
    }

    /// <inheritdoc />
    public void ReleaseResetKey()
    {
        OnMachineThread(() =>
        {
            resetKeyPressed = false;
            UpdateResetLineState();
        });
    }

    /// <inheritdoc />
    public void TypeString(string text, int delayMs = 50)
    {
        ArgumentNullException.ThrowIfNull(text);

        // Convert characters to Apple II ASCII and enqueue non-zero values
        var keysToEnqueue = text
            .Select(c => ConvertToAppleAscii(c))
            .Where(ascii => ascii != 0)
            .Select(ascii => (ascii, delayMs))
            .ToArray();

        OnMachineThread(() =>
        {
            foreach (var entry in keysToEnqueue)
            {
                typeQueue.Enqueue(entry);
            }

            // Hand off to the scheduler: every step of typing (key down, key up, gap) runs as a
            // scheduler event in emulated time. A run already in progress picks the new
            // characters up when it reaches the end of the queue.
            if (!typingActive && typeQueue.Count > 0 && scheduler is not null)
            {
                typingActive = true;
                int generation = typeGeneration;
                scheduler.ScheduleAfter(1, ScheduledEventKind.DeferredWork, 0, _ => TypeNextKey(generation), TypeEventTag);
            }
        });
    }

    /// <inheritdoc />
    /// <remarks>
    /// RESET clears the keyboard strobe and discards any type-ahead queued by
    /// <see cref="TypeString"/>. The machine flushes the scheduler on RESET, which drops the
    /// pending release of the key currently being typed, so that key is released here instead
    /// of being left stuck down. Keys and modifiers the user is physically holding are unchanged.
    /// </remarks>
    public void OnResetAsserted(Cycle cycle)
    {
        strobe = false;
        CancelTyping();
    }

    /// <inheritdoc />
    public void OnResetReleased(Cycle cycle)
    {
    }

    private static ulong MillisecondsToCycles(int milliseconds) => (ulong)Math.Max(1, milliseconds) * CyclesPerMillisecond;

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

    private byte ReadPushButton0(byte offset, in BusAccess context)
    {
        // PB0 / Open Apple: bit 7 set = button pressed
        return modifiers.HasFlag(KeyboardModifiers.OpenApple) ? ButtonPressedBit : (byte)0x00;
    }

    private byte ReadPushButton1(byte offset, in BusAccess context)
    {
        // PB1 / Closed Apple: bit 7 set = button pressed
        return modifiers.HasFlag(KeyboardModifiers.ClosedApple) ? ButtonPressedBit : (byte)0x00;
    }

    private byte ReadPushButton2(byte offset, in BusAccess context)
    {
        // PB2 / Shift key: bit 7 set = key pressed
        return modifiers.HasFlag(KeyboardModifiers.Shift) ? ButtonPressedBit : (byte)0x00;
    }

    private void OnMachineThread(Action action)
    {
        if (dispatcher is null)
        {
            action();
        }
        else
        {
            dispatcher.Post(action);
        }
    }

    private void CancelTyping()
    {
        typeQueue.Clear();
        typeGeneration++;

        if (typingActive)
        {
            typingActive = false;

            // RESET flushes the scheduler, which drops the pending release of the key being
            // typed, so release it here rather than leave it stuck down.
            keyDown = false;
        }
    }

    /// <summary>
    /// Presses the next queued character, or waits for software to clear the strobe of the
    /// previous one. Runs on the machine thread as a scheduler event.
    /// </summary>
    /// <param name="generation">The typing generation the event was scheduled for.</param>
    private void TypeNextKey(int generation)
    {
        if (generation != typeGeneration || scheduler is null)
        {
            return;
        }

        if (typeQueue.Count == 0)
        {
            typingActive = false;
            return;
        }

        if (strobe)
        {
            // Software has not consumed the previous key yet. Never overwrite type-ahead: check
            // again later. Clearing the strobe does not start the next key by itself.
            scheduler.ScheduleAfter(MillisecondsToCycles(StrobeWaitPollMs), ScheduledEventKind.DeviceTimer, 0, _ => TypeNextKey(generation), TypeEventTag);
            return;
        }

        var (key, delayMs) = typeQueue.Dequeue();
        lastKey = (byte)(key & 0x7F);
        strobe = true;
        keyDown = true;

        scheduler.ScheduleAfter(MillisecondsToCycles(delayMs), ScheduledEventKind.DeviceTimer, 0, _ => ReleaseTypedKey(generation, delayMs), TypeEventTag);
    }

    /// <summary>
    /// Releases the key being typed and schedules the next one after the inter-key gap.
    /// </summary>
    /// <param name="generation">The typing generation the event was scheduled for.</param>
    /// <param name="delayMs">The hold time of the released key, used to derive the gap.</param>
    private void ReleaseTypedKey(int generation, int delayMs)
    {
        if (generation != typeGeneration || scheduler is null)
        {
            return;
        }

        keyDown = false;

        int gapMs = Math.Max(MinimumTypeGapMs, delayMs / TypeGapDivisor);
        scheduler.ScheduleAfter(MillisecondsToCycles(gapMs), ScheduledEventKind.DeviceTimer, 0, _ => TypeNextKey(generation), TypeEventTag);
    }

    private void UpdateResetLineState()
    {
        bool controlHeld = modifiers.HasFlag(KeyboardModifiers.Control);
        bool shouldAssert = resetKeyPressed && (!RequireControlForReset || controlHeld);
        SetResetLineState(shouldAssert);
    }

    private void SetResetLineState(bool asserted)
    {
        if (signals is null || resetAsserted == asserted)
        {
            return;
        }

        resetAsserted = asserted;

        if (asserted)
        {
            signals.Assert(SignalLine.Reset, ResetSourceDeviceId, scheduler?.Now ?? Cycle.Zero);
        }
        else
        {
            signals.Deassert(SignalLine.Reset, ResetSourceDeviceId, scheduler?.Now ?? Cycle.Zero);
        }
    }
}