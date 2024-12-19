using System;
using UnityEngine;

namespace GameEventSystem
{
    public class GameEvent<T1, T2> : ScriptableObject
    {
        public event Action<T1, T2> Event;

        public void Invoke(T1 arg1, T2 arg2) { Event?.Invoke(arg1, arg2); }
    }
}