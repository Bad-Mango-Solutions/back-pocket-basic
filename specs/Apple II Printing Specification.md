# Apple II Printing Specification

## Document Information

| Field        | Value                                              |
|--------------|----------------------------------------------------|
| Version      | 1.0                                                |
| Date         | 2025-12-28                                         |
| Status       | Initial Draft                                      |
| Applies To   | Pocket2e, Pocket2c, PocketGS                       |

---

## 1. Overview

Printing on the Apple II family evolved from simple parallel interfaces to sophisticated
GS/OS printer drivers. This specification covers the common printer interfaces and how
to emulate them.

### 1.1 Printer Interface History

| Era       | Interface Type        | Common Cards/Ports          |
|-----------|-----------------------|-----------------------------|
| 1977-1983 | Parallel (Centronics) | Apple Parallel Interface    |
| 1981-1993 | Serial (RS-232)       | Super Serial Card, IIc/IIgs |
| 1984-1993 | AppleTalk (LocalTalk) | IIgs, AppleTalk card        |
| 1986-1993 | ImageWriter II        | Serial/AppleTalk            |

### 1.2 Common Printers

| Printer           | Interface    | Protocol                    |
|-------------------|--------------|------------------------------|
| Epson MX-80       | Parallel     | ESC/P                       |
| ImageWriter       | Serial       | ImageWriter protocol        |
| ImageWriter II    | Serial/AT    | ImageWriter protocol        |
| LaserWriter       | AppleTalk    | PostScript                  |

---

## 2. Parallel Printer Interface

The Apple Parallel Interface Card (and compatibles) provides a standard Centronics parallel
interface.

### 2.1 Hardware Description

| Signal    | Direction | Description                         |
|-----------|-----------|-------------------------------------|
| D0-D7     | Output    | 8-bit data bus                      |
| STROBE    | Output    | Data strobe (active low)            |
| ACK       | Input     | Acknowledge from printer            |
| BUSY      | Input     | Printer busy                        |
| PE        | Input     | Paper empty                         |
| SELECT    | Input     | Printer selected                    |
| ERROR     | Input     | Printer error                       |

### 2.2 Memory-Mapped Interface

For parallel interface in slot 1 ($C090 base):

| Offset | Register         | Access | Description                    |
|--------|------------------|--------|--------------------------------|
| +$00   | Data Out         | W      | Data to send to printer        |
| +$01   | Strobe           | W      | Assert strobe (write anything) |
| +$02   | Status           | R      | Printer status bits            |

### 2.3 Status Register

| Bit | Signal   | Meaning when set                       |
|-----|----------|----------------------------------------|
| 7   | BUSY     | Printer is busy (do not send data)     |
| 6   | ACK      | Acknowledge (data received)            |
| 5   | PE       | Paper empty                            |
| 4   | SELECT   | Printer is selected/online             |
| 3   | ERROR    | Printer error                          |
| 2-0 | Reserved | Always 0                               |

### 2.4 Printing Protocol

```csharp
public void PrintCharacter(byte ch)
{
    // Wait for printer ready
    while (IsBusy())
        WaitOneCycle();
    
    // Send data
    WriteData(ch);
    
    // Assert strobe
    WriteStrobe();
    
    // Wait for acknowledge
    while (!IsAcknowledged())
        WaitOneCycle();
}

public bool IsBusy() => (ReadStatus() & 0x80) != 0;
public bool IsAcknowledged() => (ReadStatus() & 0x40) != 0;
```

---

## 3. Serial Printer Interface

Most later printers use serial communication via the Super Serial Card or built-in ports.

### 3.1 ImageWriter Protocol

The ImageWriter family uses a proprietary but well-documented protocol.

#### 3.1.1 Escape Sequences

