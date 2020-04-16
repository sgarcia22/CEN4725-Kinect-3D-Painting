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
        public string side;             // “left” if left hand, “right” if right
        public int thumbExtended;       // True (1) if the thumb joint is extended
        public int handTipOpen;         // True (1) if the hand tip join is extended
        public string palmOrientation;  // "towards" if the palm is facing the Kinect, "away" if the palm is facing away from the Kinect, "neither" if neither
    }

    // HandPattern depicts the shape of the user’s hand across 100 unity frames
    public class HandPattern
    {
        public Queue<HandShape> pastShapes = new Queue<HandShape>();    // The queue containing the past 30 frames of hand shapes
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

    // Unit gesture can be the trigger for a continuous gesture or part of a series trigger for a discrete gesture
    public class UnitGesture
    {
        public string name;             // Name of the unit gesture
        public int thumbExtended;		// 1 if thumb is extended, 0 if not, -1 if irrelevant
        public int handTipOpen;			// 1 if hand tip is open, 0 if not, -1 if irrelevant
        bool isMet;                     // Used for discrete gesture series
        public string palmOrientation;  // "towards" if the palm must face the Kinect, "away" if the palm must face away from the Kinect, "irrelevant" if irrelevant

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

                // Check if palm is facing the correct way
                if(palmOrientation != "irrelevant" && palmOrientation != shape.palmOrientation)
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
        public UnitGesture[] gestureSeries; // Array of all unit gestures that make up this discrete gesture’s series
    }

    public string userDominantHandSide;         // "left" if the user is left-handed, "right" if the user is right-handed
    public string currentDominantGesture;       // name of the active gesture for the user's dominant hand
    public string currentNonDominantGesture;    // name of the active gesture for the user's non-dominant hand
    public HandPattern leftPattern;             // HandPattern object for the user's left hand
    public HandPattern rightPattern;            // HandPattern object for the user's right hand
    public ContinuousGesture[] allCGestures;    // Array containing all ContinuousGesture objects implemented in the project
    public DiscreteGesture[] allDGestures;      // Array containing all DiscreteGesture objects implemented in the project

    // Used to measure average hand distances to determine appropriate ratios
    public double lengthCount;

    void Awake()
    {
        // Initialize most global variables
        userDominantHandSide = "right";          // We're assuming the user is right-handed for now
        currentDominantGesture = "Neutral";     // Default to the Neutral gesture
        currentNonDominantGesture = "Neutral";  // Default to the Neutral gesture
        leftPattern = new HandPattern();        // Create HandPattern object
        leftPattern.side = "left";              // Indicate that this HandPattern is for the left hand
        rightPattern = new HandPattern();       // Create HandPattern object
        rightPattern.side = "right";            // Indicate that this HandPattern is for the right hand

        handLength = 0.8215; // Default hand length for development
        lengthCount = 1;


        // Initialize all ContinuousGesture objects within allCGestures

        // Indeces for each gesture
        // 0 - Neutral (dominant)
        // 1 - Neutral (non-dominant)
        // 2 - Draw
        // 3 - Erase

        int numCGestures = 4;
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
        ContinuousGesture Erase = new ContinuousGesture();              // Create new ContinuousGesture object
        Erase.gestureName = gestureName;                                // Assign "Erase" to ContinuousGesture name
        Erase.dominant = dominant;                                      // Indicate that this is for the dominant hand
        Erase.triggerGesture = triggerGesture;                          // Assign triggerGesture to Erase 
        allCGestures[3] = Erase;                                        // Add Erase to allCGestures





        // Initialize all DiscreteGesture objects within allCGestures

        // Indeces for each gesture
        // 0 - Undo
        // 1 - Redo

        int numDGestures = 2;
        allDGestures = new DiscreteGesture[numDGestures];

        string stepGestureName = "";
        UnitGesture stepGesture = new UnitGesture();

        // Undo gesture (dominant hand)
        DiscreteGesture Undo = new DiscreteGesture();                   // Create DiscreteGesture object for Undo
        Undo.gestureName = "Undo";                                      // Assign "Undo" as name for discrete gesture
        Undo.dominant = true;                                           // Indicate that this discrete gesture is for the dominant hand
        Undo.gestureSeries = new UnitGesture[3];                        // Indicate that this discrete gesture has 3 steps
        stepGesture = new UnitGesture();                                // Create first UnitGesture in this discrete gesture's series
        stepGesture.name = "Undo0";                                     // Indicate that this is step 0 for "Undo"
        stepGesture.thumbExtended = 0;                                  // For step 0, the thumb must be closed
        stepGesture.handTipOpen = 0;                                    // For step 0, the hand tip must be closed
        stepGesture.palmOrientation = "towards";                        // For step 0, the palm must be facing towards the Kinect
        Undo.gestureSeries[0] = stepGesture;                            // Assign step 0
        stepGesture = new UnitGesture();                                // Create a new reference for stepGesture
        stepGesture.name = "Undo1";                                     // Indicate that this is step 1 for "Undo"
        stepGesture.thumbExtended = 1;                                  // For step 1, the thumb must be extended
        stepGesture.handTipOpen = 0;                                    // For step 1, the hand tip must be closed
        stepGesture.palmOrientation = "towards";                        // For step 1, the palm must be facing towards the Kinect
        Undo.gestureSeries[1] = stepGesture;                            // Assign step 1
        stepGesture = new UnitGesture();                                // Create a new reference for stepGesture
        stepGesture.name = "Undo2";                                     // Indicate that this is step 2 for "Undo"
        stepGesture.thumbExtended = 0;                                  // For step 2, the thumb must be closed
        stepGesture.handTipOpen = 0;                                    // For step 2, the hand tip must be closed
        stepGesture.palmOrientation = "towards";                        // For step 2, the palm must be facing towards the Kinect
        Undo.gestureSeries[2] = stepGesture;                            // Assign step 2
        stepGesture = new UnitGesture();                                // Create a new reference for stepGesture
        allDGestures[0] = Undo;                                         // Assign Undo to index 0 in allDGestures

        // Redo gesture (dominant hand)
        DiscreteGesture Redo = new DiscreteGesture();                   // Create DiscreteGesture object for Redo
        Redo.gestureName = "Redo";                                      // Assign "Redo" as name for discrete gesture
        Redo.dominant = true;                                           // Indicate that this discrete gesture is for the dominant hand
        Redo.gestureSeries = new UnitGesture[3];                        // Indicate that this discrete gesture has 3 steps
        stepGesture = new UnitGesture();                                // Create first UnitGesture in this discrete gesture's series
        stepGesture.name = "Redo0";                                     // Indicate that this is step 0 for "Redo"
        stepGesture.thumbExtended = 0;                                  // For step 0, the thumb must be closed
        stepGesture.handTipOpen = 0;                                    // For step 0, the hand tip must be closed
        stepGesture.palmOrientation = "away";                           // For step 0, the palm must be facing towards the Kinect
        Redo.gestureSeries[0] = stepGesture;                            // Assign step 0
        stepGesture = new UnitGesture();                                // Create a new reference for stepGesture
        stepGesture.name = "Redo1";                                     // Indicate that this is step 1 for "Redo"
        stepGesture.thumbExtended = 1;                                  // For step 1, the thumb must be extended
        stepGesture.handTipOpen = 0;                                    // For step 1, the hand tip must be closed
        stepGesture.palmOrientation = "away";                           // For step 1, the palm must be facing towards the Kinect
        Redo.gestureSeries[1] = stepGesture;                            // Assign step 1
        stepGesture = new UnitGesture();                                // Create a new reference for stepGesture
        stepGesture.name = "Redo2";                                     // Indicate that this is step 2 for "Redo"
        stepGesture.thumbExtended = 0;                                  // For step 2, the thumb must be closed
        stepGesture.handTipOpen = 0;                                    // For step 2, the hand tip must be closed
        stepGesture.palmOrientation = "away";                           // For step 2, the palm must be facing towards the Kinect
        Redo.gestureSeries[2] = stepGesture;                            // Assign step 2
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
        {
            if(side == "right")
            {
                //Debug.Log("True");
            }
            return true;
        }
        else
        {
            if (side == "right")
            {
                //Debug.Log("False");
            }
            return false;
        }
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

    // Gets the angles for the hand joint
    public Vector3 getPalmNormal(Kinect.Body b, string side)
    {
        Kinect.Joint handJoint;
        Kinect.Joint handTipJoint;
        Kinect.Joint thumbJoint;

        if(side == "right")
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
        if(side == "right")
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
        
        if(Vector3.Angle(getPalmNormal(b, side), Vector3.back) < 60.0)
        {
            orientation = "towards";
        }
        if(Vector3.Angle(getPalmNormal(b, side), Vector3.back) > 120.0)
        {
            orientation = "away";
        }

        return orientation;
    }

    public void FrameCheck(Kinect.Body b)
    {
        string side;
        bool thumbExtended;
        bool handTipOpen;
        double[] palmPitchRollYaw = new double[3];

        // Add newest values to leftPattern
        side = "left";
        thumbExtended = checkThumbExtended(b, "left");
        handTipOpen = checkHandTipOpen(b, "left");
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

        leftShape.palmOrientation = getPalmOrientation(b, "left");

        // Add newest values to rightPattern
        side = "right";
        thumbExtended = checkThumbExtended(b, "right");
        handTipOpen = checkHandTipOpen(b, "right");
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

        leftShape.palmOrientation = getPalmOrientation(b, "right");

        // Add the new HandShape objects to the HandPattern objects
        leftPattern.add(leftShape);
        rightPattern.add(rightShape);
    }

    // Update is called once per frame
    public void Test(Kinect.Body b)
    {
        // Update global HandPattern objects
        FrameCheck(b);

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
        undividedSum = undividedSum + newLength;    // Add new distance to undivided sum
        handLength = undividedSum / (lengthCount + 1);    // Divide by new count to get average
        
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

        // Code for discrete gestures will be placed here
        // No discrete gestures are currently implemented

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

        //Debug.Log(currentDominantGesture);
        //Debug.Log(lastAvg);
    }
}