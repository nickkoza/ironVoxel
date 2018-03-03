// -------   ironVoxel   -------
// Copyright 2014  Nicholas Koza

using UnityEngine;
using System.Collections;

using ironVoxel;

public class DestroyTimer : MonoBehaviour {
    
    public float delayInSeconds = 5.0f;
    private int destroyCountdown;
    
    // Use this for initialization
    void Start()
    {
        destroyCountdown = (int)(60.0f * delayInSeconds);
    }
    
    // Update is called once per frame
    void Update()
    {
        destroyCountdown--;
        if (destroyCountdown <= 0) {
            Destroy(gameObject);
        }
    }
}
