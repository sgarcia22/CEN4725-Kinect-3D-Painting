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

    //private LineRenderer lr;
    private HandStates states;
    private int index;

    private List<GameObject> spheres;

    void Start()
    {
        //lr = GetComponent<LineRenderer>();
        states = new HandStates();
        InitializeVariables();
    }

    private void InitializeVariables()
    {
        spheres = GameManager.spheres;
        //lr.positionCount = 1;
        index = 0;
        drawing = false;
    }

    /// <summary>
    /// Draw with Line Renderer
    /// Currently Right Hand Only
    /// </summary>
    /// <param name="body">Represents the Kinect Body Data</param>
    public void Draw(Kinect.Body b, bool strokeStart)
    {
        Kinect.Joint sourceJoint = b.Joints[Kinect.JointType.HandRight];
        GameObject temp = Instantiate(sphere, bodyView.GetVector3FromJoint(sourceJoint), Quaternion.identity);
        temp.transform.parent = parent.transform;
        temp.GetComponent<SphereController>().index = spheres.Count;
        if (spheres.Count != 0)
        {
            AddLineRenderer(temp, strokeStart);
            temp.GetComponent<MeshRenderer>().enabled = false;
            spheres.Add(temp);
            float distance = Vector3.Distance(spheres[spheres.Count - 2].transform.position, temp.transform.position);
            if (distance > 1f)
            {
                temp = Instantiate(sphere, (spheres[spheres.Count - 2].transform.position + temp.transform.position) / 2, Quaternion.identity);
                temp.transform.parent = parent.transform;
                temp.GetComponent<SphereController>().index = spheres.Count;
                AddLineRenderer(temp, false);
                temp.GetComponent<MeshRenderer>().enabled = false;
                spheres.Add(temp);
            }
        }
        else
        {
            temp.GetComponent<MeshRenderer>().enabled = false;
            spheres.Add(temp);
        }
    }

    private void AddLineRenderer(GameObject temp, bool strokeStart)
    {
        if (strokeStart) return;
        LineRenderer lr = temp.AddComponent<LineRenderer>();
        int tempIndex = spheres.Count - 1;
        lr.positionCount = 2;
        lr.SetPosition(0, spheres[tempIndex].transform.position);
        lr.SetPosition(1, temp.transform.position);
        lr.startWidth = .1f;
        lr.endWidth = .1f;
    }
}