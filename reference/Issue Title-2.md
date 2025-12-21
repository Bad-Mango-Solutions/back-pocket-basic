**Create a GitHub issue for implementing Stage 1.5 of the Applesoft BASIC interpreter refactoring plan.**

## Issue Title

`Refactor: Extract ExecutionVisitor and separate parsing from execution`

## Issue Body

### Problem Statement
The current `BasicInterpreter` class violates the Single Responsibility Principle in two ways:

1. **Mixed Concerns**: It combines orchestration (program flow control) with execution (statement/expression evaluation) by implementing both `IBasicInterpreter` and `IAstVisitor<BasicValue>`.
2. **Parse-Execute Coupling**: The `Run(string source)` method performs both parsing and execution, preventing alternative loading strategies (e.g., loading from tokenized Apple II memory format).

This design limits extensibility for future visitors (e.g., `TokenizationVisitor` for SAVE/LOAD, `ListingVisitor` for LIST command).

### Current State

**BasicInterpreter.cs (900+ lines):**
```csharp
public class BasicInterpreter : IBasicInterpreter, IAstVisitor<BasicValue>
{
    // 50+ Visit* methods mixed with orchestration logic
    public void Run(string source)
    {
        program = parser.Parse(source);  // Parsing coupled with execution
        runtime.Clear();
        Execute();  // Calls Visit* methods directly
    }
    
    public BasicValue VisitPrintStatement(PrintStatement node) { ... }
    public BasicValue VisitForStatement(ForStatement node) { ... }
    // ... 48 more Visit* methods
}
```

### Proposed Solution

#### 1. Extract Execution Visitor
Move all `Visit*` methods into a dedicated `ExecutionVisitor` class that focuses solely on statement/expression evaluation.

#### 2. Separate Parsing from Execution
Split `Run(string source)` into two methods:
- `ProgramNode LoadFromSource(string source)` - Parsing only
- `void Run(ProgramNode program)` - Execution only

#### 3. Simplified Architecture
```
BasicInterpreter (Orchestrator)
├── LoadFromSource(string) → ProgramNode
├── Run(ProgramNode) → uses ExecutionVisitor
└── Flow control methods (JumpToLine, Stop, etc.)

ExecutionVisitor (Statement Executor)
├── VisitPrintStatement(...)
├── VisitForStatement(...)
└── 50+ Visit* methods
```

### Proposed Interfaces

**Updated IBasicInterpreter:**
```csharp
public interface IBasicInterpreter
{
    IAppleSystem AppleSystem { get; }
    
    /// <summary>
    /// Parses BASIC source code into an AST program representation.
    /// </summary>
    ProgramNode LoadFromSource(string source);
    
    /// <summary>
    /// Executes a parsed BASIC program.
    /// </summary>
    void Run(ProgramNode program);
    
    /// <summary>
    /// Stops execution of the currently running program.
    /// </summary>
    void Stop();
    
    // Internal APIs for ExecutionVisitor
    void JumpToLine(int lineNumber);
    int GetCurrentLineNumber();
}
```

**New ExecutionVisitor:**
```csharp
public class ExecutionVisitor : IAstVisitor<BasicValue>
{
    private readonly IBasicRuntimeContext runtime;
    private readonly ISystemContext system;
    private readonly IBasicInterpreter interpreter;
    private readonly ILogger logger;
    private Random random;
    
    public ExecutionVisitor(
        IBasicRuntimeContext runtime,
        ISystemContext system,
        IBasicInterpreter interpreter,
        ILogger<ExecutionVisitor> logger)
    {
        this.runtime = runtime;
        this.system = system;
        this.interpreter = interpreter;
        this.logger = logger;
        random = new Random();
    }
    
    // All 50+ Visit* methods moved here
    public BasicValue VisitPrintStatement(PrintStatement node) { ... }
    public BasicValue VisitForStatement(ForStatement node) { ... }
    // ... etc
}
```

**Simplified BasicInterpreter:**
```csharp
public class BasicInterpreter : IBasicInterpreter
{
    private readonly IParser parser;
    private readonly IBasicRuntimeContext runtime;
    private readonly ISystemContext system;
    private readonly ILogger<BasicInterpreter> logger;
    private readonly Dictionary<int, int> lineNumberIndex = [];
    
    private ProgramNode? program;
    private int currentLineIndex;
    private int currentStatementIndex;
    private bool running;
    private bool shouldStop;
    
    public ProgramNode LoadFromSource(string source)
    {
        logger.LogInformation("Parsing BASIC source code");
        var program = parser.Parse(source);
        
        // Build line number index
        lineNumberIndex.Clear();
        for (int i = 0; i < program.Lines.Count; i++)
        {
            lineNumberIndex[program.Lines[i].LineNumber] = i;
        }
        
        return program;
    }
    
    public void Run(ProgramNode program)
    {
        this.program = program;
        logger.LogInformation("Starting BASIC program execution");
        
        try
        {
            runtime.Clear();
            runtime.Data.Initialize(program.DataValues);
            
            currentLineIndex = 0;
            currentStatementIndex = 0;
            running = true;
            shouldStop = false;
            
            var executor = new ExecutionVisitor(runtime, system, this, 
                logger.CreateChildLogger<ExecutionVisitor>());
            
            while (running && !shouldStop)
            {
                if (currentLineIndex >= program.Lines.Count)
                    break;
                    
                var line = program.Lines[currentLineIndex];
                
                while (currentStatementIndex < line.Statements.Count)
                {
                    if (shouldStop) break;
                    
                    var statement = line.Statements[currentStatementIndex];
                    
                    try
                    {
                        statement.Accept(executor);
                    }
                    catch (GotoException ex)
                    {
                        JumpToLine(ex.LineNumber);
                        break;
                    }
                    catch (NextIterationException)
                    {
                        break;
                    }
                    
                    currentStatementIndex++;
                }
                
                if (currentStatementIndex >= line.Statements.Count)
                {
                    currentLineIndex++;
                    currentStatementIndex = 0;
                }
            }
        }
        catch (ProgramEndException)
        {
            logger.LogInformation("Program ended normally");
        }
        catch (BasicRuntimeException ex)
        {
            system.IO.WriteLine();
            system.IO.WriteLine(ex.Message);
            logger.LogError(ex, "Runtime error");
        }
        finally
        {
            running = false;
        }
    }
    
    public void JumpToLine(int lineNumber)
    {
        if (!lineNumberIndex.TryGetValue(lineNumber, out int index))
            throw new BasicRuntimeException("?UNDEF'DP STATEMENT ERROR", GetCurrentLineNumber());
            
        currentLineIndex = index;
        currentStatementIndex = 0;
    }
    
    public int GetCurrentLineNumber()
    {
        if (program != null && currentLineIndex < program.Lines.Count)
            return program.Lines[currentLineIndex].LineNumber;
        return 0;
    }
    
    public void Stop() => shouldStop = true;
}
```

