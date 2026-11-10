// <copyright file="CSharp15LanguageFeatureTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Tests;

/// <summary>
/// Validates the C# 15 language feature polyfills used when targeting net10.0 with the .NET 11 SDK.
/// </summary>
[TestFixture]
public class CSharp15LanguageFeatureTests
{
    /// <summary>
    /// Verifies that a union with an exhaustive switch can compile and evaluate on the net10 runtime.
    /// </summary>
    [Test]
    public void Union_Switch_HandlesEveryCase()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Evaluate(new TestStopReason("bad-password")), Is.EqualTo("bad-password"));
            Assert.That(Evaluate(new TestStopReason(42)), Is.EqualTo("42"));
        });
    }

    /// <summary>
    /// Verifies that a closed hierarchy with an exhaustive switch can compile and evaluate.
    /// </summary>
    [Test]
    public void ClosedType_Switch_HandlesEveryCase()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Describe(new RunningState("running")), Is.EqualTo("running"));
            Assert.That(Describe(new FaultedState("faulted")), Is.EqualTo("faulted"));
        });
    }

    private static string Evaluate(TestStopReason reason) => reason switch
    {
        { Value: string s } => s,
        { Value: int i } => i.ToString(),
    };

    private static string Describe(TestMachineState state) => state switch
    {
        RunningState running => running.Name,
        FaultedState faulted => faulted.Name,
    };

#pragma warning disable CS0436 // The compiler-reserved union types are intentionally polyfilled in every assembly.

    [System.Runtime.CompilerServices.Union]
    private partial class TestStopReason : System.Runtime.CompilerServices.IUnion
    {
        public TestStopReason(string value) => this.Value = value;

        public TestStopReason(int value) => this.Value = value;

        public object? Value { get; }
    }

    private closed class TestMachineState
    {
    }

    private sealed class RunningState : TestMachineState
    {
        public RunningState(string name)
        {
            this.Name = name;
        }

        public string Name { get; }
    }

    private sealed class FaultedState : TestMachineState
    {
        public FaultedState(string name)
        {
            this.Name = name;
        }

        public string Name { get; }
    }
#pragma warning restore CS0436
}