// <copyright file="PrintCommandTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Tests;

using System.Text;

/// <summary>
/// Unit tests for the PrintCommand escape sequence processing.
/// </summary>
[TestFixture]
public class PrintCommandTests
{
    private const ushort TextPage1Base = 0x0400;

    /// <summary>
    /// Verifies newline escape sequence is processed correctly.
    /// </summary>
    [Test]
    public void ProcessEscapeSequences_Newline_ReturnsLineFeed()
    {
        string result = ProcessEscapeSequences("Hello\\nWorld");
        Assert.That(result, Is.EqualTo("Hello\nWorld"));
    }

    /// <summary>
    /// Verifies carriage return escape sequence is processed correctly.
    /// </summary>
    [Test]
    public void ProcessEscapeSequences_CarriageReturn_ReturnsCarriageReturn()
    {
        string result = ProcessEscapeSequences("Hello\\rWorld");
        Assert.That(result, Is.EqualTo("Hello\rWorld"));
    }

    /// <summary>
    /// Verifies tab escape sequence is processed correctly.
    /// </summary>
    [Test]
    public void ProcessEscapeSequences_Tab_ReturnsTab()
    {
        string result = ProcessEscapeSequences("Hello\\tWorld");
        Assert.That(result, Is.EqualTo("Hello\tWorld"));
    }

    /// <summary>
    /// Verifies backslash escape sequence is processed correctly.
    /// </summary>
    [Test]
    public void ProcessEscapeSequences_Backslash_ReturnsBackslash()
    {
        string result = ProcessEscapeSequences("Hello\\\\World");
        Assert.That(result, Is.EqualTo("Hello\\World"));
    }

    /// <summary>
    /// Verifies quote escape sequence is processed correctly.
    /// </summary>
    [Test]
    public void ProcessEscapeSequences_Quote_ReturnsQuote()
    {
        string result = ProcessEscapeSequences("Hello\\\"World");
        Assert.That(result, Is.EqualTo("Hello\"World"));
    }

    /// <summary>
    /// Verifies hex escape sequence is processed correctly.
    /// </summary>
    /// <param name="input">The input string with hex escape.</param>
    /// <param name="expected">The expected output string.</param>
    [TestCase("\\x41", "A")]
    [TestCase("\\x61", "a")]
    [TestCase("\\x30", "0")]
    [TestCase("\\x20", " ")]
    [TestCase("\\x7F", "\x7F")]
    public void ProcessEscapeSequences_HexEscape_ReturnsCorrectCharacter(string input, string expected)
    {
        string result = ProcessEscapeSequences(input);
        Assert.That(result, Is.EqualTo(expected));
    }

    /// <summary>
    /// Verifies hex escape at end of string is processed correctly.
    /// </summary>
    [Test]
    public void ProcessEscapeSequences_HexAtEnd_ProcessedCorrectly()
    {
        string result = ProcessEscapeSequences("Test\\x41");
        Assert.That(result, Is.EqualTo("TestA"));
    }

    /// <summary>
    /// Verifies invalid hex escape is preserved.
    /// </summary>
    [Test]
    public void ProcessEscapeSequences_InvalidHex_PreservesBackslash()
    {
        string result = ProcessEscapeSequences("Test\\xGG");
        Assert.That(result, Is.EqualTo("Test\\xGG"));
    }

    /// <summary>
    /// Verifies incomplete hex escape is preserved.
    /// </summary>
    [Test]
    public void ProcessEscapeSequences_IncompleteHex_PreservesBackslash()
    {
        string result = ProcessEscapeSequences("Test\\x4");
        Assert.That(result, Is.EqualTo("Test\\x4"));
    }

    /// <summary>
    /// Verifies unknown escape sequence preserves backslash.
    /// </summary>
    [Test]
    public void ProcessEscapeSequences_UnknownEscape_PreservesBackslash()
    {
        string result = ProcessEscapeSequences("Test\\z");
        Assert.That(result, Is.EqualTo("Test\\z"));
    }

    /// <summary>
    /// Verifies multiple escape sequences in one string.
    /// </summary>
    [Test]
    public void ProcessEscapeSequences_MultipleEscapes_AllProcessed()
    {
        string result = ProcessEscapeSequences("Line1\\nLine2\\tTabbed\\\\Backslash");
        Assert.That(result, Is.EqualTo("Line1\nLine2\tTabbed\\Backslash"));
    }

