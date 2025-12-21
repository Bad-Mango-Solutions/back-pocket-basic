// <copyright file="BasicParser.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace BadMango.Basic.Parser;

using AST;

using Lexer;

using Microsoft.Extensions.Logging;

using Tokens;

/// <summary>
/// Parser for Applesoft BASIC.
/// </summary>
public class BasicParser : IParser
{
    private readonly ILexer lexer;
    private readonly ILogger<BasicParser> logger;
    private List<Token> tokens = [];
    private int current;

    /// <summary>
    /// Initializes a new instance of the <see cref="BasicParser"/> class.
    /// </summary>
    /// <param name="lexer">The lexer used to tokenize the Applesoft BASIC source code.</param>
    /// <param name="logger">The logger used for logging parser-related activities.</param>
    public BasicParser(ILexer lexer, ILogger<BasicParser> logger)
    {
        this.lexer = lexer;
        this.logger = logger;
    }

    /// <summary>
    /// Parses the provided Applesoft BASIC source code and constructs an abstract syntax tree (AST) representation.
    /// </summary>
    /// <param name="source">The Applesoft BASIC source code to parse.</param>
    /// <returns>
    /// A <see cref="ProgramNode"/> representing the parsed program, including its lines, statements, and data values.
    /// </returns>
    /// <remarks>
    /// This method tokenizes the source code, processes each line, and organizes the resulting statements into a
    /// structured program. Empty lines are skipped, and lines are sorted by their line numbers in ascending order.
    /// </remarks>
    public ProgramNode Parse(string source)
    {
        tokens = lexer.Tokenize(source);
        current = 0;

        var program = new ProgramNode();

        logger.LogDebug("Starting parse of {Count} tokens", tokens.Count);

        while (!IsAtEnd())
        {
            // Skip empty lines
            while (Check(TokenType.Newline))
            {
                Advance();
            }

            if (IsAtEnd())
            {
                break;
            }

            var line = ParseLine();
            if (line != null)
            {
                program.Lines.Add(line);

                // Collect DATA values
                foreach (var stmt in line.Statements)
                {
                    if (stmt is DataStatement data)
                    {
                        program.DataValues.AddRange(data.Values);
                    }
                }
            }
        }

        // Sort lines by line number
        program.Lines.Sort((a, b) => a.LineNumber.CompareTo(b.LineNumber));

        logger.LogDebug("Parse complete. {Count} lines parsed", program.Lines.Count);

        return program;
    }

    private static bool IsBuiltInFunction(TokenType type)
    {
        return type switch
        {
            // Math functions
            TokenType.ABS or TokenType.ATN or TokenType.COS or TokenType.EXP or
                TokenType.INT or TokenType.LOG or TokenType.RND or TokenType.SGN or
                TokenType.SIN or TokenType.SQR or TokenType.TAN or

            // String functions
                TokenType.LEN or TokenType.VAL or TokenType.ASC or
                TokenType.MID_S or TokenType.LEFT_S or TokenType.RIGHT_S or
                TokenType.STR_S or TokenType.CHR_S or

            // Utility functions
                TokenType.PEEK or TokenType.FRE or TokenType.POS or
                TokenType.SCRN or TokenType.PDL or TokenType.USR or
                TokenType.TAB or TokenType.SPC => true,
            _ => false,
        };
    }

    private LineNode? ParseLine()
    {
        // Expect a line number
        if (!Check(TokenType.Number))
        {
            throw new ParseException("Expected line number", Current().Line, Current().Column);
        }

        var lineNumToken = Advance();
        int lineNumber = (int)(double)lineNumToken.Value!;

        var line = new LineNode(lineNumber);

        // Parse statements until end of line
        while (!Check(TokenType.Newline) && !IsAtEnd())
        {
            var statement = ParseStatement();
            if (statement != null)
            {
                line.Statements.Add(statement);
            }

            // Colon separates multiple statements on a line
            if (Check(TokenType.Colon))
            {
                Advance();
            }
        }

        // Consume the newline
        if (Check(TokenType.Newline))
        {
            Advance();
        }

        return line;
    }

