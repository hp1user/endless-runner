using UnityEngine;
using System.Collections.Generic;

public enum WeaponFireMode { Single, Burst, Auto }
public enum WeaponCategory { Pistol, SMG, Shotgun, AssaultRifle, Sniper, RocketLauncher, Minigun, Grenade }

[CreateAssetMenu(fileName = "WeaponDatabase", menuName = "Player/Weapon Database")]
public class WeaponDatabase : ScriptableObject
{
    private static WeaponDatabase _instance;
    public static WeaponDatabase Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Resources.Load<WeaponDatabase>("WeaponDatabase");
                if (_instance == null)
                {
                    Debug.LogError("WeaponDatabase: Could not find 'WeaponDatabase' in any Resources folder! Please make sure the asset is named WeaponDatabase and is inside a Resources folder.");
                }
            }
            return _instance;
        }
    }

    public List<WeaponData> weaponEntries = new List<WeaponData>();

    public WeaponData GetWeaponByID(string searchID)
    {
        foreach (WeaponData weapon in weaponEntries)
        {
            if (weapon.weaponID == searchID)
            {
                return weapon;
            }
        }
        Debug.LogWarning($"WeaponDatabase: Could not find weapon with ID: {searchID}");
        return null;
    }

    public WeaponData GetEntryByLayer(int layer)
    {
        if (weaponEntries == null) return null;
        return weaponEntries.Find(e => e.animatorLayer == layer);
    }

    public WeaponData GetWeaponByCategory(WeaponCategory searchCategory)
    {
        foreach (WeaponData weapon in weaponEntries)
        {
            if (weapon.category == searchCategory)
            {
                return weapon;
            }
        }

        Debug.LogWarning($"WeaponDatabase: Could not find a weapon with category {searchCategory}!");
        return null;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying || UnityEditor.EditorApplication.isUpdating) return;
        
        // Auto-populate from Resources/Weapons when edited in inspector
        weaponEntries = new List<WeaponData>(Resources.LoadAll<WeaponData>("Weapons"));
    }
#endif
}