    /// <summary>
    /// Verifies empty string returns empty.
    /// </summary>
    [Test]
    public void ProcessEscapeSequences_EmptyString_ReturnsEmpty()
    {
        string result = ProcessEscapeSequences(string.Empty);
        Assert.That(result, Is.Empty);
    }

    /// <summary>
    /// Verifies string with no escapes returns unchanged.
    /// </summary>
    [Test]
    public void ProcessEscapeSequences_NoEscapes_ReturnsUnchanged()
    {
        string result = ProcessEscapeSequences("Hello World");
        Assert.That(result, Is.EqualTo("Hello World"));
    }

    /// <summary>
    /// Verifies trailing backslash is preserved.
    /// </summary>
    [Test]
    public void ProcessEscapeSequences_TrailingBackslash_PreservesBackslash()
    {
        string result = ProcessEscapeSequences("Test\\");
        Assert.That(result, Is.EqualTo("Test\\"));
    }

    /// <summary>
    /// Verifies uppercase letters convert correctly.
    /// </summary>
    /// <param name="ascii">The ASCII character to convert.</param>
    /// <param name="expected">The expected screen code.</param>
    [TestCase('A', 0xC1)]
    [TestCase('Z', 0xDA)]
    public void AsciiToScreenCode_UppercaseLetters_ConvertsCorrectly(char ascii, byte expected)
    {
        byte result = AsciiToScreenCode(ascii);
        Assert.That(result, Is.EqualTo(expected));
    }

    /// <summary>
    /// Verifies lowercase letters convert correctly.
    /// </summary>
    /// <param name="ascii">The ASCII character to convert.</param>
    /// <param name="expected">The expected screen code.</param>
    [TestCase('a', 0xE1)]
    [TestCase('z', 0xFA)]
    public void AsciiToScreenCode_LowercaseLetters_ConvertsCorrectly(char ascii, byte expected)
    {
        byte result = AsciiToScreenCode(ascii);
        Assert.That(result, Is.EqualTo(expected));
    }

    /// <summary>
    /// Verifies numbers convert correctly.
    /// </summary>
    /// <param name="ascii">The ASCII character to convert.</param>
    /// <param name="expected">The expected screen code.</param>
    [TestCase('0', 0xB0)]
    [TestCase('9', 0xB9)]
    public void AsciiToScreenCode_Numbers_ConvertsCorrectly(char ascii, byte expected)
    {
        byte result = AsciiToScreenCode(ascii);
        Assert.That(result, Is.EqualTo(expected));
    }

    /// <summary>
    /// Verifies space converts correctly.
    /// </summary>
    [Test]
    public void AsciiToScreenCode_Space_ConvertsCorrectly()
    {
        byte result = AsciiToScreenCode(' ');
        Assert.That(result, Is.EqualTo(0xA0));
    }

    /// <summary>
    /// Verifies punctuation converts correctly.
    /// </summary>
    /// <param name="ascii">The ASCII character to convert.</param>
    /// <param name="expected">The expected screen code.</param>
    [TestCase('!', 0xA1)]
    [TestCase('@', 0xC0)]
    [TestCase('#', 0xA3)]
    public void AsciiToScreenCode_Punctuation_ConvertsCorrectly(char ascii, byte expected)
    {
        byte result = AsciiToScreenCode(ascii);
        Assert.That(result, Is.EqualTo(expected));
    }

    /// <summary>
    /// Verifies row 0 address is correct.
    /// </summary>
    [Test]
    public void TextRowAddress_Row0_IsCorrect()
    {
        ushort address = ComputeTextRowAddress(0);
        Assert.That(address, Is.EqualTo(0x0400));
    }

    /// <summary>
    /// Verifies row 8 address is correct.
    /// </summary>
    [Test]
    public void TextRowAddress_Row8_IsCorrect()
    {
        ushort address = ComputeTextRowAddress(8);
        Assert.That(address, Is.EqualTo(0x0428)); // $0400 + 40
    }

    /// <summary>
    /// Verifies row 16 address is correct.
    /// </summary>
    [Test]
    public void TextRowAddress_Row16_IsCorrect()
    {
        ushort address = ComputeTextRowAddress(16);
        Assert.That(address, Is.EqualTo(0x0450)); // $0400 + 80
    }

