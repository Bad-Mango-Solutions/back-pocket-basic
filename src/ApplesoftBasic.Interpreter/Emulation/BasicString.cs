// <copyright file="BasicString.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.Emulation;

using System.Text;

/// <summary>
/// Represents an Applesoft BASIC string value as stored on the Apple II.
/// </summary>
/// <remarks>
/// <para>
/// Apple II strings use 7-bit ASCII encoding. Characters are stored with the high bit
/// clear (0-127 range). The high bit was sometimes used for special purposes (like
/// inverse/flashing characters on screen), but for string storage, standard 7-bit ASCII applies.
/// </para>
/// <para>
/// Applesoft BASIC strings have a maximum length of 255 characters, stored with a
/// 1-byte length prefix in the string descriptor.
/// </para>
/// <para>
/// Key characteristics:
/// </para>
/// <list type="bullet">
/// <item><description>7-bit ASCII encoding (characters 0-127)</description></item>
/// <item><description>Maximum length: 255 characters</description></item>
/// <item><description>Characters outside 7-bit range are masked to 7 bits</description></item>
/// <item><description>Empty strings are valid (length 0)</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// // Create from .NET string
/// BasicString value = "HELLO";
///
/// // Convert to .NET string
/// string result = value;
///
/// // Get raw 7-bit ASCII bytes
/// byte[] bytes = value.ToBytes();
///
/// // Create from bytes
/// BasicString fromBytes = BasicString.FromBytes(new byte[] { 0x48, 0x45, 0x4C, 0x4C, 0x4F });
/// </code>
/// </example>
public readonly struct BasicString : IEquatable<BasicString>
{
    /// <summary>
    /// The maximum length of a BasicString in characters.
    /// </summary>
    public const int MaxLength = 255;

    /// <summary>
    /// The mask applied to convert 8-bit characters to 7-bit ASCII.
    /// </summary>
    private const byte SevenBitMask = 0x7F;

    private readonly byte[] bytes;

    /// <summary>
    /// Initializes a new instance of the <see cref="BasicString"/> struct from raw 7-bit ASCII bytes.
    /// </summary>
    /// <param name="bytes">The 7-bit ASCII byte array.</param>
    private BasicString(byte[] bytes)
    {
        this.bytes = bytes ?? Array.Empty<byte>();
    }

    /// <summary>
    /// Gets an empty BasicString.
    /// </summary>
    public static BasicString Empty => new(Array.Empty<byte>());

    /// <summary>
    /// Gets the length of the string in characters/bytes.
    /// </summary>
    public int Length => bytes?.Length ?? 0;

    /// <summary>
    /// Gets a value indicating whether this string is empty.
    /// </summary>
    public bool IsEmpty => Length == 0;

    /// <summary>
    /// Gets the character at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index of the character.</param>
    /// <returns>The character at the specified index.</returns>
    /// <exception cref="IndexOutOfRangeException">
    /// Thrown when index is less than 0 or greater than or equal to Length.
    /// </exception>
    public char this[int index]
    {
        get
        {
            if (index < 0 || index >= Length)
            {
                throw new IndexOutOfRangeException($"Index {index} is out of range for string of length {Length}.");
            }

            return (char)bytes[index];
        }
    }

    /// <summary>
    /// Implicitly converts a .NET string to a BasicString.
    /// </summary>
    /// <param name="value">The string to convert.</param>
    /// <returns>A BasicString representing the value.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when the string exceeds the maximum length of 255 characters.
    /// </exception>
    public static implicit operator BasicString(string value) => FromString(value);

    /// <summary>
    /// Implicitly converts a BasicString to a .NET string.
    /// </summary>
    /// <param name="basicString">The BasicString to convert.</param>
    /// <returns>The .NET string representation.</returns>
    public static implicit operator string(BasicString basicString) => basicString.ToString();

    /// <summary>
    /// Determines whether two BasicString values are equal.
    /// </summary>
    /// <param name="left">The first BasicString value to compare.</param>
    /// <param name="right">The second BasicString value to compare.</param>
    /// <returns><c>true</c> if the values are equal; otherwise, <c>false</c>.</returns>
    public static bool operator ==(BasicString left, BasicString right) => left.Equals(right);

    /// <summary>
    /// Determines whether two BasicString values are not equal.
    /// </summary>
    /// <param name="left">The first BasicString value to compare.</param>
    /// <param name="right">The second BasicString value to compare.</param>
    /// <returns><c>true</c> if the values are not equal; otherwise, <c>false</c>.</returns>
    public static bool operator !=(BasicString left, BasicString right) => !left.Equals(right);

    /// <summary>
    /// Creates a BasicString from a .NET string.
    /// </summary>
    /// <param name="value">The string to convert.</param>
    /// <returns>A BasicString representing the value.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when the string exceeds the maximum length of 255 characters.
    /// </exception>
    /// <remarks>
    /// Characters are converted to 7-bit ASCII by masking with 0x7F.
    /// Characters outside the printable ASCII range may produce unexpected results.
    /// </remarks>
    public static BasicString FromString(string value)
    {
        if (value == null)
        {
            return Empty;
        }

        if (value.Length > MaxLength)
        {
            throw new ArgumentException($"String length {value.Length} exceeds maximum BasicString length of {MaxLength}.", nameof(value));
        }

        byte[] bytes = new byte[value.Length];
        for (int i = 0; i < value.Length; i++)
        {
            // Mask to 7-bit ASCII
            bytes[i] = (byte)(value[i] & SevenBitMask);
        }

        return new BasicString(bytes);
    }

    /// <summary>
    /// Creates a BasicString from a byte array (assumed to be 7-bit ASCII).
    /// </summary>
    /// <param name="bytes">The byte array to convert.</param>
    /// <returns>A BasicString created from the bytes.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when the byte array exceeds the maximum length of 255 bytes.
    /// </exception>
    /// <remarks>
    /// Bytes are masked to 7-bit ASCII (0-127 range) for safety.
    /// </remarks>
    public static BasicString FromBytes(byte[] bytes)
    {
        if (bytes == null)
        {
            return Empty;
        }

        if (bytes.Length > MaxLength)
        {
            throw new ArgumentException($"Byte array length {bytes.Length} exceeds maximum BasicString length of {MaxLength}.", nameof(bytes));
        }

        // Create a copy and mask to 7-bit ASCII
        byte[] maskedBytes = new byte[bytes.Length];
        for (int i = 0; i < bytes.Length; i++)
        {
            maskedBytes[i] = (byte)(bytes[i] & SevenBitMask);
        }

        return new BasicString(maskedBytes);
    }

    /// <summary>
    /// Creates a BasicString from a byte array without masking (raw Apple II bytes).
    /// </summary>
    /// <param name="bytes">The byte array containing raw Apple II string data.</param>
    /// <returns>A BasicString created from the raw bytes.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when the byte array exceeds the maximum length of 255 bytes.
    /// </exception>
    /// <remarks>
    /// Use this method when reading string data directly from emulated Apple II memory
    /// where the bytes are already in the correct format. No masking is applied.
    /// </remarks>
    public static BasicString FromRawBytes(byte[] bytes)
    {
        if (bytes == null)
        {
            return Empty;
        }

        if (bytes.Length > MaxLength)
        {
            throw new ArgumentException($"Byte array length {bytes.Length} exceeds maximum BasicString length of {MaxLength}.", nameof(bytes));
        }

        // Create a copy without masking
        byte[] copyBytes = new byte[bytes.Length];
        Array.Copy(bytes, copyBytes, bytes.Length);
        return new BasicString(copyBytes);
    }

    /// <summary>
    /// Converts a character to its Apple II keyboard code representation.
    /// </summary>
    /// <param name="c">The character to convert.</param>
    /// <returns>The 7-bit ASCII code for the character.</returns>
    /// <remarks>
    /// This method converts a character to the byte value that would be stored
    /// in Apple II memory. The high bit is always clear.
    /// </remarks>
    public static byte CharToAppleAscii(char c) => (byte)(c & SevenBitMask);

    /// <summary>
    /// Converts an Apple II ASCII code to a character.
    /// </summary>
    /// <param name="b">The 7-bit ASCII code.</param>
    /// <returns>The character representation.</returns>
    public static char AppleAsciiToChar(byte b) => (char)(b & SevenBitMask);

    /// <summary>
    /// Returns the raw 7-bit ASCII bytes of this string.
    /// </summary>
    /// <returns>A byte array containing the 7-bit ASCII representation.</returns>
    public byte[] ToBytes()
    {
        if (bytes == null || bytes.Length == 0)
        {
            return Array.Empty<byte>();
        }

        byte[] copy = new byte[bytes.Length];
        Array.Copy(bytes, copy, bytes.Length);
        return copy;
    }

    /// <summary>
    /// Returns a substring of this BasicString.
    /// </summary>
    /// <param name="startIndex">The zero-based starting position.</param>
    /// <param name="length">The number of characters to extract.</param>
    /// <returns>A new BasicString containing the specified substring.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when startIndex or length is out of range.
    /// </exception>
    public BasicString Substring(int startIndex, int length)
    {
        if (startIndex < 0 || startIndex > Length)
        {
            throw new ArgumentOutOfRangeException(nameof(startIndex), $"Start index {startIndex} is out of range.");
        }

        if (length < 0 || startIndex + length > Length)
        {
            throw new ArgumentOutOfRangeException(nameof(length), $"Length {length} is out of range.");
        }

        if (length == 0)
        {
            return Empty;
        }

        byte[] subBytes = new byte[length];
        Array.Copy(bytes, startIndex, subBytes, 0, length);
        return new BasicString(subBytes);
    }

    /// <summary>
    /// Concatenates this BasicString with another.
    /// </summary>
    /// <param name="other">The BasicString to append.</param>
    /// <returns>A new BasicString containing the concatenated result.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when the combined length exceeds the maximum of 255 characters.
    /// </exception>
    public BasicString Concat(BasicString other)
    {
        int newLength = Length + other.Length;
        if (newLength > MaxLength)
        {
            throw new ArgumentException($"Concatenated string length {newLength} exceeds maximum BasicString length of {MaxLength}.");
        }

        if (Length == 0)
        {
            return other;
        }

        if (other.Length == 0)
        {
            return this;
        }

        byte[] newBytes = new byte[newLength];
        Array.Copy(bytes, 0, newBytes, 0, Length);
        Array.Copy(other.bytes, 0, newBytes, Length, other.Length);
        return new BasicString(newBytes);
    }

    /// <inheritdoc/>
    public bool Equals(BasicString other)
    {
        if (Length != other.Length)
        {
            return false;
        }

        if (bytes == null && other.bytes == null)
        {
            return true;
        }

        if (bytes == null || other.bytes == null)
        {
            return false;
        }

        for (int i = 0; i < Length; i++)
        {
            if (bytes[i] != other.bytes[i])
            {
                return false;
            }
        }

        return true;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is BasicString other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        if (bytes == null || bytes.Length == 0)
        {
            return 0;
        }

        HashCode hash = default;
        foreach (byte b in bytes)
        {
            hash.Add(b);
        }

        return hash.ToHashCode();
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        if (bytes == null || bytes.Length == 0)
        {
            return string.Empty;
        }

        // Convert 7-bit ASCII bytes to .NET string
        return Encoding.ASCII.GetString(bytes);
    }
}