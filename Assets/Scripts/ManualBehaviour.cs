using UnityEngine;
using DynamicMeshCutter;
using System.Collections.Generic;
using System;

public class ManualBehaviour : CutterBehaviour
{
    private class ColliderComparer : Comparer<Collider>
    {
        public ColliderComparer(Vector3 position)
        {
            comparePosition = position;
        }

        private Vector3 comparePosition;

        public void SetPosition(Vector3 position)
        {
            comparePosition = position;
        }

        public override int Compare(Collider x, Collider y)
        {
            if (x == null && y == null) return 0;
            else if (x == null) return -1;
            else if (y == null) return 1;
            else
            {
                return Vector3.Distance(x.transform.position, comparePosition).
        CompareTo(Vector3.Distance(y.transform.position, comparePosition));
            }
        }
    }

    [Header("Slice Parameters"), Space(5)]
    [SerializeField, Min(0)]
    private long pierceCap;

    [SerializeField, Min(0)]
    private long checkCap;
    [SerializeField]
    private float explosionForce;
    [SerializeField]
    private float sliceForce;
    [SerializeField, Min(0)]
    private Vector3 slicePlaneHalfExtents;
    [SerializeField]
    private Vector3 slicePlaneOffset;
    [SerializeField]
    private LayerMask hitLayers;

    private Collider[] colliders;
    private ColliderComparer distanceCompare;

    private readonly HashSet<Sliceable> sliceables = new();
    private HashSet<Sliceable> previousSliceables = new();

    private bool sliceRight = false;
    private Vector3 sliceDirection;
    private long pierceCount;
    private int hitCount;
    private Vector3 startPosition;

    private void OnValidate()
    {
        colliders = new Collider[checkCap];
        sliceDirection = transform.right;
        distanceCompare = new ColliderComparer(transform.position + slicePlaneOffset + transform.forward * slicePlaneHalfExtents.z + sliceDirection * slicePlaneHalfExtents.x);
    }

    protected override void Update()
    {
        base.Update();
        if (TimeManager.Instance.MenuPause) return;

        CheckPlane();
    }

    private void CheckPlane()
    {
        sliceDirection = transform.right;
        if (sliceRight) sliceDirection *= -1;

        startPosition = transform.position + slicePlaneOffset + transform.forward * slicePlaneHalfExtents.z;
        hitCount = Physics.OverlapBoxNonAlloc(startPosition, slicePlaneHalfExtents, colliders, transform.rotation, hitLayers, QueryTriggerInteraction.Collide);
        distanceCompare.SetPosition(startPosition + sliceDirection * slicePlaneHalfExtents.x);
        Array.Sort(colliders, 0, hitCount, distanceCompare);
        pierceCount = pierceCap;

        sliceables.Clear();
        for (int i = 0; i < hitCount; i++)
        {
            if (colliders[i] != null && colliders[i].TryGetComponent(out Sliceable sliceable))
            {
                sliceables.Add(sliceable);
                pierceCount -= sliceable.PierceValue;
                if (pierceCount < 0)
                {
                    hitCount = i;
                    break;
                }
            }
        }

        if (sliceables.Count > previousSliceables.Count)
        {
            sliceables.ExceptWith(previousSliceables);
            foreach (var sliceable in sliceables)
            {
                sliceable.TriggerHitFeedback();
            }
        }
        else
        {
            previousSliceables.Clear();
        }

        previousSliceables.UnionWith(sliceables);
    }

    public void Cut()
    {
        sliceRight = !sliceRight;

        for (int i = 0; i < hitCount; i++)
        {
            if (colliders[i] == null || !colliders[i].TryGetComponent(out MeshTarget target)) continue;

            Cut(target, transform.position + slicePlaneOffset, transform.up, null, OnCreated);
        }
    }

    private void OnCreated(Info info, MeshCreationData data)
    {
        Vector3 averagePosition = Vector3.zero;
        foreach (var gameObject in data.CreatedObjects)
        {
            if (gameObject == null) continue;
            averagePosition += gameObject.transform.position;
            
        }
        averagePosition /= data.CreatedObjects.Length;

        foreach (var gameObject in data.CreatedObjects)
        {
            if (gameObject != null && gameObject.TryGetComponent(out Rigidbody rb))
            {
                rb.AddForce((rb.position - averagePosition).normalized * explosionForce + sliceDirection * sliceForce, ForceMode.VelocityChange);
            }
        }

        if (info.MeshTarget.TryGetComponent(out Sliceable sliceable))
        {
            sliceable.TriggerSliceFeedback();

            foreach (var target in data.CreatedTargets)
            {
                if (target == null) continue;
                var addedSliceable = target.gameObject.AddComponent<Sliceable>();
                addedSliceable.Initialize(sliceable);
                previousSliceables.Add(addedSliceable);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 debugDirection = Vector3.right;
        if (sliceRight) debugDirection *= -1;
        Gizmos.color = Color.green;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(slicePlaneOffset + Vector3.forward * slicePlaneHalfExtents.z, slicePlaneHalfExtents * 2);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(slicePlaneOffset + debugDirection, Vector3.one);
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(slicePlaneOffset - debugDirection, Vector3.one);
    }
}
