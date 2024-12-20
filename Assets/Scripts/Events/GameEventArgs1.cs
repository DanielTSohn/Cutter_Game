using System;
using UnityEngine;

namespace GameEventSystem
{
    public abstract class GameEvent<T> : ScriptableObject
    {
        public event Action<T> Event;

        public void Invoke(T arg1) { Event?.Invoke(arg1); }
    }

    [CreateAssetMenu(fileName = "Int Game Event", menuName = "Game Events/Single Argument/Int")]
    public class IntGameEvent : GameEvent<int> { }

    [CreateAssetMenu(fileName = "Float Game Event", menuName = "Game Events/Single Argument/Float")]
    public class FloatGameEvent : GameEvent<float> { }

    [CreateAssetMenu(fileName = "String Game Event", menuName = "Game Events/Single Argument/String")]
    public class StringGameEvent : GameEvent<string> { }

    [CreateAssetMenu(fileName = "Bool Game Event", menuName = "Game Events/Single Argument/Bool")]
    public class BoolGameEvent : GameEvent<bool> { }

    [CreateAssetMenu(fileName = "Vector2 Game Event", menuName = "Game Events/Single Argument/Vector2")]
    public class Vector2GameEvent : GameEvent<Vector2> { }
    [CreateAssetMenu(fileName = "Vector2Int Game Event", menuName = "Game Events/Single Argument/Vector2Int")]
    public class Vector2IntGameEvent : GameEvent<Vector2Int> { }

    [CreateAssetMenu(fileName = "Vector3 Game Event", menuName = "Game Events/Single Argument/Vector3")]
    public class Vector3GameEvent : GameEvent<Vector3> { }
    [CreateAssetMenu(fileName = "Vector3Int Game Event", menuName = "Game Events/Single Argument/Vector3Int")]
    public class Vector3IntGameEvent : GameEvent<Vector3Int> { }

    [CreateAssetMenu(fileName = "Quaternion Game Event", menuName = "Game Events/Single Argument/Quaternion")]
    public class QuaternionGameEvent : GameEvent<Quaternion> { }

    [CreateAssetMenu(fileName = "Transform Game Event", menuName = "Game Events/Single Argument/Transform")]
    public class TransformGameEvent : GameEvent<Transform> { }
}