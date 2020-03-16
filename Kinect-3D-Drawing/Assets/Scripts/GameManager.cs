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
    Neutral,
    Drawing,
    Erasing,
    Zooming,
    Rotating,
    Undo,
    Redo
}

public class GameManager : MonoBehaviour
{
    VisualGestureBuilderDatabase _gestureDatabase;
    VisualGestureBuilderFrameSource _gestureFrameSource;
    VisualGestureBuilderFrameReader _gestureFrameReader;
    Kinect.KinectSensor _kinect;
    Gesture thumbs_down;
    Gesture thumbs_up;
    ParticleSystem _ps;

    [SerializeField]
    private Drawing draw;
    [SerializeField]
    private Erase erase;
    // Add other classes
    VisualGestureBuilderFrameArrivedEventArgs e;

    [SerializeField]
    private float threshold;

    private ProcessState CurrentState { get; set; }
    public BodySourceView bodyView;
    public GameObject AttachedObject;
    private LinkedList<string> handStates;     //Keep track of past 10 frames of hand states
    private Dictionary<string, int> handCount; //Keep track of amount of hand states

    void Awake()
    {
        Neutral(); // Set initial gesture to neutral
        draw.bodyView = bodyView;
        handStates = new LinkedList<string>();
        handCount = new Dictionary<string, int>();
    }

    void Start()
    {
        //gestureController = new GestureController();
        //gestureController.GestureRecognized += OnGestureRecognized;
        if (AttachedObject != null)
        {
            _ps = AttachedObject.GetComponent<ParticleSystem>();
            _ps.emissionRate = 4;
            _ps.startColor = Color.blue;
        }
        _kinect = Kinect.KinectSensor.GetDefault();

        _gestureDatabase = VisualGestureBuilderDatabase.Create(Application.streamingAssetsPath + "/Thumbs_down.gbd");
        _gestureFrameSource = VisualGestureBuilderFrameSource.Create(_kinect, 0);

        //Debug.Log("Before loop");
        foreach (var gesture in _gestureDatabase.AvailableGestures)
        {
            Debug.Log(_gestureDatabase.AvailableGestures);
            _gestureFrameSource.AddGesture(gesture);

            if (gesture.Name == "thumbs_down")
            {
                thumbs_down = gesture;
                //Debug.Log("Confirmed gesture");
            }
            if (gesture.Name == "Thumbs_up")
            {
                thumbs_up = gesture;
            }
        }

        _gestureFrameReader = _gestureFrameSource.OpenReader();
        _gestureFrameReader.IsPaused = true;
        //_gestureFrameReader_FrameArrived();
    }
    void _gestureFrameReader_FrameArrived(object sender, VisualGestureBuilderFrameArrivedEventArgs e)
    {
        Debug.Log("called");
        VisualGestureBuilderFrameReference frameReference = e.FrameReference;
        using (VisualGestureBuilderFrame frame = frameReference.AcquireFrame())
        {
            if (frame != null && frame.DiscreteGestureResults != null)
            {
                if (AttachedObject == null)
                    return;
                

                DiscreteGestureResult result = null;

                if (frame.DiscreteGestureResults.Count > 0)
                    result = frame.DiscreteGestureResults[thumbs_down];
                Debug.Log(result);
                if (result == null)
                    return;
                if(result != null)
                {
                    Debug.Log("detected?");
                }
                if (result.Detected == true)
                {
                    var progressResult = frame.ContinuousGestureResults[thumbs_up];
                    if (AttachedObject != null)
                    {
                        var prog = progressResult.Progress;
                        float scale = 0.5f + prog * 3.0f;
                        AttachedObject.transform.localScale = new Vector3(scale, scale, scale);
                        if (_ps != null)
                        {
                            _ps.emissionRate = 100 * prog;
                            _ps.startColor = Color.red;
                        }
                    }
                }
                else
                {
                    if (_ps != null)
                    {
                        _ps.emissionRate = 4;
                        _ps.startColor = Color.blue;
                    }
                }
            }
        }
    }

    public void SetTrackingId(ulong id)
    {
        _gestureFrameReader.IsPaused = false;
        _gestureFrameSource.TrackingId = id;
        _gestureFrameReader.FrameArrived += _gestureFrameReader_FrameArrived;
    }

    void Update()
    {
        //Currently only doing Right Hand
        foreach (KeyValuePair<ulong, BodySourceView.BodyValue> body in bodyView.GetBodyGameObject())
        {
            Kinect.Body b = body.Value.body;
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
            //attempts
            /*if(thumbs_down == true)
            {
                Debug.Log("Compare");
            }
            if(body.Value.body == thumbs_down)
            {
                 Debug.Log("Compare");
            }*/
            if (DetermineGesture(body.Value.body) == thumbs_down)
            {
                Debug.Log("Compare");
            }
        }
    }

    private void DetermineGesture(Kinect.Body body)
    {
        Tuple<string, int> max = MaxOccurrence();
        if (max.Item2 >= (threshold / 100) * handStates.Count)
        {
            CurrentState = GetState(max.Item1);
            CallClass(body);
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
            case "Opened":
                return Erase();
            case "Zooming":
                return Zoom();
            case "Rotating":
                return Rotate();
          //  case thumbs_down:
           // Debug.Log("VAL: TRUE");
                return 0;
            default:
                return Neutral();
        }
    }


   /* private void OnGestureRecognized(object sender, Gestures e)
    {
        switch (e.GestureName)
        {
            case thumbs_down:
                // do what you want to do
                Debug.Log("working");
                break;

            default:
                break;
        }
    }*/
    /// <summary>
    /// Call the appropriate class based on current gesture state
    /// </summary>
    /// <param name="body">Kinect Body</param>
    private void CallClass (Kinect.Body body)
    {
        switch (CurrentState)
        {
            case ProcessState.Neutral:
                break;
            case ProcessState.Drawing:
                draw.Draw(body);
                break;
            case ProcessState.Erasing:
                break;
            case ProcessState.Zooming:
                break;
     //       case thumbs_down:
       //         Debug.Log("in process");
            case ProcessState.Rotating:
                break;
        }
    }

    private ProcessState Neutral() { return ProcessState.Neutral; }
    private ProcessState Draw() { return ProcessState.Drawing; }
    private ProcessState Erase() { return ProcessState.Erasing; }
    private ProcessState Zoom() { return ProcessState.Zooming; }
    private ProcessState Rotate() { return ProcessState.Rotating; }
}
