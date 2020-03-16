using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Kinect = Windows.Kinect;

public class Erase : MonoBehaviour
{

    public GameObject eraserIndicator;

    // Game Manager Variables
    public BodySourceView bodyView;
    private List<GameObject> spheres;

    void Start()
    {
        InitializeVariables();
    }

    private void InitializeVariables()
    {
        spheres = GameManager.spheres;
    }

    public void Eraser(int? index)
    {
        if (index.HasValue && index.Value != -1 && GameManager.Instance.CurrentState == ProcessState.Erasing) {
            Debug.Log("Erasing");
            LineRenderer temp = spheres[index.Value].GetComponent<LineRenderer>();
            if (temp != null) temp.enabled = false;
            spheres[index.Value].SetActive(false);
        }
    }
}
