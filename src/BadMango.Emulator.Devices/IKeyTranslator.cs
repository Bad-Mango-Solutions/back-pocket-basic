// <copyright file="IKeyTranslator.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Devices;

/// <summary>
/// Translates a host-neutral keyboard key into a machine key code under the current modifier state.
/// </summary>
/// <remarks>
/// This abstraction intentionally avoids any Avalonia or platform-specific key types so that
/// different machine profiles (for example IIe, IIc or IIGS) can supply their own mapping rules.
/// </remarks>
public interface IKeyTranslator
{
    /// <summary>
    /// Maps the specified host-neutral key and active modifiers to the machine's key code.
    /// </summary>
    /// <param name="key">The host-neutral key to translate.</param>
    /// <param name="modifiers">The active keyboard modifiers, including Caps Lock.</param>
    /// <returns>The translated key code, or <see langword="null"/> if the key is not mapped.</returns>
    byte? Translate(AppleIIeKeyTranslator.AppleIIeKey key, KeyboardModifiers modifiers);
}
