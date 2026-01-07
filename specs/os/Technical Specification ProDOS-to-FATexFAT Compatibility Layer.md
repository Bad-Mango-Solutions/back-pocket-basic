# Technical Specification: ProDOS-to-FAT/exFAT Compatibility Layer for .NET 10–Based Apple II Emulator

---

## Introduction

The Apple II family, particularly under ProDOS 8, remains a vibrant platform for retrocomputing, educational, and archival projects. Modern emulators, such as those built in .NET and C#, have enabled Apple II software to run on contemporary hardware, but disk image formats and file system compatibility remain persistent challenges. ProDOS 8 expects block devices formatted in its native structure, while host systems overwhelmingly use FAT16, FAT32, or exFAT on removable media and hard disks. Bridging these worlds requires a robust, transparent compatibility layer that allows ProDOS 8 to access host-side FAT/exFAT volumes as if they were native block devices, with seamless translation of directory structures, file metadata, and block allocation.

This specification details the architecture, interfaces, and implementation strategies for a ProDOS-to-FAT/exFAT compatibility layer targeting a .NET 10–based Apple II emulator, using modern C#14 features. It addresses the translation of FAT/exFAT volumes into ProDOS-compatible block devices, the mapping of directory and file metadata, block allocation, and strategies for handling ProDOS limitations. The spec also explores optional support for multiple ProDOS volumes per FAT/exFAT volume and sketches a GS/OS-style File System Translator (FST) module for future IIgs clones.

---

## Architectural Overview

### Goals

- **Transparent access:** ProDOS 8 software running in the emulator should access FAT16, FAT32, or exFAT volumes as if they were native ProDOS block devices.
- **Block device abstraction:** The compatibility layer must expose host volumes as block devices, supporting ProDOS's block-based I/O model.
- **Metadata translation:** FAT/exFAT directory entries and file metadata must be synthesized into ProDOS-compatible structures, including filename, timestamps, and file types.
- **Block allocation and translation:** FAT clusters and exFAT allocation units must be mapped to ProDOS logical blocks, with support for sparse files.
- **Handling limitations:** The layer must gracefully handle ProDOS constraints (16 MB volume size, 127 files per directory, 15-character filenames).
- **Performance and integrity:** Caching, concurrency, and journaling strategies must ensure robust, performant operation.
- **Extensibility:** The design should support future enhancements, including GS/OS FST integration and multiple ProDOS volumes per host partition.

### High-Level Architecture Diagram



At the heart of the system is the **ProDOS Block Device Adapter**, which exposes a block device interface to the Apple II emulator core. This adapter delegates block read/write requests to a **FAT/exFAT Volume Reader**, which parses the host filesystem and translates requests into FAT/exFAT operations. The **ProDOS Directory Synthesizer** and **File Metadata Mapper** handle directory and file entry translation, while the **Block Allocator/Translator** manages mapping between FAT clusters and ProDOS blocks. A **Mount Manager** orchestrates volume lifecycle, caching, and concurrency.

---

## Block Device Abstraction

### Design Rationale

ProDOS interacts with storage via block devices, issuing read and write requests for 512-byte blocks identified by logical block numbers. The compatibility layer must present a block device abstraction that the emulator can plug into its virtual hardware, while internally translating these requests to FAT/exFAT operations.

### C#14 Interface Definition

```csharp
public interface IBlockDevice
{
    int BlockSize { get; } // Typically 512 bytes for ProDOS
    long BlockCount { get; } // Number of blocks exposed to ProDOS

    ValueTask<int> ReadBlockAsync(long blockNumber, Memory<byte> buffer, CancellationToken cancellationToken = default);
    ValueTask<int> WriteBlockAsync(long blockNumber, ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default);

    bool IsReadOnly { get; }
    string DeviceName { get; }
}
```

#### Explanation

- **BlockSize** and **BlockCount** define the geometry exposed to ProDOS.
- **ReadBlockAsync** and **WriteBlockAsync** are asynchronous for performance, supporting cancellation.
- **IsReadOnly** allows the emulator to expose read-only volumes (e.g., write-protected SD cards).
- **DeviceName** provides a human-readable identifier for debugging and UI.

