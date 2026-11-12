// <copyright file="DiskFsInfoCommand.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Commands.DeviceCommands;

using BadMango.Emulator.Devices;
using BadMango.Emulator.Storage.FileSystems;

/// <summary>
/// Reports the detected file system on a mounted disk volume (DOS 3.3, ProDOS, Pascal).
/// </summary>
[DeviceDebugCommand]
public sealed class DiskFsInfoCommand : CommandHandlerBase, ICommandHelp
{
    public DiskFsInfoCommand() : base("disk-fsinfo", "Show detected Apple II file system info for a mounted volume") { }

    public override IReadOnlyList<string> Aliases { get; } = new[] { "diskfsinfo", "fsinfo" };

    public override string Usage => "disk-fsinfo <slot>:<drive> [--json]";

    public string Synopsis => "Report FS type, volume name, etc. for DOS 3.3 / ProDOS / Pascal.";

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
        string? mediaType = null;

        if (DiskRuntimeHelpers.TryGetSectorImage(context, slot, dIdx, out var sectorMedia, out _) && sectorMedia != null)
        {
            mediaType = "sector";
            fs = AppleFileSystemFactory.TryOpenDos33(sectorMedia);
        }

        if (fs == null && DiskRuntimeHelpers.TryGetBlockMedia(context, slot, dIdx, out var blockMedia, out _) && blockMedia != null)
        {
            mediaType = mediaType == null ? "block" : mediaType + "+block";
            fs = AppleFileSystemFactory.TryOpenProDos(blockMedia) ?? AppleFileSystemFactory.TryOpenPascal(blockMedia);
        }

        if (fs == null)
        {
            return CommandResult.Error("No supported Apple II file system detected on the volume (tried DOS 3.3, ProDOS, Pascal).");
        }

        // Compute extra info for DOS (free space from VTOC bitmap)
        int? freeSectors = null;
        if (fs.FileSystemType == "DOS 3.3" && DiskRuntimeHelpers.TryGetSectorImage(context, slot, dIdx, out var dosMedia, out _) && dosMedia != null)
        {
            try
            {
                int vtocT = 17, vtocS = 0;
                int phys = BadMango.Emulator.Storage.Gcr.SectorSkew.LogicalToPhysical(BadMango.Emulator.Storage.Media.SectorOrder.Dos33, vtocS);
                var vtoc = new byte[256];
                dosMedia.ReadSectorPhysical(vtocT, phys, vtoc);
                int free = 0;
                for (int t = 0; t < 35; t++)
                {
                    var bm = vtoc.AsSpan(0x38 + t * 4, 4);
                    free += BitCount(bm[0]) + BitCount(bm[1]) + BitCount(bm[2]) + BitCount(bm[3]);
                }
                freeSectors = free;
            }
            catch { }
        }
        else if (fs.FileSystemType.StartsWith("ProDOS") && DiskRuntimeHelpers.TryGetBlockMedia(context, slot, dIdx, out var proMedia, out _) && proMedia != null)
        {
            try
            {
                // Read volume dir key block (block 2) to get bitmap block
                var key = new byte[512];
                proMedia.ReadBlock(2, key);
                int bitmapBlock = key[0x1C] | (key[0x1D] << 8);
                // For small ProDOS volumes (e.g. 5.25"), bitmap is small, use first bitmap block
                if (bitmapBlock > 0)
                {
                    var bm = new byte[512];
                    proMedia.ReadBlock(bitmapBlock, bm);
                    int free = 0;
                    // Count bits in bitmap (1 = free). For 280 block volume ~35 bytes
                    for (int i = 0; i < 512; i++)
                    {
                        free += BitCount(bm[i]);
                    }
                    freeSectors = free;
                }
            }
            catch { }
        }

        if (useJson)
        {
            var info = new { slot, drive = driveOneBased, fs = fs.FileSystemType, volume = fs.VolumeName, media = mediaType, freeSectors };
            context.Output.WriteLine(System.Text.Json.JsonSerializer.Serialize(info, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
            return CommandResult.Ok();
        }

        context.Output.WriteLine($"File System Info for slot {slot} drive {driveOneBased}:");
        context.Output.WriteLine($"  Type:   {fs.FileSystemType}");
        context.Output.WriteLine($"  Volume: {fs.VolumeName}");
        context.Output.WriteLine($"  Media:  {mediaType ?? "unknown"}");
        if (freeSectors.HasValue) context.Output.WriteLine($"  Free:   {freeSectors} sectors");
        return CommandResult.Ok();
    }

    public string DetailedDescription => "Detects and reports the file system type and volume name on a mounted disk.";
    public IReadOnlyList<CommandOption> Options { get; } = [new("--json", "-j", "flag", "Structured output for MCP", null)];
    public IReadOnlyList<string> Examples { get; } = ["disk-fsinfo 6:1", "disk fsinfo 6:1 --json"];
    public string? SideEffects => null;
    public IReadOnlyList<string> SeeAlso { get; } = ["disk", "disk-ls", "disk-readfile"];

    private static int BitCount(byte b)
    {
        int c = 0;
        while (b != 0) { c += b & 1; b >>= 1; }
        return c;
    }
}
