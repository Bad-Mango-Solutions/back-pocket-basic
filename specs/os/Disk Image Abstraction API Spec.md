# Design of a Reusable and Extensible C# Disk Image Abstraction API for Apple II Emulation and Tooling

------

## Introduction

The Apple II ecosystem, with its rich history and diverse disk image formats, presents unique challenges for emulator and tooling developers. Disk images—ranging from simple sector dumps to sophisticated bitstream captures—must be accessed, manipulated, and converted with precision and flexibility. As emulation and archival needs have grown, so too has the demand for robust, extensible APIs that can abstract away the complexities of disk image formats, storage backends, and metadata management. This technical design document proposes a comprehensive C# API for disk image abstraction, tailored for Apple II emulators and tooling suites. The API is designed to be reusable, extensible, and performant, supporting both emulator disk access and advanced tooling scenarios such as disk inspection, creation, and format conversion.

The design centers on a set of generic interfaces—`IMedia`, `IMedia<T>`, `IMediaMetadata`, and `IMediaMetadata<T>`—which decouple logical block access and metadata manipulation from concrete image formats and storage backends. A concrete implementation for the 2IMG format (`TwoImgDisk`) is provided as an exemplar, demonstrating how the API can wrap a disk image with rich metadata and flexible block access. Multiple storage backends are supported, including RAM-only, file-only, and RAM-cached modes, with configurable write-through and write-back caching strategies. The API also addresses sector ordering (DOS, ProDOS, physical, nibble), raw track handling (NIB, WOZ), format conversion, error handling, extensibility, and security.

This document draws on best practices from existing emulators, disk image libraries, and storage abstraction frameworks, integrating insights from projects such as CiderPress, AppleDiskImageReader, DiskAccessLibrary, and others. The goal is to provide a modern, idiomatic C# API that meets the needs of both emulator developers and power users, while remaining adaptable to future formats and use cases.

------

## API Design Goals and Architecture

### Design Goals

The API is guided by several core principles:

- **Abstraction**: Cleanly separate logical disk access, metadata, and storage backend concerns.
- **Extensibility**: Support new disk image formats, metadata schemas, and storage backends via plugins or subclassing.
- **Performance**: Enable efficient block access and caching, minimizing I/O overhead.
- **Clarity**: Provide a clear, discoverable, and idiomatic C# interface.
- **Safety**: Ensure robust error handling, validation, and thread-safety.
- **Tooling Support**: Expose APIs suitable for both emulation and advanced tooling (inspection, creation, conversion).
- **Format-Agnosticism**: Allow clients to interact with disk images without hardcoding format-specific logic.

### High-Level Architecture

The API is structured around three main abstraction layers:

1. **Media Abstraction Layer**: Defines `IMedia` and `IMedia<T>`, which provide logical block (or track/sector) read/write access, independent of the underlying image format or storage backend.
2. **Metadata Abstraction Layer**: Defines `IMediaMetadata` and `IMediaMetadata<T>`, which expose and allow modification of disk image metadata (e.g., 2IMG headers, WOZ info).
3. **Storage Backend Layer**: Abstracts the physical storage of disk image data, supporting RAM, file, and hybrid (RAM-cached) backends with configurable caching strategies.

Concrete disk image format handlers (e.g., `TwoImgDisk`, `WozDisk`) implement the media and metadata interfaces, delegating storage operations to pluggable backends. This separation enables flexible composition and reuse.

------

## Core Interface Definitions

### IMedia and IMedia

The `IMedia` interface abstracts logical block access for any disk image or drive backend. The generic variant, `IMedia<T>`, allows for type-safe access to format-specific features.

```csharp
public interface IMedia
{
    int BlockCount { get; }
    int BlockSize { get; }
    bool IsReadOnly { get; }

    // Read a logical block (by index) into a buffer.
    void ReadBlock(int blockIndex, Span<byte> buffer);

    // Write a logical block (by index) from a buffer.
    void WriteBlock(int blockIndex, ReadOnlySpan<byte> buffer);

    // Flush any pending writes to the storage backend.
    void Flush();

    // Optional: Expose raw track/sector or nibble data if supported.
    byte[]? GetRawTrackData(int trackIndex);
}

public interface IMedia<T> : IMedia
{
    // Returns the concrete disk image instance (e.g., TwoImgDisk).
    T Instance { get; }
}
```

