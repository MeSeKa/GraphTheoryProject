using System.Collections.Generic;
using UnityEngine;

public class IsometricCamera : MonoBehaviour
{
    [SerializeField] Camera  cam;
    [SerializeField] float   yaw     = 45f;
    [SerializeField] float   pitch   = 35f;
    [SerializeField] float   padding = 3f;

    public void FrameTiles(IEnumerable<HexTile> tiles)
    {
        if (cam == null) cam = Camera.main;

        var     positions = new List<Vector3>();
        Vector3 centroid  = Vector3.zero;

        foreach (var t in tiles)
        {
            positions.Add(t.transform.position);
            centroid += t.transform.position;
        }
        if (positions.Count == 0) return;
        centroid /= positions.Count;

        Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
        cam.transform.rotation = rot;

        Vector3 right   = cam.transform.right;
        Vector3 up      = cam.transform.up;
        Vector3 forward = cam.transform.forward;

        if (cam.orthographic)
        {
            float maxX = 0f, maxY = 0f;
            foreach (var pos in positions)
            {
                Vector3 delta = pos - centroid;
                float sx = Mathf.Abs(Vector3.Dot(delta, right));
                float sy = Mathf.Abs(Vector3.Dot(delta, up));
                if (sx > maxX) maxX = sx;
                if (sy > maxY) maxY = sy;
            }
            float sizeH = maxY + padding;
            float sizeW = (maxX + padding) / cam.aspect;
            cam.orthographicSize      = Mathf.Max(sizeH, sizeW);
            cam.transform.position    = centroid - forward * 100f;
        }
        else
        {
            float maxRadius = 0f;
            foreach (var pos in positions)
            {
                float d = Vector2.Distance(
                    new Vector2(pos.x, pos.z),
                    new Vector2(centroid.x, centroid.z));
                if (d > maxRadius) maxRadius = d;
            }
            float halfFov          = cam.fieldOfView * 0.5f * Mathf.Deg2Rad;
            float dist             = (maxRadius + padding) / Mathf.Tan(halfFov);
            cam.transform.position = centroid - forward * dist;
        }
    }

    private void Reset()
    {
        cam = GetComponent<Camera>();
    }
}
