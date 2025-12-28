// <copyright file="MachineProfileLoaderTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Tests;

using System.Text.Json;

using BadMango.Emulator.Core;
using BadMango.Emulator.Core.Configuration;

using Core.Interfaces;

/// <summary>
/// Unit tests for the <see cref="MachineProfileLoader"/> class.
/// </summary>
[TestFixture]
public class MachineProfileLoaderTests
{
    private string testDirectory = null!;
    private string profilesDirectory = null!;

    /// <summary>
    /// Sets up the test directory structure before each test.
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        testDirectory = Path.Combine(Path.GetTempPath(), $"MachineProfileLoaderTests_{Guid.NewGuid():N}");
        profilesDirectory = Path.Combine(testDirectory, "profiles");
        Directory.CreateDirectory(profilesDirectory);
    }

    /// <summary>
    /// Cleans up the test directory after each test.
    /// </summary>
    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(testDirectory))
        {
            Directory.Delete(testDirectory, recursive: true);
        }
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when profilesDirectory is null.
    /// </summary>
    [Test]
    public void Constructor_NullDirectory_ThrowsArgumentNullException()
    {
        Assert.That(
            () => new MachineProfileLoader(null!),
            Throws.ArgumentNullException.With.Property("ParamName").EqualTo("profilesDirectory"));
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentException when profilesDirectory is empty.
    /// </summary>
    [Test]
    public void Constructor_EmptyDirectory_ThrowsArgumentException()
    {
        Assert.That(
            () => new MachineProfileLoader(string.Empty),
            Throws.TypeOf<ArgumentException>());
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentException when profilesDirectory is whitespace.
    /// </summary>
    [Test]
    public void Constructor_WhitespaceDirectory_ThrowsArgumentException()
    {
        Assert.That(
            () => new MachineProfileLoader("   "),
            Throws.TypeOf<ArgumentException>());
    }

    /// <summary>
    /// Verifies that AvailableProfiles returns empty list when directory doesn't exist.
    /// </summary>
    [Test]
    public void AvailableProfiles_DirectoryDoesNotExist_ReturnsEmptyList()
    {
        // Arrange
        var nonExistentDir = Path.Combine(testDirectory, "nonexistent");
        var loader = new MachineProfileLoader(nonExistentDir);

        // Act
        var profiles = loader.AvailableProfiles;

        // Assert
        Assert.That(profiles, Is.Empty);
    }

    /// <summary>
    /// Verifies that AvailableProfiles returns empty list when directory is empty.
    /// </summary>
    [Test]
    public void AvailableProfiles_EmptyDirectory_ReturnsEmptyList()
    {
        // Arrange
        var loader = new MachineProfileLoader(profilesDirectory);

        // Act
        var profiles = loader.AvailableProfiles;

        // Assert
        Assert.That(profiles, Is.Empty);
    }

    /// <summary>
    /// Verifies that AvailableProfiles returns profile names without file extension.
    /// </summary>
    [Test]
    public void AvailableProfiles_WithProfiles_ReturnsNamesWithoutExtension()
    {
        // Arrange
        CreateTestProfile("test-profile1");
        CreateTestProfile("test-profile2");
        var loader = new MachineProfileLoader(profilesDirectory);

        // Act
        var profiles = loader.AvailableProfiles;

        // Assert
        Assert.That(profiles, Has.Count.EqualTo(2));
        Assert.That(profiles, Contains.Item("test-profile1"));
        Assert.That(profiles, Contains.Item("test-profile2"));
    }

    /// <summary>
    /// Verifies that AvailableProfiles only returns JSON files.
    /// </summary>
    [Test]
    public void AvailableProfiles_IgnoresNonJsonFiles()
    {
        // Arrange
        CreateTestProfile("valid-profile");
        File.WriteAllText(Path.Combine(profilesDirectory, "not-a-profile.txt"), "ignored");
        File.WriteAllText(Path.Combine(profilesDirectory, "also-not-a-profile.xml"), "<xml/>");
        var loader = new MachineProfileLoader(profilesDirectory);

        // Act
        var profiles = loader.AvailableProfiles;

        // Assert
        Assert.That(profiles, Has.Count.EqualTo(1));
        Assert.That(profiles, Contains.Item("valid-profile"));
    }

    /// <summary>
    /// Verifies that AvailableProfiles returns sorted list.
    /// </summary>
    [Test]
    public void AvailableProfiles_ReturnsSortedList()
    {
        // Arrange
        CreateTestProfile("zebra-profile");
        CreateTestProfile("alpha-profile");
        CreateTestProfile("middle-profile");
        var loader = new MachineProfileLoader(profilesDirectory);

        // Act
        var profiles = loader.AvailableProfiles;

        // Assert
        Assert.That(profiles, Has.Count.EqualTo(3));
        Assert.That(profiles[0], Is.EqualTo("alpha-profile"));
        Assert.That(profiles[1], Is.EqualTo("middle-profile"));
        Assert.That(profiles[2], Is.EqualTo("zebra-profile"));
    }

    /// <summary>
    /// Verifies that LoadProfile returns null when profile doesn't exist.
    /// </summary>
    [Test]
    public void LoadProfile_ProfileDoesNotExist_ReturnsNull()
    {
        // Arrange
        var loader = new MachineProfileLoader(profilesDirectory);

        // Act
        var profile = loader.LoadProfile("nonexistent-profile");

        // Assert
        Assert.That(profile, Is.Null);
    }

    /// <summary>
    /// Verifies that LoadProfile throws ArgumentNullException for null name.
    /// </summary>
    [Test]
    public void LoadProfile_NullName_ThrowsArgumentNullException()
    {
        // Arrange
        var loader = new MachineProfileLoader(profilesDirectory);

        // Act & Assert
        Assert.That(
            () => loader.LoadProfile(null!),
            Throws.ArgumentNullException.With.Property("ParamName").EqualTo("name"));
    }

    /// <summary>
    /// Verifies that LoadProfile throws ArgumentException for empty name.
    /// </summary>
    [Test]
    public void LoadProfile_EmptyName_ThrowsArgumentException()
    {
        // Arrange
        var loader = new MachineProfileLoader(profilesDirectory);

        // Act & Assert
        Assert.That(
            () => loader.LoadProfile(string.Empty),
            Throws.TypeOf<ArgumentException>());
    }

    /// <summary>
    /// Verifies that LoadProfile returns null for path traversal attempts.
    /// </summary>
    [Test]
    public void LoadProfile_PathTraversalAttempt_ReturnsNull()
    {
        // Arrange
        var loader = new MachineProfileLoader(profilesDirectory);

        // Act
        var profile = loader.LoadProfile("../../../etc/passwd");

        // Assert
        Assert.That(profile, Is.Null);
    }

    /// <summary>
    /// Verifies that LoadProfile returns null for names with path separators.
    /// </summary>
    /// <param name="name">The profile name containing path separators.</param>
    [Test]
    [TestCase("sub/profile")]
    [TestCase("sub\\profile")]
    public void LoadProfile_NameWithPathSeparator_ReturnsNull(string name)
    {
        // Arrange
        var loader = new MachineProfileLoader(profilesDirectory);

        // Act
        var profile = loader.LoadProfile(name);

        // Assert
        Assert.That(profile, Is.Null);
    }

    /// <summary>
    /// Verifies that LoadProfile correctly deserializes a valid profile.
    /// </summary>
    [Test]
    public void LoadProfile_ValidProfile_ReturnsDeserializedProfile()
    {
        // Arrange
        CreateTestProfile("my-machine", displayName: "My Custom Machine", cpuType: "65C02", memorySize: 65536);
        var loader = new MachineProfileLoader(profilesDirectory);

        // Act
        var profile = loader.LoadProfile("my-machine");

        // Assert
        Assert.That(profile, Is.Not.Null);
        Assert.That(profile!.Name, Is.EqualTo("my-machine"));
        Assert.That(profile.DisplayName, Is.EqualTo("My Custom Machine"));
        Assert.That(profile.Cpu.Type, Is.EqualTo("65C02"));
        Assert.That(profile.Memory.Size, Is.EqualTo(65536u));
    }

    /// <summary>
    /// Verifies that LoadProfile returns null for malformed JSON.
    /// </summary>
    [Test]
    public void LoadProfile_MalformedJson_ReturnsNull()
    {
        // Arrange
        File.WriteAllText(Path.Combine(profilesDirectory, "broken.json"), "{ this is not valid json }");
        var loader = new MachineProfileLoader(profilesDirectory);

        // Act
        var profile = loader.LoadProfile("broken");

        // Assert
        Assert.That(profile, Is.Null);
    }

    /// <summary>
    /// Verifies that LoadProfileFromFile throws FileNotFoundException for missing file.
    /// </summary>
    [Test]
    public void LoadProfileFromFile_FileDoesNotExist_ThrowsFileNotFoundException()
    {
        // Arrange
        var loader = new MachineProfileLoader(profilesDirectory);
        var nonExistentPath = Path.Combine(profilesDirectory, "does-not-exist.json");

        // Act & Assert
        Assert.That(
            () => loader.LoadProfileFromFile(nonExistentPath),
            Throws.TypeOf<FileNotFoundException>());
    }

    /// <summary>
    /// Verifies that LoadProfileFromFile throws ArgumentNullException for null path.
    /// </summary>
    [Test]
    public void LoadProfileFromFile_NullPath_ThrowsArgumentNullException()
    {
        // Arrange
        var loader = new MachineProfileLoader(profilesDirectory);

        // Act & Assert
        Assert.That(
            () => loader.LoadProfileFromFile(null!),
            Throws.ArgumentNullException.With.Property("ParamName").EqualTo("path"));
    }

    /// <summary>
    /// Verifies that LoadProfileFromFile correctly deserializes a valid profile.
    /// </summary>
    [Test]
    public void LoadProfileFromFile_ValidFile_ReturnsDeserializedProfile()
    {
        // Arrange
        var filePath = CreateTestProfile("from-file", displayName: "Loaded From File");
        var loader = new MachineProfileLoader(profilesDirectory);

        // Act
        var profile = loader.LoadProfileFromFile(filePath);

        // Assert
        Assert.That(profile, Is.Not.Null);
        Assert.That(profile.Name, Is.EqualTo("from-file"));
        Assert.That(profile.DisplayName, Is.EqualTo("Loaded From File"));
    }

    /// <summary>
    /// Verifies that LoadProfileFromFile can load files outside the profiles directory.
    /// </summary>
    [Test]
    public void LoadProfileFromFile_FileOutsideProfilesDirectory_LoadsSuccessfully()
    {
        // Arrange
        var externalDir = Path.Combine(testDirectory, "external");
        Directory.CreateDirectory(externalDir);
        var filePath = Path.Combine(externalDir, "external-profile.json");
        var profile = CreateProfileJson("external-profile", "External Profile");
        File.WriteAllText(filePath, profile);
        var loader = new MachineProfileLoader(profilesDirectory);

        // Act
        var loadedProfile = loader.LoadProfileFromFile(filePath);

        // Assert
        Assert.That(loadedProfile, Is.Not.Null);
        Assert.That(loadedProfile.Name, Is.EqualTo("external-profile"));
    }

    /// <summary>
    /// Verifies that DefaultProfile returns fallback when file doesn't exist.
    /// </summary>
    [Test]
    public void DefaultProfile_NoDefaultFile_ReturnsFallbackProfile()
    {
        // Arrange
        var loader = new MachineProfileLoader(profilesDirectory);

        // Act
        var profile = loader.DefaultProfile;

        // Assert
        Assert.That(profile, Is.Not.Null);
        Assert.That(profile.Name, Is.EqualTo("simple-65c02"));
        Assert.That(profile.Cpu.Type, Is.EqualTo("65C02"));
        Assert.That(profile.Memory.Size, Is.EqualTo(65536u));
    }

    /// <summary>
    /// Verifies that DefaultProfile returns file-based profile when it exists.
    /// </summary>
    [Test]
    public void DefaultProfile_DefaultFileExists_ReturnsFileBasedProfile()
    {
        // Arrange
        CreateTestProfile(
            "simple-65c02",
            displayName: "Custom Default",
            description: "Custom description",
            cpuType: "65C02",
            clockSpeed: 2000000,
            memorySize: 131072);
        var loader = new MachineProfileLoader(profilesDirectory);

        // Act
        var profile = loader.DefaultProfile;

        // Assert
        Assert.That(profile, Is.Not.Null);
        Assert.That(profile.Name, Is.EqualTo("simple-65c02"));
        Assert.That(profile.DisplayName, Is.EqualTo("Custom Default"));
        Assert.That(profile.Cpu.ClockSpeed, Is.EqualTo(2000000));
        Assert.That(profile.Memory.Size, Is.EqualTo(131072u));
    }

    /// <summary>
    /// Verifies that DefaultProfile is cached and returns same instance.
    /// </summary>
    [Test]
    public void DefaultProfile_MultipleCalls_ReturnsSameInstance()
    {
        // Arrange
        CreateTestProfile("simple-65c02");
        var loader = new MachineProfileLoader(profilesDirectory);

        // Act
        var profile1 = loader.DefaultProfile;
        var profile2 = loader.DefaultProfile;

        // Assert
        Assert.That(profile1, Is.SameAs(profile2));
    }

    /// <summary>
    /// Verifies that AvailableProfiles is cached and returns same instance.
    /// </summary>
    [Test]
    public void AvailableProfiles_MultipleCalls_ReturnsSameInstance()
    {
        // Arrange
        CreateTestProfile("test-profile");
        var loader = new MachineProfileLoader(profilesDirectory);

        // Act
        var profiles1 = loader.AvailableProfiles;
        var profiles2 = loader.AvailableProfiles;

        // Assert
        Assert.That(profiles1, Is.SameAs(profiles2));
    }

    /// <summary>
    /// Verifies that JSON with comments is handled correctly.
    /// </summary>
    [Test]
    public void LoadProfile_JsonWithComments_ParsesSuccessfully()
    {
        // Arrange
        var jsonWithComments = """
            {
                // This is a comment
                "name": "commented-profile",
                "displayName": "Profile with Comments",
                /* Multi-line
                   comment */
                "cpu": {
                    "type": "65C02",
                    "clockSpeed": 1000000
                },
                "memory": {
                    "size": 65536,
                    "type": "basic"
                }
            }
            """;
        File.WriteAllText(Path.Combine(profilesDirectory, "commented-profile.json"), jsonWithComments);
        var loader = new MachineProfileLoader(profilesDirectory);

        // Act
        var profile = loader.LoadProfile("commented-profile");

        // Assert
        Assert.That(profile, Is.Not.Null);
        Assert.That(profile!.Name, Is.EqualTo("commented-profile"));
    }

    /// <summary>
    /// Verifies that JSON with trailing commas is handled correctly.
    /// </summary>
    [Test]
    public void LoadProfile_JsonWithTrailingCommas_ParsesSuccessfully()
    {
        // Arrange
        var jsonWithTrailingCommas = """
            {
                "name": "trailing-comma-profile",
                "displayName": "Profile with Trailing Commas",
                "cpu": {
                    "type": "65C02",
                    "clockSpeed": 1000000,
                },
                "memory": {
                    "size": 65536,
                    "type": "basic",
                },
            }
            """;
        File.WriteAllText(Path.Combine(profilesDirectory, "trailing-comma-profile.json"), jsonWithTrailingCommas);
        var loader = new MachineProfileLoader(profilesDirectory);

        // Act
        var profile = loader.LoadProfile("trailing-comma-profile");

        // Assert
        Assert.That(profile, Is.Not.Null);
        Assert.That(profile!.Name, Is.EqualTo("trailing-comma-profile"));
    }

    /// <summary>
    /// Verifies that property names are case-insensitive.
    /// </summary>
    [Test]
    public void LoadProfile_CaseInsensitivePropertyNames_ParsesSuccessfully()
    {
        // Arrange
        var jsonWithMixedCase = """
            {
                "Name": "mixed-case-profile",
                "DISPLAYNAME": "Mixed Case Profile",
                "Cpu": {
                    "TYPE": "65C02",
                    "ClockSpeed": 1000000
                },
                "MEMORY": {
                    "SIZE": 65536,
                    "Type": "basic"
                }
            }
            """;
        File.WriteAllText(Path.Combine(profilesDirectory, "mixed-case-profile.json"), jsonWithMixedCase);
        var loader = new MachineProfileLoader(profilesDirectory);

        // Act
        var profile = loader.LoadProfile("mixed-case-profile");

        // Assert
        Assert.That(profile, Is.Not.Null);
        Assert.That(profile!.Name, Is.EqualTo("mixed-case-profile"));
        Assert.That(profile.DisplayName, Is.EqualTo("Mixed Case Profile"));
    }

    /// <summary>
    /// Verifies that the loader implements IMachineProfileLoader interface.
    /// </summary>
    [Test]
    public void MachineProfileLoader_ImplementsIMachineProfileLoader()
    {
        // Arrange
        var loader = new MachineProfileLoader(profilesDirectory);

        // Assert
        Assert.That(loader, Is.InstanceOf<IMachineProfileLoader>());
    }

    /// <summary>
    /// Creates a profile JSON string.
    /// </summary>
    /// <param name="name">The profile name.</param>
    /// <param name="displayName">The display name.</param>
    /// <param name="description">The description.</param>
    /// <param name="cpuType">The CPU type.</param>
    /// <param name="clockSpeed">The clock speed in Hz.</param>
    /// <param name="memorySize">The memory size in bytes.</param>
    /// <returns>The JSON string representing the profile.</returns>
    private static string CreateProfileJson(
        string name,
        string? displayName = null,
        string? description = null,
        string cpuType = "65C02",
        long clockSpeed = 1_000_000,
        uint memorySize = 65536)
    {
        var profile = new
        {
            name,
            displayName = displayName ?? $"{name} Display Name",
            description = description ?? $"Description for {name}",
            cpu = new
            {
                type = cpuType,
                clockSpeed,
            },
            memory = new
            {
                size = memorySize,
                type = "basic",
            },
        };

        return JsonSerializer.Serialize(profile, new JsonSerializerOptions { WriteIndented = true });
    }

    /// <summary>
    /// Creates a test profile JSON file.
    /// </summary>
    /// <param name="name">The profile name.</param>
    /// <param name="displayName">The display name.</param>
    /// <param name="description">The description.</param>
    /// <param name="cpuType">The CPU type.</param>
    /// <param name="clockSpeed">The clock speed in Hz.</param>
    /// <param name="memorySize">The memory size in bytes.</param>
    /// <returns>The full path to the created file.</returns>
    private string CreateTestProfile(
        string name,
        string? displayName = null,
        string? description = null,
        string cpuType = "65C02",
        long clockSpeed = 1_000_000,
        uint memorySize = 65536)
    {
        var json = CreateProfileJson(name, displayName, description, cpuType, clockSpeed, memorySize);
        var filePath = Path.Combine(profilesDirectory, $"{name}.json");
        File.WriteAllText(filePath, json);
        return filePath;
    }
}