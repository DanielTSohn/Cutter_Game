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
    private int pierceCap;

    [SerializeField, Min(0)]
    private int checkCap;
    [SerializeField]
    private float seperationForce;
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

    private bool sliceRight = false;
    private Vector3 sliceDirection;
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
    }

    private void OnCreated(Info info, MeshCreationData creationData)
    {
        Vector3 averagePosition = Vector3.zero;
        Action<Vector3> OnAveraged = null;

        foreach (var createdObject in creationData.CreatedObjects)
        {
            if (createdObject == null) continue;

            averagePosition += createdObject.transform.position;
            if (createdObject.TryGetComponent(out Rigidbody rb))
            {
                OnAveraged += (position) =>
                {
                    if (rb != null)
                        rb.AddForce((rb.position - averagePosition).normalized * seperationForce + sliceDirection * sliceForce, ForceMode.VelocityChange);
                };
            }
        }

        averagePosition /= creationData.CreatedObjects.Length;
        OnAveraged?.Invoke(averagePosition);

        int layer = info.MeshTarget.gameObject.layer;
        string tag = info.MeshTarget.gameObject.tag;
        foreach (var createdTarget in creationData.CreatedTargets)
        {
            if (createdTarget == null) continue;

            createdTarget.gameObject.layer = layer;
            createdTarget.tag = tag;
        }
    }

    public void Cut()
    {
        sliceRight = !sliceRight;

        long pierceCount = pierceCap;
        for (int i = 0; i < hitCount; i++)
        {
            if (colliders[i] == null) continue;

            if (colliders[i].TryGetComponent(out Target target))
            {
                pierceCount -= target.PierceValue;
                if (pierceCount < 0) break;

                if (--target.PierceValue <= 0) target.OnSlice();
            }

            if (colliders[i].TryGetComponent(out MeshTarget meshTarget))
            {
                Cut(meshTarget, transform.position + slicePlaneOffset, transform.up, null, OnCreated);
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
