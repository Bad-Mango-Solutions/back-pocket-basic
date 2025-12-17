// <copyright file="TokenType.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

// ReSharper disable InconsistentNaming
namespace ApplesoftBasic.Interpreter.Tokens;

/// <summary>
/// Defines the various token types used in the Applesoft BASIC interpreter.
/// </summary>
/// <remarks>
/// Tokens represent the smallest units of meaning in the Applesoft BASIC language.
/// These include keywords, operators, literals, and other symbols.
/// </remarks>
public enum TokenType
{
    // Literals

    /// <summary>
    /// Represents a numeric literal token in the Applesoft BASIC interpreter.
    /// </summary>
    /// <remarks>
    /// This token type is used to identify numeric values within the source code,
    /// such as integers or floating-point numbers.
    /// </remarks>
    Number,

    /// <summary>
    /// Represents a token type for string literals in the Applesoft BASIC interpreter.
    /// </summary>
    /// <remarks>
    /// String tokens are used to identify sequences of characters enclosed in quotes
    /// within the Applesoft BASIC language.
    /// </remarks>
    String,

    /// <summary>
    /// Represents an identifier token in the Applesoft BASIC interpreter.
    /// </summary>
    /// <remarks>
    /// Identifiers are used to name variables, functions, and other user-defined elements
    /// within the Applesoft BASIC language.
    /// </remarks>
    Identifier,

    // Keywords - Program Control

    /// <summary>
    /// Represents the REM token in Applesoft BASIC.
    /// </summary>
    /// <remarks>
    /// The REM token is used to denote comments in Applesoft BASIC code.
    /// Any text following the REM keyword on the same line is ignored by the interpreter.
    /// </remarks>
    REM,

    /// <summary>
    /// Represents the <c>LET</c> keyword in Applesoft BASIC.
    /// </summary>
    /// <remarks>
    /// The <c>LET</c> keyword is used to assign a value to a variable in Applesoft BASIC.
    /// While the use of <c>LET</c> is optional in many cases, it explicitly indicates an assignment operation.
    /// </remarks>
    LET,

    /// <summary>
    /// Represents the DIM keyword in the Applesoft BASIC language.
    /// </summary>
    /// <remarks>
    /// The DIM keyword is used to declare arrays in Applesoft BASIC.
    /// It specifies the dimensions of the array to be created.
    /// </remarks>
    DIM,

    /// <summary>
    /// Represents the <c>DEF</c> token in the Applesoft BASIC interpreter.
    /// </summary>
    /// <remarks>
    /// The <c>DEF</c> token is used to define user-defined functions in Applesoft BASIC.
    /// It allows the creation of reusable function definitions that can be invoked elsewhere in the program.
    /// </remarks>
    DEF,

    /// <summary>
    /// Represents the token type for a user-defined function in Applesoft BASIC.
    /// </summary>
    /// <remarks>
    /// The <c>FN</c> token is used to define or reference a user-defined function
    /// within the Applesoft BASIC interpreter.
    /// </remarks>
    FN,

    /// <summary>
    /// Represents the `END` keyword in the Applesoft BASIC language.
    /// </summary>
    /// <remarks>
    /// The `END` token is used to signify the termination of a program or a block of code.
    /// It indicates that no further execution should occur beyond this point.
    /// </remarks>
    END,

    /// <summary>
    /// Represents the STOP token in Applesoft BASIC, which is used to halt the execution of a program.
    /// </summary>
    STOP,

    // Keywords - Flow Control

    /// <summary>
    /// Represents the <c>GOTO</c> token in Applesoft BASIC.
    /// </summary>
    /// <remarks>
    /// The <c>GOTO</c> token is used to perform an unconditional jump to a specified line number
    /// in the program, altering the flow of execution.
    /// </remarks>
    GOTO,

    /// <summary>
    /// Represents the GOSUB token in Applesoft BASIC, which is used to call a subroutine.
    /// </summary>
    /// <remarks>
    /// The GOSUB statement transfers program control to a specified line number where a subroutine begins.
    /// Execution resumes at the statement following the corresponding RETURN statement.
    /// </remarks>
    GOSUB,

    /// <summary>
    /// Represents the <c>RETURN</c> token in Applesoft BASIC,
    /// which is used to return control from a subroutine invoked by a <c>GOSUB</c> statement.
    /// </summary>
    RETURN,

