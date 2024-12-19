using System;
using UnityEngine;

namespace GameEventSystem
{
    public class GameEvent<T> : ScriptableObject
    {
        public event Action<T> Event;

        public void Invoke(T arg1) { Event?.Invoke(arg1); }
    }
}