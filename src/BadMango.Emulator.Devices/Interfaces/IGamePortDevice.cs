// <copyright file="IGamePortDevice.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Devices.Interfaces;

using BadMango.Emulator.Bus.Interfaces;

/// <summary>
/// Game I/O device interface for joystick and paddle input.
/// </summary>
/// <remarks>
/// <para>
/// This interface defines the host-facing API for the game port device,
/// allowing the emulator frontend to inject joystick and paddle input.
/// </para>
/// <para>
/// The Apple II game port provides:
/// </para>
/// <list type="bullet">
/// <item><description>4 analog inputs (paddles 0-3) read via timing loops after triggering $C070</description></item>
/// <item><description>3 digital pushbuttons at $C061-$C063 (bit 7 = pressed)</description></item>
/// <item><description>Button 0 is shared with Open Apple key</description></item>
/// <item><description>Button 1 is shared with Closed Apple key</description></item>
/// </list>
/// <para>
/// For joystick input, paddle 0 maps to X axis and paddle 1 maps to Y axis.
/// The second joystick (if present) uses paddles 2 and 3.
/// </para>
/// </remarks>
public interface IGamePortDevice : IMotherboardDevice
{
    /// <summary>
    /// Gets the current state of pushbuttons (0-3).
    /// </summary>
    /// <value>A read-only list of button states where <see langword="true"/> means pressed.</value>
    IReadOnlyList<bool> Buttons { get; }

    /// <summary>
    /// Gets the current paddle positions (0-3), range 0-255.
    /// </summary>
    /// <value>A read-only list of paddle analog values from 0 (left/up) to 255 (right/down).</value>
    IReadOnlyList<byte> Paddles { get; }

    /// <summary>
    /// Sets a pushbutton state.
    /// </summary>
    /// <param name="button">Button index (0-3).</param>
    /// <param name="pressed"><see langword="true"/> if pressed; otherwise, <see langword="false"/>.</param>
    /// <exception cref="ArgumentOutOfRangeException">Button index is out of range 0-3.</exception>
    void SetButton(int button, bool pressed);

    /// <summary>
    /// Sets a paddle position.
    /// </summary>
    /// <param name="paddle">Paddle index (0-3).</param>
    /// <param name="position">Position value (0-255).</param>
    /// <exception cref="ArgumentOutOfRangeException">Paddle index is out of range 0-3.</exception>
    void SetPaddle(int paddle, byte position);

    /// <summary>
    /// Sets joystick position (maps to paddles 0-1).
    /// </summary>
    /// <param name="x">X axis (-1.0 to 1.0, where -1 is left and 1 is right).</param>
    /// <param name="y">Y axis (-1.0 to 1.0, where -1 is up and 1 is down).</param>
    /// <remarks>
    /// <para>
    /// This is a convenience method that converts normalized joystick coordinates
    /// to the Apple II paddle format. The x value maps to paddle 0 and y to paddle 1.
    /// </para>
    /// <para>
    /// Values outside the -1.0 to 1.0 range are clamped.
    /// </para>
    /// </remarks>
    void SetJoystick(float x, float y);
}