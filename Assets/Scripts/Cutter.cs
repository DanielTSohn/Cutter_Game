using DynamicMeshCutter;
using System;
using UnityEngine;

public class Cutter : MonoBehaviour
{
    [Header("Dependencies"), Space(5)]
    [SerializeField]
    private TriggerBehaviour cutBehaviour;

    [Header("Slice Parameters"), Space(5)]
    [SerializeField]
    private float seperationForce;
    [SerializeField]
    private float sliceForce;

    public bool SliceRight => sliceRight;
    private bool sliceRight = false;
    private Vector3 sliceDirection;

    private void Awake()
    {
        cutBehaviour.OnCreated += OnCreated;
    }

    private void OnCreated(Info info, MeshCreationData creationData)
    {
        Debug.Log("Created");
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

    public void StartCut()
    {
        sliceDirection = cutBehaviour.transform.right;
        if (sliceRight) sliceDirection *= -1;

        cutBehaviour.enabled = true;
    }
    [VInspector.Button]
    public void EndCut()
    {
        cutBehaviour.enabled = false;
        sliceRight = !sliceRight;
    }
}
