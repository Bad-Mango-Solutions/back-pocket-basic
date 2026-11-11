// <copyright file="VideoWindow.axaml.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.UI.Views;

using System.Diagnostics;
using System.Runtime.CompilerServices;

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;

using BadMango.Emulator.Bus.Interfaces;
using BadMango.Emulator.Devices;
using BadMango.Emulator.Rendering;

using Serilog;

using EmulatorKeyboardDevice = BadMango.Emulator.Devices.Interfaces.IKeyboardDevice;

/// <summary>
/// Video display window for the debugger console.
/// </summary>
/// <remarks>
/// <para>
/// This window displays the latest produced emulator frame from an <see cref="IVideoFrameSource"/>.
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

    private readonly ILogger logger;
    private readonly PixelBuffer pixelBuffer;
    private readonly DispatcherTimer fpsTimer;
    private readonly Stopwatch fpsStopwatch;

    private IVideoFrameSource? frameSource;
    private EmulatorKeyboardDevice? keyboardDevice;
    private int vblankPending;
    private volatile bool isClosed;

    private int scale = 2;
    private bool showFps;
    private DisplayColorMode colorMode = DisplayColorMode.Color;
    private int frameCount;
    private int vblankCount;
    private int uiFrameCount;
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
            if (frameSource is not null)
            {
                frameSource.ColorMode = value;
            }

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
    /// Resolves a machine-scoped frame source so frame production continues independently
    /// of this window's lifetime.
    /// </para>
    /// <para>
    /// Also attaches keyboard input handling for this window instance.
    /// </para>
    /// </remarks>
    public void AttachMachine(IMachine machine)
    {
        ArgumentNullException.ThrowIfNull(machine);
        if (isClosed)
        {
            return;
        }

        // Always detach first so reattachment is idempotent and cannot duplicate handlers.
        DetachMachine();

        this.keyboardDevice = machine.GetComponent<EmulatorKeyboardDevice>();
        AttachFrameSource(MachineVideoFrameSourceRegistry.GetOrCreate(machine));
        attachmentDiagnostic = CreateAttachmentDiagnostic(frameSource);
    }

    /// <summary>
    /// Detaches the video window from the current machine.
    /// </summary>
    public void DetachMachine()
    {
        if (frameSource is not null)
        {
            frameSource.FrameReady -= OnFrameReady;
        }

        frameSource = null;
        keyboardDevice = null;
        Interlocked.Exchange(ref vblankPending, 0);
        attachmentDiagnostic = "ATT detached";
    }

    /// <summary>
    /// Forces an immediate redraw of the video display.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Requests a fresh frame from the attached frame source and presents it.
    /// </para>
    /// <para>
    /// When already on the UI thread (for example <c>video refresh</c>, which calls
    /// this from inside <c>Dispatcher.UIThread.InvokeAsync</c>), presentation runs
    /// inline. Off the UI thread the work is marshaled with
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

        frameSource?.ForceRender();
        if (Dispatcher.UIThread.CheckAccess())
        {
            PresentLatestFrame();
            return;
        }

        _ = Dispatcher.UIThread.InvokeAsync(PresentLatestFrame);
    }

    /// <summary>
    /// Attaches a read-only frame source to this window.
    /// </summary>
    /// <param name="source">The frame source to present.</param>
    internal void AttachFrameSource(IVideoFrameSource source)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (isClosed)
        {
            return;
        }

        if (ReferenceEquals(frameSource, source))
        {
            return;
        }

        if (frameSource is not null)
        {
            frameSource.FrameReady -= OnFrameReady;
        }

        frameSource = source;
        frameSource.ColorMode = colorMode;
        frameSource.FrameReady += OnFrameReady;
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

        // Map the Avalonia key to a physical host key, then let the machine's keyboard translate it.
        byte? keyCode = TranslateKey(e.Key, e.KeyModifiers, capsLockActive);
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

    private static bool IsCriticalException(Exception ex)
    {
        return ex is OutOfMemoryException
            or AccessViolationException
            or InvalidProgramException
            or BadImageFormatException;
    }

    /// <summary>
    /// Maps an Avalonia key to the physical <see cref="HostKey"/> it represents.
    /// </summary>
    /// <param name="key">The Avalonia key code.</param>
    /// <returns>The host key, or <see cref="HostKey.None"/> if the key has no host-key equivalent.</returns>
    private static HostKey MapToHostKey(Key key)
    {
        if (key is >= Key.A and <= Key.Z)
        {
            return HostKey.A + (key - Key.A);
        }

        if (key is >= Key.D0 and <= Key.D9)
        {
            return HostKey.D0 + (key - Key.D0);
        }

        return key switch
        {
            Key.OemMinus => HostKey.Minus,
            Key.OemPlus => HostKey.Equals,
            Key.OemOpenBrackets => HostKey.OpenBracket,
            Key.OemCloseBrackets => HostKey.CloseBracket,
            Key.OemBackslash or Key.OemPipe => HostKey.Backslash,
            Key.OemSemicolon => HostKey.Semicolon,
            Key.OemQuotes => HostKey.Quote,
            Key.OemComma => HostKey.Comma,
            Key.OemPeriod => HostKey.Period,
            Key.OemQuestion => HostKey.Slash,
            Key.OemTilde => HostKey.Backtick,
            Key.Space => HostKey.Space,
            Key.Return => HostKey.Return,
            Key.Tab => HostKey.Tab,
            Key.Escape => HostKey.Escape,
            Key.Back => HostKey.Backspace,
            Key.Delete => HostKey.Delete,
            Key.Left => HostKey.Left,
            Key.Right => HostKey.Right,
            Key.Up => HostKey.Up,
            Key.Down => HostKey.Down,
            Key.LeftShift => HostKey.LeftShift,
            Key.RightShift => HostKey.RightShift,
            Key.LeftCtrl => HostKey.LeftControl,
            Key.RightCtrl => HostKey.RightControl,
            Key.LeftAlt => HostKey.LeftAlt,
            Key.RightAlt => HostKey.RightAlt,
            Key.CapsLock => HostKey.CapsLock,
            _ => HostKey.None,
        };
    }

    /// <summary>
    /// Translates an Avalonia key press into the attached keyboard's key code.
    /// </summary>
    /// <param name="key">The Avalonia key code.</param>
    /// <param name="modifiers">The active Avalonia key modifiers.</param>
    /// <param name="capsLock">Whether Caps Lock is active.</param>
    /// <returns>The key code, or <see langword="null"/> if the key produces no code.</returns>
    private byte? TranslateKey(Key key, KeyModifiers modifiers, bool capsLock)
    {
        if (keyboardDevice is null)
        {
            return null;
        }

        HostKey hostKey = MapToHostKey(key);
        if (hostKey == HostKey.None)
        {
            return null;
        }

        var keyboardModifiers = KeyboardModifiers.None;
        if (modifiers.HasFlag(KeyModifiers.Shift))
        {
            keyboardModifiers |= KeyboardModifiers.Shift;
        }

        if (modifiers.HasFlag(KeyModifiers.Control))
        {
            keyboardModifiers |= KeyboardModifiers.Control;
        }

        if (capsLock)
        {
            keyboardModifiers |= KeyboardModifiers.CapsLock;
        }

        return keyboardDevice.Translator.Translate(hostKey, keyboardModifiers);
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

    private void OnFrameReady()
    {
        if (isClosed)
        {
            return;
        }

        Interlocked.Increment(ref vblankCount);

        if (Interlocked.CompareExchange(ref vblankPending, 1, 0) == 0)
        {
            Dispatcher.UIThread.InvokeAsync(ProcessVBlankFrame, VBlankUiPriority);
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

        try
        {
            PresentLatestFrame();
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

    private void PresentLatestFrame()
    {
        if (isClosed)
        {
            return;
        }

        var pixels = pixelBuffer.GetPixels();
        if (frameSource is null || !frameSource.TryCopyLatestFrame(pixels, out _))
        {
            pixels.Clear();
        }

        pixelBuffer.Commit();
        VideoImage.InvalidateVisual();
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

    private string CreateAttachmentDiagnostic(IVideoFrameSource? source)
    {
        int windowId = RuntimeHelpers.GetHashCode(this);
        int sourceId = source is null ? 0 : RuntimeHelpers.GetHashCode(source);
        return $"ATT vw:{windowId} fs:{sourceId} attached:{source is not null}";
    }
}