    /// <summary>
    /// Represents the <c>ON</c> token in Applesoft BASIC.
    /// </summary>
    /// <remarks>
    /// The <c>ON</c> token is used in Applesoft BASIC to define conditional branching
    /// based on the evaluation of an expression. It is typically followed by a list
    /// of line numbers or labels to branch to.
    /// </remarks>
    ON,

    /// <summary>
    /// Represents the token type for the "IF" keyword in Applesoft BASIC.
    /// </summary>
    /// <remarks>
    /// The "IF" keyword is used to introduce conditional statements in Applesoft BASIC,
    /// allowing the execution of code based on a specified condition.
    /// </remarks>
    IF,

    /// <summary>
    /// Represents the "THEN" keyword in Applesoft BASIC, typically used in conditional statements
    /// to specify the action or statement to execute when a condition evaluates to true.
    /// </summary>
    THEN,

    /// <summary>
    /// Represents the "ELSE" token in the Applesoft BASIC interpreter.
    /// This token is used in conditional statements to specify the alternative block
    /// of code to execute when the condition evaluates to false.
    /// </summary>
    ELSE,

    /// <summary>
    /// Represents the token type for the "FOR" keyword in Applesoft BASIC.
    /// </summary>
    /// <remarks>
    /// The "FOR" keyword is used to define the beginning of a loop in Applesoft BASIC.
    /// It is typically followed by a variable, a starting value, an ending value,
    /// and optionally a step value.
    /// </remarks>
    FOR,

    /// <summary>
    /// Represents the "TO" keyword in Applesoft BASIC, typically used in constructs such as "FOR ... TO ...".
    /// </summary>
    TO,

    /// <summary>
    /// Represents the <c>STEP</c> keyword in Applesoft BASIC, which is used in conjunction with the <c>FOR</c> statement
    /// to specify the increment value for the loop variable.
    /// </summary>
    STEP,

    /// <summary>
    /// Represents the <c>NEXT</c> token in Applesoft BASIC, which is used to indicate the end of a <c>FOR</c> loop.
    /// </summary>
    NEXT,

    // Keywords - I/O

    /// <summary>
    /// Represents the <c>PRINT</c> token in Applesoft BASIC, which is used to output text or values to the console or screen.
    /// </summary>
    PRINT,

    /// <summary>
    /// Represents the <c>INPUT</c> token in the Applesoft BASIC interpreter.
    /// </summary>
    /// <remarks>
    /// The <c>INPUT</c> token is used to handle user input in Applesoft BASIC programs.
    /// It prompts the user to enter data, which can then be assigned to variables.
    /// </remarks>
    INPUT,

    /// <summary>
    /// Represents the `GET` token in Applesoft BASIC, which is used to read a single character from the input.
    /// </summary>
    GET,

    /// <summary>
    /// Represents the <c>DATA</c> token in Applesoft BASIC.
    /// </summary>
    /// <remarks>
    /// The <c>DATA</c> token is used to define a list of constant values
    /// that can be read sequentially using the <c>READ</c> statement.
    /// </remarks>
    DATA,

    /// <summary>
    /// Represents the READ token in Applesoft BASIC, which is used to read data values
    /// from a DATA statement into variables during program execution.
    /// </summary>
    READ,

    /// <summary>
    /// Represents the RESTORE token in Applesoft BASIC.
    /// </summary>
    /// <remarks>
    /// The RESTORE token is used to reset the data pointer to the beginning of the DATA statements,
    /// allowing subsequent READ statements to retrieve data from the start.
    /// </remarks>
    RESTORE,

    // Keywords - Graphics (stubbed)

    /// <summary>
    /// Represents the token type for the Applesoft BASIC "GR" command.
    /// </summary>
    /// <remarks>
    /// The "GR" command in Applesoft BASIC is used to switch the display mode to low-resolution graphics.
    /// </remarks>
    GR,

    /// <summary>
    /// Represents the token type for the Applesoft BASIC <c>HGR</c> command.
    /// </summary>
    /// <remarks>
    /// The <c>HGR</c> command is used to enable high-resolution graphics mode.
    /// </remarks>
    HGR,

    /// <summary>
    /// Represents the token type for the HGR2 command in Applesoft BASIC.
    /// </summary>
    /// <remarks>
    /// The HGR2 command is used to enable the second high-resolution graphics screen.
    /// </remarks>
    HGR2,

    /// <summary>
    /// Represents a token type for text in the Applesoft BASIC interpreter.
    /// </summary>
    TEXT,

