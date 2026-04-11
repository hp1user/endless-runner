using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class WeaponEntry
{
    public string weaponName = "New Weapon";
    public int animatorLayer = 1;

    [Header("3D Model")]
    [Tooltip("Drag your Gun Prefab here. (Using Transform slot to fix Unity Type Mismatch)")]
    public Transform weaponPrefab;
    
    [Header("Placement Offsets")]
    public Vector3 holdPosition;
    public Vector3 holdRotation;
    public Vector3 localScale = Vector3.one;

    [Header("Muzzle Settings")]
    public Vector3 muzzlePosition; // Offset relative to the gun model
    public Vector3 muzzleRotation;

    [Header("Audio SFX")]
    public AudioClip audioFire;
    public AudioClip audioMagOut;
    public AudioClip audioMagIn;

    [Header("VFX")]
    [Tooltip("Drag your muzzle flash prefab here.")]
    public Transform muzzleFlash;
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
}
