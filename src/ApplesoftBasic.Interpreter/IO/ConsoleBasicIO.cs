// <copyright file="ConsoleBasicIO.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.IO;

using System.Diagnostics.CodeAnalysis;

using Emulation;

/// <summary>
/// Console-based I/O implementation.
/// </summary>
[ExcludeFromCodeCoverage]
public class ConsoleBasicIO : IBasicIO
{
    // Bell character (CHR$(7))
    private const char BellChar = '\x07';

    private TextMode currentMode = TextMode.Normal;
    private int cursorColumn;
    private IAppleSpeaker? speaker;

    /// <summary>
    /// Sets the speaker for the console-based I/O implementation.
    /// </summary>
    /// <param name="speaker">
    /// An instance of <see cref="IAppleSpeaker"/> to be used for audio output, or <c>null</c> to disable speaker functionality.
    /// </param>
    public void SetSpeaker(IAppleSpeaker? speaker)
    {
        this.speaker = speaker;
    }

    /// <summary>
    /// Writes the specified text to the console, processing control characters
    /// and adjusting the text appearance based on the current text mode.
    /// </summary>
    /// <param name="text">The text to write to the console.</param>
    /// <remarks>
    /// If the current text mode is <see cref="TextMode.Inverse"/>, the text is displayed
    /// with inverted colors. Control characters in the text are processed before output.
    /// </remarks>
    public void Write(string text)
    {
        // Process text for control characters
        var processedText = ProcessControlCharacters(text);

        if (currentMode == TextMode.Inverse)
        {
            Console.BackgroundColor = ConsoleColor.White;
            Console.ForegroundColor = ConsoleColor.Black;
        }

        Console.Write(processedText);
        cursorColumn += processedText.Length;

        if (currentMode == TextMode.Inverse)
        {
            Console.ResetColor();
        }
    }

    /// <summary>
    /// Writes a line of text to the console, followed by a newline character.
    /// </summary>
    /// <param name="text">
    /// The text to write to the console. If not specified, an empty string is written.
    /// </param>
    /// <remarks>
    /// If the current text mode is set to <see cref="TextMode.Inverse"/>, the text is displayed
    /// with inverted colors. After writing, the cursor column is reset to zero.
    /// </remarks>
    public void WriteLine(string text = "")
    {
        // Process text for control characters
        var processedText = ProcessControlCharacters(text);

        if (currentMode == TextMode.Inverse)
        {
            Console.BackgroundColor = ConsoleColor.White;
            Console.ForegroundColor = ConsoleColor.Black;
        }

        Console.WriteLine(processedText);
        cursorColumn = 0;

        if (currentMode == TextMode.Inverse)
        {
            Console.ResetColor();
        }
    }

    /// <summary>
    /// Reads a line of input from the console, optionally displaying a prompt.
    /// </summary>
    /// <param name="prompt">
    /// An optional string to display as a prompt before reading the input.
    /// If <c>null</c> or empty, no prompt is displayed.
    /// </param>
    /// <returns>
    /// The input string entered by the user. If no input is provided, an empty string is returned.
    /// </returns>
    public string ReadLine(string? prompt = null)
    {
        if (!string.IsNullOrEmpty(prompt))
        {
            Write(prompt);
        }

        var result = Console.ReadLine() ?? string.Empty;
        cursorColumn = 0;
        return result;
    }

    /// <summary>
    /// Reads a single character input from the console without displaying it on the screen.
    /// </summary>
    /// <returns>The character entered by the user.</returns>
    /// <remarks>
    /// This method captures a key press from the console and returns the corresponding character.
    /// The input is read in a non-echoing mode, meaning the character is not displayed on the console.
    /// </remarks>
    public char ReadChar()
    {
        var key = Console.ReadKey(true);
        return key.KeyChar;
    }

    /// <summary>
    /// Clears the console screen and resets the cursor position to the top-left corner.
    /// </summary>
    /// <remarks>
    /// This method attempts to clear the console using <see cref="System.Console.Clear"/>.
    /// If the operation fails (e.g., in environments where <see cref="System.Console.Clear"/> is not supported),
    /// it falls back to printing blank lines to simulate clearing the screen.
    /// </remarks>
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

