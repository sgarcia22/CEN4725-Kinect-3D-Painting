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
        public string side;         // “left” if left hand, “right” if right
        public int thumbExtended;  // True (1) if the thumb joint is extended
        public int handTipOpen;    // True (1) if the hand tip join is extended
        public double palmPitch;    // Pitch of hand joint relative to Kinect
        public double palmRoll;     // Roll of hand joint relative to Kinect
        public double palmYaw;      // Yaw of hand joint relative to Kinect
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
        public double minPalmPitch;     // Min possible matching palm pitch
        public double maxPalmPitch;     // Max possible matching palm pitch
        public double minPalmRoll;      // Min possible matching palm roll
        public double maxPalmRoll;      // Max possible matching palm roll
        public double minPalmYaw;       // Min possible matching palm yaw
        public double maxPalmYaw;       // Max possible matching palm yaw

        // Returns true if the pattern matches the unit gesture thresholds
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


                // Check if the palm pitch is within the threshold
                if (shape.palmPitch < minPalmPitch || shape.palmPitch > maxPalmPitch)
                {
                    continue;
                }


                // Check if the palm roll is within the threshold
                if (shape.palmRoll < minPalmRoll || shape.palmRoll > maxPalmRoll)
                {
                    continue;
                }

                // Check if the palm yaw is within the threshold
                if (shape.palmYaw < minPalmYaw || shape.palmYaw > maxPalmYaw)
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
    public double lastAvg;
    public double lastCount;

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

        // Used to calculate averages
        lastAvg = 0.0;
        lastCount = 0;


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
        triggerGesture.minPalmPitch = 0.0;                              // Minimum palm pitch is 0 degrees
        triggerGesture.maxPalmPitch = 360.0;                            // Maximum palm pitch is 360 degrees
        triggerGesture.minPalmRoll = 0.0;                               // Minimum palm roll is 0 degrees
        triggerGesture.maxPalmRoll = 360.0;                             // Maximum palm roll is 360 degrees
        triggerGesture.minPalmYaw = 0.0;                                // Minimum palm yaw is 360 degrees
        triggerGesture.maxPalmYaw = 360.0;                              // Maximum palm yaw is 360 degrees
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
        triggerGesture.minPalmPitch = 0.0;                              // Minimum palm pitch is 0 degrees
        triggerGesture.maxPalmPitch = 360.0;                            // Maximum palm pitch is 360 degrees
        triggerGesture.minPalmRoll = 0.0;                               // Minimum palm roll is 0 degrees
        triggerGesture.maxPalmRoll = 360.0;                             // Maximum palm roll is 360 degrees
        triggerGesture.minPalmYaw = 0.0;                                // Minimum palm yaw is 360 degrees
        triggerGesture.maxPalmYaw = 360.0;                              // Maximum palm yaw is 360 degrees
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
        triggerGesture.minPalmPitch = 0.0;                              // Minimum palm pitch is 0 degrees
        triggerGesture.maxPalmPitch = 360.0;                            // Maximum palm pitch is 360 degrees
        triggerGesture.minPalmRoll = 0.0;                               // Minimum palm roll is 0 degrees
        triggerGesture.maxPalmRoll = 360.0;                             // Maximum palm roll is 360 degrees
        triggerGesture.minPalmYaw = 0.0;                                // Minimum palm yaw is 360 degrees
        triggerGesture.maxPalmYaw = 360.0;                              // Maximum palm yaw is 360 degrees
        ContinuousGesture Erase = new ContinuousGesture();              // Create new ContinuousGesture object
        Erase.gestureName = gestureName;                                // Assign "Erase" to ContinuousGesture name
        Erase.dominant = dominant;                                      // Indicate that this is for the dominant hand
        Erase.triggerGesture = triggerGesture;                          // Assign triggerGesture to Erase 
        allCGestures[3] = Erase;                                        // Add Erase to allCGestures

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
    public double[] getHandAngles(Kinect.Body b, string side)
    {
        Kinect.Joint handJoint;

        if(side == "right")
        {
            handJoint = b.Joints[Kinect.JointType.HandRight];
        }
        else
        {
            handJoint = b.Joints[Kinect.JointType.HandLeft];
        }

        // Determine angle relative to x-axis
        Vector3 handJointVector = bodyview.GetVector3FromJoint(handJoint);
        Vector3 x_axis = new Vector3(1000, handJointVector.y, handJointVector.z);

        if (side == "right")
        {
            //Debug.Log(Vector3.Angle(x_axis, handJointVector));
        }

        return null;
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
        palmPitchRollYaw = getHandAngles(b, "left");
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

        // Right now, we're not adding the values of the palm pitch, roll, and yaw
        // because they are not relevant to the three implemented gestures

        // Add newest values to rightPattern
        side = "right";
        thumbExtended = checkThumbExtended(b, "right");
        handTipOpen = checkHandTipOpen(b, "right");
        palmPitchRollYaw = getHandAngles(b, "right");
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

        // Right now, we're not adding the values of the palm pitch, roll, and yaw
        // because they are not relevant to the three implemented gestures

        // Add the new HandShape objects to the HandPattern objects
        leftPattern.add(leftShape);
        rightPattern.add(rightShape);
    }

    // Update is called once per frame
    public void Test(Kinect.Body b)
    {
        // Update global HandPattern objects
        FrameCheck(b);

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

        // Find the current dominant gesture
        foreach (ContinuousGesture cg in allCGestures)
        {
            if (!cg.dominant)
            {
                continue;
            }
            if (cg.score > bestGestureScore && cg.score > 40)
            {
                bestGestureScore = cg.score;
                bestGestureName = cg.gestureName;
            }
        }
        currentDominantGesture = bestGestureName;

        bestGestureName = "Neutral";
        bestGestureScore = 0.0;

        // Find the current non-dominant gesture
        foreach (ContinuousGesture cg in allCGestures)
        {
            if (cg.dominant)
            {
                continue;
            }
            if (cg.score > bestGestureScore && cg.score > 40)
            {
                bestGestureScore = cg.score;
                bestGestureName = cg.gestureName;
            }
        }
        currentNonDominantGesture = bestGestureName;

        //Debug.Log(currentDominantGesture);
        //Debug.Log(lastAvg);
    }
}