### Integration with Emulator

The emulator's disk controller emulation (e.g., Disk II, SmartPort) will instantiate and register one or more `IBlockDevice` instances, mapping them to ProDOS unit numbers and slots as required.

---

## FAT/exFAT Volume Reader

### Responsibilities

- **Volume mounting:** Open and parse FAT16, FAT32, or exFAT volumes, validate integrity, and expose them as block devices.
- **Cluster/block mapping:** Translate between FAT/exFAT clusters and ProDOS logical blocks.
- **Directory traversal:** Enumerate directories, files, and metadata for translation.
- **File I/O:** Read and write file contents, handling sparse files and allocation.

### C#14 Record Structs and Interfaces

```csharp
public interface IFatVolumeReader
{
    FatType FatType { get; }
    string VolumeLabel { get; }
    long ClusterSize { get; }
    long ClusterCount { get; }
    long TotalSize { get; }

    IEnumerable<FatDirectoryEntry> EnumerateDirectory(string path);
    ValueTask<Stream> OpenFileAsync(string path, FileAccess access, CancellationToken cancellationToken = default);

    ValueTask<int> ReadClusterAsync(long clusterNumber, Memory<byte> buffer, CancellationToken cancellationToken = default);
    ValueTask<int> WriteClusterAsync(long clusterNumber, ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default);
}
```

```csharp
public enum FatType
{
    Fat12,
    Fat16,
    Fat32,
    ExFat
}

public record struct FatDirectoryEntry(
    string Name,
    FatAttributes Attributes,
    DateTime CreationTime,
    DateTime ModificationTime,
    DateTime AccessTime,
    long FirstCluster,
    long FileSize,
    bool IsDirectory
);

[Flags]
public enum FatAttributes
{
    ReadOnly = 0x01,
    Hidden = 0x02,
    System = 0x04,
    VolumeLabel = 0x08,
    Directory = 0x10,
    Archive = 0x20
}
```

#### Explanation

- **FatType** distinguishes between FAT variants and exFAT.
- **EnumerateDirectory** yields directory entries for translation.
- **OpenFileAsync** provides file streams for content access.
- **ReadClusterAsync/WriteClusterAsync** allow direct cluster-level access for block mapping.

### Implementation Notes

- Use existing .NET libraries such as **DiscUtils** for FAT/exFAT parsing and low-level access.
- For exFAT, handle allocation bitmap and directory entry parsing, including time zone fields and centisecond offsets.
- Ensure robust error handling for corrupted volumes, leveraging recovery strategies as needed.

---

## ProDOS Directory and File Metadata Synthesizer

### Responsibilities

- **Directory translation:** Map FAT/exFAT directory entries to ProDOS directory structures, including file entries and headers.
- **Metadata synthesis:** Generate ProDOS-compatible metadata (filename, file type, timestamps, access flags, aux type).
- **Filename mapping:** Enforce ProDOS filename constraints (15 characters, uppercase, valid characters).
- **File type inference:** Map FAT/exFAT attributes and extensions to ProDOS file types.

### C#14 Record Structs

```csharp
public record struct ProdosDirectoryEntry(
    string Name, // Up to 15 chars, uppercase
    ProdosFileType FileType,
    ProdosStorageType StorageType,
    int KeyPointer, // Block number
    int BlocksUsed,
    int Eof, // End of file (bytes)
    DateTime CreationTime,
    DateTime ModificationTime,
    ProdosAccessFlags AccessFlags,
    int AuxType,
    int HeaderPointer // Parent directory block
);

public enum ProdosFileType : byte
{
    Typeless = 0x00,
    Text = 0x04,
    Binary = 0x06,
    Directory = 0x0F,
    // ... (see ProDOS file type table)
}

public enum ProdosStorageType : byte
{
    Seedling = 0x01,
    Sapling = 0x02,
    Tree = 0x03,
    Subdirectory = 0x0D,
    // ... (see ProDOS storage types)
}

[Flags]
public enum ProdosAccessFlags : byte
{
    DestroyEnabled = 0x80,
    RenameEnabled = 0x40,
    BackupNeeded = 0x20,
    WriteEnabled = 0x02,
    ReadEnabled = 0x01
}
```

