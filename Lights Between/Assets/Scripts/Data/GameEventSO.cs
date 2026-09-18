using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GameEventChannel_", menuName = "The Lights Between/Events/Game Event Channel")]
public class GameEventSO : ScriptableObject
{
    private readonly List<Action> listeners = new List<Action>();

    public void Raise()
    {
        for (int i = listeners.Count - 1; i >= 0; i--)
        {
            listeners[i]?.Invoke();
        }
    }

    public void RegisterListener(Action listener)
    {
        if (!listeners.Contains(listener))
        {
            listeners.Add(listener);
        }
    }

    public void UnregisterListener(Action listener)
    {
        listeners.Remove(listener);
    }
}