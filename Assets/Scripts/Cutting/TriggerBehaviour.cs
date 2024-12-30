using DynamicMeshCutter;
using System;
using System.Collections.Generic;
using UnityEngine;

public class TriggerBehaviour : CutterBehaviour
{
    [Header("Dependencies"), Space(5)]
    [SerializeField]
    private Collider[] triggers;

    [Header("Slice Parameters"), Space(5)]
    [SerializeField, Min(0)]
    private long pierceCap;

    private long pierceCount;
    private Dictionary<MeshTarget, Vector3> targetStarts = new();

    public event Action<Info, MeshCreationData> OnCreated;

    protected override void OnEnable()
    {
        base.OnEnable();
        pierceCount = pierceCap;
        foreach (var trigger in triggers) trigger.enabled = true;
    }
    protected override void OnDisable()
    {
        base.OnDisable();
        foreach (var trigger in triggers) trigger.enabled = false; 
    }

    private void OnTriggerEnter(Collider other)
    {
        int layer = other.gameObject.layer;

        if (layer == LayerMask.NameToLayer("Sliceable") && other.TryGetComponent(out MeshTarget meshTarget))
        {
            targetStarts.Add(meshTarget, other.ClosestPointOnBounds(transform.position));
        }
    }

    private void OnTriggerExit(Collider other)
    {
        int layer = other.gameObject.layer;

        if (layer == LayerMask.NameToLayer("Target") && other.TryGetComponent(out Target target))
        {
            pierceCount -= target.PierceValue;
            if (pierceCount < 0)
            {
                enabled = false;
                return;
            }

            if (--target.PierceValue <= 0) target.OnSlice();
        }

        if (layer == LayerMask.NameToLayer("Sliceable") && other.TryGetComponent(out MeshTarget meshTarget))
        {
            if (targetStarts.TryGetValue(meshTarget, out Vector3 position))
            {
                Vector3 point = other.ClosestPointOnBounds(transform.position);
                Debug.Log(Vector3.Cross(point - position, transform.forward));
                Cut(meshTarget, (point + position) / 2, Vector3.Cross(point - position, transform.forward), null, (info, data) => OnCreated?.Invoke(info, data));
            }
        }
    }
}