| Sequence    | Function                                |
|-------------|-----------------------------------------|
| ESC !       | Enable underline                        |
| ESC "       | Disable underline                       |
| ESC $       | Enable boldface                         |
| ESC %       | Disable boldface                        |
| ESC (       | Enable subscript                        |
| ESC )       | Disable subscript                       |
| ESC *       | Enable superscript                      |
| ESC +       | Disable superscript                     |
| ESC 4       | Enable italic                           |
| ESC 5       | Disable italic                          |
| ESC A       | Page feed                               |
| ESC a n1 n2 | Set absolute horizontal position        |
| ESC c       | Reset to default settings               |
| ESC G nnnn  | Graphics line (n bytes of data)         |
| ESC L nnnn  | Set left margin                         |
| ESC n       | Set line spacing (n/144 inch)           |
| ESC N       | Set line spacing (n/72 inch)            |
| ESC P       | Pica pitch (10 cpi)                     |
| ESC E       | Elite pitch (12 cpi)                    |
| ESC q       | Condensed pitch (17 cpi)                |
| ESC p       | Proportional spacing                    |

#### 3.1.2 Graphics Mode

ImageWriter graphics use a dot-addressable format:

```
ESC G nnnn <data>

Where:
  nnnn = Number of data bytes (2 bytes, little-endian)
  <data> = Column data, 8 dots per byte (MSB = top)
```

### 3.2 Epson ESC/P Protocol

Epson and compatible printers use the ESC/P control language.

#### 3.2.1 Common Escape Sequences

| Sequence    | Function                                |
|-------------|-----------------------------------------|
| ESC @       | Reset printer                           |
| ESC E       | Enable emphasized (bold)                |
| ESC F       | Disable emphasized                      |
| ESC 4       | Enable italic                           |
| ESC 5       | Disable italic                          |
| ESC -1      | Enable underline                        |
| ESC -0      | Disable underline                       |
| ESC K n1 n2 | Single-density graphics (n bytes)       |
| ESC L n1 n2 | Double-density graphics (n bytes)       |
| ESC * m n1 n2 | Select graphics mode m                |
| CR          | Carriage return                         |
| LF          | Line feed                               |
| FF          | Form feed                               |

---

## 4. Printer Interface Implementation

```csharp
/// <summary>
/// Interface for printer emulation.
/// </summary>
public interface IPrinter : IPeripheral
{
    /// <summary>Gets the printer name/model.</summary>
    string Name { get; }
    
    /// <summary>Gets whether the printer is online/ready.</summary>
    bool IsOnline { get; }
    
    /// <summary>Gets whether the printer is busy processing.</summary>
    bool IsBusy { get; }
    
    /// <summary>Gets whether the printer has paper.</summary>
    bool HasPaper { get; }
    
    /// <summary>Gets whether there is an error condition.</summary>
    bool HasError { get; }
    
    /// <summary>Sends a byte to the printer.</summary>
    void SendByte(byte data);
    
    /// <summary>Resets the printer to default state.</summary>
    void Reset();
    
    /// <summary>Ejects the current page.</summary>
    void FormFeed();
    
    /// <summary>Gets the current page as an image.</summary>
    ReadOnlySpan<byte> GetPageImage();
    
    /// <summary>Raised when a page is complete.</summary>
    event Action<ReadOnlyMemory<byte>>? PageComplete;
}

/// <summary>
/// Interface for a parallel printer port.
/// </summary>
public interface IParallelPort : IPeripheral
{
    /// <summary>Gets the connected printer, if any.</summary>
    IPrinter? Printer { get; set; }
    
    /// <summary>Writes data to the port.</summary>
    void WriteData(byte data);
    
    /// <summary>Strobes the data (signals data ready).</summary>
    void Strobe();
    
    /// <summary>Reads the status register.</summary>
    byte ReadStatus();
}
```

---

## 5. Virtual Printer Implementations

### 5.1 PDF Printer

Captures print output and generates PDF documents.

