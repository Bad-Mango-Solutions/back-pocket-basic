# Display Rendering Integration Specification

## Document Information

| Field        | Value                                             |
|--------------|---------------------------------------------------|
| Version      | 1.0                                               |
| Date         | 2025-12-28                                        |
| Status       | Initial Draft                                     |
| Applies To   | All BackPocket emulator personalities             |
| Related Docs | Apple II Video Display Specification              |
|              | Apple IIgs Video Display Specification            |
|              | Emulator UI Specification                         |
|              | Architecture Spec v1.0                            |

---

## 1. Overview

This specification describes how to integrate the emulator's video display output with the
host user interface. It bridges the gap between the guest video subsystem (which produces
pixel data based on guest memory and soft switch state) and the host display framework
(Avalonia UI with SkiaSharp rendering).

### 1.1 Purpose

The display rendering integration layer is responsible for:

1. **Frame buffer management** – Allocating and managing pixel buffers for rendering
2. **Mode detection** – Determining the current guest display mode and parameters
3. **Pixel rendering** – Converting guest video memory to host-compatible pixels
4. **Synchronization** – Coordinating rendering with guest VBlank timing
5. **Host presentation** – Delivering frames to the Avalonia UI for display
6. **Post-processing** – Applying optional visual effects (scanlines, CRT simulation)

### 1.2 Architecture Overview

```
???????????????????????????????????????????????????????????????????????????????
?                              Emulator Core                                  ?
?                                                                             ?
?  ???????????????????????????????????????????????????????????????????????   ?
?  ?                         Video Controller                             ?   ?
?  ?                                                                      ?   ?
?  ?  – IVideoController (IIe/IIc)                                        ?   ?
?  ?  – IIgsVideoController (IIgs)                                        ?   ?
?  ?                                                                      ?   ?
?  ?  Responsibilities:                                                   ?   ?
?  ?  – Track soft switch state (text/graphics, page, 80col, etc.)       ?   ?
?  ?  – Generate VBlank events                                            ?   ?
?  ?  – Provide mode queries                                              ?   ?
?  ?                                                                      ?   ?
?  ???????????????????????????????????????????????????????????????????????   ?
?                                     ?                                       ?
?  ???????????????????????????????????????????????????????????????????????   ?
?  ?                          Memory Bus                                  ?   ?
?  ?                                                                      ?   ?
?  ?  – IMemoryBus with DMA-style reads                                   ?   ?
?  ?  – Side-effect-free access for video rendering                       ?   ?
?  ?                                                                      ?   ?
?  ???????????????????????????????????????????????????????????????????????   ?
???????????????????????????????????????????????????????????????????????????????
                                      ?
                                      ?
???????????????????????????????????????????????????????????????????????????????
?                      Display Rendering Integration Layer                    ?
?                                                                             ?
?  ???????????????????????????????????????????????????????????????????????   ?
?  ?                        IDisplayService                               ?   ?
?  ?                                                                      ?   ?
?  ?  – AttachMachine(machine) – Connect to emulator instance            ?   ?
?  ?  – RenderFrame(buffer) – Generate frame from current state          ?   ?
?  ?  – Palette/Scale/Effects properties                                  ?   ?
?  ?                                                                      ?   ?
?  ???????????????????????????????????????????????????????????????????????   ?
?                                     ?                                       ?
?  ???????????????????????????????????????????????????????????????????????   ?
?  ?                      Display Renderer                                ?   ?
?  ?                                                                      ?   ?
?  ?  ???????????????????  ???????????????????  ???????????????????????  ?   ?
?  ?  ? TextRenderer    ?  ? GraphicsRenderer?  ? ShrRenderer         ?  ?   ?
?  ?  ?                 ?  ?                 ?  ?                     ?  ?   ?
?  ?  ? – 40/80 column  ?  ? – Lo-Res        ?  ? – 320/640 mode     ?  ?   ?
?  ?  ? – Flash timing  ?  ? – Hi-Res        ?  ? – Per-line palette ?  ?   ?
?  ?  ? – MouseText     ?  ? – Double Hi-Res ?  ? – Fill mode        ?  ?   ?
?  ?  ???????????????????  ???????????????????  ???????????????????????  ?   ?
?  ?                                                                      ?   ?
?  ???????????????????????????????????????????????????????????????????????   ?
?                                     ?                                       ?
?  ???????????????????????????????????????????????????????????????????????   ?
?  ?                     Frame Buffer Manager                             ?   ?
?  ?                                                                      ?   ?
?  ?  – Double buffering                                                  ?   ?
?  ?  – Dirty region tracking                                             ?   ?
?  ?  – Thread-safe buffer swap                                           ?   ?
?  ?                                                                      ?   ?
?  ???????????????????????????????????????????????????????????????????????   ?
???????????????????????????????????????????????????????????????????????????????
                                      ?
                                      ?
???????????????????????????????????????????????????????????????????????????????
?                           Host UI (Avalonia)                                ?
?                                                                             ?
?  ???????????????????????????????????????????????????????????????????????   ?
?  ?                      VideoDisplay Control                            ?   ?
?  ?                                                                      ?   ?
?  ?  – WriteableBitmap for frame presentation                           ?   ?
?  ?  – Render() called by Avalonia at UI refresh rate                   ?   ?
?  ?  – Scaling and interpolation options                                 ?   ?
?  ?  – Post-processing effects (scanlines, CRT)                         ?   ?
?  ?                                                                      ?   ?
?  ???????????????????????????????????????????????????????????????????????   ?
???????????????????????????????????????????????????????????????????????????????
```

---

## 2. Display Service Interface

### 2.1 IDisplayService Definition

The display service is the primary interface for connecting guest video to host display:

