using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SphereController : MonoBehaviour
{

    public int index;

    void Start()
    {

    }

    void Update()
    {

    }

    private void OnCollisionEnter(Collision collision)
    {
        //Collision with player
        if (collision.gameObject.name != "SphereBody")
        {
            GameManager.Instance.SphereCollided(index);
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.name != "SphereBody")
        {
            GameManager.Instance.SphereCollided(-1);
        }
    }
}