```csharp
/// <summary>
/// Virtual printer that generates PDF output.
/// </summary>
public class PdfPrinter : IPrinter
{
    private readonly List<byte> _pageData = new();
    private int _currentX, _currentY;
    private bool _bold, _italic, _underline;
    
    public string Name => "PDF Printer";
    public bool IsOnline => true;
    public bool IsBusy => false;
    public bool HasPaper => true;
    public bool HasError => false;
    
    public event Action<ReadOnlyMemory<byte>>? PageComplete;
    
    public void SendByte(byte data)
    {
        if (data == 0x1B)  // ESC
        {
            ProcessEscapeSequence();
        }
        else if (data == 0x0C)  // FF
        {
            FormFeed();
        }
        else if (data == 0x0D)  // CR
        {
            _currentX = 0;
        }
        else if (data == 0x0A)  // LF
        {
            _currentY += LineHeight;
        }
        else
        {
            AddCharacter((char)data);
        }
    }
    
    public void FormFeed()
    {
        // Generate PDF page
        var pdf = GeneratePdf();
        PageComplete?.Invoke(pdf);
        _pageData.Clear();
        _currentX = 0;
        _currentY = 0;
    }
    
    private void AddCharacter(char ch)
    {
        // Add character to current position
        _pageData.Add((byte)ch);
        _currentX += CharacterWidth;
    }
    
    // ... PDF generation code ...
}
```

### 5.2 Text File Printer

Simple printer that captures text output to a file.

```csharp
/// <summary>
/// Virtual printer that writes text to a file.
/// </summary>
public class TextFilePrinter : IPrinter
{
    private readonly StreamWriter _writer;
    private bool _escapeMode;
    
    public string Name => "Text File";
    public bool IsOnline => true;
    public bool IsBusy => false;
    public bool HasPaper => true;
    public bool HasError => false;
    
    public event Action<ReadOnlyMemory<byte>>? PageComplete;
    
    public TextFilePrinter(string filePath)
    {
        _writer = new StreamWriter(filePath, append: true);
    }
    
    public void SendByte(byte data)
    {
        if (_escapeMode)
        {
            // Skip escape sequences for text output
            if (!IsEscapeSequenceContinuation(data))
                _escapeMode = false;
            return;
        }
        
        if (data == 0x1B)  // ESC
        {
            _escapeMode = true;
            return;
        }
        
        // Convert control codes
        switch (data)
        {
            case 0x0D:  // CR
                // Ignore (LF handles line ending)
                break;
            case 0x0A:  // LF
                _writer.WriteLine();
                break;
            case 0x0C:  // FF
                _writer.WriteLine("\f");
                break;
            default:
                if (data >= 0x20)
                    _writer.Write((char)data);
                break;
        }
    }
    
    public void FormFeed()
    {
        _writer.Flush();
        PageComplete?.Invoke(ReadOnlyMemory<byte>.Empty);
    }
    
    public void Reset()
    {
        _escapeMode = false;
    }
    
    public void Dispose()
    {
        _writer.Dispose();
    }
}
```

### 5.3 ImageWriter Emulator

Accurate ImageWriter II emulation with graphics support.

