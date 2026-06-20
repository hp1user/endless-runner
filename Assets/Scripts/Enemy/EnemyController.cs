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

        // Death Settings populated from EnemyDatabase
        private float deathDuration = 3.0f;
        private float groundYPosition = 0f;

        // Hardcoded Layer Mask
        private LayerMask playerLayer => LayerMask.GetMask("Player");

        private Transform playerTransform;
        private Vector3 targetDirection;
        private float nextRethinkTime;
        private bool isDead = false;
        private bool isGameOver = false; // NEW: Tracks if the player is dead
        private bool isGroundEnemy = false;
        private bool alwaysChasePlayer = true;
        private bool spawnedInFront = true;
        private bool hasPassedPlayer = false;

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

            // 0. Reset Death State
            Collider col = GetComponent<Collider>();
            if (col != null) col.enabled = true;
            
            // Reset rotation in case it was flipped from dying previously
            transform.rotation = Quaternion.identity; 

            VATInstanceController vat = GetComponentInChildren<VATInstanceController>();
            if (vat != null)
            {
                vat.animationIndex = 0;
                vat.UpdateProperties();
            }

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
            deathDuration = data.deathDuration;
            groundYPosition = data.groundYPosition;

            // Roll dice for chasing based on database config
            alwaysChasePlayer = data.alwaysChasePlayer ? true : (Random.Range(0f, 100f) < data.chaseChance);

            playerTransform = target;

            // Cache where the enemy spawned so it doesn't suddenly change its mind and turn around after passing the player
            spawnedInFront = (transform.position.z > (playerTransform != null ? playerTransform.position.z - 5f : -5f));
            hasPassedPlayer = false;

            if (debugMode)
            {
                if (GetComponent<Rigidbody>() == null)
                    Debug.LogWarning($"<color=red>[Enemy]</color> {enemyName} has NO Rigidbody!");

                Debug.Log($"<color=green>[Enemy Level {currentLevel}]</color> {enemyName} spawned. HP: {currentHealth}, DMG: {damage}, SPD: {moveSpeed}");
            }
        }

        private void Update()
        {
            if (isGameOver) return;

            if (isDead)
            {
                // Slide the corpse backward on the X/Z plane only (prevent floating up)
                Vector3 slideDir = -targetDirection;
                slideDir.y = 0f;
                transform.Translate(slideDir.normalized * moveSpeed * Time.deltaTime, Space.World);
                return;
            }

            if (playerTransform == null) return;

            // 1. PERFORMANCE: Only recalculate direction on an interval
            if (Time.time >= nextRethinkTime)
            {
                nextRethinkTime = Time.time + rethinkInterval;
                Vector3 targetPos = playerTransform.position + Vector3.up * targetHeightOffset;
                
                if (!alwaysChasePlayer)
                {
                    targetPos.x = transform.position.x; // Keep the same lane (don't chase X)
                }

                if (isGroundEnemy)
                {
                    targetPos.y = transform.position.y; // Keep it on the ground!
                }

                Vector3 dir = targetPos - transform.position;

                // --- ENDLESS RUNNER FLOW LOGIC ---
                if (spawnedInFront) 
                {
                    // Do not artificially inflate Z distance from afar (which dilutes steering), 
                    // but ensure a minimum forward speed so it doesn't hover at the player.
                    if (dir.z > -2f) dir.z = -2f; 

                    // If it is passing the player, stop steering sideways and just run straight away
                    if (transform.position.z <= playerTransform.position.z - 5f)
                    {
                        dir.x = 0;
                        dir.y = 0;
                    }
                }
                else 
                {
                    // Boss spawned behind the player. Force strong positive Z momentum.
                    dir.z = Mathf.Max(dir.z, 10f);
                }

                targetDirection = dir.normalized;
            }

            // 2. MOVEMENT: Apply the cached direction every frame for smooth motion
            if (targetDirection != Vector3.zero)
            {
                transform.Translate(targetDirection * moveSpeed * Time.deltaTime, Space.World);
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(targetDirection), Time.deltaTime * 5f);
            }

            // Immediately notify manager to spawn a replacement if it crosses Z=-5 behind the player
            if (spawnedInFront && !hasPassedPlayer && playerTransform != null && transform.position.z < playerTransform.position.z - 5f)
            {
                hasPassedPlayer = true;
                if (myManager != null) myManager.OnEnemyPassedPlayer();
            }

            // Safety net: Despawn if it falls off the bridge
            if (transform.position.z < -40f)
            {
                Despawn();
            }
        }

        private void Despawn()
        {
            if (isDead) return;
            isDead = true;

            if (myManager != null) myManager.OnEnemyDespawned();

            Collider col = GetComponent<Collider>();
            if (col != null) col.enabled = false;

            if (myOriginalPrefab != null && PoolManager.Instance != null)
            {
                PoolManager.Instance.ReturnToPoolAfterDelay(this.gameObject, myOriginalPrefab, 0f);
            }
            else
            {
                Destroy(gameObject);
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

            Collider col = GetComponent<Collider>();
            if (col != null) col.enabled = false;

            VATInstanceController vat = GetComponentInChildren<VATInstanceController>();
            if (vat != null)
            {
                vat.animationIndex = 1;
                vat.UpdateProperties();
            }

            StartCoroutine(DeathAnimationRoutine());
        }

        private System.Collections.IEnumerator DeathAnimationRoutine()
        {
            float animTime = 0.5f; // time to fall over
            float timer = 0f;

            Quaternion startRot = transform.rotation;
            // Pitch backwards 180 degrees
            Quaternion endRot = startRot * Quaternion.Euler(180f, 0f, 0f); 

            Vector3 startPos = transform.position;
            Vector3 endPos = startPos;
            endPos.y = groundYPosition; // ALL enemies should end up at the ground height

            while (timer < animTime)
            {
                timer += Time.deltaTime;
                float t = timer / animTime;
                
                transform.rotation = Quaternion.Slerp(startRot, endRot, t);
                
                // Move the Y position down to the ground, keep X and Z handled by Update
                Vector3 currentPos = transform.position;
                currentPos.y = Mathf.Lerp(startPos.y, endPos.y, t);
                transform.position = currentPos;
                
                yield return null;
            }

            // Ensure final state
            transform.rotation = endRot;
            
            Vector3 finalPos = transform.position;
            finalPos.y = endPos.y;
            transform.position = finalPos;

            // Wait the remaining duration so it stays visible
            yield return new WaitForSeconds(Mathf.Max(0f, deathDuration - animTime));

            if (myOriginalPrefab != null && PoolManager.Instance != null)
            {
                PoolManager.Instance.ReturnToPoolAfterDelay(this.gameObject, myOriginalPrefab, 0f);
            }
            else
            {
                Destroy(gameObject);
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