**Design Rationale**:
 The `IMedia` interface provides a minimal, format-agnostic contract for block-level access, suitable for both 5.25" and 3.5" disks, hard disk images, and even future formats. The inclusion of `BlockCount` and `BlockSize` allows clients to query geometry without format-specific knowledge. The optional `GetRawTrackData` method enables advanced use cases (e.g., nibble or WOZ images) without polluting the core interface.

The generic `IMedia<T>` variant allows consumers to downcast to format-specific implementations when needed, supporting advanced features or optimizations.

### IMediaMetadata and IMediaMetadata

The `IMediaMetadata` interface exposes disk image metadata, such as headers, comments, and format-specific fields. The generic variant allows for type-safe access to structured metadata.

```csharp
public interface IMediaMetadata
{
    // Returns a dictionary of metadata fields (name-value pairs).
    IReadOnlyDictionary<string, object> GetMetadata();

    // Sets a metadata field (if supported).
    void SetMetadata(string key, object value);

    // Optional: Save metadata changes to the storage backend.
    void SaveMetadata();
}

public interface IMediaMetadata<T> : IMediaMetadata
{
    // Returns the strongly-typed metadata object (e.g., TwoImgInfo).
    T Metadata { get; }
}
```

**Design Rationale**:
 Disk image formats such as 2IMG and WOZ include rich metadata, including creator info, comments, and format flags. By abstracting metadata access, the API enables tooling to inspect and modify headers, comments, and other fields without format-specific code. The generic variant supports strongly-typed metadata objects, improving safety and discoverability.

------

## Concrete Implementation Example: TwoImgDisk

To illustrate the API in practice, we present a concrete implementation for the 2IMG format, which is widely used for Apple II disk images and supports multiple sector orderings and metadata fields.

### 2IMG Format Overview

The 2IMG format (also known as 2MG or 2IMG) is a universal container for Apple II disk images. It consists of a 64-byte header (prefix) containing metadata, followed by the disk image data, and optional comment and creator-specific data sections. The format supports DOS-order, ProDOS-order, and nibble-encoded images, and is extensible for future needs.

**Key 2IMG Metadata Fields**:

- Signature ("2IMG")
- Creator signature (e.g., "CPII")
- Format (DOS, ProDOS, Nibble)
- Number of blocks
- Data offset and length
- Comment and creator data offsets/lengths
- Flags (e.g., write-protection)

### TwoImgInfo: Strongly-Typed Metadata

```csharp
public class TwoImgInfo
{
    public string Signature { get; set; } // "2IMG"
    public string CreatorSignature { get; set; }
    public int HeaderLength { get; set; }
    public int Version { get; set; }
    public TwoImgFormat Format { get; set; }
    public int Flags { get; set; }
    public int NumberOfBlocks { get; set; }
    public int DataOffset { get; set; }
    public int DataLength { get; set; }
    public int CommentOffset { get; set; }
    public int CommentLength { get; set; }
    public int CreatorDataOffset { get; set; }
    public int CreatorDataLength { get; set; }
    public string? Comment { get; set; }
    public byte[]? CreatorData { get; set; }
}

public enum TwoImgFormat
{
    DOS = 0x00,
    ProDOS = 0x01,
    Nibble = 0x02
}
```

### TwoImgDisk Implementation

