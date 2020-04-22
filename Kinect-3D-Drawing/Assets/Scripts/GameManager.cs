﻿using System;
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
    }

    private void Start()
    {
        frameCount = 0;
        CurrentStateRight = ProcessState.None;
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
            string discreteGesture = recognizer.checkForDiscreteGesture();

            if (rightHandState == "Unknown" || rightHandState == "NotTracked") rightHandState = "Neutral";
            if (leftHandState == "Unknown" || leftHandState == "NotTracked") leftHandState = "Neutral";

            ProcessState rightPrev = CurrentStateRight;
            ProcessState leftPrev = CurrentStateLeft;

            bool strokeStart = false;

            //Get State of Right Hand
            if (discreteGesture != "")
                CurrentStateRight = GetState(discreteGesture);
            else
                strokeStart = DetermineStroke(GetState(rightHandState));
            //Get state of left hand
            CurrentStateLeft = GetState(leftHandState);

            //Change Sprites of Hands
            if (rightPrev != CurrentStateRight)
            {
                UI.ChangeSpriteRight(CurrentStateRight, recognizer.checkUndoTimedOut(), recognizer.checkRedoTimedOut());
                //Call undo/redo once upon gesture recognition
                if (CurrentStateRight == ProcessState.Undo) UndoGesture();
                if (CurrentStateRight == ProcessState.Redo) RedoGesture();
            }
            if (leftPrev != CurrentStateLeft) UI.ChangeSpriteLeft(CurrentStateLeft);

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
    /// Perform undo on stroke
    /// </summary>
    private void UndoGesture()
    {
        int tempIndex = strokesList.Count - 1;
        while (spheres[strokesList[tempIndex].Item1].activeSelf == false && tempIndex >= 0)
        {
            tempIndex -= 1;
        }
        if (tempIndex < 0) return;

        int diff = strokesList[tempIndex].Item2 - strokesList[tempIndex].Item1;
        for (int i = diff; i > 0; i--)
        {
            spheres[i].SetActive(false);
        }
        undoStack.Push(strokesList[(strokesList.Count - 1)]);
    }

    /// <summary>
    /// Perform redo on stroke
    /// </summary>
    private void RedoGesture()
    {
        if (undoStack.Count == 0) return;
        Tuple<int, int> temp = undoStack.Pop();
        int strokeDist = temp.Item2 - temp.Item1;
        for (int i = strokeDist; i > 0; i--)
        {
            spheres[i].SetActive(true);
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
            case "Undo":
                return Undo();
            case "Redo":
                return Redo();
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
    private ProcessState Undo() { return ProcessState.Undo; }
    private ProcessState Redo() { return ProcessState.Redo; }
}