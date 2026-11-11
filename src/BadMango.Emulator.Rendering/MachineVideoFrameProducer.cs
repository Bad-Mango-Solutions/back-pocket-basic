// <copyright file="MachineVideoFrameProducer.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Rendering;

using BadMango.Emulator.Bus;
using BadMango.Emulator.Bus.Interfaces;
using BadMango.Emulator.Devices;
using BadMango.Emulator.Devices.Interfaces;

/// <summary>
/// Produces rendered video frames from machine VBlank events independently from any UI window.
/// </summary>
public sealed class MachineVideoFrameProducer : IVideoFrameSource
{
    private const int FlashToggleFrames = 16;

    private readonly IVideoRenderer renderer;
    private readonly DisplayMemorySnapshot displaySnapshot = new();
    private readonly object syncLock = new();
    private uint[] frontBuffer;
    private uint[] backBuffer;

    private IMemoryBus? memoryBus;
    private IVideoDevice? videoDevice;
    private ICharacterDevice? characterDevice;
    private IExtended80ColumnDevice? extended80ColumnDevice;
    private ReadOnlyMemory<byte> mainRam;
    private Memory<byte> characterRom;
    private bool characterRomUpdatePending;
    private bool flashState;
    private int flashFrameCounter;
    private long frameSequence;
    private bool hasFrame;
    private bool attached;
    private DisplayColorMode colorMode = DisplayColorMode.Color;
    private VideoFrameMetadata metadata;

    /// <summary>
    /// Initializes a new instance of the <see cref="MachineVideoFrameProducer"/> class.
    /// </summary>
    public MachineVideoFrameProducer()
        : this(new Pocket2VideoRenderer())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MachineVideoFrameProducer"/> class.
    /// </summary>
    /// <param name="renderer">The renderer to use.</param>
    public MachineVideoFrameProducer(IVideoRenderer renderer)
    {
        ArgumentNullException.ThrowIfNull(renderer);
        this.renderer = renderer;
        frontBuffer = new uint[this.renderer.CanonicalWidth * this.renderer.CanonicalHeight];
        backBuffer = new uint[frontBuffer.Length];
        characterRom = DefaultCharacterRom.GetRomData();
    }

    /// <inheritdoc />
    public event Action? FrameReady;

    /// <inheritdoc />
    public int Width => renderer.CanonicalWidth;

    /// <inheritdoc />
    public int Height => renderer.CanonicalHeight;

    /// <inheritdoc />
    public DisplayColorMode ColorMode
    {
        get
        {
            lock (syncLock)
            {
                return colorMode;
            }
        }

        set
        {
            lock (syncLock)
            {
                colorMode = value;
            }
        }
    }

    /// <summary>
    /// Attaches this producer to a machine.
    /// </summary>
    /// <param name="machine">Machine to attach.</param>
    public void AttachMachine(IMachine machine)
    {
        ArgumentNullException.ThrowIfNull(machine);

        lock (syncLock)
        {
            DetachMachineCore();

            memoryBus = machine.Bus;
            videoDevice = machine.GetComponent<IVideoDevice>();
            characterDevice = machine.GetComponent<ICharacterDevice>();
            extended80ColumnDevice = machine.GetComponent<IExtended80ColumnDevice>();
            var mainMemoryProvider = machine.GetComponent<IMainMemoryProvider>();
            mainRam = mainMemoryProvider?.MainRam ?? ReadOnlyMemory<byte>.Empty;
            ICharacterRomProvider? characterRomProvider = characterDevice ?? machine.GetComponent<ICharacterRomProvider>();
            characterRom = characterRomProvider is not null && characterRomProvider.IsCharacterRomLoaded
                ? characterRomProvider.GetCharacterRomData()
                : DefaultCharacterRom.GetRomData();

            if (characterDevice is not null)
            {
                characterDevice.CharacterRomChanged += OnCharacterRomChanged;
            }

            if (videoDevice is not null)
            {
                videoDevice.VBlankOccurred += OnVBlankOccurred;
            }

            attached = true;
            hasFrame = false;
            displaySnapshot.Clear();
        }
    }

    /// <summary>
    /// Detaches this producer from the current machine.
    /// </summary>
    public void DetachMachine()
    {
        lock (syncLock)
        {
            DetachMachineCore();
        }
    }

