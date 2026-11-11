// <copyright file="KeyboardKey.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Devices;

/// <summary>
/// The shared key set used by all input sources and keyboard devices, not the host's keys.
/// </summary>
/// <remarks>
/// <para>
/// This is the Apple Extended Keyboard II-style key set, including machine-control keys such as
/// <see cref="KeyboardKey.Reset"/> and <see cref="KeyboardKey.Power"/>. Input sources map their
/// native keys to this enum before a machine-specific keyboard interprets them.
/// </para>
/// </remarks>
public enum KeyboardKey
{
    /// <summary>
    /// No key. Used as the default value; never produced by a real key press.
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
    /// The 0 key on the main digit row.
    /// </summary>
    D0,

    /// <summary>
    /// The 1 key on the main digit row.
    /// </summary>
    D1,

    /// <summary>
    /// The 2 key on the main digit row.
    /// </summary>
    D2,

    /// <summary>
    /// The 3 key on the main digit row.
    /// </summary>
    D3,

    /// <summary>
    /// The 4 key on the main digit row.
    /// </summary>
    D4,

    /// <summary>
    /// The 5 key on the main digit row.
    /// </summary>
    D5,

    /// <summary>
    /// The 6 key on the main digit row.
    /// </summary>
    D6,

    /// <summary>
    /// The 7 key on the main digit row.
    /// </summary>
    D7,

    /// <summary>
    /// The 8 key on the main digit row.
    /// </summary>
    D8,

    /// <summary>
    /// The 9 key on the main digit row.
    /// </summary>
    D9,

    /// <summary>
    /// The minus key (<c>-</c>, shifted <c>_</c>).
    /// </summary>
    Minus,

    /// <summary>
    /// The equals key (<c>=</c>, shifted <c>+</c>).
    /// </summary>
    Equals,

    /// <summary>
    /// The backtick key (<c>`</c>, shifted <c>~</c>).
    /// </summary>
    Backtick,

    /// <summary>
    /// The open bracket key (<c>[</c>, shifted <c>{</c>).
    /// </summary>
    OpenBracket,

    /// <summary>
    /// The close bracket key (<c>]</c>, shifted <c>}</c>).
    /// </summary>
    CloseBracket,

    /// <summary>
    /// The backslash key (<c>\</c>, shifted <c>|</c>).
    /// </summary>
    Backslash,

    /// <summary>
    /// The semicolon key (<c>;</c>, shifted <c>:</c>).
    /// </summary>
    Semicolon,

    /// <summary>
    /// The quote key (<c>'</c>, shifted <c>"</c>).
    /// </summary>
    Quote,

    /// <summary>
    /// The comma key (<c>,</c>, shifted <c>&lt;</c>).
    /// </summary>
    Comma,

    /// <summary>
    /// The period key (<c>.</c>, shifted <c>&gt;</c>).
    /// </summary>
    Period,

    /// <summary>
    /// The slash key (<c>/</c>, shifted <c>?</c>).
    /// </summary>
    Slash,

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
    /// The Backspace key.
    /// </summary>
    Backspace,

    /// <summary>
    /// The Delete key, modeled as the Apple Extended Keyboard II forward-delete key.
    /// </summary>
    ForwardDelete,

    /// <summary>
    /// The F1 key.
    /// </summary>
    F1,

    /// <summary>
    /// The F2 key.
    /// </summary>
    F2,

    /// <summary>
    /// The F3 key.
    /// </summary>
    F3,

    /// <summary>
    /// The F4 key.
    /// </summary>
    F4,

    /// <summary>
    /// The F5 key.
    /// </summary>
    F5,

    /// <summary>
    /// The F6 key.
    /// </summary>
    F6,

    /// <summary>
    /// The F7 key.
    /// </summary>
    F7,

    /// <summary>
    /// The F8 key.
    /// </summary>
    F8,

    /// <summary>
    /// The F9 key.
    /// </summary>
    F9,

    /// <summary>
    /// The F10 key.
    /// </summary>
    F10,

    /// <summary>
    /// The F11 key.
    /// </summary>
    F11,

    /// <summary>
    /// The F12 key.
    /// </summary>
    F12,

    /// <summary>
    /// The F13 key.
    /// </summary>
    F13,

    /// <summary>
    /// The F14 key.
    /// </summary>
    F14,

    /// <summary>
    /// The F15 key.
    /// </summary>
    F15,

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
    /// The Home key.
    /// </summary>
    Home,

    /// <summary>
    /// The End key.
    /// </summary>
    End,

    /// <summary>
    /// The Page Up key.
    /// </summary>
    PageUp,

    /// <summary>
    /// The Page Down key.
    /// </summary>
    PageDown,

    /// <summary>
    /// The Help key.
    /// </summary>
    Help,

    /// <summary>
    /// The 0 key on the keypad.
    /// </summary>
    Keypad0,

    /// <summary>
    /// The 1 key on the keypad.
    /// </summary>
    Keypad1,

    /// <summary>
    /// The 2 key on the keypad.
    /// </summary>
    Keypad2,

    /// <summary>
    /// The 3 key on the keypad.
    /// </summary>
    Keypad3,

    /// <summary>
    /// The 4 key on the keypad.
    /// </summary>
    Keypad4,

    /// <summary>
    /// The 5 key on the keypad.
    /// </summary>
    Keypad5,

    /// <summary>
    /// The 6 key on the keypad.
    /// </summary>
    Keypad6,

    /// <summary>
    /// The 7 key on the keypad.
    /// </summary>
    Keypad7,

    /// <summary>
    /// The 8 key on the keypad.
    /// </summary>
    Keypad8,

    /// <summary>
    /// The 9 key on the keypad.
    /// </summary>
    Keypad9,

    /// <summary>
    /// The keypad decimal point key.
    /// </summary>
    KeypadDecimal,

    /// <summary>
    /// The keypad Enter key.
    /// </summary>
    KeypadEnter,

    /// <summary>
    /// The keypad plus key.
    /// </summary>
    KeypadPlus,

    /// <summary>
    /// The keypad minus key.
    /// </summary>
    KeypadMinus,

    /// <summary>
    /// The keypad multiply key.
    /// </summary>
    KeypadMultiply,

    /// <summary>
    /// The keypad divide key.
    /// </summary>
    KeypadDivide,

    /// <summary>
    /// The keypad equals key.
    /// </summary>
    KeypadEquals,

    /// <summary>
    /// The keypad clear key.
    /// </summary>
    KeypadClear,

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
    /// The left Option key.
    /// </summary>
    LeftOption,

    /// <summary>
    /// The right Option key.
    /// </summary>
    RightOption,

    /// <summary>
    /// The left Command key.
    /// </summary>
    LeftCommand,

    /// <summary>
    /// The right Command key.
    /// </summary>
    RightCommand,

    /// <summary>
    /// The Caps Lock key.
    /// </summary>
    CapsLock,

    /// <summary>
    /// The machine reset key.
    /// </summary>
    Reset,

    /// <summary>
    /// The power key.
    /// </summary>
    Power,
}
