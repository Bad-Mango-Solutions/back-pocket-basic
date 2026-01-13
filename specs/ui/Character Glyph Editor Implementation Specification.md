# Character Glyph Editor Implementation Specification

## Document Information

| Field        | Value                                       |
| ------------ | ------------------------------------------- |
| Version      | 1.0                                         |
| Date         | 2026-01-13                                  |
| Status       | Draft                                       |
| Applies To   | Back Pocket BASIC Emulator Toolchain        |
| Related Docs | Apple II Character Generation Specification |
|              | Emulator UI Specification                   |
|              | Architecture Spec v1.0                      |

---

## 1. Overview

This specification defines the implementation of a character glyph file editor for the Back Pocket BASIC emulator toolchain. The editor enables users to create, modify, and test Apple II character glyph files (4KB, containing two 2KB character sets).

(Note: Implementation examples in this file are suggestions only.)

### 1.1 Goals

1. **Visual glyph editing**: Intuitive 7×8 pixel bitmap editor for individual characters
2. **Character set management**: View and navigate all 256 characters in each set
3. **File operations**: Create, open, save, and export glyph files
4. **Emulator integration**: Hot-load glyphs into a running emulator for live testing
5. **Reference support**: Import from existing Apple II ROM files

### 1.2 Non-Goals

1. Font rendering or TrueType conversion
2. Animation or sprite editing
3. Palette or color editing (characters are monochrome)

---

## 2. Architecture

### 2.1 Project Structure

The editor will be implemented as part of the existing `charmap` CLI tool, adding a new `edit` subcommand that launches an Avalonia window. 

```
src/
├── BadMango.Tools.CharMap/
│   ├── Commands/
│   │   ├── EditCommand.cs           # New:  launches editor window
│   │   └── ...  (existing commands)
│   └── Editor/
│       ├── Views/
│       │   ├── GlyphEditorWindow.axaml
│       │   ├── CharacterGridControl.axaml
│       │   ├── BitmapEditorControl.axaml
│       │   └── ToolButtonPanel.axaml
│       ├── ViewModels/
│       │   ├── GlyphEditorViewModel.cs
│       │   ├── CharacterGridViewModel. cs
│       │   └── BitmapEditorViewModel.cs
│       ├── Models/
│       │   ├── GlyphFile.cs
│       │   ├── CharacterGlyph.cs
│       │   └── GlyphEditHistory.cs
│       └── Services/
│           ├── IGlyphFileService.cs
│           ├── GlyphFileService.cs
│           ├── IEmulatorConnection.cs
│           └── EmulatorConnectionService.cs
```

### 2.2 Component Diagram

```
┌─────────────────────────────────────────────────────────────────────────┐
│                         GlyphEditorWindow                               │
├─────────────────────────────────────────────────────────────────────────┤
│  ┌──────────────────────────────────────────────────────────────────┐   │
│  │                        Menu Bar                                  │   │
│  │  File | Edit | View | Tools | Emulator | Help                    │   │
│  └──────────────────────────────────────────────────────────────────┘   │
│  ┌──────────────────────────────────────────────────────────────────┐   │
│  │  ○ Primary Set   ○ Alternate Set    Char:  $41 'A' [Normal]      │   │
│  └──────────────────────────────────────────────────────────────────┘   │
│  ┌─────────────────────────────┬────────────────────────────────────┐   │
│  │                             │                                    │   │
│  │    CharacterGridControl     │      BitmapEditorControl           │   │
│  │    (16×16 = 256 chars)      │      (7×8 pixel grid)              │   │
│  │                             │                                    │   │
│  │    ┌─┬─┬─┬─┬─┬─┬─┬─┐        │      ┌─────────────────┐   ┌───┐   │   │
│  │    │@│A│B│C│D│E│F│G│        │      │ ▪ ▪ █ █ ▪ ▪ ▪   │   │   │   │   │
│  │    ├─┼─┼─┼─┼─┼─┼─┼─┤        │      │ ▪ █ ▪ ▪ █ ▪ ▪   │   │ A │   │   │
│  │    │H│I│J│K│L│M│N│O│        │      │ █ ▪ ▪ ▪ ▪ █ ▪   │   │   │   │   │
│  │    ├─┼─┼─┼─┼─┼─┼─┼─┤        │      │ █ █ █ █ █ █ ▪   │   └───┘   │   │
│  │    │... 16 rows ...│        │      │ █ ▪ ▪ ▪ ▪ █ ▪   │  Preview  │   │
│  │    └─┴─┴─┴─┴─┴─┴─┴─┘        │      │ █ ▪ ▪ ▪ ▪ █ ▪   │           │   │
│  │                             │      │ ▪ ▪ ▪ ▪ ▪ ▪ ▪   │           │   │
│  │    Zoom: [−][+]             │      │ ▪ ▪ ▪ ▪ ▪ ▪ ▪   │           │   │
│  │    ☑ Show Grid             │      └─────────────────┘           │   │
│  │                             │                                    │   │
│  │                             │      ToolButtonPanel               │   │
│  │                             │      [↑][↓][←][→] [Inv][Clr][Fill] │   │
│  │                             │      [↺][↻] [⇅][⇄] [Line]        │   │
│  └─────────────────────────────┴────────────────────────────────────┘   │
│  ┌──────────────────────────────────────────────────────────────────┐   │
│  │  Status:  Ready | Modified | chars selected:  1                  │   │
│  └──────────────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## 3. Data Model

### 3.1 Glyph File Format

Glyph files are 4096 bytes (4KB), containing two 2KB character sets:

| Offset      | Size | Content                             |
| ----------- | ---- | ----------------------------------- |
| $0000-$07FF | 2KB  | Primary character set (256 chars)   |
| $0800-$0FFF | 2KB  | Alternate character set (256 chars) |

Each character occupies 8 consecutive bytes (8 scanlines, top to bottom).

### 3.2 Character Glyph Structure

```csharp
/// <summary>
/// Represents a single character glyph (8 bytes, 7×8 pixels).
/// </summary>
public sealed class CharacterGlyph
{
    /// <summary>
    /// The 8 scanlines of the glyph, from top (index 0) to bottom (index 7).
    /// Each byte contains 7 pixels in bits 0-6; bit 7 is unused.
    /// </summary>
    public byte[] Scanlines { get; } = new byte[8];
    
