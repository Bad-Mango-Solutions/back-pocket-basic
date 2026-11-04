// <copyright file="PerformanceTestAttribute.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Unit.Components;

using System;

/// <summary>An attribute used to mark a test method as a performance test.</summary>
/// <remarks>
/// This attribute is designed to facilitate performance testing by marking specific test methods.
/// If the test is executed in a Continuous Integration (CI) environment, it will be marked as inconclusive
/// with a specified reason, as performance tests may not yield reliable results in such environments.
/// </remarks>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class PerformanceTestAttribute : NUnitAttribute, ITestAction
{
    /// <summary>Initializes a new instance of the <see cref="PerformanceTestAttribute"/> class.</summary>
    /// <param name="reason">The reason why the test is considered inconclusive in a CI environment. Defaults to "Not reliable in CI".</param>
    public PerformanceTestAttribute(string reason = "Not reliable in CI")
    {
        Reason = reason;
    }

    /// <summary>Gets the reason why the test is considered inconclusive in a Continuous Integration (CI) environment.</summary>
    /// <value>
    /// A string representing the reason for marking the test as inconclusive in CI.
    /// Defaults to "Not reliable in CI".
    /// </value>
    public string Reason { get; }

    /// <inheritdoc/>
    public ActionTargets Targets => ActionTargets.Test;

    /// <inheritdoc/>
    public void BeforeTest(NUnit.Framework.Interfaces.ITest test)
    {
        UnitTest.InconclusiveIfCI(Reason);
    }

    /// <inheritdoc/>
    public void AfterTest(NUnit.Framework.Interfaces.ITest test)
    {
    }
}