        cursorColumn = 0;
    }

    /// <summary>
    /// Sets the cursor position on the console screen.
    /// </summary>
    /// <param name="column">
    /// The 1-based column position to set the cursor to. Values less than 1 will be clamped to the minimum allowed position.
    /// </param>
    /// <param name="row">
    /// The 1-based row position to set the cursor to. Values less than 1 will be clamped to the minimum allowed position.
    /// </param>
    /// <remarks>
    /// This method adjusts the provided 1-based column and row values to match the 0-based coordinate system
    /// used by the console. If the specified position exceeds the console's dimensions, it will be clamped
    /// to the maximum allowed position. Any errors during cursor positioning are ignored.
    /// </remarks>
    public void SetCursorPosition(int column, int row)
    {
        try
        {
            // Apple II is 1-based, Console is 0-based
            int col = Math.Max(0, Math.Min(column - 1, Console.WindowWidth - 1));
            int r = Math.Max(0, Math.Min(row - 1, Console.WindowHeight - 1));
            Console.SetCursorPosition(col, r);
            cursorColumn = col;
        }
        catch
        {
            // Ignore cursor positioning errors
        }
    }

    /// <summary>
    /// Retrieves the current column position of the cursor within the console window.
    /// </summary>
    /// <returns>
    /// The zero-based column position of the cursor. If the position cannot be determined,
    /// a fallback value is returned.
    /// </returns>
    /// <remarks>
    /// This method attempts to retrieve the cursor's column position using <see cref="System.Console.CursorLeft"/>.
    /// If an exception occurs, it returns a cached or default value.
    /// </remarks>
    public int GetCursorColumn()
    {
        try
        {
            return Console.CursorLeft;
        }
        catch
        {
            return cursorColumn;
        }
    }

    /// <summary>
    /// Retrieves the current row position of the cursor in the console.
    /// </summary>
    /// <returns>
    /// The zero-based row position of the cursor in the console.
    /// If the cursor position cannot be determined, returns <c>0</c>.
    /// </returns>
    /// <remarks>
    /// This method relies on <see cref="System.Console.CursorTop"/> to determine the cursor's row position.
    /// </remarks>
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

    /// <summary>
    /// Sets the text output mode for the console.
    /// </summary>
    /// <param name="mode">
    /// The <see cref="TextMode"/> to set. This determines how text is displayed,
    /// such as normal, inverse, or flashing text.
    /// </param>
    /// <remarks>
    /// Changing the text mode may affect the appearance of text output in the console.
    /// For example, setting the mode to <see cref="TextMode.Normal"/> resets the console colors.
    /// </remarks>
    public void SetTextMode(TextMode mode)
    {
        currentMode = mode;

        if (mode == TextMode.Normal)
        {
            Console.ResetColor();
        }
    }

    /// <summary>
    /// Emits a beep sound to signal an event or alert the user.
    /// </summary>
    /// <remarks>
    /// If an <see cref="IAppleSpeaker"/> instance is set, it uses the Apple II speaker emulation to produce the sound.
    /// Otherwise, it falls back to the system console beep on Windows platforms.
    /// </remarks>
    /// <exception cref="PlatformNotSupportedException">
    /// Thrown if the fallback console beep is not supported in the current environment.
    /// </exception>
    public void Beep()
    {
        // Use the Apple II speaker emulation if available
        if (speaker != null)
        {
            speaker.Beep();
        }
        else
        {
            // Fallback to console beep if speaker not available
            try
            {
                if (OperatingSystem.IsWindows())
                {
                    Console.Beep(1000, 500);
                }
            }
            catch
            {
                // Beep may not work in all environments
            }
        }
    }

    /// <summary>
    /// Processes control characters in the provided text, handling special cases such as the bell character (CHR$(7)).
    /// </summary>
    /// <param name="text">The text to process for control characters.</param>
    /// <returns>
    /// A string with control characters processed and removed as necessary. For example,
    /// bell characters are removed after triggering the corresponding beep sound.
    /// </returns>
    /// <remarks>
    /// This method identifies and processes control characters embedded in the input text.
    /// Specifically, it handles the bell character by triggering a beep sound for each occurrence
    /// and then removes the bell characters from the resulting text.
    /// </remarks>
    private string ProcessControlCharacters(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

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
            text = text.Replace(BellChar.ToString(), string.Empty);
        }

        return text;
    }
}