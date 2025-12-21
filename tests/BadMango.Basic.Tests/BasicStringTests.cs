// <copyright file="BasicStringTests.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace BadMango.Basic.Tests;

using BadMango.Basic.Emulation;

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

    #region Range and Equality Tests

    /// <summary>
    /// Range slicing returns expected substring.
    /// </summary>
    [Test]
    public void RangeIndexer_ReturnsSubstring()
    {
        BasicString value = "HELLO";
        BasicString sub = value[1..4];

        Assert.That(sub.ToString(), Is.EqualTo("ELL"));
    }

    /// <summary>
    /// Equality operators compare bytes.
    /// </summary>
    [Test]
    public void Equality_ComparesCorrectly()
    {
        BasicString a = "ABC";
        BasicString b = "ABC";
        BasicString c = "ABD";

        Assert.That(a == b, Is.True);
        Assert.That(a != c, Is.True);
    }

    /// <summary>
    /// Implicit conversions round trip.
    /// </summary>
    [Test]
    public void ImplicitConversion_RoundTrips()
    {
        BasicString value = "DATA";
        string s = value;

        Assert.That(s, Is.EqualTo("DATA"));
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

    #region Span and Range Tests

    /// <summary>
    /// Verifies that Range indexer returns correct substring.
    /// </summary>
    [Test]
    public void RangeIndexer_ValidRange_ReturnsSubstring()
    {
        BasicString value = "HELLO WORLD";

        Assert.That(value[0..5].ToString(), Is.EqualTo("HELLO"));
        Assert.That(value[6..].ToString(), Is.EqualTo("WORLD"));
        Assert.That(value[..5].ToString(), Is.EqualTo("HELLO"));
    }

    /// <summary>
    /// Verifies that Range indexer works with end-relative indices.
    /// </summary>
    [Test]
    public void RangeIndexer_EndRelative_ReturnsCorrectSubstring()
    {
        BasicString value = "HELLO WORLD";

        Assert.That(value[^5..].ToString(), Is.EqualTo("WORLD"));
        Assert.That(value[..^6].ToString(), Is.EqualTo("HELLO"));
    }

    /// <summary>
    /// Verifies that AsSpan returns correct span.
    /// </summary>
    [Test]
    public void AsSpan_ReturnsCorrectBytes()
    {
        BasicString value = "ABC";
        ReadOnlySpan<byte> span = value.AsSpan();

        Assert.That(span.Length, Is.EqualTo(3));
        Assert.That(span[0], Is.EqualTo(0x41)); // 'A'
        Assert.That(span[1], Is.EqualTo(0x42)); // 'B'
        Assert.That(span[2], Is.EqualTo(0x43)); // 'C'
    }

    /// <summary>
    /// Verifies that AsSpan with start returns correct span.
    /// </summary>
    [Test]
    public void AsSpan_WithStart_ReturnsCorrectSlice()
    {
        BasicString value = "HELLO";
        ReadOnlySpan<byte> span = value.AsSpan(2);

        Assert.That(span.Length, Is.EqualTo(3));
        Assert.That(span[0], Is.EqualTo(0x4C)); // 'L'
    }

    /// <summary>
    /// Verifies that AsSpan with start and length returns correct span.
    /// </summary>
    [Test]
    public void AsSpan_WithStartAndLength_ReturnsCorrectSlice()
    {
        BasicString value = "HELLO WORLD";
        ReadOnlySpan<byte> span = value.AsSpan(6, 5);

        Assert.That(span.Length, Is.EqualTo(5));
        Assert.That(span[0], Is.EqualTo(0x57)); // 'W'
    }

    /// <summary>
    /// Verifies that AsSpan with Range returns correct span.
    /// </summary>
    [Test]
    public void AsSpan_WithRange_ReturnsCorrectSlice()
    {
        BasicString value = "HELLO WORLD";
        ReadOnlySpan<byte> span = value.AsSpan(6..);

        Assert.That(span.Length, Is.EqualTo(5));
        Assert.That(span[0], Is.EqualTo(0x57)); // 'W'
    }

    /// <summary>
    /// Verifies that AsSpan on empty string returns empty span.
    /// </summary>
    [Test]
    public void AsSpan_EmptyString_ReturnsEmptySpan()
    {
        BasicString value = BasicString.Empty;
        ReadOnlySpan<byte> span = value.AsSpan();

        Assert.That(span.IsEmpty, Is.True);
    }

    /// <summary>
    /// Verifies that AsMemory returns correct memory.
    /// </summary>
    [Test]
    public void AsMemory_ReturnsCorrectMemory()
    {
        BasicString value = "ABC";
        ReadOnlyMemory<byte> memory = value.AsMemory();

        Assert.That(memory.Length, Is.EqualTo(3));
        Assert.That(memory.Span[0], Is.EqualTo(0x41)); // 'A'
    }

    /// <summary>
    /// Verifies that AsMemory with start returns correct memory.
    /// </summary>
    [Test]
    public void AsMemory_WithStart_ReturnsCorrectSlice()
    {
        BasicString value = "HELLO";
        ReadOnlyMemory<byte> memory = value.AsMemory(2);

        Assert.That(memory.Length, Is.EqualTo(3));
        Assert.That(memory.Span[0], Is.EqualTo(0x4C)); // 'L'
    }

    /// <summary>
    /// Verifies that AsMemory with start and length returns correct memory.
    /// </summary>
    [Test]
    public void AsMemory_WithStartAndLength_ReturnsCorrectSlice()
    {
        BasicString value = "HELLO WORLD";
        ReadOnlyMemory<byte> memory = value.AsMemory(6, 5);

        Assert.That(memory.Length, Is.EqualTo(5));
        Assert.That(memory.Span[0], Is.EqualTo(0x57)); // 'W'
    }

    /// <summary>
    /// Verifies that AsMemory with Range returns correct memory.
    /// </summary>
    [Test]
    public void AsMemory_WithRange_ReturnsCorrectSlice()
    {
        BasicString value = "HELLO WORLD";
        ReadOnlyMemory<byte> memory = value.AsMemory(6..);

        Assert.That(memory.Length, Is.EqualTo(5));
        Assert.That(memory.Span[0], Is.EqualTo(0x57)); // 'W'
    }

    /// <summary>
    /// Verifies that FromSpan creates BasicString correctly.
    /// </summary>
    [Test]
    public void FromSpan_ValidSpan_CreatesCorrectString()
    {
        byte[] data = { 0x48, 0x45, 0x4C, 0x4C, 0x4F };
        BasicString value = BasicString.FromSpan(data.AsSpan());

        Assert.That(value.ToString(), Is.EqualTo("HELLO"));
    }

    /// <summary>
    /// Verifies that FromSpan masks high bits.
    /// </summary>
    [Test]
    public void FromSpan_HighBits_Masked()
    {
        byte[] data = { 0xC8, 0xC5, 0xCC, 0xCC, 0xCF }; // With high bits set
        BasicString value = BasicString.FromSpan(data.AsSpan());

        Assert.That(value.ToString(), Is.EqualTo("HELLO")); // Masked to 7-bit
    }

    /// <summary>
    /// Verifies that FromSpan throws on too long span.
    /// </summary>
    [Test]
    public void FromSpan_TooLong_ThrowsException()
    {
        byte[] tooLong = new byte[256];
        Assert.Throws<ArgumentException>(() => BasicString.FromSpan(tooLong.AsSpan()));
    }

    /// <summary>
    /// Verifies that FromRawSpan creates BasicString without masking.
    /// </summary>
    [Test]
    public void FromRawSpan_PreservesHighBits()
    {
        byte[] data = { 0xC8, 0xC5, 0xCC, 0xCC, 0xCF }; // With high bits set
        BasicString value = BasicString.FromRawSpan(data.AsSpan());
        byte[] result = value.ToBytes();

        Assert.That(result[0], Is.EqualTo(0xC8)); // High bits preserved
    }

    /// <summary>
    /// Verifies that CopyTo copies bytes correctly.
    /// </summary>
    [Test]
    public void CopyTo_ValidDestination_CopiesCorrectly()
    {
        BasicString value = "HELLO";
        byte[] destination = new byte[10];

        value.CopyTo(destination.AsSpan());

        Assert.That(destination[0], Is.EqualTo(0x48)); // 'H'
        Assert.That(destination[4], Is.EqualTo(0x4F)); // 'O'
    }

    /// <summary>
    /// Verifies that CopyTo throws when destination is too small.
    /// </summary>
    [Test]
    public void CopyTo_TooSmall_ThrowsException()
    {
        BasicString value = "HELLO";
        byte[] destination = new byte[3];

        Assert.Throws<ArgumentException>(() => value.CopyTo(destination.AsSpan()));
    }

    /// <summary>
    /// Verifies that TryCopyTo returns true on success.
    /// </summary>
    [Test]
    public void TryCopyTo_ValidDestination_ReturnsTrue()
    {
        BasicString value = "HELLO";
        byte[] destination = new byte[10];

        bool result = value.TryCopyTo(destination.AsSpan());

        Assert.That(result, Is.True);
        Assert.That(destination[0], Is.EqualTo(0x48)); // 'H'
    }

    /// <summary>
    /// Verifies that TryCopyTo returns false when destination is too small.
    /// </summary>
    [Test]
    public void TryCopyTo_TooSmall_ReturnsFalse()
    {
        BasicString value = "HELLO";
        byte[] destination = new byte[3];

        bool result = value.TryCopyTo(destination.AsSpan());

        Assert.That(result, Is.False);
    }

    /// <summary>
    /// Verifies Span round-trip preserves data.
    /// </summary>
    [Test]
    public void SpanRoundTrip_PreservesData()
    {
        BasicString original = "HELLO WORLD";
        ReadOnlySpan<byte> span = original.AsSpan();
        BasicString restored = BasicString.FromSpan(span);

        Assert.That(restored.ToString(), Is.EqualTo(original.ToString()));
    }

    #endregion
}