    /// <summary>
    /// Gets or sets a pixel value. 
    /// </summary>
    /// <param name="x">X coordinate (0-6, where 0 is leftmost).</param>
    /// <param name="y">Y coordinate (0-7, where 0 is topmost).</param>
    /// <returns>True if pixel is set, false if clear.</returns>
    public bool this[int x, int y]
    {
        get
        {
            ArgumentOutOfRangeException.ThrowIfNegative(x);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(x, 6);
            ArgumentOutOfRangeException.ThrowIfNegative(y);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(y, 7);
            
            // Bit 6 is leftmost pixel, bit 0 is rightmost
            int bit = 6 - x;
            return (Scanlines[y] & (1 << bit)) != 0;
        }
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(x);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(x, 6);
            ArgumentOutOfRangeException.ThrowIfNegative(y);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(y, 7);
            
            int bit = 6 - x;
            if (value)
                Scanlines[y] |= (byte)(1 << bit);
            else
                Scanlines[y] &= (byte)~(1 << bit);
        }
    }
    
    /// <summary>
    /// Creates a deep copy of this glyph. 
    /// </summary>
    public CharacterGlyph Clone()
    {
        var clone = new CharacterGlyph();
        Array.Copy(Scanlines, clone.Scanlines, 8);
        return clone;
    }
    
    /// <summary>
    /// Copies scanline data from a byte array.
    /// </summary>
    public void CopyFrom(ReadOnlySpan<byte> source)
    {
        if (source.Length < 8)
            throw new ArgumentException("Source must contain at least 8 bytes.", nameof(source));
        source[.. 8].CopyTo(Scanlines);
    }
    
    /// <summary>
    /// Copies scanline data to a byte array. 
    /// </summary>
    public void CopyTo(Span<byte> destination)
    {
        if (destination.Length < 8)
            throw new ArgumentException("Destination must have capacity for 8 bytes.", nameof(destination));
        Scanlines.CopyTo(destination);
    }
}
```

### 3.3 Glyph File Model

```csharp
/// <summary>
/// Represents a complete 4KB glyph file with two character sets.
/// </summary>
public sealed class GlyphFile
{
    public const int FileSize = 4096;
    public const int CharacterSetSize = 2048;
    public const int CharactersPerSet = 256;
    public const int BytesPerCharacter = 8;
    
    private readonly CharacterGlyph[] _primarySet = new CharacterGlyph[CharactersPerSet];
    private readonly CharacterGlyph[] _alternateSet = new CharacterGlyph[CharactersPerSet];
    
    /// <summary>
    /// Gets the file path, or null if this is a new unsaved file.
    /// </summary>
    public string? FilePath { get; private set; }
    
    /// <summary>
    /// Gets whether the file has unsaved modifications.
    /// </summary>
    public bool IsModified { get; private set; }
    
    /// <summary>
    /// Gets a glyph from the specified character set.
    /// </summary>
    /// <param name="charCode">Character code (0-255).</param>
    /// <param name="useAlternateSet">True for alternate set, false for primary. </param>
    public CharacterGlyph GetGlyph(byte charCode, bool useAlternateSet)
        => useAlternateSet ? _alternateSet[charCode] :  _primarySet[charCode];
    
    /// <summary>
    /// Gets the primary character set.
    /// </summary>
    public IReadOnlyList<CharacterGlyph> PrimarySet => _primarySet;
    
    /// <summary>
    /// Gets the alternate character set.
    /// </summary>
    public IReadOnlyList<CharacterGlyph> AlternateSet => _alternateSet;
    
    /// <summary>
    /// Marks the file as modified.
    /// </summary>
    public void MarkModified() => IsModified = true;
    
    /// <summary>
    /// Clears the modified flag (called after save).
    /// </summary>
    public void ClearModified() => IsModified = false;
    
    /// <summary>
    /// Creates a new empty glyph file.
    /// </summary>
    public static GlyphFile CreateNew()
    {
        var file = new GlyphFile();
        for (int i = 0; i < CharactersPerSet; i++)
        {
            file._primarySet[i] = new CharacterGlyph();
            file._alternateSet[i] = new CharacterGlyph();
        }
        return file;
    }
    
    /// <summary>
    /// Loads a glyph file from disk.
    /// </summary>
    public static GlyphFile Load(string path)
    {
        var data = File.ReadAllBytes(path);
        if (data.Length != FileSize)
            throw new InvalidDataException($"Glyph file must be exactly {FileSize} bytes.");
        
        var file = new GlyphFile { FilePath = path };
        
        for (int i = 0; i < CharactersPerSet; i++)
        {
            file._primarySet[i] = new CharacterGlyph();
            file._primarySet[i].CopyFrom(data. AsSpan(i * BytesPerCharacter, BytesPerCharacter));
            
            file._alternateSet[i] = new CharacterGlyph();
            file._alternateSet[i].CopyFrom(data.AsSpan(CharacterSetSize + i * BytesPerCharacter, BytesPerCharacter));
        }
        
        return file;
    }
    
