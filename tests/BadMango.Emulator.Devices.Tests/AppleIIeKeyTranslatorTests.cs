// <copyright file="AppleIIeKeyTranslatorTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Devices.Tests;

/// <summary>
/// Table-driven tests for <see cref="AppleIIeKeyTranslator"/>: every host key the US-layout Apple IIe
/// keyboard produces, under every relevant modifier combination.
/// </summary>
[TestFixture]
public sealed class AppleIIeKeyTranslatorTests
{
    /// <summary>
    /// Digit and punctuation keys with their unshifted and shifted US-layout legends.
    /// </summary>
    private static readonly (HostKey Key, char Unshifted, char Shifted)[] PrintableKeys =
    [
        (HostKey.D1, '1', '!'),
        (HostKey.D2, '2', '@'),
        (HostKey.D3, '3', '#'),
        (HostKey.D4, '4', '$'),
        (HostKey.D5, '5', '%'),
        (HostKey.D6, '6', '^'),
        (HostKey.D7, '7', '&'),
        (HostKey.D8, '8', '*'),
        (HostKey.D9, '9', '('),
        (HostKey.D0, '0', ')'),
        (HostKey.Minus, '-', '_'),
        (HostKey.Equals, '=', '+'),
        (HostKey.OpenBracket, '[', '{'),
        (HostKey.CloseBracket, ']', '}'),
        (HostKey.Backslash, '\\', '|'),
        (HostKey.Semicolon, ';', ':'),
        (HostKey.Quote, '\'', '"'),
        (HostKey.Comma, ',', '<'),
        (HostKey.Period, '.', '>'),
        (HostKey.Slash, '/', '?'),
        (HostKey.Backtick, '`', '~'),
    ];

    /// <summary>
    /// Special keys and the code each produces regardless of Shift, Caps Lock and Control.
    /// </summary>
    private static readonly (HostKey Key, byte Code)[] SpecialKeys =
    [
        (HostKey.Left, 0x08),
        (HostKey.Right, 0x15),
        (HostKey.Up, 0x0B),
        (HostKey.Down, 0x0A),
        (HostKey.Return, 0x0D),
        (HostKey.Tab, 0x09),
        (HostKey.Escape, 0x1B),
        (HostKey.Space, 0x20),
        (HostKey.Backspace, 0x08),
        (HostKey.Delete, 0x7F),
    ];

    /// <summary>
    /// Modifier keys, which never produce a code themselves.
    /// </summary>
    private static readonly HostKey[] ModifierKeys =
    [
        HostKey.None,
        HostKey.LeftShift,
        HostKey.RightShift,
        HostKey.LeftControl,
        HostKey.RightControl,
        HostKey.LeftAlt,
        HostKey.RightAlt,
        HostKey.CapsLock,
    ];

    private static readonly KeyboardModifiers[] AppleKeyCombinations =
    [
        KeyboardModifiers.OpenApple,
        KeyboardModifiers.ClosedApple,
        KeyboardModifiers.OpenApple | KeyboardModifiers.ClosedApple,
    ];

    private readonly IKeyTranslator translator = new AppleIIeKeyTranslator();

    /// <summary>
    /// Verifies every letter under all four Shift and Caps Lock combinations. Exactly one of Shift
    /// and Caps Lock gives uppercase; neither or both give lowercase (current PC-style behavior).
    /// </summary>
    /// <param name="key">The letter key.</param>
    /// <param name="modifiers">The active modifiers.</param>
    /// <param name="expected">The expected character.</param>
    [TestCaseSource(nameof(LetterCases))]
    public void Translate_Letter_FollowsShiftXorCapsLock(HostKey key, KeyboardModifiers modifiers, char expected)
    {
        Assert.That(translator.Translate(key, modifiers), Is.EqualTo((byte)expected));
    }

    /// <summary>
    /// Verifies every digit and punctuation key, unshifted and shifted, with and without Caps Lock.
    /// Caps Lock must not change any of them (for example Shift+1 is always <c>!</c>).
    /// </summary>
    /// <param name="key">The digit or punctuation key.</param>
    /// <param name="modifiers">The active modifiers.</param>
    /// <param name="expected">The expected character.</param>
    [TestCaseSource(nameof(PrintableCases))]
    public void Translate_DigitOrPunctuation_UsesUsLegendAndIgnoresCapsLock(HostKey key, KeyboardModifiers modifiers, char expected)
    {
        Assert.That(translator.Translate(key, modifiers), Is.EqualTo((byte)expected));
    }

    /// <summary>
    /// Verifies that Ctrl+A..Z give <c>$01</c>-<c>$1A</c> no matter what Shift and Caps Lock do.
    /// </summary>
    /// <param name="key">The letter key.</param>
    /// <param name="modifiers">The active modifiers, always including Control.</param>
    /// <param name="expected">The expected control code.</param>
    [TestCaseSource(nameof(ControlLetterCases))]
    public void Translate_ControlLetter_GivesControlCodeIgnoringShiftAndCapsLock(HostKey key, KeyboardModifiers modifiers, int expected)
    {
        Assert.That(translator.Translate(key, modifiers), Is.EqualTo((byte)expected));
    }

