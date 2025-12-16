// <copyright file="FunctionManager.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.Runtime;

using AST;

/// <summary>
/// Default implementation of function manager.
/// </summary>
public class FunctionManager : IFunctionManager
{
    private readonly Dictionary<string, UserFunction> _functions = new(StringComparer.OrdinalIgnoreCase);

    public void DefineFunction(string name, string parameter, IExpression body)
    {
        // Normalize function name (first 2 chars only, like variables)
        string normalizedName = NormalizeFunctionName(name);
        _functions[normalizedName] = new UserFunction(normalizedName, parameter, body);
    }

    public UserFunction? GetFunction(string name)
    {
        string normalizedName = NormalizeFunctionName(name);
        return _functions.TryGetValue(normalizedName, out var func) ? func : null;
    }

    public bool FunctionExists(string name)
    {
        return _functions.ContainsKey(NormalizeFunctionName(name));
    }

    public void Clear()
    {
        _functions.Clear();
    }

    private static string NormalizeFunctionName(string name)
    {
        if (name.Length > 2)
        {
            return name[..2].ToUpperInvariant();
        }
        return name.ToUpperInvariant();
    }
}
