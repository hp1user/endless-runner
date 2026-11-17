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
    [Tooltip("Percentage damage reduction (e.g. 40% for Bosses, 20% for Elites). 0 = full damage taken.")]
    [Range(0f, 100f)]
    public float damageReduction = 0f;

    [Header("VFX Settings")]
    [Tooltip("Primary VFX prefab spawned when this enemy dies (Death Splash).")]
    public GameObject deathVFX;
    [Tooltip("Optional secondary death VFX prefab (e.g. random splash variant).")]
    public GameObject deathVFX_Variant;
    [Tooltip("VFX prefab spawned when this enemy takes hit damage.")]
    public GameObject hitVFX;
    [Tooltip("Lifetime of the spawned death VFX before returning to pool.")]
    public float deathVFXLifetime = 2.0f;
    [Tooltip("Lifetime of the spawned hit VFX before returning to pool.")]
    public float hitVFXLifetime = 1.0f;

    [Header("Death Settings")]
    public float deathDuration = 3.0f;
    public float groundYPosition = 0f;

    [Header("Boss Minions")]
    public bool canSpawnMinions = false;
    public float minionSpawnInterval = 5f;
    public List<EnemyEntry> minionTypes = new List<EnemyEntry>();

    [Header("Boss Spawn Cinematic FX")]
    [Tooltip("Roar audio played when the boss arrives on the bridge.")]
    public AudioClip bossSpawnRoarClip;
    [Tooltip("Animator trigger or state name played when the boss arrives (e.g. 'Roar').")]
    public string bossRoarAnimationTrigger = "Roar";
    [Tooltip("How long the roar intro lasts in seconds before resuming chase locomotion.")]
    public float bossRoarAnimationDuration = 1.5f;
    [Tooltip("Whether the boss pauses movement while roaring upon entrance.")]
    public bool pauseMovementDuringRoar = true;
    [Tooltip("Whether to trigger a camera shake when the boss lands/arrives.")]
    public bool enableSpawnCameraShake = true;
    [Tooltip("Intensity of the camera shake upon boss arrival.")]
    public float spawnCameraShakeIntensity = 0.6f;
    [Tooltip("Duration of the camera shake in seconds.")]
    public float spawnCameraShakeDuration = 0.8f;
    [Tooltip("Whether to trigger haptic vibration when the boss arrives.")]
    public bool enableSpawnHaptics = true;
    [Tooltip("Whether to trigger the roar sequence (audio, animation, shake) when entering Phase 3 (both legs broken).")]
    public bool playRoarOnPhase3 = true;

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
        copy.damageReduction = this.damageReduction;
        copy.deathVFX = this.deathVFX;
        copy.deathVFX_Variant = this.deathVFX_Variant;
        copy.hitVFX = this.hitVFX;
        copy.deathVFXLifetime = this.deathVFXLifetime;
        copy.hitVFXLifetime = this.hitVFXLifetime;
        copy.deathDuration = this.deathDuration;
        copy.groundYPosition = this.groundYPosition;
        copy.canSpawnMinions = false;
        copy.minionSpawnInterval = this.minionSpawnInterval;
        copy.minionTypes = new List<EnemyEntry>();
        copy.bossSpawnRoarClip = this.bossSpawnRoarClip;
        copy.bossRoarAnimationTrigger = this.bossRoarAnimationTrigger;
        copy.bossRoarAnimationDuration = this.bossRoarAnimationDuration;
        copy.pauseMovementDuringRoar = this.pauseMovementDuringRoar;
        copy.enableSpawnCameraShake = this.enableSpawnCameraShake;
        copy.spawnCameraShakeIntensity = this.spawnCameraShakeIntensity;
        copy.spawnCameraShakeDuration = this.spawnCameraShakeDuration;
        copy.enableSpawnHaptics = this.enableSpawnHaptics;
        copy.playRoarOnPhase3 = this.playRoarOnPhase3;
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
