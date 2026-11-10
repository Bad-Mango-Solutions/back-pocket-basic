// <copyright file="IAppleFileSystem.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Storage.FileSystems;

/// <summary>
/// Represents a high-level view of an Apple II file system on a disk volume.
/// Supports listing directories and reading files for debug and MCP use.
/// </summary>
public interface IAppleFileSystem
{
    /// <summary>
    /// Gets the name of the volume, if it can be determined.
    /// </summary>
    string VolumeName { get; }

    /// <summary>
    /// Gets the human-readable file system type name.
    /// </summary>
    string FileSystemType { get; }

    /// <summary>
    /// Lists the contents of the root or a specified directory.
    /// </summary>
    /// <param name="path">The directory path to query; empty or "/" indicates the root.</param>
    /// <returns>The file entries contained in the directory.</returns>
    IReadOnlyList<FileEntry> ListDirectory(string path = "");

    /// <summary>
    /// Reads a file from the volume as raw bytes.
    /// </summary>
    /// <param name="path">The path of the file to read.</param>
    /// <returns>The file contents as a byte array.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the file does not exist.</exception>
    byte[] ReadFile(string path);

    /// <summary>
    /// Attempts to read a file from the volume.
    /// </summary>
    /// <param name="path">The path of the file to read.</param>
    /// <param name="data">When this method returns <see langword="true"/>, contains the file contents; otherwise, <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if the file was read; otherwise, <see langword="false"/>.</returns>
    bool TryReadFile(string path, out byte[]? data);
}

/// <summary>
/// Lightweight description of a file or directory entry.
/// </summary>
public sealed class FileEntry
{
    /// <summary>
    /// Gets the entry name.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets the file type tag for the entry.
    /// </summary>
    public string Type { get; init; } = string.Empty;

    /// <summary>
    /// Gets the entry size in bytes.
    /// </summary>
    public int Size { get; init; }

    /// <summary>
    /// Gets a value indicating whether the entry is locked.
    /// </summary>
    public bool IsLocked { get; init; }

    /// <summary>
    /// Gets a value indicating whether the entry is a directory.
    /// </summary>
    public bool IsDirectory { get; init; }

    /// <summary>
    /// Gets the full path of the entry, if known.
    /// </summary>
    public string? FullPath { get; init; }

    /// <inheritdoc />
    public override string ToString() => $"{Name} ({Type}, {Size} bytes{(IsLocked ? ", locked" : string.Empty)})";
}