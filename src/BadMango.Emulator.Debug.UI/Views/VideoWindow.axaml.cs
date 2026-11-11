// <copyright file="VideoWindow.axaml.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.UI.Views;

using System.Diagnostics;
using System.Runtime.CompilerServices;

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;

using BadMango.Emulator.Bus;
using BadMango.Emulator.Bus.Interfaces;
using BadMango.Emulator.Devices;
using BadMango.Emulator.Devices.Interfaces;
using BadMango.Emulator.Rendering;

using Serilog;

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

    private readonly ILogger logger;
    private readonly IVideoRenderer renderer;
    private readonly PixelBuffer pixelBuffer;
    private readonly DispatcherTimer fpsTimer;
    private readonly Stopwatch fpsStopwatch;

    // Snapshot of display memory ($0400-$5FFF, main and aux) captured at VBlank start.
    // Rendering reads only from this snapshot, so the image never depends on the
    // CPU's MMU state (RAMRD/80STORE/PAGE2) or on half-updated memory.
    private readonly DisplayMemorySnapshot displaySnapshot = new();
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
    private volatile bool isClosed;

    private int scale = 2;
    private bool showFps;
    private DisplayColorMode colorMode = DisplayColorMode.Color;
    private int frameCount;
    private int vblankCount;
    private int uiFrameCount;
    private int flashFrameCounter;
    private bool flashState;
    private double lastFps;
    private string attachmentDiagnostic = "ATT detached";

    // Track Left/Right Alt state separately since Avalonia's KeyModifiers.Alt doesn't distinguish
    private bool leftAltPressed;
    private bool rightAltPressed;
    private bool capsLockActive;

    /// <summary>
    /// Initializes a new instance of the <see cref="VideoWindow"/> class.
    /// </summary>
    public VideoWindow()
        : this(null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="VideoWindow"/> class.
    /// </summary>
    /// <param name="logger">The logger to use for video-window errors.</param>
    public VideoWindow(ILogger? logger)
    {
        this.logger = logger ?? Serilog.Core.Logger.None;

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
    /// <value>The color mode (Green, Amber, White, or Color). Default is Color.</value>
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
    /// Dispatcher priority used to marshal VBlank frames onto the UI thread.
    /// </summary>
    /// <remarks>
    /// Avalonia 12.1.5 introduced a dedicated render-priority band
    /// (<c>UiThreadRender</c> / <c>AfterRender</c> / <c>BeforeRender</c>).
    /// Posting at <see cref="DispatcherPriority.Render"/> was observed to leave
    /// <c>ProcessVBlankFrame</c> unrun (FPS overlay stuck at 0.0 from the Background
    /// timer) while the CPU, disk, and speaker kept running. <see cref="DispatcherPriority.Normal"/>
    /// is processed on a regular dispatcher pump. The frame still presents via
    /// <c>InvalidateVisual</c> after <see cref="PixelBuffer.Commit"/>.
    /// </remarks>
    internal static DispatcherPriority VBlankUiPriority { get; } = DispatcherPriority.Normal;

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
        if (isClosed)
        {
            return;
        }

        var previousVideoDevice = videoDevice;

        // Always detach first so reattachment is idempotent and cannot duplicate handlers.
        DetachMachine();

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

        attachmentDiagnostic = CreateAttachmentDiagnostic(previousVideoDevice, videoDevice);
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
        Interlocked.Exchange(ref vblankPending, 0);
        characterRomUpdatePending = false;
        attachmentDiagnostic = "ATT detached";

        lock (snapshotLock)
        {
            displaySnapshot.Clear();
        }
    }

    /// <summary>
    /// Forces an immediate redraw of the video display.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A fresh display memory snapshot is captured from physical RAM before rendering,
    /// so the redraw reflects current memory even if no VBlank has occurred recently.
    /// </para>
    /// <para>
    /// When already on the UI thread (for example <c>video refresh</c>, which calls
    /// this from inside <c>Dispatcher.UIThread.InvokeAsync</c>), the capture and
    /// render run inline. Off the UI thread the work is marshaled with
    /// <c>InvokeAsync</c>; failures are surfaced through the dispatcher and logged
    /// by the window logger. This method does not return a <c>Task</c>, so callers
    /// cannot await or catch exceptions from the marshaled path.
    /// </para>
    /// </remarks>
    public void ForceRedraw()
    {
        if (isClosed)
        {
            return;
        }

        if (Dispatcher.UIThread.CheckAccess())
        {
            RedrawFromCurrentMemory();
            return;
        }

        _ = Dispatcher.UIThread.InvokeAsync(RedrawFromCurrentMemory);
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
        var effectiveModifiers = KeyboardModifiers.None;
        if (modifiers.HasFlag(KeyModifiers.Shift))
        {
            effectiveModifiers |= KeyboardModifiers.Shift;
        }

        if (modifiers.HasFlag(KeyModifiers.Control))
        {
            effectiveModifiers |= KeyboardModifiers.Control;
        }

        if (capsLock)
        {
            effectiveModifiers |= KeyboardModifiers.CapsLock;
        }

        var translated = key switch
        {
            Key.A => AppleIIeKeyTranslator.AppleIIeKey.A,
            Key.B => AppleIIeKeyTranslator.AppleIIeKey.B,
            Key.C => AppleIIeKeyTranslator.AppleIIeKey.C,
            Key.D => AppleIIeKeyTranslator.AppleIIeKey.D,
            Key.E => AppleIIeKeyTranslator.AppleIIeKey.E,
            Key.F => AppleIIeKeyTranslator.AppleIIeKey.F,
            Key.G => AppleIIeKeyTranslator.AppleIIeKey.G,
            Key.H => AppleIIeKeyTranslator.AppleIIeKey.H,
            Key.I => AppleIIeKeyTranslator.AppleIIeKey.I,
            Key.J => AppleIIeKeyTranslator.AppleIIeKey.J,
            Key.K => AppleIIeKeyTranslator.AppleIIeKey.K,
            Key.L => AppleIIeKeyTranslator.AppleIIeKey.L,
            Key.M => AppleIIeKeyTranslator.AppleIIeKey.M,
            Key.N => AppleIIeKeyTranslator.AppleIIeKey.N,
            Key.O => AppleIIeKeyTranslator.AppleIIeKey.O,
            Key.P => AppleIIeKeyTranslator.AppleIIeKey.P,
            Key.Q => AppleIIeKeyTranslator.AppleIIeKey.Q,
            Key.R => AppleIIeKeyTranslator.AppleIIeKey.R,
            Key.S => AppleIIeKeyTranslator.AppleIIeKey.S,
            Key.T => AppleIIeKeyTranslator.AppleIIeKey.T,
            Key.U => AppleIIeKeyTranslator.AppleIIeKey.U,
            Key.V => AppleIIeKeyTranslator.AppleIIeKey.V,
            Key.W => AppleIIeKeyTranslator.AppleIIeKey.W,
            Key.X => AppleIIeKeyTranslator.AppleIIeKey.X,
            Key.Y => AppleIIeKeyTranslator.AppleIIeKey.Y,
            Key.Z => AppleIIeKeyTranslator.AppleIIeKey.Z,
            Key.D0 => AppleIIeKeyTranslator.AppleIIeKey.D0,
            Key.D1 => AppleIIeKeyTranslator.AppleIIeKey.D1,
            Key.D2 => AppleIIeKeyTranslator.AppleIIeKey.D2,
            Key.D3 => AppleIIeKeyTranslator.AppleIIeKey.D3,
            Key.D4 => AppleIIeKeyTranslator.AppleIIeKey.D4,
            Key.D5 => AppleIIeKeyTranslator.AppleIIeKey.D5,
            Key.D6 => AppleIIeKeyTranslator.AppleIIeKey.D6,
            Key.D7 => AppleIIeKeyTranslator.AppleIIeKey.D7,
            Key.D8 => AppleIIeKeyTranslator.AppleIIeKey.D8,
            Key.D9 => AppleIIeKeyTranslator.AppleIIeKey.D9,
            Key.OemMinus => AppleIIeKeyTranslator.AppleIIeKey.Minus,
            Key.OemPlus => AppleIIeKeyTranslator.AppleIIeKey.Plus,
            Key.OemOpenBrackets => AppleIIeKeyTranslator.AppleIIeKey.OpenBracket,
            Key.OemCloseBrackets => AppleIIeKeyTranslator.AppleIIeKey.CloseBracket,
            Key.OemBackslash or Key.OemPipe => AppleIIeKeyTranslator.AppleIIeKey.Backslash,
            Key.OemSemicolon => AppleIIeKeyTranslator.AppleIIeKey.Semicolon,
            Key.OemQuotes => AppleIIeKeyTranslator.AppleIIeKey.Quote,
            Key.OemComma => AppleIIeKeyTranslator.AppleIIeKey.Comma,
            Key.OemPeriod => AppleIIeKeyTranslator.AppleIIeKey.Period,
            Key.OemQuestion => AppleIIeKeyTranslator.AppleIIeKey.Slash,
            Key.OemTilde => AppleIIeKeyTranslator.AppleIIeKey.Backtick,
            Key.Left => AppleIIeKeyTranslator.AppleIIeKey.Left,
            Key.Right => AppleIIeKeyTranslator.AppleIIeKey.Right,
            Key.Up => AppleIIeKeyTranslator.AppleIIeKey.Up,
            Key.Down => AppleIIeKeyTranslator.AppleIIeKey.Down,
            Key.Return => AppleIIeKeyTranslator.AppleIIeKey.Return,
            Key.Tab => AppleIIeKeyTranslator.AppleIIeKey.Tab,
            Key.Escape => AppleIIeKeyTranslator.AppleIIeKey.Escape,
            Key.Space => AppleIIeKeyTranslator.AppleIIeKey.Space,
            Key.Back => AppleIIeKeyTranslator.AppleIIeKey.Delete,
            _ => AppleIIeKeyTranslator.AppleIIeKey.None,
        };

        if (translated == AppleIIeKeyTranslator.AppleIIeKey.None)
        {
            return null;
        }

        return AppleIIeKeyTranslator.Translate(translated, effectiveModifiers);
    }

    private static bool IsCriticalException(Exception ex)
    {
        return ex is OutOfMemoryException
            or AccessViolationException
            or InvalidProgramException
            or BadImageFormatException;
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
        if (isClosed)
        {
            return;
        }

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
        isClosed = true;
        DetachMachine();
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
    /// a snapshot of the display memory (text and hi-res pages) here to prevent the
    /// renderer from seeing half-updated data or the CPU's MMU-mapped view of memory.
    /// </para>
    /// <para>
    /// The snapshot is captured synchronously before queuing the UI render, ensuring
    /// the renderer always sees a consistent view of the display memory from the
    /// exact moment of VBlank start.
    /// </para>
    /// </remarks>
    private void OnVBlankOccurred()
    {
        if (isClosed)
        {
            return;
        }

        Interlocked.Increment(ref vblankCount);

        // Capture display memory snapshot on the emulator thread while CPU is paused.
        // This is the key to preventing 80-column flicker: the firmware modifies
        // PAGE1/PAGE2 buffers between VBlanks, but we capture a stable copy here.
        // Swallow snapshot failures so they cannot unwind VideoDevice.OnVBlankStart
        // and tear down the CPU run loop.
        try
        {
            CaptureDisplaySnapshot();
        }
        catch (Exception ex) when (!IsCriticalException(ex))
        {
            logger.Error(ex, "Video window failed to capture display snapshot");
        }

        // Only queue a UI render if one isn't already pending.
        // Use Normal rather than Render: see VBlankUiPriority remarks.
        if (Interlocked.CompareExchange(ref vblankPending, 1, 0) == 0)
        {
            Dispatcher.UIThread.InvokeAsync(ProcessVBlankFrame, VBlankUiPriority);
        }
    }

    /// <summary>
    /// Captures a snapshot of the display memory from both main and auxiliary RAM.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method captures <c>$0400-$5FFF</c> (text/lo-res pages 1 and 2 and hi-res
    /// pages 1 and 2) from both main and auxiliary memory. The snapshot is taken at
    /// VBlank start when the CPU is not actively modifying video memory, ensuring the
    /// renderer sees a consistent frame.
    /// </para>
    /// <para>
    /// Both main and auxiliary memory are copied directly from their physical RAM backing
    /// stores, bypassing all soft switch mapping (RAMRD, 80STORE, PAGE2, HIRES). Real IIe
    /// video hardware fetches display data from physical RAM without going through the
    /// MMU: main RAM always, plus auxiliary RAM for 80-column text and double hi-res.
    /// Reading through the memory bus instead would, for example, show auxiliary
    /// <c>$2000-$5FFF</c> whenever a program runs with RAMRD on.
    /// </para>
    /// <para>
    /// Machines that do not expose physical main RAM fall back to side-effect-free bus reads.
    /// </para>
    /// </remarks>
    private void CaptureDisplaySnapshot()
    {
        lock (snapshotLock)
        {
            if (mainRam.Length >= DisplayMemorySnapshot.EndAddress)
            {
                displaySnapshot.CaptureMain(mainRam.Span);
            }
            else if (memoryBus is not null)
            {
                displaySnapshot.CaptureMain(ReadMemoryByte);
            }
            else
            {
                return;
            }

            if (extended80ColumnDevice is not null)
            {
                var auxRam = extended80ColumnDevice.AuxiliaryRam;
                if (auxRam.Length >= DisplayMemorySnapshot.EndAddress)
                {
                    displaySnapshot.CaptureAux(auxRam);
                }
                else
                {
                    displaySnapshot.CaptureAux(extended80ColumnDevice.ReadAuxRam);
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
        if (isClosed)
        {
            return;
        }

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

        // Render the frame, but keep the counters moving even when rendering throws.
        // This preserves a non-zero FPS and makes a stale image look like a render
        // issue instead of a stopped VBlank signal.
        try
        {
            RenderFrame();
        }
        catch (Exception ex) when (!IsCriticalException(ex))
        {
            logger.Error(ex, "Video window failed to render VBlank frame");
        }
        finally
        {
            frameCount++;
            uiFrameCount++;
        }
    }

    /// <summary>
    /// Captures a fresh snapshot and renders it, logging and rethrowing on failure.
    /// </summary>
    private void RedrawFromCurrentMemory()
    {
        if (isClosed)
        {
            return;
        }

        try
        {
            CaptureDisplaySnapshot();
            RenderFrame();
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Video window redraw failed");
            throw;
        }
    }

    private void OnFpsTimer(object? sender, EventArgs e)
    {
        if (fpsStopwatch.ElapsedMilliseconds >= 1000)
        {
            UpdateFpsDisplay(fpsStopwatch.ElapsedMilliseconds / 1000.0);
            fpsStopwatch.Restart();
        }
    }

    /// <summary>
    /// Updates FPS overlay values using per-second rates for FPS, VBlank callbacks, and UI frames.
    /// </summary>
    /// <param name="elapsedSeconds">The elapsed interval in seconds.</param>
    private void UpdateFpsDisplay(double elapsedSeconds)
    {
        if (elapsedSeconds <= 0)
        {
            return;
        }

        lastFps = frameCount / elapsedSeconds;
        var vblankFrames = Interlocked.Exchange(ref vblankCount, 0);
        var vblankRate = (int)Math.Round(vblankFrames / elapsedSeconds, MidpointRounding.AwayFromZero);
        var uiFrameRate = (int)Math.Round(uiFrameCount / elapsedSeconds, MidpointRounding.AwayFromZero);
        FpsDisplay.Text = $"{lastFps:F1} FPS  vb:{vblankRate} ui:{uiFrameRate} {attachmentDiagnostic}";
        frameCount = 0;
        uiFrameCount = 0;
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
        if (isClosed)
        {
            return;
        }

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

        if (!snapshotValid)
        {
            // No VBlank has been captured yet — take a snapshot now so that
            // rendering never reads display memory through the MMU-mapped bus.
            CaptureDisplaySnapshot();
        }

        // Lock to prevent a concurrent VBlank from overwriting the snapshot
        // while the renderer is reading from it.
        lock (snapshotLock)
        {
            renderer.RenderFrame(
                pixels,
                mode,
                displaySnapshot.ReadMain,
                characterRom.Span,
                useAltCharSet,
                videoDevice.IsPage2,
                flashState,
                noFlash1,
                noFlash2,
                colorMode,
                extended80ColumnDevice is not null ? displaySnapshot.ReadAux : null);
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

    private string CreateAttachmentDiagnostic(IVideoDevice? previousDevice, IVideoDevice? currentDevice)
    {
        int windowId = RuntimeHelpers.GetHashCode(this);
        int deviceId = currentDevice is null ? 0 : RuntimeHelpers.GetHashCode(currentDevice);
        bool hasVideoDevice = currentDevice is not null;
        bool replacedDevice = previousDevice is not null && !ReferenceEquals(previousDevice, currentDevice);
        return $"ATT vw:{windowId} vd:{deviceId} hasvd:{hasVideoDevice} replaced:{replacedDevice}";
    }
}