    private IStatement? ParseStatement()
    {
        // Handle REM specially - consumes rest of line
        if (Check(TokenType.REM))
        {
            return ParseRem();
        }

        if (Check(TokenType.PRINT) || Check(TokenType.Question))
        {
            return ParsePrint();
        }

        if (Check(TokenType.INPUT))
        {
            return ParseInput();
        }

        if (Check(TokenType.LET))
        {
            Advance(); // consume LET
            return ParseAssignment();
        }

        if (Check(TokenType.IF))
        {
            return ParseIf();
        }

        if (Check(TokenType.GOTO))
        {
            return ParseGoto();
        }

        if (Check(TokenType.GOSUB))
        {
            return ParseGosub();
        }

        if (Check(TokenType.RETURN))
        {
            Advance();
            return new ReturnStatement();
        }

        if (Check(TokenType.FOR))
        {
            return ParseFor();
        }

        if (Check(TokenType.NEXT))
        {
            return ParseNext();
        }

        if (Check(TokenType.DIM))
        {
            return ParseDim();
        }

        if (Check(TokenType.READ))
        {
            return ParseRead();
        }

        if (Check(TokenType.DATA))
        {
            return ParseData();
        }

        if (Check(TokenType.RESTORE))
        {
            return ParseRestore();
        }

        if (Check(TokenType.END))
        {
            Advance();
            return new EndStatement();
        }

        if (Check(TokenType.STOP))
        {
            Advance();
            return new StopStatement();
        }

        if (Check(TokenType.POKE))
        {
            return ParsePoke();
        }

        if (Check(TokenType.CALL))
        {
            return ParseCall();
        }

        if (Check(TokenType.GET))
        {
            return ParseGet();
        }

        if (Check(TokenType.ON))
        {
            return ParseOn();
        }

        if (Check(TokenType.DEF))
        {
            return ParseDef();
        }

        if (Check(TokenType.HOME))
        {
            Advance();
            return new HomeStatement();
        }

        if (Check(TokenType.HTAB))
        {
            return ParseHtab();
        }

        if (Check(TokenType.VTAB))
        {
            return ParseVtab();
        }

        if (Check(TokenType.TEXT))
        {
            Advance();
            return new TextStatement();
        }

        if (Check(TokenType.GR))
        {
            Advance();
            return new GrStatement();
        }

        if (Check(TokenType.HGR))
        {
            Advance();
            return new HgrStatement { IsHgr2 = false };
        }

        if (Check(TokenType.HGR2))
        {
            Advance();
            return new HgrStatement { IsHgr2 = true };
        }

        if (Check(TokenType.COLOR))
        {
            return ParseColor();
        }

        if (Check(TokenType.HCOLOR))
        {
            return ParseHcolor();
        }

        if (Check(TokenType.PLOT))
        {
            return ParsePlot();
        }

        if (Check(TokenType.HPLOT))
        {
            return ParseHplot();
        }

        if (Check(TokenType.DRAW))
        {
            return ParseDraw();
        }

        if (Check(TokenType.XDRAW))
        {
            return ParseXdraw();
        }

        if (Check(TokenType.INVERSE))
        {
            Advance();
            return new InverseStatement();
        }

        if (Check(TokenType.FLASH))
        {
            Advance();
            return new FlashStatement();
        }

        if (Check(TokenType.NORMAL))
        {
            Advance();
            return new NormalStatement();
        }

        if (Check(TokenType.CLEAR))
        {
            Advance();
            return new ClearStatement();
        }

        if (Check(TokenType.SLEEP))
        {
            return ParseSleep();
        }

        if (Check(TokenType.HIMEM))
        {
            return ParseHimem();
        }

        if (Check(TokenType.LOMEM))
        {
            return ParseLomem();
        }

        if (Check(TokenType.AMPERSAND))
        {
            Advance();
            return new AmpersandStatement();
        }

        // Implicit LET (assignment without LET keyword)
        if (Check(TokenType.Identifier))
        {
            return ParseAssignment();
        }

        // Skip unknown tokens
        if (!Check(TokenType.Newline) && !Check(TokenType.Colon) && !IsAtEnd())
        {
            logger.LogWarning("Unexpected token: {Token}", Current());
            Advance();
        }

        return null;
    }

