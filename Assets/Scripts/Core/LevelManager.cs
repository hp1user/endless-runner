using System.Collections.Generic;
using Player.Control;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    public enum MoveDirection { Backward, Forward }

    [Header("Movement Settings")]
    [Tooltip("Backward = Chunks move towards -Z. Forward = Chunks move towards +Z.")]
    public MoveDirection chunkDirection = MoveDirection.Forward;
    public float worldMoveSpeed = 15f;

    [Header("Transition Settings")]
    [Tooltip("World movement speed during bridge approach transition (smooth ~12-15s city run)")]
    public float transitionSpeed = 4.5f;

    [Header("Bridge & Boss Arena Settings")]
    [Tooltip("If true, normal road chunks (chunkVariants or customBossRoadChunk) will be used for the boss fight instead of a separate bridge model.")]
    public bool useRoadChunksForBridge = true;

    [Tooltip("Optional: Specify a particular road chunk prefab to use during the boss encounter. If null, random road chunks from the current theme's chunkVariants are used.")]
    public GameObject customBossRoadChunk;

    [Tooltip("Maximum duration (seconds) for the approach transition when using road chunks before boss battle begins.")]
    public float roadTransitionDuration = 15f;

    [Header("Chunk Settings")]
    public LevelDatabase levelDatabase;
    public float chunkLength = 40f;
    public int chunksOnScreen = 3;

    [Tooltip("Distance from 0 where the chunk is destroyed (use a positive number)")]
    public float despawnDistance = 45f;

    public bool isTransitioningToBridge { get; private set; } = false;
    public bool isPlayerOnBridge { get; private set; } = false;

    private LevelThemeData currentTheme;
    private int currentThemeIndex = 0;

    public struct ChunkTracker
    {
        public GameObject instance;
        public GameObject originalPrefab;
    }

    private Queue<ChunkTracker> activeChunks = new Queue<ChunkTracker>();
    private Transform lastSpawnedChunk;

    private bool isGameOver = false;
    private bool isBossPhase = false;

    // Road fallback tracking
    private GameObject targetBossChunkInstance;
    private float roadTransitionTimer = 0f;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void OnEnable()
    {
        PlayerController.OnPlayerDeath += HandleGameOver;
        GameManager.OnBossTransitionStarted += HandleBossTransitionStarted;
        GameManager.OnBossFightStarted += HandleBossFight;
        GameManager.OnBossDefeated += HandleBossDefeated;
        GameManager.OnLevelCompleted += HandleLevelComplete;
    }

    private void OnDisable()
    {
        PlayerController.OnPlayerDeath -= HandleGameOver;
        GameManager.OnBossTransitionStarted -= HandleBossTransitionStarted;
        GameManager.OnBossFightStarted -= HandleBossFight;
        GameManager.OnBossDefeated -= HandleBossDefeated;
        GameManager.OnLevelCompleted -= HandleLevelComplete;
    }

    private void HandleGameOver()
    {
        isGameOver = true;
    }

    private void HandleBossTransitionStarted()
    {
        isBossPhase = true;
        isTransitioningToBridge = true;
        isPlayerOnBridge = false;
        roadTransitionTimer = roadTransitionDuration;

        bool shouldUseRoadFallback = useRoadChunksForBridge || currentTheme == null || currentTheme.transitionBridge == null;

        // Place the Bridge chunk at the horizon (Chunk 2, ~60-80m away) so it immediately connects to the city road ahead
        if (!shouldUseRoadFallback && currentTheme != null && currentTheme.transitionBridge != null && activeChunks.Count >= 3)
        {
            var chunkList = new List<ChunkTracker>(activeChunks);
            // chunkList[0] = City chunk currently under player
            // chunkList[1] = City road in front of player
            // chunkList[2] and beyond = Bridge at the horizon!
            for (int i = 2; i < chunkList.Count; i++)
            {
                if (chunkList[i].originalPrefab != currentTheme.transitionBridge)
                {
                    Vector3 pos = chunkList[i].instance.transform.position;
                    Quaternion rot = chunkList[i].instance.transform.rotation;
                    PoolManager.Instance.ReturnToPool(chunkList[i].instance, chunkList[i].originalPrefab);
                    GameObject newBridge = PoolManager.Instance.SpawnFromPool(currentTheme.transitionBridge, pos, rot, this.transform);
                    chunkList[i] = new ChunkTracker { instance = newBridge, originalPrefab = currentTheme.transitionBridge };
                }
            }
            activeChunks = new Queue<ChunkTracker>(chunkList);
            lastSpawnedChunk = chunkList[chunkList.Count - 1].instance.transform;
            targetBossChunkInstance = null;
            Debug.Log("<color=yellow>[LevelManager]</color> Boss Phase started: Bridge placed at the horizon. Player running towards bridge!");
        }
        else if (shouldUseRoadFallback && activeChunks.Count > 0)
        {
            var chunkList = new List<ChunkTracker>(activeChunks);
            // The horizon chunk marks the start of the boss arena
            int targetIdx = Mathf.Min(2, chunkList.Count - 1);
            targetBossChunkInstance = chunkList[targetIdx].instance;
            Debug.Log("<color=yellow>[LevelManager]</color> Boss Phase started: Using road chunks for boss arena. Player approaching boss encounter...");
        }
    }

    private void HandleBossFight()
    {
        isBossPhase = true;
        isTransitioningToBridge = false;
        isPlayerOnBridge = true;
    }

    private void HandleBossDefeated()
    {
        isBossPhase = false;
        isTransitioningToBridge = false;
        isPlayerOnBridge = false;
        targetBossChunkInstance = null;

        Debug.Log("<color=orange>[LevelManager]</color> Boss Defeated: Resuming normal road generation!");
    }

    private void HandleLevelComplete(int newLevel)
    {
        isBossPhase = false;
        isTransitioningToBridge = false;
        isPlayerOnBridge = false;
        targetBossChunkInstance = null;

        if (levelDatabase != null && levelDatabase.allThemes.Count > 0)
        {
            currentThemeIndex = (currentThemeIndex + 1) % levelDatabase.allThemes.Count;
            currentTheme = levelDatabase.allThemes[currentThemeIndex];
            Debug.Log($"<color=cyan>[LevelManager]</color> Advancing to Theme: {currentTheme.themeName}");
        }
    }

    private void Start()
    {
        if (levelDatabase == null || levelDatabase.allThemes.Count == 0)
        {
            Debug.LogError("LevelManager: No Level Database assigned!");
            return;
        }

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

        // Clamp deltaTime to prevent the world from teleporting when unpausing
        float dt = Mathf.Min(Time.deltaTime, 0.1f);
        Vector3 moveDir = (chunkDirection == MoveDirection.Backward) ? Vector3.back : Vector3.forward;
        float currentSpeed = isTransitioningToBridge ? transitionSpeed : worldMoveSpeed;

        // 1. Move all active chunks
        foreach (ChunkTracker tracker in activeChunks)
        {
            if (tracker.instance != null)
            {
                tracker.instance.transform.position += moveDir * currentSpeed * dt;
            }
        }

        // 2. Detect which chunk is currently under the player (at Z = 0)
        if (activeChunks.Count > 0 && currentTheme != null)
        {
            ChunkTracker chunkAtPlayer = GetChunkAtPlayer();
            if (chunkAtPlayer.instance != null)
            {
                bool shouldUseRoadFallback = useRoadChunksForBridge || currentTheme.transitionBridge == null;

                if (!shouldUseRoadFallback && currentTheme.transitionBridge != null)
                {
                    bool isBridge = (chunkAtPlayer.originalPrefab == currentTheme.transitionBridge);

                    if (!isPlayerOnBridge && isBridge)
                    {
                        isPlayerOnBridge = true;
                        isTransitioningToBridge = false;
                        Debug.Log("<color=green>[LevelManager]</color> Player stepped onto the Bridge! Triggering Boss Fight...");
                        if (GameManager.Instance != null)
                        {
                            GameManager.Instance.OnBridgeReached();
                        }
                    }
                    else if (isPlayerOnBridge && !isBridge)
                    {
                        isPlayerOnBridge = false;
                        Debug.Log("<color=cyan>[LevelManager]</color> Player exited the Bridge and returned to the City!");
                    }
                }
                else
                {
                    // Road chunks used for boss arena
                    if (isTransitioningToBridge)
                    {
                        roadTransitionTimer -= dt;
                        bool reachedTargetChunk = (targetBossChunkInstance != null && chunkAtPlayer.instance == targetBossChunkInstance);
                        bool timerExpired = (roadTransitionTimer <= 0f);

                        if (reachedTargetChunk || timerExpired)
                        {
                            isPlayerOnBridge = true;
                            isTransitioningToBridge = false;
                            targetBossChunkInstance = null;
                            Debug.Log("<color=green>[LevelManager]</color> Player reached Boss Arena (Road Chunks)! Triggering Boss Fight...");
                            if (GameManager.Instance != null)
                            {
                                GameManager.Instance.OnBridgeReached();
                            }
                        }
                    }
                }
            }
        }

        // 3. Recycle chunks that have passed behind the camera and immediately replenish at horizon
        if (activeChunks.Count > 0)
        {
            // Keep chunks within visible camera range (despawn promptly once past player and camera)
            float threshold = Mathf.Min(despawnDistance, chunkLength + 5f);
            bool shouldDespawn = false;
            float firstChunkZ = activeChunks.Peek().instance.transform.position.z;

            if (chunkDirection == MoveDirection.Backward)
                shouldDespawn = firstChunkZ < -threshold;
            else
                shouldDespawn = firstChunkZ > threshold;

            if (shouldDespawn)
            {
                ChunkTracker oldChunk = activeChunks.Dequeue();

                // If target chunk despawned during road fallback transition, trigger boss arena immediately
                if (isTransitioningToBridge && oldChunk.instance != null && oldChunk.instance == targetBossChunkInstance)
                {
                    isPlayerOnBridge = true;
                    isTransitioningToBridge = false;
                    targetBossChunkInstance = null;
                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.OnBridgeReached();
                    }
                }

                PoolManager.Instance.ReturnToPool(oldChunk.instance, oldChunk.originalPrefab);

                // Maintain constant chunk count: immediately spawn next chunk at horizon
                SpawnNextChunk();
            }
        }
    }

    /// <summary>
    /// Returns the chunk tracker whose center is currently closest to the player at Z = 0.
    /// </summary>
    public ChunkTracker GetChunkAtPlayer()
    {
        if (activeChunks.Count == 0) return default;

        ChunkTracker closest = default;
        float minDistance = float.MaxValue;

        foreach (ChunkTracker tracker in activeChunks)
        {
            if (tracker.instance == null) continue;
            float dist = Mathf.Abs(tracker.instance.transform.position.z);
            if (dist < minDistance)
            {
                minDistance = dist;
                closest = tracker;
            }
        }

        return closest;
    }

    private void SpawnNextChunk()
    {
        if (currentTheme == null) return;

        GameObject prefabToSpawn;
        bool shouldUseRoadFallback = useRoadChunksForBridge || currentTheme.transitionBridge == null;

        // In boss phase, spawn the Bridge chunk if available and not using road fallback!
        if (isBossPhase && !shouldUseRoadFallback && currentTheme.transitionBridge != null)
        {
            prefabToSpawn = currentTheme.transitionBridge;
        }
        else
        {
            if (isBossPhase && customBossRoadChunk != null)
            {
                prefabToSpawn = customBossRoadChunk;
            }
            else
            {
                if (currentTheme.chunkVariants == null || currentTheme.chunkVariants.Length == 0) return;
                int randomVariantIndex = Random.Range(0, currentTheme.chunkVariants.Length);
                prefabToSpawn = currentTheme.chunkVariants[randomVariantIndex];
            }
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

        // Support dynamic runtime procedural chunks
        var procChunk = newChunk.GetComponent<EndlessRunner.LevelGen.ProceduralChunk>();
        if (procChunk != null)
        {
            procChunk.GenerateRandomLayout();
        }

        ChunkTracker newTracker = new ChunkTracker { instance = newChunk, originalPrefab = prefabToSpawn };
        activeChunks.Enqueue(newTracker);

        lastSpawnedChunk = newChunk.transform;
    }
}