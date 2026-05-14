// <copyright file="UnitTest.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Unit.Components;

/// <summary>Provides utility methods for unit tests.</summary>
/// <remarks>
/// This static class contains helper methods designed to facilitate testing scenarios,
/// including handling specific conditions such as Continuous Integration (CI) environments.
/// </remarks>
public static class UnitTest
{
    /// <summary>Marks the test as inconclusive if the environment is a Continuous Integration (CI) environment.</summary>
    /// <param name="reason">The reason why the test is considered inconclusive in a CI environment. Defaults to "Not reliable in CI".</param>
    /// <remarks>
    /// This method checks the "CI" environment variable to determine if the test is running in a CI environment.
    /// If the variable is set to "true" (case-insensitive), the test is marked as inconclusive with the provided reason.
    /// </remarks>
    /// <exception cref="NUnit.Framework.InconclusiveException">
    /// Thrown to indicate that the test is inconclusive in a CI environment.
    /// </exception>
    public static void InconclusiveIfCI(string reason = "Not reliable in CI")
    {
        if (string.Equals(
                System.Environment.GetEnvironmentVariable("CI"),
                "true",
                StringComparison.OrdinalIgnoreCase))
        {
            Assert.Inconclusive($"Test is inconclusive in CI environment: {reason}");
        }
    }
}