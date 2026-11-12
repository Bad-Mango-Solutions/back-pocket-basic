// <copyright file="DiskLsCommand.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Commands.DeviceCommands;

using BadMango.Emulator.Devices;
using BadMango.Emulator.Storage.FileSystems;
using BadMango.Emulator.Storage.Formats;
using BadMango.Emulator.Storage.Gcr;
using BadMango.Emulator.Storage.Media;

/// <summary>
/// Lists files on a mounted Apple II disk using the native file system (DOS 3.3, ProDOS, Pascal).
/// </summary>
[DeviceDebugCommand]
public sealed class DiskLsCommand : CommandHandlerBase, ICommandHelp
{
    public DiskLsCommand() : base("disk-ls", "List files on a mounted Apple II disk volume") { }

    public override IReadOnlyList<string> Aliases { get; } = new[] { "diskls", "lsdisk" };

    public override string Usage => "disk-ls <slot>:<drive> [path] [--json]";

    public string Synopsis => this.Usage;

    public override CommandResult Execute(ICommandContext context, string[] args)
    {
        if (args.Length < 1)
            return CommandResult.Error("Usage: " + Usage);

        if (!DiskRuntimeHelpers.TryParseSlotDrive(args[0], out int slot, out int driveOneBased, out var err))
            return CommandResult.Error(err!);

        if (!DiskRuntimeHelpers.TryGetSlotManager(context, out var sm, out var smErr) || sm is null)
            return CommandResult.Error(smErr!);

        if (!DiskRuntimeHelpers.TryGetController(sm, slot, driveOneBased, out var ctrl, out int dIdx, out var cErr) || ctrl is null)
            return CommandResult.Error(cErr!);

        bool useJson = context.JsonOutput || args.Any(a => a is "--json" or "-j");

        IAppleFileSystem? fs = null;

        if (DiskRuntimeHelpers.TryGetSectorImage(context, slot, dIdx, out var sectorMedia, out _) && sectorMedia != null)
        {
            fs = AppleFileSystemFactory.TryOpenDos33(sectorMedia);
        }

        if (fs == null && DiskRuntimeHelpers.TryGetBlockMedia(context, slot, dIdx, out var blockMedia, out _) && blockMedia != null)
        {
            fs = AppleFileSystemFactory.TryOpenProDos(blockMedia);
        }

        if (fs == null)
        {
            return CommandResult.Error("Could not open a supported file system (DOS 3.3, ProDOS, Pascal) for the mounted disk.");
        }

        var path = (args.Length > 1 && !args[1].StartsWith("-")) ? args[1] : "";
        var entries = fs.ListDirectory(path);

        if (useJson)
        {
            var list = entries.Select(e => new { e.Name, e.Type, e.Size, locked = e.IsLocked, dir = e.IsDirectory }).ToList();
            context.Output.WriteLine(System.Text.Json.JsonSerializer.Serialize(new { volume = fs.VolumeName, fs = fs.FileSystemType, path, entries = list }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
            return CommandResult.Ok();
        }

        context.Output.WriteLine($"{fs.FileSystemType} volume: {fs.VolumeName}");
        context.Output.WriteLine("Name                              Type     Size");
        context.Output.WriteLine(new string('-', 50));
        foreach (var e in entries)
        {
            context.Output.WriteLine($"{e.Name,-32} {e.Type,-8} {e.Size,8}");
        }

        return CommandResult.Ok($"{entries.Count} entries");
    }

    public string DetailedDescription => "Lists files using the native Apple II file system parser (DOS 3.3 catalog, ProDOS dir, etc.).";
    public IReadOnlyList<CommandOption> Options { get; } = [ new("--json", "-j", "flag", "JSON output for agents", null) ];
    public IReadOnlyList<string> Examples { get; } = ["disk-ls 6:1", "disk ls 6:1 --json"];
    public string? SideEffects => "None (read only).";
    public IReadOnlyList<string> SeeAlso { get; } = ["disk", "disk-insert", "disk-read-sector"];

    private static int ListDos33Files(SectorImageMedia media, bool useJson, System.IO.TextWriter output)
    {
        // DOS 3.3 VTOC is at track 17, DOS logical sector 0
        const int vtocT = 17;
        const int vtocS = 0;

        int physicalS = SectorSkew.LogicalToPhysical(SectorOrder.Dos33, vtocS);
        var vtoc = new byte[256];
        media.ReadSectorPhysical(vtocT, physicalS, vtoc);

        if (vtoc[0x35] != 0x10) // not 16 sectors
            return 0;

        var files = new System.Collections.Generic.List<object>();
        int catT = vtoc[0x01];
        int catS = vtoc[0x02];

        int count = 0;
        int guard = 0;
        while (catT != 0 && guard++ < 16)
        {
            physicalS = SectorSkew.LogicalToPhysical(SectorOrder.Dos33, catS);
            var cat = new byte[256];
            media.ReadSectorPhysical(catT, physicalS, cat);

            for (int i = 0; i < 7; i++)
            {
                int off = 0x0B + i * 0x23;
                byte tb = cat[off];
                if (tb == 0) continue;

                var nameBytes = new byte[30];
                for (int b = 0; b < 30; b++) nameBytes[b] = (byte)(cat[off + 3 + b] & 0x7F);
                string name = System.Text.Encoding.ASCII.GetString(nameBytes).TrimEnd((char)0, ' ');
                if (string.IsNullOrWhiteSpace(name) || name.All(c => c < ' ' || c > '~')) continue;
                int typ = tb & 0x7F;
                bool locked = (tb & 0x80) != 0;
                string type = typ switch { 0 => "T", 1 => "I", 2 => "A", 4 => "B", _ => $"${typ:X2}" };

                files.Add(new { name, type, locked });
                count++;
            }

            catT = cat[1];
            catS = cat[2];
        }

        if (useJson)
        {
            output.WriteLine(System.Text.Json.JsonSerializer.Serialize(new { fs = "DOS 3.3", files }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
        }
        else
        {
            output.WriteLine("DOS 3.3 files:");
            foreach (var f in files)
            {
                output.WriteLine($"  {f}");
            }
        }
        return count;
    }

    // ICommandHelp implementation

}
