// <copyright file="DebugCommandsTests.Poke.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Tests;

/// <summary>
/// Unit tests for <see cref="PokeCommand"/>.
/// </summary>
public partial class DebugCommandsTests
{
    /// <summary>
    /// Verifies that PokeCommand has correct name.
    /// </summary>
    [Test]
    public void PokeCommand_HasCorrectName()
    {
        var command = new PokeCommand();
        Assert.That(command.Name, Is.EqualTo("poke"));
    }

    /// <summary>
    /// Verifies that PokeCommand writes single byte.
    /// </summary>
    [Test]
    public void PokeCommand_WritesSingleByte()
    {
        var command = new PokeCommand();
        var result = command.Execute(debugContext, ["$0300", "$AB"]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(ReadByte(bus, 0x0300), Is.EqualTo(0xAB));
        });
    }

    /// <summary>
    /// Verifies that PokeCommand writes multiple bytes.
    /// </summary>
    [Test]
    public void PokeCommand_WritesMultipleBytes()
    {
        var command = new PokeCommand();
        var result = command.Execute(debugContext, ["$0400", "$11", "$22", "$33"]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(ReadByte(bus, 0x0400), Is.EqualTo(0x11));
            Assert.That(ReadByte(bus, 0x0401), Is.EqualTo(0x22));
            Assert.That(ReadByte(bus, 0x0402), Is.EqualTo(0x33));
        });
    }

    /// <summary>
    /// Verifies that PokeCommand returns error when address missing.
    /// </summary>
    [Test]
    public void PokeCommand_ReturnsError_WhenAddressMissing()
    {
        var command = new PokeCommand();
        var result = command.Execute(debugContext, []);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Does.Contain("Address required"));
        });
    }

    /// <summary>
    /// Verifies that PokeCommand accepts unprefixed hex bytes.
    /// </summary>
    [Test]
    public void PokeCommand_AcceptsUnprefixedHexBytes()
    {
        var command = new PokeCommand();
        var result = command.Execute(debugContext, ["$0900", "ab", "cd", "ef"]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(ReadByte(bus, 0x0900), Is.EqualTo(0xAB));
            Assert.That(ReadByte(bus, 0x0901), Is.EqualTo(0xCD));
            Assert.That(ReadByte(bus, 0x0902), Is.EqualTo(0xEF));
        });
    }

    /// <summary>
    /// Verifies that PokeCommand accepts mixed prefixed and unprefixed bytes.
    /// </summary>
    [Test]
    public void PokeCommand_AcceptsMixedPrefixedAndUnprefixedBytes()
    {
        var command = new PokeCommand();
        var result = command.Execute(debugContext, ["$0950", "$11", "22", "0x33"]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(ReadByte(bus, 0x0950), Is.EqualTo(0x11));
            Assert.That(ReadByte(bus, 0x0951), Is.EqualTo(0x22));
            Assert.That(ReadByte(bus, 0x0952), Is.EqualTo(0x33));
        });
    }

    /// <summary>
    /// Verifies that PokeCommand interactive mode writes bytes from input.
    /// </summary>
    [Test]
    public void PokeCommand_InteractiveMode_WritesBytesFromInput()
    {
        // Set up input with some hex bytes and blank line to finish
        var inputText = "AA BB CC\n\n";
        using var inputReader = new StringReader(inputText);
        var contextWithInput = new DebugContext(dispatcher, outputWriter, errorWriter, cpu, bus, disassembler, machineInfo: null, input: inputReader);

        var command = new PokeCommand();
        var result = command.Execute(contextWithInput, ["$0500", "-i"]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(ReadByte(bus, 0x0500), Is.EqualTo(0xAA));
            Assert.That(ReadByte(bus, 0x0501), Is.EqualTo(0xBB));
            Assert.That(ReadByte(bus, 0x0502), Is.EqualTo(0xCC));
            Assert.That(outputWriter.ToString(), Does.Contain("Interactive poke mode"));
        });
    }

    /// <summary>
    /// Verifies that PokeCommand interactive mode handles multiple lines.
    /// </summary>
    [Test]
    public void PokeCommand_InteractiveMode_HandlesMultipleLines()
    {
        var inputText = "11 22\n33 44\n\n";
        using var inputReader = new StringReader(inputText);
        var contextWithInput = new DebugContext(dispatcher, outputWriter, errorWriter, cpu, bus, disassembler, machineInfo: null, input: inputReader);

        var command = new PokeCommand();
        var result = command.Execute(contextWithInput, ["$0600", "--interactive"]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(ReadByte(bus, 0x0600), Is.EqualTo(0x11));
            Assert.That(ReadByte(bus, 0x0601), Is.EqualTo(0x22));
            Assert.That(ReadByte(bus, 0x0602), Is.EqualTo(0x33));
            Assert.That(ReadByte(bus, 0x0603), Is.EqualTo(0x44));
        });
    }

    /// <summary>
    /// Verifies that PokeCommand interactive mode exits on empty line.
    /// </summary>
    [Test]
    public void PokeCommand_InteractiveMode_ExitsOnEmptyLine()
    {
        var inputText = "55\n\n";
        using var inputReader = new StringReader(inputText);
        var contextWithInput = new DebugContext(dispatcher, outputWriter, errorWriter, cpu, bus, disassembler, machineInfo: null, input: inputReader);

        var command = new PokeCommand();
        var result = command.Execute(contextWithInput, ["$0700", "-i"]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(ReadByte(bus, 0x0700), Is.EqualTo(0x55));
            Assert.That(outputWriter.ToString(), Does.Contain("Interactive mode complete"));
        });
    }

    /// <summary>
    /// Verifies that PokeCommand interactive mode returns error when no input available.
    /// </summary>
    [Test]
    public void PokeCommand_InteractiveMode_ReturnsError_WhenNoInputAvailable()
    {
        // Create context without input reader
        var contextWithoutInput = new DebugContext(dispatcher, outputWriter, errorWriter, cpu, bus, disassembler, null);

        var command = new PokeCommand();
        var result = command.Execute(contextWithoutInput, ["$0800", "-i"]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Does.Contain("Interactive mode not available"));
        });
    }

    /// <summary>
    /// Verifies that PokeCommand interactive mode supports address prefix to change write location.
    /// </summary>
    [Test]
    public void PokeCommand_InteractiveMode_SupportsAddressPrefix()
    {
        // Start at $0A00, then change to $0B00
        var inputText = "11 22\n$0B00: 33 44\n\n";
        using var inputReader = new StringReader(inputText);
        var contextWithInput = new DebugContext(dispatcher, outputWriter, errorWriter, cpu, bus, disassembler, machineInfo: null, input: inputReader);

        var command = new PokeCommand();
        var result = command.Execute(contextWithInput, ["$0A00", "-i"]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(ReadByte(bus, 0x0A00), Is.EqualTo(0x11));
            Assert.That(ReadByte(bus, 0x0A01), Is.EqualTo(0x22));
            Assert.That(ReadByte(bus, 0x0B00), Is.EqualTo(0x33));
            Assert.That(ReadByte(bus, 0x0B01), Is.EqualTo(0x44));
            Assert.That(outputWriter.ToString(), Does.Contain("Address changed to $0B00"));
        });
    }

    /// <summary>
    /// Verifies that PokeCommand interactive mode supports address-only line to change location.
    /// </summary>
    [Test]
    public void PokeCommand_InteractiveMode_SupportsAddressOnlyLine()
    {
        // Start at $0C00, change to $0D00, then write
        var inputText = "$0D00:\n55 66\n\n";
        using var inputReader = new StringReader(inputText);
        var contextWithInput = new DebugContext(dispatcher, outputWriter, errorWriter, cpu, bus, disassembler, machineInfo: null, input: inputReader);

        var command = new PokeCommand();
        var result = command.Execute(contextWithInput, ["$0C00", "-i"]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(ReadByte(bus, 0x0D00), Is.EqualTo(0x55));
            Assert.That(ReadByte(bus, 0x0D01), Is.EqualTo(0x66));
        });
    }

    /// <summary>
    /// Verifies that PokeCommand interactive mode with 0x address prefix works.
    /// </summary>
    [Test]
    public void PokeCommand_InteractiveMode_Supports0xAddressPrefix()
    {
        var inputText = "0x0E00: 77 88\n\n";
        using var inputReader = new StringReader(inputText);
        var contextWithInput = new DebugContext(dispatcher, outputWriter, errorWriter, cpu, bus, disassembler, machineInfo: null, input: inputReader);

        var command = new PokeCommand();
        var result = command.Execute(contextWithInput, ["$0100", "-i"]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(ReadByte(bus, 0x0E00), Is.EqualTo(0x77));
            Assert.That(ReadByte(bus, 0x0E01), Is.EqualTo(0x88));
        });
    }

    /// <summary>
    /// Verifies that PokeCommand file mode reads bytes from a text file using
    /// the same format as interactive mode, ignoring blank lines and trailing
    /// whitespace.
    /// </summary>
    [Test]
    public void PokeCommand_FileMode_WritesBytesFromFile()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"poke_file_mode_{Guid.NewGuid():N}.txt");
        try
        {
            // Note: trailing whitespace and a blank line are intentional.
            File.WriteAllText(
                tempFile,
                "1000: a2 20   \n1002: a2 00   \n1004: a2 03   \n1006: c9 00   \n1008: b0 0a   \n\n");

            debugContext.AttachPathResolver(new DebugPathResolver());

            var command = new PokeCommand();
            var result = command.Execute(debugContext, ["$0000", "-f", tempFile]);

            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.True, result.Message);
                Assert.That(ReadByte(bus, 0x1000), Is.EqualTo(0xA2));
                Assert.That(ReadByte(bus, 0x1001), Is.EqualTo(0x20));
                Assert.That(ReadByte(bus, 0x1002), Is.EqualTo(0xA2));
                Assert.That(ReadByte(bus, 0x1003), Is.EqualTo(0x00));
                Assert.That(ReadByte(bus, 0x1004), Is.EqualTo(0xA2));
                Assert.That(ReadByte(bus, 0x1005), Is.EqualTo(0x03));
                Assert.That(ReadByte(bus, 0x1006), Is.EqualTo(0xC9));
                Assert.That(ReadByte(bus, 0x1007), Is.EqualTo(0x00));
                Assert.That(ReadByte(bus, 0x1008), Is.EqualTo(0xB0));
                Assert.That(ReadByte(bus, 0x1009), Is.EqualTo(0x0A));
                Assert.That(outputWriter.ToString(), Does.Contain("File poke mode"));
                Assert.That(outputWriter.ToString(), Does.Contain("File mode complete"));
            });
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    /// <summary>
    /// Verifies that PokeCommand file mode treats a single hex digit as an
    /// invalid byte representation and stops reading at that point.
    /// </summary>
    [Test]
    public void PokeCommand_FileMode_StopsAtFirstInvalidByteRepresentation()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"poke_file_mode_{Guid.NewGuid():N}.txt");
        try
        {
            // The "0" on the third line is a single hex digit, which is not a
            // valid byte representation; reading must stop there.
            File.WriteAllText(
                tempFile,
                "10fd: aa\n10fe: bb\n10ff: 0\n1100: cc\n");

            debugContext.AttachPathResolver(new DebugPathResolver());

            var command = new PokeCommand();
            var result = command.Execute(debugContext, ["$0000", "-f", tempFile]);

            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.True, result.Message);
                Assert.That(ReadByte(bus, 0x10FD), Is.EqualTo(0xAA));
                Assert.That(ReadByte(bus, 0x10FE), Is.EqualTo(0xBB));

                // Nothing should have been written at or after the invalid line.
                Assert.That(ReadByte(bus, 0x10FF), Is.EqualTo(0x00));
                Assert.That(ReadByte(bus, 0x1100), Is.EqualTo(0x00));
            });
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    /// <summary>
    /// Verifies that PokeCommand file mode treats a two-digit hex byte as valid
    /// even when it appears on a line resembling the invalid case.
    /// </summary>
    [Test]
    public void PokeCommand_FileMode_AcceptsTwoDigitHexByteAtEnd()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"poke_file_mode_{Guid.NewGuid():N}.txt");
        try
        {
            File.WriteAllText(tempFile, "10ff: 0a\n");

            debugContext.AttachPathResolver(new DebugPathResolver());

            var command = new PokeCommand();
            var result = command.Execute(debugContext, ["$0000", "-f", tempFile]);

            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.True, result.Message);
                Assert.That(ReadByte(bus, 0x10FF), Is.EqualTo(0x0A));
            });
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    /// <summary>
    /// Verifies that PokeCommand file mode returns an error when the file does
    /// not exist.
    /// </summary>
    [Test]
    public void PokeCommand_FileMode_ReturnsError_WhenFileMissing()
    {
        debugContext.AttachPathResolver(new DebugPathResolver());

        var command = new PokeCommand();
        var missingPath = Path.Combine(Path.GetTempPath(), $"poke_missing_{Guid.NewGuid():N}.txt");
        var result = command.Execute(debugContext, ["$1000", "-f", missingPath]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Does.Contain("File not found"));
        });
    }

    /// <summary>
    /// Verifies that PokeCommand file mode returns an error when no file path
    /// argument is supplied.
    /// </summary>
    [Test]
    public void PokeCommand_FileMode_ReturnsError_WhenPathMissing()
    {
        var command = new PokeCommand();
        var result = command.Execute(debugContext, ["$1000", "-f"]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Does.Contain("File path required"));
        });
    }
}