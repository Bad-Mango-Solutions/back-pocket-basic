// <copyright file="VideoWindow.axaml.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.UI.Views;

using System.Diagnostics;

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;

using BadMango.Emulator.Bus;
using BadMango.Emulator.Bus.Interfaces;
using BadMango.Emulator.Devices;
using BadMango.Emulator.Devices.Interfaces;
using BadMango.Emulator.Rendering;

using EmulatorKeyboardDevice = BadMango.Emulator.Devices.Interfaces.IKeyboardDevice;

/// <summary>
/// Video display window for the debugger console.
/// </summary>
/// <remarks>
/// <para>
/// This window displays the emulated Pocket2e video output using the
/// <see cref="IVideoRenderer"/> to convert emulated video memory to pixels.
/// </para>
/// <para>
/// The window captures keyboard input and forwards it to the emulated
/// keyboard device according to the Keyboard Mapping Specification.
/// </para>
/// <para>
/// Apple key mappings:
/// <list type="bullet">
/// <item><description>Left Alt → Open Apple (PB0 at $C061)</description></item>
/// <item><description>Right Alt → Closed Apple (PB1 at $C062)</description></item>
/// </list>
/// </para>
/// </remarks>
public partial class VideoWindow : Window
{
    /// <summary>
    /// Canonical framebuffer width in pixels.
    /// </summary>
    private const int CanonicalWidth = 560;

    /// <summary>
    /// Canonical framebuffer height in pixels.
    /// </summary>
    private const int CanonicalHeight = 384;

    /// <summary>
    /// Number of frames between flash state toggles (~1.9 Hz at 60 fps).
    /// </summary>
    private const int FlashToggleFrames = 16;

    /// <summary>
    /// Base address of the text page snapshot range ($0400).
    /// </summary>
    private const int TextPageSnapshotBase = 0x0400;

    /// <summary>
    /// Size of the text page snapshot covering both text page ranges ($0400-$0BFF).
    /// </summary>
    /// <remarks>
    /// <para>
    /// In 40-column mode, Page 1 uses main $0400-$07FF and Page 2 uses main $0800-$0BFF.
    /// In 80-column mode, the display interleaves main and auxiliary memory at $0400-$07FF
    /// (even columns from aux, odd columns from main). The 80-column firmware typically
    /// uses PAGE1 addresses ($0400-$07FF) in both main and aux RAM — PAGE2 in 80-column
    /// mode refers to $0400-$07FF in auxiliary RAM, not $0800.
    /// </para>
    /// <para>
    /// We snapshot the full $0400-$0BFF range from both main and aux to cover all
    /// page configurations.
    /// </para>
    /// </remarks>
    private const int TextPageSnapshotSize = 0x0800;

    private readonly IVideoRenderer renderer;
    private readonly PixelBuffer pixelBuffer;
    private readonly DispatcherTimer fpsTimer;
    private readonly Stopwatch fpsStopwatch;

    // Snapshot buffers for display memory captured at VBlank start.
    // This prevents the renderer from seeing half-updated memory when the
    // firmware is actively modifying text pages between frames.
    private readonly byte[] mainMemorySnapshot = new byte[TextPageSnapshotSize];
    private readonly byte[] auxMemorySnapshot = new byte[TextPageSnapshotSize];
    private readonly object snapshotLock = new();

    private IMachine? machine;
    private IVideoDevice? videoDevice;
    private ICharacterDevice? characterDevice;
    private IExtended80ColumnDevice? extended80ColumnDevice;
    private EmulatorKeyboardDevice? keyboardDevice;
    private IMemoryBus? memoryBus;
    private ReadOnlyMemory<byte> mainRam;
    private Memory<byte> characterRom;
    private bool characterRomUpdatePending;
    private int vblankPending;
    private volatile bool snapshotValid;

    private int scale = 2;
    private bool showFps;
    private DisplayColorMode colorMode = DisplayColorMode.Green;
    private int frameCount;
    private int flashFrameCounter;
    private bool flashState;
    private double lastFps;

    // Track Left/Right Alt state separately since Avalonia's KeyModifiers.Alt doesn't distinguish
    private bool leftAltPressed;
    private bool rightAltPressed;
    private bool capsLockActive;

