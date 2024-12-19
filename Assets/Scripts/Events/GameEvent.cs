using System;
using UnityEngine;

namespace GameEventSystem
{
    [CreateAssetMenu(fileName = "New Game Event", menuName = "Game Events/Game Event", order = 0)]
    public class GameEvent : ScriptableObject
    {
        public event Action Event;

        public void Invoke() { Event?.Invoke(); }
    }
}