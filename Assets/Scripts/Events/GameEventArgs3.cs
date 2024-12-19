using System;
using UnityEngine;

namespace GameEventSystem
{
    public class GameEvent<T1, T2, T3> : ScriptableObject
    {
        public event Action<T1, T2, T3> Event;

        public void Invoke(T1 arg1, T2 arg2, T3 arg3) { Event?.Invoke(arg1, arg2, arg3); }
    }
}