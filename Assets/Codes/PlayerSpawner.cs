using System;
using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    public GameObject PlayerPrefab;
    public Vector3 spawnPosition;
    public GameObject currentPlayerPrefab;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SpawnNewPlayerCar()
    {
        currentPlayerPrefab = GameObject.FindGameObjectWithTag("Player");
        Destroy(currentPlayerPrefab);
        Instantiate(PlayerPrefab, spawnPosition, Quaternion.LookRotation(Vector3.back));
        
        
    }
}
