using UnityEngine;
using System.Collections.Generic;

public class LootManager : MonoBehaviour
{
    [Header("Drop Settings")]
    [Tooltip("X = Minimum time, Y = Maximum time between drops")]
    public Vector2 dropIntervalRange = new Vector2(6f, 14f); // NEW: Vector2 for randomness!
    public float[] lanePositions = new float[] { -2f, 0f, 2f };

    [Header("Movement Overrides")]
    [Tooltip("Which way should the dropped items travel?")]
    public LootItem.MoveDirection itemDirection = LootItem.MoveDirection.Forward;
    [Tooltip("How far past 0 should the item travel before despawning?")]
    public float itemDespawnThreshold = 20f;

    [Header("Loot Pool")]
    public List<GameObject> possibleLootPrefabs;

    private float timer;
    private float currentTargetInterval; // NEW: Holds the current random target time

    private void Start()
    {
        // Pick the very first random drop time when the game starts
        PickNewDropInterval();
    }

    private void Update()
    {
        if (possibleLootPrefabs == null || possibleLootPrefabs.Count == 0) return;

        timer += Time.deltaTime;

        // Check against our randomized target interval
        if (timer >= currentTargetInterval)
        {
            timer = 0f;
            DropSupplyCrate();

            // Pick a brand new random time for the next drop!
            PickNewDropInterval();
        }
    }

    private void PickNewDropInterval()
    {
        // Random.Range with floats gives you any decimal between X and Y
        currentTargetInterval = Random.Range(dropIntervalRange.x, dropIntervalRange.y);
    }

    private void DropSupplyCrate()
    {
        float randomLaneX = lanePositions[Random.Range(0, lanePositions.Length)];
        Vector3 dropPos = new Vector3(randomLaneX, 1f, transform.position.z);

        GameObject prefabToDrop = possibleLootPrefabs[Random.Range(0, possibleLootPrefabs.Count)];
        GameObject droppedItem = PoolManager.Instance.SpawnFromPool(prefabToDrop, dropPos, Quaternion.identity);

        LootItem lootScript = droppedItem.GetComponent<LootItem>();
        if (lootScript != null)
        {
            lootScript.Initialize(prefabToDrop, itemDirection, itemDespawnThreshold);
        }
    }
}