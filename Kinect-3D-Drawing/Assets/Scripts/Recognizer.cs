using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Kinect = Windows.Kinect;

public class Recognizer : MonoBehaviour
{
    public BodySourceView bodyview;
    public double handLength; // Default hand length for development

    // HandShape depicts the shape of a user’s hand in one frame
    public class HandShape
    {
        public string side;                     // “left” if left hand, “right” if right
        public int thumbExtended;               // True (1) if the thumb joint is extended
        public int handTipOpen;                 // True (1) if the hand tip join is extended
        public string palmOrientation;          // "towards" if the palm is facing the Kinect, "away" if the palm is facing away from the Kinect, "neither" if neither
        public string extendedThumbElevation;   // "down" if thumb is extended downwards, "up" if thumb is extended upwards, "other" if other
    }

    // HandPattern depicts the shape of the user’s hand across 100 unity frames
    public class HandPattern
    {
        public Queue<HandShape> pastShapes = new Queue<HandShape>();    // The queue containing the past 100(?) frames of hand shapes
        public HandShape previouslyAddedHandShape = new HandShape();    // The previous item to be added to pastShapes, i.e. the newest item in pastShapes
        public string side;                                             // “left” or “right” for the hand

        public void add(HandShape newestShape)
        {
            if (pastShapes.Count < 100)
            {
                pastShapes.Enqueue(newestShape);
            }
            else
            {
                pastShapes.Dequeue();
                pastShapes.Enqueue(newestShape);
            }
        }
    }

    // A shorterm more current version of the HandPattern class with higher accuracy requirements (for discrete gestures)
    public class QuickHandPattern
    {
        public Queue<HandShape> pastShapes = new Queue<HandShape>();    // The queue containing the past 20(?) frames of hand shapes
        public HandShape previouslyAddedHandShape = new HandShape();    // The previous item to be added to pastShapes, i.e. the newest item in pastShapes
        public string side;                                             // “left” or “right” for the hand

        public void add(HandShape newestShape)
        {
            if (pastShapes.Count < 20)
            {
                pastShapes.Enqueue(newestShape);
            }
            else
            {
                pastShapes.Dequeue();
                pastShapes.Enqueue(newestShape);
            }
        }
    }

    // Unit gesture can be the trigger for a continuous gesture or part of a series trigger for a discrete gesture
    public class UnitGesture
    {
        public string name;                     // Name of the unit gesture
        public int thumbExtended;		        // 1 if thumb is extended, 0 if not, -1 if irrelevant
        public int handTipOpen;			        // 1 if hand tip is open, 0 if not, -1 if irrelevant
        public bool isMet;                      // Used for discrete gesture series
        public string palmOrientation;          // "towards" if the palm must face the Kinect, "away" if the palm must face away from the Kinect, "irrelevant" if irrelevant
        public string extendedThumbElevation;   // "down" if thumb must be extended downwards, "up" if thumb must be extended upwards, "irrelevant" if irrelevant

        public double matches(HandPattern inputHandPattern)
        {
            double matchingShapes = 0;

            foreach (HandShape shape in inputHandPattern.pastShapes)
            {
                // Check if the thumb position matches 
                if (thumbExtended != -1 && shape.thumbExtended != thumbExtended)
                {
                    continue;
                }

                // Check if hand tip position matches
                if (handTipOpen != -1 && shape.handTipOpen != handTipOpen)
                {
                    continue;
                }

                // Check if extended thumb is extended the correct way
                if(extendedThumbElevation != "irrelevant" && extendedThumbElevation != shape.extendedThumbElevation)
                {
                    continue;
                }

                matchingShapes++;
            }

            return matchingShapes;

        }
    }

    public class ContinuousGesture
    {
        public string gestureName;          // Name / identifier of this gesture
        public bool dominant;               // True if dominant hand
        public UnitGesture triggerGesture;  // The unit gesture that triggers this continuous gesture
        public double score;                // The score for this continuous gesture (in terms of recognition)
    }

    public class DiscreteGesture
    {
        public string gestureName;          // Name / identifier of this gesture
        public bool dominant;               // True if dominant hand
        public bool timedOut;               // True if this gesture cannot be triggered (timed out)
        public bool triggeredRecently;      // True if this gesture has been triggered in the current cycle
        public int triggeredAt;             // The cycle time at which this gesture was triggered (-1 if not timed out)
        public UnitGesture[] gestureSeries; // Array of all unit gestures that make up this discrete gesture’s series
    }

