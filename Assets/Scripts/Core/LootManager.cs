using UnityEngine;
using System.Collections.Generic;
using Player.Control; // Needed to talk to the PlayerController!

public class LootManager : MonoBehaviour
{
    [Header("Drop Settings")]
    [Tooltip("X = Minimum time, Y = Maximum time between drops")]
    public Vector2 dropIntervalRange = new Vector2(6f, 14f);
    public float[] lanePositions = new float[] { -2f, 0f, 2f };
    public float spawnYPosition = 0f;

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
    public float weaponDropChance = 59.4f;

    [Header("Weapon Drop Weights")]
    [Tooltip("Drop weights for weapon archetypes (higher = more frequent)")]
    public float commonWeaponWeight = 50f;   // AR, SMG (High frequency)
    public float shotgunWeight = 8f;         // Shotgun (Rare)
    public float sniperWeight = 4f;          // Sniper (Very Rare)
    public float minigunWeight = 2f;         // Minigun (Ultra Rare)
    public float heavyLauncherWeight = 2f;   // RPG, Grenade

    [Header("Item & Ammo Drop Weights")]
    [Tooltip("Drop weights for consumable items & ammo")]
    public float consumableItemWeight = 30f; // Health, Armor
    public float standardAmmoWeight = 30f;   // AR, SMG
    public float shotgunAmmoWeight = 10f;    // Shotgun ammo
    public float sniperAmmoWeight = 6f;      // Sniper ammo
    public float minigunAmmoWeight = 5f;     // Minigun ammo
    public float heavyAmmoWeight = 5f;       // RPG, Grenade ammo

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

    private float GetPrefabWeight(GameObject prefab, bool isWeapon)
    {
        if (prefab == null) return 1f;

        LootItem loot = prefab.GetComponent<LootItem>();
        if (loot == null) loot = prefab.GetComponentInChildren<LootItem>();

        if (loot == null) return isWeapon ? commonWeaponWeight : consumableItemWeight;

        if (isWeapon || loot.type == LootItem.LootType.Weapon)
        {
            switch (loot.ammoCategory)
            {
                case WeaponCategory.Shotgun:
                    return shotgunWeight;
                case WeaponCategory.Sniper:
                    return sniperWeight;
                case WeaponCategory.Minigun:
                    return minigunWeight;
                case WeaponCategory.RocketLauncher:
                    return heavyLauncherWeight;
                case WeaponCategory.AssaultRifle:
                case WeaponCategory.SMG:
                default:
                    return commonWeaponWeight;
            }
        }
        else if (loot.type == LootItem.LootType.Ammo)
        {
            switch (loot.ammoCategory)
            {
                case WeaponCategory.Shotgun:
                    return shotgunAmmoWeight;
                case WeaponCategory.Sniper:
                    return sniperAmmoWeight;
                case WeaponCategory.Minigun:
                    return minigunAmmoWeight;
                case WeaponCategory.RocketLauncher:
                    return heavyAmmoWeight;
                case WeaponCategory.AssaultRifle:
                case WeaponCategory.SMG:
                default:
                    return standardAmmoWeight;
            }
        }

        return consumableItemWeight;
    }

    private GameObject PickWeightedPrefab(List<GameObject> pool, bool isWeapon)
    {
        if (pool == null || pool.Count == 0) return null;
        if (pool.Count == 1) return pool[0];

        float totalWeight = 0f;
        for (int i = 0; i < pool.Count; i++)
        {
            totalWeight += Mathf.Max(0.001f, GetPrefabWeight(pool[i], isWeapon));
        }

        float roll = Random.Range(0f, totalWeight);
        float cumulative = 0f;
        for (int i = 0; i < pool.Count; i++)
        {
            cumulative += Mathf.Max(0.001f, GetPrefabWeight(pool[i], isWeapon));
            if (roll <= cumulative)
            {
                return pool[i];
            }
        }

        return pool[pool.Count - 1];
    }

    private void DropSupplyCrate()
    {
        // 1. Check if we have any prefabs loaded
        bool hasItems = itemLootPool != null && itemLootPool.Count > 0;
        bool hasWeapons = weaponLootPool != null && weaponLootPool.Count > 0;
        bool hasAmmo = ammoLootPool != null && ammoLootPool.Count > 0;

        if (!hasItems && !hasWeapons && !hasAmmo) return;

        GameObject prefabToDrop = null;
        float roll = Random.Range(0f, 100f);

        // 2. Did we roll a weapon?
        if (roll <= weaponDropChance && hasWeapons)
        {
            prefabToDrop = PickWeightedPrefab(weaponLootPool, isWeapon: true);
        }
        else
        {
            // 3. We rolled a consumable item or ammo!
            List<GameObject> dynamicItemPool = new List<GameObject>();

            // Always add Health/Armor to the legal drop list
            if (hasItems) dynamicItemPool.AddRange(itemLootPool);

            // SMART LOOT AMMO CHECK
            if (hasAmmo && PlayerController.Instance != null)
            {
                // Ask the player what guns they have
                List<WeaponCategory> ownedCategories = PlayerController.Instance.GetOwnedWeaponCategories();

                // Loop through all our ammo prefabs
                foreach (GameObject ammoPrefab in ammoLootPool)
                {
                    if (ammoPrefab == null) continue;
                    LootItem lootScript = ammoPrefab.GetComponent<LootItem>();
                    if (lootScript == null) lootScript = ammoPrefab.GetComponentInChildren<LootItem>();

                    // Skip pistol ammo until pistol variants are added
                    if (lootScript != null && lootScript.ammoCategory == WeaponCategory.Pistol) continue;

                    // If the player owns the category for this ammo, add it to the legal drop list!
                    if (lootScript != null && ownedCategories.Contains(lootScript.ammoCategory))
                    {
                        dynamicItemPool.Add(ammoPrefab);
                    }
                }
            }

            if (dynamicItemPool.Count > 0)
            {
                prefabToDrop = PickWeightedPrefab(dynamicItemPool, isWeapon: false);
            }
            else if (hasWeapons)
            {
                // Safety net fallback to weapon
                prefabToDrop = PickWeightedPrefab(weaponLootPool, isWeapon: true);
            }
            else if (hasItems)
            {
                prefabToDrop = itemLootPool[Random.Range(0, itemLootPool.Count)];
            }
        }

        if (prefabToDrop == null) return;

        // 4. Find a safe lane to spawn in
        List<int> availableLanes = new List<int>();
        for (int i = 0; i < lanePositions.Length; i++)
        {
            if (SpawnTracker.IsLaneSafe(i, 2.0f)) // 2 seconds cooldown
            {
                availableLanes.Add(i);
            }
        }

        // If no lanes are safe, skip spawning this frame
        if (availableLanes.Count == 0) return;

        int chosenLaneIndex = availableLanes[Random.Range(0, availableLanes.Count)];
        SpawnTracker.RegisterSpawn(chosenLaneIndex);
        float chosenLaneX = lanePositions[chosenLaneIndex];

        Vector3 dropPos = new Vector3(chosenLaneX, spawnYPosition, transform.position.z);

        GameObject droppedItem = PoolManager.Instance.SpawnFromPool(prefabToDrop, dropPos, Quaternion.identity);

        LootItem droppedScript = droppedItem.GetComponent<LootItem>();
        if (droppedScript == null) droppedScript = droppedItem.GetComponentInChildren<LootItem>();
        if (droppedScript != null)
        {
            droppedScript.Initialize(prefabToDrop, itemDirection, itemDespawnThreshold);
        }
    }
}