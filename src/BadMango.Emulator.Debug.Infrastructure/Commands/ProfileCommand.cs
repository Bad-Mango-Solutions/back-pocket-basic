// <copyright file="ProfileCommand.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Commands;

using BadMango.Emulator.Bus.Interfaces;
using BadMango.Emulator.Core.Configuration;
using BadMango.Emulator.Core.Interfaces;

/// <summary>
/// Shows and manages machine profiles.
/// </summary>
/// <remarks>
/// <para>
/// Displays configuration information about the current machine, including
/// CPU type, memory size, and any attached peripherals. Also supports listing,
/// loading, saving, and setting default profiles.
/// </para>
/// <para>
/// This command requires machine info to be attached to the debug context.
/// </para>
/// </remarks>
public sealed class ProfileCommand : CommandHandlerBase, ICommandHelp
{
    /// <summary>
    /// The name of the file storing the default profile setting.
    /// </summary>
    private const string DefaultProfileFileName = ".default-profile";

    /// <summary>
    /// The default clock speed for profiles in Hz (1 MHz).
    /// </summary>
    private const int DefaultClockSpeedHz = 1_000_000;

    private readonly IMachineProfileLoader profileLoader;
    private readonly MachineProfileSerializer serializer;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProfileCommand"/> class.
    /// </summary>
    public ProfileCommand()
        : this(null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProfileCommand"/> class
    /// with a profile loader.
    /// </summary>
    /// <param name="profileLoader">The machine profile loader, or null to use the default.</param>
    public ProfileCommand(IMachineProfileLoader? profileLoader)
        : base("profile", "Show and manage machine profiles")
    {
        this.profileLoader = profileLoader ?? new MachineProfileLoader();
        this.serializer = new();
    }

    /// <inheritdoc/>
    public override IReadOnlyList<string> Aliases { get; } = ["machine", "info"];

    /// <inheritdoc/>
    public override string Usage => "profile [list|load <name>|save <name>|default [name]|initroms <name>]";

    /// <inheritdoc/>
    public string Synopsis => "profile [list|load <name>|save <name>|default [name]|initroms <name>]";

    /// <inheritdoc/>
    public string DetailedDescription =>
        "Displays configuration information about the current machine, or manages profiles. " +
        "Use 'profile list' to see available profiles, 'profile load <name>' to switch profiles, " +
        "'profile save <name>' to save the current configuration, 'profile default [name]' " +
        "to view or set the default profile, and 'profile initroms <name>' to create missing ROM files.";

    /// <inheritdoc/>
    public IReadOnlyList<CommandOption> Options { get; } =
    [
        new("list", null, "subcommand", "List all available loadable profiles", null),
        new("load", null, "subcommand", "Load a profile by name and rebuild the machine", "<name>"),
        new("save", null, "subcommand", "Save the current profile to the profiles directory", "<name>"),
        new("default", null, "subcommand", "View or set the default profile for future sessions", "[name]"),
        new("initroms", null, "subcommand", "Create blank ROM files required by a profile", "<name>"),
    ];

    /// <inheritdoc/>
    public IReadOnlyList<string> Examples { get; } =
    [
        "profile                  Display current machine profile information",
        "profile list             List all available profiles",
        "profile load pocket2e    Load and switch to the pocket2e profile",
        "profile save myconfig    Save current profile as 'myconfig'",
        "profile default          Show the current default profile",
        "profile default pocket2e Set pocket2e as the default profile",
        "profile initroms simple-65c02-with-rom  Create missing ROM files for a profile",
    ];

    /// <inheritdoc/>
    public string? SideEffects =>
        "The 'load' subcommand halts the CPU and rebuilds the entire machine, clearing all " +
        "memory and resetting state. The 'save' subcommand writes a file to the profiles " +
        "directory. The 'default' subcommand writes to a configuration file. The 'initroms' " +
        "subcommand creates blank ROM files in the library directory.";

    /// <inheritdoc/>
    public IReadOnlyList<string> SeeAlso { get; } = ["devicemap", "regions", "pages", "reset"];

    /// <inheritdoc/>
    public override CommandResult Execute(ICommandContext context, string[] args)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context is not IDebugContext debugContext)
        {
            return CommandResult.Error("Debug context required for this command.");
        }

