// <copyright file="HostKey.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Devices;

/// <summary>
/// A physical key on a modern host keyboard, independent of any UI framework and of any emulated machine.
/// </summary>
/// <remarks>
/// <para>
/// Input sources (the video window, the debugger REPL, an agent host) describe key presses with
/// <see cref="HostKey"/> values plus <see cref="KeyboardModifiers"/>. Each emulated machine's
/// keyboard turns them into its own codes, for example through an <see cref="IKeyTranslator"/>.
/// </para>
/// <para>
/// Values name physical key positions using US-layout legends. Layout differences (Dvorak,
/// international keyboards) are the translator's concern, not the input source's.
/// </para>
/// </remarks>
public enum HostKey
{
    /// <summary>
    /// No key. Used as the default value; never produced by a real key press.
    /// </summary>
    None,

    /// <summary>
    /// The A letter key.
    /// </summary>
    A,

    /// <summary>
    /// The B letter key.
    /// </summary>
    B,

    /// <summary>
    /// The C letter key.
    /// </summary>
    C,

    /// <summary>
    /// The D letter key.
    /// </summary>
    D,

    /// <summary>
    /// The E letter key.
    /// </summary>
    E,

    /// <summary>
    /// The F letter key.
    /// </summary>
    F,

    /// <summary>
    /// The G letter key.
    /// </summary>
    G,

    /// <summary>
    /// The H letter key.
    /// </summary>
    H,

    /// <summary>
    /// The I letter key.
    /// </summary>
    I,

    /// <summary>
    /// The J letter key.
    /// </summary>
    J,

    /// <summary>
    /// The K letter key.
    /// </summary>
    K,

    /// <summary>
    /// The L letter key.
    /// </summary>
    L,

    /// <summary>
    /// The M letter key.
    /// </summary>
    M,

    /// <summary>
    /// The N letter key.
    /// </summary>
    N,

    /// <summary>
    /// The O letter key.
    /// </summary>
    O,

    /// <summary>
    /// The P letter key.
    /// </summary>
    P,

    /// <summary>
    /// The Q letter key.
    /// </summary>
    Q,

    /// <summary>
    /// The R letter key.
    /// </summary>
    R,

    /// <summary>
    /// The S letter key.
    /// </summary>
    S,

    /// <summary>
    /// The T letter key.
    /// </summary>
    T,

    /// <summary>
    /// The U letter key.
    /// </summary>
    U,

    /// <summary>
    /// The V letter key.
    /// </summary>
    V,

    /// <summary>
    /// The W letter key.
    /// </summary>
    W,

    /// <summary>
    /// The X letter key.
    /// </summary>
    X,

    /// <summary>
    /// The Y letter key.
    /// </summary>
    Y,

    /// <summary>
    /// The Z letter key.
    /// </summary>
    Z,

    /// <summary>
    /// The 0 key on the main (non-keypad) digit row.
    /// </summary>
    D0,

    /// <summary>
    /// The 1 key on the main (non-keypad) digit row.
    /// </summary>
    D1,

    /// <summary>
    /// The 2 key on the main (non-keypad) digit row.
    /// </summary>
    D2,

    /// <summary>
    /// The 3 key on the main (non-keypad) digit row.
    /// </summary>
    D3,

    /// <summary>
    /// The 4 key on the main (non-keypad) digit row.
    /// </summary>
    D4,

    /// <summary>
    /// The 5 key on the main (non-keypad) digit row.
    /// </summary>
    D5,

    /// <summary>
    /// The 6 key on the main (non-keypad) digit row.
    /// </summary>
    D6,

    /// <summary>
    /// The 7 key on the main (non-keypad) digit row.
    /// </summary>
    D7,

    /// <summary>
    /// The 8 key on the main (non-keypad) digit row.
    /// </summary>
    D8,

    /// <summary>
    /// The 9 key on the main (non-keypad) digit row.
    /// </summary>
    D9,

    /// <summary>
    /// The minus key (<c>-</c>, shifted <c>_</c> on a US layout).
    /// </summary>
    Minus,

    /// <summary>
    /// The equals key (<c>=</c>, shifted <c>+</c> on a US layout).
    /// </summary>
    Equals,

    /// <summary>
    /// The open bracket key (<c>[</c>, shifted <c>{</c> on a US layout).
    /// </summary>
    OpenBracket,

    /// <summary>
    /// The close bracket key (<c>]</c>, shifted <c>}</c> on a US layout).
    /// </summary>
    CloseBracket,

    /// <summary>
    /// The backslash key (<c>\</c>, shifted <c>|</c> on a US layout).
    /// </summary>
    Backslash,

    /// <summary>
    /// The semicolon key (<c>;</c>, shifted <c>:</c> on a US layout).
    /// </summary>
    Semicolon,

    /// <summary>
    /// The quote key (<c>'</c>, shifted <c>"</c> on a US layout).
    /// </summary>
    Quote,

    /// <summary>
    /// The comma key (<c>,</c>, shifted <c>&lt;</c> on a US layout).
    /// </summary>
    Comma,

    /// <summary>
    /// The period key (<c>.</c>, shifted <c>&gt;</c> on a US layout).
    /// </summary>
    Period,

    /// <summary>
    /// The slash key (<c>/</c>, shifted <c>?</c> on a US layout).
    /// </summary>
    Slash,

    /// <summary>
    /// The backtick key (<c>`</c>, shifted <c>~</c> on a US layout).
    /// </summary>
    Backtick,

    /// <summary>
    /// The space bar.
    /// </summary>
    Space,

    /// <summary>
    /// The Return (Enter) key.
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
    /// The Backspace key.
    /// </summary>
    Backspace,

    /// <summary>
    /// The Delete (forward delete) key.
    /// </summary>
    Delete,

    /// <summary>
    /// The left arrow key.
    /// </summary>
    Left,

    /// <summary>
    /// The right arrow key.
    /// </summary>
    Right,

    /// <summary>
    /// The up arrow key.
    /// </summary>
    Up,

    /// <summary>
    /// The down arrow key.
    /// </summary>
    Down,

    /// <summary>
    /// The left Shift key.
    /// </summary>
    LeftShift,

    /// <summary>
    /// The right Shift key.
    /// </summary>
    RightShift,

    /// <summary>
    /// The left Control key.
    /// </summary>
    LeftControl,

    /// <summary>
    /// The right Control key.
    /// </summary>
    RightControl,

    /// <summary>
    /// The left Alt (Option) key. Hosts map it to Open Apple on Apple II machines.
    /// </summary>
    LeftAlt,

    /// <summary>
    /// The right Alt (Option) key. Hosts map it to Closed Apple (Option on the IIgs) on Apple II machines.
    /// </summary>
    RightAlt,

    /// <summary>
    /// The Caps Lock key.
    /// </summary>
    CapsLock,
}