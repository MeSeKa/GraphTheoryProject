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

        Vector3 centroid  = Vector3.zero;
        float   maxRadius = 0f;
        int     count     = 0;

        foreach (var t in tiles)
        {
            centroid += t.transform.position;
            count++;
        }
        if (count == 0) return;
        centroid /= count;

        foreach (var t in tiles)
        {
            float d = Vector2.Distance(
                new Vector2(t.transform.position.x, t.transform.position.z),
                new Vector2(centroid.x, centroid.z));
            if (d > maxRadius) maxRadius = d;
        }

        float halfFov  = cam.fieldOfView * 0.5f * Mathf.Deg2Rad;
        float dist     = (maxRadius + padding) / Mathf.Tan(halfFov);

        Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
        Vector3    back = rot * Vector3.back;

        cam.transform.position = centroid - back * dist;
        cam.transform.rotation = rot;
    }

    private void Reset()
    {
        cam = GetComponent<Camera>();
    }
}
