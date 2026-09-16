using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "StringEventChannel_", menuName = "The Lights Between/Events/String Event Channel")]
public class StringGameEventSO : ScriptableObject
{
    private readonly List<Action<string>> listeners = new List<Action<string>>();

    public void Raise(string value)
    {
        for (int i = listeners.Count - 1; i >= 0; i--)
        {
            listeners[i]?.Invoke(value);
        }
    }

    public void RegisterListener(Action<string> listener)
    {
        if (!listeners.Contains(listener))
        {
            listeners.Add(listener);
        }
    }

    public void UnregisterListener(Action<string> listener)
    {
        listeners.Remove(listener);
    }
}