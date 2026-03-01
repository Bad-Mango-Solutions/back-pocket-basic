# **📚 Disk Inspection, Image Creation, Analysis & Conversion API**

### *A layered, extensible architecture built on your unified block‑device model*

------

# **1. High‑Level Architecture**

Your new subsystem consists of four cooperating layers:

```
┌──────────────────────────────┐
│  DiskImageFormat Providers   │  (.2mg, .po, .do, .nib, .woz, .hdv, .dimg)
└───────────────┬──────────────┘
                │
┌───────────────▼──────────────┐
│   IMedia / IMediaMetadata     │  (your existing block device abstraction)
└───────────────┬──────────────┘
                │
┌───────────────▼──────────────┐
│   Filesystem Inspectors       │  (ProDOS, DOS 3.3, Pascal, GS/OS FSTs)
└───────────────┬──────────────┘
                │
┌───────────────▼──────────────┐
│   Disk Tools API              │  (create, inspect, analyze, convert)
└──────────────────────────────┘
```

This keeps **image formats**, **block devices**, and **filesystems** cleanly separated.

------

# **2. Core Interfaces**

## **2.1 IMedia (already defined by you)**

Represents a block‑addressable device.

## **2.2 IMediaMetadata**

Represents metadata for a disk image format.

```csharp
public interface IMediaMetadata<TMetadata>
{
    TMetadata GetMetadata();
    void SetMetadata(TMetadata metadata);
}
```

## **2.3 IDiskImageFormat**

Every disk image format implements this.

```csharp
public interface IDiskImageFormat
{
    string FormatName { get; }
    string[] FileExtensions { get; }

    bool CanLoad(ReadOnlySpan<byte> header);
    IMedia Load(Stream stream, out IMediaMetadata? metadata);

    void Save(IMedia media, IMediaMetadata? metadata, Stream output);
}
```

This gives you a **plugin‑style registry** for formats.

------

# **3. Filesystem Inspection API**

This is where ProDOS directory parsing, DOS 3.3 catalog reading, etc. live.

## **3.1 IFileSystemInspector**

```csharp
public interface IFileSystemInspector
{
    bool CanInspect(IMedia media);

    FileSystemInfo Inspect(IMedia media);

    IEnumerable<FileEntry> EnumerateFiles(IMedia media, string? path = null);

    Stream OpenFile(IMedia media, string path);
}
```

## **3.2 FileSystemInfo**

```csharp
public record FileSystemInfo(
    string FileSystemType,
    uint TotalBlocks,
    uint FreeBlocks,
    IReadOnlyDictionary<string, object>? Extended
);
```

## **3.3 FileEntry**

```csharp
public record FileEntry(
    string Name,
    string Path,
    uint StartBlock,
    uint LengthBytes,
    FileType Type,
    DateTime? Created,
    DateTime? Modified,
    IReadOnlyDictionary<string, object>? Extended
);
```

------

# **4. ProDOS Filesystem Inspector**

This is the most important inspector for Apple II tooling.

### **4.1 ProDOS Directory Parsing**

- Read block 2 (volume header)
- Follow index blocks for subdirectories
- Parse entries (name, type, key block, EOF, timestamps)
- Handle seedling/sapling/tree files

### **4.2 ProDOS Inspector Implementation**

```csharp
public class ProDosInspector : IFileSystemInspector
{
    public bool CanInspect(IMedia media)
    {
        // Check block 2 for ProDOS signature
    }

    public FileSystemInfo Inspect(IMedia media)
    {
        // Parse volume header
    }

    public IEnumerable<FileEntry> EnumerateFiles(IMedia media, string? path = null)
    {
        // Recursively walk directories
    }

    public Stream OpenFile(IMedia media, string path)
    {
        // Return a stream that reads seedling/sapling/tree files
    }
}
```

------

# **5. Disk Tools API**

This is the user‑facing API for your debug console and tooling.