#### Explanation

- **Name:** Must be mapped to ProDOS constraints; truncate or sanitize as needed.
- **FileType:** Inferred from FAT/exFAT attributes and extensions.
- **StorageType:** Determined by file size and mapping strategy.
- **KeyPointer, BlocksUsed, Eof:** Calculated during block allocation.
- **Timestamps:** Synthesized from FAT/exFAT timestamps, with conversion to ProDOS format.
- **AccessFlags:** Mapped from FAT/exFAT attributes (e.g., read-only, hidden).

### Directory Structure Synthesis

ProDOS directories are organized as linked lists of blocks, each containing a header and file entries. The synthesizer must:

- Allocate directory blocks as needed, up to 127 entries per directory (or more for ProDOS 2.5+).
- Populate header fields (name, entry length, entries per block, file count, bitmap pointer).
- Link entries to synthesized files and subdirectories.

### Filename Mapping Algorithm

- Truncate FAT/exFAT filenames to 15 characters.
- Convert to uppercase (unless ProDOS 2.5+ or GS/OS mixed-case support is enabled).
- Replace invalid characters with underscores or remove.
- Ensure uniqueness within directory; append numeric suffix if needed.

---

## Block Allocator and Translator

### Responsibilities

- **Mapping clusters to blocks:** Translate FAT/exFAT clusters (or allocation units) to ProDOS logical block numbers.
- **Sparse file handling:** Support ProDOS sparse files by mapping unallocated clusters to zero-filled blocks.
- **Allocation bitmap management:** Synthesize ProDOS volume bitmap from FAT/exFAT allocation tables.
- **Block allocation for writes:** Allocate new clusters/blocks as needed, updating FAT/exFAT structures.

### C#14 Record Structs and Interfaces

```csharp
public interface IBlockAllocator
{
    int AllocateBlock();
    void FreeBlock(int blockNumber);
    int GetPhysicalCluster(int blockNumber);
    bool IsBlockAllocated(int blockNumber);
}

public record struct BlockMapping(
    int ProdosBlockNumber,
    long FatClusterNumber,
    bool IsSparse
);
```

#### Explanation

- **AllocateBlock/FreeBlock:** Manage block allocation for new files and directories.
- **GetPhysicalCluster:** Map ProDOS block number to FAT/exFAT cluster.
- **IsBlockAllocated:** Used for bitmap synthesis and sparse file detection.
- **BlockMapping:** Encapsulates the translation between ProDOS blocks and FAT/exFAT clusters.

### Mapping Strategy

- For files, maintain a mapping table from ProDOS logical blocks to FAT/exFAT clusters.
- For directories, allocate contiguous blocks as needed.
- For sparse files, map unallocated clusters to zero-filled blocks, as per ProDOS semantics.

### Handling Block Allocation Edge Cases

- **FAT fragmentation:** Use best-fit or first-fit strategies to minimize fragmentation when allocating clusters.
- **exFAT allocation bitmap:** Parse and update bitmap for allocation/deallocation.
- **Crash recovery:** Implement journaling or transactional updates to prevent corruption on power loss.

---

## Translating FAT Directory Entries and File Contents

### Directory Entry Translation

#### FAT/FAT32

- **8.3 filenames:** Map directly to ProDOS names, truncating as needed.
- **Long File Names (LFN):** Use LFN if present, otherwise fallback to 8.3.
- **Attributes:** Map read-only, hidden, system, and archive to ProDOS access flags.
- **Timestamps:** Convert FAT date/time fields to ProDOS format (year, month, day, hour, minute).

#### exFAT

- **Unicode filenames:** Truncate and sanitize to ProDOS constraints.
- **Time zone fields:** Convert exFAT time zone codes to UTC or local time for ProDOS timestamps.
- **Centisecond offsets:** Use for higher-precision timestamps if desired.
- **Attributes:** Map exFAT flags to ProDOS access flags.

### File Content Translation

