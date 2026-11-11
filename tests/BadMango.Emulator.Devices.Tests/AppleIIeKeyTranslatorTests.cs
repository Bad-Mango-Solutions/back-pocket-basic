// <copyright file="AppleIIeKeyTranslatorTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Devices.Tests;

/// <summary>
/// Table-driven tests for <see cref="AppleIIeKeyTranslator"/>: every <see cref="KeyboardKey"/> has an explicit
/// code or deliberate <see langword="null"/> result.
/// </summary>
[TestFixture]
public sealed class AppleIIeKeyTranslatorTests
{
    private static readonly (KeyboardKey Key, char Unshifted, char Shifted)[] PrintableKeys =
    [
        (KeyboardKey.D1, '1', '!'),
        (KeyboardKey.D2, '2', '@'),
        (KeyboardKey.D3, '3', '#'),
        (KeyboardKey.D4, '4', '$'),
        (KeyboardKey.D5, '5', '%'),
        (KeyboardKey.D6, '6', '^'),
        (KeyboardKey.D7, '7', '&'),
        (KeyboardKey.D8, '8', '*'),
        (KeyboardKey.D9, '9', '('),
        (KeyboardKey.D0, '0', ')'),
        (KeyboardKey.Minus, '-', '_'),
        (KeyboardKey.Equals, '=', '+'),
        (KeyboardKey.OpenBracket, '[', '{'),
        (KeyboardKey.CloseBracket, ']', '}'),
        (KeyboardKey.Backslash, '\', '|'),
        (KeyboardKey.Semicolon, ';', ':'),
        (KeyboardKey.Quote, ''', '"'),
        (KeyboardKey.Comma, ',', '<'),
        (KeyboardKey.Period, '.', '>'),
        (KeyboardKey.Slash, '/', '?'),
        (KeyboardKey.Backtick, '`', '~'),
    ];

    private static readonly (KeyboardKey Key, byte Code)[] SpecialKeys =
    [
        (KeyboardKey.Left, 0x08),
        (KeyboardKey.Right, 0x15),
        (KeyboardKey.Up, 0x0B),
        (KeyboardKey.Down, 0x0A),
        (KeyboardKey.Return, 0x0D),
        (KeyboardKey.Tab, 0x09),
        (KeyboardKey.Escape, 0x1B),
        (KeyboardKey.Space, 0x20),
        (KeyboardKey.Backspace, 0x08),
        (KeyboardKey.ForwardDelete, 0x7F),
    ];

    private static readonly KeyboardKey[] ModifierKeys =
    [
        KeyboardKey.None,
        KeyboardKey.LeftShift,
        KeyboardKey.RightShift,
        KeyboardKey.LeftControl,
        KeyboardKey.RightControl,
        KeyboardKey.LeftOption,
        KeyboardKey.RightOption,
        KeyboardKey.LeftCommand,
        KeyboardKey.RightCommand,
        KeyboardKey.CapsLock,
        KeyboardKey.F1, KeyboardKey.F2, KeyboardKey.F3, KeyboardKey.F4, KeyboardKey.F5, KeyboardKey.F6, KeyboardKey.F7, KeyboardKey.F8, KeyboardKey.F9, KeyboardKey.F10, KeyboardKey.F11, KeyboardKey.F12, KeyboardKey.F13, KeyboardKey.F14, KeyboardKey.F15,
        KeyboardKey.Home,
        KeyboardKey.End,
        KeyboardKey.PageUp,
        KeyboardKey.PageDown,
        KeyboardKey.Help,
        KeyboardKey.Keypad0, KeyboardKey.Keypad1, KeyboardKey.Keypad2, KeyboardKey.Keypad3, KeyboardKey.Keypad4, KeyboardKey.Keypad5, KeyboardKey.Keypad6, KeyboardKey.Keypad7, KeyboardKey.Keypad8, KeyboardKey.Keypad9,
        KeyboardKey.KeypadDecimal,
        KeyboardKey.KeypadEnter,
        KeyboardKey.KeypadPlus,
        KeyboardKey.KeypadMinus,
        KeyboardKey.KeypadMultiply,
        KeyboardKey.KeypadDivide,
        KeyboardKey.KeypadEquals,
        KeyboardKey.KeypadClear,
        KeyboardKey.Reset,
        KeyboardKey.Power,
    ];

    private static readonly KeyboardModifiers[] AppleKeyCombinations =
    [
        KeyboardModifiers.OpenApple,
        KeyboardModifiers.ClosedApple,
        KeyboardModifiers.OpenApple | KeyboardModifiers.ClosedApple,
    ];

    private readonly IKeyTranslator translator = new AppleIIeKeyTranslator();

    [TestCaseSource(nameof(LetterCases))]
    public void Translate_Letter_FollowsShiftXorCapsLock(KeyboardKey key, KeyboardModifiers modifiers, char expected)
    {
        Assert.That(translator.Translate(key, modifiers), Is.EqualTo((byte)expected));
    }

    [TestCaseSource(nameof(PrintableCases))]
    public void Translate_DigitOrPunctuation_UsesUsLegendAndIgnoresCapsLock(KeyboardKey key, KeyboardModifiers modifiers, char expected)
    {
        Assert.That(translator.Translate(key, modifiers), Is.EqualTo((byte)expected));
    }

    [TestCaseSource(nameof(ControlLetterCases))]
    public void Translate_ControlLetter_GivesControlCodeIgnoringShiftAndCapsLock(KeyboardKey key, KeyboardModifiers modifiers, int expected)
    {
        Assert.That(translator.Translate(key, modifiers), Is.EqualTo((byte)expected));
    }

    [TestCaseSource(nameof(ControlPunctuationCases))]
    public void Translate_ControlPunctuation_GivesControlCode(KeyboardKey key, KeyboardModifiers modifiers, int expected)
    {
        Assert.That(translator.Translate(key, modifiers), Is.EqualTo((byte)expected));
    }

    [TestCaseSource(nameof(ControlUnchangedCases))]
    public void Translate_ControlWithOtherPrintable_LeavesCodeUnchanged(KeyboardKey key, KeyboardModifiers modifiers, char expected)
    {
        Assert.That(translator.Translate(key, modifiers), Is.EqualTo((byte)expected));
    }

    [TestCaseSource(nameof(SpecialCases))]
    public void Translate_SpecialKey_IgnoresModifiers(KeyboardKey key, KeyboardModifiers modifiers, int expected)
    {
        Assert.That(translator.Translate(key, modifiers), Is.EqualTo((byte)expected));
    }

    [TestCaseSource(nameof(AppleKeyCases))]
    public void Translate_AppleKeys_NeverChangeTheCode(KeyboardKey key, KeyboardModifiers baseModifiers, KeyboardModifiers appleKeys)
    {
        Assert.That(translator.Translate(key, baseModifiers | appleKeys), Is.EqualTo(translator.Translate(key, baseModifiers)));
    }

    [TestCaseSource(nameof(ModifierKeys))]
    public void Translate_ModifierKey_ReturnsNull(KeyboardKey key)
    {
        Assert.Multiple(() =>
        {
            foreach (var modifiers in AllShiftCapsControlCombinations())
            {
                Assert.That(translator.Translate(key, modifiers), Is.Null, $"{key} with {modifiers}");
            }
        });
    }

    [Test]
    public void Translate_EveryKeyboardKey_IsCoveredByTheTables()
    {
        var covered = new HashSet<KeyboardKey>(ModifierKeys);
        covered.UnionWith(PrintableKeys.Select(p => p.Key));
        covered.UnionWith(SpecialKeys.Select(s => s.Key));
        covered.UnionWith(Enumerable.Range(0, 26).Select(i => KeyboardKey.A + i));
        Assert.That(covered, Is.EquivalentTo(Enum.GetValues<KeyboardKey>()));
    }

    private static IEnumerable<KeyboardModifiers> AllShiftCapsControlCombinations()
    {
        foreach (var shift in new[] { KeyboardModifiers.None, KeyboardModifiers.Shift })
        {
            foreach (var caps in new[] { KeyboardModifiers.None, KeyboardModifiers.CapsLock })
            {
                foreach (var ctrl in new[] { KeyboardModifiers.None, KeyboardModifiers.Control })
                {
                    yield return shift | caps | ctrl;
                }
            }
        }
    }

    private static IEnumerable<TestCaseData> LetterCases()
    {
        for (int i = 0; i < 26; i++)
        {
            var key = KeyboardKey.A + i;
            char upper = (char)('A' + i);
            char lower = char.ToLowerInvariant(upper);
            yield return new TestCaseData(key, KeyboardModifiers.None, lower);
            yield return new TestCaseData(key, KeyboardModifiers.Shift, upper);
            yield return new TestCaseData(key, KeyboardModifiers.CapsLock, upper);
            yield return new TestCaseData(key, KeyboardModifiers.Shift | KeyboardModifiers.CapsLock, lower);
        }
    }

    private static IEnumerable<TestCaseData> PrintableCases()
    {
        foreach (var (key, unshifted, shifted) in PrintableKeys)
        {
            yield return new TestCaseData(key, KeyboardModifiers.None, unshifted);
            yield return new TestCaseData(key, KeyboardModifiers.CapsLock, unshifted);
            yield return new TestCaseData(key, KeyboardModifiers.Shift, shifted);
            yield return new TestCaseData(key, KeyboardModifiers.Shift | KeyboardModifiers.CapsLock, shifted);
        }
    }

    private static IEnumerable<TestCaseData> ControlLetterCases()
    {
        for (int i = 0; i < 26; i++)
        {
            var key = KeyboardKey.A + i;
            foreach (var modifiers in AllShiftCapsControlCombinations().Where(m => m.HasFlag(KeyboardModifiers.Control)))
            {
                yield return new TestCaseData(key, modifiers, i + 1);
            }
        }
    }

    private static IEnumerable<TestCaseData> ControlPunctuationCases()
    {
        (KeyboardKey Key, int Code)[] controlKeys =
        [
            (KeyboardKey.OpenBracket, 0x1B),
            (KeyboardKey.Backslash, 0x1C),
            (KeyboardKey.CloseBracket, 0x1D),
            (KeyboardKey.D2, 0x00),
            (KeyboardKey.D6, 0x1E),
            (KeyboardKey.Minus, 0x1F),
        ];

        foreach (var (key, code) in controlKeys)
        {
            foreach (var modifiers in AllShiftCapsControlCombinations().Where(m => m.HasFlag(KeyboardModifiers.Control)))
            {
                yield return new TestCaseData(key, modifiers, code);
            }
        }
    }

    private static IEnumerable<TestCaseData> ControlUnchangedCases()
    {
        var controlKeys = new HashSet<KeyboardKey> { KeyboardKey.OpenBracket, KeyboardKey.Backslash, KeyboardKey.CloseBracket, KeyboardKey.D2, KeyboardKey.D6, KeyboardKey.Minus };
        foreach (var (key, unshifted, shifted) in PrintableKeys.Where(p => !controlKeys.Contains(p.Key)))
        {
            yield return new TestCaseData(key, KeyboardModifiers.Control, unshifted);
            yield return new TestCaseData(key, KeyboardModifiers.Control | KeyboardModifiers.CapsLock, unshifted);
            yield return new TestCaseData(key, KeyboardModifiers.Control | KeyboardModifiers.Shift, shifted);
            yield return new TestCaseData(key, KeyboardModifiers.Control | KeyboardModifiers.Shift | KeyboardModifiers.CapsLock, shifted);
        }
    }

    private static IEnumerable<TestCaseData> SpecialCases()
    {
        foreach (var (key, code) in SpecialKeys)
        {
            foreach (var modifiers in AllShiftCapsControlCombinations())
            {
                yield return new TestCaseData(key, modifiers, (int)code);
            }
        }
    }

    private static IEnumerable<TestCaseData> AppleKeyCases()
    {
        var keys = Enum.GetValues<KeyboardKey>();
        foreach (var key in keys)
        {
            foreach (var baseModifiers in AllShiftCapsControlCombinations())
            {
                foreach (var appleKeys in AppleKeyCombinations)
                {
                    yield return new TestCaseData(key, baseModifiers, appleKeys);
                }
            }
        }
    }
}
