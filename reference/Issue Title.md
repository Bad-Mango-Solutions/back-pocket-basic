**Create a GitHub issue for implementing Stage 1 of the Applesoft BASIC interpreter refactoring plan.**

## Issue Title
`Refactor: Introduce SystemContext pattern to reduce constructor bloat`

## Issue Body

### Problem Statement
The current `BasicInterpreter` constructor has 9 parameters, which will become unmanageable as we add more features (disk controllers, expansion cards, file managers). This refactoring is a prerequisite for the 11-stage emulator evolution plan.

**Current Constructor:**
```csharp
public BasicInterpreter(
    IParser parser,
    IBasicIO io,
    IVariableManager variables,
    IFunctionManager functions,
    IDataManager data,
    ILoopManager loops,
    IGosubManager gosub,
    IAppleSystem appleSystem,
    ILogger<BasicInterpreter> logger)
```

### Proposed Solution
Introduce a `SystemContext` pattern to aggregate related dependencies into a single context object.

**Target Constructor:**
```csharp
public BasicInterpreter(
    IParser parser,
    ISystemContext context)
```

**New Context Interface:**
```csharp
public interface ISystemContext
{
    IAppleSystem System { get; }
    IBasicIO IO { get; }
    IVariableManager Variables { get; }
    IFunctionManager Functions { get; }
    IDataManager Data { get; }
    ILoopManager Loops { get; }
    IGosubManager Gosub { get; }
    ILogger Logger { get; }
}
```

### Implementation Tasks
- [ ] Create `src/ApplesoftBasic.Interpreter/Runtime/ISystemContext.cs` with interface definition
- [ ] Create `src/ApplesoftBasic.Interpreter/Runtime/SystemContext.cs` with implementation
- [ ] Modify `src/ApplesoftBasic.Interpreter/Execution/BasicInterpreter.cs` to accept `ISystemContext`
- [ ] Update `src/ApplesoftBasic.Interpreter/InterpreterModule.cs` DI registration
- [ ] Update all test files to use new constructor signature
- [ ] Ensure all existing unit tests pass
- [ ] Verify build produces no warnings (SA1518, SA1600 compliance)

### Acceptance Criteria
- [ ] `ISystemContext` interface created with all 8 current dependencies
- [ ] `SystemContext` class implements interface with proper XML documentation
- [ ] `BasicInterpreter` constructor reduced from 9 parameters to 2
- [ ] Autofac `InterpreterModule` registers `SystemContext` as `ISystemContext`
- [ ] All existing tests pass without modification to test logic
- [ ] Build succeeds with zero warnings
- [ ] Code follows repository style guidelines (no SA1518/SA1600 suppressions)

### Technical Notes
- This is a **pure refactoring** with no functional changes
- All existing behavior must be preserved
- This change enables future expansion without breaking existing code
- Next stage (Stage 2) will add command-line infrastructure building on this foundation

### Files to Modify
**New Files:**
- `src/ApplesoftBasic.Interpreter/Runtime/ISystemContext.cs`
- `src/ApplesoftBasic.Interpreter/Runtime/SystemContext.cs`

**Modified Files:**
- `src/ApplesoftBasic.Interpreter/Execution/BasicInterpreter.cs`
- `src/ApplesoftBasic.Interpreter/InterpreterModule.cs`
- `tests/ApplesoftBasic.Tests/*.cs` (test fixture setup)

### Dependencies
None - this is the foundation stage

### Estimated Effort
2-4 hours

### Labels
`refactor`, `enhancement`, `good first issue`, `stage-1`

### Related Documentation
See `reference/Emulator Evolution Plan.md` for full context of the 11-stage evolution plan.

---

**Additional Instructions for Copilot:**
- Use C# 14 and .NET 10 features where appropriate
- Follow existing code style (StyleCop compliance)
- Ensure all public members have XML documentation comments
- Use `InstancePerLifetimeScope` for DI registration
- Maintain backward compatibility with existing tests