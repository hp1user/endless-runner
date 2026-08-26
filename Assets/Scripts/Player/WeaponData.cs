using UnityEngine;

[CreateAssetMenu(fileName = "New Weapon", menuName = "Game Data/Weapon Data")]
public class WeaponData : ScriptableObject
{
    public WeaponCategory category;
    public string weaponName = "New Weapon";
    public string weaponID = "new_weapon";
    public int animatorLayer = 1;

    [Header("Fire Mode Settings")]
    public WeaponFireMode fireMode = WeaponFireMode.Auto;
    public int burstCount = 3;

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
    
    [Tooltip("Optional: Visual bullet trail. If empty, a default one will be generated.")]
    public GameObject bulletTrailPrefab;
    public float trailDuration = 0.05f;
}
