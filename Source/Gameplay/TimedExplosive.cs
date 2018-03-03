// -------   ironVoxel   -------
// Copyright 2014  Nicholas Koza

using UnityEngine;
using System.Collections;

using ironVoxel;

public class TimedExplosive : MonoBehaviour {

    public float delayInSeconds = 5.0f;
    public float explosiveBlockRadius = 3.0f;
    private int destroyCountdown;
    public GameObject particleGenerator;

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
            ironVoxel.Service.ChunkRepository.RemoveBlocksWithinRadius(transform.position, explosiveBlockRadius, BlockParticle.CreateBlockParticle);
            if (particleGenerator != null) {
                GameObject.Instantiate(particleGenerator, transform.position, Quaternion.identity);
            }
            Destroy(gameObject);
        }
    }
}
