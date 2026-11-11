// <copyright file="AppleIIeKeyTranslatorTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Devices.Tests;

using BadMango.Emulator.Devices;

/// <summary>
/// Tests the host-neutral Apple IIe keyboard translator.
/// </summary>
[TestFixture]
public sealed class AppleIIeKeyTranslatorTests
{
    /// <summary>
    /// Verifies letter output against Shift and Caps Lock combinations.
    /// </summary>
    /// <param name="letter">The letter key to translate.</param>
    /// <param name="shift">Whether Shift is active.</param>
    /// <param name="capsLock">Whether Caps Lock is active.</param>
    /// <param name="expected">The expected Apple II ASCII code.</param>
    [TestCase('a', false, false, (byte)'a')]
    [TestCase('a', true, false, (byte)'A')]
    [TestCase('a', false, true, (byte)'A')]
    [TestCase('a', true, true, (byte)'a')]
    [TestCase('z', false, false, (byte)'z')]
    [TestCase('z', true, false, (byte)'Z')]
    [TestCase('z', false, true, (byte)'Z')]
    [TestCase('z', true, true, (byte)'z')]
    public void Translate_Letters_RespectsShiftAndCapsLock(
        char letter,
        bool shift,
        bool capsLock,
        byte expected)
    {
        var modifiers = KeyboardModifiers.None;
        if (shift)
        {
            modifiers |= KeyboardModifiers.Shift;
        }

        if (capsLock)
        {
            modifiers |= KeyboardModifiers.CapsLock;
        }

        var actual = AppleIIeKeyTranslator.MapKeyToPocket2(
            letter switch
            {
                'a' => AppleIIeKeyTranslator.AppleIIeKey.A,
                'z' => AppleIIeKeyTranslator.AppleIIeKey.Z,
                _ => throw new ArgumentOutOfRangeException(nameof(letter)),
            },
            modifiers);

        Assert.That(actual, Is.EqualTo(expected));
    }

    /// <summary>
    /// Verifies the digit and punctuation mappings are unaffected by Caps Lock.
    /// </summary>
    /// <param name="key">The key to translate.</param>
    /// <param name="modifiers">The active modifier flags.</param>
    /// <param name="expected">The expected Apple II ASCII code.</param>
    [TestCase(AppleIIeKeyTranslator.AppleIIeKey.D1, KeyboardModifiers.None, (byte)'1')]
    [TestCase(AppleIIeKeyTranslator.AppleIIeKey.D1, KeyboardModifiers.Shift, (byte)'!')]
    [TestCase(AppleIIeKeyTranslator.AppleIIeKey.D2, KeyboardModifiers.None, (byte)'2')]
    [TestCase(AppleIIeKeyTranslator.AppleIIeKey.D2, KeyboardModifiers.Shift, (byte)'@')]
    [TestCase(AppleIIeKeyTranslator.AppleIIeKey.D0, KeyboardModifiers.None, (byte)'0')]
    [TestCase(AppleIIeKeyTranslator.AppleIIeKey.D0, KeyboardModifiers.Shift, (byte)')')]
    public void Translate_DigitsAndPunctuation_UsesShiftAndIgnoresCapsLock(
        AppleIIeKeyTranslator.AppleIIeKey key,
        KeyboardModifiers modifiers,
        byte expected)
    {
        var withCaps = modifiers | KeyboardModifiers.CapsLock;
        Assert.Multiple(() =>
        {
            Assert.That(AppleIIeKeyTranslator.MapKeyToPocket2(key, modifiers), Is.EqualTo(expected));
            Assert.That(AppleIIeKeyTranslator.MapKeyToPocket2(key, withCaps), Is.EqualTo(expected));
        });
    }

