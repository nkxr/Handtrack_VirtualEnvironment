using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pipe_spawner : MonoBehaviour
{
    [SerializeField] private GameObject pipePrefab;
    [SerializeField] private float minY = -2f;
    [SerializeField] private float MaxY = -2f;

    private float distanceSinceLastSpawn;
    private bool spawningPaused;
    private int pipesSpawnedThisWave;

    public void ResetSpawnTimer()
    {
        distanceSinceLastSpawn = 0f;
    }

    public void SetSpawningPaused(bool paused)
    {
        spawningPaused = paused;
        if (paused)
            distanceSinceLastSpawn = 0f;
    }

    public void StartWave()
    {
        pipesSpawnedThisWave = 0;
        distanceSinceLastSpawn = 0f;
        spawningPaused = false;
    }

    void Update()
    {
        if (GameManager.instance == null || spawningPaused)
            return;

        int pipesPerWave = GameManager.instance.PipesPerWave;
        if (pipesSpawnedThisWave >= pipesPerWave)
            return;

        distanceSinceLastSpawn += GameManager.instance.PipeSpeed * Time.deltaTime;
        if (distanceSinceLastSpawn >= GameManager.instance.PipeSpacing)
        {
            distanceSinceLastSpawn = 0f;
            Spawn_Pipe();
            pipesSpawnedThisWave++;

            if (pipesSpawnedThisWave >= pipesPerWave)
                spawningPaused = true;
        }
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
