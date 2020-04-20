using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Zoom : MonoBehaviour
{
    public GameObject cam;
    //Maximum amount we want the camera to zoom in or out
    [SerializeField] private float maxZ, minZ, speed;
    private Vector3 zoomInTarget, zoomOutTarget;

    private void Start()
    {
        zoomInTarget = cam.transform.position;
        zoomInTarget.z = maxZ;

        zoomOutTarget = cam.transform.position;
        zoomOutTarget.z = minZ;
    }

    /// <summary>
    /// Move the camera in the positive Z direction
    /// </summary>
    public void ZoomIn()
    {
        Debug.Log("ZoomIn");
        cam.transform.position = Vector3.MoveTowards(cam.transform.position, zoomInTarget, speed * Time.deltaTime);
    }

    /// <summary>
    /// Move the camera in the negative Z direction
    /// </summary>
    public void ZoomOut()
    {
        Debug.Log("ZoomOut");
        cam.transform.position = Vector3.MoveTowards(cam.transform.position, zoomOutTarget, speed * Time.deltaTime);
    }
}
