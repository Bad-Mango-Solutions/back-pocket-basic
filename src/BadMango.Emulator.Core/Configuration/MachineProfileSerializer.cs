// <copyright file="MachineProfileSerializer.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Core.Configuration;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Serializes and deserializes machine profiles with round-trip fidelity.
/// </summary>
/// <remarks>
/// <para>
/// This serializer provides consistent JSON formatting for machine profiles,
/// ensuring that profiles can be serialized and deserialized without loss of
/// information. The serialization format matches the JSON schema.
/// </para>
/// <para>
/// The serializer supports the full machine profile schema including:
/// <list type="bullet">
/// <item><description>Memory regions, swap groups, and controllers</description></item>
/// <item><description>Slot system with card configurations</description></item>
/// <item><description>ROM definitions with hash validation</description></item>
/// <item><description>Device configurations with presets</description></item>
/// <item><description>Boot configuration</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class MachineProfileSerializer
{
    private static readonly JsonSerializerOptions SerializeOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private static readonly JsonSerializerOptions DeserializeOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    /// <summary>
    /// Serializes a machine profile to JSON.
    /// </summary>
    /// <param name="profile">The profile to serialize.</param>
    /// <returns>The JSON string representation of the profile.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="profile"/> is <see langword="null"/>.</exception>
    public string Serialize(MachineProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        return JsonSerializer.Serialize(profile, SerializeOptions);
    }

    /// <summary>
    /// Deserializes a machine profile from JSON.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized machine profile.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Thrown when deserialization fails.</exception>
    public MachineProfile Deserialize(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        return JsonSerializer.Deserialize<MachineProfile>(json, DeserializeOptions)
            ?? throw new InvalidOperationException("Failed to deserialize profile: result was null.");
    }

    /// <summary>
    /// Serializes a machine profile to a file.
    /// </summary>
    /// <param name="profile">The profile to serialize.</param>
    /// <param name="filePath">The path to write the JSON file.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="profile"/> or <paramref name="filePath"/> is <see langword="null"/>.
    /// </exception>
    public void SerializeToFile(MachineProfile profile, string filePath)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        string json = Serialize(profile);
        File.WriteAllText(filePath, json);
    }

    /// <summary>
    /// Deserializes a machine profile from a file.
    /// </summary>
    /// <param name="filePath">The path to the JSON file.</param>
    /// <returns>The deserialized machine profile.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="filePath"/> is <see langword="null"/>.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the file does not exist.</exception>
    /// <exception cref="InvalidOperationException">Thrown when deserialization fails.</exception>
    public MachineProfile DeserializeFromFile(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Profile file not found: {filePath}", filePath);
        }

        string json = File.ReadAllText(filePath);
        return Deserialize(json);
    }

    /// <summary>
    /// Validates that a profile can be round-tripped without data loss.
    /// </summary>
    /// <param name="profile">The profile to validate.</param>
    /// <returns><see langword="true"/> if the profile round-trips successfully; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="profile"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// This method serializes the profile to JSON and then deserializes it back,
    /// then serializes again to compare the JSON output. If the two JSON strings
    /// are identical, the round-trip is considered successful.
    /// </remarks>
    public bool ValidateRoundTrip(MachineProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        try
        {
            string json1 = Serialize(profile);
            var deserialized = Deserialize(json1);
            string json2 = Serialize(deserialized);

            return string.Equals(json1, json2, StringComparison.Ordinal);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Creates a deep copy of a machine profile through serialization.
    /// </summary>
    /// <param name="profile">The profile to clone.</param>
    /// <returns>A deep copy of the profile.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="profile"/> is <see langword="null"/>.</exception>
    public MachineProfile Clone(MachineProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        return Deserialize(Serialize(profile));
    }
}