    public string userDominantHandSide;         // "left" if the user is left-handed, "right" if the user is right-handed
    public string currentDominantGesture;       // name of the active gesture for the user's dominant hand
    public string currentNonDominantGesture;    // name of the active gesture for the user's non-dominant hand
    public HandPattern leftPattern;             // HandPattern object for the user's left hand
    public HandPattern rightPattern;            // HandPattern object for the user's right hand
    public QuickHandPattern leftQuickPattern;   // QuickHandPattern object for the user's left hand
    public QuickHandPattern rightQuickPattern;  // QuickHandPattern object for the user's right hand
    public ContinuousGesture[] allCGestures;    // Array containing all ContinuousGesture objects implemented in the project
    public DiscreteGesture[] allDGestures;      // Array containing all DiscreteGesture objects implemented in the project
    public int triggeredDiscreteGesture;        // String containing index of triggered (but not yet run) discrete gesture (blank if no new trigger)
    public string readyDiscreteGesture;         // The name of the discrete gesture that has been most recently triggered but not yet called by an outside source
    public int cycle;                           // Tracks cycles for discrete gestures

    // For testing
    public UnitGesture Undo0;
    public UnitGesture Undo1;
    public UnitGesture Redo0;
    public UnitGesture Redo1;

    // Used to measure average hand distances to determine appropriate ratios
    public double lengthCount;