```csharp
/// <summary>
/// ImageWriter II emulation with graphics support.
/// </summary>
public class ImageWriterEmulator : IPrinter
{
    private const int PageWidth = 960;   // 8" at 120 dpi
    private const int PageHeight = 1320; // 11" at 120 dpi
    
    private readonly byte[] _pageBuffer;
    private int _headX, _headY;
    private bool _graphicsMode;
    private int _graphicsBytes;
    private int _lineSpacing = 12;  // 12/144" = 1/12"
    
    public ImageWriterEmulator()
    {
        _pageBuffer = new byte[PageWidth * PageHeight / 8];
    }
    
    public void SendByte(byte data)
    {
        if (_graphicsMode)
        {
            ProcessGraphicsData(data);
            return;
        }
        
        if (_escapeSequence != null)
        {
            ProcessEscapeSequence(data);
            return;
        }
        
        switch (data)
        {
            case 0x1B:  // ESC
                _escapeSequence = new List<byte>();
                break;
            case 0x0D:  // CR
                _headX = 0;
                break;
            case 0x0A:  // LF
                _headY += _lineSpacing;
                if (_headY >= PageHeight)
                    FormFeed();
                break;
            case 0x0C:  // FF
                FormFeed();
                break;
            default:
                PrintCharacter(data);
                break;
        }
    }
    
    private void ProcessGraphicsData(byte data)
    {
        // Each byte is 8 vertical dots
        for (int i = 0; i < 8; i++)
        {
            if ((data & (0x80 >> i)) != 0)
                SetPixel(_headX, _headY + i);
        }
        _headX++;
        _graphicsBytes--;
        
        if (_graphicsBytes <= 0)
            _graphicsMode = false;
    }
    
    private void SetPixel(int x, int y)
    {
        if (x < 0 || x >= PageWidth || y < 0 || y >= PageHeight)
            return;
        
        int offset = (y * PageWidth + x) / 8;
        int bit = 7 - (x % 8);
        _pageBuffer[offset] |= (byte)(1 << bit);
    }
    
    // ... character rendering, escape sequence handling ...
}
```

---

## 6. Print Spooling

### 6.1 Print Queue Interface

```csharp
/// <summary>
/// Print spooling service.
/// </summary>
public interface IPrintSpooler
{
    /// <summary>Adds a job to the queue.</summary>
    Guid AddJob(string name, IPrinter printer, ReadOnlyMemory<byte> data);
    
    /// <summary>Gets pending jobs.</summary>
    IReadOnlyList<PrintJob> PendingJobs { get; }
    
    /// <summary>Cancels a pending job.</summary>
    bool CancelJob(Guid jobId);
    
    /// <summary>Gets the status of a job.</summary>
    PrintJobStatus GetJobStatus(Guid jobId);
    
    /// <summary>Raised when a job completes.</summary>
    event Action<Guid, PrintJobResult>? JobCompleted;
}

/// <summary>
/// Print job information.
/// </summary>
public record PrintJob(
    Guid Id,
    string Name,
    DateTime Submitted,
    int ByteCount,
    PrintJobStatus Status
);

/// <summary>
/// Print job status.
/// </summary>
public enum PrintJobStatus
{
    Pending,
    Printing,
    Completed,
    Cancelled,
    Error
}
```

---

## 7. ProDOS Printer Driver

### 7.1 Printer Driver Interface

ProDOS communicates with printers through a standardized driver interface:

| Function | Vector        | Description                      |
|----------|---------------|----------------------------------|
| INIT     | Driver+$00    | Initialize printer               |
| OUTPUT   | Driver+$03    | Send byte to printer             |
| STATUS   | Driver+$06    | Get printer status               |

### 7.2 Slot-Based Access

```assembly
; Send character to printer in slot 1
PR#1_ENTRY  = $C100
            LDA CharToPrint
            JSR PR#1_ENTRY
```

### 7.3 Printer.Setup

GS/OS provides a Printer Toolbox for high-level printing:

```assembly
; GS/OS Printer Manager calls
PMBootInit     = $1F00
PMStartup      = $1F01
PMShutDown     = $1F02
PMVersion      = $1F03
PMStatus       = $1F05
PMOpen         = $1F09
PMClose        = $1F0A
PMWrite        = $1F0D
PMControl      = $1F0F
```

---

## 8. AppleTalk Printing (IIgs)

### 8.1 Overview

The IIgs can print to AppleTalk-connected printers like the LaserWriter.

### 8.2 PAP (Printer Access Protocol)

AppleTalk printing uses PAP:

1. Lookup printer name in the Chooser
2. Open PAP connection to printer
3. Send PostScript or other print data
4. Close connection when complete