    /// <summary>
    /// Represents the COLOR token in Applesoft BASIC, typically used to set or manipulate
    /// the color settings for graphical operations.
    /// </summary>
    COLOR,

    /// <summary>
    /// Represents the token type for setting the high-resolution color (HCOLOR) in Applesoft BASIC.
    /// </summary>
    /// <remarks>
    /// This token is used to specify the color for high-resolution graphics commands.
    /// </remarks>
    HCOLOR,

    /// <summary>
    /// Represents the PLOT command token in Applesoft BASIC.
    /// </summary>
    /// <remarks>
    /// The PLOT command is used to plot a point on the graphics screen at a specified coordinate.
    /// </remarks>
    PLOT,

    /// <summary>
    /// Represents the HPLOT token in Applesoft BASIC, which is used to plot high-resolution lines
    /// on the high-resolution graphics screen.
    /// </summary>
    HPLOT,

    /// <summary>
    /// Represents the DRAW token in Applesoft BASIC, used to execute graphical drawing commands.
    /// </summary>
    DRAW,

    /// <summary>
    /// Represents the XDRAW token in the Applesoft BASIC interpreter.
    /// </summary>
    /// <remarks>
    /// The XDRAW token is used to perform extended drawing operations in graphical mode.
    /// </remarks>
    XDRAW,

    /// <summary>
    /// Represents the HTAB token in Applesoft BASIC, which is used to set the horizontal tab position
    /// for text output on the screen.
    /// </summary>
    HTAB,

    /// <summary>
    /// Represents the VTAB token in Applesoft BASIC, which is used to set the vertical text position on the screen.
    /// </summary>
    VTAB,

    /// <summary>
    /// Represents the HOME command in Applesoft BASIC, which clears the text screen and moves the cursor to the top-left corner.
    /// </summary>
    HOME,

    /// <summary>
    /// Represents the <c>INVERSE</c> token in Applesoft BASIC,
    /// typically used to indicate the inverse text display mode.
    /// </summary>
    INVERSE,

    /// <summary>
    /// Represents the <c>FLASH</c> token in Applesoft BASIC.
    /// This token is used to enable flashing text mode in the interpreter.
    /// </summary>
    FLASH,

    /// <summary>
    /// Represents a normal token type in the Applesoft BASIC interpreter.
    /// </summary>
    NORMAL,

    // Keywords - Sound
    // (Apple II didn't have dedicated sound commands in Applesoft)

    // Keywords - Memory/System

    /// <summary>
    /// Represents the PEEK token in Applesoft BASIC.
    /// </summary>
    /// <remarks>
    /// The PEEK token is used to read a byte of memory from a specified address.
    /// </remarks>
    PEEK,

    /// <summary>
    /// Represents the <c>POKE</c> token in Applesoft BASIC.
    /// </summary>
    /// <remarks>
    /// The <c>POKE</c> command is used to directly modify memory locations
    /// by specifying an address and a value to store at that address.
    /// </remarks>
    POKE,

    /// <summary>
    /// Represents the <c>CALL</c> token in Applesoft BASIC, which is used to invoke a machine language subroutine.
    /// </summary>
    CALL,

    /// <summary>
    /// Represents the HIMEM token in Applesoft BASIC, which is used to access or manipulate
    /// the high memory address in the system.
    /// </summary>
    HIMEM,

    /// <summary>
    /// Represents the <c>LOMEM</c> token in Applesoft BASIC, which is used to manage
    /// the lower memory boundary in the program.
    /// </summary>
    LOMEM,

    /// <summary>
    /// Represents the CLEAR token in Applesoft BASIC, which is used to reset variables, arrays, and other program states.
    /// </summary>
    CLEAR,

    /// <summary>
    /// Represents the token type for the <c>NEW</c> command in Applesoft BASIC.
    /// </summary>
    /// <remarks>
    /// The <c>NEW</c> command is used to clear the current program from memory,
    /// effectively resetting the program space for a new program to be written.
    /// </remarks>
    NEW,

    /// <summary>
    /// Represents the <c>RUN</c> token in Applesoft BASIC, which is used to execute the program
    /// currently loaded in memory from the beginning or from a specified line number.
    /// </summary>
    RUN,

    /// <summary>
    /// Represents the <c>LIST</c> token in Applesoft BASIC, which is used to display
    /// the program listing on the screen.
    /// </summary>
    LIST,

