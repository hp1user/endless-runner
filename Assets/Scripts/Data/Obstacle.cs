using UnityEngine;
using Player.Control;

public class Obstacle : MonoBehaviour
{
    public enum MoveDirection { Backward, Forward }

    public enum ObstacleCategory
    {
        Standard,   // Normal size (Cars, Debris, Concrete Blocks, Cones, etc.)
        HeavyWide   // Large/Wide vehicles (Bus, Van, Heavy Trucks, etc.)
    }

    public enum ObstacleLaneRestriction
    {
        AnyLane = 0,     // Can spawn in any lane (Left, Center, Right)
        SidesOnly = 1,   // Can ONLY spawn in Left or Right side lanes (NEVER Center lane)
        CenterOnly = 2,  // Can ONLY spawn in Center lane
        LeftOnly = 3,    // Can ONLY spawn in Left lane (-X)
        RightOnly = 4    // Can ONLY spawn in Right lane (+X)
    }

    [Header("Classification & Lane Rules")]
    [Tooltip("Obstacle category classification")]
    public ObstacleCategory category = ObstacleCategory.Standard;
    [Tooltip("Allowed lanes for this obstacle. SidesOnly prevents spawning in the center lane so the player always has an escape path.")]
    public ObstacleLaneRestriction laneRestriction = ObstacleLaneRestriction.AnyLane;

    [Header("Obstacle Settings")]
    public float damageAmount = 15f;

    [Header("Movement")]
    public float worldMoveSpeed = 15f;

    [Header("Rotation Settings")]
    [Tooltip("Base Euler rotation for this obstacle")]
    public Vector3 baseRotation = Vector3.zero;
    [Tooltip("If true, randomizes Y rotation (Yaw) on spawn")]
    public bool randomizeYaw = true;
    [Tooltip("If true, randomly flips between forward (0 deg) and reverse (180 deg)")]
    public bool allowFlip180 = true;
    [Tooltip("Max random angle deviation added to yaw (e.g. 15 deg for slight angle)")]
    public float maxRandomYawVariation = 15f;
    [Tooltip("If true, spawns with full random 0-360 degree rotation (best for debris/rubble)")]
    public bool fullRandom360 = false;

    // Hidden variables managed by the ObstacleManager
    private MoveDirection currentDirection;
    private float currentDespawnThreshold;
    private GameObject originalPrefab;
    private bool isHit = false;

    /// <summary>
    /// Calculates the randomized spawn rotation for this obstacle based on its rules.
    /// </summary>
    public Quaternion GetSpawnRotation()
    {
        float yaw = baseRotation.y;

        if (fullRandom360)
        {
            yaw = Random.Range(0f, 360f);
        }
        else
        {
            if (allowFlip180 && Random.value > 0.5f)
            {
                yaw += 180f;
            }

            if (randomizeYaw && maxRandomYawVariation > 0f)
            {
                yaw += Random.Range(-maxRandomYawVariation, maxRandomYawVariation);
            }
        }

        return Quaternion.Euler(baseRotation.x, yaw, baseRotation.z);
    }

    // Called by ObstacleManager right after spawning from the pool
    public void Initialize(GameObject prefab, MoveDirection dir, float despawnDist)
    {
        originalPrefab = prefab;
        currentDirection = dir;
        currentDespawnThreshold = despawnDist;
        isHit = false;
    }

    private void Update()
    {
        // Clamp deltaTime to prevent huge jumps
        float dt = Mathf.Min(Time.deltaTime, 0.1f);

        // Move based on the Manager's direction
        Vector3 dir = (currentDirection == MoveDirection.Backward) ? Vector3.back : Vector3.forward;
        transform.position += dir * worldMoveSpeed * dt;

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
        if (isHit) return;

        // Check if we hit the player
        if (((1 << other.gameObject.layer) & LayerMask.GetMask("Player")) != 0)
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player == null) player = other.GetComponentInParent<PlayerController>();

            if (player != null) 
            {
                HitPlayer(player);
            }
        }
    }

    private void HitPlayer(PlayerController player)
    {
        isHit = true;
        player.TakeDamage(damageAmount);
        
        // Optional: Play a hit sound or particle effect here if desired before recycling
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