    void Awake()
    {
        // Initialize most global variables
        userDominantHandSide = "right";         // We're assuming the user is right-handed for now
        currentDominantGesture = "Neutral";     // Default to the Neutral gesture
        currentNonDominantGesture = "Neutral";  // Default to the Neutral gesture

        leftPattern = new HandPattern();        // Create HandPattern object
        leftPattern.side = "left";              // Indicate that this HandPattern is for the left hand
        rightPattern = new HandPattern();       // Create HandPattern object
        rightPattern.side = "right";            // Indicate that this HandPattern is for the right hand

        leftQuickPattern = new QuickHandPattern();
        leftQuickPattern.side = "left";
        rightQuickPattern = new QuickHandPattern();
        rightQuickPattern.side = "right";

        triggeredDiscreteGesture = -1;          // No currently triggered discrete gesture
        readyDiscreteGesture = "";
        cycle = -1;                             // Start with cycle -1 since the cycle count increments in the beginning

        handLength = 0.8215; // Default hand length for development
        lengthCount = 1;


        // Initialize all ContinuousGesture objects within allCGestures

        // Indeces for each gesture
        // 0 - Neutral (dominant)
        // 1 - Neutral (non-dominant)
        // 2 - Draw
        // 3 - Erase

        int numCGestures = 8;
        allCGestures = new ContinuousGesture[numCGestures];

        string gestureName;
        bool dominant;
        UnitGesture triggerGesture = new UnitGesture();

        // Neutral gesture (created for dominant and non-dominant hands)
        gestureName = "Neutral";                                        // "Neutral"
        dominant = true;                                                // For the dominant hand
        triggerGesture = new UnitGesture();                             // Set triggerGesture to point to a new instance of the UnitGesture class
        triggerGesture.name = gestureName;                              // Set triggerGesture to have the name "Neutral"
        triggerGesture.thumbExtended = 0;                               // The thumb must be closed
        triggerGesture.handTipOpen = 0;                                 // The hand tip must be closed
        triggerGesture.palmOrientation = "irrelevant";                  // Palm orientation is not relevant to this gesture
        triggerGesture.extendedThumbElevation = "irrelevant";           // Thumb is not extended, so irrelevant
        ContinuousGesture NeutralDominant = new ContinuousGesture();    // Create new ContinuousGesture object
        NeutralDominant.gestureName = gestureName;                      // Assign "Neutral" to ContinuousGesture name
        NeutralDominant.dominant = dominant;                            // Indicate that this is for the dominant hand
        NeutralDominant.triggerGesture = triggerGesture;                // Assign triggerGesture to NeutralDominant 
        allCGestures[0] = NeutralDominant;                              // Add NeutralDominant to allCGestures
        dominant = false;                                               // Now add the non-dominant variant of this gesture
        ContinuousGesture NeutralNonDominant = new ContinuousGesture(); // Create new ContinuousGesture object
        NeutralNonDominant.gestureName = gestureName;                   // This will have the same name "Neutral"
        NeutralNonDominant.dominant = dominant;                         // Set its dominant value to false
        NeutralNonDominant.triggerGesture = triggerGesture;             // This will have the same triggerGesture
        allCGestures[1] = NeutralNonDominant;                           // Add NeutralNonDominant to allCGestures

        // Draw gesture (dominant)
        gestureName = "Draw";                                           // "Draw"
        dominant = true;                                                // For the dominant hand
        triggerGesture = new UnitGesture();                             // Set triggerGesture to point to a new instance of the UnitGesture class
        triggerGesture.name = gestureName;                              // Set triggerGesture to have the name "Draw"
        triggerGesture.thumbExtended = 0;                               // The thumb must be closed
        triggerGesture.handTipOpen = 1;                                 // The hand tip must be open
        triggerGesture.palmOrientation = "irrelevant";                  // Palm orientation is not relevant to this gesture
        triggerGesture.extendedThumbElevation = "irrelevant";           // Thumb is not extended, so irrelevant
        ContinuousGesture Draw = new ContinuousGesture();               // Create new ContinuousGesture object
        Draw.gestureName = gestureName;                                 // Assign "Draw" to ContinuousGesture name
        Draw.dominant = dominant;                                       // Indicate that this is for the dominant hand
        Draw.triggerGesture = triggerGesture;                           // Assign triggerGesture to Draw 
        allCGestures[2] = Draw;                                         // Add Draw to allCGestures

        // Erase gesture (dominant)
        gestureName = "Erase";                                          // "Erase"
        dominant = true;                                                // For the dominant hand
        triggerGesture = new UnitGesture();                             // Set triggerGesture to point to a new instance of the UnitGesture class
        triggerGesture.name = gestureName;                              // Set triggerGesture to have the name "Erase"
        triggerGesture.thumbExtended = 1;                               // The thumb must be extended
        triggerGesture.handTipOpen = 1;                                 // The hand tip must be open
        triggerGesture.palmOrientation = "irrelevant";                  // Palm orientation is not relevant to this gesture
        triggerGesture.extendedThumbElevation = "irrelevant";           // Thumb position is not relevant
        ContinuousGesture Erase = new ContinuousGesture();              // Create new ContinuousGesture object
        Erase.gestureName = gestureName;                                // Assign "Erase" to ContinuousGesture name
        Erase.dominant = dominant;                                      // Indicate that this is for the dominant hand
        Erase.triggerGesture = triggerGesture;                          // Assign triggerGesture to Erase 
        allCGestures[3] = Erase;                                        // Add Erase to allCGestures

        // RotateClockwise (non-dominant)
        gestureName = "RotateClockwise";
        dominant = false;
        triggerGesture = new UnitGesture();
        triggerGesture.thumbExtended = 1;
        triggerGesture.handTipOpen = 0;
        triggerGesture.palmOrientation = "irrelevant";
        triggerGesture.extendedThumbElevation = "left";
        ContinuousGesture RotateClockwise = new ContinuousGesture();
        RotateClockwise.gestureName = gestureName;
        RotateClockwise.dominant = dominant;
        RotateClockwise.triggerGesture = triggerGesture;
        allCGestures[4] = RotateClockwise;

        // RotateCounterClockwise (non-dominant)
        gestureName = "RotateCounterClockwise";
        dominant = false;
        triggerGesture = new UnitGesture();
        triggerGesture.thumbExtended = 1;
        triggerGesture.handTipOpen = 0;
        triggerGesture.palmOrientation = "irrelevant";
        triggerGesture.extendedThumbElevation = "right";
        ContinuousGesture RotateCounterClockwise = new ContinuousGesture();
        RotateCounterClockwise.gestureName = gestureName;
        RotateCounterClockwise.dominant = dominant;
        RotateCounterClockwise.triggerGesture = triggerGesture;
        allCGestures[5] = RotateCounterClockwise;

        // ZoomIn (non-dominant)
        gestureName = "ZoomIn";
        dominant = false;
        triggerGesture = new UnitGesture();
        triggerGesture.thumbExtended = 1;
        triggerGesture.handTipOpen = 0;
        triggerGesture.palmOrientation = "irrelevant";
        triggerGesture.extendedThumbElevation = "up";
        ContinuousGesture ZoomIn = new ContinuousGesture();
        ZoomIn.gestureName = gestureName;
        ZoomIn.dominant = dominant;
        ZoomIn.triggerGesture = triggerGesture;
        allCGestures[6] = ZoomIn;

        // ZoomOut (non-dominant)
        gestureName = "ZoomOut";
        dominant = false;
        triggerGesture = new UnitGesture();
        triggerGesture.thumbExtended = 1;
        triggerGesture.handTipOpen = 0;
        triggerGesture.palmOrientation = "irrelevant";
        triggerGesture.extendedThumbElevation = "down";
        ContinuousGesture ZoomOut = new ContinuousGesture();
        ZoomOut.gestureName = gestureName;
        ZoomOut.dominant = dominant;
        ZoomOut.triggerGesture = triggerGesture;
        allCGestures[7] = ZoomOut;

        // Initialize all DiscreteGesture objects within allDGestures

        // Indeces for each gesture
        // 0 - Undo
        // 1 - Redo

        int numDGestures = 2;
        allDGestures = new DiscreteGesture[numDGestures];

        UnitGesture stepGesture = new UnitGesture();

        // Undo gesture (dominant)
        DiscreteGesture Undo = new DiscreteGesture();                   // Create DiscreteGesture object for Undo
        Undo.gestureName = "Undo";                                      // Assign "Undo" as name for discrete gesture
        Undo.dominant = true;                                           // Indicate that this discrete gesture is for the dominant hand
        Undo.timedOut = false;                                          // Not timed out at the start
        Undo.triggeredAt = -1;                                          // Not timed out
        Undo.triggeredRecently = false;                                 // Not timed out
        Undo.gestureSeries = new UnitGesture[2];                        // Indicate that this discrete gesture has 3 steps
        stepGesture = new UnitGesture();                                // Create first UnitGesture in this discrete gesture's series
        
        stepGesture.name = "Undo0";                                     // Indicate that this is step 0 for "Undo"
        stepGesture.thumbExtended = 1;                                  // For step 0, the thumb must be extended
        stepGesture.handTipOpen = 0;                                    // For step 0, the hand tip must be closed
        stepGesture.palmOrientation = "irrelevant";
        if (userDominantHandSide == "right")
        {
            stepGesture.extendedThumbElevation = "left";
        }
        else
        {
            stepGesture.extendedThumbElevation = "right";
        }
        Undo.gestureSeries[0] = stepGesture;                            // Assign step 0
        Undo0 = stepGesture;
        stepGesture = new UnitGesture();                                // Create a new reference for stepGesture

        stepGesture.name = "Undo1";                                     // Indicate that this is step 1 for "Undo"
        stepGesture.thumbExtended = 1;                                  // For step 1, the thumb must be extended
        stepGesture.handTipOpen = 0;                                    // For step 1, the hand tip must be closed
        stepGesture.palmOrientation = "irrelevant";                     // 
        stepGesture.extendedThumbElevation = "down";                    // Thumb must be extended downwards
        Undo.gestureSeries[1] = stepGesture;                            // Assign step 1
        Undo1 = stepGesture;
        stepGesture = new UnitGesture();                                // Create a new reference for stepGesture

        stepGesture.name = "Undo2";                                     // Indicate that this is step 0 for "Undo"
        stepGesture.thumbExtended = 1;                                  // For step 2, the thumb must be extended
        stepGesture.handTipOpen = 0;                                    // For step 2, the hand tip must be closed
        stepGesture.palmOrientation = "irrelevant";
        if (userDominantHandSide == "right")
        {
            stepGesture.extendedThumbElevation = "left";
        }
        else
        {
            stepGesture.extendedThumbElevation = "right";
        }
        //Undo.gestureSeries[2] = stepGesture;                            // Assign step 2
        stepGesture = new UnitGesture();                                // Create a new reference for stepGesture

        allDGestures[0] = Undo;                                         // Assign Undo to index 0 in allDGestures

        // Redo gesture (dominant)
        DiscreteGesture Redo = new DiscreteGesture();                   // Create DiscreteGesture object for Redo
        Redo.gestureName = "Redo";                                      // Assign "Redo" as name for discrete gesture
        Redo.dominant = true;                                           // Indicate that this discrete gesture is for the dominant hand
        Redo.timedOut = false;                                          // Not timed out at the start
        Redo.triggeredAt = -1;                                          // Not timed out
        Redo.triggeredRecently = false;                                 // Not timed out
        Redo.gestureSeries = new UnitGesture[2];                        // Indicate that this discrete gesture has 3 steps
        stepGesture = new UnitGesture();                                // Create first UnitGesture in this discrete gesture's series

        stepGesture.name = "Redo0";                                     // Indicate that this is step 0 for "Redo"
        stepGesture.thumbExtended = 1;                                  // For step 0, the thumb must be extended
        stepGesture.handTipOpen = 0;                                    // For step 0, the hand tip must be closed
        stepGesture.palmOrientation = "irrelevant";
        if (userDominantHandSide == "right")
        {
            stepGesture.extendedThumbElevation = "left";
        }
        else
        {
            stepGesture.extendedThumbElevation = "right";
        }
        Redo.gestureSeries[0] = stepGesture;                            // Assign step 0
        Redo0 = stepGesture;
        stepGesture = new UnitGesture();                                // Create a new reference for stepGesture

        stepGesture.name = "Redo1";                                     // Indicate that this is step 1 for "Redo"
        stepGesture.thumbExtended = 1;                                  // For step 1, the thumb must be extended
        stepGesture.handTipOpen = 0;                                    // For step 1, the hand tip must be closed
        stepGesture.palmOrientation = "irrelevant";                     // 
        stepGesture.extendedThumbElevation = "up";                      // Thumb must be extended upwards
        Redo.gestureSeries[1] = stepGesture;                            // Assign step 1
        Redo1 = stepGesture;
        stepGesture = new UnitGesture();                                // Create a new reference for stepGesture

        stepGesture.name = "Redo2";                                     // Indicate that this is step 0 for "Redo"
        stepGesture.thumbExtended = 1;                                  // For step 2, the thumb must be extended
        stepGesture.handTipOpen = 0;                                    // For step 2, the hand tip must be closed
        stepGesture.palmOrientation = "irrelevant";
        if (userDominantHandSide == "right")
        {
            stepGesture.extendedThumbElevation = "left";
        }
        else
        {
            stepGesture.extendedThumbElevation = "right";
        }
        //Redo.gestureSeries[2] = stepGesture;                          // Assign step 2
        stepGesture = new UnitGesture();                                // Create a new reference for stepGesture

        allDGestures[1] = Redo;                                         // Assign Redo to index 0 in allDGestures

    }