    /// <summary>
    /// Represents the token type for the "CONT" command in Applesoft BASIC.
    /// </summary>
    /// <remarks>
    /// The "CONT" command is used to continue program execution from where it was stopped.
    /// </remarks>
    CONT,

    // Keywords - String/Array

    /// <summary>
    /// Represents the token type for the MID\$ function in Applesoft BASIC.
    /// </summary>
    /// <remarks>
    /// The <c>MID$</c> function is used to extract a substring from a string, starting at a specified position
    /// and optionally for a specified length.
    /// </remarks>
    MID_S,      // MID$

    /// <summary>
    /// Represents the token type for the LEFT\$ function in Applesoft BASIC.
    /// </summary>
    /// <remarks>
    /// The <c>LEFT$</c> function is used to extract a specified number of characters
    /// from the beginning (left side) of a string.
    /// </remarks>
    LEFT_S,     // LEFT$

    /// <summary>
    /// Represents the RIGHT\$ function token in Applesoft BASIC.
    /// </summary>
    /// <remarks>
    /// The <c>RIGHT$</c> function is used to extract a specified number of characters
    /// from the right end of a string.
    /// </remarks>
    RIGHT_S,    // RIGHT$

    /// <summary>
    /// Represents the token type for the LEN function in Applesoft BASIC.
    /// </summary>
    /// <remarks>
    /// The <c>LEN</c> function is used to determine the length of a string.
    /// </remarks>
    LEN,

    /// <summary>
    /// Represents the token type for the VAL function in Applesoft BASIC.
    /// </summary>
    /// <remarks>
    /// The <c>VAL</c> function is used to convert a string representation of a number
    /// into its numeric equivalent.
    /// </remarks>
    VAL,

    /// <summary>
    /// Represents the token type for the <c>STR$</c> function in Applesoft BASIC.
    /// </summary>
    /// <remarks>
    /// The <c>STR$</c> function is used to convert a numeric value into its string representation.
    /// </remarks>
    STR_S,      // STR$

    /// <summary>
    /// Represents the token type for the Applesoft BASIC <c>CHR$</c> function.
    /// </summary>
    /// <remarks>
    /// The <c>CHR$</c> function is used to return the character associated with a specified ASCII code.
    /// </remarks>
    CHR_S,      // CHR$

    /// <summary>
    /// Represents the token type for the ASC function in Applesoft BASIC.
    /// </summary>
    /// <remarks>
    /// The <c>ASC</c> function is used to retrieve the ASCII code of the first character in a string.
    /// </remarks>
    ASC,

    // Keywords - Math Functions

    /// <summary>
    /// Represents the token type for the ABS function in Applesoft BASIC.
    /// The ABS function returns the absolute value of a numeric expression.
    /// </summary>
    ABS,

    /// <summary>
    /// Represents the arctangent (ATN) mathematical function token in Applesoft BASIC.
    /// </summary>
    /// <remarks>
    /// The ATN function calculates the arctangent of a given numeric value, returning the angle in radians.
    /// </remarks>
    ATN,

    /// <summary>
    /// Represents the token type for the cosine mathematical function (COS) in Applesoft BASIC.
    /// </summary>
    COS,

    /// <summary>
    /// Represents the exponential function (e^x) in Applesoft BASIC.
    /// </summary>
    EXP,

    /// <summary>
    /// Represents the INT function token in Applesoft BASIC.
    /// </summary>
    /// <remarks>
    /// The INT function in Applesoft BASIC is used to return the largest integer
    /// less than or equal to a given numeric value.
    /// </remarks>
    INT,

    /// <summary>
    /// Represents the logarithmic function token in Applesoft BASIC.
    /// </summary>
    /// <remarks>
    /// This token corresponds to the LOG function, which calculates the natural logarithm
    /// (base e) of a given numeric expression.
    /// </remarks>
    LOG,

    /// <summary>
    /// Represents the token type for the RND function in Applesoft BASIC.
    /// </summary>
    /// <remarks>
    /// The RND function is used to generate random numbers in Applesoft BASIC.
    /// </remarks>
    RND,

    /// <summary>
    /// Represents the token type for the <c>SGN</c> function in Applesoft BASIC.
    /// </summary>
    /// <remarks>
    /// The <c>SGN</c> function is used to determine the sign of a numeric value.
    /// It returns:
    /// <list type="bullet">
    /// <item><description><c>-1</c> if the value is negative.</description></item>
    /// <item><description><c>0</c> if the value is zero.</description></item>
    /// <item><description><c>1</c> if the value is positive.</description></item>
    /// </list>
    /// </remarks>
    SGN,

