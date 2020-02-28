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
/// </summary>
public class Drawing : MonoBehaviour
{

    public BodySourceView bodyView;
    private LineRenderer lr;
    private HandStates states;

    void Start()
    {
        lr = GetComponent<LineRenderer>();
        states = new HandStates();
    }

    void Update()
    {
        foreach (KeyValuePair<ulong, BodySourceView.BodyValue> body in bodyView.GetBodyGameObject())
        {
            if (body.Value.body.HandLeftState.ToString().Equals(states.Lasso.ToString())) Draw (body, true);
            if (body.Value.body.HandRightState.ToString().Equals(states.Lasso.ToString())) Draw(body, false);
        }
    }

    /// <summary>
    /// Draw with Line Renderer
    /// </summary>
    /// <param name="body">Represents the Kinect Body Data</param>
    private void Draw(KeyValuePair<ulong, BodySourceView.BodyValue> body, bool leftHand)
    {
        Transform handLocation;
        //TODO
        //if (leftHand) body.Value.obj.
    }
}
