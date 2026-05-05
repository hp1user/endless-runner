using System.Collections.Generic;
using Player.Control;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    public enum MoveDirection { Backward, Forward }

    [Header("Movement Settings")]
    [Tooltip("Backward = Chunks move towards -Z. Forward = Chunks move towards +Z.")]
    public MoveDirection chunkDirection = MoveDirection.Backward;
    public float worldMoveSpeed = 15f;

    [Header("Chunk Settings")]
    public LevelDatabase levelDatabase;
    public float chunkLength = 40f;
    public int chunksOnScreen = 5;

    [Tooltip("Distance from 0 where the chunk is destroyed (use a positive number)")]
    public float despawnDistance = 40f;

    private LevelThemeData currentTheme;
    private int currentThemeIndex = 0; // NEW: Tracks which theme we are currently using

    // THE FIX: We now track both the spawned chunk AND the prefab it came from!
    private struct ChunkTracker
    {
        public GameObject instance;
        public GameObject originalPrefab;
    }

    private Queue<ChunkTracker> activeChunks = new Queue<ChunkTracker>();
    private Transform lastSpawnedChunk;

    private bool isGameOver = false;
    private bool isBossPhase = false; // NEW: Tells the spawner to use the bridge!

    private void OnEnable()
    {
        PlayerController.OnPlayerDeath += HandleGameOver;
        GameManager.OnBossFightStarted += HandleBossFight; // Listen for the Boss
        GameManager.OnLevelCompleted += HandleLevelComplete; // Listen for Theme changes
    }

    private void OnDisable()
    {
        PlayerController.OnPlayerDeath -= HandleGameOver;
        GameManager.OnBossFightStarted -= HandleBossFight;
        GameManager.OnLevelCompleted -= HandleLevelComplete;
    }

    private void HandleGameOver()
    {
        isGameOver = true;
    }

    private void HandleBossFight()
    {
        isBossPhase = true; // Switch to Bridge chunks!
    }

    private void HandleLevelComplete(int newLevel)
    {
        isBossPhase = false; // Turn off the Bridge chunks

        // Move to the next theme in the database!
        currentThemeIndex++;

        // If we run out of themes, loop back to the beginning
        if (currentThemeIndex >= levelDatabase.allThemes.Count)
        {
            currentThemeIndex = 0;
        }

        currentTheme = levelDatabase.allThemes[currentThemeIndex];
        Debug.Log($"<color=cyan>[LevelManager]</color> Advancing to Theme: {currentTheme.themeName}");
    }

    private void Start()
    {
        if (levelDatabase == null || levelDatabase.allThemes.Count == 0)
        {
            Debug.LogError("LevelManager: No Level Database assigned!");
            return;
        }

        // Start with the first theme in the database
        currentThemeIndex = 0;
        currentTheme = levelDatabase.allThemes[currentThemeIndex];

        for (int i = 0; i < chunksOnScreen; i++)
        {
            SpawnNextChunk();
        }
    }

    private void Update()
    {
        if (isGameOver) return;

        Vector3 moveDir = (chunkDirection == MoveDirection.Backward) ? Vector3.back : Vector3.forward;

        // 1. Move all active chunks
        foreach (ChunkTracker tracker in activeChunks)
        {
            tracker.instance.transform.position += moveDir * worldMoveSpeed * Time.deltaTime;
        }

        // 2. Check if we need to spawn a new chunk
        bool needsNewChunk = false;

        if (chunkDirection == MoveDirection.Backward)
        {
            needsNewChunk = lastSpawnedChunk.position.z < (chunksOnScreen * chunkLength) - chunkLength;
        }
        else
        {
            needsNewChunk = lastSpawnedChunk.position.z > -(chunksOnScreen * chunkLength) + chunkLength;
        }

        if (needsNewChunk)
        {
            SpawnNextChunk();
        }

        // 3. POOL RETURN: Recycle chunks that have passed the despawn threshold
        bool shouldDespawn = false;
        float firstChunkZ = activeChunks.Peek().instance.transform.position.z;

        if (chunkDirection == MoveDirection.Backward)
            shouldDespawn = firstChunkZ < -despawnDistance;
        else
            shouldDespawn = firstChunkZ > despawnDistance;

        if (shouldDespawn)
        {
            ChunkTracker oldChunk = activeChunks.Dequeue();
            PoolManager.Instance.ReturnToPool(oldChunk.instance, oldChunk.originalPrefab);
        }
    }

    private void SpawnNextChunk()
    {
        GameObject prefabToSpawn;

        // NEW: Let the GameManager dictate what spawns, not a chunk counter!
        if (isBossPhase)
        {
            prefabToSpawn = currentTheme.transitionBridge;
        }
        else
        {
            int randomVariantIndex = Random.Range(0, currentTheme.chunkVariants.Length);
            prefabToSpawn = currentTheme.chunkVariants[randomVariantIndex];
        }

        float spawnZ = 0f;
        if (lastSpawnedChunk != null)
        {
            if (chunkDirection == MoveDirection.Backward)
                spawnZ = lastSpawnedChunk.position.z + chunkLength;
            else
                spawnZ = lastSpawnedChunk.position.z - chunkLength;
        }

        Vector3 spawnPos = new Vector3(0, 0, spawnZ);

        GameObject newChunk = PoolManager.Instance.SpawnFromPool(prefabToSpawn, spawnPos, Quaternion.identity, this.transform);

        ChunkTracker newTracker = new ChunkTracker { instance = newChunk, originalPrefab = prefabToSpawn };
        activeChunks.Enqueue(newTracker);

        lastSpawnedChunk = newChunk.transform;
    }
}