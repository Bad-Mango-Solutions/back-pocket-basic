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

    private readonly IVideoRenderer renderer;
    private readonly PixelBuffer pixelBuffer;
    private readonly DispatcherTimer fpsTimer;
    private readonly Stopwatch fpsStopwatch;

    private IMachine? machine;
    private IVideoDevice? videoDevice;
    private ICharacterDevice? characterDevice;
    private IExtended80ColumnDevice? extended80ColumnDevice;
    private EmulatorKeyboardDevice? keyboardDevice;
    private IMemoryBus? memoryBus;
    private Memory<byte> characterRom;
    private bool characterRomUpdatePending;
    private int vblankPending;

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
        characterRom = Memory<byte>.Empty;
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
    /// Marshals the rendering call to the UI thread.
    /// </summary>
    private void OnVBlankOccurred()
    {
        // Only queue a UI render if one isn't already pending
        if (Interlocked.CompareExchange(ref vblankPending, 1, 0) == 0)
        {
            Dispatcher.UIThread.InvokeAsync(ProcessVBlankFrame, DispatcherPriority.Render);
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

        // Get auxiliary memory reader for 80-column mode
        // This reads directly from the Extended 80-Column device's auxiliary RAM
        Func<ushort, byte>? readAuxMemory = extended80ColumnDevice is not null
            ? extended80ColumnDevice.ReadAuxRam
            : null;

        // Render frame using the Pocket2VideoRenderer with current color mode
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