```csharp
/// <summary>
/// Manages video display rendering and host presentation.
/// </summary>
public interface IDisplayService
{
    /// <summary>Gets the current display mode.</summary>
    DisplayMode CurrentMode { get; }
    
    /// <summary>Gets the native resolution of the current mode.</summary>
    PixelSize NativeResolution { get; }
    
    /// <summary>Gets or sets the display scaling factor.</summary>
    double Scale { get; set; }
    
    /// <summary>Gets or sets the scaling mode.</summary>
    ScalingMode ScalingMode { get; set; }
    
    /// <summary>Gets or sets scanline effect intensity (0.0 = off, 1.0 = full).</summary>
    double ScanlineIntensity { get; set; }
    
    /// <summary>Gets or sets the color palette.</summary>
    ColorPalette Palette { get; set; }
    
    /// <summary>Gets or sets whether NTSC artifact coloring is enabled.</summary>
    bool NtscArtifactColoring { get; set; }
    
    /// <summary>Attaches to a machine's video output.</summary>
    /// <param name="machine">Machine to attach to.</param>
    void AttachMachine(IMachine machine);
    
    /// <summary>Detaches from current machine.</summary>
    void Detach();
    
    /// <summary>Gets the attached machine, if any.</summary>
    IMachine? AttachedMachine { get; }
    
    /// <summary>Renders current frame to the provided bitmap.</summary>
    /// <param name="target">Target bitmap for rendering.</param>
    /// <returns>True if a new frame was rendered.</returns>
    bool RenderFrame(WriteableBitmap target);
    
    /// <summary>Event raised when display mode changes.</summary>
    event EventHandler<DisplayModeChangedEventArgs>? ModeChanged;
    
    /// <summary>Event raised when a frame is ready for presentation.</summary>
    event EventHandler? FrameReady;
}

/// <summary>
/// Display mode enumeration covering all supported guest modes.
/// </summary>
public enum DisplayMode
{
    /// <summary>40-column text mode (280–192).</summary>
    Text40,
    
    /// <summary>80-column text mode (560–192).</summary>
    Text80,
    
    /// <summary>Lo-res graphics mode (40–48, displayed as 280–192).</summary>
    LoRes,
    
    /// <summary>Double lo-res graphics mode (80–48).</summary>
    DoubleLoRes,
    
    /// <summary>Hi-res graphics mode (280–192).</summary>
    HiRes,
    
    /// <summary>Double hi-res graphics mode (560–192).</summary>
    DoubleHiRes,
    
    /// <summary>Mixed mode (graphics with 4-line text window).</summary>
    Mixed,
    
    /// <summary>Super hi-res 320 mode (320–200, IIgs only).</summary>
    SuperHiRes320,
    
    /// <summary>Super hi-res 640 mode (640–200, IIgs only).</summary>
    SuperHiRes640
}

/// <summary>
/// Scaling mode for display output.
/// </summary>
public enum ScalingMode
{
    /// <summary>Integer scaling (1–, 2–, 3–, etc.).</summary>
    Integer,
    
    /// <summary>Maintain 4:3 aspect ratio.</summary>
    AspectCorrect,
    
    /// <summary>Fill the available space.</summary>
    Fill,
    
    /// <summary>No scaling (native resolution).</summary>
    Native
}

/// <summary>
/// Color palette options.
/// </summary>
public enum ColorPalette
{
    /// <summary>NTSC artifact colors (authentic Apple II look).</summary>
    Ntsc,
    
    /// <summary>Clean RGB colors (IIgs-style).</summary>
    Rgb,
    
    /// <summary>Green phosphor monochrome.</summary>
    Green,
    
    /// <summary>Amber phosphor monochrome.</summary>
    Amber,
    
    /// <summary>White phosphor monochrome.</summary>
    White,
    
    /// <summary>User-defined custom palette.</summary>
    Custom
}
```

### 2.2 Display Service Implementation

```csharp
/// <summary>
/// Default implementation of IDisplayService.
/// </summary>
public sealed class DisplayService : IDisplayService, IDisposable
{
    private IMachine? _machine;
    private IVideoController? _videoController;
    private IDisplayRenderer _renderer;
    private FrameBufferManager _bufferManager;
    private DisplayMode _lastMode;
    
    // Configuration
    public double Scale { get; set; } = 2.0;
    public ScalingMode ScalingMode { get; set; } = ScalingMode.Integer;
    public double ScanlineIntensity { get; set; } = 0.0;
    public ColorPalette Palette { get; set; } = ColorPalette.Ntsc;
    public bool NtscArtifactColoring { get; set; } = true;
    
    public DisplayMode CurrentMode { get; private set; } = DisplayMode.Text40;
    
    public PixelSize NativeResolution => CurrentMode switch
    {
        DisplayMode.Text40 or DisplayMode.LoRes or DisplayMode.HiRes 
            => new PixelSize(280, 192),
        DisplayMode.Text80 or DisplayMode.DoubleLoRes or DisplayMode.DoubleHiRes 
            => new PixelSize(560, 192),
        DisplayMode.SuperHiRes320 
            => new PixelSize(320, 200),
        DisplayMode.SuperHiRes640 
            => new PixelSize(640, 200),
        _ => new PixelSize(280, 192)
    };
    
    public IMachine? AttachedMachine => _machine;
    
    public event EventHandler<DisplayModeChangedEventArgs>? ModeChanged;
    public event EventHandler? FrameReady;
    
    public DisplayService()
    {
        _bufferManager = new FrameBufferManager(640, 400);
        _renderer = new AppleIIDisplayRenderer();
    }
    
    public void AttachMachine(IMachine machine)
    {
        Detach();
        
        _machine = machine;
        _videoController = machine.Video;
        
        // Select appropriate renderer
        if (_videoController is IIgsVideoController)
        {
            _renderer = new IIgsDisplayRenderer();
        }
        else
        {
            _renderer = new AppleIIDisplayRenderer();
        }
        
        // Subscribe to VBlank
        _videoController.VBlankStart += OnVBlank;
    }
    
    public void Detach()
    {
        if (_videoController != null)
        {
            _videoController.VBlankStart -= OnVBlank;
            _videoController = null;
        }
        _machine = null;
    }
    
    private void OnVBlank()
    {
        if (_machine == null || _videoController == null)
            return;
        
        // Detect mode change
        var newMode = DetectMode(_videoController);
        if (newMode != _lastMode)
        {
            _lastMode = newMode;
            CurrentMode = newMode;
            ModeChanged?.Invoke(this, new DisplayModeChangedEventArgs(newMode));
        }
        
        // Render frame
        _renderer.RenderFrame(_bufferManager.BackBuffer, _videoController, _machine.Bus);
        _bufferManager.Present();
        
        FrameReady?.Invoke(this, EventArgs.Empty);
    }
    
    public bool RenderFrame(WriteableBitmap target)
    {
        return _bufferManager.CopyToTarget(target);
    }
    
    private DisplayMode DetectMode(IVideoController video)
    {
        if (video is IIgsVideoController iigs && iigs.IsSuperHiResMode)
        {
            // Check first scanline for mode
            byte scb = iigs.GetScanlineControlByte(0);
            return (scb & 0x80) != 0 ? DisplayMode.SuperHiRes640 : DisplayMode.SuperHiRes320;
        }
        
        if (video.IsTextMode)
        {
            return video.Is80ColumnMode ? DisplayMode.Text80 : DisplayMode.Text40;
        }
        
        if (video.IsMixedMode)
        {
            return DisplayMode.Mixed;
        }
        
        if (video.IsHiResMode)
        {
            return video.IsDoubleHiResMode ? DisplayMode.DoubleHiRes : DisplayMode.HiRes;
        }
        
        return video.IsDoubleHiResMode ? DisplayMode.DoubleLoRes : DisplayMode.LoRes;
    }
    
    public void Dispose()
    {
        Detach();
        _bufferManager.Dispose();
    }
}
```