    private RemStatement ParseRem()
    {
        Advance(); // consume REM

        // Collect the rest of the line as comment
        int start = current;
        while (!Check(TokenType.Newline) && !IsAtEnd())
        {
            Advance();
        }

        // Build comment from tokens
        var comment = string.Join(string.Empty, tokens.Skip(start).Take(current - start).Select(t => t.Lexeme));
        return new(comment.Trim());
    }

    private PrintStatement ParsePrint()
    {
        Advance(); // consume PRINT or ?
        var stmt = new PrintStatement();

        while (!Check(TokenType.Newline) && !Check(TokenType.Colon) && !IsAtEnd())
        {
            // Handle TAB and SPC functions
            if (Check(TokenType.TAB) || Check(TokenType.SPC))
            {
                var funcType = Current().Type;
                Advance();
                Consume(TokenType.LeftParen, "Expected '(' after TAB/SPC");
                var arg = ParseExpression();
                Consume(TokenType.RightParen, "Expected ')' after TAB/SPC argument");

                var funcExpr = new FunctionCallExpression(funcType);
                funcExpr.Arguments.Add(arg);
                stmt.Expressions.Add(funcExpr);
            }
            else if (Check(TokenType.Semicolon))
            {
                Advance();
                stmt.Separators.Add(PrintSeparator.Semicolon);
                stmt.EndsWithSeparator = true;
            }
            else if (Check(TokenType.Comma))
            {
                Advance();
                stmt.Separators.Add(PrintSeparator.Comma);
                stmt.EndsWithSeparator = true;
            }
            else
            {
                var expr = ParseExpression();
                stmt.Expressions.Add(expr);
                stmt.EndsWithSeparator = false;

                // Add None separator after expression if more items follow
                if (stmt.Separators.Count < stmt.Expressions.Count - 1)
                {
                    stmt.Separators.Add(PrintSeparator.None);
                }
            }
        }

        return stmt;
    }

    private InputStatement ParseInput()
    {
        Advance(); // consume INPUT
        var stmt = new InputStatement();

        // Check for optional prompt string
        if (Check(TokenType.String))
        {
            stmt.Prompt = (string)Advance().Value!;
            if (Check(TokenType.Semicolon))
            {
                Advance();
            }
        }

        // Parse variable list
        do
        {
            if (Check(TokenType.Comma))
            {
                Advance();
            }

            var varName = Consume(TokenType.Identifier, "Expected variable name").Lexeme;
            stmt.Variables.Add(new(varName));
        }
        while (Check(TokenType.Comma));

        return stmt;
    }

    private LetStatement ParseAssignment()
    {
        var varToken = Consume(TokenType.Identifier, "Expected variable name");
        var varName = varToken.Lexeme;
        List<IExpression>? indices = null;

        // Check for array subscript
        if (Check(TokenType.LeftParen))
        {
            Advance();
            indices = [ParseExpression()];
            while (Check(TokenType.Comma))
            {
                Advance();
                indices.Add(ParseExpression());
            }

            Consume(TokenType.RightParen, "Expected ')' after array subscript");
        }

        Consume(TokenType.Equal, "Expected '=' in assignment");
        var value = ParseExpression();

        var stmt = new LetStatement(new(varName), value) { ArrayIndices = indices };
        return stmt;
    }

