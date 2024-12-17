using DynamicMeshCutter;
using System.Linq;
using UnityEngine;

public class InputCutter
{
    public InputCutter(PlaneBehaviour cutter, int pierceCount)
    {
        planeBehaviour = cutter;
        colliders = new Collider[pierceCount];
    }

    private readonly Collider[] colliders;
    private readonly PlaneBehaviour planeBehaviour;
    private float explosionForce;
    private float explosionRadius;

    public void Cut(in Transform transform, in Vector3 offset, in Vector3 halfExtents, int mask, QueryTriggerInteraction queryTrigger, float force, float radius)
    {
        Physics.OverlapBoxNonAlloc(transform.position + offset, halfExtents, colliders, transform.rotation, mask, queryTrigger);
        foreach (Collider collider in colliders)
        {
            if (collider == null || !collider.TryGetComponent(out MeshTarget target)) continue;
            planeBehaviour.Cut(target, transform.position + offset, transform.up, null, OnCreated);
        }

        explosionForce = force;
        explosionRadius = radius;
    }

    private void OnCreated(Info info, MeshCreationData data)
    {
        /*
        Vector3 averagePosition = Vector3.zero;
        var createdObjects = data.CreatedObjects;
        Rigidbody[] rigidbodies = new Rigidbody[createdObjects.LongLength];

        for (long i = 0; i < createdObjects.LongLength; i++)
        {
            if (createdObjects[i] == null) continue;
            averagePosition += createdObjects[i].transform.position;
            rigidbodies[i] = createdObjects[i].GetComponent<Rigidbody>();
        }
        averagePosition /= createdObjects.LongLength;
        foreach (var item in rigidbodies)
        {
            if (item == null) continue;
            item.AddExplosionForce(explosionForce, averagePosition, explosionRadius, 0.05f, ForceMode.VelocityChange);
            item.mass /= 2;
        };
        */
    }
}
