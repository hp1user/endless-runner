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
        private bool isBoss = false;
        private bool alwaysChasePlayer = true;
        private bool spawnedInFront = true;
        private bool hasPassedPlayer = false;

        // BOSS SPECIFIC
        private bool canSpawnMinions = false;
        private float minionSpawnInterval = 5f;
        private System.Collections.Generic.List<EnemyEntry> minionTypes;
        private float nextMinionSpawnTime;

        // POOLING TRACKERS
        private EnemyManager myManager;
        private GameObject myOriginalPrefab;

        public bool IsDead => isDead;
        public bool IsBoss => isBoss;
        public float CurrentHealth => currentHealth;

        [Header("Boss Body Parts & Animation")]
        [SerializeField] private float bossHeadHealth = 600f;
        [SerializeField] private float bossLegHealth = 200f;
        [SerializeField] private float bossHeadMultiplier = 1.0f;
        private Animator animator;
        private bool leftLegBroken = false;
        private bool rightLegBroken = false;
        private int horizontalParamHash = Animator.StringToHash("Horizontal");
        private int verticalParamHash = Animator.StringToHash("Vertical");
        private int deathParamHash = Animator.StringToHash("Death");

        private void Awake()
        {
            // Failsafe if boss is placed directly in the scene or spawned without calling Initialize()
            if (currentHealth <= 0f)
            {
                if (name.ToLower().Contains("boss"))
                {
                    isBoss = true;
                    bossHeadHealth = 600f;
                    bossLegHealth = 200f;
                    currentHealth = 1000f;
                }
                else
                {
                    currentHealth = 30f;
                }
            }

            if (isBoss)
            {
                animator = GetComponentInChildren<Animator>();
                SetupBossBodyParts();
            }
        }

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
            
            // Immediately face the target upon spawning
            if (target != null)
            {
                Vector3 dirToTarget = target.position - transform.position;
                dirToTarget.y = 0; // Keep rotation strictly horizontal
                if (dirToTarget != Vector3.zero)
                {
                    transform.rotation = Quaternion.LookRotation(dirToTarget.normalized);
                }
            }
            else
            {
                transform.rotation = Quaternion.identity;
            }

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
            moveSpeed = data.moveSpeed * speedMultiplier;
            damage = data.damage * damageMultiplier;
            isGroundEnemy = data.isGroundEnemy;
            isBoss = data.category == EnemyCategory.Boss;
            deathDuration = data.deathDuration;
            groundYPosition = data.groundYPosition;

            if (isBoss)
            {
                bossHeadHealth = 600f * healthMultiplier;
                bossLegHealth = 200f * healthMultiplier;
                currentHealth = bossHeadHealth + (bossLegHealth * 2f);
            }
            else
            {
                currentHealth = data.maxHealth * healthMultiplier;
            }

            canSpawnMinions = data.canSpawnMinions;
            minionSpawnInterval = data.minionSpawnInterval;
            minionTypes = data.minionTypes;
            nextMinionSpawnTime = Time.time + minionSpawnInterval;

            // Ensure enemies and all child colliders can be hit by raycasts
            SetLayerRecursively(gameObject, LayerMask.NameToLayer("Enemy"));

            // Initialize Boss parts and animator if this is a boss
            if (isBoss)
            {
                animator = GetComponentInChildren<Animator>();
                if (animator != null)
                {
                    animator.SetFloat(horizontalParamHash, 0f);
                    animator.SetFloat(verticalParamHash, 1f); // Normal Walk
                }
                leftLegBroken = false;
                rightLegBroken = false;
                SetupBossBodyParts();
            }

            // Roll dice for chasing based on database config (bosses always chase)
            alwaysChasePlayer = isBoss || (data.alwaysChasePlayer ? true : (Random.Range(0f, 100f) < data.chaseChance));

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
                    // Boss spawned behind the player: continuously chase the player!
                    if (dir.z < 2f) dir.z = 2f;
                }

                targetDirection = dir.normalized;
            }

            // 2. MOVEMENT: Apply the cached direction every frame for smooth motion
            if (targetDirection != Vector3.zero)
            {
                float currentMoveSpeed = moveSpeed;
                if (isBoss && leftLegBroken && rightLegBroken)
                {
                    currentMoveSpeed = moveSpeed * 0.5f;
                }

                transform.Translate(targetDirection * currentMoveSpeed * Time.deltaTime, Space.World);
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

            // 3. Boss Minion Spawning
            if (isBoss && canSpawnMinions && minionTypes != null && minionTypes.Count > 0)
            {
                if (Time.time >= nextMinionSpawnTime)
                {
                    nextMinionSpawnTime = Time.time + minionSpawnInterval;
                    SpawnMinion();
                }
            }

            // 4. Boss Reached Player check (instantly kills player for Game Over)
            if (isBoss && !isDead && playerTransform != null)
            {
                float deltaZ = Mathf.Abs(transform.position.z - playerTransform.position.z);
                float deltaX = Mathf.Abs(transform.position.x - playerTransform.position.x);
                if (deltaZ <= 1.5f && deltaX <= 1.5f)
                {
                    PlayerController player = playerTransform.GetComponent<PlayerController>();
                    if (player != null)
                    {
                        player.Kill();
                    }
                }
            }
        }

        private void SpawnMinion()
        {
            if (myManager == null || playerTransform == null) return;
            EnemyEntry minionData = minionTypes[Random.Range(0, minionTypes.Count)];
            if (minionData == null || minionData.prefab == null) return;

            Vector3 spawnPos = transform.position;
            spawnPos.z += 5f; // Spawn slightly in front of the boss
            spawnPos.x += Random.Range(-2f, 2f); // Random lane offset

            if (PoolManager.Instance != null)
            {
                GameObject minionObj = PoolManager.Instance.SpawnFromPool(minionData.prefab.gameObject, spawnPos, Quaternion.identity);
                EnemyController minionController = minionObj.GetComponent<EnemyController>();
                if (minionController == null) minionController = minionObj.AddComponent<EnemyController>();
                minionController.Initialize(minionData, playerTransform, myManager, minionData.prefab.gameObject);
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

        private void SetLayerRecursively(GameObject obj, int newLayer)
        {
            if (obj == null) return;
            obj.layer = newLayer;
            foreach (Transform child in obj.transform)
            {
                if (child != null) SetLayerRecursively(child.gameObject, newLayer);
            }
        }

        private void SetupBossBodyParts()
        {
            BossBodyPart[] parts = GetComponentsInChildren<BossBodyPart>(true);
            if (parts.Length > 0)
            {
                foreach (var part in parts)
                {
                    float health = (part.partType == BossBodyPartType.Head) ? bossHeadHealth : bossLegHealth;
                    float mult = (part.partType == BossBodyPartType.Head) ? bossHeadMultiplier : 1.0f;
                    part.Initialize(this, health, mult);
                }
                RecalculateBossHealth();
                return;
            }

            // Auto-detect colliders by name on children if BossBodyPart not already present
            Collider[] colliders = GetComponentsInChildren<Collider>(true);
            foreach (var c in colliders)
            {
                if (c.gameObject == this.gameObject) continue;

                string n = c.name.ToLower();
                BossBodyPartType type;
                if (n.Contains("head"))
                {
                    type = BossBodyPartType.Head;
                }
                else if (n.Contains("l_leg") || n.Contains("left"))
                {
                    type = BossBodyPartType.LeftLeg;
                }
                else if (n.Contains("r_leg") || n.Contains("right"))
                {
                    type = BossBodyPartType.RightLeg;
                }
                else
                {
                    continue;
                }

                BossBodyPart part = c.gameObject.GetComponent<BossBodyPart>();
                if (part == null) part = c.gameObject.AddComponent<BossBodyPart>();
                float health = (type == BossBodyPartType.Head) ? bossHeadHealth : bossLegHealth;
                float mult = (type == BossBodyPartType.Head) ? bossHeadMultiplier : 1.0f;
                part.partType = type;
                part.Initialize(this, health, mult);
            }
            RecalculateBossHealth();
        }

        public void RecalculateBossHealth()
        {
            BossBodyPart[] parts = GetComponentsInChildren<BossBodyPart>(true);
            if (parts != null && parts.Length > 0)
            {
                float total = 0f;
                foreach (var part in parts)
                {
                    total += Mathf.Max(0f, part.currentHealth);
                }
                currentHealth = total;
            }
        }

        public void OnPartDamaged(BossBodyPartType partType, float damage, bool isBroken)
        {
            if (isDead) return;

            // Directly damage boss overall health pool
            currentHealth -= damage;
            Debug.Log($"<color=orange>[Boss]</color> {partType} took {damage:F1} DMG! Boss Remaining Total HP: {Mathf.Max(0f, currentHealth):F1}");

            if (currentHealth <= 0f)
            {
                currentHealth = 0f;
                Debug.Log("<color=red><b>[Boss] Defeated! Boss total HP reached 0.</b></color>");
                Die();
                return;
            }

            // Leg damage and cripple handling
            if (partType == BossBodyPartType.LeftLeg)
            {
                if (isBroken && !leftLegBroken)
                {
                    leftLegBroken = true;
                    Debug.Log("<color=red>[Boss]</color> <b>Left Leg is now BROKEN!</b>");
                }
                UpdateLegCrippleState();
            }
            else if (partType == BossBodyPartType.RightLeg)
            {
                if (isBroken && !rightLegBroken)
                {
                    rightLegBroken = true;
                    Debug.Log("<color=red>[Boss]</color> <b>Right Leg is now BROKEN!</b>");
                }
                UpdateLegCrippleState();
            }
        }

        private void UpdateLegCrippleState()
        {
            if (isDead || animator == null) return;

            // If BOTH legs are broken, boss does NOT die; it moves at 1/2 speed and limps heavily towards player
            if (leftLegBroken && rightLegBroken)
            {
                Debug.Log("<color=orange><b>[Boss] BOTH legs are broken! Boss moves slowly (1/2 speed) towards player!</b></color>");
                animator.SetFloat(horizontalParamHash, 0f);
                animator.SetFloat(verticalParamHash, 2f);
                return;
            }

            // At a time only 1 cripple animation can play:
            // Notice: Right Leg broken -> Horizontal = -1, Vertical = 2
            //         Left Leg broken  -> Horizontal =  1, Vertical = 2
            if (rightLegBroken && !leftLegBroken)
            {
                animator.SetFloat(horizontalParamHash, -1f);
                animator.SetFloat(verticalParamHash, 2f);
            }
            else if (leftLegBroken && !rightLegBroken)
            {
                animator.SetFloat(horizontalParamHash, 1f);
                animator.SetFloat(verticalParamHash, 2f);
            }
            else
            {
                // Neither leg broken yet: walk normally!
                animator.SetFloat(horizontalParamHash, 0f);
                animator.SetFloat(verticalParamHash, 1f);
            }
        }

        public void TakeDamage(float damage, Collider hitCollider = null)
        {
            if (isDead) return;

            if (isBoss && hitCollider != null)
            {
                BossBodyPart part = hitCollider.GetComponent<BossBodyPart>();
                if (part == null) part = hitCollider.GetComponentInParent<BossBodyPart>();
                if (part != null)
                {
                    part.TakeDamage(damage);
                    return;
                }

                string colName = hitCollider.name.ToLower();
                if (colName.Contains("head"))
                {
                    OnPartDamaged(BossBodyPartType.Head, damage * bossHeadMultiplier, false);
                    return;
                }
                else if (colName.Contains("l_leg") || colName.Contains("left"))
                {
                    OnPartDamaged(BossBodyPartType.LeftLeg, damage, false);
                    return;
                }
                else if (colName.Contains("r_leg") || colName.Contains("right"))
                {
                    OnPartDamaged(BossBodyPartType.RightLeg, damage, false);
                    return;
                }
            }

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

            if (isBoss)
            {
                if (GameManager.Instance != null) GameManager.Instance.BossDefeated();
            }
            else
            {
                if (GameManager.Instance != null) GameManager.Instance.RegisterEnemyKill();
            }

            Collider col = GetComponent<Collider>();
            if (col != null) col.enabled = false;

            // Also disable child hitboxes
            Collider[] childCols = GetComponentsInChildren<Collider>();
            foreach (var c in childCols) c.enabled = false;

            if (animator != null)
            {
                animator.SetTrigger(deathParamHash);
                StartCoroutine(BossDeathRoutine());
                return;
            }

            VATInstanceController vat = GetComponentInChildren<VATInstanceController>();
            if (vat != null)
            {
                vat.animationIndex = 1;
                vat.UpdateProperties();
            }

            StartCoroutine(DeathAnimationRoutine());
        }

        private System.Collections.IEnumerator BossDeathRoutine()
        {
            yield return new WaitForSeconds(deathDuration);

            if (myOriginalPrefab != null && PoolManager.Instance != null)
            {
                PoolManager.Instance.ReturnToPoolAfterDelay(this.gameObject, myOriginalPrefab, 0f);
            }
            else
            {
                Destroy(gameObject);
            }
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

            PlayerController player = other.GetComponent<PlayerController>();
            if (player == null) player = other.GetComponentInParent<PlayerController>();

            if (player != null)
            {
                if (isBoss)
                {
                    player.Kill(); // Boss reaches player -> Game Over!
                }
                else
                {
                    player.TakeDamage(damage);
                    Die(); // Normal enemies explode on impact; bosses do not
                }
            }
        }
    }
}