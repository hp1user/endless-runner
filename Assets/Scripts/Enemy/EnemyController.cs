using UnityEngine;
using Player.Control;

namespace Enemy.Control
{
    public class EnemyController : MonoBehaviour
    {
        [Header("Runtime Stats")]
        [SerializeField] private string enemyName;
        [SerializeField] private float currentHealth;
        [SerializeField] private float moveSpeed;
        [SerializeField] private float damage;

        [Header("Optimization")]
        public bool debugMode = false;
        [Tooltip("Target offset from player's feet (e.g. 1.0 = Chest).")]
        public float targetHeightOffset = 1.0f;
        [Tooltip("How often (in seconds) the enemy recalculates the path to the player. Higher = Better Performance.")]
        public float rethinkInterval = 0.2f;

        // Hardcoded Layer Mask
        private LayerMask playerLayer => LayerMask.GetMask("Player");

        private Transform playerTransform;
        private Vector3 targetDirection;
        private float nextRethinkTime;
        private bool isDead = false;

        public void Initialize(EnemyEntry data, Transform target)
        {
            enemyName = data.enemyName;
            currentHealth = data.maxHealth;
            moveSpeed = data.moveSpeed;
            damage = data.damage; // RESTORED: This was missing!
            playerTransform = target;

            if (debugMode)
            {
                // Physics Check
                if (GetComponent<Rigidbody>() == null && GetComponent<Rigidbody2D>() == null)
                {
                    Debug.LogWarning($"<color=red>[Enemy]</color> {enemyName} has NO Rigidbody! Physics collisions might not work.");
                }

                int mask = playerLayer.value;
                if (mask == 0) Debug.LogError($"<color=red>[Enemy]</color> {enemyName} CANNOT find the 'Player' layer! Check Layer settings.");
                else Debug.Log($"<color=green>[Enemy]</color> {enemyName} initialized. Damage: {damage}, LayerMask: {mask}");
            }
        }

        private void Update()
        {
            if (isDead || playerTransform == null) return;

            // 1. PERFORMANCE: Only recalculate direction on an interval
            if (Time.time >= nextRethinkTime)
            {
                nextRethinkTime = Time.time + rethinkInterval;
                Vector3 targetPos = playerTransform.position + Vector3.up * targetHeightOffset;
                targetDirection = (targetPos - transform.position).normalized;
            }

            // 2. MOVEMENT: Apply the cached direction every frame for smooth motion
            if (targetDirection != Vector3.zero)
            {
                transform.Translate(targetDirection * moveSpeed * Time.deltaTime, Space.World);
                
                // Rotation is also smoothed (optional: could also put this on the interval)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(targetDirection), Time.deltaTime * 5f);
            }
        }

        public void TakeDamage(float damage)
        {
            if (isDead) return;

            currentHealth -= damage;
            Debug.Log($"<color=red>[Enemy]</color> {enemyName} took {damage} damage! Remaining HP: {currentHealth}");

            if (currentHealth <= 0)
            {
                Die();
            }
        }

        private void Die()
        {
            isDead = true;
            Debug.Log($"<color=black><b>[Enemy]</b></color> {enemyName} has been defeated!");
            
            // Future: Play death animation/particles here
            
            Destroy(gameObject, 0.1f);
        }

        private void OnTriggerEnter(Collider other)
        {
            HandleContact(other.gameObject);
        }

        private void OnCollisionEnter(Collision collision)
        {
            HandleContact(collision.gameObject);
        }

        private void HandleContact(GameObject other)
        {
            if (isDead) return;

            // NEW: Forced log to confirm physics is firing at all
            if (debugMode) Debug.Log($"[Enemy] Physics Contact with '{other.name}' (Layer: {LayerMask.LayerToName(other.layer)})");

            // PERFORMANCE: Bitwise layer check
            if (((1 << other.layer) & playerLayer) != 0)
            {
                PlayerController player = other.GetComponent<PlayerController>();
                if (player == null) player = other.GetComponentInParent<PlayerController>();

                if (player != null)
                {
                    player.TakeDamage(damage);
                    if (debugMode) Debug.Log($"<color=orange>[Combat]</color> {enemyName} dealt {damage} damage to player via contact!");
                    
                    // NEW: Destroy enemy when it hits the player
                    Die();
                }
            }
            else if (debugMode)
            {
                // This helps you see if the hit object is on the wrong layer
                Debug.Log($"[Enemy] Contact with {other.name} on layer '{LayerMask.LayerToName(other.layer)}'. (Expected 'Player' layer)");
            }
        }
    }
}
