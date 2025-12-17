// <copyright file="BasicStringTests.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Tests;

using ApplesoftBasic.Interpreter.Emulation;

/// <summary>
/// Contains unit tests for the <see cref="BasicString"/> struct.
/// </summary>
[TestFixture]
public class BasicStringTests
{
    #region Creation Tests

    /// <summary>
    /// Verifies that BasicString correctly stores a simple string.
    /// </summary>
    [Test]
    public void FromString_SimpleString_StoresCorrectly()
    {
        BasicString value = "HELLO";

        Assert.That(value.Length, Is.EqualTo(5));
        Assert.That(value.IsEmpty, Is.False);
        Assert.That(value.ToString(), Is.EqualTo("HELLO"));
    }

    /// <summary>
    /// Verifies that BasicString correctly handles empty string.
    /// </summary>
    [Test]
    public void FromString_EmptyString_StoresCorrectly()
    {
        BasicString value = string.Empty;

        Assert.That(value.Length, Is.EqualTo(0));
        Assert.That(value.IsEmpty, Is.True);
        Assert.That(value.ToString(), Is.EqualTo(string.Empty));
    }

    /// <summary>
    /// Verifies that BasicString correctly handles null.
    /// </summary>
    [Test]
    public void FromString_Null_ReturnsEmpty()
    {
        BasicString value = BasicString.FromString(null!);

        Assert.That(value.IsEmpty, Is.True);
    }

    /// <summary>
    /// Verifies that BasicString masks high bit for 7-bit ASCII.
    /// </summary>
    [Test]
    public void FromString_HighBitChars_MaskedTo7Bit()
    {
        // Character 0x80 should be masked to 0x00
        string input = "\u0080\u00FF";
        BasicString value = input;
        byte[] bytes = value.ToBytes();

        Assert.That(bytes[0], Is.EqualTo(0x00)); // 0x80 & 0x7F = 0x00
        Assert.That(bytes[1], Is.EqualTo(0x7F)); // 0xFF & 0x7F = 0x7F
    }

    /// <summary>
    /// Verifies that BasicString rejects strings over 255 characters.
    /// </summary>
    [Test]
    public void FromString_TooLong_ThrowsException()
    {
        string tooLong = new string('A', 256);

        Assert.Throws<ArgumentException>(() => BasicString.FromString(tooLong));
    }

    /// <summary>
    /// Verifies that BasicString accepts max length string.
    /// </summary>
    [Test]
    public void FromString_MaxLength_AcceptsCorrectly()
    {
        string maxLength = new string('A', 255);
        BasicString value = maxLength;

        Assert.That(value.Length, Is.EqualTo(255));
    }

    #endregion

    #region Byte Conversion Tests

    /// <summary>
    /// Verifies that ToBytes returns correct ASCII bytes.
    /// </summary>
    [Test]
    public void ToBytes_ReturnsAsciiBytes()
    {
        BasicString value = "ABC";
        byte[] bytes = value.ToBytes();

        Assert.That(bytes.Length, Is.EqualTo(3));
        Assert.That(bytes[0], Is.EqualTo(0x41)); // 'A'
        Assert.That(bytes[1], Is.EqualTo(0x42)); // 'B'
        Assert.That(bytes[2], Is.EqualTo(0x43)); // 'C'
    }

    /// <summary>
    /// Verifies that FromBytes reads ASCII bytes correctly.
    /// </summary>
    [Test]
    public void FromBytes_ReadsCorrectly()
    {
        byte[] bytes = new byte[] { 0x41, 0x42, 0x43 };
        BasicString value = BasicString.FromBytes(bytes);

        Assert.That(value.ToString(), Is.EqualTo("ABC"));
    }

    /// <summary>
    /// Verifies round-trip through bytes.
    /// </summary>
    [Test]
    public void ByteRoundTrip_PreservesValue()
    {
        BasicString original = "HELLO WORLD";
        byte[] bytes = original.ToBytes();
        BasicString restored = BasicString.FromBytes(bytes);

        Assert.That(restored.ToString(), Is.EqualTo("HELLO WORLD"));
    }

    /// <summary>
    /// Verifies that FromBytes with null returns empty.
    /// </summary>
    [Test]
    public void FromBytes_Null_ReturnsEmpty()
    {
        BasicString result = BasicString.FromBytes(null!);
        Assert.That(result.IsEmpty, Is.True);
    }

    /// <summary>
    /// Verifies that FromBytes with too long array throws.
    /// </summary>
    [Test]
    public void FromBytes_TooLong_ThrowsException()
    {
        byte[] tooLong = new byte[256];
        Assert.Throws<ArgumentException>(() => BasicString.FromBytes(tooLong));
    }

    /// <summary>
    /// Verifies that FromRawBytes doesn't mask bytes.
    /// </summary>
    [Test]
    public void FromRawBytes_DoesNotMask()
    {
        byte[] bytes = new byte[] { 0x80, 0xFF };
        BasicString value = BasicString.FromRawBytes(bytes);
        byte[] result = value.ToBytes();

        Assert.That(result[0], Is.EqualTo(0x80));
        Assert.That(result[1], Is.EqualTo(0xFF));
    }