    private IfStatement ParseIf()
    {
        Advance(); // consume IF
        var condition = ParseExpression();

        Consume(TokenType.THEN, "Expected THEN after IF condition");

        var stmt = new IfStatement(condition);

        // Check if THEN is followed by a line number (implicit GOTO)
        if (Check(TokenType.Number))
        {
            var lineNum = (int)(double)Advance().Value!;
            stmt.GotoLineNumber = lineNum;
        }
        else
        {
            // Parse statements after THEN
            while (!Check(TokenType.Newline) && !Check(TokenType.Colon) && !IsAtEnd())
            {
                var thenStmt = ParseStatement();
                if (thenStmt != null)
                {
                    stmt.ThenBranch.Add(thenStmt);
                }

                if (Check(TokenType.Colon))
                {
                    Advance();
                }
            }
        }

        return stmt;
    }

    private GotoStatement ParseGoto()
    {
        Advance(); // consume GOTO
        var lineNum = (int)(double)Consume(TokenType.Number, "Expected line number after GOTO").Value!;
        return new(lineNum);
    }

    private GosubStatement ParseGosub()
    {
        Advance(); // consume GOSUB
        var lineNum = (int)(double)Consume(TokenType.Number, "Expected line number after GOSUB").Value!;
        return new(lineNum);
    }

    private ForStatement ParseFor()
    {
        Advance(); // consume FOR
        var varName = Consume(TokenType.Identifier, "Expected variable name").Lexeme;
        Consume(TokenType.Equal, "Expected '=' after FOR variable");
        var start = ParseExpression();
        Consume(TokenType.TO, "Expected TO in FOR statement");
        var end = ParseExpression();

        var stmt = new ForStatement(varName, start, end);

        if (Check(TokenType.STEP))
        {
            Advance();
            stmt.Step = ParseExpression();
        }

        return stmt;
    }

    private NextStatement ParseNext()
    {
        Advance(); // consume NEXT
        var stmt = new NextStatement();

        // NEXT can have no variable, or one or more variables
        if (Check(TokenType.Identifier))
        {
            stmt.Variables.Add(Advance().Lexeme);
            while (Check(TokenType.Comma))
            {
                Advance();
                stmt.Variables.Add(Consume(TokenType.Identifier, "Expected variable name").Lexeme);
            }
        }

        return stmt;
    }

    private DimStatement ParseDim()
    {
        Advance(); // consume DIM
        var stmt = new DimStatement();

        do
        {
            if (Check(TokenType.Comma))
            {
                Advance();
            }

            var arrayName = Consume(TokenType.Identifier, "Expected array name").Lexeme;
            var decl = new ArrayDeclaration(arrayName);

            Consume(TokenType.LeftParen, "Expected '(' after array name");
            decl.Dimensions.Add(ParseExpression());
            while (Check(TokenType.Comma))
            {
                Advance();
                decl.Dimensions.Add(ParseExpression());
            }

            Consume(TokenType.RightParen, "Expected ')' after dimensions");

            stmt.Arrays.Add(decl);
        }
        while (Check(TokenType.Comma));

        return stmt;
    }

    private ReadStatement ParseRead()
    {
        Advance(); // consume READ
        var stmt = new ReadStatement();

        do
        {
            if (Check(TokenType.Comma))
            {
                Advance();
            }

            var varName = Consume(TokenType.Identifier, "Expected variable name").Lexeme;
            stmt.Variables.Add(new(varName));
        }
        while (Check(TokenType.Comma));

        return stmt;
    }

