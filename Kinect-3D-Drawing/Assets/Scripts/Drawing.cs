using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Kinect = Windows.Kinect;

/// <summary>
///  Possible values for Hand States
/// </summary>
public class HandStates
{
    public string Closed = "Closed";     //Fist
    public string Open = "Open";       //Palm Open
    public string Lasso = "Lasso";       //Pointing
}

/// <summary>
/// Kinect 3D Drawing Script
/// TODO:
///     -Draw spheres wherever hand tip joint goes 
///         -If too bad, then can use palm joint and approximate where finger is
///     -Connect spheres with Line Renderer
/// </summary>
public class Drawing : MonoBehaviour
{

    public BodySourceView bodyView;
    private LineRenderer lr;
    private HandStates states;

    public GameObject sphere, parent;
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
    }

    void Update()
    {
        foreach (KeyValuePair<ulong, BodySourceView.BodyValue> body in bodyView.GetBodyGameObject())
        {
            //if (body.Value.body.HandLeftState.ToString().Equals(states.Lasso.ToString())) Draw (body, true);
            //if (body.Value.body.HandRightState.ToString().Equals(states.Lasso.ToString())) Draw(body, false);
            Kinect.Body b = body.Value.body;
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
    
    /// <summary>
    /// Draw with Line Renderer
    /// </summary>
    /// <param name="body">Represents the Kinect Body Data</param>
    private void Draw(KeyValuePair<ulong, BodySourceView.BodyValue> body, bool leftHand)
    {
        Transform handLocation;
        
    }
}