---

## 3. Frame Buffer Management

### 3.1 Buffer Dimensions and Format

| Personality   | Recommended Buffer Size | Notes                           |
|---------------|-------------------------|----------------------------------|
| Pocket2e/2c   | 560–384                 | 2– scale for all IIe modes       |
| PocketGS      | 640–400 or 640–432      | Native SHR width, 2– vertical    |
| All           | 32-bit ARGB (BGRA)      | Avalonia/SkiaSharp compatible    |

### 3.2 Double Buffering Implementation

```csharp
/// <summary>
/// Manages double-buffered frame rendering with thread safety.
/// </summary>
public sealed class FrameBufferManager : IDisposable
{
    private uint[] _writeBuffer;
    private uint[] _readBuffer;
    private readonly int _width;
    private readonly int _height;
    private readonly ReaderWriterLockSlim _lock = new();
    private bool _frameReady;
    
    /// <summary>Gets the frame buffer width.</summary>
    public int Width => _width;
    
    /// <summary>Gets the frame buffer height.</summary>
    public int Height => _height;
    
    /// <summary>
    /// Initializes a new frame buffer manager.
    /// </summary>
    /// <param name="width">Buffer width in pixels.</param>
    /// <param name="height">Buffer height in pixels.</param>
    public FrameBufferManager(int width, int height)
    {
        _width = width;
        _height = height;
        _writeBuffer = new uint[width * height];
        _readBuffer = new uint[width * height];
    }
    
    /// <summary>
    /// Gets the back buffer for rendering (emulator thread).
    /// </summary>
    public Span<uint> BackBuffer
    {
        get
        {
            _lock.EnterReadLock();
            try
            {
                return _writeBuffer.AsSpan();
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }
    }
    
    /// <summary>
    /// Marks the back buffer as ready and swaps buffers.
    /// Called by emulator thread after rendering.
    /// </summary>
    public void Present()
    {
        _lock.EnterWriteLock();
        try
        {
            (_writeBuffer, _readBuffer) = (_readBuffer, _writeBuffer);
            _frameReady = true;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }
    
    /// <summary>
    /// Copies the front buffer to the target bitmap.
    /// Called by UI thread for display.
    /// </summary>
    /// <param name="target">Target WriteableBitmap.</param>
    /// <returns>True if a new frame was copied.</returns>
    public bool CopyToTarget(WriteableBitmap target)
    {
        _lock.EnterUpgradeableReadLock();
        try
        {
            if (!_frameReady)
                return false;
            
            _lock.EnterWriteLock();
            try
            {
                _frameReady = false;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
            
            // Copy pixels to bitmap
            using var frame = target.Lock();
            unsafe
            {
                fixed (uint* src = _readBuffer)
                {
                    int byteCount = _readBuffer.Length * sizeof(uint);
                    Buffer.MemoryCopy(src, (void*)frame.Address, byteCount, byteCount);
                }
            }
            
            return true;
        }
        finally
        {
            _lock.ExitUpgradeableReadLock();
        }
    }
    
    /// <summary>
    /// Resizes the buffers (clears existing content).
    /// </summary>
    /// <param name="width">New width.</param>
    /// <param name="height">New height.</param>
    public void Resize(int width, int height)
    {
        _lock.EnterWriteLock();
        try
        {
            if (width * height != _writeBuffer.Length)
            {
                _writeBuffer = new uint[width * height];
                _readBuffer = new uint[width * height];
            }
            _frameReady = false;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }
    
    public void Dispose()
    {
        _lock.Dispose();
    }
}
```

### 3.3 Dirty Region Tracking (Optional Optimization)

For improved performance, track which regions of video memory have changed:

```csharp
/// <summary>
/// Tracks dirty regions for incremental rendering.
/// </summary>
public sealed class DirtyRegionTracker
{
    private readonly BitArray _textDirty = new(24 * 40);    // Per-character
    private readonly BitArray _loResDirty = new(24);        // Per-text-row (2 gr rows)
    private readonly BitArray _hiResDirty = new(192);       // Per-scanline
    private readonly BitArray _shrDirty = new(200);         // Per-scanline
    private bool _paletteDirty = true;
    private bool _modeDirty = true;
    
    /// <summary>
    /// Marks a memory address as written.
    /// </summary>
    public void MarkDirty(Addr address)
    {
        if (address >= 0x0400 && address < 0x0800)
        {
            // Text page 1
            int offset = (int)(address - 0x0400);
            _textDirty[offset] = true;
            _loResDirty[offset / 40] = true;
        }
        else if (address >= 0x2000 && address < 0x4000)
        {
            // Hi-res page 1 - determine scanline
            int line = HiResAddressToLine((int)address);
            if (line >= 0 && line < 192)
            {
                _hiResDirty[line] = true;
            }
        }
        else if (address >= 0xE12000 && address < 0xE1A000)
        {
            // Super hi-res (IIgs)
            int offset = (int)(address - 0xE12000);
            if (offset < 0x7D00)
            {
                // Pixel data
                int line = offset / 160;
                if (line < 200)
                {
                    _shrDirty[line] = true;
                }
            }
            else if (offset < 0x7E00)
            {
                // SCBs
                int line = offset - 0x7D00;
                if (line < 200)
                {
                    _shrDirty[line] = true;
                }
            }
            else
            {
                // Palettes
                _paletteDirty = true;
            }
        }
    }
    
    /// <summary>
    /// Marks that the display mode soft switches changed.
    /// </summary>
    public void MarkModeChanged()
    {
        _modeDirty = true;
    }
    
    /// <summary>
    /// Returns true if a full re-render is needed.
    /// </summary>
    public bool NeedsFullRender => _modeDirty || _paletteDirty;
    
    /// <summary>
    /// Gets dirty hi-res scanlines.
    /// </summary>
    public IEnumerable<int> GetDirtyHiResLines()
    {
        for (int i = 0; i < 192; i++)
        {
            if (_hiResDirty[i])
                yield return i;
        }
    }
    
    /// <summary>
    /// Clears all dirty flags after rendering.
    /// </summary>
    public void Clear()
    {
        _textDirty.SetAll(false);
        _loResDirty.SetAll(false);
        _hiResDirty.SetAll(false);
        _shrDirty.SetAll(false);
        _paletteDirty = false;
        _modeDirty = false;
    }
    
    private static int HiResAddressToLine(int address)
    {
        int offset = address - 0x2000;
        int block = offset / 1024;       // 0-7
        int blockOffset = offset % 1024;
        int subBlock = blockOffset / 128;
        int rowInGroup = blockOffset % 128 / 40;
        
        return (rowInGroup * 8) + block + (subBlock * 64);
    }
}
```

---

## 4. Display Renderer Interface

### 4.1 IDisplayRenderer Definition

```csharp
/// <summary>
/// Renders guest video memory to a frame buffer.
/// </summary>
public interface IDisplayRenderer
{
    /// <summary>
    /// Renders the current frame to the buffer.
    /// </summary>
    /// <param name="buffer">Target pixel buffer (ARGB format).</param>
    /// <param name="video">Video controller for mode state.</param>
    /// <param name="bus">Memory bus for reading video memory.</param>
    void RenderFrame(Span<uint> buffer, IVideoController video, IMemoryBus bus);
    
    /// <summary>Gets or sets the color palette.</summary>
    ColorPalette Palette { get; set; }
    
    /// <summary>Gets or sets the custom color palette (when Palette == Custom).</summary>
    IReadOnlyList<uint>? CustomPalette { get; set; }
    
    /// <summary>Gets the required buffer dimensions for this renderer.</summary>
    (int Width, int Height) RequiredBufferSize { get; }
}
```

### 4.2 Character Generator Interface

```csharp
/// <summary>
/// Provides character bitmap data for text mode rendering.
/// </summary>
public interface ICharacterGenerator
{
    /// <summary>
    /// Gets one row of pixels for a character.
    /// </summary>
    /// <param name="charCode">Character code (0-255).</param>
    /// <param name="scanline">Scanline within character (0-7).</param>
    /// <param name="flash">Current flash state (true = inverted).</param>
    /// <param name="altCharSet">Use alternate character set (MouseText on IIe).</param>
    /// <returns>8 bits representing pixels (bit 7 = leftmost, or bit 6 for 7-wide).</returns>
    byte GetCharacterRow(byte charCode, int scanline, bool flash, bool altCharSet);
}

/// <summary>
/// Apple IIe character generator implementation.
/// </summary>
public sealed class AppleIIeCharacterGenerator : ICharacterGenerator
{
    private readonly byte[] _normalChars;   // Primary character set
    private readonly byte[] _altChars;      // Alternate (MouseText) set
    
    public AppleIIeCharacterGenerator(byte[] characterRom)
    {
        // Character ROM is 2KB: 256 chars – 8 scanlines
        // First 1KB = normal, second 1KB = alternate
        _normalChars = characterRom[..0x800];
        _altChars = characterRom[0x800..];
    }
    
    public byte GetCharacterRow(byte charCode, int scanline, bool flash, bool altCharSet)
    {
        // Determine display mode based on high bits
        bool isInverse = charCode < 0x40;
        bool isFlashing = charCode >= 0x40 && charCode < 0x80;
        
        // Select character set
        byte[] charSet = altCharSet && (charCode >= 0x40 && charCode < 0x80) 
            ? _altChars 
            : _normalChars;
        
        // Map character code to ROM offset
        int romChar = charCode & 0x3F;
        if (charCode >= 0x80)
        {
            romChar = charCode & 0x7F;
        }
        
        // Get bitmap row
        byte pixels = charSet[(romChar * 8) + scanline];
        
        // Apply inverse/flash
        if (isInverse || (isFlashing && flash))
        {
            pixels = (byte)~pixels;
        }
        
        return pixels;
    }
}
```

---

## 5. Avalonia VideoDisplay Control

### 5.1 Control Definition