    /// <summary>
    /// Verifies the control codes above <c>$1A</c> and Ctrl+@.
    /// </summary>
    /// <param name="key">The punctuation or digit key.</param>
    /// <param name="modifiers">The active modifiers.</param>
    /// <param name="expected">The expected control code.</param>
    [TestCase(HostKey.OpenBracket, KeyboardModifiers.Control, 0x1B, TestName = "Ctrl+[ is $1B")]
    [TestCase(HostKey.Backslash, KeyboardModifiers.Control, 0x1C, TestName = "Ctrl+\\ is $1C")]
    [TestCase(HostKey.CloseBracket, KeyboardModifiers.Control, 0x1D, TestName = "Ctrl+] is $1D")]
    [TestCase(HostKey.D2, KeyboardModifiers.Control | KeyboardModifiers.Shift, 0x00, TestName = "Ctrl+@ (Shift+2) is $00")]
    [TestCase(HostKey.D6, KeyboardModifiers.Control | KeyboardModifiers.Shift, 0x1E, TestName = "Ctrl+^ (Shift+6) is $1E")]
    [TestCase(HostKey.Minus, KeyboardModifiers.Control | KeyboardModifiers.Shift, 0x1F, TestName = "Ctrl+_ (Shift+Minus) is $1F")]
    [TestCase(HostKey.OpenBracket, KeyboardModifiers.Control | KeyboardModifiers.Shift, 0x1B, TestName = "Ctrl+Shift+[ is still $1B")]
    [TestCase(HostKey.D2, KeyboardModifiers.Control | KeyboardModifiers.Shift | KeyboardModifiers.CapsLock, 0x00, TestName = "Ctrl+@ ignores Caps Lock")]
    public void Translate_ControlPunctuation_GivesControlCode(HostKey key, KeyboardModifiers modifiers, int expected)
    {
        Assert.That(translator.Translate(key, modifiers), Is.EqualTo((byte)expected));
    }

    /// <summary>
    /// Verifies that Ctrl with a printable key that has no Apple IIe control code produces nothing.
    /// In particular there are no invented codes for Ctrl+arrow keys.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="modifiers">The active modifiers.</param>
    [TestCase(HostKey.D1, KeyboardModifiers.Control)]
    [TestCase(HostKey.D2, KeyboardModifiers.Control)]
    [TestCase(HostKey.D6, KeyboardModifiers.Control)]
    [TestCase(HostKey.Minus, KeyboardModifiers.Control)]
    [TestCase(HostKey.Equals, KeyboardModifiers.Control)]
    [TestCase(HostKey.Semicolon, KeyboardModifiers.Control)]
    [TestCase(HostKey.Slash, KeyboardModifiers.Control | KeyboardModifiers.Shift)]
    public void Translate_ControlWithUnmappedPrintable_ReturnsNull(HostKey key, KeyboardModifiers modifiers)
    {
        Assert.That(translator.Translate(key, modifiers), Is.Null);
    }

    /// <summary>
    /// Verifies special keys produce their code under every Shift, Caps Lock and Control combination.
    /// </summary>
    /// <param name="key">The special key.</param>
    /// <param name="modifiers">The active modifiers.</param>
    /// <param name="expected">The expected code.</param>
    [TestCaseSource(nameof(SpecialCases))]
    public void Translate_SpecialKey_IgnoresModifiers(HostKey key, KeyboardModifiers modifiers, int expected)
    {
        Assert.That(translator.Translate(key, modifiers), Is.EqualTo((byte)expected));
    }

    /// <summary>
    /// Verifies that Open Apple and Closed Apple, alone or together, never change the code of any key.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="baseModifiers">The modifiers without the Apple keys.</param>
    /// <param name="appleKeys">The Apple keys added.</param>
    [TestCaseSource(nameof(AppleKeyCases))]
    public void Translate_AppleKeys_NeverChangeTheCode(HostKey key, KeyboardModifiers baseModifiers, KeyboardModifiers appleKeys)
    {
        Assert.That(translator.Translate(key, baseModifiers | appleKeys), Is.EqualTo(translator.Translate(key, baseModifiers)));
    }

    /// <summary>
    /// Verifies that modifier keys and <see cref="HostKey.None"/> produce no code under any modifiers.
    /// </summary>
    /// <param name="key">The modifier key.</param>
    [TestCaseSource(nameof(ModifierKeys))]
    public void Translate_ModifierKey_ReturnsNull(HostKey key)
    {
        Assert.Multiple(() =>
        {
            foreach (var modifiers in AllShiftCapsControlCombinations())
            {
                Assert.That(translator.Translate(key, modifiers), Is.Null, $"{key} with {modifiers}");
            }
        });
    }

    /// <summary>
    /// Verifies that every host key is either translated or deliberately unmapped, so adding a
    /// <see cref="HostKey"/> member without deciding its Apple IIe code fails this test.
    /// </summary>
    [Test]
    public void Translate_EveryHostKey_IsCoveredByTheTables()
    {
        var covered = new HashSet<HostKey>(ModifierKeys);
        covered.UnionWith(PrintableKeys.Select(p => p.Key));
        covered.UnionWith(SpecialKeys.Select(s => s.Key));
        covered.UnionWith(Enumerable.Range(0, 26).Select(i => HostKey.A + i));

        Assert.That(covered, Is.EquivalentTo(Enum.GetValues<HostKey>()));
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
            var key = HostKey.A + i;
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
            var key = HostKey.A + i;
            foreach (var modifiers in AllShiftCapsControlCombinations().Where(m => m.HasFlag(KeyboardModifiers.Control)))
            {
                yield return new TestCaseData(key, modifiers, i + 1);
            }
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
        var keys = Enum.GetValues<HostKey>();
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