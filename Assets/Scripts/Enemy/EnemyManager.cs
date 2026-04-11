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

    private int activeEnemyCount = 0;
    private float timer;

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
        timer += Time.deltaTime;

        if (timer >= spawnInterval)
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

        // 2. Randomly select enemy and spawn location
        EnemyEntry data = enemyDatabase.GetRandomEnemy();
        if (data == null || data.prefab == null) return;

        Transform spawnOrigin = null;
        if (spawnPoints.Count > 0)
        {
            spawnOrigin = spawnPoints[Random.Range(0, spawnPoints.Count)];
        }

        Vector3 spawnPos = Vector3.zero;
        Quaternion spawnRot = Quaternion.identity;

        if (spawnOrigin != null)
        {
            spawnRot = spawnOrigin.rotation;
            
            // 1. Initial Position (Center of marker)
            spawnPos = spawnOrigin.position;

            // 2. Add Box Logic if available
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

            // 3. APPLY 2D SPREAD (X = Lane Width, Z = Fixed)
            // We randomize the lane position, but keep height consistent with the marker.
            spawnPos.x += Random.Range(-spawnSpread, spawnSpread);

            // 4. APPLY BASE HEIGHT OFFSET (If you want a fixed boost to the starting Y)
            spawnPos.y += spawnOffsetY;
        }

        // 3. Instantiate and setup
        GameObject enemyObj = Instantiate(data.prefab.gameObject, spawnPos, spawnRot);
        
        EnemyController controller = enemyObj.GetComponent<EnemyController>();
        if (controller == null)
        {
            controller = enemyObj.AddComponent<EnemyController>();
        }
        
        // Final link to the player
        Transform target = (player != null) ? player.transform : null;
        controller.Initialize(data, target);
        
        activeEnemyCount++;
    }

    public void OnEnemyDied()
    {
        activeEnemyCount--;
    }
}