```csharp
/// <summary>
/// Avalonia control for displaying emulator video output.
/// </summary>
public partial class VideoDisplay : UserControl
{
    // Styled properties
    public static readonly StyledProperty<IMachine?> MachineProperty =
        AvaloniaProperty.Register<VideoDisplay, IMachine?>(nameof(Machine));
    
    public static readonly StyledProperty<double> DisplayScaleProperty =
        AvaloniaProperty.Register<VideoDisplay, double>(nameof(DisplayScale), 2.0);
    
    public static readonly StyledProperty<ScalingMode> ScalingModeProperty =
        AvaloniaProperty.Register<VideoDisplay, ScalingMode>(
            nameof(ScalingMode), ScalingMode.Integer);
    
    public static readonly StyledProperty<double> ScanlineIntensityProperty =
        AvaloniaProperty.Register<VideoDisplay, double>(nameof(ScanlineIntensity), 0.0);
    
    public static readonly StyledProperty<ColorPalette> PaletteProperty =
        AvaloniaProperty.Register<VideoDisplay, ColorPalette>(
            nameof(Palette), ColorPalette.Ntsc);
    
    public static readonly StyledProperty<bool> CaptureInputProperty =
        AvaloniaProperty.Register<VideoDisplay, bool>(nameof(CaptureInput), true);
    
    // Private fields
    private IDisplayService? _displayService;
    private WriteableBitmap? _frameBitmap;
    private DispatcherTimer? _refreshTimer;
    
    // Properties
    public IMachine? Machine
    {
        get => GetValue(MachineProperty);
        set => SetValue(MachineProperty, value);
    }
    
    public double DisplayScale
    {
        get => GetValue(DisplayScaleProperty);
        set => SetValue(DisplayScaleProperty, value);
    }
    
    public ScalingMode ScalingMode
    {
        get => GetValue(ScalingModeProperty);
        set => SetValue(ScalingModeProperty, value);
    }
    
    public double ScanlineIntensity
    {
        get => GetValue(ScanlineIntensityProperty);
        set => SetValue(ScanlineIntensityProperty, value);
    }
    
    public ColorPalette Palette
    {
        get => GetValue(PaletteProperty);
        set => SetValue(PaletteProperty, value);
    }
    
    public bool CaptureInput
    {
        get => GetValue(CaptureInputProperty);
        set => SetValue(CaptureInputProperty, value);
    }
    
    public VideoDisplay()
    {
        InitializeComponent();
    }
    
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        
        if (change.Property == MachineProperty)
        {
            OnMachineChanged(change.GetOldValue<IMachine?>(), 
                           change.GetNewValue<IMachine?>());
        }
        else if (change.Property == PaletteProperty && _displayService != null)
        {
            _displayService.Palette = Palette;
        }
        else if (change.Property == ScanlineIntensityProperty && _displayService != null)
        {
            _displayService.ScanlineIntensity = ScanlineIntensity;
        }
    }
    
    private void OnMachineChanged(IMachine? oldMachine, IMachine? newMachine)
    {
        // Stop refresh timer
        _refreshTimer?.Stop();
        
        // Detach from old machine
        if (_displayService != null && oldMachine != null)
        {
            _displayService.Detach();
            _displayService.FrameReady -= OnFrameReady;
        }
        
        if (newMachine == null)
        {
            _displayService = null;
            _frameBitmap = null;
            InvalidateVisual();
            return;
        }
        
        // Create display service
        _displayService = new DisplayService
        {
            Palette = Palette,
            ScanlineIntensity = ScanlineIntensity,
            Scale = DisplayScale,
            ScalingMode = ScalingMode
        };
        
        _displayService.AttachMachine(newMachine);
        _displayService.FrameReady += OnFrameReady;
        
        // Create frame bitmap matching service buffer size
        var (width, height) = (_displayService as DisplayService)?.BufferManager?.Width ?? 640;
        CreateFrameBitmap(width, height);
        
        // Start refresh timer for UI updates
        _refreshTimer = new DispatcherTimer(
            TimeSpan.FromMilliseconds(16.67),  // 60 Hz
            DispatcherPriority.Render,
            OnRefreshTimer);
        _refreshTimer.Start();
    }
    
    private void CreateFrameBitmap(int width, int height)
    {
        _frameBitmap = new WriteableBitmap(
            new PixelSize(width, height),
            new Vector(96, 96),
            PixelFormat.Bgra8888,
            AlphaFormat.Opaque);
    }
    
    private void OnFrameReady(object? sender, EventArgs e)
    {
        // Frame is ready in buffer manager - UI will pick up on next timer tick
    }
    
    private void OnRefreshTimer(object? sender, EventArgs e)
    {
        if (_displayService?.RenderFrame(_frameBitmap!) == true)
        {
            InvalidateVisual();
        }
    }
    
    public override void Render(DrawingContext context)
    {
        if (_frameBitmap == null)
        {
            // Draw placeholder
            context.FillRectangle(Brushes.Black, new Rect(Bounds.Size));
            return;
        }
        
        // Calculate destination rectangle based on scaling mode
        var destRect = CalculateDestRect();
        
        // Set interpolation mode
        RenderOptions.SetBitmapInterpolationMode(this, 
            ScalingMode == ScalingMode.Integer 
                ? BitmapInterpolationMode.None 
                : BitmapInterpolationMode.LowQuality);
        
        // Draw frame
        context.DrawImage(_frameBitmap, destRect);
        
        // Apply scanline effect
        if (ScanlineIntensity > 0)
        {
            RenderScanlines(context, destRect);
        }
    }
    
    private Rect CalculateDestRect()
    {
        if (_frameBitmap == null)
            return new Rect(Bounds.Size);
        
        var sourceSize = _frameBitmap.Size;
        
        return ScalingMode switch
        {
            ScalingMode.Native => CenterRect(sourceSize),
            ScalingMode.Integer => CenterRect(GetIntegerScaledSize(sourceSize)),
            ScalingMode.AspectCorrect => GetAspectCorrectRect(sourceSize),
            ScalingMode.Fill => new Rect(0, 0, Bounds.Width, Bounds.Height),
            _ => new Rect(Bounds.Size)
        };
    }
    
    private Rect CenterRect(Size size)
    {
        double x = (Bounds.Width - size.Width) / 2;
        double y = (Bounds.Height - size.Height) / 2;
        return new Rect(x, y, size.Width, size.Height);
    }
    
    private Size GetIntegerScaledSize(Size source)
    {
        int scaleX = Math.Max(1, (int)(Bounds.Width / source.Width));
        int scaleY = Math.Max(1, (int)(Bounds.Height / source.Height));
        int scale = Math.Min(scaleX, scaleY);
        return new Size(source.Width * scale, source.Height * scale);
    }
    
    private Rect GetAspectCorrectRect(Size source)
    {
        double targetAspect = 4.0 / 3.0;  // Apple II aspect ratio
        double controlAspect = Bounds.Width / Bounds.Height;
        
        double width, height;
        if (controlAspect > targetAspect)
        {
            // Control is wider - fit to height
            height = Bounds.Height;
            width = height * targetAspect;
        }
        else
        {
            // Control is taller - fit to width
            width = Bounds.Width;
            height = width / targetAspect;
        }
        
        return CenterRect(new Size(width, height));
    }
    
    private void RenderScanlines(DrawingContext context, Rect destRect)
    {
        byte alpha = (byte)(255 * ScanlineIntensity * 0.3);
        using var pen = new Pen(
            new SolidColorBrush(Color.FromArgb(alpha, 0, 0, 0)), 
            1);
        
        double lineSpacing = destRect.Height / 192;
        for (int i = 0; i < 192; i++)
        {
            double y = destRect.Y + (i * lineSpacing) + (lineSpacing * 0.5);
            context.DrawLine(pen, 
                new Point(destRect.X, y), 
                new Point(destRect.Right, y));
        }
    }
    
    // Input handling
    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (CaptureInput && Machine != null)
        {
            // Forward to input service
            // (Implementation depends on IInputService)
            e.Handled = true;
        }
        base.OnKeyDown(e);
    }
    
    protected override void OnKeyUp(KeyEventArgs e)
    {
        if (CaptureInput && Machine != null)
        {
            e.Handled = true;
        }
        base.OnKeyUp(e);
    }
}
```

