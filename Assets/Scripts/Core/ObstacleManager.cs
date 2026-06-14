using UnityEngine;
using System.Collections.Generic;
using Player.Control;

public class ObstacleManager : MonoBehaviour
{
    [Header("Spawn Settings")]
    [Tooltip("X = Minimum time, Y = Maximum time between obstacle spawns")]
    public Vector2 spawnIntervalRange = new Vector2(4f, 10f);
    public float[] lanePositions = new float[] { -2f, 0f, 2f };
    public float spawnYPosition = 0f;

    [Header("Movement Overrides")]
    public Obstacle.MoveDirection obstacleDirection = Obstacle.MoveDirection.Forward;
    public float obstacleDespawnThreshold = 20f;

    [Header("Obstacle Pool")]
    public List<GameObject> obstaclePool;

    private float timer;
    private float currentTargetInterval;
    private bool isGameOver = false;

    private void OnEnable()
    {
        PlayerController.OnPlayerDeath += HandleGameOver;
    }

    private void OnDisable()
    {
        PlayerController.OnPlayerDeath -= HandleGameOver;
    }

    private void HandleGameOver()
    {
        isGameOver = true;
    }

    private void Start()
    {
        PickNewSpawnInterval();
    }

    private void Update()
    {
        if (isGameOver) return;

        timer += Time.deltaTime;

        if (timer >= currentTargetInterval)
        {
            timer = 0f;
            SpawnObstacle();
            PickNewSpawnInterval();
        }
    }

    private void PickNewSpawnInterval()
    {
        currentTargetInterval = Random.Range(spawnIntervalRange.x, spawnIntervalRange.y);
    }

    private void SpawnObstacle()
    {
        if (obstaclePool == null || obstaclePool.Count == 0) return;

        // 1. Pick a random prefab
        GameObject prefabToSpawn = obstaclePool[Random.Range(0, obstaclePool.Count)];

        // 2. Find a safe lane to spawn in (coordinated with LootManager via SpawnTracker)
        List<int> availableLanes = new List<int>();
        for (int i = 0; i < lanePositions.Length; i++)
        {
            // Requires the lane to be free of recent spawns (2.0s cooldown)
            if (SpawnTracker.IsLaneSafe(i, 2.0f)) 
            {
                availableLanes.Add(i);
            }
        }

        // If no lanes are safe, skip spawning this frame
        if (availableLanes.Count == 0) return;

        int chosenLaneIndex = availableLanes[Random.Range(0, availableLanes.Count)];
        SpawnTracker.RegisterSpawn(chosenLaneIndex);
        float chosenLaneX = lanePositions[chosenLaneIndex];

        Vector3 spawnPos = new Vector3(chosenLaneX, spawnYPosition, transform.position.z);

        // 3. Spawn and initialize
        GameObject spawnedObstacle = PoolManager.Instance.SpawnFromPool(prefabToSpawn, spawnPos, Quaternion.identity);

        Obstacle obstacleScript = spawnedObstacle.GetComponent<Obstacle>();
        if (obstacleScript != null)
        {
            obstacleScript.Initialize(prefabToSpawn, obstacleDirection, obstacleDespawnThreshold);
        }
    }
}
