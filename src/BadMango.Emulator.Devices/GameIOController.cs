// <copyright file="GameIOController.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Devices;

using BadMango.Emulator.Bus;
using BadMango.Emulator.Bus.Interfaces;

using Interfaces;

/// <summary>
/// Game I/O controller: pushbuttons ($C060-$C063), paddles ($C064-$C067),
/// and paddle trigger ($C070).
/// </summary>
/// <remarks>
/// <para>
/// The Apple II game port provides:
/// </para>
/// <list type="bullet">
/// <item><description>4 pushbuttons at $C060-$C063 (bit 7 = pressed)</description></item>
/// <item><description>4 analog paddle inputs read via timing loops after triggering $C070</description></item>
/// </list>
/// <para>
/// Paddle timing: When $C070 is accessed, one-shot timers start for all 4 paddles.
/// Reading $C064-$C067 returns bit 7 high while the timer is running. The timer
/// duration is proportional to the paddle position (0-255), with approximately
/// 11 cycles per unit at 1 MHz.
/// </para>
/// </remarks>
public sealed class GameIOController : IGamePortDevice
{
    private const byte PushButton0Offset = 0x61;
    private const byte PushButton1Offset = 0x62;
    private const byte PushButton2Offset = 0x63;
    private const byte Paddle0Offset = 0x64;
    private const byte PaddleTriggerOffset = 0x70;
    private const byte ButtonPressedBit = 0x80;

    /// <summary>
    /// Cycles per paddle unit at 1 MHz. Paddle values 0-255 map to timer
    /// durations of 0-2805 cycles (approximately 0-2.75ms).
    /// </summary>
    private const int CyclesPerPaddleUnit = 11;

    private readonly bool[] buttons = new bool[4];
    private readonly byte[] paddleValues = new byte[4];
    private readonly ulong[] paddleTimers = new ulong[4];
    private IScheduler? scheduler;

    /// <inheritdoc />
    public string Name => "Game I/O Controller";

    /// <inheritdoc />
    public string DeviceType => "GameIO";

    /// <inheritdoc />
    public PeripheralKind Kind => PeripheralKind.Motherboard;

    /// <inheritdoc />
    public IReadOnlyList<bool> Buttons => buttons;

    /// <inheritdoc />
    public IReadOnlyList<byte> Paddles => paddleValues;

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

        // $C060: Open Apple / Pushbutton 3 (active low on real hardware, but we use high = pressed)
        dispatcher.RegisterRead(0x60, (o, in ctx) => ReadButton(3));

        // Pushbuttons $C061-$C063
        dispatcher.RegisterRead(PushButton0Offset, (o, in ctx) => ReadButton(0));
        dispatcher.RegisterRead(PushButton1Offset, (o, in ctx) => ReadButton(1));
        dispatcher.RegisterRead(PushButton2Offset, (o, in ctx) => ReadButton(2));

        // Paddles $C064-$C067
        for (byte i = 0; i < 4; i++)
        {
            byte paddleIndex = i;
            dispatcher.RegisterRead((byte)(Paddle0Offset + i), (o, in ctx) => ReadPaddle(paddleIndex, ctx.Cycle));
        }

        // Paddle trigger $C070 (and $C070-$C07F mirror)
        for (byte i = 0; i < 16; i++)
        {
            byte offset = (byte)(PaddleTriggerOffset + i);
            dispatcher.Register(offset, TriggerPaddlesRead, TriggerPaddlesWrite);
        }
    }

    /// <inheritdoc />
    public void Reset()
    {
        Array.Clear(buttons);
        Array.Clear(paddleValues);
        Array.Clear(paddleTimers);
    }

    /// <inheritdoc />
    public void SetButton(int button, bool pressed)
    {
        if (button < 0 || button >= buttons.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(button), button, "Button index must be 0-3.");
        }

        buttons[button] = pressed;
    }

    /// <inheritdoc />
    public void SetPaddle(int paddle, byte position)
    {
        if (paddle < 0 || paddle >= paddleValues.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(paddle), paddle, "Paddle index must be 0-3.");
        }

        paddleValues[paddle] = position;
    }

    /// <inheritdoc />
    public void SetJoystick(float x, float y)
    {
        // Clamp and convert from -1..1 to 0..255
        x = Math.Clamp(x, -1f, 1f);
        y = Math.Clamp(y, -1f, 1f);

        paddleValues[0] = (byte)((x + 1f) * 127.5f);
        paddleValues[1] = (byte)((y + 1f) * 127.5f);
    }

    private byte ReadButton(int index)
    {
        return buttons[index] ? ButtonPressedBit : (byte)0x00;
    }

    private byte ReadPaddle(int index, ulong currentCycle)
    {
        // Return bit 7 high if timer hasn't expired
        if (currentCycle < paddleTimers[index])
        {
            return ButtonPressedBit;
        }

        return 0x00;
    }

    private byte TriggerPaddlesRead(byte offset, in BusAccess context)
    {
        if (!context.IsSideEffectFree)
        {
            TriggerPaddles(context.Cycle);
        }

        return 0xFF; // Floating bus
    }

    private void TriggerPaddlesWrite(byte offset, byte value, in BusAccess context)
    {
        if (!context.IsSideEffectFree)
        {
            TriggerPaddles(context.Cycle);
        }
    }

    private void TriggerPaddles(ulong currentCycle)
    {
        // Start timers for all paddles
        for (int i = 0; i < 4; i++)
        {
            // Timer duration is proportional to paddle value
            // Approximately 11 cycles per unit at 1 MHz
            ulong duration = (ulong)(paddleValues[i] * CyclesPerPaddleUnit);
            paddleTimers[i] = currentCycle + duration;
        }
    }
}