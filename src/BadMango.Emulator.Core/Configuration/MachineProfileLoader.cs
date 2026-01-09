// <copyright file="MachineProfileLoader.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Core.Configuration;

using System.Collections.ObjectModel;
using System.Text.Json;

using Interfaces;

/// <summary>
/// Default implementation of <see cref="IMachineProfileLoader"/> that loads profiles from the filesystem.
/// </summary>
/// <remarks>
/// <para>
/// Profiles are loaded from JSON files in the "profiles" directory relative to the application's base directory.
/// Each profile file should have a ".json" extension and follow the machine profile schema.
/// </para>
/// <para>
/// This implementation uses secure file handling practices:
/// <list type="bullet">
///   <item><description>Path validation to prevent directory traversal attacks.</description></item>
///   <item><description>Read-only file access.</description></item>
///   <item><description>Proper exception handling for file operations.</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class MachineProfileLoader : IMachineProfileLoader
{
    /// <summary>
    /// The default profile name used when no specific profile is requested.
    /// </summary>
    public const string DefaultProfileName = "simple-65c02";

    /// <summary>
    /// The file extension for profile files.
    /// </summary>
    private const string ProfileExtension = ".json";

    /// <summary>
    /// The name of the profiles directory.
    /// </summary>
    private const string ProfilesDirectoryName = "profiles";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private readonly string profilesDirectory;
    private readonly Lazy<IReadOnlyList<string>> availableProfiles;
    private readonly Lazy<MachineProfile> defaultProfile;

    /// <summary>
    /// Initializes a new instance of the <see cref="MachineProfileLoader"/> class.
    /// </summary>
    /// <remarks>
    /// Uses the default profiles directory located relative to the application's base directory.
    /// </remarks>
    public MachineProfileLoader()
        : this(GetDefaultProfilesDirectory())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MachineProfileLoader"/> class
    /// with a custom profiles directory.
    /// </summary>
    /// <param name="profilesDirectory">The path to the directory containing profile JSON files.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="profilesDirectory"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="profilesDirectory"/> is empty or whitespace.</exception>
    public MachineProfileLoader(string profilesDirectory)
    {
        ArgumentNullException.ThrowIfNull(profilesDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(profilesDirectory);

        this.profilesDirectory = Path.GetFullPath(profilesDirectory);
        this.availableProfiles = new(LoadAvailableProfiles);
        this.defaultProfile = new(LoadDefaultProfile);
    }

    /// <inheritdoc />
    public MachineProfile DefaultProfile => defaultProfile.Value;

    /// <inheritdoc />
    public IReadOnlyList<string> AvailableProfiles => availableProfiles.Value;

    /// <inheritdoc />
    public MachineProfile? LoadProfile(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        // Validate the name doesn't contain path separators (security check)
        if (name.AsSpan().ContainsAny(Path.GetInvalidFileNameChars()) ||
            name.Contains(".."))
        {
            return null;
        }

        string filePath = GetProfileFilePath(name);

        if (!File.Exists(filePath))
        {
            return null;
        }

        return LoadProfileFromFileInternal(filePath);
    }

    /// <inheritdoc />
    public MachineProfile LoadProfileFromFile(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        string fullPath = Path.GetFullPath(path);

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"Profile file not found: {fullPath}", fullPath);
        }

        return LoadProfileFromFileInternal(fullPath)
               ?? throw new InvalidOperationException($"Failed to deserialize profile from: {fullPath}");
    }

    /// <summary>
    /// Gets the default profiles directory based on the application's base directory.
    /// </summary>
    /// <returns>The full path to the default profiles directory.</returns>
    private static string GetDefaultProfilesDirectory()
    {
        string baseDir = AppContext.BaseDirectory;
        return Path.Combine(baseDir, ProfilesDirectoryName);
    }

    /// <summary>
    /// Creates a fallback profile when the file-based default cannot be loaded.
    /// </summary>
    /// <returns>A built-in default profile configuration.</returns>
    private static MachineProfile CreateFallbackProfile()
    {
        return new()
        {
            Name = DefaultProfileName,
            DisplayName = "Simple 65C02 System with 64KB RAM",
            Description = "Basic 65C02 with 64KB RAM - suitable for debugging and development",
            Cpu = new()
            {
                Type = "65C02",
                ClockSpeed = 1_000_000,
            },
            AddressSpace = 16,
            Memory = new()
            {
                Regions =
                [
                    new()
                    {
                        Name = "main-ram",
                        Type = "ram",
                        Start = "0x0000",
                        Size = "0x10000",
                        Permissions = "rwx",
                    },
                ],
            },
        };
    }

    /// <summary>
    /// Loads and deserializes a profile from a file path.
    /// </summary>
    /// <param name="filePath">The full path to the profile file.</param>
    /// <returns>The deserialized profile, or null if deserialization fails.</returns>
    private static MachineProfile? LoadProfileFromFileInternal(string filePath)
    {
        try
        {
            using var stream = new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 4096,
                FileOptions.SequentialScan);

            return JsonSerializer.Deserialize<MachineProfile>(stream, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    /// <summary>
    /// Loads the list of available profile names from the profiles directory.
    /// </summary>
    /// <returns>A read-only list of profile names (without file extensions).</returns>
    private IReadOnlyList<string> LoadAvailableProfiles()
    {
        if (!Directory.Exists(profilesDirectory))
        {
            return ReadOnlyCollection<string>.Empty;
        }

        try
        {
            var profiles = Directory.EnumerateFiles(profilesDirectory, $"*{ProfileExtension}")
                .Select(Path.GetFileNameWithoutExtension)
                .Where(name => !string.IsNullOrEmpty(name))
                .Cast<string>()
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            return profiles.AsReadOnly();
        }
        catch (IOException)
        {
            return ReadOnlyCollection<string>.Empty;
        }
        catch (UnauthorizedAccessException)
        {
            return ReadOnlyCollection<string>.Empty;
        }
    }

    /// <summary>
    /// Loads the default machine profile.
    /// </summary>
    /// <returns>The default machine profile.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the default profile cannot be loaded.</exception>
    private MachineProfile LoadDefaultProfile()
    {
        var profile = LoadProfile(DefaultProfileName);

        if (profile is not null)
        {
            return profile;
        }

        // If the file-based default doesn't exist, return a built-in fallback
        return CreateFallbackProfile();
    }

    /// <summary>
    /// Gets the full file path for a profile by name.
    /// </summary>
    /// <param name="name">The profile name.</param>
    /// <returns>The full path to the profile file.</returns>
    private string GetProfileFilePath(string name)
    {
        return Path.Combine(profilesDirectory, $"{name}{ProfileExtension}");
    }
}