        // Handle subcommands
        if (args.Length > 0)
        {
            return args[0].ToUpperInvariant() switch
            {
                "LIST" => ExecuteList(debugContext),
                "LOAD" => ExecuteLoad(debugContext, args),
                "SAVE" => ExecuteSave(debugContext, args),
                "DEFAULT" => ExecuteDefault(debugContext, args),
                "INITROMS" => ExecuteInitRoms(debugContext, args),
                _ => ShowProfileInfo(debugContext),
            };
        }

        return ShowProfileInfo(debugContext);
    }

    /// <summary>
    /// Gets the library roms directory (library root + roms).
    /// </summary>
    /// <returns>The library roms directory path.</returns>
    private static string GetLibraryRomsDirectory()
    {
        return Path.Combine(GetLibraryRoot(), "roms");
    }

    private static string FormatMemorySize(int bytes)
    {
        if (bytes >= 1024 * 1024)
        {
            return $"{bytes / (1024 * 1024)} MB ({bytes:N0} bytes)";
        }

        if (bytes >= 1024)
        {
            return $"{bytes / 1024} KB ({bytes:N0} bytes)";
        }

        return $"{bytes} bytes";
    }

    private static string GetProfilesDirectory()
    {
        string baseDir = AppContext.BaseDirectory;
        return Path.Combine(baseDir, "profiles");
    }

    /// <summary>
    /// Gets the library root directory (user's home directory + .backpocket).
    /// </summary>
    /// <returns>The library root path.</returns>
    private static string GetLibraryRoot()
    {
        string homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(homeDir, ".backpocket");
    }

    /// <summary>
    /// Gets the library profiles directory (library root + Profiles).
    /// </summary>
    /// <returns>The library profiles directory path.</returns>
    private static string GetLibraryProfilesDirectory()
    {
        return Path.Combine(GetLibraryRoot(), "Profiles");
    }

    /// <summary>
    /// Finds a profile file by name, searching multiple locations.
    /// </summary>
    /// <param name="profileName">The profile name to search for.</param>
    /// <returns>The full path to the profile file, or null if not found.</returns>
    private static string? FindProfileFile(string profileName)
    {
        // Validate the name doesn't contain path separators (security check)
        if (profileName.AsSpan().ContainsAny(Path.GetInvalidFileNameChars()) ||
            profileName.Contains(".."))
        {
            return null;
        }

        // Search locations in priority order:
        // 1. App's default profiles directory (AppContext.BaseDirectory/profiles)
        // 2. Library profiles directory (~/.backpocket/Profiles)
        string fileName = $"{profileName}.json";

        // Check app profiles directory first
        string appProfilePath = Path.Combine(GetProfilesDirectory(), fileName);
        if (File.Exists(appProfilePath))
        {
            return appProfilePath;
        }

        // Check library profiles directory
        string libraryProfilesDir = GetLibraryProfilesDirectory();
        if (!Directory.Exists(libraryProfilesDir))
        {
            // Create the directory for future use
            try
            {
                Directory.CreateDirectory(libraryProfilesDir);
            }
            catch (UnauthorizedAccessException)
            {
                // User doesn't have permission to create the directory - ignore
            }
            catch (IOException)
            {
                // I/O error creating directory - ignore
            }
        }

        string libraryProfilePath = Path.Combine(libraryProfilesDir, fileName);
        if (File.Exists(libraryProfilePath))
        {
            return libraryProfilePath;
        }

        return null;
    }

    private static string GetDefaultProfileFilePath()
    {
        return Path.Combine(GetProfilesDirectory(), DefaultProfileFileName);
    }

    private static string GetCurrentDefaultProfile()
    {
        string defaultFilePath = GetDefaultProfileFilePath();

        if (File.Exists(defaultFilePath))
        {
            try
            {
                string profileName = File.ReadAllText(defaultFilePath).Trim();
                if (!string.IsNullOrEmpty(profileName))
                {
                    return profileName;
                }
            }
            catch
            {
                // Fall through to default
            }
        }

        return MachineProfileLoader.DefaultProfileName;
    }

    private CommandResult ShowProfileInfo(IDebugContext debugContext)
    {
        debugContext.Output.WriteLine("Machine Profile:");
        debugContext.Output.WriteLine();

        // Display MachineInfo if available
        if (debugContext.MachineInfo is not null)
        {
            var info = debugContext.MachineInfo;
            debugContext.Output.WriteLine($"  Name:         {info.DisplayName}");
            debugContext.Output.WriteLine($"  ID:           {info.Name}");
            debugContext.Output.WriteLine($"  CPU:          {info.CpuType}");
            debugContext.Output.WriteLine($"  Memory:       {FormatMemorySize((int)info.MemorySize)}");
        }
        else
        {
            debugContext.Output.WriteLine("  No machine profile available.");
        }

        debugContext.Output.WriteLine();

        // Display CPU info if available
        if (debugContext.Cpu is not null)
        {
            var cpu = debugContext.Cpu;
            debugContext.Output.WriteLine("CPU Status:");
            debugContext.Output.WriteLine($"  PC:           ${cpu.GetPC():X4}");
            debugContext.Output.WriteLine($"  Halted:       {cpu.Halted}");

            if (cpu.Halted)
            {
                debugContext.Output.WriteLine($"  Halt Reason:  {cpu.HaltReason}");
            }
        }

        // Display bus info if available
        if (debugContext.Bus is not null)
        {
            var bus = debugContext.Bus;
            var pageSize = 1 << bus.PageShift;
            var totalMemory = bus.PageCount * pageSize;

            debugContext.Output.WriteLine();
            debugContext.Output.WriteLine("Memory Bus:");
            debugContext.Output.WriteLine($"  Page Count:   {bus.PageCount}");
            debugContext.Output.WriteLine($"  Page Size:    {FormatMemorySize(pageSize)}");
            debugContext.Output.WriteLine($"  Address Space: {FormatMemorySize(totalMemory)}");
        }

        // Show default profile info
        string defaultProfile = GetCurrentDefaultProfile();
        debugContext.Output.WriteLine();
        debugContext.Output.WriteLine($"Default Profile: {defaultProfile}");

        return CommandResult.Ok();
    }

    private CommandResult ExecuteList(IDebugContext debugContext)
    {
        // Combine profiles from multiple locations
        var allProfiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Add profiles from the app's default directory
        foreach (var profile in this.profileLoader.AvailableProfiles)
        {
            allProfiles.Add(profile);
        }

        // Add profiles from the library directory (~/.backpocket/Profiles)
        string libraryProfilesDir = GetLibraryProfilesDirectory();
        if (Directory.Exists(libraryProfilesDir))
        {
            try
            {
                var libraryProfiles = Directory.EnumerateFiles(libraryProfilesDir, "*.json")
                    .Select(Path.GetFileNameWithoutExtension)
                    .Where(name => !string.IsNullOrEmpty(name))
                    .Cast<string>();

                foreach (var profile in libraryProfiles)
                {
                    allProfiles.Add(profile);
                }
            }
            catch (UnauthorizedAccessException)
            {
                // User doesn't have permission to read the directory - ignore
            }
            catch (IOException)
            {
                // I/O error reading directory - ignore
            }
        }

        var profiles = allProfiles.OrderBy(p => p, StringComparer.OrdinalIgnoreCase).ToList();

        if (profiles.Count == 0)
        {
            debugContext.Output.WriteLine("No profiles available.");
            return CommandResult.Ok();
        }

        string currentProfile = debugContext.MachineInfo?.Name ?? string.Empty;
        string defaultProfile = GetCurrentDefaultProfile();

        debugContext.Output.WriteLine("Available Profiles:");
        debugContext.Output.WriteLine();

        foreach (var profileName in profiles)
        {
            var indicators = new List<string>();

            if (string.Equals(profileName, currentProfile, StringComparison.OrdinalIgnoreCase))
            {
                indicators.Add("active");
            }

            if (string.Equals(profileName, defaultProfile, StringComparison.OrdinalIgnoreCase))
            {
                indicators.Add("default");
            }

            string suffix = indicators.Count > 0 ? $" ({string.Join(", ", indicators)})" : string.Empty;
            debugContext.Output.WriteLine($"  {profileName}{suffix}");
        }

        debugContext.Output.WriteLine();
        debugContext.Output.WriteLine($"Total: {profiles.Count} profile(s)");

        return CommandResult.Ok();
    }

    private CommandResult ExecuteLoad(IDebugContext debugContext, string[] args)
    {
        if (args.Length < 2)
        {
            return CommandResult.Error("Usage: profile load <name>");
        }

        string profileName = args[1];

        // Try to find the profile file path from multiple locations
        string? profileFilePath = FindProfileFile(profileName);
        if (profileFilePath is null)
        {
            return CommandResult.Error($"Profile '{profileName}' not found.");
        }

        // Load the profile from the found file
        MachineProfile profile;
        try
        {
            profile = this.profileLoader.LoadProfileFromFile(profileFilePath);
        }
        catch (Exception ex)
        {
            return CommandResult.Error($"Failed to load profile '{profileName}': {ex.Message}");
        }

        // Rebuild the machine with the new profile
        if (debugContext is not DebugContext mutableContext)
        {
            return CommandResult.Error("Cannot reload profile: context does not support machine rebuilding.");
        }

        try
        {
            // Create a path resolver with the library root and profile file path
            string libraryRoot = GetLibraryRoot();
            var pathResolver = new ProfilePathResolver(libraryRoot, profileFilePath);

            // Create new machine with all debug components from profile
            (IMachine machine, IDisassembler disassembler, MachineInfo info) =
                MachineFactory.CreateDebugSystem(profile, pathResolver);

            // Detach old system and attach new one
            mutableContext.DetachSystem();

            // Recreate tracing listener for the new CPU
            var tracingListener = new TracingDebugListener();
            machine.Cpu.AttachDebugger(tracingListener);

            // Attach the full machine with all debug components
            mutableContext.AttachMachine(machine, disassembler, info, tracingListener);

            debugContext.Output.WriteLine($"Loaded profile: {profile.DisplayName ?? profile.Name}");
            debugContext.Output.WriteLine($"CPU reset to ${machine.Cpu.GetPC():X4}");
        }
        catch (Exception ex)
        {
            return CommandResult.Error($"Failed to load profile: {ex.Message}");
        }

        return CommandResult.Ok();
    }

    private CommandResult ExecuteSave(IDebugContext debugContext, string[] args)
    {
        if (args.Length < 2)
        {
            return CommandResult.Error("Usage: profile save <name>");
        }

        string profileName = args[1];

        // Validate name
        if (profileName.AsSpan().ContainsAny(Path.GetInvalidFileNameChars()) || profileName.Contains(".."))
        {
            return CommandResult.Error("Invalid profile name.");
        }

        var machineInfo = debugContext.MachineInfo;
        if (machineInfo is null)
        {
            return CommandResult.Error("No machine profile is currently loaded.");
        }

        // Create a profile from current machine info
        var profile = new MachineProfile
        {
            Name = profileName,
            DisplayName = $"{profileName} (saved profile)",
            Description = $"Profile saved from debug console on {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
            Cpu = new()
            {
                Type = machineInfo.CpuType,
                ClockSpeed = DefaultClockSpeedHz,
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
                        Size = $"0x{machineInfo.MemorySize:X}",
                        Permissions = "rwx",
                    },
                ],
            },
        };

        try
        {
            string profilesDir = GetProfilesDirectory();
            if (!Directory.Exists(profilesDir))
            {
                Directory.CreateDirectory(profilesDir);
            }

            string filePath = Path.Combine(profilesDir, $"{profileName}.json");
            this.serializer.SerializeToFile(profile, filePath);

            debugContext.Output.WriteLine($"Profile saved: {filePath}");
        }
        catch (Exception ex)
        {
            return CommandResult.Error($"Failed to save profile: {ex.Message}");
        }

        return CommandResult.Ok();
    }

    private CommandResult ExecuteDefault(IDebugContext debugContext, string[] args)
    {
        // If no argument, show current default
        if (args.Length < 2)
        {
            string currentDefault = GetCurrentDefaultProfile();
            debugContext.Output.WriteLine($"Current default profile: {currentDefault}");
            return CommandResult.Ok();
        }

        string profileName = args[1];

        // Verify profile exists using the multi-location search
        string? profileFilePath = FindProfileFile(profileName);
        if (profileFilePath is null)
        {
            return CommandResult.Error($"Profile '{profileName}' not found. Use 'profile list' to see available profiles.");
        }

        try
        {
            string profilesDir = GetProfilesDirectory();
            if (!Directory.Exists(profilesDir))
            {
                Directory.CreateDirectory(profilesDir);
            }

            string defaultFilePath = GetDefaultProfileFilePath();
            File.WriteAllText(defaultFilePath, profileName);

            debugContext.Output.WriteLine($"Default profile set to: {profileName}");
            debugContext.Output.WriteLine("This will take effect on the next session start.");
        }
        catch (Exception ex)
        {
            return CommandResult.Error($"Failed to set default profile: {ex.Message}");
        }

        return CommandResult.Ok();
    }

    private CommandResult ExecuteInitRoms(IDebugContext debugContext, string[] args)
    {
        if (args.Length < 2)
        {
            return CommandResult.Error("Usage: profile initroms <name>");
        }

        string profileName = args[1];

        // Try to find the profile file path from multiple locations
        string? profileFilePath = FindProfileFile(profileName);
        if (profileFilePath is null)
        {
            return CommandResult.Error($"Profile '{profileName}' not found.");
        }

        // Load the profile from the found file
        MachineProfile profile;
        try
        {
            profile = this.profileLoader.LoadProfileFromFile(profileFilePath);
        }
        catch (Exception ex)
        {
            return CommandResult.Error($"Failed to load profile '{profileName}': {ex.Message}");
        }

        // Create a path resolver with the library root and profile file path
        string libraryRoot = GetLibraryRoot();
        var pathResolver = new ProfilePathResolver(libraryRoot, profileFilePath);

        // Find all ROM images defined in the profile's rom-images array
        var romImages = new List<(string Name, string Source, uint Size)>();
        if (profile.Memory?.RomImages != null)
        {
            foreach (var romImage in profile.Memory.RomImages)
            {
                if (!HexParser.TryParseUInt32(romImage.Size, out uint size))
                {
                    debugContext.Output.WriteLine($"Warning: Invalid size '{romImage.Size}' for ROM image '{romImage.Name}', skipping.");
                    continue;
                }

                romImages.Add((romImage.Name, romImage.Source, size));
            }
        }

        if (romImages.Count == 0)
        {
            debugContext.Output.WriteLine("No ROM images are defined in this profile's rom-images array.");
            return CommandResult.Ok();
        }

        int createdCount = 0;
        int skippedCount = 0;
        int errorCount = 0;

        foreach (var (name, source, size) in romImages)
        {
            try
            {
                // Try to resolve the path
                string resolvedPath = pathResolver.Resolve(source);

                if (File.Exists(resolvedPath))
                {
                    debugContext.Output.WriteLine($"  {name}: {source} (exists, skipped)");
                    skippedCount++;
                    continue;
                }

                // Ensure the directory exists
                string? directory = Path.GetDirectoryName(resolvedPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Create a blank ROM file of the correct size using FileStream.SetLength
                // This avoids allocating large byte arrays in memory
                using (var fileStream = File.Create(resolvedPath))
                {
                    fileStream.SetLength(size);
                }

                debugContext.Output.WriteLine($"  {name}: Created {source} ({FormatMemorySize((int)size)})");
                createdCount++;
            }
            catch (InvalidOperationException ex)
            {
                // Path resolver error (e.g., library root not configured - shouldn't happen)
                debugContext.Output.WriteLine($"  {name}: Error resolving {source} - {ex.Message}");
                errorCount++;
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                debugContext.Output.WriteLine($"  {name}: Failed to create {source} - {ex.Message}");
                errorCount++;
            }
        }

        debugContext.Output.WriteLine();
        debugContext.Output.WriteLine($"Summary: {createdCount} created, {skippedCount} skipped, {errorCount} errors");

        if (createdCount > 0)
        {
            debugContext.Output.WriteLine();
            debugContext.Output.WriteLine("Blank ROM files have been created. You can now use 'profile load' to load the profile.");
            debugContext.Output.WriteLine($"ROM directory: {GetLibraryRomsDirectory()}");
        }

        return errorCount > 0 ? CommandResult.Error("Some ROM files could not be created.") : CommandResult.Ok();
    }
}