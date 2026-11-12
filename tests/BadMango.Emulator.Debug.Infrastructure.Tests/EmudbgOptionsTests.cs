// <copyright file="EmudbgOptionsTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Tests;

/// <summary>
/// Unit tests for <see cref="EmudbgOptions"/> CLI argument parsing.
/// </summary>
[TestFixture]
public class EmudbgOptionsTests
{
    /// <summary>
    /// Verifies that empty arguments produce default options.
    /// </summary>
    [Test]
    public void Parse_EmptyArgs_ReturnsDefaults()
    {
        var options = EmudbgOptions.Parse([]);

        Assert.Multiple(() =>
        {
            Assert.That(options.Profile, Is.Null);
            Assert.That(options.ExecCommands, Is.Null);
            Assert.That(options.ScriptFile, Is.Null);
            Assert.That(options.NoBanner, Is.False);
            Assert.That(options.ShowHelp, Is.False);
        });
    }

    /// <summary>
    /// Verifies various forms of profile arguments are parsed.
    /// </summary>
    /// <param name="arg1">The argument key or key=value.</param>
    /// <param name="expected">The expected profile name.</param>
    [Test]
    [TestCase("--profile", "pocket2e-a2-enh")]
    [TestCase("-p", "simple-65c02")]
    [TestCase("--profile=pocket2e-lite", "pocket2e-lite")]
    [TestCase("-p=simple-65c02", "simple-65c02")]
    public void Parse_Profile_SetsProfile(string arg1, string expected)
    {
        string[] args = arg1.Contains('=') ? [arg1] : [arg1, expected];
        var options = EmudbgOptions.Parse(args);

        Assert.That(options.Profile, Is.EqualTo(expected));
    }

    /// <summary>
    /// Verifies various forms of --exec arguments are parsed.
    /// </summary>
    /// <param name="arg1">The argument key or key=value.</param>
    /// <param name="expected">The expected commands string.</param>
    [Test]
    [TestCase("--exec", "boot;regs;exit")]
    [TestCase("-e", "profile list\nregs")]
    [TestCase("--exec=boot;step 5", "boot;step 5")]
    public void Parse_Exec_SetsExecCommands(string arg1, string expected)
    {
        string[] args = arg1.Contains('=') ? [arg1] : [arg1, expected];
        var options = EmudbgOptions.Parse(args);

        Assert.That(options.ExecCommands, Is.EqualTo(expected));
    }

    /// <summary>
    /// Verifies various forms of --file arguments are parsed.
    /// </summary>
    /// <param name="arg1">The argument key or key=value.</param>
    /// <param name="expected">The expected file path.</param>
    [Test]
    [TestCase("--file", "my-script.emudbg")]
    [TestCase("-f", "commands.txt")]
    [TestCase("--file=debug.seq", "debug.seq")]
    public void Parse_File_SetsScriptFile(string arg1, string expected)
    {
        string[] args = arg1.Contains('=') ? [arg1] : [arg1, expected];
        var options = EmudbgOptions.Parse(args);

        Assert.That(options.ScriptFile, Is.EqualTo(expected));
    }

    /// <summary>
    /// Verifies --no-banner flag is recognized.
    /// </summary>
    [Test]
    public void Parse_NoBanner_SetsFlag()
    {
        var options = EmudbgOptions.Parse(["--no-banner"]);

        Assert.That(options.NoBanner, Is.True);
    }

    /// <summary>
    /// Verifies all help flag variants set ShowHelp.
    /// </summary>
    /// <param name="helpArg">A help flag variant.</param>
    [Test]
    [TestCase("--help")]
    [TestCase("-h")]
    [TestCase("/?")]
    [TestCase("-?")]
    public void Parse_HelpVariants_SetShowHelp(string helpArg)
    {
        var options = EmudbgOptions.Parse([helpArg]);

        Assert.That(options.ShowHelp, Is.True);
    }

    /// <summary>
    /// Verifies multiple different options can be combined.
    /// </summary>
    [Test]
    public void Parse_MixedArgs_ParsesAll()
    {
        var options = EmudbgOptions.Parse([
            "--profile", "pocket2e",
            "--exec", "boot;regs",
            "--no-banner",
            "-h"
        ]);

        Assert.Multiple(() =>
        {
            Assert.That(options.Profile, Is.EqualTo("pocket2e"));
            Assert.That(options.ExecCommands, Is.EqualTo("boot;regs"));
            Assert.That(options.NoBanner, Is.True);
            Assert.That(options.ShowHelp, Is.True);
        });
    }

    /// <summary>
    /// Verifies unknown arguments are ignored and do not affect parsing.
    /// </summary>
    [Test]
    public void Parse_UnknownArgs_AreIgnored()
    {
        var options = EmudbgOptions.Parse(["--profile", "foo", "--unknown", "bar", "-x"]);

        Assert.That(options.Profile, Is.EqualTo("foo"));
    }

    /// <summary>
    /// Verifies that later values override earlier ones for repeated options.
    /// </summary>
    [Test]
    public void Parse_LastValueWins_ForRepeats()
    {
        var options = EmudbgOptions.Parse([
            "--profile", "first",
            "-p", "second"
        ]);

        Assert.That(options.Profile, Is.EqualTo("second"));
    }

    /// <summary>
    /// Verifies --json / -j sets the flag.
    /// </summary>
    /// <param name="flag">Json output flag.</param>
    [Test]
    [TestCase("--json")]
    [TestCase("-j")]
    public void Parse_JsonOutput_SetsFlag(string flag)
    {
        var options = EmudbgOptions.Parse([flag]);
        Assert.That(options.JsonOutput, Is.True);
    }

    /// <summary>
    /// Verifies --json combines with other options.
    /// </summary>
    [Test]
    public void Parse_JsonWithOtherOptions()
    {
        var options = EmudbgOptions.Parse(["--profile", "test", "--json", "--no-banner"]);
        Assert.Multiple(() =>
        {
            Assert.That(options.Profile, Is.EqualTo("test"));
            Assert.That(options.JsonOutput, Is.True);
            Assert.That(options.NoBanner, Is.True);
        });
    }

    /// <summary>
    /// Tests that the <c>--headless</c> argument correctly sets the <see cref="EmudbgOptions.Headless"/> flag to <c>true</c>.
    /// </summary>
    [Test]
    public void Parse_Headless_SetsFlag()
    {
        var options = EmudbgOptions.Parse(["--headless"]);
        Assert.That(options.Headless, Is.True);
    }

    /// <summary>
    /// Tests the parsing of command-line arguments for the combination of
    /// headless mode, JSON output, and no banner options.
    /// </summary>
    /// <remarks>
    /// This test verifies that the <see cref="EmudbgOptions"/> instance is correctly
    /// populated when the arguments <c>--headless</c>, <c>--json</c>, and <c>--no-banner</c>
    /// are provided, along with a profile argument.
    /// </remarks>
    /// <example>
    /// Example arguments: <c>--headless --json --no-banner --profile test</c>.
    /// </example>
    [Test]
    public void Parse_HeadlessWithJsonAndNoBanner()
    {
        var options = EmudbgOptions.Parse(["--headless", "--json", "--no-banner", "--profile", "test"]);
        Assert.Multiple(() =>
        {
            Assert.That(options.Headless, Is.True);
            Assert.That(options.JsonOutput, Is.True);
            Assert.That(options.NoBanner, Is.True);
            Assert.That(options.Profile, Is.EqualTo("test"));
        });
    }
}