- **Seedling files:** For files ≤512 bytes, map directly to a single ProDOS block.
- **Sapling files:** For files >512 bytes and ≤128 KB, create index blocks pointing to data blocks.
- **Tree files:** For files >128 KB and ≤16 MB, create master index blocks pointing to index blocks, each pointing to data blocks.
- **Sparse files:** For files with gaps, map missing clusters to zero-filled blocks, as per ProDOS sparse file semantics.

### Pseudocode Example: Directory Entry Translation

```csharp
ProdosDirectoryEntry TranslateFatEntry(FatDirectoryEntry fatEntry)
{
    var name = MapFatFilenameToProdos(fatEntry.Name);
    var fileType = InferProdosFileType(fatEntry.Name, fatEntry.Attributes);
    var storageType = DetermineStorageType(fatEntry.FileSize);
    var keyPointer = AllocateKeyBlock(fatEntry);
    var blocksUsed = CalculateBlocksUsed(fatEntry.FileSize);
    var eof = (int)fatEntry.FileSize;
    var creationTime = ConvertFatTimestamp(fatEntry.CreationTime);
    var modificationTime = ConvertFatTimestamp(fatEntry.ModificationTime);
    var accessFlags = MapFatAttributesToProdos(fatEntry.Attributes);
    var auxType = InferAuxType(fatEntry.Name);
    var headerPointer = GetParentDirectoryBlock();

    return new ProdosDirectoryEntry(
        name, fileType, storageType, keyPointer, blocksUsed, eof,
        creationTime, modificationTime, accessFlags, auxType, headerPointer
    );
}
```

---

## Handling ProDOS Limitations

### Volume Size (16 MB)

- **ProDOS 8:** Maximum volume size is 16 MB (0xFFFFFF bytes, 32,768 blocks).
- **FAT/exFAT volumes larger than 16 MB:** Present only the first 16 MB as a ProDOS volume, or partition the host volume into multiple ProDOS volumes.
- **ProDOS 2.5+:** Support for up to 32 MB volumes and more than 127 files per directory; compatibility layer should detect ProDOS version and adjust limits accordingly.

### Directory Entry Limits

- **127 files per directory:** Enforce limit when synthesizing directories; optionally support more for ProDOS 2.5+.
- **Root directory exceptions:** ProDOS 2.5+ removes the 51-file root directory limit; compatibility layer should support this if possible.

### Filename Constraints

- **15-character limit:** Truncate FAT/exFAT filenames to 15 characters.
- **Valid characters:** Allow only uppercase letters, digits, and periods; sanitize others.
- **Uniqueness:** Ensure unique filenames within directory; append suffixes if needed.

### Table: ProDOS vs FAT/exFAT Constraints

| Constraint                | ProDOS 8           | FAT16/FAT32         | exFAT              | Compatibility Layer Strategy                |
|---------------------------|--------------------|---------------------|--------------------|---------------------------------------------|
| Max volume size           | 16 MB              | 2 GB/32 GB/2 TB     | 128 PB             | Partition host volume, expose ≤16 MB        |
| Max files per directory   | 127 (root: 51)     | 512 (FAT16 root)    | Unlimited          | Truncate/split directories as needed        |
| Filename length           | 15 chars           | 8.3/255 (LFN)       | 255 Unicode        | Truncate/sanitize to 15 chars, uppercase    |
| File size                 | 16 MB              | 2 GB/4 GB           | 128 PB             | Truncate or split large files               |
| Directory nesting         | Unlimited          | Unlimited           | Unlimited          | Map directly, enforce ProDOS limits         |

#### Analysis

The compatibility layer must enforce ProDOS constraints when exposing host volumes, gracefully handling overflows by truncation, partitioning, or error reporting. For advanced users, configuration options may allow mapping multiple ProDOS volumes from a single FAT/exFAT partition.

---

## Support for Multiple ProDOS Volumes per FAT/exFAT Volume

### Motivation

Many FAT/exFAT volumes exceed ProDOS's 16 MB limit. To maximize usability, the compatibility layer should optionally expose multiple ProDOS volumes, each mapped to a segment of the host volume.

