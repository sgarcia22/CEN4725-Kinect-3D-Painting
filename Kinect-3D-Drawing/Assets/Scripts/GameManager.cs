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

    [SerializeField]
    private Drawing draw;
    public Erase erase;
    [SerializeField]
    private Recognizer recognizer;
    // Add other classes

    [SerializeField]
    private float threshold;

    public ProcessState CurrentState { get; set; }
    public BodySourceView bodyView;

    private LinkedList<string> handStates;     //Keep track of past 10 frames of hand states
    private Dictionary<string, int> handCount; //Keep track of amount of hand states
    private List<Tuple<int, int>> strokesList; //Keep track of strokes
    public static List<GameObject> spheres;

    #region Temp
    int startingStrokeIndex;
    int? sphereCollidedIndex;
    #endregion Temp

    void Awake()
    {
        Neutral(); // Set initial gesture to neutral
        draw.bodyView = bodyView;
        erase.bodyView = bodyView;
        handStates = new LinkedList<string>();
        handCount = new Dictionary<string, int>();
        strokesList = new List<Tuple<int, int>>();
        spheres = new List<GameObject>();
    }

    private void Start()
    {
    }

    void Update()
    {
        //Currently only doing Right Hand
        foreach (KeyValuePair<ulong, BodySourceView.BodyValue> body in bodyView.GetBodyGameObject())
        {
            Kinect.Body b = body.Value.body;
            recognizer.Test(b);
            string rightHandState = b.HandRightState.ToString();
            if (rightHandState == "Unknown" || rightHandState == "NotTracked") rightHandState = "Neutral";

            if (handStates.Count > 10)
            {
                handCount[handStates.First.Value]--;
                handStates.RemoveFirst();
            }

            if (!handCount.ContainsKey(rightHandState)) handCount.Add(rightHandState, 0);
            handStates.AddLast(rightHandState);
            handCount[rightHandState]++;

            DetermineGesture(body.Value.body);
        }
    }

    private void DetermineGesture(Kinect.Body body)
    {
        Tuple<string, int> max = MaxOccurrence();
        if (max.Item2 >= (threshold / 100) * handStates.Count)
        {
            ProcessState prev = CurrentState;
            CurrentState = GetState(max.Item1);
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
            }
            CallClass(body, strokeStart);
        }
    }

    /// <summary>
    /// Determine maximum occurance
    /// </summary>
    /// <returns>Maximum occurance of hand state in Dictionary</returns>
    private Tuple<string, int> MaxOccurrence()
    {
        string maxState = "";
        int maxCount = 0;

        foreach (KeyValuePair<string, int> entry in handCount)
        {
            if (entry.Value > maxCount)
            {
                maxState = entry.Key;
                maxCount = entry.Value;
            }
        }

        return Tuple.Create(maxState, maxCount);
    }

    /// <summary>
    /// Change the state of the FSM
    /// </summary>
    /// <param name="state">Current State</param>
    private ProcessState GetState(string state)
    {
        switch (state)
        {
            case "Closed":
                return Neutral();
            case "Lasso":
                return Draw();
            case "Open":
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
                draw.Draw(body, strokeStart);
                break;
            case ProcessState.Erasing:
                //erase.Eraser(body, sphereCollidedIndex);
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

    private ProcessState Neutral() { return ProcessState.Neutral; }
    private ProcessState Draw() { return ProcessState.Drawing; }
    private ProcessState Erase() { return ProcessState.Erasing; }
    private ProcessState Zoom() { return ProcessState.Zooming; }
    private ProcessState Rotate() { return ProcessState.Rotating; }
}