### 5.2 AXAML Definition

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:controls="using:BadMango.Emulator.UI.Controls"
             x:Class="BadMango.Emulator.UI.Controls.VideoDisplay"
             Focusable="True"
             Background="Black">
    
    <!-- Control is entirely code-rendered via OnRender override -->
    
</UserControl>
```

---

## 6. Threading Model

### 6.1 Thread Responsibilities

| Thread          | Responsibilities                                    |
|-----------------|-----------------------------------------------------|
| Emulator Thread | CPU execution, VBlank callbacks, frame rendering    |
| UI Thread       | Timer ticks, bitmap copy, Avalonia rendering        |

### 6.2 Synchronization Flow

```
Emulator Thread                          UI Thread
      ?                                       ?
      ?  VBlank occurs                        ?
      ?                                       ?
 OnVBlank()                                   ?
      ?                                       ?
      ?                                       ?
 RenderFrame()                                ?
 (write to back buffer)                       ?
      ?                                       ?
      ?                                       ?
 Present()                                    ?
 (swap buffers, set ready flag)              ?
      ?                                       ?
      ?  ?????????????????????????????????????
      ?                                       ?  Timer tick (16.67ms)
      ?                                       ?
      ?                               CopyToTarget()
      ?                               (copy front buffer to bitmap)
      ?                                       ?
      ?                                       ?
      ?                               InvalidateVisual()
      ?                                       ?
      ?                                       ?
      ?                               Render()
      ?                               (draw bitmap to screen)
      ?                                       ?
      ?????????????????????????????????????????
      ?                                       ?
```

### 6.3 Lock-Free Alternative (Advanced)

For high-performance scenarios, consider a lock-free triple buffer:

```csharp
/// <summary>
/// Lock-free triple buffer for maximum throughput.
/// </summary>
public sealed class TripleBuffer<T> where T : class
{
    private T[] _buffers;
    private volatile int _writeIndex = 0;
    private volatile int _readIndex = 2;
    private volatile int _middleIndex = 1;
    private volatile bool _newFrameAvailable = false;
    
    public TripleBuffer(Func<T> factory)
    {
        _buffers = [factory(), factory(), factory()];
    }
    
    /// <summary>
    /// Gets the buffer for writing (emulator thread).
    /// </summary>
    public T WriteBuffer => _buffers[_writeIndex];
    
    /// <summary>
    /// Publishes the write buffer and gets a new one.
    /// </summary>
    public void Publish()
    {
        // Swap write and middle
        int oldMiddle = Interlocked.Exchange(ref _middleIndex, _writeIndex);
        _writeIndex = oldMiddle;
        _newFrameAvailable = true;
    }
    
    /// <summary>
    /// Gets the latest frame for reading (UI thread).
    /// </summary>
    /// <returns>The buffer, or null if no new frame available.</returns>
    public T? GetLatestFrame()
    {
        if (!_newFrameAvailable)
            return null;
        
        // Swap read and middle
        int oldMiddle = Interlocked.Exchange(ref _middleIndex, _readIndex);
        _readIndex = oldMiddle;
        _newFrameAvailable = false;
        
        return _buffers[_readIndex];
    }
}
```

---

## 7. Post-Processing Effects

### 7.1 Scanline Effect

Already shown in the VideoDisplay control. Can be enhanced with shader-based rendering.

### 7.2 CRT Simulation (Advanced)

For authentic CRT appearance, consider these effects:

```csharp
/// <summary>
/// Post-processing effect options.
/// </summary>
public record PostProcessingOptions
{
    /// <summary>Scanline intensity (0-1).</summary>
    public double ScanlineIntensity { get; init; } = 0.0;
    