### 8.3 PostScript

LaserWriter and compatible printers use Adobe PostScript:

```postscript
%!PS
/Helvetica findfont 12 scalefont setfont
72 720 moveto
(Hello from Apple IIgs!) show
showpage
```

---

## 9. Implementation Notes

### 9.1 Flow Control

Handle XON/XOFF for serial printers:

```csharp
private bool _xonXoffEnabled;
private bool _outputHalted;

public void ProcessReceivedByte(byte data)
{
    if (_xonXoffEnabled)
    {
        if (data == 0x13)  // XOFF
        {
            _outputHalted = true;
            return;
        }
        if (data == 0x11)  // XON
        {
            _outputHalted = false;
            return;
        }
    }
}

public void SendByte(byte data)
{
    while (_outputHalted)
        Thread.Sleep(1);
    
    _serialPort.Write(data);
}
```

### 9.2 Page Sizing

Common paper sizes for emulation:

| Size    | Width (in) | Height (in) | At 72 DPI      |
|---------|------------|-------------|----------------|
| Letter  | 8.5        | 11          | 612 × 792      |
| Legal   | 8.5        | 14          | 612 × 1008     |
| A4      | 8.27       | 11.69       | 595 × 842      |

---

## Document History

| Version | Date       | Changes                            |
|---------|------------|------------------------------------|
| 1.0     | 2025-12-28 | Initial specification              |

---

## Appendix A: Bus Architecture Integration

This appendix provides implementation guidance for integrating printer support
with the emulator's bus architecture.

### A.1 Parallel Port as IPeripheral

The parallel printer interface card implements `IPeripheral`:

```csharp
/// <summary>
/// Parallel printer interface card.
/// </summary>
public sealed class ParallelPrinterCard : IPeripheral
{
    private readonly byte[] _slotRom = new byte[256];
    private IPrinter? _printer;
    
    /// <inheritdoc/>
    public string Name => "Parallel Printer Card";
    
    /// <inheritdoc/>
    public string DeviceType => "ParallelPrinter";
    
    /// <inheritdoc/>
    public int SlotNumber { get; set; }
    
    /// <inheritdoc/>
    public IBusTarget? MMIORegion { get; }
    
    /// <inheritdoc/>
    public IBusTarget? ROMRegion { get; }
    
    /// <inheritdoc/>
    public IBusTarget? ExpansionROMRegion => null;  // No expansion ROM
    
    public ParallelPrinterCard()
    {
        MMIORegion = new ParallelPortTarget(this);
        ROMRegion = new RomTarget(_slotRom);
    }
    
    public void Connect(IPrinter printer)
    {
        _printer = printer;
    }
    
    public void Disconnect()
    {
        _printer = null;
    }
}
```

### A.2 Parallel Port Target Implementation

```csharp
/// <summary>
/// Parallel port MMIO registers.
/// </summary>
public sealed class ParallelPortTarget : IBusTarget
{
    private readonly ParallelPrinterCard _card;
    private byte _dataLatch;
    private bool _strobeActive;
    
    /// <inheritdoc/>
    public TargetCaps Capabilities => TargetCaps.SideEffects;
    
    /// <inheritdoc/>
    public byte Read8(Addr physicalAddress, in BusAccess access)
    {
        int offset = (int)(physicalAddress & 0x0F);
        
        return offset switch
        {
            0x00 => _dataLatch,             // Data register (read back)
            0x02 => ReadStatusRegister(),   // Status register
            _ => 0xFF
        };
    }
    
    /// <inheritdoc/>
    public void Write8(Addr physicalAddress, byte value, in BusAccess access)
    {
        if (access.IsSideEffectFree)
            return;
        
        int offset = (int)(physicalAddress & 0x0F);
        
        switch (offset)
        {
            case 0x00:
                _dataLatch = value;
                break;
            case 0x01:
                // Strobe register - any write triggers strobe
                TriggerStrobe();
                break;
        }
    }
    
    private byte ReadStatusRegister()
    {
        byte status = 0;
        
        var printer = _card.Printer;
        if (printer != null)
        {
            if (printer.IsBusy) status |= 0x80;
            if (!printer.HasError) status |= 0x08;  // No error = bit 3 set
            if (printer.IsOnline) status |= 0x10;
        }
        else
        {
            // No printer connected - always busy
            status |= 0x80;
        }
        
        return status;
    }
    
    private void TriggerStrobe()
    {
        var printer = _card.Printer;
        if (printer != null && !printer.IsBusy)
        {
            printer.SendByte(_dataLatch);
        }
    }
}
```

