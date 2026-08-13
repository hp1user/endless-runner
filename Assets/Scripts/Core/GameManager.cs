using UnityEngine;
using System;
using Player.Control;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Progression Settings")]
    public int currentLevel = 1;
    public int enemiesKilledThisLevel = 0;
    [Tooltip("How many enemies must die on Wave 1 to advance?")]
    public int baseEnemiesPerWave = 10;
    [Tooltip("How many extra enemies are added per Wave Phase?")]
    public int additionalEnemiesPerWave = 2;
    [Header("Speed Tracking")]
    public float levelClearTimer = 0f;

    [Header("Boss Settings")]
    [Tooltip("A boss spawns every X levels.")]
    public int levelsBetweenBosses = 5;
    public bool isBossFightActive = false;

    // --- GLOBAL EVENTS (The GameManager shouting to the world) ---
    public static event Action<int> OnLevelCompleted; // Tells LevelManager to swap environments
    public static event Action OnBossFightStarted;    // Tells the Camera to flip 180 degrees!
    public static event Action OnBossDefeated;        // Tells the Camera to flip back
    public static event Action OnEnemyKilled;         // Tells listeners (like Ultimate skill) that an enemy died

    private void Awake()
    {
        // Standard Singleton Setup
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // Enemies will call this method right before they die
    public void RegisterEnemyKill()
    {
        // Don't count normal kills if we are currently fighting a boss
        if (isBossFightActive || PlayerController.Instance.isDead) return;

        enemiesKilledThisLevel++;
        OnEnemyKilled?.Invoke(); // Announce that an enemy was killed

        // Calculate the quota for this specific level
        int requiredKills = GetRequiredKillsForCurrentLevel();

        if (enemiesKilledThisLevel >= requiredKills)
        {
            TriggerNextPhase();
        }
    }

    public int GetRequiredKillsForCurrentLevel()
    {
        int wavePhase = (currentLevel - 1) / levelsBetweenBosses;
        return baseEnemiesPerWave + (wavePhase * additionalEnemiesPerWave);
    }

    private void TriggerNextPhase()
    {
        enemiesKilledThisLevel = 0; // Reset the counter

        // Is the NEXT level a Boss Level? (e.g., Level 5, 10, 15...)
        if (currentLevel % levelsBetweenBosses == 0)
        {
            StartBossFight();
        }
        else
        {
            currentLevel++;
            OnLevelCompleted?.Invoke(currentLevel);
            Debug.Log($"<color=cyan>[GameManager]</color> LEVEL UP! Welcome to Level {currentLevel}");
        }
    }

    private void StartBossFight()
    {
        isBossFightActive = true;
        OnBossFightStarted?.Invoke();
        Debug.Log($"<color=red>[GameManager] WARNING:</color> BOSS FIGHT INITIATED!");

        // TODO: Tell EnemyManager to spawn the Boss Prefab!
    }

    // The Boss will call this method when its health hits 0
    public void BossDefeated()
    {
        isBossFightActive = false;
        currentLevel++;

        OnBossDefeated?.Invoke();
        OnLevelCompleted?.Invoke(currentLevel); // Trigger the environment swap!

        Debug.Log($"<color=orange>[GameManager]</color> BOSS DEFEATED! Advancing to Level {currentLevel}");
    }

    public void SkipBossPhase()
    {
        isBossFightActive = false;
        currentLevel++; // Jump to level 6!
        OnLevelCompleted?.Invoke(currentLevel); // Tell the environment to swap

        Debug.Log($"<color=cyan>[GameManager]</color> No Boss found. Skipping phase! Advancing to Level {currentLevel}");
    }
}