    /// <summary>
    /// Saves the glyph file to disk.
    /// </summary>
    public void Save(string?  path = null)
    {
        path ??= FilePath ??  throw new InvalidOperationException("No file path specified.");
        
        var data = new byte[FileSize];
        
        for (int i = 0; i < CharactersPerSet; i++)
        {
            _primarySet[i].CopyTo(data.AsSpan(i * BytesPerCharacter, BytesPerCharacter));
            _alternateSet[i].CopyTo(data.AsSpan(CharacterSetSize + i * BytesPerCharacter, BytesPerCharacter));
        }
        
        File.WriteAllBytes(path, data);
        FilePath = path;
        IsModified = false;
    }
    
    /// <summary>
    /// Gets the raw 4KB byte array for hot-loading.
    /// </summary>
    public byte[] ToByteArray()
    {
        var data = new byte[FileSize];
        for (int i = 0; i < CharactersPerSet; i++)
        {
            _primarySet[i].CopyTo(data.AsSpan(i * BytesPerCharacter, BytesPerCharacter));
            _alternateSet[i].CopyTo(data.AsSpan(CharacterSetSize + i * BytesPerCharacter, BytesPerCharacter));
        }
        return data;
    }
}
```

### 3.4 Undo/Redo History (Per-Character)

```csharp
/// <summary>
/// Manages undo/redo history for a single character glyph.
/// </summary>
public sealed class GlyphEditHistory
{
    private readonly Stack<byte[]> _undoStack = new();
    private readonly Stack<byte[]> _redoStack = new();
    private const int MaxHistoryDepth = 50;
    
    /// <summary>
    /// Gets whether undo is available.
    /// </summary>
    public bool CanUndo => _undoStack.Count > 0;
    
    /// <summary>
    /// Gets whether redo is available. 
    /// </summary>
    public bool CanRedo => _redoStack.Count > 0;
    
    /// <summary>
    /// Records the current state before a modification.
    /// </summary>
    /// <param name="glyph">The glyph about to be modified.</param>
    public void RecordState(CharacterGlyph glyph)
    {
        var state = new byte[8];
        glyph.CopyTo(state);
        _undoStack. Push(state);
        _redoStack.Clear();
        
        // Limit history depth
        while (_undoStack.Count > MaxHistoryDepth)
        {
            // Remove oldest entry (inefficient, but acceptable for small stacks)
            var temp = _undoStack.ToArray();
            _undoStack.Clear();
            for (int i = 0; i < temp.Length - 1; i++)
                _undoStack.Push(temp[temp.Length - 2 - i]);
        }
    }
    
    /// <summary>
    /// Undoes the last modification.
    /// </summary>
    /// <param name="glyph">The glyph to restore.</param>
    /// <returns>True if undo was performed. </returns>
    public bool Undo(CharacterGlyph glyph)
    {
        if (_undoStack.Count == 0)
            return false;
        
        // Save current state for redo
        var currentState = new byte[8];
        glyph.CopyTo(currentState);
        _redoStack.Push(currentState);
        
        // Restore previous state
        var previousState = _undoStack.Pop();
        glyph.CopyFrom(previousState);
        
        return true;
    }
    
    /// <summary>
    /// Redoes the last undone modification. 
    /// </summary>
    /// <param name="glyph">The glyph to restore. </param>
    /// <returns>True if redo was performed.</returns>
    public bool Redo(CharacterGlyph glyph)
    {
        if (_redoStack.Count == 0)
            return false;
        
        // Save current state for undo
        var currentState = new byte[8];
        glyph.CopyTo(currentState);
        _undoStack.Push(currentState);
        
        // Restore redo state
        var redoState = _redoStack.Pop();
        glyph.CopyFrom(redoState);
        
        return true;
    }
    
    /// <summary>
    /// Clears all history.
    /// </summary>
    public void Clear()
    {
        _undoStack.Clear();
        _redoStack.Clear();
    }
}
```

---

## 4. Emulator Integration

### 4.1 IGlyphHotLoader Interface

This interface is implemented by `CharacterDevice` to support hot-loading from the editor:

```csharp
/// <summary>
/// Interface for devices that support hot-loading of glyph data from external tools.
/// Separate from ICharacterRomProvider to keep the runtime rendering interface clean.
/// </summary>
public interface IGlyphHotLoader
{
    /// <summary>
    /// Hot-loads a complete 4KB glyph file into the device.
    /// </summary>
    /// <param name="glyphData">4KB glyph data (two 2KB character sets).</param>
    /// <param name="target">Whether to load into ROM (permanent) or RAM (overlay).</param>
    void HotLoadGlyphData(ReadOnlySpan<byte> glyphData, GlyphLoadTarget target);
    
    /// <summary>
    /// Hot-loads a single character's bitmap. 
    /// </summary>
    /// <param name="charCode">Character code (0-255).</param>
    /// <param name="scanlines">8 bytes of scanline data.</param>
    /// <param name="useAltCharSet">Target primary or alternate set.</param>
    /// <param name="target">Whether to load into ROM or RAM.</param>
    void HotLoadCharacter(
        byte charCode, 
        ReadOnlySpan<byte> scanlines,
        bool useAltCharSet, 
        GlyphLoadTarget target);
    
