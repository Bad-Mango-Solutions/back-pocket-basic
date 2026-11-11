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
/// Backspace <c>$08</c> and Delete <c>$7F</c>.
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
    public byte? Translate(HostKey key, KeyboardModifiers modifiers)
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

        if (key is >= HostKey.A and <= HostKey.Z)
        {
            bool uppercase = shift ^ modifiers.HasFlag(KeyboardModifiers.CapsLock);
            byte upper = (byte)('A' + (key - HostKey.A));
            return uppercase ? upper : (byte)(upper + 0x20);
        }

        return TranslatePrintable(key, shift);
    }

    private static byte? TranslateSpecial(HostKey key)
    {
        return key switch
        {
            HostKey.Left => 0x08,
            HostKey.Right => 0x15,
            HostKey.Up => 0x0B,
            HostKey.Down => 0x0A,
            HostKey.Return => 0x0D,
            HostKey.Tab => 0x09,
            HostKey.Escape => 0x1B,
            HostKey.Space => 0x20,
            HostKey.Backspace => 0x08,
            HostKey.Delete => AsciiDelete,
            _ => null,
        };
    }

    private static byte? TranslateControl(HostKey key)
    {
        if (key is >= HostKey.A and <= HostKey.Z)
        {
            return (byte)(0x01 + (key - HostKey.A));
        }

        return key switch
        {
            HostKey.OpenBracket => 0x1B,
            HostKey.Backslash => 0x1C,
            HostKey.CloseBracket => 0x1D,
            HostKey.D2 => 0x00,
            HostKey.D6 => 0x1E,
            HostKey.Minus => 0x1F,
            _ => null,
        };
    }

    private static byte? TranslatePrintable(HostKey key, bool shift)
    {
        (char Unshifted, char Shifted)? legend = key switch
        {
            HostKey.D1 => ('1', '!'),
            HostKey.D2 => ('2', '@'),
            HostKey.D3 => ('3', '#'),
            HostKey.D4 => ('4', '$'),
            HostKey.D5 => ('5', '%'),
            HostKey.D6 => ('6', '^'),
            HostKey.D7 => ('7', '&'),
            HostKey.D8 => ('8', '*'),
            HostKey.D9 => ('9', '('),
            HostKey.D0 => ('0', ')'),
            HostKey.Minus => ('-', '_'),
            HostKey.Equals => ('=', '+'),
            HostKey.OpenBracket => ('[', '{'),
            HostKey.CloseBracket => (']', '}'),
            HostKey.Backslash => ('\\', '|'),
            HostKey.Semicolon => (';', ':'),
            HostKey.Quote => ('\'', '"'),
            HostKey.Comma => (',', '<'),
            HostKey.Period => ('.', '>'),
            HostKey.Slash => ('/', '?'),
            HostKey.Backtick => ('`', '~'),
            _ => null,
        };

        if (legend is not { } pair)
        {
            return null;
        }

        return (byte)(shift ? pair.Shifted : pair.Unshifted);
    }
}