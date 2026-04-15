using System.Collections.Generic;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    public enum MoveDirection { Backward, Forward }

    [Header("Movement Settings")]
    [Tooltip("Backward = Chunks move towards -Z. Forward = Chunks move towards +Z.")]
    public MoveDirection chunkDirection = MoveDirection.Backward;
    public float worldMoveSpeed = 15f;    // How fast the world moves

    [Header("Chunk Settings")]
    public LevelDatabase levelDatabase;
    public float chunkLength = 40f;       // Exact Z-size of your chunk
    public int chunksOnScreen = 5;        // How many to render at once
    public int chunksPerLevel = 10;       // How many chunks before the Bridge appears

    [Tooltip("Distance from 0 where the chunk is destroyed (use a positive number)")]
    public float despawnDistance = 40f;

    private LevelThemeData currentTheme;
    private int chunksSpawnedThisLevel = 0;

    private Queue<GameObject> activeChunks = new Queue<GameObject>();
    private Transform lastSpawnedChunk;

    private void Start()
    {
        if (levelDatabase == null || levelDatabase.allThemes.Count == 0)
        {
            Debug.LogError("LevelManager: No Level Database assigned!");
            return;
        }

        currentTheme = levelDatabase.allThemes[0];

        // Pre-build the starting runway
        for (int i = 0; i < chunksOnScreen; i++)
        {
            SpawnNextChunk();
        }
    }

    private void Update()
    {
        // 1. Determine Movement Direction
        Vector3 moveDir = (chunkDirection == MoveDirection.Backward) ? Vector3.back : Vector3.forward;

        // Move all active chunks
        foreach (GameObject chunk in activeChunks)
        {
            chunk.transform.position += moveDir * worldMoveSpeed * Time.deltaTime;
        }

        // 2. Check if we need to spawn a new chunk
        bool needsNewChunk = false;

        if (chunkDirection == MoveDirection.Backward)
        {
            needsNewChunk = lastSpawnedChunk.position.z < (chunksOnScreen * chunkLength) - chunkLength;
        }
        else // Moving Forward
        {
            needsNewChunk = lastSpawnedChunk.position.z > -(chunksOnScreen * chunkLength) + chunkLength;
        }

        if (needsNewChunk)
        {
            SpawnNextChunk();
        }

        // 3. Destroy chunks that have passed the despawn threshold
        bool shouldDespawn = false;
        float firstChunkZ = activeChunks.Peek().transform.position.z;

        if (chunkDirection == MoveDirection.Backward)
        {
            shouldDespawn = firstChunkZ < -despawnDistance;
        }
        else // Moving Forward
        {
            shouldDespawn = firstChunkZ > despawnDistance;
        }

        if (shouldDespawn)
        {
            GameObject oldChunk = activeChunks.Dequeue();
            Destroy(oldChunk);
        }
    }

    private void SpawnNextChunk()
    {
        GameObject prefabToSpawn;

        // Check if it's time to spawn the transition bridge
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

        // Calculate exact spawn position based on direction
        float spawnZ = 0f;
        if (lastSpawnedChunk != null)
        {
            if (chunkDirection == MoveDirection.Backward)
            {
                spawnZ = lastSpawnedChunk.position.z + chunkLength; // Spawn ahead (+Z)
            }
            else
            {
                spawnZ = lastSpawnedChunk.position.z - chunkLength; // Spawn behind (-Z)
            }
        }

        Vector3 spawnPos = new Vector3(0, 0, spawnZ);

        // Spawn and track
        GameObject newChunk = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);
        newChunk.transform.SetParent(this.transform);

        activeChunks.Enqueue(newChunk);
        lastSpawnedChunk = newChunk.transform;
    }
}