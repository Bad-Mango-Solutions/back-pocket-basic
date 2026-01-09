// <copyright file="IMachineProfileLoader.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Core.Interfaces;

using Configuration;

/// <summary>
/// Service for loading and managing machine profiles.
/// </summary>
public interface IMachineProfileLoader
{
    /// <summary>
    /// Gets the default profile (simple-65c02).
    /// </summary>
    MachineProfile DefaultProfile { get; }

    /// <summary>
    /// Gets all available profile names.
    /// </summary>
    IReadOnlyList<string> AvailableProfiles { get; }

    /// <summary>
    /// Loads a profile by name.
    /// </summary>
    /// <param name="name">The profile name (e.g., "simple-65c02").</param>
    /// <returns>The loaded profile, or null if not found.</returns>
    MachineProfile? LoadProfile(string name);

    /// <summary>
    /// Loads a profile from a file path.
    /// </summary>
    /// <param name="path">The path to the JSON profile file.</param>
    /// <returns>The loaded profile. </returns>
    MachineProfile LoadProfileFromFile(string path);
}