// <copyright file="VideoWindow.axaml.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.UI.Views;

using System.Diagnostics;
using System.Runtime.InteropServices;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
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
    /// Target frame rate in frames per second.
    /// </summary>
    private const double TargetFrameRate = 60.0;

    /// <summary>
    /// Number of frames between flash state toggles (~1.9 Hz at 60 fps).
    /// </summary>
    private const int FlashToggleFrames = 16;

    private readonly IVideoRenderer renderer;
    private readonly uint[] pixelBuffer;
    private readonly WriteableBitmap frameBitmap;
    private readonly DispatcherTimer refreshTimer;
    private readonly Stopwatch fpsStopwatch;

    private IMachine? machine;
    private IVideoDevice? videoDevice;
    private EmulatorKeyboardDevice? keyboardDevice;
    private IMemoryBus? memoryBus;
    private Memory<byte> characterRom;

    private int scale = 2;
    private bool showFps;
    private int frameCount;
    private int flashFrameCounter;
    private bool flashState;
    private double lastFps;

    /// <summary>
    /// Initializes a new instance of the <see cref="VideoWindow"/> class.
    /// </summary>
    public VideoWindow()
    {
        InitializeComponent();

        renderer = new Pocket2VideoRenderer();
        pixelBuffer = new uint[CanonicalWidth * CanonicalHeight];
        frameBitmap = new WriteableBitmap(
            new PixelSize(CanonicalWidth, CanonicalHeight),
            new Vector(96, 96),
            PixelFormat.Bgra8888,
            AlphaFormat.Opaque);

        fpsStopwatch = new Stopwatch();

        // Set initial bitmap
        VideoImage.Source = frameBitmap;

        // Initialize refresh timer at ~60 Hz
        refreshTimer = new DispatcherTimer(DispatcherPriority.Render)
        {
            Interval = TimeSpan.FromMilliseconds(1000.0 / TargetFrameRate),
        };
        refreshTimer.Tick += OnRefreshTimer;

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
    /// Attaches the video window to a machine instance.
    /// </summary>
    /// <param name="machine">The machine to attach to.</param>
    /// <remarks>
    /// <para>
    /// Extracts the video device, keyboard device, and memory bus from the machine
    /// for rendering and input handling.
    /// </para>
    /// </remarks>
    public void AttachMachine(IMachine machine)
    {
        ArgumentNullException.ThrowIfNull(machine);

        this.machine = machine;
        this.memoryBus = machine.Bus;
        this.videoDevice = machine.GetComponent<IVideoDevice>();
        this.keyboardDevice = machine.GetComponent<EmulatorKeyboardDevice>();

        // Get character ROM data from video device
        if (videoDevice is ICharacterRomProvider charProvider)
        {
            characterRom = charProvider.GetCharacterRomData();
        }
    }

    /// <summary>
    /// Detaches the video window from the current machine.
    /// </summary>
    public void DetachMachine()
    {
        machine = null;
        memoryBus = null;
        videoDevice = null;
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

        // Handle modifier keys
        UpdateModifiers(e);

        // Map key to Pocket2e byte code
        byte? keyCode = MapKeyToPocket2(e.Key, e.KeyModifiers);
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
    /// <returns>The Pocket2e ASCII code, or null if the key is not mapped.</returns>
    private static byte? MapKeyToPocket2(Key key, KeyModifiers modifiers)
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
            return shift ? baseCode : (byte)(baseCode + 0x20); // Lowercase if no shift
        }

        if (key >= Key.D0 && key <= Key.D9)
        {
            if (shift)
            {
                // Shifted number keys produce symbols
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

        // Punctuation
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

    private void OnWindowOpened(object? sender, EventArgs e)
    {
        UpdateWindowSize();
        fpsStopwatch.Start();
        refreshTimer.Start();
    }

    private void OnWindowClosed(object? sender, EventArgs e)
    {
        refreshTimer.Stop();
        fpsStopwatch.Stop();
    }

    private void OnRefreshTimer(object? sender, EventArgs e)
    {
        // Update flash state
        flashFrameCounter++;
        if (flashFrameCounter >= FlashToggleFrames)
        {
            flashFrameCounter = 0;
            flashState = !flashState;
        }

        // Render frame
        RenderFrame();

        // Update FPS counter
        frameCount++;
        if (fpsStopwatch.ElapsedMilliseconds >= 1000)
        {
            lastFps = frameCount / (fpsStopwatch.ElapsedMilliseconds / 1000.0);
            FpsDisplay.Text = $"{lastFps:F1} FPS";
            frameCount = 0;
            fpsStopwatch.Restart();
        }
    }

    private void RenderFrame()
    {
        if (memoryBus is null || videoDevice is null)
        {
            // No machine attached - render blank screen
            renderer.Clear(pixelBuffer);
            CommitFrameBuffer();
            return;
        }

        // Determine current video mode
        VideoMode mode = videoDevice.CurrentMode;

        // Render frame
        renderer.RenderFrame(
            pixelBuffer,
            mode,
            ReadMemoryByte,
            characterRom.Span,
            videoDevice.IsAltCharSet,
            videoDevice.IsPage2,
            flashState);

        CommitFrameBuffer();
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

    private void CommitFrameBuffer()
    {
        using var frameBuffer = frameBitmap.Lock();
        unsafe
        {
            fixed (uint* src = pixelBuffer)
            {
                var span = new ReadOnlySpan<byte>(src, pixelBuffer.Length * sizeof(uint));
                span.CopyTo(new Span<byte>((void*)frameBuffer.Address, span.Length));
            }
        }

        VideoImage.InvalidateVisual();
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

        // Left Alt → Open Apple
        // Right Alt → Closed Apple
        if (e.KeyModifiers.HasFlag(KeyModifiers.Alt))
        {
            // Avalonia doesn't distinguish left/right Alt in KeyModifiers,
            // so we map Alt to Open Apple by default
            modifiers |= KeyboardModifiers.OpenApple;
        }

        keyboardDevice.SetModifiers(modifiers);
    }
}