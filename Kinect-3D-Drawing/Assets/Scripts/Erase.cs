using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Kinect = Windows.Kinect;

public class Erase : MonoBehaviour
{
    public bool erasing;
    public BodySourceView bodyView;
    public GameObject sphere, parent;

    private HandStates states;
    private int index;
    private List<GameObject> spheres;

    void Start()
    {
        states = new HandStates();
        InitializeVariables();
    }

    private void InitializeVariables()
    {
        spheres = GameManager.spheres;
        index = 0;
        erasing = false;
    }

    public void Eraser(Kinect.Body b, int index)
    {
       //Here you can do something like

        /*if (index != null) {

        spheres[index].GetComponent<LineRenderer>().setActive(false);
        spheres[index].SetActive(false);
        }
    
     */
    }
}
