// <copyright file="TestCpuFactory.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Devices.Tests;

using BadMango.Emulator.Bus.Interfaces;
using BadMango.Emulator.Core.Cpu;
using BadMango.Emulator.Core.Interfaces.Cpu;

using Moq;

/// <summary>
/// Provides a mock CPU factory for testing motherboard device functionality.
/// </summary>
internal static class TestCpuFactory
{
    /// <summary>
    /// Creates a CPU factory function that returns a mock CPU.
    /// </summary>
    /// <returns>A function that creates a mock <see cref="ICpu"/> given an <see cref="IEventContext"/>.</returns>
    public static Func<IEventContext, ICpu> Create()
    {
        return _ =>
        {
            var mockCpu = new Mock<ICpu>();
            mockCpu.Setup(c => c.Halted).Returns(false);
            mockCpu.Setup(c => c.IsStopRequested).Returns(false);
            mockCpu.Setup(c => c.Step()).Returns(new CpuStepResult(CpuRunState.Running, 1));
            return mockCpu.Object;
        };
    }
}