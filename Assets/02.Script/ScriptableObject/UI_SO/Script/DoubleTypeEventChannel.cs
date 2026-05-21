using System;
using UnityEngine;

public abstract class DoubleTypeEventChannel<T1,T2> : ScriptableObject
{
    private event Action<T1,T2> listeners;

    public void Raise(T1 value , T2 value2)
    {
        listeners?.Invoke(value,value2);
    }

    public void Register(Action<T1,T2> listener)
    {
        listeners += listener;
    }

    public void Unregister(Action<T1,T2> listener)
    {
        listeners -= listener;
    }
}