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
    public Material rendMaterial;

    public Material blueLine;
    public Material redLine;
    public Material greenLine;
    public Material purpleLine;
    public Material blackLine;
    public Material orangeLine;

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
    /// Change color of the Line Renderer
    /// </summary>
    /// <param name="newColor"></param>
    public void ChangeColor(string newColor)
    {
        switch(newColor)
        {
            case "Blue":
                rendMaterial = blueLine;
                break;
            case "Red":
                rendMaterial = redLine;
                break;
            case "Green":
                rendMaterial = greenLine;
                break;
            case "Purple":
                rendMaterial = purpleLine;
                break;
            case "Black":
                rendMaterial = blackLine;
                break;
            case "Orange":
                rendMaterial = orangeLine;
                break;
        }
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
        if (spheres.Count != 0) AddLineRenderer(temp, strokeStart);
        spheres.Add(temp);
    }

    /// <summary>
    /// Add Line Renderer to newly created GameObject
    /// </summary>
    /// <param name="temp">Sphere Created</param>
    /// <param name="strokeStart">Start of stroke</param>
    private void AddLineRenderer(GameObject temp, bool strokeStart)
    {
        if (strokeStart) return;

        LineRenderer lr = temp.AddComponent<LineRenderer>();
        int tempIndex = spheres.Count - 1;
        lr.positionCount = 2;
        lr.SetPosition(0, spheres[tempIndex].transform.position);
        lr.SetPosition(1, temp.transform.position);
        lr.startWidth = .2f;
        lr.endWidth = .2f;
        lr.material = rendMaterial;
        lr.numCapVertices = 2;
    }
}