    /// <summary>
    /// Initializes a new instance of the <see cref="VideoWindow"/> class.
    /// </summary>
    public VideoWindow()
    {
        InitializeComponent();

        renderer = new Pocket2VideoRenderer();
        pixelBuffer = new PixelBuffer(CanonicalWidth, CanonicalHeight);
        fpsStopwatch = new Stopwatch();

        // Set initial bitmap from PixelBuffer
        VideoImage.Source = pixelBuffer.Bitmap;

        // Initialize FPS timer at 1 Hz for FPS counter updates only
        fpsTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromSeconds(1),
        };
        fpsTimer.Tick += OnFpsTimer;

        // Subscribe to window events
        Opened += OnWindowOpened;
        Closed += OnWindowClosed;
    }

    /// <summary>
    /// Gets or sets the display scale factor.
    /// </summary>
    /// <value>The integer scale factor (1-4). Default is 2.</value>
    public int Scale
    {
        get => scale;
        set
        {
            if (value < 1 || value > 4)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Scale must be between 1 and 4.");
            }

            scale = value;
            UpdateWindowSize();
            ForceRedraw();
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether FPS display is shown.
    /// </summary>
    public bool ShowFps
    {
        get => showFps;
        set
        {
            showFps = value;
            FpsDisplay.IsVisible = value;
        }
    }

    /// <summary>
    /// Gets or sets the display color mode.
    /// </summary>
    /// <value>The color mode (Green, Amber, White, or Color). Default is Green.</value>
    public DisplayColorMode ColorMode
    {
        get => colorMode;
        set
        {
            colorMode = value;
            ForceRedraw();
        }
    }

    /// <summary>
    /// Attaches the video window to a machine instance.
    /// </summary>
    /// <param name="machine">The machine to attach to.</param>
    /// <remarks>
    /// <para>
    /// Extracts the video device, character device, keyboard device, extended 80-column
    /// device, and memory bus from the machine for rendering and input handling.
    /// </para>
    /// <para>
    /// If no <see cref="ICharacterRomProvider"/> is found, falls back to the default ROM.
    /// </para>
    /// </remarks>
    public void AttachMachine(IMachine machine)
    {
        ArgumentNullException.ThrowIfNull(machine);

        this.machine = machine;
        this.memoryBus = machine.Bus;
        this.videoDevice = machine.GetComponent<IVideoDevice>();
        this.keyboardDevice = machine.GetComponent<EmulatorKeyboardDevice>();
        this.extended80ColumnDevice = machine.GetComponent<IExtended80ColumnDevice>();

        // Get direct physical main RAM reference for snapshot capture.
        // This bypasses all soft switch mapping (80STORE, PAGE2, RAMRD) to ensure
        // the video snapshot always reads from the correct physical memory.
        var mainMemoryProvider = machine.GetComponent<IMainMemoryProvider>();
        this.mainRam = mainMemoryProvider?.MainRam ?? ReadOnlyMemory<byte>.Empty;

        // Look for CharacterDevice (preferred) or any ICharacterRomProvider
        this.characterDevice = machine.GetComponent<ICharacterDevice>();
        ICharacterRomProvider? charProvider = characterDevice ?? machine.GetComponent<ICharacterRomProvider>();

        // Get character ROM data from character provider, or use default ROM
        if (charProvider != null && charProvider.IsCharacterRomLoaded)
        {
            characterRom = charProvider.GetCharacterRomData();
        }
        else
        {
            // Fall back to default ROM
            characterRom = DefaultCharacterRom.GetRomData();
        }

        // Subscribe to CharacterRomChanged event if available
        if (characterDevice != null)
        {
            characterDevice.CharacterRomChanged += OnCharacterRomChanged;
        }

        // Subscribe to VBlank event for frame-accurate rendering
        if (videoDevice != null)
        {
            videoDevice.VBlankOccurred += OnVBlankOccurred;
        }
    }

    /// <summary>
    /// Detaches the video window from the current machine.
    /// </summary>
    public void DetachMachine()
    {
        // Unsubscribe from events
        if (characterDevice != null)
        {
            characterDevice.CharacterRomChanged -= OnCharacterRomChanged;
        }

        if (videoDevice != null)
        {
            videoDevice.VBlankOccurred -= OnVBlankOccurred;
        }

        machine = null;
        memoryBus = null;
        videoDevice = null;
        characterDevice = null;
        extended80ColumnDevice = null;
        keyboardDevice = null;
        mainRam = ReadOnlyMemory<byte>.Empty;
        characterRom = Memory<byte>.Empty;
        snapshotValid = false;
    }

    /// <summary>
    /// Forces an immediate redraw of the video display.
    /// </summary>
    public void ForceRedraw()
    {
        Dispatcher.UIThread.InvokeAsync(RenderFrame);
    }

    /// <inheritdoc/>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (keyboardDevice is null)
        {
            return;
        }

        // Track Left/Right Alt separately for Open/Closed Apple
        if (e.Key == Key.LeftAlt)
        {
            leftAltPressed = true;
        }
        else if (e.Key == Key.RightAlt)
        {
            rightAltPressed = true;
        }
        else if (e.Key == Key.CapsLock)
        {
            // Toggle Caps Lock state
            capsLockActive = !capsLockActive;
        }

        // Handle modifier keys
        UpdateModifiers(e);

        // Map key to Pocket2e byte code
        byte? keyCode = MapKeyToPocket2(e.Key, e.KeyModifiers, capsLockActive);
        if (keyCode.HasValue)
        {
            keyboardDevice.KeyDown(keyCode.Value);
            e.Handled = true;
        }
    }

    /// <inheritdoc/>
    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);

        if (keyboardDevice is null)
        {
            return;
        }

        // Track Left/Right Alt separately for Open/Closed Apple
        if (e.Key == Key.LeftAlt)
        {
            leftAltPressed = false;
        }
        else if (e.Key == Key.RightAlt)
        {
            rightAltPressed = false;
        }

        // Update modifiers
        UpdateModifiers(e);

        // Release key
        keyboardDevice.KeyUp();
        e.Handled = true;
    }

    /// <summary>
    /// Maps Avalonia key codes to Pocket2e ASCII codes.
    /// </summary>
    /// <param name="key">The Avalonia key code.</param>
    /// <param name="modifiers">The active key modifiers.</param>
    /// <param name="capsLock">Whether Caps Lock is active.</param>
    /// <returns>The Pocket2e ASCII code, or null if the key is not mapped.</returns>
    private static byte? MapKeyToPocket2(Key key, KeyModifiers modifiers, bool capsLock)
    {
        bool ctrl = modifiers.HasFlag(KeyModifiers.Control);
        bool shift = modifiers.HasFlag(KeyModifiers.Shift);

        // Control key combinations
        if (ctrl)
        {
            return key switch
            {
                >= Key.A and <= Key.Z => (byte)(key - Key.A + 1), // Ctrl+A = 0x01, etc.
                Key.OemOpenBrackets => 0x1B, // Ctrl+[ = ESC
                _ => null,
            };
        }

        // Arrow keys
        switch (key)
        {
            case Key.Left:
                return 0x08; // Backspace / Left arrow
            case Key.Right:
                return 0x15; // Ctrl+U / Right arrow
            case Key.Up:
                return 0x0B; // Ctrl+K / Up arrow
            case Key.Down:
                return 0x0A; // Ctrl+J / Down arrow
            case Key.Return:
                return 0x0D; // Carriage return
            case Key.Back:
                return 0x08; // Backspace
            case Key.Tab:
                return 0x09; // Tab
            case Key.Escape:
                return 0x1B; // Escape
            case Key.Space:
                return 0x20; // Space
        }

        // Alphanumeric keys
        if (key >= Key.A && key <= Key.Z)
        {
            byte baseCode = (byte)(key - Key.A + 0x41); // A = 0x41

            // Caps Lock affects only letters: XOR with shift for toggle behavior
            // - Caps Lock off, Shift off → lowercase
            // - Caps Lock off, Shift on → uppercase
            // - Caps Lock on, Shift off → uppercase
            // - Caps Lock on, Shift on → lowercase (shift "undoes" caps lock)
            bool uppercase = shift ^ capsLock;
            return uppercase ? baseCode : (byte)(baseCode + 0x20);
        }

        if (key >= Key.D0 && key <= Key.D9)
        {
            if (shift)
            {
                // Shifted number keys produce symbols (Caps Lock does NOT affect these)
                return key switch
                {
                    Key.D1 => (byte)'!',
                    Key.D2 => (byte)'@',
                    Key.D3 => (byte)'#',
                    Key.D4 => (byte)'$',
                    Key.D5 => (byte)'%',
                    Key.D6 => (byte)'^',
                    Key.D7 => (byte)'&',
                    Key.D8 => (byte)'*',
                    Key.D9 => (byte)'(',
                    Key.D0 => (byte)')',
                    _ => null,
                };
            }

            return (byte)(key - Key.D0 + 0x30); // 0 = 0x30
        }

        // Punctuation (Caps Lock does NOT affect these)
        return key switch
        {
            Key.OemMinus => shift ? (byte)'_' : (byte)'-',
            Key.OemPlus => shift ? (byte)'+' : (byte)'=',
            Key.OemOpenBrackets => shift ? (byte)'{' : (byte)'[',
            Key.OemCloseBrackets => shift ? (byte)'}' : (byte)']',
            Key.OemBackslash or Key.OemPipe => shift ? (byte)'|' : (byte)'\\',
            Key.OemSemicolon => shift ? (byte)':' : (byte)';',
            Key.OemQuotes => shift ? (byte)'"' : (byte)'\'',
            Key.OemComma => shift ? (byte)'<' : (byte)',',
            Key.OemPeriod => shift ? (byte)'>' : (byte)'.',
            Key.OemQuestion => shift ? (byte)'?' : (byte)'/',
            Key.OemTilde => shift ? (byte)'~' : (byte)'`',
            _ => null,
        };
    }

    /// <summary>
    /// Called when the character ROM configuration changes.
    /// </summary>
    /// <remarks>
    /// This is called from the CharacterDevice at VBLANK to signal that the
    /// character table has changed and should be reloaded.
    /// </remarks>
    private void OnCharacterRomChanged()
    {
        // Mark that we need to update the character ROM on the next render
        characterRomUpdatePending = true;
    }

    private void OnWindowOpened(object? sender, EventArgs e)
    {
        UpdateWindowSize();
        fpsStopwatch.Start();
        fpsTimer.Start();
    }

    private void OnWindowClosed(object? sender, EventArgs e)
    {
        fpsTimer.Stop();
        fpsStopwatch.Stop();
        pixelBuffer.Dispose();
    }

    /// <summary>
    /// Called when the video device fires a VBlank event.
    /// Captures a display memory snapshot and marshals the rendering call to the UI thread.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This callback runs on the emulator thread during the scheduler dispatch,
    /// meaning the CPU is NOT executing instructions at this point. We capture
    /// a snapshot of the text page memory here to prevent the renderer from seeing
    /// half-updated data when the firmware is actively modifying text pages.
    /// </para>
    /// <para>
    /// The snapshot is captured synchronously before queuing the UI render, ensuring
    /// the renderer always sees a consistent view of the display memory from the
    /// exact moment of VBlank start.
    /// </para>
    /// </remarks>
    private void OnVBlankOccurred()
    {
        // Capture display memory snapshot on the emulator thread while CPU is paused.
        // This is the key to preventing 80-column flicker: the firmware modifies
        // PAGE1/PAGE2 buffers between VBlanks, but we capture a stable copy here.
        CaptureDisplaySnapshot();

        // Only queue a UI render if one isn't already pending
        if (Interlocked.CompareExchange(ref vblankPending, 1, 0) == 0)
        {
            Dispatcher.UIThread.InvokeAsync(ProcessVBlankFrame, DispatcherPriority.Render);
        }
    }

    /// <summary>
    /// Captures a snapshot of the text page display memory from both main and auxiliary RAM.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method captures the text page address range ($0400-$0BFF) from both main and
    /// auxiliary memory. The snapshot is taken at VBlank start when the CPU is not actively
    /// modifying video memory, ensuring the renderer sees a consistent frame.
    /// </para>
    /// <para>
    /// Both main and auxiliary memory are read directly from their physical RAM backing
    /// stores, bypassing all soft switch mapping (80STORE, PAGE2, RAMRD). This is critical
    /// because when 80STORE is enabled, reads to $0400-$07FF through the memory bus may
    /// be redirected to auxiliary RAM based on the PAGE2 state, causing the main memory
    /// snapshot to contain auxiliary data (and vice versa). The video hardware on real
    /// Apple IIe accesses VRAM directly without going through the MMU soft switch logic.
    /// </para>
    /// <para>
    /// In 80-column mode, the display interleaves main and auxiliary memory at $0400-$07FF:
    /// even columns come from auxiliary RAM and odd columns from main RAM. PAGE2 in
    /// 80-column mode refers to $0400-$07FF in auxiliary RAM, not the $0800 page.
    /// Both main and aux sources must be captured to prevent interleaving artifacts.
    /// </para>
    /// </remarks>
    private void CaptureDisplaySnapshot()
    {
        lock (snapshotLock)
        {
            // Capture main memory text pages ($0400-$0BFF) directly from physical RAM,
            // bypassing soft switch mapping that could redirect reads to aux memory.
            if (!mainRam.IsEmpty
                && mainRam.Length >= TextPageSnapshotBase + TextPageSnapshotSize)
            {
                mainRam.Span.Slice(TextPageSnapshotBase, TextPageSnapshotSize)
                    .CopyTo(mainMemorySnapshot);
            }
            else if (memoryBus is not null)
            {
                // Fallback to bus reads if direct RAM access is unavailable
                // or the physical memory is too small
                for (int i = 0; i < TextPageSnapshotSize; i++)
                {
                    mainMemorySnapshot[i] = ReadMemoryByte((ushort)(TextPageSnapshotBase + i));
                }
            }

            // Capture auxiliary memory text pages (same address range in aux RAM)
            if (extended80ColumnDevice is not null)
            {
                for (int i = 0; i < TextPageSnapshotSize; i++)
                {
                    auxMemorySnapshot[i] = extended80ColumnDevice.ReadAuxRam(
                        (ushort)(TextPageSnapshotBase + i));
                }
            }

            snapshotValid = true;
        }
    }

    /// <summary>
    /// Processes a VBlank-triggered frame redraw on the UI thread.
    /// </summary>
    private void ProcessVBlankFrame()
    {
        Interlocked.Exchange(ref vblankPending, 0);

        // Update flash state
        flashFrameCounter++;
        if (flashFrameCounter >= FlashToggleFrames)
        {
            flashFrameCounter = 0;
            flashState = !flashState;
        }

        // Process pending character ROM update (happens at VBLANK)
        if (characterRomUpdatePending)
        {
            characterRomUpdatePending = false;
            ReloadCharacterRom();
        }

        // Render frame
        RenderFrame();

        // Update FPS counter
        frameCount++;
    }

    private void OnFpsTimer(object? sender, EventArgs e)
    {
        if (fpsStopwatch.ElapsedMilliseconds >= 1000)
        {
            lastFps = frameCount / (fpsStopwatch.ElapsedMilliseconds / 1000.0);
            FpsDisplay.Text = $"{lastFps:F1} FPS";
            frameCount = 0;
            fpsStopwatch.Restart();
        }
    }

    /// <summary>
    /// Reloads the character ROM data from the character device.
    /// </summary>
    private void ReloadCharacterRom()
    {
        if (characterDevice != null && characterDevice.IsCharacterRomLoaded)
        {
            characterRom = characterDevice.GetCharacterRomData();
        }
    }

    private void RenderFrame()
    {
        var pixels = pixelBuffer.GetPixels();

        if (memoryBus is null || videoDevice is null)
        {
            // No machine attached - render blank screen
            renderer.Clear(pixels);
            pixelBuffer.Commit();
            VideoImage.InvalidateVisual();
            return;
        }

        // Determine current video mode
        VideoMode mode = videoDevice.CurrentMode;

        // Get ALTCHAR state from CharacterDevice if available, otherwise assume false
        bool useAltCharSet = characterDevice?.IsAltCharSet ?? false;

        // Get NOFLASH states from CharacterDevice if available
        // Default: Bank 1 = flashing enabled (noFlash = false), Bank 2 = flashing disabled (noFlash = true)
        bool noFlash1 = characterDevice?.IsNoFlash1Enabled ?? false;
        bool noFlash2 = characterDevice?.IsNoFlash2Enabled ?? true;

        if (snapshotValid)
        {
            // Use snapshot data — guaranteed consistent from VBlank capture.
            // Lock to prevent a concurrent VBlank from overwriting the snapshot
            // while the renderer is reading from it.
            lock (snapshotLock)
            {
                renderer.RenderFrame(
                    pixels,
                    mode,
                    ReadSnapshotMainByte,
                    characterRom.Span,
                    useAltCharSet,
                    videoDevice.IsPage2,
                    flashState,
                    noFlash1,
                    noFlash2,
                    colorMode,
                    extended80ColumnDevice is not null ? ReadSnapshotAuxByte : null);
            }
        }
        else
        {
            // No snapshot available (e.g., force redraw) — fall back to live memory
            Func<ushort, byte>? readAuxMemory = extended80ColumnDevice is not null
                ? extended80ColumnDevice.ReadAuxRam
                : null;

            renderer.RenderFrame(
                pixels,
                mode,
                ReadMemoryByte,
                characterRom.Span,
                useAltCharSet,
                videoDevice.IsPage2,
                flashState,
                noFlash1,
                noFlash2,
                colorMode,
                readAuxMemory);
        }

        // Commit pixel buffer to bitmap
        pixelBuffer.Commit();
        VideoImage.InvalidateVisual();
    }

    private byte ReadMemoryByte(ushort address)
    {
        if (memoryBus is null)
        {
            return 0;
        }

        // Use side-effect-free DMA-style read
        var access = new BusAccess(
            Address: address,
            Value: 0,
            WidthBits: 8,
            Mode: BusAccessMode.Atomic,
            EmulationFlag: true,
            Intent: AccessIntent.DmaRead,
            SourceId: 0,
            Cycle: 0,
            Flags: AccessFlags.NoSideEffects);

        return memoryBus.Read8(access);
    }

    /// <summary>
    /// Reads a byte from the main memory snapshot captured at VBlank.
    /// </summary>
    /// <param name="address">The memory address to read.</param>
    /// <returns>The byte value from the snapshot, or from live memory if outside the snapshot range.</returns>
    private byte ReadSnapshotMainByte(ushort address)
    {
        int offset = address - TextPageSnapshotBase;
        if (offset >= 0 && offset < TextPageSnapshotSize)
        {
            return mainMemorySnapshot[offset];
        }

        // Address is outside snapshot range — fall back to live memory
        return ReadMemoryByte(address);
    }

    /// <summary>
    /// Reads a byte from the auxiliary memory snapshot captured at VBlank.
    /// </summary>
    /// <param name="address">The memory address to read.</param>
    /// <returns>The byte value from the auxiliary memory snapshot, or 0 if outside range.</returns>
    private byte ReadSnapshotAuxByte(ushort address)
    {
        int offset = address - TextPageSnapshotBase;
        if (offset >= 0 && offset < TextPageSnapshotSize)
        {
            return auxMemorySnapshot[offset];
        }

        // Address is outside snapshot range — fall back to live aux memory
        return extended80ColumnDevice?.ReadAuxRam(address) ?? 0;
    }

    private void UpdateWindowSize()
    {
        int width = CanonicalWidth * scale;
        int height = CanonicalHeight * scale;

        Width = width + 20; // Add padding for window chrome
        Height = height + 40;

        // Update Image control size
        VideoImage.Width = width;
        VideoImage.Height = height;
    }

    private void UpdateModifiers(KeyEventArgs e)
    {
        if (keyboardDevice is null)
        {
            return;
        }

        var modifiers = KeyboardModifiers.None;

        if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
        {
            modifiers |= KeyboardModifiers.Shift;
        }

        if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            modifiers |= KeyboardModifiers.Control;
        }

        // Left Alt → Open Apple (PB0)
        // Right Alt → Closed Apple (PB1)
        // Use our tracked state since Avalonia's KeyModifiers.Alt doesn't distinguish left/right
        if (leftAltPressed)
        {
            modifiers |= KeyboardModifiers.OpenApple;
        }

        if (rightAltPressed)
        {
            modifiers |= KeyboardModifiers.ClosedApple;
        }

        // Track Caps Lock state
        if (capsLockActive)
        {
            modifiers |= KeyboardModifiers.CapsLock;
        }

        keyboardDevice.SetModifiers(modifiers);
    }
}