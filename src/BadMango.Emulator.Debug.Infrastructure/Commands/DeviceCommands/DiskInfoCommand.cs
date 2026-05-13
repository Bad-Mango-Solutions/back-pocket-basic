// <copyright file="DiskInfoCommand.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Commands.DeviceCommands;

using System.Globalization;

using BadMango.Emulator.Devices;
using BadMango.Emulator.Storage.Formats;

/// <summary>
/// Reports format / geometry / write-protect / sniffed-order metadata for a disk image
/// without mounting it.
/// </summary>
/// <remarks>
/// <para>
/// Implements <c>disk info &lt;path&gt;</c>. Resolves only the
/// <see cref="Storage.Formats.DiskImageFactory"/> and <see cref="IDebugPathResolver"/>
/// from the supplied context — no machine is required.
/// </para>
/// <para>
/// For ambiguous <c>.dsk</c> images, also reports the sniffed sector ordering per
/// PRD §10 decision 5. For <c>.2mg</c>/<c>.2img</c> images, reports the parsed 2MG
/// header metadata (creator, format code, write-protect flag, embedded DOS volume).
/// </para>
/// </remarks>
[DeviceDebugCommand]
public sealed class DiskInfoCommand : CommandHandlerBase, ICommandHelp
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DiskInfoCommand"/> class.
    /// </summary>
    public DiskInfoCommand()
        : base("disk-info", "Report format / geometry / metadata of a disk image without mounting it")
    {
    }

    /// <inheritdoc/>
    public override IReadOnlyList<string> Aliases { get; } = ["diskinfo"];

    /// <inheritdoc/>
    public override string Usage => "disk-info <path>";

    /// <inheritdoc/>
    public string Synopsis => this.Usage;

    /// <inheritdoc/>
    public string DetailedDescription =>
        "Reports the format DiskImageFactory would pick for the supplied image, the " +
        "geometry, the write-protect flag, the sniffed .dsk sector ordering (per PRD " +
        "§10 decision 5) and any 2MG header metadata. The image is opened read-only and " +
        "is not mounted on any controller.";

    /// <inheritdoc/>
    public IReadOnlyList<CommandOption> Options { get; } = [];

    /// <inheritdoc/>
    public IReadOnlyList<string> Examples { get; } =
    [
        "disk-info game.dsk",
        "disk-info hd32m.hdv",
        "disk-info wrapped.2mg",
    ];

    /// <inheritdoc/>
    public string? SideEffects => "None. The image is opened read-only and not mounted.";

    /// <inheritdoc/>
    public IReadOnlyList<string> SeeAlso { get; } = ["disk", "disk-create"];

    /// <inheritdoc/>
    public override CommandResult Execute(ICommandContext context, string[] args)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(args);

        if (args.Length == 0)
        {
            return CommandResult.Error("Path required. Usage: disk-info <path>");
        }

        if (args.Length > 1)
        {
            return CommandResult.Error("Too many arguments. Usage: disk-info <path>");
        }

        var factory = (context as IDebugContext)?.DiskImageFactory;
        if (factory is null)
        {
            return CommandResult.Error("DiskImageFactory not available on debug context.");
        }

        var rawPath = args[0];
        string path = rawPath;
        var resolver = (context as IDebugContext)?.PathResolver;
        if (resolver is not null)
        {
            if (!resolver.TryResolve(rawPath, out var resolved))
            {
                return CommandResult.Error($"Cannot resolve path: '{rawPath}'.");
            }

            path = resolved!;
        }

        if (!File.Exists(path))
        {
            return CommandResult.Error($"File not found: '{path}'.");
        }

        // Read the 2MG header (if any) BEFORE opening through the factory: the factory
        // takes a writable handle with FileShare.None for non-read-only opens, which
        // would otherwise block this read.
        var ext = Path.GetExtension(path).ToLowerInvariant();
        TwoImgHeader? twoImgHeader = null;
        if (ext is ".2mg" or ".2img")
        {
            try
            {
                var head = ReadFirstBytes(path, 64);
                if (head.Length >= 64 && head.AsSpan(0, 4).SequenceEqual(TwoImgHeader.Magic))
                {
                    twoImgHeader = TwoImgHeader.Parse(head);
                }
            }
            catch (IOException)
            {
                // Reported below, with the rest of disk-info.
            }
        }

        DiskImageOpenResult open;
        try
        {
            // Always open read-only to avoid leaking a writable file handle: the underlying
            // FileStorageBackend uses FileShare.None for writable opens (and FileShare.Read
            // for read-only opens), either of which would prevent the user from renaming or
            // deleting the file with their OS until the process exits.
            // The intrinsic write-protect state is computed separately below.
            // The result is disposed at the end of this method via the using declaration so
            // the file handle is released as soon as 'disk info' completes.
            open = factory.Open(path, forceReadOnly: true);
        }
        catch (Exception ex) when (ex is InvalidDataException or NotSupportedException)
        {
            return CommandResult.Error($"Cannot identify '{path}': {ex.Message}");
        }

        using (open)
        {
            // Intrinsic write-protect: OS read-only attribute, OR 2MG header bit if present.
            // We do NOT consult open.IsReadOnly because we forced read-only above, which would
            // always report "yes" regardless of the file's true state.
            var fileInfo = new FileInfo(path);
            var osReadOnly = (fileInfo.Attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly;
            var intrinsicWriteProtected = osReadOnly || (twoImgHeader is TwoImgHeader hdr && hdr.IsWriteProtected);

            var output = context.Output;
            output.WriteLine();
            output.WriteLine($"Disk image: {path}");
            output.WriteLine($"  File size:        {fileInfo.Length} bytes");
            output.WriteLine($"  Detected format:  {open.Format}");
            output.WriteLine($"  Write-protected:  {(intrinsicWriteProtected ? "yes" : "no")}");

            switch (open)
            {
                case Image525AndBlockResult both:
                    {
                        var geom = both.TrackMedia.Geometry;
                        output.WriteLine($"  Media kind:       5.25\" sector image (track + block views)");
                        output.WriteLine(string.Format(CultureInfo.InvariantCulture, "  Geometry:         {0} tracks x {1} sectors x {2} bytes/sector", geom.TrackCount, geom.SectorsPerTrack, geom.BytesPerSector));
                        output.WriteLine($"  Sector order:     {both.SectorOrder}");
                        output.WriteLine($"  Block count:      {both.BlockMedia.BlockCount} (512-byte blocks)");
                        if (Path.GetExtension(path).Equals(".dsk", StringComparison.OrdinalIgnoreCase))
                        {
                            output.WriteLine($"  .dsk sniffed:     {(both.WasOrderSniffed ? "yes (matched signature)" : "no (fell back to DOS)")}");
                        }

                        break;
                    }

                case Image525Result trackOnly:
                    {
                        var geom = trackOnly.Media.Geometry;
                        output.WriteLine($"  Media kind:       5.25\" nibble image (track view only)");
                        output.WriteLine(string.Format(CultureInfo.InvariantCulture, "  Geometry:         {0} tracks", geom.TrackCount));
                        break;
                    }

                case ImageBlockResult blockOnly:
                    {
                        output.WriteLine($"  Media kind:       block image");
                        output.WriteLine($"  Block count:      {blockOnly.Media.BlockCount} ({blockOnly.Media.BlockSize}-byte blocks)");
                        break;
                    }
            }

            // For 2MG images, report header metadata too.
            if (twoImgHeader is TwoImgHeader header)
            {
                output.WriteLine($"  2MG creator:      {header.Creator}");
                output.WriteLine(string.Format(CultureInfo.InvariantCulture, "  2MG header len:   {0} bytes", header.HeaderLength));
                output.WriteLine(string.Format(CultureInfo.InvariantCulture, "  2MG payload:      offset {0}, length {1}", header.DataOffset, header.DataLength));
                output.WriteLine(string.Format(CultureInfo.InvariantCulture, "  2MG format code:  {0} ({1})", header.Format, FormatCodeName(header.Format)));
                output.WriteLine(string.Format(CultureInfo.InvariantCulture, "  2MG flags:        0x{0:X8}", header.Flags));
                output.WriteLine($"  2MG write-prot:   {(header.IsWriteProtected ? "yes" : "no")}");
                if (header.HasDosVolumeNumber)
                {
                    output.WriteLine(string.Format(CultureInfo.InvariantCulture, "  2MG DOS volume:   {0}", header.DosVolumeNumber));
                }
            }

            output.WriteLine();
            return CommandResult.Ok();
        }
    }

    private static byte[] ReadFirstBytes(string path, int count)
    {
        // Use FileShare.ReadWrite so this read still succeeds when another handle (e.g.
        // the open returned by DiskImageFactory) holds the file with FileShare.None.
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        var len = (int)Math.Min(count, fs.Length);
        var buf = new byte[len];
        var read = 0;
        while (read < len)
        {
            var n = fs.Read(buf.AsSpan(read));
            if (n <= 0)
            {
                break;
            }

            read += n;
        }

        return buf;
    }

    private static string FormatCodeName(int code) => code switch
    {
        0 => "DOS 3.3 sector order",
        1 => "ProDOS sector order",
        2 => "nibble",
        _ => "unknown",
    };
}