```csharp
public interface IDiskTools
{
    IMedia CreateBlankImage(DiskCreationOptions options);

    DiskAnalysisResult Analyze(IMedia media);

    IEnumerable<FileEntry> ListFiles(IMedia media);

    void ConvertImage(Stream input, IDiskImageFormat from,
                      Stream output, IDiskImageFormat to);

    void FormatProDos(IMedia media, string volumeName);
}
```

------

# **6. Disk Creation API**

## **6.1 DiskCreationOptions**

```csharp
public record DiskCreationOptions(
    string Format,          // "2mg", "po", "woz", "hdv"
    uint BlockCount,
    uint BlockSize,
    string? VolumeName,
    bool Bootable,
    bool InitializeFileSystem
);
```

## **6.2 Creating a blank ProDOS disk**

Steps:

1. Allocate blocks
2. Write ProDOS volume header
3. Write root directory
4. Install boot blocks (optional)
5. Wrap in chosen image format (2MG, PO, WOZ, etc.)

------

# **7. Disk Analysis API**

## **7.1 DiskAnalysisResult**

```csharp
public record DiskAnalysisResult(
    string Format,
    string FileSystem,
    uint BlockCount,
    uint BlockSize,
    bool IsBootable,
    IReadOnlyList<string> Issues,
    IReadOnlyDictionary<string, object>? Extended
);
```

Analysis includes:

- Format detection
- Filesystem detection
- Boot sector validation
- ProDOS volume header validation
- Directory structure integrity
- Orphaned blocks
- Cross‑linked files
- Timestamp anomalies

------

# **8. Format Conversion API**

This is the Apple II equivalent of `qemu-img convert`.

## **8.1 Conversion Flow**

```
Input Stream → IDiskImageFormat.Load → IMedia → IDiskImageFormat.Save → Output Stream
```

## **8.2 Special Cases**

### **PO/DO → 2MG**

- Wrap raw blocks in a 2MG header

### **2MG → PO**

- Strip header
- Reorder sectors if needed

### **2MG → WOZ**

- Requires nibblization (optional)
- Or store raw blocks in WOZ v2’s “raw track” mode

### **WOZ → 2MG**

- Requires denibblization (track → sector extraction)

### **NIB → 2MG**

- Same as WOZ but without metadata

------

# **9. Storage Backends**

Your existing storage model fits perfectly:

| Backend        | Pros                        | Cons               |
| -------------- | --------------------------- | ------------------ |
| **RAM‑only**   | Fast, ideal for debug tools | Volatile           |
| **File‑only**  | Minimal RAM                 | Slow random access |
| **RAM‑cached** | Best of both                | Complexity         |

All backends implement `IMedia`.

------

# **10. Example: TwoImgDisk Implementation**

```csharp
public class TwoImgDisk :
    IMedia<TwoImgDisk>,
    IMediaMetadata<TwoImgInfo>
{
    private readonly IStorageBackend _storage;
    private TwoImgInfo _metadata;

    public TwoImgDisk(IStorageBackend storage, TwoImgInfo metadata)
    {
        _storage = storage;
        _metadata = metadata;
    }

    public TwoImgInfo GetMetadata() => _metadata;

    public void SetMetadata(TwoImgInfo metadata) => _metadata = metadata;

    public Task ReadBlockAsync(uint block, Span<byte> buffer)
        => _storage.ReadAsync(block, buffer);

    public Task WriteBlockAsync(uint block, ReadOnlySpan<byte> buffer)
        => _storage.WriteAsync(block, buffer);
}
```

------

# **11. Putting It All Together**

Your debug console can now do things like:

```
disk create blank.po --blocks 280 --bootable --name WORK
disk inspect disk.2mg
disk ls disk.2mg
disk cat disk.2mg /BASIC/HELLO
disk convert disk.2mg disk.woz
disk analyze disk.po
```

All powered by:

- Unified block device API
- Pluggable image formats
- Pluggable filesystem inspectors
- Clean metadata model
- Storage backends

This is the foundation of a **world‑class Apple II disk subsystem**.

------

If you want, I can now generate:

- Full C# implementations for each interface
- A complete ProDOS filesystem parser
- A WOZ reader/writer
- A conversion engine
- A command‑line tool spec for your debugger

Just tell me where you want to go next.