    /// <summary>
    /// Verifies Ctrl-based control-code mappings ignore Shift and Caps Lock.
    /// </summary>
    /// <param name="key">The key to translate.</param>
    /// <param name="modifiers">The active modifier flags.</param>
    /// <param name="expected">The expected control code.</param>
    [TestCase(AppleIIeKeyTranslator.AppleIIeKey.A, KeyboardModifiers.Control, 0x01)]
    [TestCase(AppleIIeKeyTranslator.AppleIIeKey.Z, KeyboardModifiers.Control, 0x1A)]
    [TestCase(AppleIIeKeyTranslator.AppleIIeKey.OpenBracket, KeyboardModifiers.Control, 0x1B)]
    [TestCase(AppleIIeKeyTranslator.AppleIIeKey.Backslash, KeyboardModifiers.Control, 0x1C)]
    [TestCase(AppleIIeKeyTranslator.AppleIIeKey.CloseBracket, KeyboardModifiers.Control, 0x1D)]
    [TestCase(AppleIIeKeyTranslator.AppleIIeKey.Up, KeyboardModifiers.Control, 0x1E)]
    [TestCase(AppleIIeKeyTranslator.AppleIIeKey.Right, KeyboardModifiers.Control, 0x1F)]
    public void Translate_ControlCombinations_UsesAsciiControlCodes(
        AppleIIeKeyTranslator.AppleIIeKey key,
        KeyboardModifiers modifiers,
        byte expected)
    {
        Assert.That(AppleIIeKeyTranslator.MapKeyToPocket2(key, modifiers), Is.EqualTo(expected));
        Assert.That(
            AppleIIeKeyTranslator.MapKeyToPocket2(key, modifiers | KeyboardModifiers.Shift | KeyboardModifiers.CapsLock),
            Is.EqualTo(expected));
    }

    /// <summary>
    /// Verifies special key mappings.
    /// </summary>
    /// <param name="key">The key to translate.</param>
    /// <param name="expected">The expected ASCII code.</param>
    [TestCase(AppleIIeKeyTranslator.AppleIIeKey.Left, 0x08)]
    [TestCase(AppleIIeKeyTranslator.AppleIIeKey.Right, 0x15)]
    [TestCase(AppleIIeKeyTranslator.AppleIIeKey.Up, 0x0B)]
    [TestCase(AppleIIeKeyTranslator.AppleIIeKey.Down, 0x0A)]
    [TestCase(AppleIIeKeyTranslator.AppleIIeKey.Return, 0x0D)]
    [TestCase(AppleIIeKeyTranslator.AppleIIeKey.Tab, 0x09)]
    [TestCase(AppleIIeKeyTranslator.AppleIIeKey.Escape, 0x1B)]
    [TestCase(AppleIIeKeyTranslator.AppleIIeKey.Space, 0x20)]
    [TestCase(AppleIIeKeyTranslator.AppleIIeKey.Delete, 0x7F)]
    public void Translate_SpecialKeys_MapsExpectedCodes(
        AppleIIeKeyTranslator.AppleIIeKey key,
        byte expected)
    {
        Assert.That(AppleIIeKeyTranslator.MapKeyToPocket2(key, KeyboardModifiers.None), Is.EqualTo(expected));
    }

    /// <summary>
    /// Verifies Apple modifier keys do not change the character code.
    /// </summary>
    /// <param name="modifiers">The active Apple II modifier bits.</param>
    /// <param name="expected">The expected letter code.</param>
    [TestCase(KeyboardModifiers.OpenApple, 0x61)]
    [TestCase(KeyboardModifiers.ClosedApple, 0x61)]
    [TestCase(KeyboardModifiers.OpenApple | KeyboardModifiers.ClosedApple, 0x61)]
    public void Translate_AppleModifiers_DoesNotChangeCharacterCode(KeyboardModifiers modifiers, byte expected)
    {
        Assert.That(AppleIIeKeyTranslator.MapKeyToPocket2(AppleIIeKeyTranslator.AppleIIeKey.A, modifiers), Is.EqualTo(expected));
    }
}