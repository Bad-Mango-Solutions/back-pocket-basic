// <copyright file="PascalFileSystem.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Storage.FileSystems;

using System.Text;

using BadMango.Emulator.Storage.Media;

/// <summary>
/// Basic Apple Pascal (UCSD Pascal) file system support for debug and MCP tooling.
/// </summary>
/// <remarks>
/// Pascal uses 512-byte blocks. This minimal implementation focuses on volume detection and a basic directory listing.
/// </remarks>
public sealed class PascalFileSystem : IAppleFileSystem
{
    private readonly Func<int, byte[]> readBlock;
    private readonly List<FileEntry> entries = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="PascalFileSystem"/> class.
    /// </summary>
    /// <param name="readBlock">A callback that reads a Pascal block by number.</param>
    public PascalFileSystem(Func<int, byte[]> readBlock)
    {
        this.readBlock = readBlock ?? throw new ArgumentNullException(nameof(readBlock));
        VolumeName = "PASCAL-VOL";
        ParseVolume();
    }

    /// <inheritdoc />
    public string VolumeName { get; private set; }

    /// <inheritdoc />
    public string FileSystemType => "Pascal";

    /// <inheritdoc />
    public IReadOnlyList<FileEntry> ListDirectory(string path = "")
    {
        return entries;
    }

    /// <inheritdoc />
    public byte[] ReadFile(string path)
    {
        if (!TryReadFile(path, out var data) || data is null)
        {
            throw new FileNotFoundException(path);
        }

        return data;
    }

    /// <inheritdoc />
    public bool TryReadFile(string path, out byte[]? data)
    {
        data = null;
        return false;
    }

    private void ParseVolume()
    {
        entries.Clear();
        try
        {
            var dirBlock = readBlock(2);
            if (dirBlock.Length >= 0x20)
            {
                int nameLen = dirBlock[0x06] & 0x0F;
                if (nameLen > 0 && nameLen <= 7 && 0x07 + nameLen < dirBlock.Length)
                {
                    var nameBytes = new byte[nameLen];
                    for (int i = 0; i < nameLen; i++)
                    {
                        nameBytes[i] = (byte)(dirBlock[0x07 + i] & 0x7F);
                    }

                    var volName = Encoding.ASCII.GetString(nameBytes).Trim();
                    if (!string.IsNullOrWhiteSpace(volName))
                    {
                        VolumeName = volName;
                    }
                }
            }

            entries.Add(new FileEntry
            {
                Name = VolumeName,
                Type = "VOL",
                Size = 0,
                IsLocked = false,
                IsDirectory = true,
                FullPath = "/"
            });
        }
        catch
        {
        }
    }
}