    /// <summary>
    /// Gets the current glyph data for the editor to read back.
    /// </summary>
    /// <param name="target">Whether to read from ROM or RAM.</param>
    /// <returns>4KB glyph data.</returns>
    byte[] GetGlyphData(GlyphLoadTarget target);
    
    /// <summary>
    /// Event raised when character ROM/RAM data changes.
    /// The video renderer should subscribe to this to refresh the glyph cache.
    /// </summary>
    event EventHandler<GlyphDataChangedEventArgs>? GlyphDataChanged;
}

/// <summary>
/// Target location for hot-loaded glyph data.
/// </summary>
public enum GlyphLoadTarget
{
    /// <summary>Load into character ROM (replaces base glyphs).</summary>
    Rom,
    
    /// <summary>Load into glyph RAM bank 1 (overlay for primary set).</summary>
    GlyphRamBank1,
    
    /// <summary>Load into glyph RAM bank 2 (overlay for alternate set).</summary>
    GlyphRamBank2
}

/// <summary>
/// Event arguments for glyph data changes. 
/// </summary>
public sealed class GlyphDataChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the target that was modified.
    /// </summary>
    public GlyphLoadTarget Target { get; init; }
    
    /// <summary>
    /// Gets the specific character code that changed, or null if the entire set changed.
    /// </summary>
    public byte? CharacterCode { get; init; }
    
    /// <summary>
    /// Gets whether this affects the alternate character set.
    /// </summary>
    public bool IsAlternateSet { get; init; }
}
```

### 4.2 CharacterDevice Implementation (partial)

```csharp
public sealed partial class CharacterDevice : ICharacterDevice, IGlyphHotLoader
{
    /// <inheritdoc />
    public event EventHandler<GlyphDataChangedEventArgs>? GlyphDataChanged;
    
    /// <inheritdoc />
    public void HotLoadGlyphData(ReadOnlySpan<byte> glyphData, GlyphLoadTarget target)
    {
        if (glyphData.Length != CharacterRomSize)
            throw new ArgumentException($"Glyph data must be exactly {CharacterRomSize} bytes.");
        
        switch (target)
        {
            case GlyphLoadTarget.Rom:
                glyphData.CopyTo(characterRom. Span);
                break;
            case GlyphLoadTarget.GlyphRamBank1:
                glyphData[.. CharacterSetSize].CopyTo(glyphRam. Span);
                break;
            case GlyphLoadTarget. GlyphRamBank2:
                glyphData[CharacterSetSize..].CopyTo(glyphRam.Span[CharacterSetSize..]);
                break;
        }
        
        OnGlyphDataChanged(new GlyphDataChangedEventArgs 
        { 
            Target = target,
            CharacterCode = null,
            IsAlternateSet = target == GlyphLoadTarget.GlyphRamBank2
        });
    }
    
    /// <inheritdoc />
    public void HotLoadCharacter(
        byte charCode, 
        ReadOnlySpan<byte> scanlines,
        bool useAltCharSet, 
        GlyphLoadTarget target)
    {
        if (scanlines. Length < 8)
            throw new ArgumentException("Scanlines must contain at least 8 bytes.");
        
        int offset = (useAltCharSet ? CharacterSetSize : 0) + (charCode * 8);
        var targetMemory = target == GlyphLoadTarget.Rom ? characterRom :  glyphRam;
        
        scanlines[.. 8].CopyTo(targetMemory.Span[offset..]);
        
        OnGlyphDataChanged(new GlyphDataChangedEventArgs
        {
            Target = target,
            CharacterCode = charCode,
            IsAlternateSet = useAltCharSet
        });
    }
    
    /// <inheritdoc />
    public byte[] GetGlyphData(GlyphLoadTarget target)
    {
        var result = new byte[CharacterRomSize];
        var source = target == GlyphLoadTarget.Rom ? characterRom : glyphRam;
        source. Span.CopyTo(result);
        return result;
    }
    
    private void OnGlyphDataChanged(GlyphDataChangedEventArgs e)
    {
        GlyphDataChanged?.Invoke(this, e);
    }
}
```

### 4.3 Emulator Connection Service

```csharp
/// <summary>
/// Service for connecting the glyph editor to a running emulator instance.
/// </summary>
public interface IEmulatorConnection
{
    /// <summary>
    /// Gets whether a connection to an emulator is active.
    /// </summary>
    bool IsConnected { get; }
    
    /// <summary>
    /// Gets the connected machine, or null if not connected.
    /// </summary>
    IMachine?  Machine { get; }
    
    /// <summary>
    /// Attempts to connect to a running emulator instance.
    /// </summary>
    /// <param name="debugContext">The debug context from the emulator.</param>
    /// <returns>True if connection succeeded.</returns>
    bool Connect(IDebugContext debugContext);
    
    /// <summary>
    /// Disconnects from the emulator.
    /// </summary>
    void Disconnect();
    
    /// <summary>
    /// Gets the character device from the connected machine.
    /// </summary>
    /// <returns>The character device, or null if not available.</returns>
    IGlyphHotLoader?  GetGlyphHotLoader();
    
    /// <summary>
    /// Hot-loads glyph data to the connected emulator.
    /// </summary>
    /// <param name="glyphData">4KB glyph data. </param>
    /// <param name="target">Target location.</param>
    /// <returns>True if successful.</returns>
    bool HotLoad(byte[] glyphData, GlyphLoadTarget target);
    
