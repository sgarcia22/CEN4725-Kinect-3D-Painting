using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Kinect = Windows.Kinect;
using Microsoft.Kinect.VisualGestureBuilder;

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
    ZoomIn,
    ZoomOut,
    RotateClockwise,
    RotateCounterClockwise,
    Select,
    Undo,
    Redo
}

public class GameManager : MonoBehaviour
{

    private static GameManager _instance;
    VisualGestureBuilderDatabase _gestureDatabase;
    VisualGestureBuilderFrameSource _gestureFrameSource;
    VisualGestureBuilderFrameReader _gestureFrameReader;
    Kinect.KinectSensor _kinect;
    Gesture thumbs_down;
    Gesture thumbs_up;
    ParticleSystem _ps;

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
        public int startIndex = -1;
        public int endIndex = -1;
    }

    //Different referenced classes
    [SerializeField] private Drawing draw;
    [SerializeField] private Zoom zoom;
    [SerializeField] private Rotate rotate;
    public Erase erase;
    [SerializeField] private Recognizer recognizer;
    [SerializeField] private UserInterface UI;

    [SerializeField] private int frameDelay;

    public ProcessState CurrentStateRight { get; set; }
    public ProcessState CurrentStateLeft { get; set; }
    public BodySourceView bodyView;
    public GameObject AttachedObject;
    private List<Tuple<int, int>> strokesList; //Keep track of strokes
    public static List<GameObject> spheres;

    private Stack<Tuple<int, int>> undoStack;

    #region Temp
    int startingStrokeIndex;
    int? sphereCollidedIndex;
    int frameCount = 0;
    bool undo = false, redo = false;
    #endregion Temp

    void Awake()
    {
        Neutral(); // Set initial gesture to neutral
        draw.bodyView = bodyView;
        erase.bodyView = bodyView;
        strokesList = new List<Tuple<int, int>>();
        spheres = new List<GameObject>();
        undoStack = new Stack<Tuple<int, int>>();
    }

    public void SetTrackingId(ulong id)
    {
        if (_gestureFrameReader != null)
        {

            _gestureFrameReader.IsPaused = false;
            _gestureFrameSource.TrackingId = id;

        }
    }

    private void Start()
    {
        frameCount = 0;
        CurrentStateRight = ProcessState.None;

        _kinect = Kinect.KinectSensor.GetDefault();

        _gestureDatabase = VisualGestureBuilderDatabase.Create(Application.streamingAssetsPath + "/Thumbs_down.gbd");
        _gestureFrameSource = VisualGestureBuilderFrameSource.Create(_kinect, 0);

        foreach (var gesture in _gestureDatabase.AvailableGestures)
        {

            _gestureFrameSource.AddGesture(gesture);

            if (gesture.Name == "thumbs_down")
            {
                thumbs_down = gesture;
            }
            if (gesture.Name == "thumbs_up")
            {
                thumbs_up = gesture;
            }
        }

        _gestureFrameReader = _gestureFrameSource.OpenReader();
        _gestureFrameReader.IsPaused = true;
        _gestureFrameReader.FrameArrived += _gestureFrameReader_FrameArrived;
    }

    /// <summary>
    /// Perform Undo/Redo Functions
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    void _gestureFrameReader_FrameArrived(object sender, VisualGestureBuilderFrameArrivedEventArgs e)
    {
        VisualGestureBuilderFrameReference frameReference = e.FrameReference;
        using (VisualGestureBuilderFrame frame = frameReference.AcquireFrame())
        {
            if (frame != null && frame.DiscreteGestureResults != null)
            {
                DiscreteGestureResult result = null;
                DiscreteGestureResult resultUp = null;

                if (frame.DiscreteGestureResults.Count > 0)
                {

                    result = frame.DiscreteGestureResults[thumbs_down];
                    resultUp = frame.DiscreteGestureResults[thumbs_up];

                }
                if (result == null || resultUp == null)
                    return;

                //Discrete Gesture
                if (result.Detected == true && undo == false)
                {
                    undo = true;
                    Undo();
                }
                else if (result.Detected == false && undo == true)
                {
                    undo = false;
                }

                if (resultUp.Detected == true && redo == false)
                {
                    redo = true;
                    Redo();
                }
                else if (resultUp.Detected == false && redo == true)
                {
                    redo = false;
                }
            }
        }
    }

    void Update()
    {
        //Get Kinect body data
        foreach (KeyValuePair<ulong, BodySourceView.BodyValue> body in bodyView.GetBodyGameObject())
        {

            if (CurrentStateRight == ProcessState.None) frameCount = 0;

            Kinect.Body b = body.Value.body;
            recognizer.Recognize(b);

            //Change color of line renderer
            if (recognizer.checkClear()) clearCanvas();
            draw.ChangeColor(recognizer.getColor());

            string rightHandState = recognizer.getRightHandGesture();
            string leftHandState = recognizer.getLeftHandGesture();

            if (rightHandState == "Unknown" || rightHandState == "NotTracked") rightHandState = "Neutral";
            if (leftHandState == "Unknown" || leftHandState == "NotTracked") leftHandState = "Neutral";

            ProcessState rightPrev = CurrentStateRight;
            ProcessState leftPrev = CurrentStateLeft;

            bool strokeStart = false;

            //Right Hand State
            if (undo || redo)
            {
                CurrentStateRight = (undo) ? ProcessState.Undo : ProcessState.Redo;
            }
            else
            {
                //Determine if a new stroke has started
                 strokeStart = DetermineStroke(GetState(rightHandState));
            }

            //Get state of left hand
            CurrentStateLeft = GetState(leftHandState);

            //Change Sprites of Hands
            if (rightPrev != CurrentStateRight) UI.ChangeSpriteRight(CurrentStateRight);
            if (leftPrev != CurrentStateLeft) UI.ChangeSpriteLeft(CurrentStateLeft);

            //Call the appropriate functions
            CallClass(body.Value.body, strokeStart);
        }
        frameCount++;
        if (frameCount > frameDelay)
            frameCount = 0;
    }

    /// <summary>
    /// Perform undo on stroke
    /// </summary>
    private void Undo()
    {
        int tempIndex = strokesList.Count - 1;
        int cnt = spheres.Count - 1;
        int j=0;
        while (spheres[strokesList[tempIndex].Item1].activeSelf == false && tempIndex >= 0) {
            tempIndex -= 1;
        }
        if (tempIndex < 0) return;

        int diff = strokesList[tempIndex].Item2 - strokesList[tempIndex].Item1;
        while(spheres[(spheres.Count - 1)].activeSelf ==false)
        {
            j++;
        }
        for (int i = diff; i > 0; i--)
        {
            spheres[cnt-i].SetActive(false);
        }
        undoStack.Push(strokesList[(strokesList.Count - 1)]);
    }

    /// <summary>
    /// Perform redo on stroke
    /// </summary>
    private void Redo()
    {
        if (undoStack.Count == 0) return;
        Tuple<int, int> temp = undoStack.Pop();
        int strokeDist = temp.Item2 - temp.Item1;
        for (int i = strokeDist; i > 0; i--)
        {
            spheres[(spheres.Count - 1)-i].SetActive(true);
        }
    }

    /// <summary>
    /// Returns true it beginning of stroke
    /// </summary>
    /// <param name="state"></param>
    /// <returns></returns>
    private bool DetermineStroke(ProcessState state)
    {
        //Set previous and current states
        ProcessState prev = CurrentStateRight;
        CurrentStateRight = state;

        bool strokeStart = false;
        if (prev != ProcessState.Drawing && CurrentStateRight == ProcessState.Drawing && spheres.Count > 0)
        {
            startingStrokeIndex = spheres.Count - 1;
            strokeStart = true;
        }
        if (prev == ProcessState.Drawing && CurrentStateRight != prev)
        {
            Tuple<int, int> tempStroke = Tuple.Create(startingStrokeIndex, spheres.Count - 1);
            strokesList.Add(tempStroke);
            frameCount = 0;
            if (undoStack.Count != 0) undoStack.Clear();
        }
        return strokeStart;
    }

    private void clearCanvas()
    {
        CurrentStateRight = ProcessState.Erasing;
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
            case "ZoomIn":
                return ZoomIn();
            case "ZoomOut":
                return ZoomOut();
            case "RotateClockwise":
                return RotateClockwise();
            case "RotateCounterClockwise":
                return RotateCounterClockwise();
            case "Select":
                return Select();
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
        switch (CurrentStateLeft)
        {
            case ProcessState.ZoomIn:
                zoom.ZoomIn();
                return;
            case ProcessState.ZoomOut:
                zoom.ZoomOut();
                return;
            case ProcessState.RotateClockwise:
                break;
            case ProcessState.RotateCounterClockwise:
                break;
        }
        switch (CurrentStateRight)
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
    private ProcessState ZoomIn() { return ProcessState.ZoomIn; }
    private ProcessState ZoomOut() { return ProcessState.ZoomOut; }
    private ProcessState RotateClockwise() { return ProcessState.RotateClockwise; }
    private ProcessState RotateCounterClockwise() { return ProcessState.RotateCounterClockwise; }
    private ProcessState Select() { return ProcessState.Select; }
}