    #endregion

    #region Indexer Tests

    /// <summary>
    /// Verifies that indexer returns correct character.
    /// </summary>
    [Test]
    public void Indexer_ValidIndex_ReturnsCorrectChar()
    {
        BasicString value = "HELLO";

        Assert.That(value[0], Is.EqualTo('H'));
        Assert.That(value[4], Is.EqualTo('O'));
    }

    /// <summary>
    /// Verifies that indexer throws on invalid index.
    /// </summary>
    [Test]
    public void Indexer_InvalidIndex_ThrowsException()
    {
        BasicString value = "HELLO";

        Assert.Throws<IndexOutOfRangeException>(() => _ = value[-1]);
        Assert.Throws<IndexOutOfRangeException>(() => _ = value[5]);
    }

    #endregion

    #region Substring Tests

    /// <summary>
    /// Verifies that Substring works correctly.
    /// </summary>
    [Test]
    public void Substring_ValidRange_ReturnsCorrectSubstring()
    {
        BasicString value = "HELLO WORLD";
        BasicString sub = value.Substring(6, 5);

        Assert.That(sub.ToString(), Is.EqualTo("WORLD"));
    }

    /// <summary>
    /// Verifies that Substring with zero length returns empty.
    /// </summary>
    [Test]
    public void Substring_ZeroLength_ReturnsEmpty()
    {
        BasicString value = "HELLO";
        BasicString sub = value.Substring(2, 0);

        Assert.That(sub.IsEmpty, Is.True);
    }

    /// <summary>
    /// Verifies that Substring throws on invalid range.
    /// </summary>
    [Test]
    public void Substring_InvalidRange_ThrowsException()
    {
        BasicString value = "HELLO";

        Assert.Throws<ArgumentOutOfRangeException>(() => value.Substring(-1, 2));
        Assert.Throws<ArgumentOutOfRangeException>(() => value.Substring(0, 10));
    }

    #endregion

    #region Concatenation Tests

    /// <summary>
    /// Verifies that Concat works correctly.
    /// </summary>
    [Test]
    public void Concat_TwoStrings_ConcatenatesCorrectly()
    {
        BasicString a = "HELLO";
        BasicString b = " WORLD";
        BasicString result = a.Concat(b);

        Assert.That(result.ToString(), Is.EqualTo("HELLO WORLD"));
    }

    /// <summary>
    /// Verifies that Concat with empty string returns original.
    /// </summary>
    [Test]
    public void Concat_WithEmpty_ReturnsOriginal()
    {
        BasicString a = "HELLO";
        BasicString empty = BasicString.Empty;

        Assert.That(a.Concat(empty).ToString(), Is.EqualTo("HELLO"));
        Assert.That(empty.Concat(a).ToString(), Is.EqualTo("HELLO"));
    }

    /// <summary>
    /// Verifies that Concat throws when result exceeds max length.
    /// </summary>
    [Test]
    public void Concat_ExceedsMaxLength_ThrowsException()
    {
        BasicString a = new string('A', 200);
        BasicString b = new string('B', 100);

        Assert.Throws<ArgumentException>(() => a.Concat(b));
    }

    #endregion

    #region Equality Tests

    /// <summary>
    /// Verifies that equal strings are equal.
    /// </summary>
    [Test]
    public void Equality_SameString_ReturnsTrue()
    {
        BasicString a = "HELLO";
        BasicString b = "HELLO";

        Assert.That(a == b, Is.True);
        Assert.That(a.Equals(b), Is.True);
    }

    /// <summary>
    /// Verifies that different strings are not equal.
    /// </summary>
    [Test]
    public void Equality_DifferentString_ReturnsFalse()
    {
        BasicString a = "HELLO";
        BasicString b = "WORLD";

        Assert.That(a == b, Is.False);
        Assert.That(a != b, Is.True);
    }

    /// <summary>
    /// Verifies that empty strings are equal.
    /// </summary>
    [Test]
    public void Equality_BothEmpty_ReturnsTrue()
    {
        BasicString a = BasicString.Empty;
        BasicString b = string.Empty;

        Assert.That(a == b, Is.True);
    }

    #endregion

    #region Static Helper Tests

    /// <summary>
    /// Verifies CharToAppleAscii converts correctly.
    /// </summary>
    [Test]
    public void CharToAppleAscii_ReturnsCorrectByte()
    {
        Assert.That(BasicString.CharToAppleAscii('A'), Is.EqualTo(0x41));
        Assert.That(BasicString.CharToAppleAscii('\u00C1'), Is.EqualTo(0x41)); // Masked high bit
    }

    /// <summary>
    /// Verifies AppleAsciiToChar converts correctly.
    /// </summary>
    [Test]
    public void AppleAsciiToChar_ReturnsCorrectChar()
    {
        Assert.That(BasicString.AppleAsciiToChar(0x41), Is.EqualTo('A'));
        Assert.That(BasicString.AppleAsciiToChar(0xC1), Is.EqualTo('A')); // Masked high bit
    }

    #endregion
}