    /// <summary>
    /// Event raised when connection state changes.
    /// </summary>
    event EventHandler<bool>? ConnectionStateChanged;
}

/// <summary>
/// Implementation of emulator connection service.
/// </summary>
public sealed class EmulatorConnectionService :  IEmulatorConnection
{
    private IDebugContext?  _debugContext;
    
    public bool IsConnected => _debugContext?. Machine != null;
    
    public IMachine? Machine => _debugContext?.Machine;
    
    public event EventHandler<bool>? ConnectionStateChanged;
    
    public bool Connect(IDebugContext debugContext)
    {
        if (debugContext. Machine == null)
            return false;
        
        _debugContext = debugContext;
        ConnectionStateChanged?.Invoke(this, true);
        return true;
    }
    
    public void Disconnect()
    {
        _debugContext = null;
        ConnectionStateChanged?.Invoke(this, false);
    }
    
    public IGlyphHotLoader? GetGlyphHotLoader()
    {
        if (_debugContext?. Machine is not { } machine)
            return null;
        
        // Get CharacterDevice from machine's device registry
        // This depends on how devices are registered in the machine
        return machine.GetDevice<IGlyphHotLoader>("character-generator");
    }
    
    public bool HotLoad(byte[] glyphData, GlyphLoadTarget target)
    {
        var hotLoader = GetGlyphHotLoader();
        if (hotLoader == null)
            return false;
        
        try
        {
            hotLoader. HotLoadGlyphData(glyphData, target);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
```

---

## 5. View Models

### 5.1 Main Editor ViewModel

```csharp
/// <summary>
/// Main view model for the glyph editor window.
/// </summary>
public sealed partial class GlyphEditorViewModel :  ViewModelBase
{
    private readonly IGlyphFileService _fileService;
    private readonly IEmulatorConnection _emulatorConnection;
    private readonly Dictionary<(byte charCode, bool isAlt), GlyphEditHistory> _historyMap = new();
    
    [ObservableProperty]
    private GlyphFile? _currentFile;
    
    [ObservableProperty]
    private bool _useAlternateSet;
    
    [ObservableProperty]
    private byte _selectedCharCode;
    
    [ObservableProperty]
    private CharacterGlyph?  _selectedGlyph;
    
    [ObservableProperty]
    private bool _showGrid = true;
    
    [ObservableProperty]
    private bool _flashPreviewEnabled;
    
    [ObservableProperty]
    private bool _isFlashOn;
    
    [ObservableProperty]
    private int _gridZoomLevel = 2;
    
    [ObservableProperty]
    private string _windowTitle = "Character Glyph Editor";
    
    [ObservableProperty]
    private string _statusText = "Ready";
    
    [ObservableProperty]
    private bool _isEmulatorConnected;
    
    [ObservableProperty]
    private GlyphLoadTarget _hotLoadTarget = GlyphLoadTarget.Rom;
    
    // Selection state for copy/paste
    private readonly HashSet<byte> _selectedCharCodes = new();
    private List<CharacterGlyph>? _clipboard;
    private bool _clipboardIsFromAltSet;
    
    public GlyphEditorViewModel(
        IGlyphFileService fileService,
        IEmulatorConnection emulatorConnection)
    {
        _fileService = fileService;
        _emulatorConnection = emulatorConnection;
        
        _emulatorConnection.ConnectionStateChanged += (_, connected) =>
        {
            IsEmulatorConnected = connected;
        };
        
        // Start flash timer if preview enabled
        SetupFlashTimer();
    }
    
    /// <summary>
    /// Gets the display mode for the selected character code.
    /// </summary>
    public string DisplayMode => SelectedCharCode switch
    {
        < 0x40 => "Inverse",
        < 0x80 => UseAlternateSet ? "MouseText" : "Flashing",
        _ => "Normal"
    };
    
    /// <summary>
    /// Gets the printable character representation.
    /// </summary>
    public string CharacterDisplay
    {
        get
        {
            char c = (char)(SelectedCharCode & 0x7F);
            if (c < 0x20 || c == 0x7F)
                return $"${SelectedCharCode:X2}";
            return $"${SelectedCharCode:X2} '{c}'";
        }
    }
    
    /// <summary>
    /// Gets the undo/redo history for the currently selected character.
    /// </summary>
    private GlyphEditHistory GetCurrentHistory()
    {
        var key = (SelectedCharCode, UseAlternateSet);
        if (!_historyMap.TryGetValue(key, out var history))
        {
            history = new GlyphEditHistory();
            _historyMap[key] = history;
        }
        return history;
    }
    
    // ─── File Operations ────────────────────────────────────────────────
    
    [RelayCommand]
    private async Task NewFileAsync()
    {
        if (! await ConfirmDiscardChangesAsync())
            return;
        
        CurrentFile = GlyphFile.CreateNew();
        _historyMap.Clear();
        SelectedCharCode = 0;
        UpdateSelectedGlyph();
        UpdateWindowTitle();
        StatusText = "New file created";
    }
    
    [RelayCommand]
    private async Task OpenFileAsync()
    {
        if (!await ConfirmDiscardChangesAsync())
            return;
        
        var path = await _fileService.ShowOpenDialogAsync();
        if (path == null)
            return;
        
        try
        {
            CurrentFile = GlyphFile.Load(path);
            _historyMap.Clear();
            SelectedCharCode = 0;
            UpdateSelectedGlyph();
            UpdateWindowTitle();
            StatusText = $"Opened:  {Path.GetFileName(path)}";
        }
        catch (Exception ex)
        {
            await ShowErrorAsync($"Failed to open file: {ex.Message}");
        }
    }
    
    [RelayCommand]
    private async Task SaveFileAsync()
    {
        if (CurrentFile == null)
            return;
        
        if (CurrentFile.FilePath == null)
        {
            await SaveFileAsAsync();
            return;
        }
        
        try
        {
            CurrentFile.Save();
            UpdateWindowTitle();
            StatusText = $"Saved: {Path. GetFileName(CurrentFile.FilePath)}";
        }
        catch (Exception ex)
        {
            await ShowErrorAsync($"Failed to save file: {ex.Message}");
        }
    }
    
    [RelayCommand]
    private async Task SaveFileAsAsync()
    {
        if (CurrentFile == null)
            return;
        
        var path = await _fileService.ShowSaveDialogAsync();
        if (path == null)
            return;
        
        try
        {
            CurrentFile.Save(path);
            UpdateWindowTitle();
            StatusText = $"Saved: {Path. GetFileName(path)}";
        }
        catch (Exception ex)
        {
            await ShowErrorAsync($"Failed to save file: {ex.Message}");
        }
    }
    
    [RelayCommand]
    private async Task ImportFromRomAsync()
    {
        var path = await _fileService.ShowOpenRomDialogAsync();
        if (path == null)
            return;
        
        try
        {
            var romData = await File.ReadAllBytesAsync(path);
            
            // Apple II character ROMs are typically 2KB or 4KB
            if (romData.Length == 2048)
            {
                // Single character set - load into primary
                if (CurrentFile == null)
                    CurrentFile = GlyphFile. CreateNew();
                
                for (int i = 0; i < 256; i++)
                {
                    CurrentFile.GetGlyph((byte)i, false).CopyFrom(romData. AsSpan(i * 8, 8));
                }
                CurrentFile. MarkModified();
            }
            else if (romData.Length == 4096)
            {
                // Full character ROM - load both sets
                CurrentFile = GlyphFile.CreateNew();
                for (int i = 0; i < 256; i++)
                {
                    CurrentFile.GetGlyph((byte)i, false).CopyFrom(romData.AsSpan(i * 8, 8));
                    CurrentFile.GetGlyph((byte)i, true).CopyFrom(romData.AsSpan(2048 + i * 8, 8));
                }
                CurrentFile.MarkModified();
            }
            else
            {
                await ShowErrorAsync($"Unexpected ROM size: {romData. Length} bytes.  Expected 2048 or 4096.");
                return;
            }
            
            _historyMap.Clear();
            UpdateSelectedGlyph();
            UpdateWindowTitle();
            StatusText = $"Imported from:  {Path.GetFileName(path)}";
        }
        catch (Exception ex)
        {
            await ShowErrorAsync($"Failed to import ROM: {ex.Message}");
        }
    }
    
    [RelayCommand]
    private async Task ExportPreviewImageAsync()
    {
        if (CurrentFile == null)
            return;
        
        var path = await _fileService.ShowSaveImageDialogAsync();
        if (path == null)
            return;
        
        try
        {
            await _fileService.ExportCharacterSetPreview(CurrentFile, UseAlternateSet, path);
            StatusText = $"Exported preview:  {Path.GetFileName(path)}";
        }
        catch (Exception ex)
        {
            await ShowErrorAsync($"Failed to export preview: {ex.Message}");
        }
    }
    
    // ─── Edit Operations ────────────────────────────────────────────────
    
    [RelayCommand(CanExecute = nameof(CanUndo))]
    private void Undo()
    {
        if (SelectedGlyph == null || CurrentFile == null)
            return;
        
        var history = GetCurrentHistory();
        if (history. Undo(SelectedGlyph))
        {
            CurrentFile. MarkModified();
            OnPropertyChanged(nameof(SelectedGlyph));
            UpdateWindowTitle();
        }
    }
    
    private bool CanUndo() => GetCurrentHistory().CanUndo;
    
    [RelayCommand(CanExecute = nameof(CanRedo))]
    private void Redo()
    {
        if (SelectedGlyph == null || CurrentFile == null)
            return;
        
        var history = GetCurrentHistory();
        if (history. Redo(SelectedGlyph))
        {
            CurrentFile.MarkModified();
            OnPropertyChanged(nameof(SelectedGlyph));
            UpdateWindowTitle();
        }
    }
    
    private bool CanRedo() => GetCurrentHistory().CanRedo;
    
    [RelayCommand]
    private void CopySelectedGlyphs()
    {
        if (CurrentFile == null || _selectedCharCodes.Count == 0)
            return;
        
        _clipboard = _selectedCharCodes
            .OrderBy(c => c)
            .Select(c => CurrentFile.GetGlyph(c, UseAlternateSet).Clone())
            .ToList();
        _clipboardIsFromAltSet = UseAlternateSet;
        
        StatusText = $"Copied {_clipboard.Count} character(s)";
    }
    
    [RelayCommand(CanExecute = nameof(CanPaste))]
    private void PasteGlyphs()
    {
        if (CurrentFile == null || _clipboard == null || _clipboard.Count == 0)
            return;
        
        // Paste starting from currently selected character
        int targetCode = SelectedCharCode;
        int pasteCount = 0;
        
        foreach (var glyph in _clipboard)
        {
            if (targetCode > 255)
                break;
            
            var targetGlyph = CurrentFile.GetGlyph((byte)targetCode, UseAlternateSet);
            var history = GetHistoryFor((byte)targetCode, UseAlternateSet);
            history.RecordState(targetGlyph);
            
            Array.Copy(glyph. Scanlines, targetGlyph.Scanlines, 8);
            targetCode++;
            pasteCount++;
        }
        
        CurrentFile.MarkModified();
        UpdateSelectedGlyph();
        UpdateWindowTitle();
        StatusText = $"Pasted {pasteCount} character(s)";
    }
    
    private bool CanPaste() => _clipboard != null && _clipboard.Count > 0;
    
    // ─── Glyph Editing Tools ────────────────────────────────────────────
    
    [RelayCommand]
    private void InvertGlyph()
    {
        if (SelectedGlyph == null || CurrentFile == null)
            return;
        
        RecordCurrentState();
        
        for (int y = 0; y < 8; y++)
        {
            SelectedGlyph.Scanlines[y] = (byte)(~SelectedGlyph.Scanlines[y] & 0x7F);
        }
        
        CurrentFile.MarkModified();
        OnPropertyChanged(nameof(SelectedGlyph));
        UpdateWindowTitle();
    }
    
    [RelayCommand]
    private void ClearGlyph()
    {
        if (SelectedGlyph == null || CurrentFile == null)
            return;
        
        RecordCurrentState();
        Array.Clear(SelectedGlyph.Scanlines);
        
        CurrentFile.MarkModified();
        OnPropertyChanged(nameof(SelectedGlyph));
        UpdateWindowTitle();
    }
    
    [RelayCommand]
    private void FillGlyph()
    {
        if (SelectedGlyph == null || CurrentFile == null)
            return;
        
        RecordCurrentState();
        Array.Fill(SelectedGlyph. Scanlines, (byte)0x7F);
        
        CurrentFile.MarkModified();
        OnPropertyChanged(nameof(SelectedGlyph));
        UpdateWindowTitle();
    }
    
    [RelayCommand]
    private void FlipHorizontal()
    {
        if (SelectedGlyph == null || CurrentFile == null)
            return;
        
        RecordCurrentState();
        
        for (int y = 0; y < 8; y++)
        {
            byte original = SelectedGlyph. Scanlines[y];
            byte flipped = 0;
            
            for (int x = 0; x < 7; x++)
            {
                if ((original & (1 << x)) != 0)
                    flipped |= (byte)(1 << (6 - x));
            }
            
            SelectedGlyph.Scanlines[y] = flipped;
        }
        
        CurrentFile.MarkModified();
        OnPropertyChanged(nameof(SelectedGlyph));
        UpdateWindowTitle();
    }
    
    [RelayCommand]
    private void FlipVertical()
    {
        if (SelectedGlyph == null || CurrentFile == null)
            return;
        
        RecordCurrentState();
        Array.Reverse(SelectedGlyph.Scanlines);
        
        CurrentFile.MarkModified();
        OnPropertyChanged(nameof(SelectedGlyph));
        UpdateWindowTitle();
    }
    
    [RelayCommand]
    private void ShiftUp()
    {
        if (SelectedGlyph == null || CurrentFile == null)
            return;
        
        RecordCurrentState();
        
        byte top = SelectedGlyph. Scanlines[0];
        for (int y = 0; y < 7; y++)
            SelectedGlyph.Scanlines[y] = SelectedGlyph.Scanlines[y + 1];
        SelectedGlyph.Scanlines[7] = top; // Wrap around
        
        CurrentFile. MarkModified();
        OnPropertyChanged(nameof(SelectedGlyph));
        UpdateWindowTitle();
    }
    
    [RelayCommand]
    private void ShiftDown()
    {
        if (SelectedGlyph == null || CurrentFile == null)
            return;
        
        RecordCurrentState();
        
        byte bottom = SelectedGlyph. Scanlines[7];
        for (int y = 7; y > 0; y--)
            SelectedGlyph. Scanlines[y] = SelectedGlyph.Scanlines[y - 1];
        SelectedGlyph.Scanlines[0] = bottom; // Wrap around
        
        CurrentFile.MarkModified();
        OnPropertyChanged(nameof(SelectedGlyph));
        UpdateWindowTitle();
    }
    
    [RelayCommand]
    private void ShiftLeft()
    {
        if (SelectedGlyph == null || CurrentFile == null)
            return;
        
        RecordCurrentState();
        
        for (int y = 0; y < 8; y++)
        {
            byte row = SelectedGlyph. Scanlines[y];
            bool leftBit = (row & 0x40) != 0;
            row = (byte)((row << 1) & 0x7F);
            if (leftBit) row |= 0x01; // Wrap around
            SelectedGlyph.Scanlines[y] = row;
        }
        
        CurrentFile.MarkModified();
        OnPropertyChanged(nameof(SelectedGlyph));
        UpdateWindowTitle();
    }
    
    [RelayCommand]
    private void ShiftRight()
    {
        if (SelectedGlyph == null || CurrentFile == null)
            return;
        
        RecordCurrentState();
        
        for (int y = 0; y < 8; y++)
        {
            byte row = SelectedGlyph.Scanlines[y];
            bool rightBit = (row & 0x01) != 0;
            row = (byte)(row >> 1);
            if (rightBit) row |= 0x40; // Wrap around
            SelectedGlyph.Scanlines[y] = row;
        }
        
        CurrentFile. MarkModified();
        OnPropertyChanged(nameof(SelectedGlyph));
        UpdateWindowTitle();
    }
    
    [RelayCommand]
    private void RotateClockwise()
    {
        if (SelectedGlyph == null || CurrentFile == null)
            return;
        
        RecordCurrentState();
        
        // For a 7×8 glyph, rotation is approximate (not square)
        // We rotate the 7×7 portion and leave the bottom row
        var original = new byte[8];
        Array.Copy(SelectedGlyph.Scanlines, original, 8);
        Array.Clear(SelectedGlyph. Scanlines);
        
        for (int y = 0; y < 7; y++)
        {
            for (int x = 0; x < 7; x++)
            {
                bool pixel = (original[y] & (1 << (6 - x))) != 0;
                if (pixel)
                {
                    // New position:  (6-y, x)
                    int newX = 6 - y;
                    int newY = x;
                    if (newY < 8)
                        SelectedGlyph.Scanlines[newY] |= (byte)(1 << (6 - newX));
                }
            }
        }
        
        CurrentFile.MarkModified();
        OnPropertyChanged(nameof(SelectedGlyph));
        UpdateWindowTitle();
    }
    
    [RelayCommand]
    private void RotateCounterClockwise()
    {
        if (SelectedGlyph == null || CurrentFile == null)
            return;
        
        RecordCurrentState();
        
        var original = new byte[8];
        Array.Copy(SelectedGlyph.Scanlines, original, 8);
        Array.Clear(SelectedGlyph. Scanlines);
        
        for (int y = 0; y < 7; y++)
        {
            for (int x = 0; x < 7; x++)
            {
                bool pixel = (original[y] & (1 << (6 - x))) != 0;
                if (pixel)
                {
                    // New position: (y, 6-x)
                    int newX = y;
                    int newY = 6 - x;
                    if (newY < 8 && newX < 7)
                        SelectedGlyph.Scanlines[newY] |= (byte)(1 << (6 - newX));
                }
            }
        }
        
        CurrentFile. MarkModified();
        OnPropertyChanged(nameof(SelectedGlyph));
        UpdateWindowTitle();
    }
    
    // ─── Emulator Integration ───────────────────────────────────────────
    
    [RelayCommand(CanExecute = nameof(CanHotLoad))]
    private void HotLoadToEmulator()
    {
        if (CurrentFile == null || !IsEmulatorConnected)
            return;
        
        var data = CurrentFile.ToByteArray();
        if (_emulatorConnection. HotLoad(data, HotLoadTarget))
        {
            StatusText = $"Hot-loaded to emulator ({HotLoadTarget})";
        }
        else
        {
            StatusText = "Failed to hot-load to emulator";
        }
    }
    
    private bool CanHotLoad() => CurrentFile != null && IsEmulatorConnected;
    
    [RelayCommand]
    private async Task ConnectToEmulatorAsync()
    {
        // This would typically show a dialog to select an emulator instance
        // For now, we assume there's a way to get the debug context
        StatusText = "Connecting to emulator...";
        
        // Implementation depends on how the editor is launched and
        // how it communicates with emulator instances
    }
    
    // ─── Helper Methods ─────────────────────────────────────────────────
    
    private void RecordCurrentState()
    {
        if (SelectedGlyph != null)
            GetCurrentHistory().RecordState(SelectedGlyph);
    }
    
    private GlyphEditHistory GetHistoryFor(byte charCode, bool isAlt)
    {
        var key = (charCode, isAlt);
        if (!_historyMap.TryGetValue(key, out var history))
        {
            history = new GlyphEditHistory();
            _historyMap[key] = history;
        }
        return history;
    }
    
    private void UpdateSelectedGlyph()
    {
        if (CurrentFile != null)
            SelectedGlyph = CurrentFile. GetGlyph(SelectedCharCode, UseAlternateSet);
        else
            SelectedGlyph = null;
        
        OnPropertyChanged(nameof(DisplayMode));
        OnPropertyChanged(nameof(CharacterDisplay));
        UndoCommand.NotifyCanExecuteChanged();
        RedoCommand.NotifyCanExecuteChanged();
    }
    
    private void UpdateWindowTitle()
    {
        var fileName = CurrentFile?.FilePath != null 
            ? Path.GetFileName(CurrentFile.FilePath) 
            : "Untitled";
        var modified = CurrentFile?.IsModified == true ?  " *" : "";
        WindowTitle = $"{fileName}{modified} - Character Glyph Editor";
    }
    
    partial void OnSelectedCharCodeChanged(byte value)
    {
        UpdateSelectedGlyph();
    }
    
    partial void OnUseAlternateSetChanged(bool value)
    {
        UpdateSelectedGlyph();
    }
    
    private async Task<bool> ConfirmDiscardChangesAsync()
    {
        if (CurrentFile?. IsModified != true)
            return true;
        
        return await _fileService.ShowConfirmDiscardDialogAsync();
    }
    
    private async Task ShowErrorAsync(string message)
    {
        await _fileService. ShowErrorDialogAsync(message);
    }
    
    // ─── Flash Preview Timer ────────────────────────────────────────────
    
    private DispatcherTimer?  _flashTimer;
    
    private void SetupFlashTimer()
    {
        _flashTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(267) // ~3. 75 Hz (Apple II flash rate)
        };
        _flashTimer.Tick += (_, _) =>
        {
            if (FlashPreviewEnabled)
                IsFlashOn = !IsFlashOn;
        };
    }
    
    partial void OnFlashPreviewEnabledChanged(bool value)
    {
        if (value)
            _flashTimer?.Start();
        else
        {
            _flashTimer?.Stop();
            IsFlashOn = false;
        }
    }
}
```
