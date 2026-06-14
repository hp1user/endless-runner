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

    [Tooltip("If this is a Boss, EXACTLY what level does it spawn on?")]
    public int bossTargetLevel = 5;

    [Header("Stats")]
    public float maxHealth = 30f;
    public float moveSpeed = 5f;
    public float damage = 10f;
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

    // 2. Gets a BOSS valid for the current level
    public EnemyEntry GetBossForLevel(int currentLevel)
    {
        if (enemyTypes == null || enemyTypes.Count == 0) return null;

        List<EnemyEntry> validBosses = new List<EnemyEntry>();

        foreach (EnemyEntry enemy in enemyTypes)
        {
            // NEW: Check if it's a boss AND its target level exactly matches the current level!
            if (enemy.category == EnemyCategory.Boss && enemy.bossTargetLevel == currentLevel)
            {
                validBosses.Add(enemy);
            }
        }

        if (validBosses.Count == 0) return null;

        return validBosses[Random.Range(0, validBosses.Count)];
    }
}
