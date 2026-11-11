// <copyright file="IKeyTranslator.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Devices;

/// <summary>
/// Translates a physical host key and the active modifiers into an emulated machine's key code.
/// </summary>
/// <remarks>
/// <para>
/// This is a building block for machine keyboards that, like the Apple IIe and IIc, present
/// software with a single character code per key press. It is not a contract every machine must
/// fit: a keyboard that reports raw key codes (such as the Apple IIgs ADB keyboard) does its own
/// translation.
/// </para>
/// <para>
/// Implementations hold no UI-framework types and no mutable state, so one instance may be
/// shared by any number of machines. Keyboard layouts (US, Dvorak, international) are separate
/// translator implementations or configurations selected by the machine profile.
/// </para>
/// </remarks>
public interface IKeyTranslator
{
    /// <summary>
    /// Maps a physical host key and the active modifiers to the machine's key code.
    /// </summary>
    /// <param name="key">The physical host key that went down.</param>
    /// <param name="modifiers">The active modifiers, including the Caps Lock state.</param>
    /// <returns>The machine key code, or <see langword="null"/> if the key produces no code.</returns>
    byte? Translate(KeyboardKey key, KeyboardModifiers modifiers);
}