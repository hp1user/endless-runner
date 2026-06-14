using UnityEngine;
using Player.Control;

public class Obstacle : MonoBehaviour
{
    public enum MoveDirection { Backward, Forward }

    [Header("Obstacle Settings")]
    public float damageAmount = 15f;

    [Header("Movement")]
    public float worldMoveSpeed = 15f;

    // Hidden variables managed by the ObstacleManager
    private MoveDirection currentDirection;
    private float currentDespawnThreshold;
    private GameObject originalPrefab;
    private bool isHit = false;

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
