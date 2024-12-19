using System;
using UnityEngine;

namespace GameEventSystem
{
    public class GameEvent<T1, T2, T3, T4> : ScriptableObject
    {
        public event Action<T1, T2, T3, T4> Event;

        public void Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4) { Event?.Invoke(arg1, arg2, arg3, arg4); }
    }
}