    private DataStatement ParseData()
    {
        Advance(); // consume DATA
        var stmt = new DataStatement();

        do
        {
            if (Check(TokenType.Comma))
            {
                Advance();
            }

            if (Check(TokenType.Number))
            {
                stmt.Values.Add((double)Advance().Value!);
            }
            else if (Check(TokenType.String))
            {
                stmt.Values.Add((string)Advance().Value!);
            }
            else if (Check(TokenType.Identifier))
            {
                // Unquoted string in DATA
                stmt.Values.Add(Advance().Lexeme);
            }
            else if (Check(TokenType.Minus))
            {
                // Negative number
                Advance();
                if (Check(TokenType.Number))
                {
                    stmt.Values.Add(-(double)Advance().Value!);
                }
            }
            else
            {
                break;
            }
        }
        while (Check(TokenType.Comma));

        return stmt;
    }

    private RestoreStatement ParseRestore()
    {
        Advance(); // consume RESTORE
        var stmt = new RestoreStatement();

        if (Check(TokenType.Number))
        {
            stmt.LineNumber = (int)(double)Advance().Value!;
        }

        return stmt;
    }

    private PokeStatement ParsePoke()
    {
        Advance(); // consume POKE
        var address = ParseExpression();
        Consume(TokenType.Comma, "Expected ',' in POKE statement");
        var value = ParseExpression();
        return new(address, value);
    }

    private CallStatement ParseCall()
    {
        Advance(); // consume CALL
        var address = ParseExpression();
        return new(address);
    }

    private GetStatement ParseGet()
    {
        Advance(); // consume GET
        var varName = Consume(TokenType.Identifier, "Expected variable name").Lexeme;
        return new(new(varName));
    }

    private IStatement ParseOn()
    {
        Advance(); // consume ON
        var expr = ParseExpression();

        if (Check(TokenType.GOTO))
        {
            Advance();
            var stmt = new OnGotoStatement(expr);
            do
            {
                if (Check(TokenType.Comma))
                {
                    Advance();
                }

                var lineNum = (int)(double)Consume(TokenType.Number, "Expected line number").Value!;
                stmt.LineNumbers.Add(lineNum);
            }
            while (Check(TokenType.Comma));
            return stmt;
        }
        else if (Check(TokenType.GOSUB))
        {
            Advance();
            var stmt = new OnGosubStatement(expr);
            do
            {
                if (Check(TokenType.Comma))
                {
                    Advance();
                }

                var lineNum = (int)(double)Consume(TokenType.Number, "Expected line number").Value!;
                stmt.LineNumbers.Add(lineNum);
            }
            while (Check(TokenType.Comma));
            return stmt;
        }

        throw new ParseException("Expected GOTO or GOSUB after ON expression", Current().Line, Current().Column);
    }

    private DefStatement ParseDef()
    {
        Advance(); // consume DEF
        Consume(TokenType.FN, "Expected FN after DEF");

        var funcName = Consume(TokenType.Identifier, "Expected function name").Lexeme;
        Consume(TokenType.LeftParen, "Expected '(' after function name");
        var param = Consume(TokenType.Identifier, "Expected parameter name").Lexeme;
        Consume(TokenType.RightParen, "Expected ')' after parameter");
        Consume(TokenType.Equal, "Expected '=' in function definition");
        var body = ParseExpression();

        return new(funcName, param, body);
    }

    private HtabStatement ParseHtab()
    {
        Advance(); // consume HTAB
        var column = ParseExpression();
        return new(column);
    }

    private VtabStatement ParseVtab()
    {
        Advance(); // consume VTAB
        var row = ParseExpression();
        return new(row);
    }

    private ColorStatement ParseColor()
    {
        Advance(); // consume COLOR; it can be followed by = sign

        if (Check(TokenType.Equal))
        {
            Advance();
        }

        var color = ParseExpression();
        return new(color);
    }

    private HcolorStatement ParseHcolor()
    {
        Advance(); // consume HCOLOR
        if (Check(TokenType.Equal))
        {
            Advance();
        }

        var color = ParseExpression();
        return new(color);
    }

    private PlotStatement ParsePlot()
    {
        Advance(); // consume PLOT
        var x = ParseExpression();
        Consume(TokenType.Comma, "Expected ',' in PLOT statement");
        var y = ParseExpression();
        return new(x, y);
    }

