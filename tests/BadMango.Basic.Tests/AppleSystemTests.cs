// <copyright file="AppleSystemTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Basic.Tests;

using BadMango.Basic.Emulation;
using Microsoft.Extensions.Logging;
using Moq;

/// <summary>
/// Tests for <see cref="AppleSystem"/>.
/// </summary>
[TestFixture]
public class AppleSystemTests
{
    private AppleSystem system = null!;
    private TestCpu cpu = null!;
    private TestSpeaker speaker = null!;
    private AppleMemory memory = null!;

    /// <summary>
    /// Set up test system.
    /// </summary>
    [SetUp]
    public void Setup()
    {
        var logger = new Mock<ILogger<AppleSystem>>();
        var memoryLogger = new Mock<ILogger<AppleMemory>>();
        memory = new AppleMemory(memoryLogger.Object);
        cpu = new TestCpu(memory);
        speaker = new TestSpeaker();
        system = new AppleSystem(memory, cpu, speaker, logger.Object);
    }

    /// <summary>
    /// Disposes the speaker.
    /// </summary>
    [TearDown]
    public void Teardown()
    {
        speaker.Dispose();
    }

    /// <summary>
    /// POKE writes are visible via PEEK.
    /// </summary>
    [Test]
    public void Poke_WritesValue()
    {
        system.Poke(0x200, 0x7F);

        Assert.That(system.Peek(0x200), Is.EqualTo(0x7F));
    }

    /// <summary>
    /// ROM bell call beeps without CPU execution.
    /// </summary>
    [Test]
    public void Call_RomBell_UsesSpeaker()
    {
        system.Call(AppleSystem.MemoryLocations.BELL);

        Assert.That(speaker.Beeps, Is.EqualTo(1));
        Assert.That(cpu.Executions, Is.EqualTo(0));
    }

    /// <summary>
    /// CALL executes CPU at address.
    /// </summary>
    [Test]
    public void Call_CustomAddress_ExecutesCpu()
    {
        system.Call(0x1234);

        Assert.That(cpu.Executions, Is.EqualTo(1));
        Assert.That(cpu.LastStartAddress, Is.EqualTo(0x1234));
    }

    /// <summary>
    /// Keyboard input sets and clears strobe.
    /// </summary>
    [Test]
    public void KeyboardInput_SetsAndClears()
    {
        system.SetKeyboardInput('A');

        Assert.That(system.Peek(AppleSystem.MemoryLocations.KBD) & 0x80, Is.EqualTo(0x80));
        Assert.That(system.GetKeyboardInput(), Is.EqualTo('A'));
        Assert.That(system.Peek(AppleSystem.MemoryLocations.KBD) & 0x80, Is.EqualTo(0));
    }

    /// <summary>
    /// Reset reinitializes window defaults.
    /// </summary>
    [Test]
    public void Reset_InitializesWindow()
    {
        memory.Write(AppleSystem.MemoryLocations.WNDLFT, 5);

        system.Reset();

        Assert.That(memory.Read(AppleSystem.MemoryLocations.WNDLFT), Is.EqualTo(0));
        Assert.That(memory.Read(AppleSystem.MemoryLocations.WNDWDTH), Is.EqualTo(40));
    }

    private sealed class TestCpu : ICpu
    {
        public TestCpu(IMemory memory)
        {
            Memory = memory;
            Registers = new Cpu6502Registers();
        }

        public Cpu6502Registers Registers { get; }

        public IMemory Memory { get; }

        public bool Halted => false;

        public int Executions { get; private set; }

        public int LastStartAddress { get; private set; }

        public int Step() => 0;

        public void Execute(int startAddress)
        {
            Executions++;
            LastStartAddress = startAddress;
        }

        public void Reset()
        {
        }
    }

    private sealed class TestSpeaker : IAppleSpeaker
    {
        public int Beeps { get; private set; }

        public void Click()
        {
        }

        public void Beep() => Beeps++;

        public void Flush()
        {
        }

        public void Dispose()
        {
        }
    }
}