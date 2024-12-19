using System;
using UnityEngine;

namespace GameEventSystem
{
    public class GameEvent<T1, T2, T3, T4, T5, T6, T7, T8> : ScriptableObject
    {
        public event Action<T1, T2, T3, T4, T5, T6, T7, T8> Event;

        public void Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8) { Event?.Invoke(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8); }
    }
}