### Implementation Strategy

- **Partitioning:** Divide the FAT/exFAT volume into N segments, each ≤16 MB, and expose each as a separate ProDOS block device.
- **Volume naming:** Assign unique volume names (e.g., /HD1, /HD2) for each segment.
- **Slot/drive mapping:** Map each ProDOS volume to a unique slot/drive combination in the emulator, following SmartPort conventions.
- **Directory mapping:** For directories exceeding entry limits, split across volumes or truncate.

### C#14 Example: Volume Partitioning

```csharp
public IEnumerable<IBlockDevice> PartitionFatVolume(IFatVolumeReader fatReader)
{
    const long prodosMaxSize = 16 * 1024 * 1024;
    long totalSize = fatReader.TotalSize;
    int volumeCount = (int)Math.Ceiling((double)totalSize / prodosMaxSize);

    for (int i = 0; i < volumeCount; i++)
    {
        long offset = i * prodosMaxSize;
        long size = Math.Min(prodosMaxSize, totalSize - offset);
        yield return new FatProdosBlockDevice(fatReader, offset, size, $"HD{i + 1}");
    }
}
```

#### Edge Cases

- **File spanning volumes:** Disallow or split files that cross volume boundaries.
- **Directory spanning volumes:** Truncate or split directories as needed.
- **Mounting logic:** Ensure consistent mapping across emulator restarts.

---

## Mounting Logic and Lifecycle Management

### Mount Manager Responsibilities

- **Volume detection:** Scan host system for FAT/exFAT volumes, validate, and mount as block devices.
- **Lifecycle events:** Handle volume insertion/removal, read-only status, and error conditions.
- **Concurrency:** Support concurrent access from multiple emulator threads, synchronize I/O.
- **Caching:** Implement block and directory entry caching for performance.
- **Crash recovery:** Use journaling or transactional updates to prevent corruption.

### C#14 Mount Manager Skeleton

```csharp
public class MountManager
{
    private readonly List<IBlockDevice> _mountedDevices = new();

    public void ScanAndMountVolumes()
    {
        foreach (var volume in DiscUtils.VolumeManager.GetFatExFatVolumes())
        {
            var fatReader = new FatVolumeReader(volume);
            var blockDevices = PartitionFatVolume(fatReader);
            _mountedDevices.AddRange(blockDevices);
        }
    }

    public void UnmountVolume(string deviceName)
    {
        var device = _mountedDevices.FirstOrDefault(d => d.DeviceName == deviceName);
        if (device != null)
        {
            // Flush caches, close handles, remove from list
            _mountedDevices.Remove(device);
        }
    }

    public IEnumerable<IBlockDevice> GetMountedDevices() => _mountedDevices;
}
```

#### Integration

The emulator queries `MountManager.GetMountedDevices()` to enumerate available block devices, mapping them to ProDOS slots/units as needed.

---

## Concurrency, Caching, and Performance Strategies

### Concurrency

- Use asynchronous I/O (`ValueTask`, `async/await`) for block and cluster operations.
- Employ locks or concurrent collections to synchronize access to shared resources (e.g., allocation bitmap, directory cache).
- Support multiple open files and directories, up to ProDOS's limit of eight open files.

### Caching

- Implement block-level caching for frequently accessed blocks.
- Cache directory entries and file metadata to minimize repeated parsing.
- Use LRU or adaptive cache eviction strategies.

### Performance Optimization

- Batch block reads/writes when possible.
- Prefetch directory blocks during enumeration.
- Use memory-mapped files for host-side disk images if supported.

### .NET 10 Features

- Leverage new .NET 10 caching APIs (HybridCache) for distributed or local caching scenarios.
- Use modern concurrency primitives (e.g., `SemaphoreSlim`, `ConcurrentDictionary`) for thread safety.

---

## Data Integrity, Journaling, and Crash Recovery

### Strategies

- **Transactional writes:** Buffer block writes and commit atomically to prevent partial updates.
- **Journaling:** Maintain a write-ahead log of pending updates; replay on startup after crash.
- **Redundant FAT/exFAT tables:** Use secondary FAT tables for recovery if primary is corrupted.
- **Volume bitmap validation:** Periodically verify ProDOS bitmap against FAT/exFAT allocation tables.

