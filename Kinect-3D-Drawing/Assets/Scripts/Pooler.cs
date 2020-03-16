using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pooler : MonoBehaviour
{
    [SerializeField]
    private int poolAmount = 2000;
    public GameObject sphere;
    public static Queue<GameObject> pool;

    private static Pooler _instance;

    public static Pooler Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = GameObject.FindObjectOfType<Pooler>();
            }

            return _instance;
        }
    }

    // Start is called before the first frame update
    void Awake()
    {
        pool = new Queue<GameObject>();
        for (int i = 0; i < poolAmount; ++i)
        {
            pool.Enqueue(Instantiate(sphere, Vector3.zero, Quaternion.identity));
        }
    }

    public GameObject GetSphere ()
    {
        if (pool.Count == 0) pool.Enqueue(Instantiate(sphere, Vector3.zero, Quaternion.identity));
        return pool.Dequeue();
    }

}