### A.3 Virtual Printer Implementation

```csharp
/// <summary>
/// Virtual printer that captures output to a text file.
/// </summary>
public sealed class TextFilePrinter : IPrinter, IDisposable
{
    private readonly StreamWriter _writer;
    private bool _escapeMode;
    private readonly List<byte> _escapeSequence = new();
    
    /// <inheritdoc/>
    public string Name => "Text File Printer";
    
    /// <inheritdoc/>
    public bool IsOnline => true;
    
    /// <inheritdoc/>
    public bool IsBusy => false;
    
    /// <inheritdoc/>
    public bool HasPaper => true;
    
    /// <inheritdoc/>
    public bool HasError => false;
    
    public event Action<ReadOnlyMemory<byte>>? PageComplete;
    
    public TextFilePrinter(string filePath)
    {
        _writer = new StreamWriter(filePath, append: true);
    }
    
    /// <inheritdoc/>
    public void SendByte(byte data)
    {
        if (_escapeMode)
        {
            _escapeSequence.Add(data);
            if (IsEscapeSequenceComplete())
            {
                ProcessEscapeSequence();
                _escapeMode = false;
                _escapeSequence.Clear();
            }
            return;
        }
        
        switch (data)
        {
            case 0x1B:  // ESC
                _escapeMode = true;
                _escapeSequence.Clear();
                break;
            case 0x0D:  // CR - ignore (LF handles line ending)
                break;
            case 0x0A:  // LF
                _writer.WriteLine();
                break;
            case 0x0C:  // FF
                _writer.WriteLine("\f");
                FormFeed();
                break;
            default:
                if (data >= 0x20)
                    _writer.Write((char)data);
                break;
        }
    }
    
    /// <inheritdoc/>
    public void Reset()
    {
        _escapeMode = false;
        _escapeSequence.Clear();
    }
    
    /// <inheritdoc/>
    public void FormFeed()
    {
        _writer.Flush();
        PageComplete?.Invoke(ReadOnlyMemory<byte>.Empty);
    }
    
    /// <inheritdoc/>
    public ReadOnlySpan<byte> GetPageImage() => ReadOnlySpan<byte>.Empty;
    
    public void Dispose()
    {
        _writer.Dispose();
    }
}
```

### A.4 PDF Printer Implementation

