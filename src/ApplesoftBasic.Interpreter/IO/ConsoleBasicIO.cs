// <copyright file="ConsoleBasicIO.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.IO;

using Emulation;

/// <summary>
/// Console-based I/O implementation.
/// </summary>
public class ConsoleBasicIO : IBasicIO
{
    private TextMode _currentMode = TextMode.Normal;
    private int _cursorColumn;
    private IAppleSpeaker? _speaker;

    // Bell character (CHR$(7))
    private const char BellChar = '\x07';

    public void SetSpeaker(IAppleSpeaker? speaker)
    {
        _speaker = speaker;
    }

    public void Write(string text)
    {
        // Process text for control characters
        var processedText = ProcessControlCharacters(text);

        if (_currentMode == TextMode.Inverse)
        {
            Console.BackgroundColor = ConsoleColor.White;
            Console.ForegroundColor = ConsoleColor.Black;
        }

        Console.Write(processedText);
        _cursorColumn += processedText.Length;

        if (_currentMode == TextMode.Inverse)
        {
            Console.ResetColor();
        }
    }

    public void WriteLine(string text = "")
    {
        // Process text for control characters
        var processedText = ProcessControlCharacters(text);

        if (_currentMode == TextMode.Inverse)
        {
            Console.BackgroundColor = ConsoleColor.White;
            Console.ForegroundColor = ConsoleColor.Black;
        }

        Console.WriteLine(processedText);
        _cursorColumn = 0;

        if (_currentMode == TextMode.Inverse)
        {
            Console.ResetColor();
        }
    }

    /// <summary>
    /// Processes control characters in text, handling special cases like CHR$(7) (bell).
    /// </summary>
    private string ProcessControlCharacters(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        // Check for bell character and trigger beep
        if (text.Contains(BellChar))
        {
            // Count and trigger beeps
            foreach (char c in text)
            {
                if (c == BellChar)
                {
                    Beep();
                }
            }
            // Remove bell characters from output text
            text = text.Replace(BellChar.ToString(), "");
        }

        return text;
    }

    public string ReadLine(string? prompt = null)
    {
        if (!string.IsNullOrEmpty(prompt))
        {
            Write(prompt);
        }

        var result = Console.ReadLine() ?? string.Empty;
        _cursorColumn = 0;
        return result;
    }

    public char ReadChar()
    {
        var key = Console.ReadKey(true);
        return key.KeyChar;
    }

    public void ClearScreen()
    {
        try
        {
            Console.Clear();
        }
        catch
        {
            // Console.Clear may not work in all environments
            for (int i = 0; i < 24; i++)
            {
                Console.WriteLine();
            }
        }
        _cursorColumn = 0;
    }

    public void SetCursorPosition(int column, int row)
    {
        try
        {
            // Apple II is 1-based, Console is 0-based
            int col = Math.Max(0, Math.Min(column - 1, Console.WindowWidth - 1));
            int r = Math.Max(0, Math.Min(row - 1, Console.WindowHeight - 1));
            Console.SetCursorPosition(col, r);
            _cursorColumn = col;
        }
        catch
        {
            // Ignore cursor positioning errors
        }
    }

    public int GetCursorColumn()
    {
        try
        {
            return Console.CursorLeft;
        }
        catch
        {
            return _cursorColumn;
        }
    }

    public int GetCursorRow()
    {
        try
        {
            return Console.CursorTop;
        }
        catch
        {
            return 0;
        }
    }

    public void SetTextMode(TextMode mode)
    {
        _currentMode = mode;

        if (mode == TextMode.Normal)
        {
            Console.ResetColor();
        }
    }

    public void Beep()
    {
        // Use the Apple II speaker emulation if available
        if (_speaker != null)
        {
            _speaker.Beep();
        }
        else
        {
            // Fallback to console beep if speaker not available
            try
            {
                if (OperatingSystem.IsWindows())
                {
                    Console.Beep(1000, 100);
                }
            }
            catch
            {
                // Beep may not work in all environments
            }
        }
    }
}