    /// <summary>
    /// Represents the token for the sine (SIN) mathematical function in Applesoft BASIC.
    /// </summary>
    SIN,

    /// <summary>
    /// Represents the square root (SQR) function token in Applesoft BASIC.
    /// </summary>
    /// <remarks>
    /// The SQR function calculates the square root of a given numeric expression.
    /// </remarks>
    SQR,

    /// <summary>
    /// Represents the token type for the tangent (TAN) mathematical function in Applesoft BASIC.
    /// </summary>
    TAN,

    // Keywords - Utility Functions

    /// <summary>
    /// Represents the <c>FRE</c> token in Applesoft BASIC.
    /// </summary>
    /// <remarks>
    /// The <c>FRE</c> token is used to determine the amount of free memory available in the system.
    /// </remarks>
    FRE,

    /// <summary>
    /// Represents the POS function in Applesoft BASIC, which is used to retrieve the current horizontal position of the cursor.
    /// </summary>
    POS,

    /// <summary>
    /// Represents the token type for the SCRN keyword in Applesoft BASIC.
    /// </summary>
    /// <remarks>
    /// The SCRN keyword is used to interact with or retrieve information about the screen in Applesoft BASIC.
    /// </remarks>
    SCRN,

    /// <summary>
    /// Represents the token type for the Paddle (PDL) command in Applesoft BASIC.
    /// </summary>
    /// <remarks>
    /// The PDL command is used to read the position of a game paddle or similar input device.
    /// </remarks>
    PDL,

    /// <summary>
    /// Represents the <c>USR</c> token in Applesoft BASIC, which is used to invoke a user-defined machine language routine.
    /// </summary>
    USR,

    // Keywords - Other

    /// <summary>
    /// Represents the TAB token in Applesoft BASIC, which is used to control horizontal tabulation
    /// in output operations, typically for formatting purposes.
    /// </summary>
    TAB,

    /// <summary>
    /// Represents the SPC (space) token in Applesoft BASIC, which is used to insert a specified number of spaces in output.
    /// </summary>
    SPC,

    /// <summary>
    /// Represents the logical NOT operator in the Applesoft BASIC interpreter.
    /// </summary>
    NOT,

    /// <summary>
    /// Represents the logical AND operator in Applesoft BASIC.
    /// </summary>
    AND,

    /// <summary>
    /// Represents the logical OR operator token in Applesoft BASIC.
    /// </summary>
    OR,

    // Keywords - Disk/File (ProDOS)

    /// <summary>
    /// Represents the token type for the "OPEN" keyword in Applesoft BASIC.
    /// </summary>
    /// <remarks>
    /// The "OPEN" keyword is used to open files or devices for input or output operations.
    /// </remarks>
    OPEN,

    /// <summary>
    /// Represents the token type for the "CLOSE" operation in Applesoft BASIC.
    /// Typically used to close a file or resource that was previously opened.
    /// </summary>
    CLOSE,

    /// <summary>
    /// Represents the token type for the PRINT# statement in Applesoft BASIC,
    /// which is used to write data to a file.
    /// </summary>
    PRINT_FILE,  // PRINT#

    /// <summary>
    /// Represents a token type for specifying an input file operation in the Applesoft BASIC interpreter.
    /// </summary>
    INPUT_FILE,  // INPUT#

    /// <summary>
    /// Represents the token type for retrieving a file in the Applesoft BASIC interpreter.
    /// </summary>
    GET_FILE,    // GET#

    /// <summary>
    /// Represents the <c>ONERR</c> token in Applesoft BASIC, which is used to handle errors
    /// by specifying a recovery point in the program.
    /// </summary>
    ONERR,

    /// <summary>
    /// Represents the <c>RESUME</c> token in Applesoft BASIC, which is used to continue execution
    /// from the point where an error occurred, typically in conjunction with <c>ONERR</c>.
    /// </summary>
    RESUME,

    /// <summary>
    /// Represents the ampersand ('&amp;') operator token in the Applesoft BASIC interpreter.
    /// </summary>
    /// <remarks>
    /// The ampersand operator in Applesoft BASIC is used to call machine language routines.
    /// When executed, it performs a JSR instruction to memory location $03F5.
    /// This is an original Applesoft BASIC feature, not a custom extension.
    /// </remarks>
    AMPERSAND,      // &

