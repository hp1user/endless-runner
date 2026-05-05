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
    public int chunksPerLevel = 10;

    [Tooltip("Distance from 0 where the chunk is destroyed (use a positive number)")]
    public float despawnDistance = 40f;

    private LevelThemeData currentTheme;
    private int chunksSpawnedThisLevel = 0;

    // THE FIX: We now track both the spawned chunk AND the prefab it came from!
    private struct ChunkTracker
    {
        public GameObject instance;
        public GameObject originalPrefab;
    }

    private Queue<ChunkTracker> activeChunks = new Queue<ChunkTracker>();
    private Transform lastSpawnedChunk;

    private bool isGameOver = false;

    // Listen for the death shout when this script turns on
    private void OnEnable()
    {
        PlayerController.OnPlayerDeath += HandleGameOver;
    }

    // Stop listening if this script gets destroyed
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
        if (levelDatabase == null || levelDatabase.allThemes.Count == 0)
        {
            Debug.LogError("LevelManager: No Level Database assigned!");
            return;
        }

        currentTheme = levelDatabase.allThemes[0];

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
            // Grab the oldest chunk from the queue
            ChunkTracker oldChunk = activeChunks.Dequeue();

            // INSTEAD OF DESTROYING, RETURN IT TO THE POOL!
            PoolManager.Instance.ReturnToPool(oldChunk.instance, oldChunk.originalPrefab);
        }
    }

    private void SpawnNextChunk()
    {
        GameObject prefabToSpawn;

        if (chunksSpawnedThisLevel >= chunksPerLevel)
        {
            prefabToSpawn = currentTheme.transitionBridge;
            currentTheme = levelDatabase.allThemes[Random.Range(0, levelDatabase.allThemes.Count)];
            chunksSpawnedThisLevel = 0;
        }
        else
        {
            int randomVariantIndex = Random.Range(0, currentTheme.chunkVariants.Length);
            prefabToSpawn = currentTheme.chunkVariants[randomVariantIndex];
            chunksSpawnedThisLevel++;
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

        // INSTEAD OF INSTANTIATING, SPAWN FROM THE POOL!
        GameObject newChunk = PoolManager.Instance.SpawnFromPool(prefabToSpawn, spawnPos, Quaternion.identity, this.transform);

        // Add to our tracking queue
        ChunkTracker newTracker = new ChunkTracker { instance = newChunk, originalPrefab = prefabToSpawn };
        activeChunks.Enqueue(newTracker);

        lastSpawnedChunk = newChunk.transform;
    }
}