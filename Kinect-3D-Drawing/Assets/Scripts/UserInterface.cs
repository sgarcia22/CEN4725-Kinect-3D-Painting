using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Kinect = Windows.Kinect;

public class UserInterface : MonoBehaviour
{
  
    public Sprite drawing, erasing, neutral, thumbsUp, thumbsDown;
    public SpriteRenderer rightRend, leftRend;

    private BodySourceView bodyView;

    private void Start()
    {
        //Reference to same body view as Game Manager
        bodyView = GameManager.Instance.bodyView;
    }

    /// <summary>
    /// Change Sprite of the Right Hand
    /// </summary>
    /// <param name="state"></param>
    public void ChangeSpriteRight(ProcessState state, bool checkUndoTimedOut = false, bool checkRedoTimedOut = false)
    {
        if ((checkUndoTimedOut && rightRend.sprite == thumbsDown) || checkRedoTimedOut && rightRend.sprite == thumbsUp) return;

        switch (state)
        {
            case ProcessState.Neutral:
                if (rightRend.sprite != neutral)
                {
                    rightRend.sprite = neutral;
                    rightRend.flipX = true;
                }
                break;
            case ProcessState.Drawing:
                if (rightRend.sprite != drawing)
                {
                    rightRend.sprite = drawing;
                    rightRend.flipX = true;
                }
                break;
            case ProcessState.Erasing:
                if (rightRend.sprite != erasing)
                {
                    rightRend.sprite = erasing;
                    rightRend.flipX = true;
                }
                break;
            case ProcessState.Undo:
                if (rightRend.sprite != thumbsDown)
                {
                    rightRend.sprite = thumbsDown;
                    rightRend.flipX = true;
                }
                break;
            case ProcessState.Redo:
                if (rightRend.sprite != thumbsUp)
                {
                    rightRend.sprite = thumbsUp;
                    rightRend.flipX = true;
                }
                break;
        }
    }

    /// <summary>
    /// Change Sprite of the Left Hand
    /// </summary>
    /// <param name="state"></param>
    public void ChangeSpriteLeft(ProcessState state)
    {
        switch (state)
        {
            case ProcessState.Neutral:
                if (leftRend.sprite != neutral)
                {
                    leftRend.sprite = neutral;
                    leftRend.flipX = false;
                }
                break;
            case ProcessState.Drawing:
                if (leftRend.sprite != drawing) {
                    leftRend.sprite = drawing;
                    leftRend.flipX = false;
                }
                break;
            case ProcessState.ZoomIn:
                if (leftRend.sprite != thumbsUp)
                {
                    leftRend.sprite = thumbsUp;
                    leftRend.flipX = false;
                }
                break;
            case ProcessState.ZoomOut:
                if (leftRend.sprite != thumbsDown)
                {
                    leftRend.sprite = thumbsDown;
                    leftRend.flipX = false;
                }
                break;
            case ProcessState.Select:
                if (leftRend.sprite != erasing)
                {
                    leftRend.sprite = erasing;
                    leftRend.flipX = false;
                }
                break;
        }
    }

    private void Update()
    {
        foreach (KeyValuePair<ulong, BodySourceView.BodyValue> body in bodyView.GetBodyGameObject())
        {
            //Get positions of right and left hand
            Kinect.Body bod = body.Value.body;
            Kinect.Joint sourceJointRightHand = bod.Joints[Kinect.JointType.WristRight];
            Kinect.Joint sourceJointLeftHand = bod.Joints[Kinect.JointType.HandLeft];
            Vector3 newRightHandPosition = bodyView.GetVector3FromJoint(sourceJointRightHand);
            Vector3 newLeftHandPosition = bodyView.GetVector3FromJoint(sourceJointLeftHand);
            //Move hands a little back to prevent rendering clipping
            newRightHandPosition.z += .5f;
            newLeftHandPosition.z += .5f;
            //Change the positions accordingly
            rightRend.gameObject.transform.position = newRightHandPosition;
            leftRend.gameObject.transform.position = newLeftHandPosition;
        }
    }

}