    void Start()
    {

    }

    // Returns true if the hand's thumb is extended (determined by "side")
    public bool checkThumbExtended(Kinect.Body b, string side)
    {
        // Experimentally, the ratio to determine the threshold was found
        // to be best at about 0.7304 times the hand length
        double threshold = 0.7304 * handLength;

        Kinect.Joint handJoint;
        Kinect.Joint handTipJoint;
        Kinect.Joint thumbJoint;

        Vector3 handJointVector;
        Vector3 handTipJointVector;
        Vector3 thumbJointVector;

        if (side == "right")
        {
            handJoint = b.Joints[Kinect.JointType.HandRight];
            handJointVector = bodyview.GetVector3FromJoint(handJoint);
            handTipJoint = b.Joints[Kinect.JointType.HandTipRight];
            handTipJointVector = bodyview.GetVector3FromJoint(handTipJoint);
            thumbJoint = b.Joints[Kinect.JointType.ThumbRight];
            thumbJointVector = bodyview.GetVector3FromJoint(thumbJoint);
        }
        else
        {
            handJoint = b.Joints[Kinect.JointType.HandLeft];
            handJointVector = bodyview.GetVector3FromJoint(handJoint);
            handTipJoint = b.Joints[Kinect.JointType.HandTipLeft];
            handTipJointVector = bodyview.GetVector3FromJoint(handTipJoint);
            thumbJoint = b.Joints[Kinect.JointType.ThumbLeft];
            thumbJointVector = bodyview.GetVector3FromJoint(thumbJoint);
        }

        // Find the point one third of the distance from the hand joint to the hand tip joint
        Vector3 handCenterVector = Vector3.MoveTowards(handJointVector, handTipJointVector, (float)(handLength / 2.3));

        if (Vector3.Distance(handCenterVector, thumbJointVector) > threshold)
            return true;
        else
            return false;
    }

