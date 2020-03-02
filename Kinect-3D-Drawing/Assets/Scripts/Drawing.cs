using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Kinect = Windows.Kinect;

/// <summary>
/// Kinect 3D Drawing Script
/// </summary>
public class Drawing : MonoBehaviour
{
    public bool drawing;
    public BodySourceView bodyView;
    public GameObject sphere, parent;

    private LineRenderer lr;
    private HandStates states;
    private List<GameObject> spheres;
    private int index;

    void Start()
    {
        lr = GetComponent<LineRenderer>();
        states = new HandStates();
        InitializeVariables();
    }

    private void InitializeVariables ()
    {
        spheres = new List<GameObject>();
        lr.positionCount = 1;
        index = 0;
        drawing = false;
    }

    /// <summary>
    /// Draw with Line Renderer
    /// Currently Right Hand Only
    /// </summary>
    /// <param name="body">Represents the Kinect Body Data</param>
    public void Draw(Kinect.Body b)
    {
        Kinect.Joint sourceJoint = b.Joints[Kinect.JointType.HandRight];
        GameObject temp = Instantiate(sphere, bodyView.GetVector3FromJoint(sourceJoint), Quaternion.identity);
        temp.transform.parent = parent.transform;
        spheres.Add(temp);
        if (spheres.Capacity >= 2)
        {
            lr.positionCount += 1;
            lr.SetPosition(index, spheres[index].transform.position);
            lr.SetPosition(++index, temp.transform.position);
        }
    }
}