    /// <summary>CRT curvature amount (0-1).</summary>
    public double CrtCurvature { get; init; } = 0.0;
    
    /// <summary>Bloom/glow intensity (0-1).</summary>
    public double BloomIntensity { get; init; } = 0.0;
    
    /// <summary>Color fringing (RGB separation) (0-1).</summary>
    public double ColorFringing { get; init; } = 0.0;
    
    /// <summary>Vignette intensity (0-1).</summary>
    public double VignetteIntensity { get; init; } = 0.0;
}

/// <summary>
/// Applies post-processing effects to rendered frames.
/// </summary>
public interface IPostProcessor
{
    /// <summary>
    /// Applies post-processing to the frame buffer.
    /// </summary>
    void Apply(Span<uint> buffer, int width, int height, PostProcessingOptions options);
}
```

### 7.3 NTSC Artifact Coloring

For authentic Apple II hi-res colors, implement NTSC signal simulation:

```csharp
/// <summary>
/// NTSC artifact color simulation for hi-res graphics.
/// </summary>
public static class NtscColorizer
{
    // NTSC phase angles for pixel positions
    private const double ColorBurstPhase = 0.0;
    private const double PhasePerPixel = Math.PI / 2;  // 90 degrees
    
    /// <summary>
    /// Converts a horizontal run of pixels to NTSC artifact colors.
    /// </summary>
    public static void ColorizeHiResLine(
        ReadOnlySpan<bool> monoBits,    // Input: mono pixels (280)
        Span<uint> colorPixels,          // Output: ARGB colors
        int startColumn,                 // Starting column (affects phase)
        bool palette)                    // Palette bit (shifts colors)
    {
        // Simplified NTSC colorization
        // Real implementation would simulate composite signal
        
        uint violet = 0xFFDD22DD;
        uint green = 0xFF11DD00;
        uint blue = 0xFF2222FF;
        uint orange = 0xFFFF6600;
        uint white = 0xFFFFFFFF;
        uint black = 0xFF000000;
        
        uint color0 = palette ? blue : violet;
        uint color1 = palette ? orange : green;
        
        for (int i = 0; i < monoBits.Length; i++)
        {
            bool current = monoBits[i];
            bool prev = i > 0 && monoBits[i - 1];
            bool next = i < monoBits.Length - 1 && monoBits[i + 1];
            
            if (!current)
            {
                colorPixels[i] = black;
            }
            else if (prev || next)
            {
                colorPixels[i] = white;
            }
            else
            {
                int screenCol = startColumn + i;
                colorPixels[i] = (screenCol & 1) == 0 ? color0 : color1;
            }
        }
    }
}
```

---

## 8. Performance Guidelines

### 8.1 Optimization Strategies

| Strategy                    | Benefit                                    |
|-----------------------------|--------------------------------------------|
| Dirty region tracking       | Skip unchanged screen areas                |
| Pre-computed lookup tables  | Avoid per-pixel calculations               |
| SIMD pixel operations       | Parallel pixel processing                  |
| Efficient memory layout     | Cache-friendly access patterns             |
| Separate render thread      | Don't block emulator execution             |

### 8.2 Memory Layout

Organize pixel buffers for sequential access:

```csharp
/// <summary>
/// Ensures cache-efficient access patterns.
/// </summary>
public static class RenderingOptimizations
{
    /// <summary>
    /// Renders a horizontal scanline with sequential writes.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RenderScanlineSequential(
        Span<uint> buffer,
        int bufferWidth,
        int y,
        ReadOnlySpan<uint> pixels)
    {
        int offset = y * bufferWidth;
        pixels.CopyTo(buffer.Slice(offset, pixels.Length));
    }
    
    /// <summary>
    /// Fills a horizontal span with a single color.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void FillHorizontal(
        Span<uint> buffer,
        int bufferWidth,
        int x, int y,
        int width,
        uint color)
    {
        int offset = (y * bufferWidth) + x;
        buffer.Slice(offset, width).Fill(color);
    }
}
```

### 8.3 Benchmark Targets

| Operation              | Target Time (ms) | Notes                        |
|------------------------|------------------|------------------------------|
| Full frame render      | < 2.0            | At 60 fps = 16.67ms budget   |
| Buffer swap            | < 0.1            | Lock contention minimal      |
| Bitmap copy to UI      | < 1.0            | 640–400 @ 32bpp              |
| Avalonia render        | < 5.0            | Simple blit operation        |
| Total frame time       | < 10.0           | Leaves margin for emulation  |

---

## 9. Testing Strategy

### 9.1 Unit Tests

```csharp
/// <summary>
/// Unit tests for display rendering.
/// </summary>
public class DisplayRendererTests
{
    [Fact]
    public void Text40Mode_RendersCorrectDimensions()
    {
        var buffer = new uint[560 * 384];
        var renderer = new AppleIIDisplayRenderer();
        var mockVideo = CreateMockVideoController(text: true, col80: false);
        var mockBus = CreateMockMemoryBus();
        
        renderer.RenderFrame(buffer, mockVideo, mockBus);
        
        // Verify buffer was written
        Assert.Contains(buffer, p => p != 0);
    }
    
    [Fact]
    public void HiResMode_AppliesNtscColors()
    {
        var buffer = new uint[560 * 384];
        var renderer = new AppleIIDisplayRenderer { Palette = ColorPalette.Ntsc };
        
        // Set up hi-res with alternating pixels
        var mockVideo = CreateMockVideoController(text: false, hires: true);
        var mockBus = CreateMockMemoryBusWithPattern(0x2000, 0x55);  // 01010101
        
        renderer.RenderFrame(buffer, mockVideo, mockBus);
        
        // Verify artifact colors present (not just black/white)
        var uniqueColors = buffer.Distinct().ToList();
        Assert.True(uniqueColors.Count > 2, "Should have artifact colors");
    }
    