```csharp
/// <summary>
/// Virtual printer that generates PDF output.
/// </summary>
public sealed class PdfPrinter : IPrinter
{
    private readonly List<PrintedLine> _currentPage = new();
    private int _currentX, _currentY;
    private bool _bold, _italic, _underline;
    private int _fontSize = 12;
    
    /// <inheritdoc/>
    public string Name => "PDF Printer";
    
    /// <inheritdoc/>
    public bool IsOnline => true;
    
    /// <inheritdoc/>
    public bool IsBusy => false;
    
    /// <inheritdoc/>
    public bool HasPaper => true;
    
    /// <inheritdoc/>
    public bool HasError => false;
    
    public event Action<ReadOnlyMemory<byte>>? PageComplete;
    
    /// <inheritdoc/>
    public void SendByte(byte data)
    {
        if (_escapeMode)
        {
            ProcessEscapeSequenceByte(data);
            return;
        }
        
        switch (data)
        {
            case 0x1B:  // ESC
                _escapeMode = true;
                break;
            case 0x0D:  // CR
                _currentX = 0;
                break;
            case 0x0A:  // LF
                _currentY += _lineHeight;
                break;
            case 0x0C:  // FF
                FormFeed();
                break;
            default:
                AddCharacter((char)data);
                break;
        }
    }
    
    /// <inheritdoc/>
    public void FormFeed()
    {
        var pdf = GeneratePdf();
        PageComplete?.Invoke(pdf);
        _currentPage.Clear();
        _currentX = 0;
        _currentY = 0;
    }
    
    private void AddCharacter(char ch)
    {
        _currentPage.Add(new PrintedLine(
            _currentX, 
            _currentY,
            ch.ToString(),
            _bold,
            _italic,
            _underline,
            _fontSize));
        
        _currentX += GetCharacterWidth(ch);
    }
    
    private ReadOnlyMemory<byte> GeneratePdf()
    {
        // Generate PDF document from _currentPage
        // This is a simplified example
        using var ms = new MemoryStream();
        using var writer = new StreamWriter(ms);
        
        writer.WriteLine("%PDF-1.4");
        // ... PDF generation code ...
        
        return ms.ToArray();
    }
}
```

### A.5 Composite Page Integration

```csharp
public sealed class AppleIIIOPage : ICompositeTarget
{
    private readonly ISlotManager _slots;
    
    /// <inheritdoc/>
    public IBusTarget? ResolveTarget(Addr offset, AccessIntent intent)
    {
        // Slot device I/O ($C090-$C0FF)
        if (offset >= 0x90 && offset < 0x100)
        {
            int slot = ((offset - 0x80) >> 4);
            var card = _slots.GetCard(slot);
            
            // Return the card's MMIO region
            return card?.MMIORegion;
        }
        
        return null;
    }
}
```

### A.6 Scheduler Integration for Print Timing

```csharp
/// <summary>
/// Printer with realistic timing emulation.
/// </summary>
public sealed class TimedPrinter : IPrinter, ISchedulable
{
    private readonly IScheduler _scheduler;
    private readonly Queue<byte> _printQueue = new();
    private bool _printing;
    
    // Approximately 80 characters per second (CPS)
    private const ulong CyclesPerCharacter = 12_500;  // At 1 MHz
    
    /// <inheritdoc/>
    public bool IsBusy => _printing || _printQueue.Count >= MaxQueueSize;
    
    /// <inheritdoc/>
    public void Initialize(IEventContext context)
    {
        _scheduler = context.Scheduler;
    }
    
    /// <inheritdoc/>
    public void SendByte(byte data)
    {
        _printQueue.Enqueue(data);
        
        if (!_printing)
        {
            _printing = true;
            _scheduler.ScheduleAfter(this, CyclesPerCharacter);
        }
    }
    
    /// <inheritdoc/>
    public ulong Execute(ulong currentCycle)
    {
        if (_printQueue.Count > 0)
        {
            byte data = _printQueue.Dequeue();
            ProcessByte(data);
            
            if (_printQueue.Count > 0)
            {
                _scheduler.ScheduleAfter(this, CyclesPerCharacter);
                return CyclesPerCharacter;
            }
        }
        
        _printing = false;
        return 0;
    }
}
```

### A.7 Device Registry

```csharp
public void RegisterPrinterDevices(IDeviceRegistry registry, int slot)
{
    // Printer interface card
    registry.Register(
        registry.GenerateId(),
        DevicePageId.Create(DevicePageClass.CompatIO, instance: (byte)slot, page: 0),
        kind: "ParallelPrinter",
        name: $"Parallel Printer (Slot {slot})",
        wiringPath: $"main/slots/{slot}/printer");
}
```

### A.8 Trap Handler for Printer ROM

