// <copyright file="TokenType.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.Tokens;

/// <summary>
/// Represents all token types in Applesoft BASIC.
/// </summary>
public enum TokenType
{
    // Literals
    Number,
    String,
    Identifier,

    // Keywords - Program Control
    REM,
    LET,
    DIM,
    DEF,
    FN,
    END,
    STOP,

    // Keywords - Flow Control
    GOTO,
    GOSUB,
    RETURN,
    ON,
    IF,
    THEN,
    ELSE,
    FOR,
    TO,
    STEP,
    NEXT,

    // Keywords - I/O
    PRINT,
    INPUT,
    GET,
    DATA,
    READ,
    RESTORE,

    // Keywords - Graphics (stubbed)
    GR,
    HGR,
    HGR2,
    TEXT,
    COLOR,
    HCOLOR,
    PLOT,
    HPLOT,
    DRAW,
    XDRAW,
    HTAB,
    VTAB,
    HOME,
    INVERSE,
    FLASH,
    NORMAL,

    // Keywords - Sound
    // (Apple II didn't have dedicated sound commands in Applesoft)

    // Keywords - Memory/System
    PEEK,
    POKE,
    CALL,
    HIMEM,
    LOMEM,
    CLEAR,
    NEW,
    RUN,
    LIST,
    CONT,

    // Keywords - String/Array
    MID_S,      // MID$
    LEFT_S,     // LEFT$
    RIGHT_S,    // RIGHT$
    LEN,
    VAL,
    STR_S,      // STR$
    CHR_S,      // CHR$
    ASC,

    // Keywords - Math Functions
    ABS,
    ATN,
    COS,
    EXP,
    INT,
    LOG,
    RND,
    SGN,
    SIN,
    SQR,
    TAN,

    // Keywords - Utility Functions
    FRE,
    POS,
    SCRN,
    PDL,
    USR,

    // Keywords - Other
    TAB,
    SPC,
    NOT,
    AND,
    OR,

    // Keywords - Disk/File (ProDOS)
    OPEN,
    CLOSE,
    PRINT_FILE,  // PRINT#
    INPUT_FILE,  // INPUT#
    GET_FILE,    // GET#
    ONERR,
    RESUME,

    // Custom extension
    SLEEP,

    // Operators
    Plus,           // +
    Minus,          // -
    Multiply,       // *
    Divide,         // /
    Power,          // ^
    Equal,          // =
    NotEqual,       // <> or ><
    LessThan,       // <
    GreaterThan,    // >
    LessOrEqual,    // <=
    GreaterOrEqual, // >=

    // Punctuation
    LeftParen,      // (
    RightParen,     // )
    Comma,          // ,
    Semicolon,      // ;
    Colon,          // :
    Dollar,         // $ (for string variables)
    Percent,        // % (for integer variables)
    Hash,           // # (for file numbers)
    Question,       // ? (shorthand for PRINT)
    At,             // @ (for AT in PRINT)

    // Special
    Newline,
    EOF,
    Unknown
}