    [Fact]
    public void BufferManager_ThreadSafeSwap()
    {
        var manager = new FrameBufferManager(640, 400);
        var tasks = new List<Task>();
        
        // Simulate emulator thread
        tasks.Add(Task.Run(() =>
        {
            for (int i = 0; i < 1000; i++)
            {
                var buffer = manager.BackBuffer;
                buffer[0] = (uint)i;
                manager.Present();
            }
        }));
        
        // Simulate UI thread
        var bitmap = new WriteableBitmap(
            new PixelSize(640, 400),
            new Vector(96, 96),
            PixelFormat.Bgra8888,
            AlphaFormat.Opaque);
        
        tasks.Add(Task.Run(() =>
        {
            for (int i = 0; i < 1000; i++)
            {
                manager.CopyToTarget(bitmap);
            }
        }));
        
        // Should complete without deadlock or exception
        Task.WhenAll(tasks).Wait(TimeSpan.FromSeconds(5));
    }
}
```

### 9.2 Visual Regression Tests

Use snapshot testing to catch visual regressions:

```csharp
/// <summary>
/// Visual regression tests using snapshot comparison.
/// </summary>
public class VisualRegressionTests
{
    [Theory]
    [InlineData("text_hello", DisplayMode.Text40)]
    [InlineData("hires_colors", DisplayMode.HiRes)]
    [InlineData("dhr_colors", DisplayMode.DoubleHiRes)]
    public async Task RenderSnapshot_MatchesBaseline(string testName, DisplayMode mode)
    {
        var renderer = CreateRendererForMode(mode);
        var buffer = new uint[640 * 400];
        var testData = LoadTestData(testName);
        
        renderer.RenderFrame(buffer, testData.Video, testData.Bus);
        
        var baselinePath = $"baselines/{testName}.png";
        var actualPath = $"actual/{testName}.png";
        
        SaveBufferAsPng(buffer, 640, 400, actualPath);
        
        if (!File.Exists(baselinePath))
        {
            // First run - save baseline
            File.Copy(actualPath, baselinePath);
            return;
        }
        
        // Compare with baseline
        var diff = CompareImages(baselinePath, actualPath);
        Assert.True(diff < 0.01, $"Visual difference {diff:P} exceeds threshold");
    }
}
```

---

## 10. Error Handling

### 10.1 Graceful Degradation

```csharp
/// <summary>
/// Handles rendering errors gracefully.
/// </summary>
public sealed class ResilientDisplayRenderer : IDisplayRenderer
{
    private readonly IDisplayRenderer _inner;
    private readonly ILogger _logger;
    private int _consecutiveErrors;
    
    public void RenderFrame(Span<uint> buffer, IVideoController video, IMemoryBus bus)
    {
        try
        {
            _inner.RenderFrame(buffer, video, bus);
            _consecutiveErrors = 0;
        }
        catch (Exception ex)
        {
            _consecutiveErrors++;
            _logger.LogWarning(ex, "Render error (count: {Count})", _consecutiveErrors);
            
            if (_consecutiveErrors > 10)
            {
                // Too many errors - render error screen
                RenderErrorScreen(buffer);
            }
            else
            {
                // Leave buffer unchanged (show last good frame)
            }
        }
    }
    
    private void RenderErrorScreen(Span<uint> buffer)
    {
        // Fill with distinctive pattern to indicate error
        buffer.Fill(0xFF800000);  // Dark red
    }
}
```

### 10.2 Memory Access Errors

```csharp
/// <summary>
/// Handles invalid memory access during rendering.
/// </summary>
public sealed class SafeMemoryReader
{
    private readonly IMemoryBus _bus;
    private readonly byte _fallbackValue;
    
    public SafeMemoryReader(IMemoryBus bus, byte fallbackValue = 0x00)
    {
        _bus = bus;
        _fallbackValue = fallbackValue;
    }
    
    public byte ReadSafe(Addr address, in BusAccess access)
    {
        var result = _bus.Read8(access with { Address = address });
        
        if (result.Fault != null)
        {
            // Log but don't throw - return fallback
            return _fallbackValue;
        }
        
        return result.Value;
    }
}
```

---

## 11. Future Considerations

### 11.1 GPU-Accelerated Rendering

For enhanced post-processing, consider GPU shaders:

```csharp
/// <summary>
/// GPU-accelerated renderer using SkiaSharp.
/// </summary>
public interface IGpuRenderer
{
    /// <summary>
    /// Uploads frame buffer to GPU texture.
    /// </summary>
    void UploadFrame(ReadOnlySpan<uint> buffer, int width, int height);
    
    /// <summary>
    /// Renders with GPU post-processing.
    /// </summary>
    void RenderWithEffects(DrawingContext context, PostProcessingOptions options);
}
```

### 11.2 HDR and Wide Color Gamut

For modern displays:

```csharp
/// <summary>
/// HDR color space support for wide gamut displays.
/// </summary>
public interface IHdrColorConverter
{
    /// <summary>
    /// Converts sRGB frame buffer to HDR10 or Display P3.
    /// </summary>
    void ConvertToHdr(
        ReadOnlySpan<uint> srgbBuffer,
        Span<ushort> hdrBuffer,
        HdrColorSpace targetSpace);
}

public enum HdrColorSpace
{
    Hdr10,
    DisplayP3,
    Rec2020
}
```

### 11.3 Multi-Window Support

For pop-out display windows:

```csharp
/// <summary>
/// Manages multiple display windows for the same machine.
/// </summary>
public interface IMultiDisplayManager
{
    /// <summary>
    /// Creates a new display window for the attached machine.
    /// </summary>
    IDisplayWindow CreateWindow(DisplayWindowOptions options);
    
    /// <summary>
    /// Gets all active display windows.
    /// </summary>
    IReadOnlyList<IDisplayWindow> Windows { get; }
}
```

---

## Document History

| Version | Date       | Changes                            |
|---------|------------|------------------------------------|
| 1.0     | 2025-12-28 | Initial specification              |
