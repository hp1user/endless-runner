using UnityEngine;
using System.Collections.Generic;
using Enemy.Control;
using Player.Control;

public class EnemyManager : MonoBehaviour
{
    [Header("Settings")]
    public PlayerController player;
    public EnemyDatabase enemyDatabase;
    public List<Transform> spawnPoints = new List<Transform>();

    [Tooltip("How much random distance to add around the spawn point?")]
    public float spawnSpread = 3f;

    [Tooltip("How much vertical height to add to the spawn point?")]
    public float spawnOffsetY = 0f;

    [Tooltip("Time between spawns in seconds.")]
    public float spawnInterval = 3f;

    [Tooltip("Maximum number of enemies allowed in the scene at once.")]
    public int maxEnemies = 5;

    [Header("Boss Settings")]
    [Tooltip("How far behind the player should the boss spawn? (Negative number)")]
    public float bossSpawnZOffset = -25f;

    private int activeEnemyCount = 0;
    private float timer;

    private bool isGameOver = false;

    private void OnEnable()
    {
        PlayerController.OnPlayerDeath += HandleGameOver;
        GameManager.OnBossFightStarted += SpawnBoss; // NEW: Listen for the Boss!
    }

    private void OnDisable()
    {
        PlayerController.OnPlayerDeath -= HandleGameOver;
        GameManager.OnBossFightStarted -= SpawnBoss; // NEW: Stop listening
    }

    private void HandleGameOver()
    {
        isGameOver = true;
    }

    private void Start()
    {
        if (enemyDatabase == null)
        {
            Debug.LogError("[EnemyManager] Please assign an Enemy Database!");
            return;
        }

        if (spawnPoints.Count == 0)
        {
            Debug.LogWarning("[EnemyManager] No spawn points assigned. Enemies will spawn at (0,0,0).");
        }
    }

    private void Update()
    {
        if (isGameOver) return;

        if (GameManager.Instance != null && GameManager.Instance.isBossFightActive) return;

        timer += Time.deltaTime;

        float currentSpawnRate = spawnInterval / (GameManager.Instance != null ? GameManager.Instance.currentLevel : 1);

        // FIXED: Using currentSpawnRate here instead of spawnInterval!
        if (timer >= currentSpawnRate)
        {
            timer = 0f;
            TrySpawnEnemy();
        }
    }

    private void TrySpawnEnemy()
    {
        // 1. Check constraints
        if (activeEnemyCount >= maxEnemies) return;
        if (enemyDatabase == null) return;

        // 2. NEW: Ask the database for a valid enemy for the CURRENT level
        int currentLevel = GameManager.Instance != null ? GameManager.Instance.currentLevel : 1;
        EnemyEntry data = enemyDatabase.GetRandomEnemyForLevel(currentLevel);

        if (data == null || data.prefab == null) return;

        Transform spawnOrigin = null;
        if (spawnPoints.Count > 0)
        {
            spawnOrigin = spawnPoints[Random.Range(0, spawnPoints.Count)];
        }

        Vector3 spawnPos = Vector3.zero;
        Quaternion spawnRot = Quaternion.identity;

        // ALL OF YOUR CUSTOM POSITIONING LOGIC IS UNTOUCHED HERE
        if (spawnOrigin != null)
        {
            spawnRot = spawnOrigin.rotation;
            spawnPos = spawnOrigin.position;

            BoxCollider box = spawnOrigin.GetComponentInChildren<BoxCollider>();
            if (box != null)
            {
                Bounds b = box.bounds;
                spawnPos = new Vector3(
                    Random.Range(b.min.x, b.max.x),
                    Random.Range(b.min.y, b.max.y),
                    Random.Range(b.min.z, b.max.z)
                );
            }

            spawnPos.x += Random.Range(-spawnSpread, spawnSpread);
            spawnPos.y += spawnOffsetY;
        }

        // 3. POOL MANAGER INTEGRATION
        GameObject prefabObj = data.prefab.gameObject;
        GameObject enemyObj = PoolManager.Instance.SpawnFromPool(prefabObj, spawnPos, spawnRot);

        EnemyController controller = enemyObj.GetComponent<EnemyController>();
        if (controller == null)
        {
            controller = enemyObj.AddComponent<EnemyController>();
        }

        Transform target = (player != null) ? player.transform : null;
        controller.Initialize(data, target, this, prefabObj);

        activeEnemyCount++;
    }

    // --- NEW: THE BOSS SPAWNER ---
    private void SpawnBoss()
    {
        if (enemyDatabase == null || player == null) return;

        int currentLevel = GameManager.Instance != null ? GameManager.Instance.currentLevel : 5;

        // Ask the database for a valid Boss!
        EnemyEntry bossData = enemyDatabase.GetBossForLevel(currentLevel);

        if (bossData == null || bossData.prefab == null)
        {
            Debug.LogWarning($"<color=yellow>[EnemyManager]</color> No Boss designed for Level {currentLevel} yet. Skipping!");

            // Tell the GameManager to instantly cancel the boss fight and go to the next level
            if (GameManager.Instance != null) GameManager.Instance.SkipBossPhase();

            return; // Stop the rest of the spawn code!
        }

        // Calculate boss spawn position relative to the player
        Vector3 spawnPos = player.transform.position;
        spawnPos.z += bossSpawnZOffset; // Spawns way behind the player
        spawnPos.y += spawnOffsetY;

        // Bosses use Instantiate instead of the Object Pool
        GameObject bossObj = Instantiate(bossData.prefab.gameObject, spawnPos, Quaternion.identity);

        EnemyController controller = bossObj.GetComponent<EnemyController>();
        if (controller == null)
        {
            controller = bossObj.AddComponent<EnemyController>();
        }

        // Initialize the Boss!
        controller.Initialize(bossData, player.transform, this, bossData.prefab.gameObject);

        Debug.Log($"<color=magenta>[EnemyManager]</color> The {bossData.enemyName} has arrived!");
    }

    public void OnEnemyDied()
    {
        activeEnemyCount--;
        activeEnemyCount = Mathf.Max(0, activeEnemyCount);
    }
}