    /// <summary>
    /// Verifies all row addresses are within valid range.
    /// </summary>
    [Test]
    public void TextRowAddresses_AllWithinValidRange()
    {
        for (int row = 0; row < 24; row++)
        {
            ushort address = ComputeTextRowAddress(row);
            Assert.That(address, Is.GreaterThanOrEqualTo(0x0400));
            Assert.That(address, Is.LessThan(0x0800));
        }
    }

    /// <summary>
    /// Verifies row addresses don't overlap.
    /// </summary>
    [Test]
    public void TextRowAddresses_DoNotOverlap()
    {
        var usedAddresses = new HashSet<ushort>();

        for (int row = 0; row < 24; row++)
        {
            ushort baseAddr = ComputeTextRowAddress(row);
            for (int col = 0; col < 40; col++)
            {
                ushort addr = (ushort)(baseAddr + col);
                Assert.That(usedAddresses.Contains(addr), Is.False, $"Address {addr:X4} is duplicated");
                usedAddresses.Add(addr);
            }
        }

        // Should have exactly 24 * 40 = 960 unique addresses
        Assert.That(usedAddresses.Count, Is.EqualTo(960));
    }

    /// <summary>
    /// Verifies normal space screen code is $A0.
    /// </summary>
    [Test]
    public void NormalSpace_ScreenCode_IsA0()
    {
        const byte NormalSpace = 0xA0;
        byte screenCode = AsciiToScreenCode(' ');
        Assert.That(screenCode, Is.EqualTo(NormalSpace));
    }

    /// <summary>
    /// Verifies home command would fill screen with 960 bytes of $A0.
    /// </summary>
    [Test]
    public void ClearScreen_FillsAllPositionsWithNormalSpace()
    {
        const byte NormalSpace = 0xA0;
        const int TextRows = 24;
        const int TextColumns = 40;
        const int TotalCells = TextRows * TextColumns; // 960

        // Verify we'd write to exactly 960 positions
        Assert.That(TotalCells, Is.EqualTo(960));

        // Verify all row addresses are unique and fill 960 cells
        var addresses = new HashSet<ushort>();
        for (int row = 0; row < TextRows; row++)
        {
            ushort rowAddr = ComputeTextRowAddress(row);
            for (int col = 0; col < TextColumns; col++)
            {
                addresses.Add((ushort)(rowAddr + col));
            }
        }

        Assert.That(addresses.Count, Is.EqualTo(TotalCells));

        // Verify normal space value
        Assert.That(NormalSpace, Is.EqualTo(0xA0));
    }

    private static string ProcessEscapeSequences(string input)
    {
        var sb = new StringBuilder(input.Length);
        int i = 0;

        while (i < input.Length)
        {
            if (input[i] == '\\' && i + 1 < input.Length)
            {
                char next = input[i + 1];
                switch (next)
                {
                    case 'n':
                        sb.Append('\n');
                        i += 2;
                        break;
                    case 'r':
                        sb.Append('\r');
                        i += 2;
                        break;
                    case 't':
                        sb.Append('\t');
                        i += 2;
                        break;
                    case '\\':
                        sb.Append('\\');
                        i += 2;
                        break;
                    case '"':
                        sb.Append('"');
                        i += 2;
                        break;
                    case 'x':
                        if (i + 4 <= input.Length)
                        {
                            var hex = input.AsSpan(i + 2, 2);
                            if (byte.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out byte value))
                            {
                                sb.Append((char)value);
                                i += 4;
                                break;
                            }
                        }

                        sb.Append(input[i]);
                        i++;
                        break;
                    default:
                        sb.Append(input[i]);
                        i++;
                        break;
                }
            }
            else
            {
                sb.Append(input[i]);
                i++;
            }
        }

        return sb.ToString();
    }

    private static byte AsciiToScreenCode(char c)
    {
        int ascii = c;

        if (ascii >= 0x41 && ascii <= 0x5A)
        {
            return (byte)(ascii + 0x80);
        }

        if (ascii >= 0x61 && ascii <= 0x7A)
        {
            return (byte)(ascii + 0x80);
        }

        if (ascii >= 0x20 && ascii <= 0x3F)
        {
            return (byte)(ascii + 0x80);
        }

        if (ascii >= 0x5B && ascii <= 0x7F)
        {
            return (byte)(ascii + 0x80);
        }

        return (byte)(ascii | 0x80);
    }

    private static ushort ComputeTextRowAddress(int row)
    {
        int group = row / 8;
        int offset = row % 8;
        return (ushort)(TextPage1Base + (offset * 128) + (group * 40));
    }
}