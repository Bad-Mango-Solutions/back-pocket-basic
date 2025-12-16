// <copyright file="DataManager.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.Runtime;

/// <summary>
/// Default implementation of data manager.
/// </summary>
public class DataManager : IDataManager
{
    private List<object> _dataValues = new();
    private int _dataPointer;

    public void Initialize(List<object> dataValues)
    {
        _dataValues = new List<object>(dataValues);
        _dataPointer = 0;
    }

    public BasicValue Read()
    {
        if (_dataPointer >= _dataValues.Count)
        {
            throw new BasicRuntimeException("?OUT OF DATA ERROR");
        }

        object value = _dataValues[_dataPointer++];

        return value switch
        {
            double d => BasicValue.FromNumber(d),
            string s => BasicValue.FromString(s),
            _ => BasicValue.FromString(value.ToString() ?? string.Empty)
        };
    }

    public void Restore()
    {
        _dataPointer = 0;
    }

    public void RestoreToPosition(int position)
    {
        if (position < 0 || position >= _dataValues.Count)
        {
            _dataPointer = 0;
        }
        else
        {
            _dataPointer = position;
        }
    }

    public void Clear()
    {
        _dataValues.Clear();
        _dataPointer = 0;
    }
}
