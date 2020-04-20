using System;
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
/// Represents different gesture states
/// </summary>
public enum ProcessState
{
    None,
    Neutral,
    Drawing,
    Erasing,
    Zooming,
    Rotating
}

public class GameManager : MonoBehaviour
{

    private static GameManager _instance;

    public static GameManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = GameObject.FindObjectOfType<GameManager>();
            }

            return _instance;
        }
    }

    private class Strokes
    {
        public int startIndex;
        public int endIndex;
    }

    //Different referenced classes
    [SerializeField] private Drawing draw;
    public Erase erase;
    [SerializeField] private Recognizer recognizer;

    [SerializeField] private int frameDelay;

    public ProcessState CurrentState { get; set; }
    public BodySourceView bodyView;

    private List<Tuple<int, int>> strokesList; //Keep track of strokes
    public static List<GameObject> spheres;

    #region Temp
    int startingStrokeIndex;
    int? sphereCollidedIndex;
    int frameCount = 0;
    #endregion Temp

    void Awake()
    {
        Neutral(); // Set initial gesture to neutral
        draw.bodyView = bodyView;
        erase.bodyView = bodyView;
        strokesList = new List<Tuple<int, int>>();
        spheres = new List<GameObject>();
    }

    private void Start()
    {
        frameCount = 0;
        CurrentState = ProcessState.None;
    }

    void Update()
    {
        //Get Kinect body data
        foreach (KeyValuePair<ulong, BodySourceView.BodyValue> body in bodyView.GetBodyGameObject())
        {

            if (CurrentState == ProcessState.None) frameCount = 0;

            Kinect.Body b = body.Value.body;
            recognizer.Recognize(b);

            //Change color of line renderer
            if (recognizer.checkClear()) clearCanvas();
            draw.ChangeColor(recognizer.getColor());

            string rightHandState = recognizer.getRightHandGesture();

            if (rightHandState == "Unknown" || rightHandState == "NotTracked") rightHandState = "Neutral";

            //Determine if a new stroke has started
            bool strokeStart = DetermineStroke(GetState(rightHandState));

            //Call the appropriate functions
            CallClass(body.Value.body, strokeStart);
        }
        frameCount++;
        if (frameCount > frameDelay)
            frameCount = 0;
    }

    /// <summary>
    /// Returns true it beginning of stroke
    /// </summary>
    /// <param name="state"></param>
    /// <returns></returns>
    private bool DetermineStroke(ProcessState state)
    {
        //Set previous and current states
        ProcessState prev = CurrentState;
        CurrentState = state;

        bool strokeStart = false;
        if (prev != ProcessState.Drawing && CurrentState == ProcessState.Drawing && spheres.Count > 0)
        {
            startingStrokeIndex = spheres.Count - 1;
            strokeStart = true;
        }
        if (prev == ProcessState.Drawing && CurrentState != prev)
        {
            Tuple<int, int> tempStroke = Tuple.Create(startingStrokeIndex, spheres.Count - 1);
            strokesList.Add(tempStroke);
            frameCount = 0;
        }
        return strokeStart;
    }

    private void clearCanvas()
    {
        CurrentState = ProcessState.Erasing;
        for(int i = 0; i < spheres.Count; i++)
        {
            erase.Eraser(i);
        }
    }

    /// <summary>
    /// Change the state of the FSM
    /// </summary>
    /// <param name="state">Current State</param>
    private ProcessState GetState(string state)
    {
        switch (state)
        {
            case "Neutral":
                return Neutral();
            case "Draw":
                return Draw();
            case "Erase":
                return Erase();
            case "Zooming":
                return Zoom();
            case "Rotating":
                return Rotate();
            default:
                return Neutral();
        }
    }

    /// <summary>
    /// Call the appropriate class based on current gesture state
    /// </summary>
    /// <param name="body">Kinect Body</param>
    private void CallClass(Kinect.Body body, bool strokeStart)
    {
        switch (CurrentState)
        {
            case ProcessState.Neutral:
                break;
            case ProcessState.Drawing:
                if (frameCount >= frameDelay || strokeStart == true)
                    draw.Draw(body, strokeStart);
                break;
            case ProcessState.Erasing:
                erase.Eraser(sphereCollidedIndex);
                break;
            case ProcessState.Zooming:
                break;
            case ProcessState.Rotating:
                break;
        }
    }

    //Body or Hand collided with sphere
    public void SphereCollided(int index)
    {
        sphereCollidedIndex = index;
    }

    private ProcessState None() { return ProcessState.None; }
    private ProcessState Neutral() { return ProcessState.Neutral; }
    private ProcessState Draw() { return ProcessState.Drawing; }
    private ProcessState Erase() { return ProcessState.Erasing; }
    private ProcessState Zoom() { return ProcessState.Zooming; }
    private ProcessState Rotate() { return ProcessState.Rotating; }
}