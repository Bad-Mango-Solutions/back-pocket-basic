// <copyright file="AppleIIeKeyTranslator.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Devices;

/// <summary>
/// Translates physical host keys into Apple IIe keyboard codes for a US layout.
/// </summary>
/// <remarks>
/// <para>
/// The result is the 7-bit code the IIe keyboard encoder latches into <c>$C000</c>:
/// </para>
/// <list type="bullet">
/// <item><description>Letters: uppercase when exactly one of Shift and Caps Lock is active, lowercase otherwise.</description></item>
/// <item><description>Digits and punctuation: the US-layout shifted or unshifted legend. Caps Lock has no effect.</description></item>
/// <item><description>
/// Control: Ctrl+A..Z give <c>$01</c>-<c>$1A</c>; Ctrl+[ <c>$1B</c>, Ctrl+\ <c>$1C</c>, Ctrl+] <c>$1D</c>,
/// Ctrl+2 (@) <c>$00</c>, Ctrl+6 (^) <c>$1E</c> and Ctrl+Minus (_) <c>$1F</c>, all regardless of Shift
/// and Caps Lock. Control does not change any other key, so for example Ctrl+1 is <c>1</c> and
/// Ctrl+Shift+1 is <c>!</c>. This follows the Apple IIe Technical Reference Manual, Table 2-3.
/// </description></item>
/// <item><description>
/// Special keys ignore Shift, Caps Lock and Ctrl: Left <c>$08</c>, Right <c>$15</c>, Up <c>$0B</c>,
/// Down <c>$0A</c>, Return <c>$0D</c>, Tab <c>$09</c>, Escape <c>$1B</c>, Space <c>$20</c>,
/// Backspace <c>$08</c> and Forward Delete <c>$7F</c>.
/// </description></item>
/// <item><description>
/// Open Apple and Closed Apple never change the code; they are only visible as the pushbuttons
/// at <c>$C061</c>/<c>$C062</c>. Modifier keys themselves produce no code.
/// </description></item>
/// </list>
/// </remarks>
public sealed class AppleIIeKeyTranslator : IKeyTranslator
{
    private const byte AsciiDelete = 0x7F;

    /// <inheritdoc />
    public byte? Translate(KeyboardKey key, KeyboardModifiers modifiers)
    {
        bool shift = modifiers.HasFlag(KeyboardModifiers.Shift);

        byte? special = TranslateSpecial(key);
        if (special.HasValue)
        {
            return special;
        }

        if (modifiers.HasFlag(KeyboardModifiers.Control))
        {
            byte? control = TranslateControl(key);
            if (control.HasValue)
            {
                return control;
            }
        }

        if (key is >= KeyboardKey.A and <= KeyboardKey.Z)
        {
            bool uppercase = shift ^ modifiers.HasFlag(KeyboardModifiers.CapsLock);
            byte upper = (byte)('A' + (key - KeyboardKey.A));
            return uppercase ? upper : (byte)(upper + 0x20);
        }

        return TranslatePrintable(key, shift);
    }

    private static byte? TranslateSpecial(KeyboardKey key)
    {
        return key switch
        {
            KeyboardKey.Left => 0x08,
            KeyboardKey.Right => 0x15,
            KeyboardKey.Up => 0x0B,
            KeyboardKey.Down => 0x0A,
            KeyboardKey.Return => 0x0D,
            KeyboardKey.Tab => 0x09,
            KeyboardKey.Escape => 0x1B,
            KeyboardKey.Space => 0x20,
            KeyboardKey.Backspace => 0x08,
            KeyboardKey.ForwardDelete => AsciiDelete,
            _ => null,
        };
    }

    private static byte? TranslateControl(KeyboardKey key)
    {
        if (key is >= KeyboardKey.A and <= KeyboardKey.Z)
        {
            return (byte)(0x01 + (key - KeyboardKey.A));
        }

        return key switch
        {
            KeyboardKey.OpenBracket => 0x1B,
            KeyboardKey.Backslash => 0x1C,
            KeyboardKey.CloseBracket => 0x1D,
            KeyboardKey.D2 => 0x00,
            KeyboardKey.D6 => 0x1E,
            KeyboardKey.Minus => 0x1F,
            _ => null,
        };
    }

    private static byte? TranslatePrintable(KeyboardKey key, bool shift)
    {
        (char Unshifted, char Shifted)? legend = key switch
        {
            KeyboardKey.D1 => ('1', '!'),
            KeyboardKey.D2 => ('2', '@'),
            KeyboardKey.D3 => ('3', '#'),
            KeyboardKey.D4 => ('4', '$'),
            KeyboardKey.D5 => ('5', '%'),
            KeyboardKey.D6 => ('6', '^'),
            KeyboardKey.D7 => ('7', '&'),
            KeyboardKey.D8 => ('8', '*'),
            KeyboardKey.D9 => ('9', '('),
            KeyboardKey.D0 => ('0', ')'),
            KeyboardKey.Minus => ('-', '_'),
            KeyboardKey.Equals => ('=', '+'),
            KeyboardKey.OpenBracket => ('[', '{'),
            KeyboardKey.CloseBracket => (']', '}'),
            KeyboardKey.Backslash => ('\\', '|'),
            KeyboardKey.Semicolon => (';', ':'),
            KeyboardKey.Quote => ('\'', '"'),
            KeyboardKey.Comma => (',', '<'),
            KeyboardKey.Period => ('.', '>'),
            KeyboardKey.Slash => ('/', '?'),
            KeyboardKey.Backtick => ('`', '~'),
            _ => null,
        };

        if (legend is not { } pair)
        {
            return null;
        }

        return (byte)(shift ? pair.Shifted : pair.Unshifted);
    }
}