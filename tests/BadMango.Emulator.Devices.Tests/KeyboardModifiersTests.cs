// <copyright file="KeyboardModifiersTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Devices.Tests;

/// <summary>
/// Unit tests for the <see cref="KeyboardModifiers"/> enumeration.
/// </summary>
[TestFixture]
public class KeyboardModifiersTests
{
    /// <summary>
    /// Verifies that KeyboardModifiers is a flags enum.
    /// </summary>
    [Test]
    public void KeyboardModifiers_IsFlagsEnum()
    {
        Assert.That(typeof(KeyboardModifiers).IsDefined(typeof(FlagsAttribute), false), Is.True);
    }

    /// <summary>
    /// Verifies that None value is 0.
    /// </summary>
    [Test]
    public void KeyboardModifiers_None_IsZero()
    {
        Assert.That((int)KeyboardModifiers.None, Is.EqualTo(0));
    }

    /// <summary>
    /// Verifies that Shift value is defined.
    /// </summary>
    [Test]
    public void KeyboardModifiers_HasShiftValue()
    {
        Assert.That(Enum.IsDefined(typeof(KeyboardModifiers), KeyboardModifiers.Shift), Is.True);
        Assert.That((int)KeyboardModifiers.Shift, Is.EqualTo(1));
    }

    /// <summary>
    /// Verifies that Control value is defined.
    /// </summary>
    [Test]
    public void KeyboardModifiers_HasControlValue()
    {
        Assert.That(Enum.IsDefined(typeof(KeyboardModifiers), KeyboardModifiers.Control), Is.True);
        Assert.That((int)KeyboardModifiers.Control, Is.EqualTo(2));
    }

    /// <summary>
    /// Verifies that OpenApple value is defined.
    /// </summary>
    [Test]
    public void KeyboardModifiers_HasOpenAppleValue()
    {
        Assert.That(Enum.IsDefined(typeof(KeyboardModifiers), KeyboardModifiers.OpenApple), Is.True);
        Assert.That((int)KeyboardModifiers.OpenApple, Is.EqualTo(4));
    }

    /// <summary>
    /// Verifies that ClosedApple value is defined.
    /// </summary>
    [Test]
    public void KeyboardModifiers_HasClosedAppleValue()
    {
        Assert.That(Enum.IsDefined(typeof(KeyboardModifiers), KeyboardModifiers.ClosedApple), Is.True);
        Assert.That((int)KeyboardModifiers.ClosedApple, Is.EqualTo(8));
    }

    /// <summary>
    /// Verifies that CapsLock value is defined.
    /// </summary>
    [Test]
    public void KeyboardModifiers_HasCapsLockValue()
    {
        Assert.That(Enum.IsDefined(typeof(KeyboardModifiers), KeyboardModifiers.CapsLock), Is.True);
        Assert.That((int)KeyboardModifiers.CapsLock, Is.EqualTo(16));
    }

    /// <summary>
    /// Verifies that modifiers can be combined.
    /// </summary>
    [Test]
    public void KeyboardModifiers_CanBeCombined()
    {
        var combined = KeyboardModifiers.Shift | KeyboardModifiers.Control;
        Assert.That(combined.HasFlag(KeyboardModifiers.Shift), Is.True);
        Assert.That(combined.HasFlag(KeyboardModifiers.Control), Is.True);
        Assert.That(combined.HasFlag(KeyboardModifiers.OpenApple), Is.False);
    }
}