using System;
using System.Collections.Generic;

public class GameEventBus
{
    private readonly Dictionary<Type, List<Action<GameEvent>>> _subscribers = new Dictionary<Type, List<Action<GameEvent>>>();
    
    public void Subscribe<T>(Action<T> handler) where T : GameEvent
    {
        Type eventType = typeof(T);
        
        if (!_subscribers.ContainsKey(eventType))
        {
            _subscribers[eventType] = new List<Action<GameEvent>>();
        }
        
        _subscribers[eventType].Add(evt => handler((T)evt));
    }
    
    public void Unsubscribe<T>(Action<T> handler) where T : GameEvent
    {
        Type eventType = typeof(T);
        
        if (!_subscribers.ContainsKey(eventType))
        {
            return;
        }
        
        _subscribers[eventType].Remove(evt => handler((T)evt));
    }
    
    public void Raise<T>(T gameEvent) where T : GameEvent
    {
        Type eventType = typeof(T);
        
        if (!_subscribers.ContainsKey(eventType))
        {
            return;
        }
        
        var handlers = _subscribers[eventType];
        for (int i = 0; i < handlers.Count; i++)
        {
            handlers[i].Invoke(gameEvent);
        }
    }
    
    public void Clear()
    {
        _subscribers.Clear();
    }
}
