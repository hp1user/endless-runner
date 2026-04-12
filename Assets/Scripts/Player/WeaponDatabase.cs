using UnityEngine;
using System.Collections.Generic;

public enum WeaponCategory { Pistol, SMG, Shotgun, AssaultRifle, Sniper, RocketLauncher, Minigun, Grenade }

[System.Serializable]
public class WeaponEntry
{
    public WeaponCategory category;
    public string weaponName = "New Weapon";
    public int animatorLayer = 1;

    [Header("3D Model")]
    [Tooltip("Drag your Gun Prefab here. (Using Transform slot to fix Unity Type Mismatch)")]
    public Transform weaponPrefab;

    [Header("Icon")]
    public Sprite icon;
    
    [Header("Placement Offsets")]
    public Vector3 holdPosition;
    public Vector3 holdRotation;
    public Vector3 localScale = Vector3.one;

    [Header("Muzzle Settings")]
    public Vector3 muzzlePosition; // Offset relative to the gun model
    public Vector3 muzzleRotation;

    [Header("Base Stats (Roguelike Foundation)")]
    public float baseDamage = 10f;
    public float fireRate = 5f; // Shots per second
    public int magSize = 30;
    public float reloadSpeedMult = 1.0f;
    public float range = 50f;

    [Header("Audio SFX")]
    public AudioClip audioFire;
    public AudioClip audioMagOut;
    public AudioClip audioMagIn;

    [Header("VFX Settings")]
    [Tooltip("Drag your muzzle flash prefab here.")]
    public Transform muzzleFlash;
    public float flashLifetime = 0.2f;
    public float impactLifetime = 1.0f;
}

[CreateAssetMenu(fileName = "WeaponDatabase", menuName = "Player/Weapon Database")]
public class WeaponDatabase : ScriptableObject
{
    public List<WeaponEntry> weaponEntries = new List<WeaponEntry>();

    public WeaponEntry GetEntryByLayer(int layer)
    {
        if (weaponEntries == null) return null;
        return weaponEntries.Find(e => e.animatorLayer == layer);
    }

    public WeaponEntry GetWeaponByCategory(WeaponCategory searchCategory)
    {
        // Assuming your list/array of weapons is called 'weapons' or 'entries'
        // Change 'allWeapons' to whatever your actual list variable is named!
        foreach (WeaponEntry weapon in weaponEntries)
        {
            if (weapon.category == searchCategory)
            {
                return weapon;
            }
        }

        Debug.LogWarning($"WeaponDatabase: Could not find a weapon with category {searchCategory}!");
        return null;
    }
}
