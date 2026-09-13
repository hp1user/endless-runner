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
    [Tooltip("Levels where boss battles occur (e.g. 5, 15, 20, 25, 40).")]
    public System.Collections.Generic.List<int> bossLevels = new System.Collections.Generic.List<int> { 5, 15, 20, 25, 40 };
    public bool isBossFightActive = false;

    public bool IsBossLevel(int level)
    {
        return bossLevels != null && bossLevels.Contains(level);
    }

    [Header("Boss Checkpoint Testing")]
    [Tooltip("Target level for the custom checkpoint jump.")]
    public int checkpointTestLevel = 5;
    [Tooltip("If true, automatically jumps to the boss checkpoint when Play mode begins.")]
    public bool autoJumpToCheckpointOnStart = false;

    // --- GLOBAL EVENTS (The GameManager shouting to the world) ---
    public static event Action<int> OnLevelCompleted; // Tells LevelManager to swap environments
    public static event Action OnBossTransitionStarted; // Tells LevelManager & systems to transition from City to Bridge!
    public static event Action OnBossFightStarted;    // Tells the Camera to flip 180 degrees when on the bridge!
    public static event Action OnBossDefeated;        // Tells the Camera to flip back
    public static event Action OnEnemyKilled;         // Tells listeners (like Ultimate skill) that an enemy died

    public bool isBossTransitionActive { get; private set; } = false;

    private void Awake()
    {
        // Standard Singleton Setup
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (autoJumpToCheckpointOnStart)
        {
            StartCoroutine(DelayedCheckpointJump(checkpointTestLevel));
        }
    }

    private System.Collections.IEnumerator DelayedCheckpointJump(int level)
    {
        yield return null; // wait 1 frame for Awake/Start on all managers
        BossCheckpointSystem.JumpToBossCheckpoint(level);
    }

    // --- CONTEXT MENU CHECKPOINT JUMPS (Right-click in Inspector during Play Mode) ---
    [ContextMenu("Jump to Boss (Level 5)")]
    public void ContextMenuJumpBossLevel5()
    {
        BossCheckpointSystem.JumpToBossCheckpoint(5);
    }

    [ContextMenu("Jump to Boss (Level 15)")]
    public void ContextMenuJumpBossLevel15()
    {
        BossCheckpointSystem.JumpToBossCheckpoint(15);
    }

    [ContextMenu("Jump to Configured Level Checkpoint")]
    public void ContextMenuJumpCustomLevel()
    {
        BossCheckpointSystem.JumpToBossCheckpoint(checkpointTestLevel);
    }

    // Enemies will call this method right before they die
    public void RegisterEnemyKill()
    {
        // Don't count normal kills if we are currently fighting a boss or transitioning
        if (isBossFightActive || isBossTransitionActive || PlayerController.Instance.isDead) return;

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
        return baseEnemiesPerWave + ((currentLevel - 1) * additionalEnemiesPerWave);
    }

    private void TriggerNextPhase()
    {
        enemiesKilledThisLevel = 0; // Reset the counter

        if (IsBossLevel(currentLevel))
        {
            StartBossTransition();
        }
        else
        {
            currentLevel++;
            OnLevelCompleted?.Invoke(currentLevel);
            Debug.Log($"<color=cyan>[GameManager]</color> LEVEL UP! Welcome to Level {currentLevel}");
        }
    }

    public void StartBossTransition()
    {
        if (isBossFightActive || isBossTransitionActive) return;

        isBossTransitionActive = true;
        OnBossTransitionStarted?.Invoke();
        Debug.Log("<color=yellow>[GameManager]</color> APPROACHING BOSS! Transitioning from City to Bridge...");
    }

    // Called by LevelManager when the player physically enters the Bridge chunk
    public void OnBridgeReached()
    {
        isBossTransitionActive = false;
        StartBossFight();
    }

    public void StartBossFight()
    {
        // If the player is not yet on the bridge and not currently in transition, initiate transition first
        if (LevelManager.Instance != null && !LevelManager.Instance.isPlayerOnBridge && !isBossTransitionActive)
        {
            StartBossTransition();
            return;
        }

        isBossTransitionActive = false;
        isBossFightActive = true;
        OnBossFightStarted?.Invoke();
        Debug.Log($"<color=red>[GameManager] WARNING:</color> ON THE BRIDGE! BOSS FIGHT INITIATED!");
    }

    // The Boss will call this method when its health hits 0
    public void BossDefeated()
    {
        isBossTransitionActive = false;
        isBossFightActive = false;
        currentLevel++;

        OnBossDefeated?.Invoke();
        OnLevelCompleted?.Invoke(currentLevel); // Trigger the environment swap!

        Debug.Log($"<color=orange>[GameManager]</color> BOSS DEFEATED! Advancing to Level {currentLevel}");
    }

    public void SkipBossPhase()
    {
        isBossTransitionActive = false;
        isBossFightActive = false;
        currentLevel++; // Jump to next level

        OnBossDefeated?.Invoke(); // Ensures cameras & environment return to normal front view!
        OnLevelCompleted?.Invoke(currentLevel); // Tell the environment to swap

        Debug.Log($"<color=cyan>[GameManager]</color> No Boss found for phase. Skipping! Advancing to Level {currentLevel}");
    }
}