using System.Collections.Generic;
using UnityEngine;
using Player.Control;

/// <summary>
/// Handles quick testing and checkpoints for Boss Battles.
/// Allows jumping directly to Level 5, Level 10, or custom boss levels,
/// automatically granting random upgrade cards, unlocking weapons with ammo,
/// clearing regular wave enemies, and initiating the boss encounter.
/// </summary>
public static class BossCheckpointSystem
{
    public static void JumpToBossCheckpoint(int targetLevel)
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("<color=yellow>[Checkpoint]</color> Boss Checkpoint can only be triggered while in Play Mode!");
            return;
        }

        if (GameManager.Instance == null)
        {
            Debug.LogError("<color=red>[Checkpoint]</color> Cannot jump: GameManager.Instance is null!");
            return;
        }

        if (PlayerController.Instance == null)
        {
            Debug.LogError("<color=red>[Checkpoint]</color> Cannot jump: PlayerController.Instance is null!");
            return;
        }

        Debug.Log($"<color=cyan><b>[Boss Checkpoint]</b></color> Preparing jump to <b>Level {targetLevel} Boss Battle</b>...");

        // 1. Set Level & reset wave counters
        GameManager.Instance.currentLevel = targetLevel;
        GameManager.Instance.enemiesKilledThisLevel = 0;

        // 2. Clear any active regular wave enemies from the field
        EnemyManager enemyManager = Object.FindFirstObjectByType<EnemyManager>();
        if (enemyManager != null)
        {
            enemyManager.ClearAllActiveEnemies();
        }

        // 3. Grant random upgrade cards matching the level progression (e.g. 5 cards for Lvl 5, 10 for Lvl 10)
        int cardCount = Mathf.Max(1, targetLevel);
        if (UpgradeManager.Instance != null)
        {
            List<UpgradeCard> awarded = UpgradeManager.Instance.ApplyRandomUpgrades(cardCount);
            Debug.Log($"<color=green>[Checkpoint]</color> Awarded {awarded.Count} random upgrade cards for Level {targetLevel}.");
        }

        // 4. Unlock weapons and stock ammo appropriate for this level
        UnlockWeaponsForLevel(targetLevel);

        // 5. Trigger the Boss Fight!
        GameManager.Instance.StartBossFight();

        Debug.Log($"<color=orange><b>[Boss Checkpoint]</b></color> <b>READY!</b> Level {targetLevel} Boss Fight initiated with full gear!");
    }

    private static void UnlockWeaponsForLevel(int level)
    {
        if (PlayerController.Instance == null) return;

        // Level 5 tier: Shotgun + SMG
        if (level >= 5)
        {
            UnlockGunSafely("shotgun1", WeaponCategory.Shotgun, 40);
            UnlockGunSafely("smg1", WeaponCategory.SMG, 120);
        }

        // Level 10 tier: Assault Rifle (M4) + LMG (machineGun) + Sniper (AWM)
        if (level >= 10)
        {
            UnlockGunSafely("M4", WeaponCategory.AssaultRifle, 150);
            UnlockGunSafely("machineGun", WeaponCategory.AssaultRifle, 200);
            UnlockGunSafely("AWM", WeaponCategory.Sniper, 30);
        }

        // Fallback for custom levels below 5: at least give SMG
        if (level < 5)
        {
            UnlockGunSafely("smg1", WeaponCategory.SMG, 100);
        }
    }

    private static void UnlockGunSafely(string weaponID, WeaponCategory category, int bonusAmmo)
    {
        if (WeaponDatabase.Instance == null) return;

        WeaponData gun = WeaponDatabase.Instance.GetWeaponByID(weaponID);
        if (gun != null)
        {
            PlayerController.Instance.UnlockWeapon(weaponID);
            PlayerController.Instance.AddAmmo(category, bonusAmmo);
            Debug.Log($"<color=yellow>[Checkpoint]</color> Unlocked <b>{gun.weaponName}</b> with +{bonusAmmo} {category} ammo!");
        }
    }
}