    // Custom extension

    /// <summary>
    /// Represents the <c>SLEEP</c> token in the Applesoft BASIC interpreter.
    /// This token is used to indicate a pause or delay in the execution of a program.
    /// </summary>
    SLEEP,

    // Operators

    /// <summary>
    /// Represents the addition operator ('+') token in the Applesoft BASIC interpreter.
    /// </summary>
    Plus,           // +

    /// <summary>
    /// Represents the subtraction operator token ("-") in the Applesoft BASIC interpreter.
    /// </summary>
    Minus,          // -

    /// <summary>
    /// Represents the multiplication operator (*) token in the Applesoft BASIC interpreter.
    /// </summary>
    Multiply,       // *

    /// <summary>
    /// Represents the division operator ("/") token in the Applesoft BASIC interpreter.
    /// </summary>
    Divide,         // /

    /// <summary>
    /// Represents the exponentiation operator (power) token in the Applesoft BASIC interpreter.
    /// </summary>
    Power,          // ^

    /// <summary>
    /// Represents the equality operator token ("=") in Applesoft BASIC.
    /// </summary>
    Equal,          // =

    /// <summary>
    /// Represents the "not equal" operator token in Applesoft BASIC.
    /// </summary>
    NotEqual,       // <> or ><

    /// <summary>
    /// Represents the "less than" comparison operator (&lt;) in the Applesoft BASIC interpreter.
    /// </summary>
    LessThan,       // <

    /// <summary>
    /// Represents the "greater than" comparison operator (&gt;) in the Applesoft BASIC interpreter.
    /// </summary>
    GreaterThan,    // >

    /// <summary>
    /// Represents the "less than or equal to" comparison operator (&lt;=) in the Applesoft BASIC interpreter.
    /// </summary>
    LessOrEqual,    // <=

    /// <summary>
    /// Represents a token that signifies a "greater than or equal to" (&gt;=) comparison operator
    /// in the Applesoft BASIC interpreter.
    /// </summary>
    GreaterOrEqual, // >=

    // Punctuation

    /// <summary>
    /// Represents the left parenthesis token '(' in the Applesoft BASIC interpreter.
    /// </summary>
    LeftParen,      // (

    /// <summary>
    /// Represents the right parenthesis token <c>)</c> in the Applesoft BASIC interpreter.
    /// </summary>
    RightParen,     // )

    /// <summary>
    /// Represents a comma (',') token in the Applesoft BASIC interpreter.
    /// </summary>
    Comma,          // ,

    /// <summary>
    /// Represents a semicolon (';') token in the Applesoft BASIC interpreter.
    /// </summary>
    Semicolon,      // ;

    /// <summary>
    /// Represents a colon (:) token in the Applesoft BASIC interpreter.
    /// </summary>
    Colon,          // :

    /// <summary>
    /// Represents the dollar sign ('$') token in the Applesoft BASIC interpreter.
    /// </summary>
    Dollar,         // $ (for string variables)

    /// <summary>
    /// Represents the percent (%) token in the Applesoft BASIC interpreter.
    /// </summary>
    Percent,        // % (for integer variables)

    /// <summary>
    /// Represents the hash symbol (#) token in the Applesoft BASIC interpreter.
    /// </summary>
    Hash,           // # (for file numbers)

    /// <summary>
    /// Represents a token type corresponding to a question mark ('?') in the Applesoft BASIC interpreter.
    /// </summary>
    Question,       // ? (shorthand for PRINT)

    /// <summary>
    /// Represents the "@" token in the Applesoft BASIC interpreter.
    /// </summary>
    At,             // @ (for AT in PRINT)

    // Special

    /// <summary>
    /// Represents a token that signifies a newline character in the Applesoft BASIC interpreter.
    /// </summary>
    Newline,

    /// <summary>
    /// Represents the end-of-file (EOF) token in the Applesoft BASIC interpreter.
    /// This token is used to signify the end of the input stream during parsing.
    /// </summary>
    EOF,

    /// <summary>
    /// Represents an unknown or unrecognized token type in the Applesoft BASIC interpreter.
    /// </summary>
    /// <remarks>
    /// This token type is used when the interpreter encounters a token that does not match any known or expected types.
    /// </remarks>
    Unknown,
}