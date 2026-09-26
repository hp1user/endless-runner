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

    /// <summary>
    /// Checks whether the specified lane coordinate is allowed for the given restriction.
    /// </summary>
    public static bool IsLaneAllowed(float laneX, Obstacle.ObstacleLaneRestriction restriction)
    {
        switch (restriction)
        {
            case Obstacle.ObstacleLaneRestriction.SidesOnly:
                // Only allowed on side lanes (abs(X) > 0.5f), NEVER on center lane (X ~= 0)
                return Mathf.Abs(laneX) > 0.5f;

            case Obstacle.ObstacleLaneRestriction.CenterOnly:
                // Only allowed on center lane (X ~= 0)
                return Mathf.Abs(laneX) <= 0.5f;

            case Obstacle.ObstacleLaneRestriction.LeftOnly:
                // Only allowed on Left lane (X < -0.5f)
                return laneX < -0.5f;

            case Obstacle.ObstacleLaneRestriction.RightOnly:
                // Only allowed on Right lane (X > 0.5f)
                return laneX > 0.5f;

            case Obstacle.ObstacleLaneRestriction.AnyLane:
            default:
                return true;
        }
    }

    private void SpawnObstacle()
    {
        if (obstaclePool == null || obstaclePool.Count == 0) return;

        // Clone list of candidates to try picking valid prefab with available lanes
        List<GameObject> candidates = new List<GameObject>(obstaclePool);

        while (candidates.Count > 0)
        {
            int pickIdx = Random.Range(0, candidates.Count);
            GameObject prefabToSpawn = candidates[pickIdx];
            candidates.RemoveAt(pickIdx);

            if (prefabToSpawn == null) continue;

            Obstacle prefabObstacle = prefabToSpawn.GetComponent<Obstacle>();
            Obstacle.ObstacleLaneRestriction restriction = prefabObstacle != null 
                ? prefabObstacle.laneRestriction 
                : Obstacle.ObstacleLaneRestriction.AnyLane;

            // 1. Find safe and allowed lanes for this specific obstacle
            List<int> availableLanes = new List<int>();
            for (int i = 0; i < lanePositions.Length; i++)
            {
                float laneX = lanePositions[i];
                if (IsLaneAllowed(laneX, restriction) && SpawnTracker.IsLaneSafe(i, 2.0f))
                {
                    availableLanes.Add(i);
                }
            }

            // If this prefab has no safe allowed lanes right now, try another candidate from the pool
            if (availableLanes.Count == 0) continue;

            int chosenLaneIndex = availableLanes[Random.Range(0, availableLanes.Count)];
            SpawnTracker.RegisterSpawn(chosenLaneIndex);
            float chosenLaneX = lanePositions[chosenLaneIndex];

            Vector3 spawnPos = new Vector3(chosenLaneX, spawnYPosition, transform.position.z);

            // 2. Calculate randomized rotation from prefab settings
            Quaternion spawnRot = (prefabObstacle != null) ? prefabObstacle.GetSpawnRotation() : Quaternion.identity;

            // 3. Spawn and initialize
            GameObject spawnedObstacle = PoolManager.Instance.SpawnFromPool(prefabToSpawn, spawnPos, spawnRot);

            Obstacle obstacleScript = spawnedObstacle.GetComponent<Obstacle>();
            if (obstacleScript != null)
            {
                obstacleScript.Initialize(prefabToSpawn, obstacleDirection, obstacleDespawnThreshold);
            }

            return;
        }
    }
}