```csharp
public class TwoImgDisk : IMedia<TwoImgDisk>, IMediaMetadata<TwoImgInfo>
{
    private readonly IStorageBackend _backend;
    private readonly TwoImgInfo _metadata;

    public TwoImgDisk(IStorageBackend backend, TwoImgInfo metadata)
    {
        _backend = backend;
        _metadata = metadata;
    }

    public int BlockCount => _metadata.NumberOfBlocks;
    public int BlockSize => _metadata.Format == TwoImgFormat.Nibble ? 6656 : 512;
    public bool IsReadOnly => (_metadata.Flags & 0x01) != 0 || !_backend.CanWrite;

    public TwoImgDisk Instance => this;
    public TwoImgInfo Metadata => _metadata;

    public void ReadBlock(int blockIndex, Span<byte> buffer)
    {
        ValidateBlockIndex(blockIndex);
        _backend.Read(_metadata.DataOffset + blockIndex * BlockSize, buffer);
    }

    public void WriteBlock(int blockIndex, ReadOnlySpan<byte> buffer)
    {
        if (IsReadOnly)
            throw new InvalidOperationException("Disk is read-only.");
        ValidateBlockIndex(blockIndex);
        _backend.Write(_metadata.DataOffset + blockIndex * BlockSize, buffer);
    }

    public void Flush() => _backend.Flush();

    public byte[]? GetRawTrackData(int trackIndex)
    {
        if (_metadata.Format != TwoImgFormat.Nibble)
            return null;
        // For nibble images, each block is a track.
        var buffer = new byte[BlockSize];
        _backend.Read(_metadata.DataOffset + trackIndex * BlockSize, buffer);
        return buffer;
    }

    public IReadOnlyDictionary<string, object> GetMetadata()
    {
        // Return a dictionary view of TwoImgInfo.
        return new Dictionary<string, object>
        {
            ["Signature"] = _metadata.Signature,
            ["CreatorSignature"] = _metadata.CreatorSignature,
            ["Format"] = _metadata.Format,
            ["Flags"] = _metadata.Flags,
            ["NumberOfBlocks"] = _metadata.NumberOfBlocks,
            ["Comment"] = _metadata.Comment ?? string.Empty
            // ... add more fields as needed
        };
    }

    public void SetMetadata(string key, object value)
    {
        // Update metadata fields as appropriate.
        switch (key)
        {
            case "Comment":
                _metadata.Comment = value as string;
                break;
            // ... handle other fields
            default:
                throw new ArgumentException($"Unknown metadata key: {key}");
        }
    }

    public void SaveMetadata()
    {
        // Write updated metadata (e.g., comment) back to the storage backend.
        // Implementation depends on backend capabilities.
        _backend.Write(_metadata.CommentOffset, Encoding.UTF8.GetBytes(_metadata.Comment ?? ""));
        _backend.Flush();
    }

    private void ValidateBlockIndex(int blockIndex)
    {
        if (blockIndex < 0 || blockIndex >= BlockCount)
            throw new ArgumentOutOfRangeException(nameof(blockIndex));
    }
}
```

**Implementation Notes**:

- The `TwoImgDisk` class composes a storage backend and a strongly-typed metadata object.
- Block access is mapped to the appropriate offset within the image, respecting the format (DOS, ProDOS, Nibble).
- Metadata access and modification are exposed via both dictionary and strongly-typed interfaces.
- Read-only status is determined by both the 2IMG flags and the backend's capabilities.
- The implementation can be extended to support additional 2IMG features (e.g., creator data, advanced flags).

------

## Storage Backend Abstractions and Implementations

### IStorageBackend Interface

To decouple disk image logic from physical storage, the API defines a storage backend interface:

```csharp
public interface IStorageBackend
{
    bool CanWrite { get; }
    long Length { get; }

    // Read bytes from the backend at the given offset.
    void Read(long offset, Span<byte> buffer);

    // Write bytes to the backend at the given offset.
    void Write(long offset, ReadOnlySpan<byte> buffer);

    // Flush any pending writes.
    void Flush();
}
```

**Design Rationale**:
 This interface abstracts the underlying storage medium, enabling disk images to be backed by in-memory arrays, file streams, or hybrid (RAM-cached) implementations. It also enables future extensions such as network or cloud-backed storage.

### Storage Backend Implementations

#### RAM-Only Backend

A simple in-memory byte array, suitable for temporary images or testing.

```csharp
public sealed class RamStorageBackend : IStorageBackend
{
    private readonly byte[] _buffer;
    public bool CanWrite => true;
    public long Length => _buffer.Length;

    public RamStorageBackend(int size)
    {
        _buffer = new byte[size];
    }

    public void Read(long offset, Span<byte> buffer)
    {
        _buffer.AsSpan((int)offset, buffer.Length).CopyTo(buffer);
    }

    public void Write(long offset, ReadOnlySpan<byte> buffer)
    {
        buffer.CopyTo(_buffer.AsSpan((int)offset, buffer.Length));
    }

    public void Flush() { /* No-op for RAM */ }
}
```

#### File-Only Backend

Directly reads/writes from a file stream, suitable for large images or persistent storage.

