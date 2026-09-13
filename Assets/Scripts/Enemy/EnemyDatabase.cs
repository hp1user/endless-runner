using UnityEngine;
using System.Collections.Generic;

public enum EnemyCategory { Standard, Elite, Boss }

[System.Serializable]
public class EnemyEntry
{
    public string enemyName = "Basic Enemy";

    [Header("Visuals")]
    [Tooltip("The model/prefab for this enemy type.")]
    public Transform prefab; // Kept exactly as you had it!

    [Header("Spawn Rules")]
    public EnemyCategory category = EnemyCategory.Standard;
    [Tooltip("If true, this enemy will spawn on the ground (ignoring Y offsets).")]
    public bool isGroundEnemy = false;
    [Tooltip("The earliest level this enemy can start spawning.")]
    public int minSpawnLevel = 1;
    [Tooltip("The last level this enemy can spawn (Use 999 for infinite).")]
    public int maxSpawnLevel = 999;

    [Header("Chase Settings")]
    [Tooltip("If true, the enemy will always chase the player across lanes.")]
    public bool alwaysChasePlayer = true;
    [Tooltip("If alwaysChasePlayer is false, this is the percentage chance (0 to 100) that the enemy will chase the player anyway.")]
    [Range(0f, 100f)]
    public float chaseChance = 80f;

    [Tooltip("If this is a Boss, EXACTLY what level does it spawn on?")]
    public int bossTargetLevel = 5;

    [Header("Stats")]
    public float maxHealth = 30f;
    public float moveSpeed = 5f;
    public float damage = 10f;

    [Header("Death Settings")]
    public float deathDuration = 3.0f;
    public float groundYPosition = 0f;

    [Header("Boss Minions")]
    public bool canSpawnMinions = false;
    public float minionSpawnInterval = 5f;
    public List<EnemyEntry> minionTypes = new List<EnemyEntry>();

    public EnemyEntry Clone()
    {
        EnemyEntry copy = new EnemyEntry();
        copy.enemyName = this.enemyName;
        copy.prefab = this.prefab;
        copy.category = this.category;
        copy.isGroundEnemy = this.isGroundEnemy;
        copy.minSpawnLevel = this.minSpawnLevel;
        copy.maxSpawnLevel = this.maxSpawnLevel;
        copy.alwaysChasePlayer = this.alwaysChasePlayer;
        copy.chaseChance = this.chaseChance;
        copy.bossTargetLevel = this.bossTargetLevel;
        copy.maxHealth = this.maxHealth;
        copy.moveSpeed = this.moveSpeed;
        copy.damage = this.damage;
        copy.deathDuration = this.deathDuration;
        copy.groundYPosition = this.groundYPosition;
        copy.canSpawnMinions = false;
        copy.minionSpawnInterval = this.minionSpawnInterval;
        copy.minionTypes = new List<EnemyEntry>();
        return copy;
    }
}

[CreateAssetMenu(fileName = "EnemyDatabase", menuName = "Enemy/Enemy Database")]
public class EnemyDatabase : ScriptableObject
{
    public List<EnemyEntry> enemyTypes = new List<EnemyEntry>();

    // Hardcoded Layer Settings
    public LayerMask EnemyLayer => LayerMask.GetMask("Enemy");
    public LayerMask PlayerLayer => LayerMask.GetMask("Player");

    // 1. Gets a random NORMAL enemy valid for the current level
    public EnemyEntry GetRandomEnemyForLevel(int currentLevel)
    {
        if (enemyTypes == null || enemyTypes.Count == 0) return null;

        List<EnemyEntry> validEnemies = new List<EnemyEntry>();

        foreach (EnemyEntry enemy in enemyTypes)
        {
            // Must NOT be a boss, and must be within the level range
            if (enemy.category != EnemyCategory.Boss &&
                currentLevel >= enemy.minSpawnLevel &&
                currentLevel <= enemy.maxSpawnLevel)
            {
                validEnemies.Add(enemy);
            }
        }

        if (validEnemies.Count == 0) return null; // Safety net

        return validEnemies[Random.Range(0, validEnemies.Count)];
    }

    // 2. Gets a BOSS valid for the current level (with fallback to available bosses)
    public EnemyEntry GetBossForLevel(int currentLevel)
    {
        if (enemyTypes == null || enemyTypes.Count == 0) return null;

        List<EnemyEntry> exactMatches = new List<EnemyEntry>();
        List<EnemyEntry> allBosses = new List<EnemyEntry>();

        foreach (EnemyEntry enemy in enemyTypes)
        {
            if (enemy.category == EnemyCategory.Boss)
            {
                allBosses.Add(enemy);
                if (enemy.bossTargetLevel == currentLevel)
                {
                    exactMatches.Add(enemy);
                }
            }
        }

        if (exactMatches.Count > 0)
        {
            return exactMatches[Random.Range(0, exactMatches.Count)];
        }

        // Fallback: If no boss is specifically assigned to this level, return an available boss
        if (allBosses.Count > 0)
        {
            Debug.Log($"<color=yellow>[EnemyDatabase]</color> No boss specifically configured for Level {currentLevel}. Using available boss: {allBosses[0].enemyName}");
            return allBosses[Random.Range(0, allBosses.Count)];
        }

        return null;
    }
}