    private HplotStatement ParseHplot()
    {
        Advance(); // consume HPLOT
        var stmt = new HplotStatement();

        var x = ParseExpression();
        Consume(TokenType.Comma, "Expected ',' in HPLOT statement");
        var y = ParseExpression();
        stmt.Points.Add((x, y));

        // Handle TO for line drawing
        while (Check(TokenType.TO))
        {
            Advance();
            x = ParseExpression();
            Consume(TokenType.Comma, "Expected ',' in HPLOT statement");
            y = ParseExpression();
            stmt.Points.Add((x, y));
        }

        return stmt;
    }

    private DrawStatement ParseDraw()
    {
        Advance(); // consume DRAW
        var shapeNum = ParseExpression();
        var stmt = new DrawStatement(shapeNum);

        if (Check(TokenType.At))
        {
            Advance();
            stmt.AtX = ParseExpression();
            Consume(TokenType.Comma, "Expected ',' after AT X coordinate");
            stmt.AtY = ParseExpression();
        }

        return stmt;
    }

    private XdrawStatement ParseXdraw()
    {
        Advance(); // consume XDRAW
        var shapeNum = ParseExpression();
        var stmt = new XdrawStatement(shapeNum);

        if (Check(TokenType.At))
        {
            Advance();
            stmt.AtX = ParseExpression();
            Consume(TokenType.Comma, "Expected ',' after AT X coordinate");
            stmt.AtY = ParseExpression();
        }

        return stmt;
    }

    private SleepStatement ParseSleep()
    {
        Advance(); // consume SLEEP
        var ms = ParseExpression();
        return new(ms);
    }

    private HimemStatement ParseHimem()
    {
        Advance(); // consume HIMEM
        if (Check(TokenType.Colon))
        {
            Advance(); // HIMEM:
        }

        var address = ParseExpression();
        return new(address);
    }

    private LomemStatement ParseLomem()
    {
        Advance(); // consume LOMEM
        if (Check(TokenType.Colon))
        {
            Advance(); // LOMEM:
        }

        var address = ParseExpression();
        return new(address);
    }

    #region Expression Parsing

    private IExpression ParseExpression()
    {
        return ParseOr();
    }

    private IExpression ParseOr()
    {
        var expr = ParseAnd();

        while (Check(TokenType.OR))
        {
            var op = Advance().Type;
            var right = ParseAnd();
            expr = new BinaryExpression(expr, op, right);
        }

        return expr;
    }

    private IExpression ParseAnd()
    {
        var expr = ParseNot();

        while (Check(TokenType.AND))
        {
            var op = Advance().Type;
            var right = ParseNot();
            expr = new BinaryExpression(expr, op, right);
        }

        return expr;
    }

    private IExpression ParseNot()
    {
        if (Check(TokenType.NOT))
        {
            var op = Advance().Type;
            var operand = ParseNot();
            return new UnaryExpression(op, operand);
        }

        return ParseComparison();
    }

    private IExpression ParseComparison()
    {
        var expr = ParseAddition();

        while (Check(TokenType.Equal) || Check(TokenType.NotEqual) ||
               Check(TokenType.LessThan) || Check(TokenType.GreaterThan) ||
               Check(TokenType.LessOrEqual) || Check(TokenType.GreaterOrEqual))
        {
            var op = Advance().Type;
            var right = ParseAddition();
            expr = new BinaryExpression(expr, op, right);
        }

        return expr;
    }

    private IExpression ParseAddition()
    {
        var expr = ParseMultiplication();

        while (Check(TokenType.Plus) || Check(TokenType.Minus))
        {
            var op = Advance().Type;
            var right = ParseMultiplication();
            expr = new BinaryExpression(expr, op, right);
        }

        return expr;
    }