```csharp
public sealed class FileStorageBackend : IStorageBackend, IDisposable
{
    private readonly FileStream _stream;
    public bool CanWrite => _stream.CanWrite;
    public long Length => _stream.Length;

    public FileStorageBackend(string path, FileAccess access)
    {
        _stream = new FileStream(path, FileMode.OpenOrCreate, access, FileShare.ReadWrite);
    }

    public void Read(long offset, Span<byte> buffer)
    {
        _stream.Seek(offset, SeekOrigin.Begin);
        _stream.Read(buffer);
    }

    public void Write(long offset, ReadOnlySpan<byte> buffer)
    {
        if (!CanWrite)
            throw new InvalidOperationException("Backend is read-only.");
        _stream.Seek(offset, SeekOrigin.Begin);
        _stream.Write(buffer);
    }

    public void Flush() => _stream.Flush();

    public void Dispose() => _stream.Dispose();
}
```

#### RAM-Cached Backend (Write-Through / Write-Back)

A hybrid backend that caches data in RAM for performance, with configurable write-through or write-back strategies.

```csharp
public sealed class RamCachedStorageBackend : IStorageBackend, IDisposable
{
    private readonly IStorageBackend _underlying;
    private readonly byte[] _cache;
    private readonly bool _writeThrough;
    private readonly BitArray _dirtyBlocks;
    private readonly int _blockSize;
    private readonly int _blockCount;

    public bool CanWrite => _underlying.CanWrite;
    public long Length => _cache.Length;

    public RamCachedStorageBackend(IStorageBackend underlying, int blockSize, bool writeThrough)
    {
        _underlying = underlying;
        _blockSize = blockSize;
        _blockCount = (int)(underlying.Length / blockSize);
        _cache = new byte[underlying.Length];
        _writeThrough = writeThrough;
        _dirtyBlocks = new BitArray(_blockCount);

        // Preload cache
        for (int i = 0; i < _blockCount; i++)
        {
            var span = new Span<byte>(_cache, i * _blockSize, _blockSize);
            _underlying.Read(i * _blockSize, span);
        }
    }

    public void Read(long offset, Span<byte> buffer)
    {
        new Span<byte>(_cache, (int)offset, buffer.Length).CopyTo(buffer);
    }

    public void Write(long offset, ReadOnlySpan<byte> buffer)
    {
        buffer.CopyTo(new Span<byte>(_cache, (int)offset, buffer.Length));
        int blockIndex = (int)(offset / _blockSize);
        _dirtyBlocks[blockIndex] = true;
        if (_writeThrough)
        {
            _underlying.Write(offset, buffer);
            _dirtyBlocks[blockIndex] = false;
        }
    }

    public void Flush()
    {
        if (!_writeThrough)
        {
            for (int i = 0; i < _blockCount; i++)
            {
                if (_dirtyBlocks[i])
                {
                    var span = new ReadOnlySpan<byte>(_cache, i * _blockSize, _blockSize);
                    _underlying.Write(i * _blockSize, span);
                    _dirtyBlocks[i] = false;
                }
            }
        }
        _underlying.Flush();
    }

    public void Dispose() => (_underlying as IDisposable)?.Dispose();
}
```

**Caching Strategies**:

- **Write-Through**: Every write is immediately persisted to the underlying storage. This is safer but may be slower.
- **Write-Back**: Writes are cached in RAM and only flushed on demand (e.g., on `Flush()` or disposal). This improves performance but risks data loss on crash or power failure.

**Design Rationale**:
 By abstracting storage backends, the API enables flexible deployment scenarios—fast in-memory testing, persistent file-backed images, or high-performance RAM-cached operation. The caching strategy can be selected based on the application's needs for performance versus data integrity.

------

## Sector/Block Addressing and Ordering Strategies

### Overview of Sector Ordering

Apple II disk images are notorious for their sector ordering ambiguities. The same physical disk can be represented in multiple logical orders, depending on the imaging tool and the intended operating system:

- **DOS Order (DO, .dsk)**: Sectors are stored in the order used by DOS 3.3 (logical sector numbers mapped to physical sectors via a skew table).
- **ProDOS Order (PO, .po)**: Sectors are grouped into 512-byte blocks, with ProDOS-specific interleaving.
- **Physical Order**: Sectors are stored in the order they physically appear on the track (rarely used in images, but relevant for some tools).
- **Nibble Order (NIB, .nib)**: Raw track data, including address and data fields, stored as a continuous byte stream per track.

**Table: Sector Ordering Comparison**