    // Returns true if the hand's tip is open is extended (determined by "side")
    public bool checkHandTipOpen(Kinect.Body b, string side)
    {
        // Experimentally, the ratio to determine the threshold was found
        // to be best at about 0.7304 times the hand length
        double threshold = 0.7304 * handLength;

        Kinect.Joint handJoint;
        Kinect.Joint handTipJoint;

        Vector3 handJointVector;
        Vector3 handTipJointVector;

        if (side == "right")
        {
            handJoint = b.Joints[Kinect.JointType.HandRight];
            handJointVector = bodyview.GetVector3FromJoint(handJoint);
            handTipJoint = b.Joints[Kinect.JointType.HandTipRight];
            handTipJointVector = bodyview.GetVector3FromJoint(handTipJoint);
        }
        else
        {
            handJoint = b.Joints[Kinect.JointType.HandLeft];
            handJointVector = bodyview.GetVector3FromJoint(handJoint);
            handTipJoint = b.Joints[Kinect.JointType.HandTipLeft];
            handTipJointVector = bodyview.GetVector3FromJoint(handTipJoint);
        }

        if (Vector3.Distance(handJointVector, handTipJointVector) > threshold)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    // Gets the vector for the normal coming out of the palm
    public Vector3 getPalmNormal(Kinect.Body b, string side)
    {
        Kinect.Joint handJoint;
        Kinect.Joint handTipJoint;
        Kinect.Joint thumbJoint;

        if (side == "right")
        {
            handJoint = b.Joints[Kinect.JointType.HandRight];
            handTipJoint = b.Joints[Kinect.JointType.HandTipRight];
            thumbJoint = b.Joints[Kinect.JointType.ThumbRight];
        }
        else
        {
            handJoint = b.Joints[Kinect.JointType.HandLeft];
            handTipJoint = b.Joints[Kinect.JointType.HandTipLeft];
            thumbJoint = b.Joints[Kinect.JointType.ThumbLeft];
        }

        // Get positions of each relevant joint
        Vector3 handJointVector = bodyview.GetVector3FromJoint(handJoint);
        Vector3 handTipJointVector = bodyview.GetVector3FromJoint(handTipJoint);
        Vector3 thumbJointVector = bodyview.GetVector3FromJoint(thumbJoint);

        // Find two vectors in the palm plane
        Vector3 handToThumbVector = thumbJointVector - handJointVector;
        Vector3 handToTipVector = handTipJointVector - handJointVector;

        // Find the normal vector to the palm (cross product)
        Vector3 palmNormalVector;
        if (side == "right")
        {
            palmNormalVector = Vector3.Cross(handToThumbVector, handToTipVector);
        }
        else
        {
            palmNormalVector = Vector3.Cross(handToTipVector, handToThumbVector);
        }

        return palmNormalVector;
    }

    // Returns string for the palm orientation
    public string getPalmOrientation(Kinect.Body b, string side)
    {
        string orientation = "neither";

        if (Vector3.Angle(getPalmNormal(b, side), Vector3.back) < 60.0)
        {
            orientation = "towards";
        }
        if (Vector3.Angle(getPalmNormal(b, side), Vector3.back) > 120.0)
        {
            orientation = "away";
        }

        return orientation;
    }

    // Returns the direction (relative to the Kinect) to which the hand is pointing
    public string getHandDirection(Kinect.Body b, string side)
    {
        Kinect.Joint handJoint;
        Kinect.Joint handTipJoint;

        if (side == "right")
        {
            handJoint = b.Joints[Kinect.JointType.HandRight];
            handTipJoint = b.Joints[Kinect.JointType.HandTipRight];
        }
        else
        {
            handJoint = b.Joints[Kinect.JointType.HandLeft];
            handTipJoint = b.Joints[Kinect.JointType.HandTipLeft];
        }

        // Get positions of each relevant joint
        Vector3 handJointVector = bodyview.GetVector3FromJoint(handJoint);
        Vector3 handTipJointVector = bodyview.GetVector3FromJoint(handTipJoint);

        // Get vector for hand direction
        Vector3 handToTipVector = handTipJointVector - handJointVector;

        string handDirection = "other";

        // Find string for hand direction RELATIVE TO THE KINECT
        if(Vector3.Angle(handToTipVector, Vector3.left) < 60.0)
        {
            handDirection = "left";
        }
        if(Vector3.Angle(handToTipVector, Vector3.right) < 60.0)
        {
            handDirection = "right";
        }

        return handDirection;
    }

    // Returns the direction in which the thumb is extended
    public string getExtendedThumbElevation(Kinect.Body b, string side)
    {
        Kinect.Joint handJoint;
        Kinect.Joint thumbJoint;

        if (side == "right")
        {
            handJoint = b.Joints[Kinect.JointType.HandRight];
            thumbJoint = b.Joints[Kinect.JointType.ThumbRight];
        }
        else
        {
            handJoint = b.Joints[Kinect.JointType.HandLeft];
            thumbJoint = b.Joints[Kinect.JointType.ThumbLeft];
        }

        // Get positions of each relevant joint
        Vector3 handJointVector = bodyview.GetVector3FromJoint(handJoint);
        Vector3 thumbJointVector = bodyview.GetVector3FromJoint(thumbJoint);

        // Find two vectors in the palm plane
        Vector3 handToThumbVector = thumbJointVector - handJointVector;

        string extendedThumbElevation = "other";

        // Find string for hand direction RELATIVE TO THE KINECT
        if (Vector3.Angle(handToThumbVector, Vector3.up) < 60.0)
        {
            extendedThumbElevation = "up";
        }
        if (Vector3.Angle(handToThumbVector, Vector3.down) < 60.0)
        {
            extendedThumbElevation = "down";
        }
        if (Vector3.Angle(handToThumbVector, Vector3.left) < 60.0)
        {
            extendedThumbElevation = "left";
        }
        if (Vector3.Angle(handToThumbVector, Vector3.right) < 60.0)
        {
            extendedThumbElevation = "right";
        }

        return extendedThumbElevation;
    }

    public string getLeftHandGesture()
    {
        if(userDominantHandSide == "right")
        {
            return currentNonDominantGesture;
        }
        else
        {
            return currentDominantGesture;
        }
    }

    public string getRightHandGesture()
    {
        if(userDominantHandSide == "right")
        {
            return currentDominantGesture;
        }
        else
        {
            return currentNonDominantGesture;
        }
    }

    //Set discrete gesture
    public void setReadyDiscreteGesture(string input_string)
    {
        readyDiscreteGesture = input_string;
    }

    //Get discrete gesture
    public string checkForDiscreteGesture()
    {
        string output_string = readyDiscreteGesture;
        readyDiscreteGesture = "";                      // Reset upon outside source looking in
        return output_string;
    }

    //Check the current frame with Kinect body
    public void FrameCheck(Kinect.Body b)
    {
        string side;
        bool thumbExtended;
        bool handTipOpen;
        double[] palmPitchRollYaw = new double[3];

        // Add newest values to leftPattern
        side = "left";
        thumbExtended = checkThumbExtended(b, side);
        handTipOpen = checkHandTipOpen(b, side);
        HandShape leftShape = new HandShape();

        leftShape.side = side;

        // Convert local thumbExtended boolean to HandShape int
        if (thumbExtended)
        {
            leftShape.thumbExtended = 1;
        }
        else
        {
            leftShape.thumbExtended = 0;
        }

        // Convert local handTipOpen boolean to HandShape int
        if (handTipOpen)
        {
            leftShape.handTipOpen = 1;
        }
        else
        {
            leftShape.handTipOpen = 0;
        }

        leftShape.palmOrientation = getPalmOrientation(b, side);

        if(leftShape.thumbExtended == 1)
        {
            leftShape.extendedThumbElevation = getExtendedThumbElevation(b, side);
        }
        else
        {
            leftShape.extendedThumbElevation = "closed";
        }

        // Add newest values to rightPattern
        side = "right";
        thumbExtended = checkThumbExtended(b, side);
        handTipOpen = checkHandTipOpen(b, side);
        HandShape rightShape = new HandShape();

        rightShape.side = side;

        // Convert local thumbExtended boolean to HandShape int
        if (thumbExtended)
        {
            rightShape.thumbExtended = 1;
        }
        else
        {
            rightShape.thumbExtended = 0;
        }

        // Convert local handTipOpen boolean to HandShape int
        if (handTipOpen)
        {
            rightShape.handTipOpen = 1;
        }
        else
        {
            rightShape.handTipOpen = 0;
        }

        rightShape.palmOrientation = getPalmOrientation(b, side);

        if (rightShape.thumbExtended == 1)
        {
            rightShape.extendedThumbElevation = getExtendedThumbElevation(b, side);
        }
        else
        {
            rightShape.extendedThumbElevation = "closed";
        }

        // Add the new HandShape objects to the HandPattern objects
        leftPattern.add(leftShape);
        rightPattern.add(rightShape);

        leftQuickPattern.add(leftShape);
        rightQuickPattern.add(rightShape);
    }

    // Called once per frame
    public void Recognize(Kinect.Body b)
    {
        // Update global HandPattern objects
        FrameCheck(b);

        // Increment cycle and discrete gestures
        cycle++;
        triggeredDiscreteGesture = -1;

        if(cycle >= 1000)
        {
            cycle = 0;
            for(int i = 0; i < allDGestures.Length; i++)
            {
                if(allDGestures[i].triggeredRecently)
                {
                    allDGestures[i].triggeredRecently = false;
                }
            }
        }

        for (int i = 0; i < allDGestures.Length; i++)
        {
            if (!allDGestures[i].triggeredRecently && allDGestures[i].timedOut && allDGestures[i].triggeredAt >= cycle)
            {
                allDGestures[i].timedOut = false;
                allDGestures[i].triggeredAt = -1;
            }
        }

        Kinect.Joint handJoint = b.Joints[Kinect.JointType.HandRight];
        Vector3 handJointVector = bodyview.GetVector3FromJoint(handJoint);
        Kinect.Joint handTipJoint = b.Joints[Kinect.JointType.HandTipRight];
        Vector3 handTipJointVector = bodyview.GetVector3FromJoint(handTipJoint);

        double newLength = Vector3.Distance(handJointVector, handTipJointVector);

        // Adjust newLength depending on Kinect-detected hand state
        if(b.HandRightState.ToString() == "Closed")
        {
            newLength = newLength / 0.6814; // Experimentally-determined value
        }

        // Update hand length estimate
        double undividedSum = handLength * lengthCount;  // Multiply handLength by lengthCount to find the value of the previous sum before division
        undividedSum = undividedSum + newLength;         // Add new distance to undivided sum
        handLength = undividedSum / (lengthCount + 1);   // Divide by new count to get average
        
        // There's a cap to how large lengthCount can get,
        // preventing subsequent hand size estimates from having too little weight
        if(lengthCount < 9999)
        {
            lengthCount++;
        }

        // Create new HandPattern objects for each hand
        HandPattern dominantHandPattern = new HandPattern();
        HandPattern nonDominantHandPattern = new HandPattern();

        // Assign existing hand patterns to each newly created pattern
        // based on what the user chose as their dominant hand
        if (userDominantHandSide == "right")
        {
            dominantHandPattern = rightPattern;
            nonDominantHandPattern = leftPattern;
        }
        else
        {
            dominantHandPattern = leftPattern;
            nonDominantHandPattern = rightPattern;
        }

        QuickHandPattern dominantQuickHandPattern = new QuickHandPattern();
        QuickHandPattern nonDominantQuickHandPattern = new QuickHandPattern();

        if (userDominantHandSide == "right")
        {
            dominantQuickHandPattern = rightQuickPattern;
            nonDominantQuickHandPattern = leftQuickPattern;
        }
        else
        {
            dominantQuickHandPattern = leftQuickPattern;
            nonDominantQuickHandPattern = rightQuickPattern;
        }

        int dgIndex = 0;

        // Scan for each possible discrete gesture
        foreach (DiscreteGesture dg in allDGestures)
        {
            // Determine which handPattern to check for this gesture
            HandPattern checkPattern = new HandPattern();
            if(dg.dominant)
            {
                checkPattern = dominantHandPattern;
            }
            else
            {
                checkPattern = nonDominantHandPattern;
            }

            // Find the index of the first non-met step in the series
            int index = 0;
            for(int i = 0; i < dg.gestureSeries.Length; i++)
            {
                if(!dg.gestureSeries[i].isMet)
                {
                    index = i;
                    break;
                }
            }

            // If the index is 0, simply check for a match
            if(index == 0)
            {
                if (dg.gestureSeries[index].matches(checkPattern) > 15)
                {
                    dg.gestureSeries[index].isMet = true;
                }
            }

            // If the index is greater than 0, check for a match
            // If no match, check if the pattern still matches the previous step
            // If neither, reset each step (break the chain)
            if(index > 0)
            {
                if (dg.gestureSeries[index].matches(checkPattern) > 15)
                {
                    dg.gestureSeries[index].isMet = true;
                }
                else if(dg.gestureSeries[index-1].matches(checkPattern) <= 15)
                {
                    for (int i = 0; i < dg.gestureSeries.Length; i++)
                    {
                        dg.gestureSeries[i].isMet = false;
                    }
                }
            }

            // If all steps are met, set triggeredDiscreteGesture to the name of dg
            bool allStepsMet = true;
            for(int i = 0; i < dg.gestureSeries.Length; i++)
            {
                if(!dg.gestureSeries[i].isMet)
                {
                    allStepsMet = false;
                    break;
                }
            }
            if(allStepsMet)
            {
                triggeredDiscreteGesture = dgIndex;
                for(int i = 0; i < dg.gestureSeries.Length; i++)
                {
                    dg.gestureSeries[i].isMet = false;
                }
            }

            dgIndex++;
        }

        for (int i = 0; i < allCGestures.Length; i++)
        {
            string matchNameDominant = "Neutral";
            string matchNameNonDominant = "Neutral";

            if (allCGestures[i].dominant)
            {
                allCGestures[i].score = allCGestures[i].triggerGesture.matches(dominantHandPattern);
            }
            if (!allCGestures[i].dominant)
            {
                allCGestures[i].score = allCGestures[i].triggerGesture.matches(nonDominantHandPattern);
            }
            currentDominantGesture = matchNameDominant;
            currentNonDominantGesture = matchNameNonDominant;
        }

        string bestGestureName = "Neutral";
        double bestGestureScore = 0.0;
        bool foundGestureGreaterThan40 = false;

        // Find the current dominant gesture
        foreach (ContinuousGesture cg in allCGestures)
        {
            if (!cg.dominant)
            {
                continue;
            }
            if(cg.score > 40)
            {
                foundGestureGreaterThan40 = true;
            }
            if (cg.score > bestGestureScore && cg.score > 40)
            {
                bestGestureScore = cg.score;
                bestGestureName = cg.gestureName;
            }
        }
        if(foundGestureGreaterThan40)
        {
            currentDominantGesture = bestGestureName;
        }
        else
        {
            currentDominantGesture = "Neutral";
        }

        bestGestureName = "Neutral";
        bestGestureScore = 0.0;
        foundGestureGreaterThan40 = false;

        // Find the current non-dominant gesture
        foreach (ContinuousGesture cg in allCGestures)
        {
            if (cg.dominant)
            {
                continue;
            }
            if (cg.score > 40)
            {
                foundGestureGreaterThan40 = true;
            }
            if (cg.score > bestGestureScore && cg.score > 40)
            {
                bestGestureScore = cg.score;
                bestGestureName = cg.gestureName;
            }
        }
        if (foundGestureGreaterThan40)
        {
            currentNonDominantGesture = bestGestureName;
        }
        else
        {
            currentNonDominantGesture = "Neutral";
        }


        if(triggeredDiscreteGesture > -1)
        {
            if (!allDGestures[triggeredDiscreteGesture].timedOut)
            {
                setReadyDiscreteGesture(allDGestures[triggeredDiscreteGesture].gestureName);
                allDGestures[triggeredDiscreteGesture].timedOut = true;
                allDGestures[triggeredDiscreteGesture].triggeredAt = cycle;
                allDGestures[triggeredDiscreteGesture].triggeredRecently = true;
            }
        }
    }
}