### Implementation Tasks

#### Phase 1: Extract ExecutionVisitor
- [ ] Create `src/ApplesoftBasic.Interpreter/Execution/ExecutionVisitor.cs`
- [ ] Move all 50+ `Visit*` methods from `BasicInterpreter` to `ExecutionVisitor`
- [ ] Pass `IBasicInterpreter` reference to `ExecutionVisitor` for flow control
- [ ] Update `BasicInterpreter` to create and use `ExecutionVisitor` instance
- [ ] Remove `IAstVisitor<BasicValue>` from `BasicInterpreter` class declaration

#### Phase 2: Separate Parsing from Execution
- [ ] Add `ProgramNode LoadFromSource(string source)` method to `BasicInterpreter`
- [ ] Change `void Run(string source)` to `void Run(ProgramNode program)`
- [ ] Update `IBasicInterpreter` interface with both methods
- [ ] Move line number index building to `LoadFromSource`

#### Phase 3: Update Consumers
- [ ] Update `Program.cs` in `ApplesoftBasic.Console` to call `LoadFromSource` then `Run`
- [ ] Update all test files to use new two-step execution pattern
- [ ] Add convenience extension method `RunFromSource(string)` if needed for tests

#### Phase 4: Update DI Registration
- [ ] Register `ExecutionVisitor` in `InterpreterModule.cs` (if needed as transient)
- [ ] Verify `IBasicInterpreter` registration still works correctly

### Files to Create/Modify

**New Files:**
- `src/ApplesoftBasic.Interpreter/Execution/ExecutionVisitor.cs` (~800 lines - all Visit* methods)

**Modified Files:**
- `src/ApplesoftBasic.Interpreter/Execution/IBasicInterpreter.cs` (interface changes)
- `src/ApplesoftBasic.Interpreter/Execution/BasicInterpreter.cs` (remove Visit* methods, split Run)
- `src/ApplesoftBasic.Console/Program.cs` (update to two-step execution)
- `tests/ApplesoftBasic.Tests/InterpreterTests.cs` (update test patterns)
- All other test files that call `Run(string)`

### Acceptance Criteria

- [ ] `ExecutionVisitor` contains all 50+ `Visit*` methods
- [ ] `BasicInterpreter` no longer implements `IAstVisitor<BasicValue>`
- [ ] `BasicInterpreter` exposes `LoadFromSource(string)` and `Run(ProgramNode)`
- [ ] All existing tests pass with new two-step execution pattern
- [ ] Build succeeds with zero warnings (SA1518, SA1600 compliance)
- [ ] Code follows repository style guidelines (StyleCop clean)
- [ ] XML documentation complete for all public members
- [ ] No functional changes - pure refactoring

### Benefits

1. **Single Responsibility**: Interpreter orchestrates; visitor executes
2. **Extensibility**: Easy to add `TokenizationVisitor`, `ListingVisitor` later
3. **Testability**: Test execution logic separately from flow control
4. **Apple II Compatibility**: Enables loading from tokenized memory format (`$0800`)
5. **Future-Ready**: Prepares for interactive mode (LIST, SAVE, LOAD commands)

### Technical Notes

- This is a **pure refactoring** with no functional changes
- All existing behavior must be preserved
- ExecutionVisitor needs `IBasicInterpreter` reference for `JumpToLine` and `GetCurrentLineNumber`
- `Random` instance state must be preserved in `ExecutionVisitor` for `RND()` function
- Exception handling strategy (GotoException, NextIterationException) unchanged

### Dependencies
- Requires Stage 1 completion (dual contexts: `IBasicRuntimeContext`, `ISystemContext`)

### Estimated Effort
3-5 hours

### Labels
`refactor`, `enhancement`, `stage-1.5`, `visitor-pattern`, `single-responsibility`

### Related Documentation
- See `reference/Emulator Evolution Plan.md` for full context
- Stage 2 (CLI) can proceed after this refactoring
- Stage 8 (File I/O) will add `TokenizationVisitor` for SAVE/LOAD
- Stage 11 (Polish) will add `ListingVisitor` for LIST command

---

**Additional Instructions for Copilot:**
- Use C# 14 and .NET 10 features where appropriate
- Follow existing code style (StyleCop compliance)
- Ensure all public members have XML documentation comments
- Maintain backward compatibility for tests (add extension methods if helpful)
- Preserve exception handling and flow control semantics exactly
- Keep `Random` instance state in `ExecutionVisitor` for `RND()` function consistency