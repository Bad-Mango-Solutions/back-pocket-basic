// <copyright file="AppleIIeKeyTranslator.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Devices;

/// <summary>
/// Translates host keyboard keys into Apple IIe character codes while honoring modifier state.
/// </summary>
public sealed class AppleIIeKeyTranslator : IKeyTranslator
{
    /// <summary>
    /// Converts a host-neutral key and active modifiers into an Apple IIe ASCII code.
    /// </summary>
    /// <param name="key">The key to translate.</param>
    /// <param name="modifiers">The active keyboard modifiers.</param>
    /// <returns>The Apple IIe character code, or <see langword="null"/> if the key is not mapped.</returns>
    public byte? Translate(AppleIIeKey key, KeyboardModifiers modifiers)
    {
        return TranslateStatic(key, modifiers);
    }

    /// <summary>
    /// Translates the specified host-neutral key using the current Apple IIe mapping rules.
    /// </summary>
    /// <param name="key">The key to translate.</param>
    /// <param name="modifiers">The active keyboard modifiers.</param>
    /// <returns>The Apple IIe character code, or <see langword="null"/> if the key is not mapped.</returns>
    public static byte? TranslateStatic(AppleIIeKey key, KeyboardModifiers modifiers)
    {
        bool ctrl = modifiers.HasFlag(KeyboardModifiers.Control);
        bool shift = modifiers.HasFlag(KeyboardModifiers.Shift);

        if (ctrl)
        {
            return key switch
            {
                AppleIIeKey.A => 0x01,
                AppleIIeKey.B => 0x02,
                AppleIIeKey.C => 0x03,
                AppleIIeKey.D => 0x04,
                AppleIIeKey.E => 0x05,
                AppleIIeKey.F => 0x06,
                AppleIIeKey.G => 0x07,
                AppleIIeKey.H => 0x08,
                AppleIIeKey.I => 0x09,
                AppleIIeKey.J => 0x0A,
                AppleIIeKey.K => 0x0B,
                AppleIIeKey.L => 0x0C,
                AppleIIeKey.M => 0x0D,
                AppleIIeKey.N => 0x0E,
                AppleIIeKey.O => 0x0F,
                AppleIIeKey.P => 0x10,
                AppleIIeKey.Q => 0x11,
                AppleIIeKey.R => 0x12,
                AppleIIeKey.S => 0x13,
                AppleIIeKey.T => 0x14,
                AppleIIeKey.U => 0x15,
                AppleIIeKey.V => 0x16,
                AppleIIeKey.W => 0x17,
                AppleIIeKey.X => 0x18,
                AppleIIeKey.Y => 0x19,
                AppleIIeKey.Z => 0x1A,
                AppleIIeKey.OpenBracket => 0x1B,
                AppleIIeKey.Backslash => 0x1C,
                AppleIIeKey.CloseBracket => 0x1D,
                AppleIIeKey.Up => 0x1E,
                AppleIIeKey.Right => 0x1F,
                _ => null,
            };
        }

        return key switch
        {
            AppleIIeKey.Left => 0x08,
            AppleIIeKey.Right => 0x15,
            AppleIIeKey.Up => 0x0B,
            AppleIIeKey.Down => 0x0A,
            AppleIIeKey.Return => 0x0D,
            AppleIIeKey.Tab => 0x09,
            AppleIIeKey.Escape => 0x1B,
            AppleIIeKey.Space => 0x20,
            AppleIIeKey.Delete => 0x7F,
            AppleIIeKey.A => MapLetter('A', shift, modifiers.HasFlag(KeyboardModifiers.CapsLock)),
            AppleIIeKey.B => MapLetter('B', shift, modifiers.HasFlag(KeyboardModifiers.CapsLock)),
            AppleIIeKey.C => MapLetter('C', shift, modifiers.HasFlag(KeyboardModifiers.CapsLock)),
            AppleIIeKey.D => MapLetter('D', shift, modifiers.HasFlag(KeyboardModifiers.CapsLock)),
            AppleIIeKey.E => MapLetter('E', shift, modifiers.HasFlag(KeyboardModifiers.CapsLock)),
            AppleIIeKey.F => MapLetter('F', shift, modifiers.HasFlag(KeyboardModifiers.CapsLock)),
            AppleIIeKey.G => MapLetter('G', shift, modifiers.HasFlag(KeyboardModifiers.CapsLock)),
            AppleIIeKey.H => MapLetter('H', shift, modifiers.HasFlag(KeyboardModifiers.CapsLock)),
            AppleIIeKey.I => MapLetter('I', shift, modifiers.HasFlag(KeyboardModifiers.CapsLock)),
            AppleIIeKey.J => MapLetter('J', shift, modifiers.HasFlag(KeyboardModifiers.CapsLock)),
            AppleIIeKey.K => MapLetter('K', shift, modifiers.HasFlag(KeyboardModifiers.CapsLock)),
            AppleIIeKey.L => MapLetter('L', shift, modifiers.HasFlag(KeyboardModifiers.CapsLock)),
            AppleIIeKey.M => MapLetter('M', shift, modifiers.HasFlag(KeyboardModifiers.CapsLock)),
            AppleIIeKey.N => MapLetter('N', shift, modifiers.HasFlag(KeyboardModifiers.CapsLock)),
            AppleIIeKey.O => MapLetter('O', shift, modifiers.HasFlag(KeyboardModifiers.CapsLock)),
            AppleIIeKey.P => MapLetter('P', shift, modifiers.HasFlag(KeyboardModifiers.CapsLock)),
            AppleIIeKey.Q => MapLetter('Q', shift, modifiers.HasFlag(KeyboardModifiers.CapsLock)),
            AppleIIeKey.R => MapLetter('R', shift, modifiers.HasFlag(KeyboardModifiers.CapsLock)),
            AppleIIeKey.S => MapLetter('S', shift, modifiers.HasFlag(KeyboardModifiers.CapsLock)),
            AppleIIeKey.T => MapLetter('T', shift, modifiers.HasFlag(KeyboardModifiers.CapsLock)),
            AppleIIeKey.U => MapLetter('U', shift, modifiers.HasFlag(KeyboardModifiers.CapsLock)),
            AppleIIeKey.V => MapLetter('V', shift, modifiers.HasFlag(KeyboardModifiers.CapsLock)),
            AppleIIeKey.W => MapLetter('W', shift, modifiers.HasFlag(KeyboardModifiers.CapsLock)),
            AppleIIeKey.X => MapLetter('X', shift, modifiers.HasFlag(KeyboardModifiers.CapsLock)),
            AppleIIeKey.Y => MapLetter('Y', shift, modifiers.HasFlag(KeyboardModifiers.CapsLock)),
            AppleIIeKey.Z => MapLetter('Z', shift, modifiers.HasFlag(KeyboardModifiers.CapsLock)),
            AppleIIeKey.D0 => MapDigit(AppleIIeKey.D0, shift),
            AppleIIeKey.D1 => MapDigit(AppleIIeKey.D1, shift),
            AppleIIeKey.D2 => MapDigit(AppleIIeKey.D2, shift),
            AppleIIeKey.D3 => MapDigit(AppleIIeKey.D3, shift),
            AppleIIeKey.D4 => MapDigit(AppleIIeKey.D4, shift),
            AppleIIeKey.D5 => MapDigit(AppleIIeKey.D5, shift),
            AppleIIeKey.D6 => MapDigit(AppleIIeKey.D6, shift),
            AppleIIeKey.D7 => MapDigit(AppleIIeKey.D7, shift),
            AppleIIeKey.D8 => MapDigit(AppleIIeKey.D8, shift),
            AppleIIeKey.D9 => MapDigit(AppleIIeKey.D9, shift),
            AppleIIeKey.Minus => shift ? (byte)'_' : (byte)'-',
            AppleIIeKey.Plus => (byte)'+',
            AppleIIeKey.Equals => shift ? (byte)'+' : (byte)'=',
            AppleIIeKey.OpenBracket => shift ? (byte)'{' : (byte)'[',
            AppleIIeKey.CloseBracket => shift ? (byte)'}' : (byte)']',
            AppleIIeKey.Backslash => shift ? (byte)'|' : (byte)'\\',
            AppleIIeKey.Semicolon => shift ? (byte)':' : (byte)';',
            AppleIIeKey.Quote => shift ? (byte)'"' : (byte)'\'',
            AppleIIeKey.Comma => shift ? (byte)'<' : (byte)',',
            AppleIIeKey.Period => shift ? (byte)'>' : (byte)'.',
            AppleIIeKey.Slash => shift ? (byte)'?' : (byte)'/',
            AppleIIeKey.Backtick => shift ? (byte)'~' : (byte)'`',
            _ => null,
        };
    }