| Format       | Typical Extension | Ordering          | Block Size | Notes                               |
| ------------ | ----------------- | ----------------- | ---------- | ----------------------------------- |
| DOS Order    | .dsk, .do         | DOS 3.3 skew      | 256 bytes  | Most common for 5.25" images        |
| ProDOS Order | .po               | ProDOS interleave | 512 bytes  | Used for ProDOS, Pascal, hard disks |
| Physical     | .img              | Physical order    | 256 bytes  | Rare; used by Copy ][+              |
| Nibble       | .nib              | Raw track data    | 6656 bytes | Includes address/data fields, sync  |

**Explanation**:

- In DOS order, sectors are arranged according to the logical-to-physical mapping used by DOS 3.3, which optimizes for sequential reads given the drive's rotational latency.
- ProDOS order groups sectors into 512-byte blocks, with a different interleave to optimize for ProDOS's access patterns.
- Physical order is rarely used but can be found in some tools and is the native order for nibble images.
- Nibble images store the actual bytes as read from the disk controller, including sector headers, sync bytes, and gaps.

### Handling Sector Ordering in the API

The API must abstract away sector ordering, allowing clients to access logical blocks regardless of the underlying format. This is achieved by:

- **Mapping logical block indices to physical offsets** using format-specific lookup tables.
- **Exposing sector/block order as part of the metadata**, enabling tooling to display or convert orderings.
- **Providing conversion utilities** to reorder sectors/blocks when converting between formats.

**Example: DOS to ProDOS Block Mapping**

For a standard 5.25" disk (35 tracks, 16 sectors per track):

- DOS order: sectors are stored as T0S0, T0S1, ..., T34S15.
- ProDOS order: blocks are stored as Block 0 (T0S0 + T0S2), Block 1 (T0S4 + T0S6), etc.

A lookup table can be used to map between these orders for read/write operations.

------

## Nibble, NIB, and WOZ Raw Track Handling

### NIB Format

The NIB format stores raw track data as read from the disk controller, including address fields, data fields, and sync bytes. Each track is typically stored as a fixed-length buffer (e.g., 6656 bytes for 5.25" disks), with no headers or metadata.

**Key Characteristics**:

- Preserves low-level disk structure, enabling emulation of copy-protected disks.
- Larger file size (~232,960 bytes for 35 tracks).
- Difficult to interpret without knowledge of Apple II disk encoding (GCR, sector headers, etc.).

### WOZ Format

The WOZ format is a modern bitstream image format designed to capture every possible Apple II disk structure, including copy protection and weak bits. It stores a highly accurate representation of the disk's magnetic flux transitions, enabling perfect emulation of even the most complex disks.

**Key Characteristics**:

- Supports both 5.25" and 3.5" disks.
- Includes a rich metadata header describing disk geometry, format, and protection features.
- Used by modern emulators and hardware drive emulators (e.g., Floppy Emu, wDrive).

### API Support for Raw Track Access

The API exposes optional methods for accessing raw track data, enabling advanced emulation and tooling scenarios:

- `byte[]? GetRawTrackData(int trackIndex)` in `IMedia`.
- Format-specific implementations (e.g., `WozDisk`, `NibDisk`) provide full access to track data and metadata.

This design allows emulators to implement accurate disk controllers, while tooling can inspect or manipulate raw tracks for analysis or conversion.

------

## Disk Image Format Comparison

A clear understanding of the various Apple II disk image formats is essential for both API design and tooling. The following table summarizes the most common formats:

| Format | Extension(s) | Structure         | Metadata       | Use Cases                  | Copy Protection | Notes                      |
| ------ | ------------ | ----------------- | -------------- | -------------------------- | --------------- | -------------------------- |
| DSK/DO | .dsk, .do    | DOS order sectors | None           | DOS 3.3 disks, emulators   | No              | 143,360 bytes (140KB)      |
| PO     | .po          | ProDOS order blks | None           | ProDOS, Pascal, hard disks | No              | 143,360 bytes (140KB)      |
| NIB    | .nib         | Raw track data    | None           | Copy-protected disks       | Yes (partial)   | 232,960 bytes (35x6656)    |
| WOZ    | .woz         | Bitstream tracks  | Rich header    | Accurate emulation         | Yes (full)      | Supports 5.25"/3.5" disks  |
| 2IMG   | .2mg, .2img  | Container + data  | 64-byte header | Universal, metadata-rich   | No (usually)    | Supports DOS/ProDOS/Nibble |

**Analysis**:

- **DSK/DO/PO**: Simple, unadorned images; easy to manipulate but cannot represent copy protection or non-standard formats.
- **NIB**: Captures more low-level detail, enabling partial support for protected disks, but lacks timing information and full accuracy.
- **WOZ**: The gold standard for preservation, supporting all disk structures and protections.
- **2IMG**: A flexible container format, supporting multiple orderings and metadata, but not as low-level as WOZ.

------

## Conversion Strategies Between Formats

### General Principles

Conversion between disk image formats is a common requirement for both emulators and tooling. The API should facilitate:

- **Order-preserving conversions** (e.g., DOS-order DSK to ProDOS-order PO).
- **Raw-to-logical conversions** (e.g., NIB/WOZ to DSK/PO, extracting sector data).
- **Logical-to-raw conversions** (e.g., DSK/PO to NIB/WOZ, reconstructing tracks).
- **Metadata preservation** (e.g., retaining comments, creator info in 2IMG).

### Example: 2IMG Conversion

The 2IMG format can encapsulate DOS, ProDOS, or Nibble images. Conversion involves:

- **Extracting the disk image data** from the 2IMG container.
- **Interpreting the format field** to determine sector ordering or nibble encoding.
- **Reordering sectors/blocks** as needed for the target format.
- **Preserving or mapping metadata** (e.g., comments, flags).

**Sample Conversion Flow: 2IMG to DSK/PO/NIB**

1. Parse the 2IMG header to determine format and offsets.
2. Extract the image data section.
3. If format is DOS, write as .dsk; if ProDOS, write as .po; if Nibble, write as .nib.
4. Optionally, extract and save comments or creator data.

**Sample Conversion Flow: DSK/PO/NIB to 2IMG**

1. Determine the source format and sector ordering.
2. Create a new 2IMG header with appropriate fields.
3. Embed the image data, comments, and creator data as needed.
4. Write the resulting file.

### Tools and Libraries

- **woz2dsk**: Converts WOZ images to DSK, PO, or NIB, handling sector ordering and format detection.
- **dsk2woz2**: Converts DSK images to WOZ, supporting both DOS and ProDOS disks, and making the resulting WOZ writable.
- **CiderPress**: Provides comprehensive conversion and inspection tools, supporting all major formats.

### API Support

The API can expose conversion utilities as static methods or extension methods, leveraging the core interfaces for reading/writing blocks and metadata. For example:

```csharp
public static class DiskImageConverter
{
    public static void ConvertTo2Img(IMedia source, IMediaMetadata? metadata, Stream output)
    {
        // Write 2IMG header, copy blocks, preserve metadata.
    }

    public static void ConvertToWoz(IMedia source, IMediaMetadata? metadata, Stream output)
    {
        // Reconstruct bitstream, write WOZ header, etc.
    }

    // ... other conversions
}
```

------

## Performance Considerations and Caching Strategies

### Caching Modes

- **RAM-Only**: Fastest access, suitable for temporary or small images.
- **File-Only**: Direct access to disk, suitable for large images or persistent storage.
- **RAM-Cached**: Balances performance and persistence; configurable as write-through or write-back.

**Write-Through vs. Write-Back**:

- **Write-Through**: Every write is immediately persisted to the underlying storage. Ensures data integrity but may incur higher latency.
- **Write-Back**: Writes are cached in RAM and only flushed on demand. Improves performance but risks data loss on crash or power failure. Requires careful management of dirty blocks and flush operations.

### Partial Writes and Incremental Flush

To optimize I/O, especially for large images, the API supports partial writes and incremental flushes. The storage backend interface allows clients to specify the offset and size of modified regions, enabling efficient updates.

### Thread-Safety and Concurrency

- **Locking**: Storage backends and disk image implementations should use fine-grained locking to ensure thread-safety during concurrent access.
- **Atomicity**: Flush and save operations should be atomic where possible, to prevent partial writes or corruption.
- **Async Support**: For high-performance scenarios, consider providing asynchronous variants of read/write/flush methods.

------

## Extensibility: Plugins, Backends, and Format Handlers

### Plugin Architecture

To support new disk image formats, metadata schemas, or storage backends, the API can adopt a plugin architecture:

- **Format Handlers**: Implement `IMedia` and `IMediaMetadata` for new formats (e.g., WOZ, NIB, DSK).
- **Storage Backends**: Implement `IStorageBackend` for new storage types (e.g., cloud, network, compressed).
- **Registration**: Use a registry or factory pattern to discover and instantiate handlers based on file extension, magic bytes, or user selection.

**Example: Factory Pattern for Disk Images**

```csharp
public static class DiskImageFactory
{
    private static readonly Dictionary<string, Func<Stream, IMedia>> _handlers = new();

    public static void RegisterHandler(string extension, Func<Stream, IMedia> handler)
    {
        _handlers[extension.ToLowerInvariant()] = handler;
    }

    public static IMedia Open(Stream stream, string extension)
    {
        if (_handlers.TryGetValue(extension.ToLowerInvariant(), out var handler))
            return handler(stream);
        throw new NotSupportedException($"No handler for extension {extension}");
    }
}
```

### Design Patterns

- **Adapter**: Wraps legacy or third-party disk image libraries to conform to the API.
- **Decorator**: Adds features (e.g., logging, validation, encryption) to existing backends or disk images.
- **Strategy**: Selects sector ordering or conversion algorithms at runtime.
- **Factory**: Instantiates disk images and backends based on format or configuration.

------

## Error Handling, Validation, and Corruption Detection

### Error Handling

- **Exceptions**: Use idiomatic C# exceptions for invalid operations (e.g., out-of-bounds access, read-only writes).
- **Validation**: Validate disk image headers, metadata, and block indices on load and access.
- **Corruption Detection**: Implement checksums or hash verification for supported formats (e.g., 2IMG, WOZ, DiskCopy).
- **Graceful Degradation**: Allow read-only access to partially corrupted images where possible.

### Example: Checksum Verification

For formats with checksums (e.g., DiskCopy, WOZ), the API can expose methods to verify integrity:

```csharp
public interface IChecksummedMedia
{
    bool VerifyChecksum();
}
```

Tooling can use these methods to alert users to potential corruption or tampering.

------

## Tooling API Surface for Inspection and Creation

### Inspection

- **Block/Sector Browsing**: Expose APIs to enumerate and read individual blocks, sectors, or tracks.
- **Metadata Display**: Provide structured access to all metadata fields, including comments and creator data.
- **Filesystem Integration**: Optionally, expose higher-level APIs for reading files and directories within disk images (e.g., ProDOS, DOS 3.3).

### Creation

- **Blank Image Creation**: Provide factory methods to create new disk images with specified geometry, format, and metadata.
- **Bootable Disk Creation**: Support utilities to initialize boot sectors and system files for bootable images.
- **Format Conversion**: Expose conversion APIs for batch processing and automation.

### Example: Disk Image Creation

```csharp
public static class DiskImageCreator
{
    public static TwoImgDisk CreateBlank2Img(int blockCount, TwoImgFormat format, string creator, string? comment = null)
    {
        var metadata = new TwoImgInfo
        {
            Signature = "2IMG",
            CreatorSignature = creator,
            HeaderLength = 64,
            Version = 1,
            Format = format,
            Flags = 0,
            NumberOfBlocks = blockCount,
            DataOffset = 64,
            DataLength = blockCount * 512,
            CommentOffset = 64 + blockCount * 512,
            CommentLength = comment?.Length ?? 0,
            Comment = comment
        };
        var backend = new RamStorageBackend(metadata.DataLength + (comment?.Length ?? 0));
        return new TwoImgDisk(backend, metadata);
    }
}
```

------

## Testing Strategies and Test Vectors

### Unit Testing

- **Block Access**: Test reading and writing of blocks at all valid and invalid indices.
- **Metadata Manipulation**: Test reading, setting, and saving metadata fields.
- **Storage Backends**: Test RAM, file, and RAM-cached backends for correctness and performance.
- **Error Handling**: Test exception paths for invalid operations, read-only enforcement, and corruption detection.

### Integration Testing

- **Format Conversion**: Test round-trip conversions between all supported formats (2IMG, DSK, PO, NIB, WOZ).
- **Sector Ordering**: Test correct mapping of logical to physical sectors/blocks for all orderings.
- **Concurrency**: Test thread-safety under concurrent access and flush operations.

### Test Vectors

- Use known-good disk images from emulator test suites and archival projects.
- Include images with edge cases (e.g., non-standard geometry, copy protection, corrupted headers).

------

## Examples from Existing Projects and Libraries

### CiderPress

CiderPress is a widely used Apple II disk image utility and library, supporting inspection, conversion, and manipulation of all major formats. Its architecture separates disk image parsing, filesystem access, and storage, providing a model for the proposed API.

### AppleDiskImageReader

A lightweight .NET library for reading 2IMG images, supporting metadata parsing and extraction of disk image data. Demonstrates idiomatic C# implementation and strong typing for metadata.

### DiskAccessLibrary

An open-source C# library for accessing physical and virtual disks, with abstractions for storage backends and on-disk structures. Provides inspiration for backend design and error handling.

### DiskM8

A cross-platform command-line tool for manipulating Apple II disk images, supporting multiple formats and advanced analysis features. Illustrates the value of a unified API for both emulation and tooling.

------

## API Usage Scenarios: Emulator vs. Tooling

### Emulator Integration

- **Mounting Disk Images**: Emulators can mount any supported disk image via `IMedia`, regardless of format or storage backend.
- **Block Access**: The emulator's disk controller logic interacts with `ReadBlock`/`WriteBlock` methods, abstracting away sector ordering and format details.
- **Raw Track Emulation**: For advanced emulation (e.g., copy protection), the emulator can access raw track data via `GetRawTrackData`.

### Tooling Integration

- **Disk Inspection**: Tools can enumerate blocks, sectors, and metadata fields for analysis and reporting.
- **Image Creation**: Tools can create new disk images, initialize boot sectors, and set metadata.
- **Format Conversion**: Tools can batch convert images between formats, preserving metadata and sector order.

------

## Security and Sandboxing Considerations

- **Read-Only Enforcement**: The API enforces read-only status based on both image metadata (e.g., 2IMG flags) and backend capabilities.
- **Input Validation**: All disk image headers and metadata are validated on load to prevent buffer overflows or malformed data.
- **Sandboxing**: When used in untrusted environments (e.g., web-based tools), storage backends can be restricted to in-memory operation, preventing file system access.
- **Resource Limits**: The API can enforce limits on image size, block count, and memory usage to prevent denial-of-service attacks.

------

## Summary Table: Disk Image Format Comparison

| Format | Extension(s) | Structure        | Metadata    | Sector Order   | Copy Protection | Typical Use Cases          |
| ------ | ------------ | ---------------- | ----------- | -------------- | --------------- | -------------------------- |
| DSK/DO | .dsk, .do    | 256B sectors     | None        | DOS order      | No              | DOS 3.3 disks, emulators   |
| PO     | .po          | 512B blocks      | None        | ProDOS order   | No              | ProDOS, Pascal, hard disks |
| NIB    | .nib         | Raw track data   | None        | Physical       | Partial         | Copy-protected disks       |
| WOZ    | .woz         | Bitstream tracks | Rich header | Physical       | Full            | Accurate emulation         |
| 2IMG   | .2mg, .2img  | Container + data | 64B header  | DOS/ProDOS/Nib | No (usually)    | Universal, metadata-rich   |

------

## Implementation Notes and Design Rationale

- **Separation of Concerns**: By decoupling media, metadata, and storage, the API enables flexible composition and future extensibility.
- **Strong Typing**: Generic interfaces and strongly-typed metadata objects improve safety and discoverability.
- **Performance**: RAM-cached backends and partial flushes optimize for both speed and data integrity.
- **Extensibility**: Plugin architecture and design patterns (adapter, decorator, factory) enable support for new formats and backends.
- **Robustness**: Comprehensive error handling, validation, and corruption detection ensure reliability in both emulation and tooling contexts.
- **Security**: Read-only enforcement, input validation, and sandboxing protect against misuse and vulnerabilities.

------

## Conclusion

The proposed C# disk image abstraction API provides a robust, extensible foundation for Apple II emulation and tooling. By unifying logical block access, metadata manipulation, and storage backend abstraction, it empowers developers to support a wide range of disk image formats, storage scenarios, and use cases. The API's design draws on best practices from leading projects and libraries, while embracing modern C# idioms and extensibility patterns. With support for advanced features such as sector ordering, raw track access, format conversion, and caching strategies, the API is well-suited to both emulator integration and sophisticated tooling. As the Apple II community continues to preserve and explore its digital heritage, such an API will be an invaluable asset for both developers and users.