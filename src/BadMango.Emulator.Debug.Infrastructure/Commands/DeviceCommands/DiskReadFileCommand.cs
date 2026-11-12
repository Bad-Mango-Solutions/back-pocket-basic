// <copyright file="DiskReadFileCommand.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Commands.DeviceCommands;

using System.Text;

using BadMango.Emulator.Devices;
using BadMango.Emulator.Storage.FileSystems;
using BadMango.Emulator.Storage.Formats;
using BadMango.Emulator.Storage.Gcr;
using BadMango.Emulator.Storage.Media;

/// <summary>
/// Reads a file from the Apple II file system on a mounted disk (DOS 3.3 first).
/// </summary>
[DeviceDebugCommand]
public sealed class DiskReadFileCommand : CommandHandlerBase, ICommandHelp
{
    public DiskReadFileCommand() : base("disk-readfile", "Read a file from the native file system on a mounted disk") { }

    public override IReadOnlyList<string> Aliases { get; } = new[] { "diskreadfile", "catdisk" };

    public override string Usage => "disk-readfile <slot>:<drive> <filename> [--hex] [--json]";

    public string Synopsis => Usage;

    public override CommandResult Execute(ICommandContext context, string[] args)
    {
        if (args.Length < 2)
            return CommandResult.Error(Usage);

        if (!DiskRuntimeHelpers.TryParseSlotDrive(args[0], out int slot, out int drv, out var perr))
            return CommandResult.Error(perr!);

        string filename = args[1];

        bool hex = args.Any(a => a is "--hex" or "-x");
        bool json = context.JsonOutput || args.Any(a => a is "--json" or "-j");

        string? outPath = null;
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--to" || args[i] == "--out")
            {
                if (i + 1 < args.Length) outPath = args[i + 1];
                break;
            }
        }

        IAppleFileSystem? fs = null;
        if (DiskRuntimeHelpers.TryGetSectorImage(context, slot, drv - 1, out var sectorMedia, out var merr) && sectorMedia != null)
        {
            fs = AppleFileSystemFactory.TryOpenDos33(sectorMedia);
        }
        if (fs == null && DiskRuntimeHelpers.TryGetBlockMedia(context, slot, drv - 1, out var blockMedia, out _) && blockMedia != null)
        {
            fs = AppleFileSystemFactory.TryOpenProDos(blockMedia);
        }

        if (fs == null)
        {
            return CommandResult.Error("Could not open file system view for the disk (tried DOS 3.3 and ProDOS).");
        }

        if (!fs.TryReadFile(filename, out var fileData) || fileData == null)
        {
            return CommandResult.Error($"File '{filename}' not found or unreadable on {fs.FileSystemType} volume.");
        }

        if (outPath != null)
        {
            System.IO.File.WriteAllBytes(outPath, fileData);
            if (json)
            {
                context.Output.WriteLine(System.Text.Json.JsonSerializer.Serialize(new { slot, drive = drv, filename, savedTo = outPath, size = fileData.Length }));
            }
            else
            {
                context.Output.WriteLine($"Wrote {fileData.Length} bytes to {outPath}");
            }
            return CommandResult.Ok();
        }

        if (json)
        {
            string content = hex ? BitConverter.ToString(fileData).Replace("-", " ") : Encoding.ASCII.GetString(fileData);
            context.Output.WriteLine(System.Text.Json.JsonSerializer.Serialize(new { slot, drive = drv, filename, size = fileData.Length, hex, content = content.Length > 4096 ? content.Substring(0, 4096) + "..." : content }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
        }
        else
        {
            if (hex)
            {
                for (int i = 0; i < fileData.Length; i += 16)
                {
                    var line = BitConverter.ToString(fileData, i, Math.Min(16, fileData.Length - i)).Replace("-", " ");
                    context.Output.WriteLine($"{i:X4}: {line}");
                }
            }
            else
            {
                context.Output.WriteLine(Encoding.ASCII.GetString(fileData));
            }
        }

        return CommandResult.Ok();
    }

    public string DetailedDescription => "Reads file contents from DOS 3.3/ProDOS catalog on the mounted volume. Use --to <path> to extract to host. JSON output for agents.";
    public IReadOnlyList<CommandOption> Options { get; } = [
        new("--json", "-j", "flag", "Structured output", null),
        new("--hex", "-x", "flag", "Hex dump", null),
        new("--to <hostpath>", null, "path", "Write file to host filesystem instead of printing", null)
    ];
    public IReadOnlyList<string> Examples { get; } = ["disk-readfile 6:1 HELLO", "disk readfile 6:1 MYFILE --json", "disk readfile 6:1 PROG.BIN --to ./prog.bin"];
    public string? SideEffects => "None for read; --to writes to host disk.";
    public IReadOnlyList<string> SeeAlso { get; } = ["disk-ls", "disk", "disk-read-sector"];
}