```csharp
/// <summary>
/// Trap handler for printer card firmware routines.
/// </summary>
public sealed class PrinterTraps
{
    private readonly ParallelPrinterCard _card;
    private readonly ISlotManager _slots;
    
    /// <summary>
    /// Trap for PR#n output routine.
    /// </summary>
    public TrapResult OutputHandler(ICpu cpu, IMemoryBus bus, IEventContext context)
    {
        // Verify slot has printer card
        if (_slots.GetCard(_card.SlotNumber)?.DeviceType != "ParallelPrinter")
            return new TrapResult(Handled: false, default, null);
        
        // Select expansion ROM (if any)
        _slots.SelectExpansionSlot(_card.SlotNumber);
        
        // Character in accumulator
        byte ch = cpu.A;
        
        // Send to printer
        _card.Printer?.SendByte(ch);
        
        return new TrapResult(
            Handled: true,
            CyclesConsumed: new Cycle(20),
            ReturnAddress: null);
    }
    
    /// <summary>
    /// Trap for printer status check.
    /// </summary>
    public TrapResult StatusHandler(ICpu cpu, IMemoryBus bus, IEventContext context)
    {
        if (_slots.GetCard(_card.SlotNumber)?.DeviceType != "ParallelPrinter")
            return new TrapResult(Handled: false, default, null);
        
        var printer = _card.Printer;
        
        // Set carry if printer not ready
        cpu.SetCarry(printer == null || printer.IsBusy);
        
        return new TrapResult(
            Handled: true,
            CyclesConsumed: new Cycle(10),
            ReturnAddress: null);
    }
}
```

### A.9 Print Spooler Service

```csharp
/// <summary>
/// Print spooler service implementation.
/// </summary>
public sealed class PrintSpooler : IPrintSpooler
{
    private readonly ConcurrentQueue<PrintJob> _queue = new();
    private readonly Dictionary<Guid, PrintJobState> _jobs = new();
    
    public event Action<Guid, PrintJobResult>? JobCompleted;
    
    /// <inheritdoc/>
    public Guid AddJob(string name, IPrinter printer, ReadOnlyMemory<byte> data)
    {
        var job = new PrintJob(
            Guid.NewGuid(),
            name,
            DateTime.UtcNow,
            data.Length,
            PrintJobStatus.Pending);
        
        _jobs[job.Id] = new PrintJobState(job, printer, data);
        _queue.Enqueue(job);
        
        ProcessQueue();
        
        return job.Id;
    }
    
    /// <inheritdoc/>
    public bool CancelJob(Guid jobId)
    {
        if (_jobs.TryGetValue(jobId, out var state))
        {
            if (state.Job.Status == PrintJobStatus.Pending)
            {
                _jobs[jobId] = state with 
                { 
                    Job = state.Job with { Status = PrintJobStatus.Cancelled } 
                };
                return true;
            }
        }
        return false;
    }
    
    private async Task ProcessQueue()
    {
        while (_queue.TryDequeue(out var job))
        {
            if (!_jobs.TryGetValue(job.Id, out var state))
                continue;
            
            if state.Job.Status == PrintJobStatus.Cancelled)
                continue;
            
            _jobs[job.Id] = state with 
            { 
                Job = state.Job with { Status = PrintJobStatus.Printing } 
            };
            
            try
            {
                foreach (byte b in state.Data.Span)
                    state.Printer.SendByte(b);
                
                _jobs[job.Id] = state with 
                { 
                    Job = state.Job with { Status = PrintJobStatus.Completed } 
                };
                
                JobCompleted?.Invoke(job.Id, PrintJobResult.Success);
            }
            catch (Exception ex)
            {
                _jobs[job.Id] = state with 
                { 
                    Job = state.Job with { Status = PrintJobStatus.Error } 
                };
                
                JobCompleted?.Invoke(job.Id, PrintJobResult.Error(ex.Message));
            }
        }
    }
}