### Error Handling

- Detect and report file system errors (e.g., invalid cluster chains, allocation bitmap mismatches).
- Provide recovery options (e.g., rollback, repair) for common corruption scenarios.

---

## Security and Permissions Mapping

### Mapping FAT/exFAT Attributes to ProDOS Access Flags

- **Read-only:** Map to ProDOS `ReadEnabled` and `WriteEnabled` flags.
- **Hidden/System:** Optionally map to ProDOS `Invisible` or restrict access.
- **Archive:** Map to ProDOS `BackupNeeded` flag.
- **Volume label:** Map to ProDOS volume name.

### File Mapping Security

- Enforce access controls at the block device and file level.
- For host-side security, respect OS-level file permissions when accessing FAT/exFAT volumes.
- Use .NET file mapping security APIs for advanced scenarios.

---

## Testing, Validation, and Interoperability

### Testing Strategies

- **Unit tests:** Validate block device, directory, and file translation logic.
- **Integration tests:** Run Apple II software (e.g., Total Replay, AppleWorks) against the compatibility layer.
- **Edge case tests:** Test long filenames, large directories, sparse files, and volume limits.
- **Interoperability:** Ensure compatibility with existing Apple II disk images and host-side FAT/exFAT volumes.

### Validation Tools

- Use ProDOS utilities (e.g., Filer, Bitsy Bye) to inspect synthesized volumes.
- Employ host-side tools (e.g., Windows File Recovery, Donemax Data Recovery) to verify FAT/exFAT integrity.
- Leverage emulator test suites (e.g., Apple2sharp, reload-emulator) for automated validation.

---

## Edge Cases: Filenames, Timestamps, Resource Forks, and Metadata

### Filenames

- Handle FAT/exFAT long filenames, Unicode, and invalid characters.
- For GS/OS or ProDOS 2.5+, support mixed-case and extended character sets.

### Timestamps

- Convert FAT/exFAT timestamps to ProDOS format (year, month, day, hour, minute).
- For exFAT, handle time zone codes and centisecond offsets for higher precision.
- For GS/OS, support extended date formats (seconds, milliseconds).

### Resource Forks and Extended Metadata

- For GS/OS, support forked files (data/resource forks) using extended storage types.
- Map host-side metadata (e.g., Finder info, NTFS alternate data streams) to ProDOS aux type or GS/OS option lists.

---

## Implementation Roadmap and Milestones

### Phase 1: Core Block Device and FAT/exFAT Reader

- Implement `IBlockDevice` and `IFatVolumeReader` interfaces.
- Integrate DiscUtils for FAT/exFAT parsing.
- Validate block read/write translation.

### Phase 2: Directory and Metadata Synthesis

- Implement ProDOS directory and file entry synthesizer.
- Map FAT/exFAT entries to ProDOS structures.
- Handle filename and metadata constraints.

### Phase 3: Block Allocation and Sparse File Support

- Implement block allocator and translator.
- Support seedling, sapling, and tree file structures.
- Handle sparse files and allocation bitmap synthesis.

### Phase 4: Multiple Volume and Advanced Features

- Support partitioning of host volumes into multiple ProDOS block devices.
- Implement mounting logic and lifecycle management.
- Add caching, concurrency, and journaling.

### Phase 5: GS/OS FST Module (Bonus)

- Design and implement GS/OS-style File System Translator (FST) interface.
- Support forked files, extended metadata, and advanced date formats.
- Validate with IIgs clone emulator and GS/OS utilities.

### Phase 6: Testing, Validation, and Release

- Develop comprehensive test suite.
- Validate interoperability with Apple II software and host-side tools.
- Document API and usage for emulator integration.

---

## GS/OS-Style File System Translator (FST) Module (Bonus)

### Motivation

GS/OS introduced File System Translators (FSTs) to support multiple filesystems (ProDOS, HFS, DOS 3.3, MS-DOS) on the IIgs. A modern FST for FAT/exFAT would allow IIgs clones to natively access host volumes, supporting advanced features like forked files, extended metadata, and mixed-case filenames.