    private IExpression ParseMultiplication()
    {
        var expr = ParsePower();

        while (Check(TokenType.Multiply) || Check(TokenType.Divide))
        {
            var op = Advance().Type;
            var right = ParsePower();
            expr = new BinaryExpression(expr, op, right);
        }

        return expr;
    }

    private IExpression ParsePower()
    {
        var expr = ParseUnary();

        if (Check(TokenType.Power))
        {
            var op = Advance().Type;
            var right = ParsePower(); // Right-associative
            expr = new BinaryExpression(expr, op, right);
        }

        return expr;
    }

    private IExpression ParseUnary()
    {
        if (Check(TokenType.Minus))
        {
            var op = Advance().Type;
            var operand = ParseUnary();
            return new UnaryExpression(op, operand);
        }

        if (Check(TokenType.Plus))
        {
            Advance(); // Consume unary +, it's a no-op
            return ParseUnary();
        }

        return ParsePrimary();
    }

    private IExpression ParsePrimary()
    {
        // Number literal
        if (Check(TokenType.Number))
        {
            return new NumberLiteral((double)Advance().Value!);
        }

        // String literal
        if (Check(TokenType.String))
        {
            return new StringLiteral((string)Advance().Value!);
        }

        // Parenthesized expression
        if (Check(TokenType.LeftParen))
        {
            Advance();
            var expr = ParseExpression();
            Consume(TokenType.RightParen, "Expected ')' after expression");
            return expr;
        }

        // FN user function call
        if (Check(TokenType.FN))
        {
            Advance();
            var funcName = Consume(TokenType.Identifier, "Expected function name after FN").Lexeme;
            Consume(TokenType.LeftParen, "Expected '(' after function name");
            var arg = ParseExpression();
            Consume(TokenType.RightParen, "Expected ')' after function argument");
            return new UserFunctionExpression(funcName, arg);
        }

        // Built-in functions
        if (IsBuiltInFunction(Current().Type))
        {
            return ParseFunctionCall();
        }

        // Variable or array access
        if (Check(TokenType.Identifier))
        {
            var name = Advance().Lexeme;

            // Check for array subscript
            if (Check(TokenType.LeftParen))
            {
                Advance();
                var arrayAccess = new ArrayAccessExpression(name);
                arrayAccess.Indices.Add(ParseExpression());
                while (Check(TokenType.Comma))
                {
                    Advance();
                    arrayAccess.Indices.Add(ParseExpression());
                }

                Consume(TokenType.RightParen, "Expected ')' after array subscript");
                return arrayAccess;
            }

            return new VariableExpression(name);
        }

        throw new ParseException($"Unexpected token: {Current()}", Current().Line, Current().Column);
    }

    private FunctionCallExpression ParseFunctionCall()
    {
        var funcType = Advance().Type;
        var func = new FunctionCallExpression(funcType);

        Consume(TokenType.LeftParen, $"Expected '(' after {funcType}");

        if (!Check(TokenType.RightParen))
        {
            func.Arguments.Add(ParseExpression());
            while (Check(TokenType.Comma))
            {
                Advance();
                func.Arguments.Add(ParseExpression());
            }
        }

        Consume(TokenType.RightParen, $"Expected ')' after {funcType} arguments");

        return func;
    }

    #endregion

    #region Helper Methods

    private bool Check(TokenType type)
    {
        if (IsAtEnd())
        {
            return false;
        }

        return Current().Type == type;
    }

    private Token Advance()
    {
        if (!IsAtEnd())
        {
            current++;
        }

        return Previous();
    }

    private bool IsAtEnd() => Current().Type == TokenType.EOF;

    private Token Current() => tokens[current];

    private Token Previous() => tokens[current - 1];

    private Token Consume(TokenType type, string message)
    {
        if (Check(type))
        {
            return Advance();
        }

        throw new ParseException(message, Current().Line, Current().Column);
    }

    #endregion
}