    /// <inheritdoc />
    public bool TryCopyLatestFrame(Span<uint> destination, out VideoFrameMetadata metadata)
    {
        if (destination.Length < frontBuffer.Length)
        {
            throw new ArgumentException("Destination is too small for the frame.", nameof(destination));
        }

        lock (syncLock)
        {
            if (!hasFrame)
            {
                metadata = default;
                return false;
            }

            frontBuffer.AsSpan().CopyTo(destination);
            metadata = this.metadata;
            return true;
        }
    }

    /// <inheritdoc />
    public void ForceRender()
    {
        ProduceFrame(advanceFlashState: false, raiseReadyEvent: false);
    }

    private static bool IsCriticalException(Exception ex)
    {
        return ex is OutOfMemoryException
            or AccessViolationException
            or InvalidProgramException
            or BadImageFormatException;
    }

    private void OnCharacterRomChanged()
    {
        lock (syncLock)
        {
            if (!attached)
            {
                return;
            }

            characterRomUpdatePending = true;
        }
    }

    private void OnVBlankOccurred()
    {
        ProduceFrame(advanceFlashState: true, raiseReadyEvent: true);
    }

    private void ProduceFrame(bool advanceFlashState, bool raiseReadyEvent)
    {
        try
        {
            lock (syncLock)
            {
                if (!attached)
                {
                    return;
                }

                CaptureDisplaySnapshot();
                if (characterRomUpdatePending)
                {
                    characterRomUpdatePending = false;
                    ReloadCharacterRom();
                }

                if (advanceFlashState)
                {
                    flashFrameCounter++;
                    if (flashFrameCounter >= FlashToggleFrames)
                    {
                        flashFrameCounter = 0;
                        flashState = !flashState;
                    }
                }

                var pixels = backBuffer.AsSpan();
                if (memoryBus is null || videoDevice is null)
                {
                    renderer.Clear(pixels);
                    metadata = new VideoFrameMetadata(
                        frameSequence + 1,
                        VideoMode.Text40,
                        false,
                        false,
                        flashState,
                        false,
                        true);
                }
                else
                {
                    VideoMode mode = videoDevice.CurrentMode;
                    bool useAltCharSet = characterDevice?.IsAltCharSet ?? false;
                    bool noFlash1 = characterDevice?.IsNoFlash1Enabled ?? false;
                    bool noFlash2 = characterDevice?.IsNoFlash2Enabled ?? true;
                    bool isPage2 = videoDevice.IsPage2;

                    renderer.RenderFrame(
                        pixels,
                        mode,
                        displaySnapshot.ReadMain,
                        characterRom.Span,
                        useAltCharSet,
                        isPage2,
                        flashState,
                        noFlash1,
                        noFlash2,
                        colorMode,
                        extended80ColumnDevice is not null ? displaySnapshot.ReadAux : null);

                    metadata = new VideoFrameMetadata(
                        frameSequence + 1,
                        mode,
                        isPage2,
                        useAltCharSet,
                        flashState,
                        noFlash1,
                        noFlash2);
                }

                (frontBuffer, backBuffer) = (backBuffer, frontBuffer);
                frameSequence++;
                hasFrame = true;
            }

            if (raiseReadyEvent)
            {
                FrameReady?.Invoke();
            }
        }
        catch (Exception ex) when (!IsCriticalException(ex))
        {
            // Keep frame production failures from unwinding the emulator VBlank callback.
        }
    }

    private void CaptureDisplaySnapshot()
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
            displaySnapshot.Clear();
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
    }

    private void ReloadCharacterRom()
    {
        if (characterDevice is not null && characterDevice.IsCharacterRomLoaded)
        {
            characterRom = characterDevice.GetCharacterRomData();
        }
    }

    private byte ReadMemoryByte(ushort address)
    {
        if (memoryBus is null)
        {
            return 0;
        }

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

    private void DetachMachineCore()
    {
        if (characterDevice is not null)
        {
            characterDevice.CharacterRomChanged -= OnCharacterRomChanged;
        }

        if (videoDevice is not null)
        {
            videoDevice.VBlankOccurred -= OnVBlankOccurred;
        }

        memoryBus = null;
        videoDevice = null;
        characterDevice = null;
        extended80ColumnDevice = null;
        mainRam = ReadOnlyMemory<byte>.Empty;
        characterRom = DefaultCharacterRom.GetRomData();
        characterRomUpdatePending = false;
        flashState = false;
        flashFrameCounter = 0;
        frameSequence = 0;
        hasFrame = false;
        attached = false;
        metadata = default;
        displaySnapshot.Clear();
        Array.Clear(frontBuffer);
        Array.Clear(backBuffer);
    }
}