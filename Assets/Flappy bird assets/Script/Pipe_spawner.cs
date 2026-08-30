using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pipe_spawner : MonoBehaviour
{
    [SerializeField] private GameObject pipePrefab;
    [SerializeField] private float spawnRate = 2f;

    [SerializeField] private float minY = -2f;
    [SerializeField] private float MaxY = -2f;
    void Start()
    {
        InvokeRepeating("Spawn_Pipe", 0f, spawnRate);
    }

    private void Spawn_Pipe()
    {
        Vector3 spawnPosition = new Vector3(
            transform.position.x,
            Random.Range(minY,MaxY),
            transform.position.z
        );
        Instantiate(pipePrefab, spawnPosition, transform.rotation,transform);
    }
}
