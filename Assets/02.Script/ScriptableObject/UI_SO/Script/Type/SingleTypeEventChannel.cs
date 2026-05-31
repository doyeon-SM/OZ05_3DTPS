using System;
using UnityEngine;

public abstract class SingleTypeEventChannel<T> : ScriptableObject
{
    private event Action<T> listeners;

    public void Raise(T value)
    {
        listeners?.Invoke(value);
    }

    public void Register(Action<T> listener)
    {
        listeners += listener;
    }

    public void Unregister(Action<T> listener)
    {
        listeners -= listener;
    }
}