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
            Assert.That(Evaluate(new LoginFailure("bad-password")), Is.EqualTo("bad-password"));
            Assert.That(Evaluate(new ServerCrash("panic")), Is.EqualTo("panic"));
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
        LoginFailure failure => (string)failure.Value,
        ServerCrash crash => (string)crash.Value,
    };

    private static string Describe(TestMachineState state) => state switch
    {
        RunningState running => running.Name,
        FaultedState faulted => faulted.Name,
    };

    [System.Runtime.CompilerServices.Union]
    public abstract partial class TestStopReason : System.Runtime.CompilerServices.IUnion
    {
        public abstract object? Value { get; }
    }

    public sealed class LoginFailure : TestStopReason
    {
        public LoginFailure(string value)
        {
            this.Value = value;
        }

        public override object? Value { get; }
    }

    public sealed class ServerCrash : TestStopReason
    {
        public ServerCrash(string value)
        {
            this.Value = value;
        }

        public override object? Value { get; }
    }

    public closed class TestMachineState
    {
    }

    public sealed class RunningState : TestMachineState
    {
        public RunningState(string name)
        {
            this.Name = name;
        }

        public string Name { get; }
    }

    public sealed class FaultedState : TestMachineState
    {
        public FaultedState(string name)
        {
            this.Name = name;
        }

        public string Name { get; }
    }
}