### FST Interface Design

```csharp
public interface IFstModule
{
    string FileSystemName { get; }
    int FileSystemId { get; }
    IEnumerable<FstVolumeInfo> EnumerateVolumes();
    FstVolume MountVolume(string path);
    void UnmountVolume(FstVolume volume);

    FstFile OpenFile(FstVolume volume, string path, FileAccess access);
    void CloseFile(FstFile file);

    int ReadFile(FstFile file, long offset, Memory<byte> buffer);
    int WriteFile(FstFile file, long offset, ReadOnlyMemory<byte> buffer);

    FstDirectoryEntry[] EnumerateDirectory(FstVolume volume, string path);
    FstFileInfo GetFileInfo(FstFile file);
    void SetFileInfo(FstFile file, FstFileInfo info);
}
```

### Volume Mounting Logic

- Scan host system for FAT/exFAT volumes.
- Validate and mount as FST volumes, exposing volume label, size, and capabilities.
- Support dynamic mounting/unmounting in response to device events.

### File I/O Mapping

- Map GS/OS file operations (open, read, write, seek) to FAT/exFAT primitives.
- Support forked files (data/resource forks) using exFAT stream extensions or alternate data streams.
- Handle extended metadata (Finder info, timestamps, permissions).

### Integration with Emulator

- Register FST module with IIgs clone emulator.
- Expose mounted volumes as GS/OS devices, supporting standard file dialogs and utilities.
- Validate interoperability with GS/OS applications and utilities.

### Diagram: FST Integration



---

## Use of Existing .NET Libraries

### DiscUtils

- Provides robust FAT16, FAT32, and exFAT parsing and manipulation.
- Supports virtual disk formats (VHD, VMDK), enabling advanced emulation scenarios.
- Extensible for custom block device and file system implementations.

### Namotion.Storage

- Offers abstractions for blob storage and file systems, useful for emulator integration and testing.

### DiskAccessLibrary

- Enables low-level access to physical and virtual disks, useful for advanced scenarios and testing.

---

## Summary Table: Key Interfaces and Record Structs

| Component                        | C#14 Interface/Record Struct         | Purpose                                         |
|-----------------------------------|--------------------------------------|-------------------------------------------------|
| Block Device Abstraction          | `IBlockDevice`                       | Exposes ProDOS-compatible block device           |
| FAT/exFAT Volume Reader           | `IFatVolumeReader`, `FatDirectoryEntry` | Parses and accesses FAT/exFAT volumes           |
| ProDOS Directory Synthesizer      | `ProdosDirectoryEntry`               | Synthesizes ProDOS directory/file entries        |
| Block Allocator/Translator        | `IBlockAllocator`, `BlockMapping`    | Maps clusters to blocks, manages allocation      |
| Mount Manager                     | `MountManager`                       | Orchestrates volume lifecycle and mounting       |
| GS/OS FST Module                  | `IFstModule`, `FstVolumeInfo`, etc.  | Provides GS/OS file system translation           |

---

## Conclusion and Future Directions

This specification provides a comprehensive, implementation-ready blueprint for a ProDOS-to-FAT/exFAT compatibility layer in a .NET 10–based Apple II emulator. By leveraging modern C#14 features, robust .NET libraries, and careful mapping of file system semantics, the layer enables transparent, performant access to host-side volumes, unlocking new possibilities for retrocomputing, archival, and educational projects.

Future enhancements may include:

- Advanced caching and performance tuning using .NET 10 HybridCache.
- Full GS/OS FST integration for IIgs clones, supporting forked files and extended metadata.
- Support for additional host file systems (NTFS, HFS+, APFS) via extensible interfaces.
- Enhanced recovery and repair tools for FAT/exFAT volumes.
- Community-driven compatibility tracking and reporting via platforms like EmuReady.

By following this specification, emulator developers can deliver a seamless, future-proof experience for Apple II and IIgs users, bridging the gap between classic software and modern storage technologies.

---

**End of Specification**
