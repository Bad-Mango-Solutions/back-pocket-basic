// <copyright file="GosubManager.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.Runtime;

/// <summary>
/// Default implementation of GOSUB manager.
/// </summary>
public class GosubManager : IGosubManager
{
    private readonly Stack<GosubReturnAddress> _stack = new();

    public int Depth => _stack.Count;

    public void Push(GosubReturnAddress address)
    {
        _stack.Push(address);
    }

    public GosubReturnAddress Pop()
    {
        if (_stack.Count == 0)
        {
            throw new BasicRuntimeException("?RETURN WITHOUT GOSUB ERROR");
        }
        return _stack.Pop();
    }

    public void Clear()
    {
        _stack.Clear();
    }
}
