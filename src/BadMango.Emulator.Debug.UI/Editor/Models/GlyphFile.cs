// <copyright file="GlyphFile.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.UI.Editor.Models;

/// <summary>
/// Represents a complete 4KB glyph file with two character sets.
/// </summary>
/// <remarks>
/// <para>
/// Glyph files are 4096 bytes (4KB), containing two 2KB character sets:
/// </para>
/// <list type="bullet">
/// <item><description>$0000-$07FF: Primary character set (256 chars × 8 bytes)</description></item>
/// <item><description>$0800-$0FFF: Alternate character set (256 chars × 8 bytes)</description></item>
/// </list>
/// </remarks>
public sealed class GlyphFile
{
    /// <summary>
    /// The total file size in bytes (4KB).
    /// </summary>
    public const int FileSize = 4096;

    /// <summary>
    /// The size of each character set in bytes (2KB).
    /// </summary>
    public const int CharacterSetSize = 2048;

    /// <summary>
    /// The number of characters per set.
    /// </summary>
    public const int CharactersPerSet = 256;

    /// <summary>
    /// The number of bytes per character.
    /// </summary>
    public const int BytesPerCharacter = 8;

    private readonly CharacterGlyph[] primarySet = new CharacterGlyph[CharactersPerSet];
    private readonly CharacterGlyph[] alternateSet = new CharacterGlyph[CharactersPerSet];

    /// <summary>
    /// Initializes a new instance of the <see cref="GlyphFile"/> class.
    /// </summary>
    private GlyphFile()
    {
    }

    /// <summary>
    /// Gets the file path, or null if this is a new unsaved file.
    /// </summary>
    public string? FilePath { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the file has unsaved modifications.
    /// </summary>
    public bool IsModified { get; private set; }

    /// <summary>
    /// Gets the primary character set.
    /// </summary>
    public IReadOnlyList<CharacterGlyph> PrimarySet => primarySet;

    /// <summary>
    /// Gets the alternate character set.
    /// </summary>
    public IReadOnlyList<CharacterGlyph> AlternateSet => alternateSet;

    /// <summary>
    /// Creates a new empty glyph file.
    /// </summary>
    /// <returns>A new <see cref="GlyphFile"/> with empty glyphs.</returns>
    public static GlyphFile CreateNew()
    {
        var file = new GlyphFile();
        for (int i = 0; i < CharactersPerSet; i++)
        {
            file.primarySet[i] = new CharacterGlyph();
            file.alternateSet[i] = new CharacterGlyph();
        }

        return file;
    }

    /// <summary>
    /// Loads a glyph file from disk.
    /// </summary>
    /// <param name="path">The file path to load from.</param>
    /// <returns>A <see cref="GlyphFile"/> containing the loaded data.</returns>
    /// <exception cref="InvalidDataException">
    /// Thrown when the file is not exactly 4096 bytes.
    /// </exception>
    public static GlyphFile Load(string path)
    {
        var data = File.ReadAllBytes(path);
        if (data.Length != FileSize)
        {
            throw new InvalidDataException(
                $"Glyph file must be exactly {FileSize} bytes, but got {data.Length}.");
        }

        var file = new GlyphFile { FilePath = path };

        for (int i = 0; i < CharactersPerSet; i++)
        {
            file.primarySet[i] = new CharacterGlyph();
            file.primarySet[i].CopyFrom(data.AsSpan(i * BytesPerCharacter, BytesPerCharacter));

            file.alternateSet[i] = new CharacterGlyph();
            file.alternateSet[i].CopyFrom(data.AsSpan(CharacterSetSize + (i * BytesPerCharacter), BytesPerCharacter));
        }

        return file;
    }

    /// <summary>
    /// Loads glyph data from a byte array.
    /// </summary>
    /// <param name="data">The 4KB byte array containing glyph data.</param>
    /// <returns>A <see cref="GlyphFile"/> containing the loaded data.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="data"/> is not exactly 4096 bytes.
    /// </exception>
    public static GlyphFile LoadFromBytes(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        if (data.Length != FileSize)
        {
            throw new ArgumentException(
                $"Data must be exactly {FileSize} bytes, but got {data.Length}.",
                nameof(data));
        }

        var file = new GlyphFile();

        for (int i = 0; i < CharactersPerSet; i++)
        {
            file.primarySet[i] = new CharacterGlyph();
            file.primarySet[i].CopyFrom(data.AsSpan(i * BytesPerCharacter, BytesPerCharacter));

            file.alternateSet[i] = new CharacterGlyph();
            file.alternateSet[i].CopyFrom(data.AsSpan(CharacterSetSize + (i * BytesPerCharacter), BytesPerCharacter));
        }

        return file;
    }

    /// <summary>
    /// Gets a glyph from the specified character set.
    /// </summary>
    /// <param name="charCode">Character code (0-255).</param>
    /// <param name="useAlternateSet">True for alternate set, false for primary.</param>
    /// <returns>The requested <see cref="CharacterGlyph"/>.</returns>
    public CharacterGlyph GetGlyph(byte charCode, bool useAlternateSet)
    {
        return useAlternateSet ? alternateSet[charCode] : primarySet[charCode];
    }

    /// <summary>
    /// Marks the file as modified.
    /// </summary>
    public void MarkModified()
    {
        IsModified = true;
    }

    /// <summary>
    /// Clears the modified flag (called after save).
    /// </summary>
    public void ClearModified()
    {
        IsModified = false;
    }

    /// <summary>
    /// Saves the glyph file to disk.
    /// </summary>
    /// <param name="path">
    /// The file path to save to, or null to use the existing <see cref="FilePath"/>.
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no file path is specified and <see cref="FilePath"/> is null.
    /// </exception>
    public void Save(string? path = null)
    {
        path ??= FilePath ?? throw new InvalidOperationException("No file path specified.");

        var data = new byte[FileSize];

        for (int i = 0; i < CharactersPerSet; i++)
        {
            primarySet[i].CopyTo(data.AsSpan(i * BytesPerCharacter, BytesPerCharacter));
            alternateSet[i].CopyTo(data.AsSpan(CharacterSetSize + (i * BytesPerCharacter), BytesPerCharacter));
        }

        File.WriteAllBytes(path, data);
        FilePath = path;
        IsModified = false;
    }

    /// <summary>
    /// Gets the raw 4KB byte array for hot-loading or export.
    /// </summary>
    /// <returns>A 4096-byte array containing all glyph data.</returns>
    public byte[] ToByteArray()
    {
        var data = new byte[FileSize];
        for (int i = 0; i < CharactersPerSet; i++)
        {
            primarySet[i].CopyTo(data.AsSpan(i * BytesPerCharacter, BytesPerCharacter));
            alternateSet[i].CopyTo(data.AsSpan(CharacterSetSize + (i * BytesPerCharacter), BytesPerCharacter));
        }

        return data;
    }
}