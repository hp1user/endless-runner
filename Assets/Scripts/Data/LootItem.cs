using UnityEngine;
using Player.Control;

public class LootItem : MonoBehaviour
{
    public enum LootType { Health, Armor, Ammo, Weapon }
    public enum MoveDirection { Backward, Forward }

    [Header("Loot Settings")]
    public LootType type;
    public float amount = 20f;
    public WeaponCategory ammoCategory;
    public string specificWeaponID;

    [Header("Movement")]
    public float worldMoveSpeed = 15f;

    // Hidden variables managed by the Supply Chopper
    private MoveDirection currentDirection;
    private float currentDespawnThreshold;
    private GameObject originalPrefab;
    private bool isCollected = false;

    private void OnEnable()
    {
        isCollected = false;
    }

    // UPDATED: Now accepts the direction and threshold from the Manager
    public void Initialize(GameObject prefab, MoveDirection dir, float despawnDist)
    {
        originalPrefab = prefab;
        currentDirection = dir;
        currentDespawnThreshold = despawnDist;
        isCollected = false;
    }

    private void Update()
    {
        if (isCollected) return;

        // Clamp deltaTime to prevent huge jumps when unpausing from the Level Up screen
        float dt = Mathf.Min(Time.deltaTime, 0.1f);

        // Dynamically match track speed with LevelManager if active, else use worldMoveSpeed
        float speed = worldMoveSpeed;
        if (LevelManager.Instance != null)
        {
            speed = LevelManager.Instance.isTransitioningToBridge ? LevelManager.Instance.transitionSpeed : LevelManager.Instance.worldMoveSpeed;
        }

        // Move based on the Manager's direction
        Vector3 dir = (currentDirection == MoveDirection.Backward) ? Vector3.back : Vector3.forward;
        transform.position += dir * speed * dt;

        // Despawn based on the Manager's direction
        if (currentDirection == MoveDirection.Backward && transform.position.z < -currentDespawnThreshold)
        {
            Recycle();
        }
        else if (currentDirection == MoveDirection.Forward && transform.position.z > currentDespawnThreshold)
        {
            Recycle();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isCollected) return;

        PlayerController player = other.GetComponent<PlayerController>() 
            ?? other.GetComponentInParent<PlayerController>()
            ?? (PlayerController.Instance != null && (other.transform.root == PlayerController.Instance.transform || other.transform.IsChildOf(PlayerController.Instance.transform)) ? PlayerController.Instance : null);

        if (player != null)
        {
            CollectLoot(player);
        }
    }

    private void CollectLoot(PlayerController player)
    {
        isCollected = true;

        switch (type)
        {
            case LootType.Health:
                player.RestoreHealth(amount);
                break;
            case LootType.Armor:
                player.AddArmor(amount);
                break;
            case LootType.Ammo:
                player.AddAmmo(ammoCategory, (int)amount);
                break;
            case LootType.Weapon:
                player.UnlockWeapon(specificWeaponID);
                break;
        }

        Debug.Log($"<color=cyan>[LootItem]</color> Collected {type}! (amount: {amount}, category: {ammoCategory}, weaponID: {specificWeaponID})");
        Recycle();
    }

    private void Recycle()
    {
        if (originalPrefab != null && PoolManager.Instance != null)
            PoolManager.Instance.ReturnToPool(gameObject, originalPrefab);
        else
            Destroy(gameObject);
    }
}
