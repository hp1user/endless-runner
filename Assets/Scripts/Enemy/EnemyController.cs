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
        private bool isGameOver = false; // NEW: Tracks if the player is dead
        private bool isGroundEnemy = false;

        // POOLING TRACKERS
        private EnemyManager myManager;
        private GameObject myOriginalPrefab;

        // --- TIME FREEZE LOGIC ---
        private void OnEnable()
        {
            PlayerController.OnPlayerDeath += HandleGameOver;
            isGameOver = false;
        }

        private void OnDisable()
        {
            PlayerController.OnPlayerDeath -= HandleGameOver;
        }

        private void HandleGameOver()
        {
            isGameOver = true;
        }

        // --- INITIALIZATION & LEVEL SCALING ---
        public void Initialize(EnemyEntry data, Transform target, EnemyManager manager, GameObject prefab)
        {
            myManager = manager;
            myOriginalPrefab = prefab;
            isDead = false;

            // 1. Find out what level we are on
            int currentLevel = 1;
            if (GameManager.Instance != null)
            {
                currentLevel = GameManager.Instance.currentLevel;
            }

            // 2. Calculate the difficulty multipliers
            float healthIncrease = myManager != null ? myManager.healthIncreasePerLevel : 0.15f;
            float damageIncrease = myManager != null ? myManager.damageIncreasePerLevel : 0.20f;
            float speedIncrease = myManager != null ? myManager.speedIncreasePerLevel : 0.05f;

            float healthMultiplier = 1f + ((currentLevel - 1) * healthIncrease);
            float damageMultiplier = 1f + ((currentLevel - 1) * damageIncrease);
            float speedMultiplier = 1f + ((currentLevel - 1) * speedIncrease);

            // 3. Apply the Base Stats * Multiplier
            enemyName = data.enemyName;
            currentHealth = data.maxHealth * healthMultiplier;
            moveSpeed = data.moveSpeed * speedMultiplier;
            damage = data.damage * damageMultiplier;
            isGroundEnemy = data.isGroundEnemy;

            playerTransform = target;

            if (debugMode)
            {
                if (GetComponent<Rigidbody>() == null && GetComponent<Rigidbody2D>() == null)
                    Debug.LogWarning($"<color=red>[Enemy]</color> {enemyName} has NO Rigidbody!");

                Debug.Log($"<color=green>[Enemy Level {currentLevel}]</color> {enemyName} spawned. HP: {currentHealth}, DMG: {damage}, SPD: {moveSpeed}");
            }
        }

        private void Update()
        {
            // NEW: Stop moving and thinking if the player is dead!
            if (isDead || playerTransform == null || isGameOver) return;

            // 1. PERFORMANCE: Only recalculate direction on an interval
            if (Time.time >= nextRethinkTime)
            {
                nextRethinkTime = Time.time + rethinkInterval;
                Vector3 targetPos = playerTransform.position + Vector3.up * targetHeightOffset;
                
                if (isGroundEnemy)
                {
                    targetPos.y = transform.position.y; // Keep it on the ground!
                }

                targetDirection = (targetPos - transform.position).normalized;
            }

            // 2. MOVEMENT: Apply the cached direction every frame for smooth motion
            if (targetDirection != Vector3.zero)
            {
                transform.Translate(targetDirection * moveSpeed * Time.deltaTime, Space.World);
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(targetDirection), Time.deltaTime * 5f);
            }

            // Safety net: Despawn if it falls off the bridge
            if (transform.position.z < -20f)
            {
                Die();
            }
        }

        public void TakeDamage(float damage)
        {
            if (isDead) return;

            currentHealth -= damage;
            if (debugMode) Debug.Log($"<color=red>[Enemy]</color> {enemyName} took {damage} damage! Remaining HP: {currentHealth}");

            if (currentHealth <= 0)
            {
                Die();
            }
        }

        private void Die()
        {
            if (isDead) return;
            isDead = true;

            if (debugMode) Debug.Log($"<color=black><b>[Enemy]</b></color> {enemyName} has been defeated!");

            if (myManager != null) myManager.OnEnemyDied();

            if (GameManager.Instance != null) GameManager.Instance.RegisterEnemyKill();

            if (myOriginalPrefab != null && PoolManager.Instance != null)
            {
                PoolManager.Instance.ReturnToPoolAfterDelay(this.gameObject, myOriginalPrefab, 0.1f);
            }
            else
            {
                Destroy(gameObject, 0.1f);
            }
        }

        private void OnTriggerEnter(Collider other) => HandleContact(other.gameObject);
        private void OnCollisionEnter(Collision collision) => HandleContact(collision.gameObject);

        private void HandleContact(GameObject other)
        {
            if (isDead) return;

            if (((1 << other.layer) & playerLayer) != 0)
            {
                PlayerController player = other.GetComponent<PlayerController>();
                if (player == null) player = other.GetComponentInParent<PlayerController>();

                if (player != null)
                {
                    player.TakeDamage(damage);
                    Die(); // Bugs explode/die when they hit the player!
                }
            }
        }
    }
}