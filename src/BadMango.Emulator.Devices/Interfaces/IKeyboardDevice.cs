// <copyright file="IKeyboardDevice.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Devices.Interfaces;

using BadMango.Emulator.Bus.Interfaces;

/// <summary>
/// Keyboard device interface for host input injection.
/// </summary>
/// <remarks>
/// <para>
/// This interface defines the host-facing API for the keyboard device,
/// allowing the emulator frontend to inject key events into the emulated system.
/// </para>
/// <para>
/// The Apple II keyboard presents a simple interface to software:
/// </para>
/// <list type="bullet">
/// <item><description>$C000: Keyboard data (bit 7 = strobe, bits 6-0 = ASCII)</description></item>
/// <item><description>$C010: Any key down flag / clear strobe</description></item>
/// </list>
/// </remarks>
public interface IKeyboardDevice : IMotherboardDevice, IResetSource
{
    /// <summary>
    /// Gets a value indicating whether a key is currently held down.
    /// </summary>
    /// <value><see langword="true"/> if a key is pressed; otherwise, <see langword="false"/>.</value>
    bool HasKeyDown { get; }

    /// <summary>
    /// Gets the current key data register value (bit 7 = strobe).
    /// </summary>
    /// <value>The keyboard data register value with high bit set if strobe is active.</value>
    byte KeyData { get; }

    /// <summary>
    /// Gets the current modifier key state.
    /// </summary>
    /// <value>The active modifier keys.</value>
    KeyboardModifiers Modifiers { get; }

    /// <summary>
    /// Gets the translator that turns physical host keys into this keyboard's key codes.
    /// </summary>
    /// <value>The key translator owned by this keyboard.</value>
    /// <remarks>
    /// Input sources such as the video window use this to translate a <see cref="HostKey"/> and the
    /// active modifiers before calling <see cref="KeyDown"/>, so they never construct or choose a
    /// translator themselves. This is interim: the planned per-machine keyboard input layer will
    /// accept host keys directly and keep the translator private to the keyboard.
    /// </remarks>
    IKeyTranslator Translator { get; }

    /// <summary>
    /// Injects a key press from the host.
    /// </summary>
    /// <param name="asciiCode">The ASCII code of the key (high bit cleared).</param>
    /// <remarks>
    /// Sets the keyboard data register to the specified value with the high bit
    /// (strobe) set, and marks a key as being held down.
    /// </remarks>
    void KeyDown(byte asciiCode);

    /// <summary>
    /// Releases the current key.
    /// </summary>
    /// <remarks>
    /// Clears the key-down state but leaves the keyboard data register unchanged
    /// until the strobe is cleared by reading $C010.
    /// </remarks>
    void KeyUp();

    /// <summary>
    /// Sets the modifier key state.
    /// </summary>
    /// <param name="modifiers">The new modifier state.</param>
    void SetModifiers(KeyboardModifiers modifiers);

    /// <summary>
    /// Injects a string of characters as sequential key presses.
    /// </summary>
    /// <param name="text">The text to inject.</param>
    /// <param name="delayMs">How long each key is held down, in milliseconds of emulated time.</param>
    /// <remarks>
    /// <para>
    /// Characters are converted to their Apple II ASCII equivalents and queued. Unsupported
    /// characters are skipped. Typing runs entirely on scheduler events in emulated time, so it
    /// is deterministic and safe to call from any thread:
    /// </para>
    /// <list type="number">
    /// <item><description>Key down: the code is latched with the strobe set and the any-key-down flag set.</description></item>
    /// <item><description>Key up, <paramref name="delayMs"/> later: the any-key-down flag clears.</description></item>
    /// <item><description>
    /// An inter-key gap follows, so the key-up is observable before the next key goes down. If
    /// software has not yet cleared the strobe for the previous key, the next key waits, so
    /// type-ahead is never overwritten. Clearing the strobe never starts the next key by itself.
    /// </description></item>
    /// </list>
    /// <para>
    /// The default of 50 ms simulates typical typing speed and allows software to process each key
    /// before the next arrives.
    /// </para>
    /// </remarks>
    void TypeString(string text, int delayMs = 50);
}