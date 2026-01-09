// <copyright file="AppleMemoryTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Basic.Tests;

using Emulation;

using Microsoft.Extensions.Logging;

using Moq;

/// <summary>
/// Tests for <see cref="AppleMemory"/>.
/// </summary>
[TestFixture]
public class AppleMemoryTests
{
    private TestSpeaker speaker = null!;
    private AppleMemory memory = null!;

    /// <summary>
    /// Sets up memory and speaker for each test.
    /// </summary>
    [SetUp]
    public void Setup()
    {
        var logger = new Mock<ILogger<AppleMemory>>();
        speaker = new();
        memory = new(logger.Object);
        memory.SetSpeaker(speaker);
    }

    /// <summary>
    /// Disposes speaker after each test.
    /// </summary>
    [TearDown]
    public void Teardown()
    {
        speaker.Dispose();
    }

    /// <summary>
    /// Write then read returns the original value.
    /// </summary>
    [Test]
    public void WriteAndRead_RoundTripsValue()
    {
        memory.Write(0x100, 0xAA);

        Assert.That(memory.Read(0x100), Is.EqualTo(0xAA));
    }

    /// <summary>
    /// Writes to ROM region are ignored.
    /// </summary>
    [Test]
    public void Write_RomArea_IsIgnored()
    {
        memory.Write(0xD000, 0x55);

        Assert.That(memory.Read(0xD000), Is.EqualTo(0));
    }

    /// <summary>
    /// Writing to the speaker toggle triggers a click.
    /// </summary>
    [Test]
    public void Write_SpeakerToggle_InvokesClick()
    {
        memory.Write(AppleMemory.SpeakerToggle, 0x01);

        Assert.That(speaker.Clicks, Is.EqualTo(1));
    }

    /// <summary>
    /// Loading data that overruns memory throws.
    /// </summary>
    [Test]
    public void LoadData_TooLarge_Throws()
    {
        Assert.That(
            () => memory.LoadData(AppleMemory.StandardMemorySize - 1, new byte[] { 1, 2, 3 }),
            Throws.TypeOf<MemoryAccessException>());
    }

    /// <summary>
    /// Invalid addresses throw on read/write.
    /// </summary>
    [Test]
    public void ValidateAddress_OutOfRange_Throws()
    {
        Assert.That(() => memory.Read(-1), Throws.Exception);
        Assert.That(() => memory.Write(AppleMemory.StandardMemorySize, 0x01), Throws.Exception);
    }

    /// <summary>
    /// Read/WriteWord operate on two bytes.
    /// </summary>
    [Test]
    public void ReadWriteWord_RoundTrips()
    {
        memory.WriteWord(0x200, 0xABCD);

        Assert.That(memory.ReadWord(0x200), Is.EqualTo(0xABCD));
    }

    /// <summary>
    /// GetRegion returns requested bytes.
    /// </summary>
    [Test]
    public void GetRegion_ReturnsSlice()
    {
        memory.Write(0x10, 1);
        memory.Write(0x11, 2);

        var region = memory.GetRegion(0x10, 2);

        Assert.That(region, Is.EqualTo(new byte[] { 1, 2 }));
    }

    /// <summary>
    /// Clear zeroes memory.
    /// </summary>
    [Test]
    public void Clear_ResetsMemory()
    {
        memory.Write(0x20, 0xFF);
        memory.Clear();

        Assert.That(memory.Read(0x20), Is.EqualTo(0));
    }

    private sealed class TestSpeaker : IAppleSpeaker
    {
        public int Clicks { get; private set; }

        public void Click() => Clicks++;

        public void Beep()
        {
        }

        public void Flush()
        {
        }

        public void Dispose()
        {
        }
    }
}