using UnityEngine;

public class CameraFramer : MonoBehaviour
{
    [SerializeField] Camera targetCamera;
    [SerializeField, Range(1f, 2f)] float padding = 1.2f;

    public void FrameAllNodes()
    {
        var nodes = FindObjectsByType<GraphNode>(FindObjectsSortMode.None);
        if (nodes.Length == 0) return;

        Vector3 center = Vector3.zero;
        foreach (var n in nodes)
            center += n.transform.position;
        center /= nodes.Length;

        float radius = 0f;
        foreach (var n in nodes)
            radius = Mathf.Max(radius, Vector3.Distance(center, n.transform.position));

        // Use the tighter of vertical and horizontal FOV so nodes don't clip
        float vFovRad = targetCamera.fieldOfView * Mathf.Deg2Rad;
        float hFovRad = 2f * Mathf.Atan(Mathf.Tan(vFovRad * 0.5f) * targetCamera.aspect);
        float dist = (radius * padding) / Mathf.Tan(Mathf.Min(vFovRad, hFovRad) * 0.5f);

        targetCamera.transform.position = center - targetCamera.transform.forward * dist;
    }
}
