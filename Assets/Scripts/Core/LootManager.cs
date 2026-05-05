using UnityEngine;
using System.Collections.Generic;
using Player.Control; // Needed to talk to the PlayerController!

public class LootManager : MonoBehaviour
{
    [Header("Drop Settings")]
    [Tooltip("X = Minimum time, Y = Maximum time between drops")]
    public Vector2 dropIntervalRange = new Vector2(6f, 14f);
    public float[] lanePositions = new float[] { -2f, 0f, 2f };

    [Header("Movement Overrides")]
    public LootItem.MoveDirection itemDirection = LootItem.MoveDirection.Forward;
    public float itemDespawnThreshold = 20f;

    [Header("Loot Pools")]
    [Tooltip("Standard consumables: Health and Armor.")]
    public List<GameObject> itemLootPool;

    [Tooltip("Ammo prefabs. Will ONLY drop if the player owns this weapon type.")]
    public List<GameObject> ammoLootPool; // NEW: The dedicated Ammo Pool!

    [Tooltip("Rare drops: Specific Weapon Prefabs.")]
    public List<GameObject> weaponLootPool;

    [Header("Drop Chances")]
    [Range(0f, 100f)]
    public float weaponDropChance = 15f;

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
        PickNewDropInterval();
    }

    private void Update()
    {
        if (isGameOver) return;

        timer += Time.deltaTime;

        if (timer >= currentTargetInterval)
        {
            timer = 0f;
            DropSupplyCrate();
            PickNewDropInterval();
        }
    }

    private void PickNewDropInterval()
    {
        currentTargetInterval = Random.Range(dropIntervalRange.x, dropIntervalRange.y);
    }

    private void DropSupplyCrate()
    {
        // 1. Check if we have any prefabs loaded
        bool hasItems = itemLootPool != null && itemLootPool.Count > 0;
        bool hasWeapons = weaponLootPool != null && weaponLootPool.Count > 0;
        bool hasAmmo = ammoLootPool != null && ammoLootPool.Count > 0;

        if (!hasItems && !hasWeapons && !hasAmmo) return;

        // 2. Create an empty temporary list to hold whatever is legally allowed to drop this frame
        List<GameObject> dynamicDropPool = new List<GameObject>();
        float roll = Random.Range(0f, 100f);

        // 3. Did we roll a weapon?
        if (roll <= weaponDropChance && hasWeapons)
        {
            dynamicDropPool.AddRange(weaponLootPool);
        }
        else
        {
            // 4. We rolled a standard item! 
            // Always add Health/Armor to the legal drop list
            if (hasItems) dynamicDropPool.AddRange(itemLootPool);

            // 5. SMART LOOT AMMO CHECK
            if (hasAmmo && PlayerController.Instance != null)
            {
                // Ask the player what guns they have
                List<WeaponCategory> ownedCategories = PlayerController.Instance.GetOwnedWeaponCategories();

                // Loop through all our ammo prefabs
                foreach (GameObject ammoPrefab in ammoLootPool)
                {
                    LootItem lootScript = ammoPrefab.GetComponent<LootItem>();

                    // If the player owns the category for this ammo, add it to the legal drop list!
                    if (lootScript != null && ownedCategories.Contains(lootScript.ammoCategory))
                    {
                        dynamicDropPool.Add(ammoPrefab);
                    }
                }
            }
        }

        // 6. Safety Net: If the dynamic pool is somehow completely empty, force a weapon drop
        if (dynamicDropPool.Count == 0)
        {
            if (hasWeapons) dynamicDropPool.AddRange(weaponLootPool);
            else return;
        }

        // 7. Pick a random prefab from our legally approved list!
        GameObject prefabToDrop = dynamicDropPool[Random.Range(0, dynamicDropPool.Count)];

        // 8. Spawn it
        float randomLaneX = lanePositions[Random.Range(0, lanePositions.Length)];
        Vector3 dropPos = new Vector3(randomLaneX, 1f, transform.position.z);

        GameObject droppedItem = PoolManager.Instance.SpawnFromPool(prefabToDrop, dropPos, Quaternion.identity);

        LootItem droppedScript = droppedItem.GetComponent<LootItem>();
        if (droppedScript != null)
        {
            droppedScript.Initialize(prefabToDrop, itemDirection, itemDespawnThreshold);
        }
    }
}