    public static byte? Translate(AppleIIeKey key, KeyboardModifiers modifiers) => TranslateStatic(key, modifiers);

    /// <summary>
    /// Alias for <see cref="Translate(AppleIIeKey, KeyboardModifiers)"/> for compatibility with the inlined mapping logic.
    /// </summary>
    /// <param name="key">The host-neutral key to translate.</param>
    /// <param name="modifiers">The active Apple II keyboard modifiers.</param>
    /// <returns>The translated Apple II character code, or <see langword="null"/> if the key is unmapped.</returns>
    public static byte? MapKeyToPocket2(AppleIIeKey key, KeyboardModifiers modifiers) => Translate(key, modifiers);

    private static byte MapLetter(char letter, bool shift, bool capsLock)
    {
        bool uppercase = shift ^ capsLock;
        return uppercase ? (byte)letter : (byte)(letter + 0x20);
    }

    private static byte MapDigit(AppleIIeKey key, bool shift)
    {
        if (!shift)
        {
            return key switch
            {
                AppleIIeKey.D0 => (byte)'0',
                AppleIIeKey.D1 => (byte)'1',
                AppleIIeKey.D2 => (byte)'2',
                AppleIIeKey.D3 => (byte)'3',
                AppleIIeKey.D4 => (byte)'4',
                AppleIIeKey.D5 => (byte)'5',
                AppleIIeKey.D6 => (byte)'6',
                AppleIIeKey.D7 => (byte)'7',
                AppleIIeKey.D8 => (byte)'8',
                AppleIIeKey.D9 => (byte)'9',
                _ => throw new ArgumentOutOfRangeException(nameof(key)),
            };
        }

        return key switch
        {
            AppleIIeKey.D1 => (byte)'!',
            AppleIIeKey.D2 => (byte)'@',
            AppleIIeKey.D3 => (byte)'#',
            AppleIIeKey.D4 => (byte)'$',
            AppleIIeKey.D5 => (byte)'%',
            AppleIIeKey.D6 => (byte)'^',
            AppleIIeKey.D7 => (byte)'&',
            AppleIIeKey.D8 => (byte)'*',
            AppleIIeKey.D9 => (byte)'(',
            AppleIIeKey.D0 => (byte)')',
            _ => throw new ArgumentOutOfRangeException(nameof(key)),
        };
    }
{
    /// <summary>
    /// Host-neutral Apple IIe key enumeration.
    /// </summary>
    public enum AppleIIeKey
    {
        /// <summary>
        /// No host key translated.
        /// </summary>
        None,

        /// <summary>
        /// The A key.
        /// </summary>
        A,

        /// <summary>
        /// The B key.
        /// </summary>
        B,

        /// <summary>
        /// The C key.
        /// </summary>
        C,

        /// <summary>
        /// The D key.
        /// </summary>
        D,

        /// <summary>
        /// The E key.
        /// </summary>
        E,

        /// <summary>
        /// The F key.
        /// </summary>
        F,

        /// <summary>
        /// The G key.
        /// </summary>
        G,

        /// <summary>
        /// The H key.
        /// </summary>
        H,

        /// <summary>
        /// The I key.
        /// </summary>
        I,

        /// <summary>
        /// The J key.
        /// </summary>
        J,

        /// <summary>
        /// The K key.
        /// </summary>
        K,

        /// <summary>
        /// The L key.
        /// </summary>
        L,

        /// <summary>
        /// The M key.
        /// </summary>
        M,

        /// <summary>
        /// The N key.
        /// </summary>
        N,

        /// <summary>
        /// The O key.
        /// </summary>
        O,

        /// <summary>
        /// The P key.
        /// </summary>
        P,

        /// <summary>
        /// The Q key.
        /// </summary>
        Q,

        /// <summary>
        /// The R key.
        /// </summary>
        R,

        /// <summary>
        /// The S key.
        /// </summary>
        S,

        /// <summary>
        /// The T key.
        /// </summary>
        T,

        /// <summary>
        /// The U key.
        /// </summary>
        U,

        /// <summary>
        /// The V key.
        /// </summary>
        V,

        /// <summary>
        /// The W key.
        /// </summary>
        W,

        /// <summary>
        /// The X key.
        /// </summary>
        X,

        /// <summary>
        /// The Y key.
        /// </summary>
        Y,

        /// <summary>
        /// The Z key.
        /// </summary>
        Z,

        /// <summary>
        /// The 0 key.
        /// </summary>
        D0,

        /// <summary>
        /// The 1 key.
        /// </summary>
        D1,

        /// <summary>
        /// The 2 key.
        /// </summary>
        D2,

        /// <summary>
        /// The 3 key.
        /// </summary>
        D3,

        /// <summary>
        /// The 4 key.
        /// </summary>
        D4,

        /// <summary>
        /// The 5 key.
        /// </summary>
        D5,

        /// <summary>
        /// The 6 key.
        /// </summary>
        D6,

        /// <summary>
        /// The 7 key.
        /// </summary>
        D7,

        /// <summary>
        /// The 8 key.
        /// </summary>
        D8,

        /// <summary>
        /// The 9 key.
        /// </summary>
        D9,

        /// <summary>
        /// The minus key.
        /// </summary>
        Minus,

        /// <summary>
        /// The plus key.
        /// </summary>
        Plus,

        /// <summary>
        /// The equals key.
        /// </summary>
        Equals,

        /// <summary>
        /// The open bracket key.
        /// </summary>
        OpenBracket,

        /// <summary>
        /// The close bracket key.
        /// </summary>
        CloseBracket,

        /// <summary>
        /// The backslash key.
        /// </summary>
        Backslash,

        /// <summary>
        /// The semicolon key.
        /// </summary>
        Semicolon,

        /// <summary>
        /// The quote key.
        /// </summary>
        Quote,

        /// <summary>
        /// The comma key.
        /// </summary>
        Comma,

        /// <summary>
        /// The period key.
        /// </summary>
        Period,

        /// <summary>
        /// The slash key.
        /// </summary>
        Slash,

        /// <summary>
        /// The backtick key.
        /// </summary>
        Backtick,

        /// <summary>
        /// The space bar.
        /// </summary>
        Space,

        /// <summary>
        /// The Return key.
        /// </summary>
        Return,

        /// <summary>
        /// The Tab key.
        /// </summary>
        Tab,

        /// <summary>
        /// The Escape key.
        /// </summary>
        Escape,

        /// <summary>
        /// The Delete key.
        /// </summary>
        Delete,

        /// <summary>
        /// The Left arrow key.
        /// </summary>
        Left,

        /// <summary>
        /// The Right arrow key.
        /// </summary>
        Right,

        /// <summary>
        /// The Up arrow key.
        /// </summary>
        Up,

        /// <summary>
        /// The Down arrow key.
        /// </summary>
        Down,
    }

    /// <summary>
    /// Converts a host-neutral key and active modifiers into an Apple IIe ASCII code.
    /// </summary>
    /// <param name="key">The key to translate.</param>
    /// <param name="modifiers">The active keyboard modifiers.</param>
    /// <returns>The Apple IIe character code, or <see langword="null"/> if the key is not mapped.</returns>
    public static byte? Translate(AppleIIeKey key, KeyboardModifiers modifiers)
    {
        bool ctrl = modifiers.HasFlag(KeyboardModifiers.Control);
        bool shift = modifiers.HasFlag(KeyboardModifiers.Shift);

        if (ctrl)
        {
            return key switch
            {
                AppleIIeKey.A => 0x01,
                AppleIIeKey.B => 0x02,
                AppleIIeKey.C => 0x03,
                AppleIIeKey.D => 0x04,
                AppleIIeKey.E => 0x05,
                AppleIIeKey.F => 0x06,
                AppleIIeKey.G => 0x07,
                AppleIIeKey.H => 0x08,
                AppleIIeKey.I => 0x09,
                AppleIIeKey.J => 0x0A,
                AppleIIeKey.K => 0x0B,
                AppleIIeKey.L => 0x0C,
                AppleIIeKey.M => 0x0D,
                AppleIIeKey.N => 0x0E,
                AppleIIeKey.O => 0x0F,
                AppleIIeKey.P => 0x10,
                AppleIIeKey.Q => 0x11,
                AppleIIeKey.R => 0x12,
                AppleIIeKey.S => 0x13,
                AppleIIeKey.T => 0x14,
                AppleIIeKey.U => 0x15,
                AppleIIeKey.V => 0x16,
                AppleIIeKey.W => 0x17,
                AppleIIeKey.X => 0x18,
                AppleIIeKey.Y => 0x19,
                AppleIIeKey.Z => 0x1A,
                AppleIIeKey.OpenBracket => 0x1B,
                AppleIIeKey.Backslash => 0x1C,
                AppleIIeKey.CloseBracket => 0x1D,
                AppleIIeKey.Up => 0x1E,
                AppleIIeKey.Right => 0x1F,
                _ => null,
            };
        }

        return key switch
        {
            AppleIIeKey.Left => 0x08,
            AppleIIeKey.Right => 0x15,
            AppleIIeKey.Up => 0x0B,
            AppleIIeKey.Down => 0x0A,
            AppleIIeKey.Return => 0x0D,
            AppleIIeKey.Tab => 0x09,
            AppleIIeKey.Escape => 0x1B,
            AppleIIeKey.Space => 0x20,
            AppleIIeKey.Delete => 0x7F,
            AppleIIeKey.A => MapLetter('A', shift, modifiers.HasFlag(KeyboardModifiers.CapsLock)),
            AppleIIeKey.B => MapLetter('B', shift, modifiers.HasFlag(KeyboardModifiers.CapsLock)),
            AppleIIeKey.C => MapLetter('C', shift, modifiers.HasFlag(KeyboardModifiers.CapsLock)),
            AppleIIeKey.D => MapLetter('D', shift, modifiers.HasFlag(KeyboardModifiers.CapsLock)),
            AppleIIeKey.E => MapLetter('E', shift, modifiers.HasFlag(KeyboardModifiers.CapsLock)),
            AppleIIeKey.F => MapLetter('F', shift, modifiers.HasFlag(KeyboardModifiers.CapsLock)),
            AppleIIeKey.G => MapLetter('G', shift, modifiers.HasFlag(KeyboardModifiers.CapsLock)),
            AppleIIeKey.H => MapLetter('H', shift, modifiers.HasFlag(KeyboardModifiers.CapsLock)),
            AppleIIeKey.I => MapLetter('I', shift, modifiers.HasFlag(KeyboardModifiers.CapsLock)),
            AppleIIeKey.J => MapLetter('J', shift, modifiers.HasFlag(KeyboardModifiers.CapsLock)),
            AppleIIeKey.K => MapLetter('K', shift, modifiers.HasFlag(KeyboardModifiers.CapsLock)),
            AppleIIeKey.L => MapLetter('L', shift, modifiers.HasFlag(KeyboardModifiers.CapsLock)),
            AppleIIeKey.M => MapLetter('M', shift, modifiers.HasFlag(KeyboardModifiers.CapsLock)),
            AppleIIeKey.N => MapLetter('N', shift, modifiers.HasFlag(KeyboardModifiers.CapsLock)),
            AppleIIeKey.O => MapLetter('O', shift, modifiers.HasFlag(KeyboardModifiers.CapsLock)),
            AppleIIeKey.P => MapLetter('P', shift, modifiers.HasFlag(KeyboardModifiers.CapsLock)),
            AppleIIeKey.Q => MapLetter('Q', shift, modifiers.HasFlag(KeyboardModifiers.CapsLock)),
            AppleIIeKey.R => MapLetter('R', shift, modifiers.HasFlag(KeyboardModifiers.CapsLock)),
            AppleIIeKey.S => MapLetter('S', shift, modifiers.HasFlag(KeyboardModifiers.CapsLock)),
            AppleIIeKey.T => MapLetter('T', shift, modifiers.HasFlag(KeyboardModifiers.CapsLock)),
            AppleIIeKey.U => MapLetter('U', shift, modifiers.HasFlag(KeyboardModifiers.CapsLock)),
            AppleIIeKey.V => MapLetter('V', shift, modifiers.HasFlag(KeyboardModifiers.CapsLock)),
            AppleIIeKey.W => MapLetter('W', shift, modifiers.HasFlag(KeyboardModifiers.CapsLock)),
            AppleIIeKey.X => MapLetter('X', shift, modifiers.HasFlag(KeyboardModifiers.CapsLock)),
            AppleIIeKey.Y => MapLetter('Y', shift, modifiers.HasFlag(KeyboardModifiers.CapsLock)),
            AppleIIeKey.Z => MapLetter('Z', shift, modifiers.HasFlag(KeyboardModifiers.CapsLock)),
            AppleIIeKey.D0 => MapDigit(AppleIIeKey.D0, shift),
            AppleIIeKey.D1 => MapDigit(AppleIIeKey.D1, shift),
            AppleIIeKey.D2 => MapDigit(AppleIIeKey.D2, shift),
            AppleIIeKey.D3 => MapDigit(AppleIIeKey.D3, shift),
            AppleIIeKey.D4 => MapDigit(AppleIIeKey.D4, shift),
            AppleIIeKey.D5 => MapDigit(AppleIIeKey.D5, shift),
            AppleIIeKey.D6 => MapDigit(AppleIIeKey.D6, shift),
            AppleIIeKey.D7 => MapDigit(AppleIIeKey.D7, shift),
            AppleIIeKey.D8 => MapDigit(AppleIIeKey.D8, shift),
            AppleIIeKey.D9 => MapDigit(AppleIIeKey.D9, shift),
            AppleIIeKey.Minus => shift ? (byte)'_' : (byte)'-',
            AppleIIeKey.Plus => (byte)'+',
            AppleIIeKey.Equals => shift ? (byte)'+' : (byte)'=',
            AppleIIeKey.OpenBracket => shift ? (byte)'{' : (byte)'[',
            AppleIIeKey.CloseBracket => shift ? (byte)'}' : (byte)']',
            AppleIIeKey.Backslash => shift ? (byte)'|' : (byte)'\\',
            AppleIIeKey.Semicolon => shift ? (byte)':' : (byte)';',
            AppleIIeKey.Quote => shift ? (byte)'"' : (byte)'\'',
            AppleIIeKey.Comma => shift ? (byte)'<' : (byte)',',
            AppleIIeKey.Period => shift ? (byte)'>' : (byte)'.',
            AppleIIeKey.Slash => shift ? (byte)'?' : (byte)'/',
            AppleIIeKey.Backtick => shift ? (byte)'~' : (byte)'`',
            _ => null,
        };
    }

    /// <summary>
    /// Alias for <see cref="Translate(AppleIIeKey, KeyboardModifiers)"/> for compatibility with the inlined mapping logic.
    /// </summary>
    /// <param name="key">The host-neutral key to translate.</param>
    /// <param name="modifiers">The active Apple II keyboard modifiers.</param>
    /// <returns>The translated Apple II character code, or <see langword="null"/> if the key is unmapped.</returns>
    public static byte? MapKeyToPocket2(AppleIIeKey key, KeyboardModifiers modifiers) => Translate(key, modifiers);

    private static byte MapLetter(char letter, bool shift, bool capsLock)
    {
        bool uppercase = shift ^ capsLock;
        return uppercase ? (byte)letter : (byte)(letter + 0x20);
    }

    private static byte MapDigit(AppleIIeKey key, bool shift)
    {
        if (!shift)
        {
            return key switch
            {
                AppleIIeKey.D0 => (byte)'0',
                AppleIIeKey.D1 => (byte)'1',
                AppleIIeKey.D2 => (byte)'2',
                AppleIIeKey.D3 => (byte)'3',
                AppleIIeKey.D4 => (byte)'4',
                AppleIIeKey.D5 => (byte)'5',
                AppleIIeKey.D6 => (byte)'6',
                AppleIIeKey.D7 => (byte)'7',
                AppleIIeKey.D8 => (byte)'8',
                AppleIIeKey.D9 => (byte)'9',
                _ => throw new ArgumentOutOfRangeException(nameof(key)),
            };
        }

        return key switch
        {
            AppleIIeKey.D1 => (byte)'!',
            AppleIIeKey.D2 => (byte)'@',
            AppleIIeKey.D3 => (byte)'#',
            AppleIIeKey.D4 => (byte)'$',
            AppleIIeKey.D5 => (byte)'%',
            AppleIIeKey.D6 => (byte)'^',
            AppleIIeKey.D7 => (byte)'&',
            AppleIIeKey.D8 => (byte)'*',
            AppleIIeKey.D9 => (byte)'(',
            AppleIIeKey.D0 => (byte)')',
            _ => throw new ArgumentOutOfRangeException(nameof(key)),
        };
    }
}