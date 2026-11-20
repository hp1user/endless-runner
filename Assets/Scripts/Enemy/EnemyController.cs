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
        public float MoveSpeed { get => moveSpeed; set => moveSpeed = value; }
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

        [Header("VFX Settings")]
        [Tooltip("Primary VFX prefab spawned when this enemy dies (Death Splash).")]
        public GameObject deathVFX;
        [Tooltip("Optional secondary death VFX prefab (e.g. random splash variant).")]
        public GameObject deathVFX_Variant;
        [Tooltip("VFX prefab spawned when this enemy takes hit damage.")]
        public GameObject hitVFX;
        [Tooltip("Lifetime of the spawned death VFX before returning to pool.")]
        public float deathVFXLifetime = 2.0f;
        [Tooltip("Lifetime of the spawned hit VFX before returning to pool.")]
        public float hitVFXLifetime = 1.0f;

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

        [Header("Boss Dynamic Combat & Action Mixing")]
        [Tooltip("Minimum and maximum minions spawned in a single wave during normal phase (3-4).")]
        [SerializeField] private Vector2Int minionsPerWaveRange = new Vector2Int(3, 4);
        [Tooltip("Minimum and maximum minions spawned per wave when boss is in Phase 3 (both legs broken) (5-6).")]
        [SerializeField] private Vector2Int phase3MinionsPerWaveRange = new Vector2Int(5, 6);
        [Tooltip("Minimum and maximum minion waves in a minion phase (e.g. 1-2, or 1-3).")]
        [SerializeField] private Vector2Int minionWavesRange = new Vector2Int(1, 2);
        [Tooltip("Minimum and maximum projectile attacks in an attack sequence (e.g. 1-3).")]
        [SerializeField] private Vector2Int attacksPerPhaseRange = new Vector2Int(1, 3);
        [Tooltip("Delay (seconds) between spawning individual minions within a single wave.")]
        [SerializeField] private float minionStaggerDelay = 0.45f;
        [Tooltip("Breather duration (seconds) after player clears all minions before boss can attack.")]
        [SerializeField] private float postMinionsBreather = 1.8f;
        [Tooltip("Breather duration (seconds) between consecutive projectile attacks in an attack phase.")]
        [SerializeField] private float timeBetweenAttacks = 1.8f;
        [Tooltip("Breather duration (seconds) after completing an attack sequence before next action.")]
        [SerializeField] private float breatherAfterAttacks = 2.0f;

        [Header("Boss Projectile Attack Tuning")]
        [Tooltip("Travel speed of the acid projectile in normal phases (m/s).")]
        [SerializeField] private float projectileSpeed = 18f;
        [Tooltip("Travel speed of the acid projectile in Phase 3 Enrage (m/s).")]
        [SerializeField] private float phase3ProjectileSpeed = 21f;
        [Tooltip("Telegraph warning duration before firing in normal phases (seconds).")]
        [SerializeField] private float projectileWindup = 0.85f;
        [Tooltip("Telegraph warning duration before firing in Phase 3 Enrage (seconds).")]
        [SerializeField] private float phase3ProjectileWindup = 0.70f;
        [Tooltip("Stationary recovery pause after shooting before resuming chase (seconds).")]
        [SerializeField] private float projectileRecovery = 1.0f;

        private Coroutine bossCombatCoroutine;
        private System.Collections.Generic.List<EnemyController> activeMinions = new System.Collections.Generic.List<EnemyController>();

        // POOLING TRACKERS
        private EnemyEntry myData;
        private EnemyManager myManager;
        private GameObject myOriginalPrefab;

        public GameObject OriginalPrefab => myOriginalPrefab;
        public bool IsDead => isDead;
        public bool IsBoss => isBoss;
        public float CurrentHealth => currentHealth;
        public float DamageReduction => damageReduction;

        /// <summary>
        /// Cleanly despawns an active enemy and returns it to PoolManager if pooled.
        /// </summary>
        public void ForceDespawn()
        {
            if (isDead) return;
            isDead = true;
            StopAllCoroutines();

            if (bossCombatCoroutine != null)
            {
                StopCoroutine(bossCombatCoroutine);
                bossCombatCoroutine = null;
            }
            ForceDespawnAllMinions();
            SpawnTracker.ClearBossSafeLane();

            VFXPerformanceManager.ClearTarget(GetInstanceID());

            Collider col = GetComponent<Collider>();
            if (col != null) col.enabled = false;
            Collider[] childCols = GetComponentsInChildren<Collider>();
            foreach (var c in childCols) c.enabled = false;

            if (myOriginalPrefab != null && PoolManager.Instance != null)
            {
                PoolManager.Instance.ReturnToPool(this.gameObject, myOriginalPrefab);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        [Header("Boss Body Parts & Animation")]
        [SerializeField] private float bossHeadHealth = 1000f;
        [SerializeField] private float bossLegHealth = 350f;
        [SerializeField] private float bossHeadMultiplier = 1.0f;
        [SerializeField] private float damageReduction = 0f;
        private Animator animator;
        private bool leftLegBroken = false;
        private bool rightLegBroken = false;
        private bool hasTriggeredPhase3Roar = false;
        public bool AreBothLegsBroken => leftLegBroken && rightLegBroken;
        public bool IsLeftLegBroken => leftLegBroken;
        public bool IsRightLegBroken => rightLegBroken;
        private int horizontalParamHash = Animator.StringToHash("Horizontal");
        private int verticalParamHash = Animator.StringToHash("Vertical");
        private int deathParamHash = Animator.StringToHash("Death");

        private bool isAttacking = false;
        private float nextProjectileAttackTime;
        private float projectileMinInterval = 4f;
        private float projectileMaxInterval = 9f;
        private float projectileDamage = 25f;

        private void Awake()
        {
            // Failsafe if boss is placed directly in the scene or spawned without calling Initialize()
            if (currentHealth <= 0f)
            {
                if (name.ToLower().Contains("boss"))
                {
                    isBoss = true;
                    bossHeadHealth = 1000f;
                    bossLegHealth = 350f;
                    currentHealth = 1700f;
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

        private void Start()
        {
            if (isBoss && bossCombatCoroutine == null && !isDead)
            {
                bossCombatCoroutine = StartCoroutine(BossCombatDirectorRoutine());
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
            myData = data;
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

            damageReduction = data.damageReduction;
            if (damageReduction <= 0f)
            {
                if (isBoss) damageReduction = 40f;
                else if (data.category == EnemyCategory.Elite) damageReduction = 20f;
            }

            if (data.deathVFX != null) deathVFX = data.deathVFX;
            if (data.deathVFX_Variant != null) deathVFX_Variant = data.deathVFX_Variant;
            if (data.hitVFX != null) hitVFX = data.hitVFX;
            if (data.deathVFXLifetime > 0f) deathVFXLifetime = data.deathVFXLifetime;
            if (data.hitVFXLifetime > 0f) hitVFXLifetime = data.hitVFXLifetime;

            if (isBoss)
            {
                float baseTotal = data.maxHealth > 0 ? data.maxHealth : 1700f;
                bossHeadHealth = (baseTotal * (1000f / 1700f)) * healthMultiplier;
                bossLegHealth = (baseTotal * (350f / 1700f)) * healthMultiplier;
                currentHealth = bossHeadHealth + (bossLegHealth * 2f);
            }
            else
            {
                currentHealth = data.maxHealth * healthMultiplier;
            }

            canSpawnMinions = data.canSpawnMinions;
            minionSpawnInterval = data.minionSpawnInterval;
            minionTypes = data.minionTypes;

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
                hasTriggeredPhase3Roar = false;
                isAttacking = false;
                SetupBossBodyParts();

                if (bossCombatCoroutine != null) StopCoroutine(bossCombatCoroutine);
                bossCombatCoroutine = StartCoroutine(BossCombatDirectorRoutine());
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
                // Slide the corpse backward matching LevelManager track speed so it stays anchored to the road
                float dt = Mathf.Min(Time.deltaTime, 0.1f);
                float speed = 15f;
                Vector3 dir = Vector3.back;
                if (LevelManager.Instance != null)
                {
                    speed = LevelManager.Instance.isTransitioningToBridge 
                        ? LevelManager.Instance.transitionSpeed 
                        : LevelManager.Instance.worldMoveSpeed;
                    dir = (LevelManager.Instance.chunkDirection == LevelManager.MoveDirection.Backward) 
                        ? Vector3.back 
                        : Vector3.forward;
                }
                transform.position += dir * speed * dt;
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

            // 2. MOVEMENT: Apply movement when not in attack windup
            if (targetDirection != Vector3.zero && !isAttacking)
            {
                float currentMoveSpeed = moveSpeed;
                if (isBoss)
                {
                    if (leftLegBroken && rightLegBroken)
                    {
                        currentMoveSpeed = 0.10f; // User specified: 0.1 for both crippled legs
                    }
                    else if (leftLegBroken || rightLegBroken)
                    {
                        currentMoveSpeed = 0.25f; // User specified: 0.25 for 1 crippled leg
                    }
                    else
                    {
                        currentMoveSpeed = 0.35f; // User specified: 0.35 base speed
                    }
                }

                transform.Translate(targetDirection * currentMoveSpeed * Time.deltaTime, Space.World);
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(targetDirection), Time.deltaTime * 5f);
            }

            // Despawn enemy once it has passed behind the player
            if (spawnedInFront && playerTransform != null && transform.position.z < playerTransform.position.z - 3.5f)
            {
                if (!hasPassedPlayer)
                {
                    hasPassedPlayer = true;
                    if (myManager != null) myManager.OnEnemyPassedPlayer();
                }
                Despawn();
                return;
            }

            // Safety net: Despawn if it falls off the bridge or too far behind
            if (playerTransform != null && transform.position.z < playerTransform.position.z - 15f)
            {
                Despawn();
                return;
            }
            else if (transform.position.z < -40f)
            {
                Despawn();
                return;
            }

            // Boss Reached Player check (instantly kills player for Game Over)
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

        private int SelectOptimalSafeLane(float[] allLanes)
        {
            Obstacle[] activeObstacles = Object.FindObjectsByType<Obstacle>(FindObjectsSortMode.None);
            float playerZ = playerTransform != null ? playerTransform.position.z : 0f;
            float bossZ = transform.position.z;
            float minZ = Mathf.Min(playerZ, bossZ) - 10f;
            float maxZ = Mathf.Max(playerZ, bossZ) + 30f;

            int[] obstacleCountPerLane = new int[allLanes.Length];
            for (int i = 0; i < allLanes.Length; i++)
            {
                float laneX = allLanes[i];
                foreach (Obstacle obs in activeObstacles)
                {
                    if (obs == null || !obs.gameObject.activeInHierarchy) continue;
                    Vector3 pos = obs.transform.position;
                    if (pos.z >= minZ && pos.z <= maxZ)
                    {
                        if (Mathf.Abs(pos.x - laneX) < 1.3f)
                        {
                            obstacleCountPerLane[i]++;
                        }
                    }
                }
            }

            // Strictly prioritize lanes that have 0 obstacles
            System.Collections.Generic.List<int> completelyFreeLanes = new System.Collections.Generic.List<int>();
            for (int i = 0; i < allLanes.Length; i++)
            {
                if (obstacleCountPerLane[i] == 0)
                {
                    completelyFreeLanes.Add(i);
                }
            }

            if (completelyFreeLanes.Count > 0)
            {
                return completelyFreeLanes[Random.Range(0, completelyFreeLanes.Count)];
            }

            // Fallback: pick the lane with the minimum obstacles (which will then be purged)
            int minObstacles = int.MaxValue;
            System.Collections.Generic.List<int> bestLanes = new System.Collections.Generic.List<int>();
            for (int i = 0; i < allLanes.Length; i++)
            {
                if (obstacleCountPerLane[i] < minObstacles)
                {
                    minObstacles = obstacleCountPerLane[i];
                    bestLanes.Clear();
                    bestLanes.Add(i);
                }
                else if (obstacleCountPerLane[i] == minObstacles)
                {
                    bestLanes.Add(i);
                }
            }

            return bestLanes[Random.Range(0, bestLanes.Count)];
        }

        private void ClearObstaclesInSafeLane(float safeLaneX)
        {
            Obstacle[] activeObstacles = Object.FindObjectsByType<Obstacle>(FindObjectsSortMode.None);
            float playerZ = playerTransform != null ? playerTransform.position.z : 0f;
            float bossZ = transform.position.z;
            float minZ = Mathf.Min(playerZ, bossZ) - 10f;
            float maxZ = Mathf.Max(playerZ, bossZ) + 30f;

            foreach (Obstacle obs in activeObstacles)
            {
                if (obs == null || !obs.gameObject.activeInHierarchy) continue;
                Vector3 pos = obs.transform.position;
                if (pos.z >= minZ && pos.z <= maxZ && Mathf.Abs(pos.x - safeLaneX) < 1.3f)
                {
                    obs.Recycle();
                    if (obs.gameObject.activeInHierarchy)
                    {
                        obs.gameObject.SetActive(false);
                    }
                }
            }

            // Physical sweep fallback to guarantee clearing
            Vector3 center = new Vector3(safeLaneX, 1f, (minZ + maxZ) * 0.5f);
            Vector3 halfExtents = new Vector3(1.2f, 3f, (maxZ - minZ) * 0.5f);
            Collider[] colliders = Physics.OverlapBox(center, halfExtents, Quaternion.identity);
            foreach (var col in colliders)
            {
                if (col == null) continue;
                Obstacle obs = col.GetComponentInParent<Obstacle>();
                if (obs != null && obs.gameObject.activeInHierarchy)
                {
                    obs.Recycle();
                    if (obs.gameObject.activeInHierarchy) obs.gameObject.SetActive(false);
                }
            }
        }

        /// <summary>
        /// Checks whether any active minions spawned by the boss or on the bridge are currently alive AND in front of the player.
        /// Projectile attacks are strictly blocked as long as this returns true.
        /// Any minions that have overshot the player (passed behind them) are automatically despawned and do not block attacks.
        /// </summary>
        public bool AreAnyMinionsActive()
        {
            CleanActiveMinionsList();
            if (activeMinions.Count > 0) return true;

            // Fallback check: ensure no active non-boss enemies remain in FRONT of player
            float playerZ = playerTransform != null ? playerTransform.position.z : 0f;
            EnemyController[] allEnemies = Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None);
            foreach (var enemy in allEnemies)
            {
                if (enemy != null && enemy != this && !enemy.IsBoss && !enemy.IsDead && enemy.gameObject.activeInHierarchy)
                {
                    if (enemy.transform.position.z < playerZ - 2.5f)
                    {
                        // Overshot minion lingering behind player: force despawn so it doesn't block combat logic
                        enemy.ForceDespawn();
                    }
                    else
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private void CleanActiveMinionsList()
        {
            float playerZ = playerTransform != null ? playerTransform.position.z : 0f;
            for (int i = activeMinions.Count - 1; i >= 0; i--)
            {
                var m = activeMinions[i];
                if (m == null || m.IsDead || !m.gameObject.activeInHierarchy)
                {
                    activeMinions.RemoveAt(i);
                }
                else if (m.transform.position.z < playerZ - 2.5f)
                {
                    // Minion has overshot the player (passed behind them).
                    // Player cannot shoot backwards; cleanly despawn it and remove from active combat tracking.
                    m.ForceDespawn();
                    activeMinions.RemoveAt(i);
                }
            }
        }

        private void ForceDespawnAllMinions()
        {
            if (activeMinions == null) return;
            foreach (var m in activeMinions)
            {
                if (m != null && !m.IsDead && m.gameObject.activeInHierarchy)
                {
                    m.ForceDespawn();
                }
            }
            activeMinions.Clear();
        }

        /// <summary>
        /// Dynamic Boss Action Director:
        /// Creates an unpredictable, unscripted combat mix:
        /// - Sometimes 1-2 minion waves -> breather -> 1-3 projectile attacks.
        /// - Sometimes 1-3 projectile attacks -> breather -> minion summon.
        /// - Sometimes 1 attack -> breather -> 1 minion wave -> breather -> 2 attacks.
        /// - Projectile attacks are STRICTLY BLOCKED as long as any minions remain on the road!
        /// </summary>
        private System.Collections.IEnumerator BossCombatDirectorRoutine()
        {
            // Initial grace window upon arrival (2.5s): player can observe entrance, shoot, and reposition
            yield return new WaitForSeconds(2.5f);

            int consecutiveMinionPhases = 0;
            int consecutiveAttackPhases = 0;

            while (!isDead && isBoss)
            {
                bool hasMinionsConfigured = canSpawnMinions && minionTypes != null && minionTypes.Count > 0;

                int nextActionType;
                if (!hasMinionsConfigured)
                {
                    nextActionType = 1; // Only attacks if no minions configured
                }
                else if (consecutiveMinionPhases >= 2)
                {
                    nextActionType = 1; // Force attack sequence to prevent endless minion loops
                }
                else if (consecutiveAttackPhases >= 2)
                {
                    nextActionType = 0; // Force minion sequence to maintain variety
                }
                else
                {
                    // Dynamic 50/50 roll for unpredictable flow
                    nextActionType = Random.Range(0, 2);
                }

                if (nextActionType == 0)
                {
                    // === MINION SEQUENCE ===
                    int currentLevel = GameManager.Instance != null ? GameManager.Instance.currentLevel : 5;
                    int maxWaves = currentLevel >= 10 ? Mathf.Max(minionWavesRange.y, 3) : minionWavesRange.y;
                    int wavesToSummon = Random.Range(minionWavesRange.x, maxWaves + 1);

                    Debug.Log($"<color=yellow><b>[Boss Combat]</b></color> Boss initiating Minion Sequence ({wavesToSummon} wave{(wavesToSummon > 1 ? "s" : "")}).");

                    for (int w = 0; w < wavesToSummon; w++)
                    {
                        if (isDead) yield break;

                        yield return StartCoroutine(SpawnMinionWaveRoutine());

                        // Wait for minions of this wave to be cleared (with anti-stall timeout)
                        float maxWait = 10f;
                        float timer = 0f;
                        while (AreAnyMinionsActive() && timer < maxWait && !isDead)
                        {
                            timer += Time.deltaTime;
                            yield return null;
                        }

                        if (AreAnyMinionsActive())
                        {
                            Debug.Log("<color=yellow><b>[Boss Combat]</b></color> Minion wave wait timeout reached; cleaning up lingering minions.");
                            ForceDespawnAllMinions();
                        }

                        CleanActiveMinionsList();

                        // Breather between minion waves or before next action
                        float breather = (w < wavesToSummon - 1) ? Random.Range(2.0f, 2.8f) : postMinionsBreather;
                        Debug.Log($"<color=green><b>[Boss Combat]</b></color> Minion wave {w + 1}/{wavesToSummon} cleared! Breather active ({breather:F1}s). Boss holds fire.");
                        yield return new WaitForSeconds(breather);
                    }

                    consecutiveMinionPhases++;
                    consecutiveAttackPhases = 0;
                }
                else
                {
                    // === PROJECTILE ATTACK SEQUENCE ===
                    int numAttacks = Random.Range(attacksPerPhaseRange.x, attacksPerPhaseRange.y + 1);

                    Debug.Log($"<color=red><b>[Boss Combat]</b></color> Boss initiating Attack Sequence ({numAttacks} projectile attack{(numAttacks > 1 ? "s" : "")}).");

                    for (int a = 0; a < numAttacks; a++)
                    {
                        if (isDead) yield break;

                        // Strict safety check: hold fire if any minions appeared on the road
                        if (AreAnyMinionsActive())
                        {
                            Debug.Log("<color=yellow><b>[Boss Combat]</b></color> Minions detected on road. Holding projectile attack until cleared.");
                            float holdTimer = 0f;
                            while (AreAnyMinionsActive() && holdTimer < 8f && !isDead)
                            {
                                holdTimer += Time.deltaTime;
                                yield return null;
                            }
                            if (AreAnyMinionsActive())
                            {
                                Debug.Log("<color=yellow><b>[Boss Combat]</b></color> Attack hold timeout reached; force-despawning lingering minions.");
                                ForceDespawnAllMinions();
                            }
                            yield return new WaitForSeconds(postMinionsBreather);
                        }

                        if (isDead) yield break;

                        yield return StartCoroutine(PerformProjectileAttackRoutine());

                        // Paced breather between attacks in this sequence
                        if (a < numAttacks - 1)
                        {
                            float attackGap = Random.Range(timeBetweenAttacks * 0.85f, timeBetweenAttacks * 1.15f);
                            yield return new WaitForSeconds(attackGap);
                        }
                    }

                    // Breather after completing the attack sequence
                    yield return new WaitForSeconds(breatherAfterAttacks);

                    consecutiveAttackPhases++;
                    consecutiveMinionPhases = 0;
                }
            }
        }

        private System.Collections.IEnumerator SpawnMinionWaveRoutine()
        {
            if (myManager == null || playerTransform == null || minionTypes == null || minionTypes.Count == 0) yield break;

            bool isPhase3 = AreBothLegsBroken;
            int countToSpawn;
            if (isPhase3)
            {
                countToSpawn = Random.Range(phase3MinionsPerWaveRange.x, phase3MinionsPerWaveRange.y + 1);
            }
            else
            {
                countToSpawn = Random.Range(minionsPerWaveRange.x, minionsPerWaveRange.y + 1);
            }

            Debug.Log($"<color=yellow><b>[Boss Combat]</b></color> Boss summoning minion wave ({(isPhase3 ? "Phase 3 Rage: " : "")}{countToSpawn} minions across separate lanes)!");

            EndlessRunner.Audio.AudioManager.Instance?.PlayBossSound(EndlessRunner.Audio.BossSoundType.Roar, transform.position);

            float[] runnerLanes = new float[] { -2f, 0f, 2f };
            System.Collections.Generic.List<int> lanePool = new System.Collections.Generic.List<int> { 0, 1, 2 };

            for (int i = 0; i < countToSpawn; i++)
            {
                if (isDead) yield break;

                int pick = Random.Range(0, lanePool.Count);
                int laneIdx = lanePool[pick];
                lanePool.RemoveAt(pick);
                if (lanePool.Count == 0) lanePool.AddRange(new int[] { 0, 1, 2 });

                float spawnX = runnerLanes[laneIdx];
                SpawnSingleMinion(spawnX, i * 2.0f);

                if (i < countToSpawn - 1)
                {
                    yield return new WaitForSeconds(minionStaggerDelay);
                }
            }
        }

        private void SpawnSingleMinion(float spawnX, float zStaggerOffset)
        {
            if (myManager == null || playerTransform == null || minionTypes == null || minionTypes.Count == 0) return;

            EnemyEntry minionData = minionTypes[Random.Range(0, minionTypes.Count)];
            if (minionData == null || minionData.prefab == null) return;

            float playerZ = playerTransform.position.z;
            float bossZ = transform.position.z;

            float spawnZ = Mathf.Clamp(playerZ + (bossZ - playerZ) * 0.5f + zStaggerOffset, playerZ + 8f, bossZ - 3.5f);
            float spawnY = minionData.groundYPosition > 0f ? minionData.groundYPosition : 0.45f;
            Vector3 spawnPos = new Vector3(spawnX, spawnY, spawnZ);

            if (PoolManager.Instance != null)
            {
                GameObject minionObj = PoolManager.Instance.SpawnFromPool(minionData.prefab.gameObject, spawnPos, Quaternion.identity);
                if (minionObj != null)
                {
                    EnemyController minionController = minionObj.GetComponent<EnemyController>();
                    if (minionController == null) minionController = minionObj.AddComponent<EnemyController>();
                    minionController.Initialize(minionData, playerTransform, myManager, minionData.prefab.gameObject);
                    if (minionController.MoveSpeed < 2.5f)
                    {
                        minionController.MoveSpeed = 2.5f;
                    }
                    activeMinions.Add(minionController);
                }
            }
        }

        private System.Collections.IEnumerator PerformProjectileAttackRoutine()
        {
            if (isDead || playerTransform == null) yield break;

            // Strictly hold fire if any minions are on the road
            if (AreAnyMinionsActive())
            {
                Debug.Log("<color=yellow><b>[Boss Attack]</b></color> Aborting projectile attack: Minions detected on road.");
                yield break;
            }

            isAttacking = true;

            // 1. Intelligently pick safe lane that is clear of obstacles
            float[] allLanes = new float[] { -2f, 0f, 2f };
            int safeLaneIndex = SelectOptimalSafeLane(allLanes);
            float safeLaneX = allLanes[safeLaneIndex];

            System.Collections.Generic.List<float> targetedLanesList = new System.Collections.Generic.List<float>();
            for (int i = 0; i < 3; i++)
            {
                if (i != safeLaneIndex) targetedLanesList.Add(allLanes[i]);
            }
            float[] targetedLanes = targetedLanesList.ToArray();

            // Dynamic Phase 3 Enrage Scaling
            bool isPhase3 = AreBothLegsBroken;
            float currentWindup = isPhase3 ? phase3ProjectileWindup : projectileWindup;
            float currentSpeed = isPhase3 ? phase3ProjectileSpeed : projectileSpeed;
            float currentRecovery = isPhase3 ? 0.65f : projectileRecovery;

            // Lock safe lane in SpawnTracker to prevent ObstacleManager from spawning into it
            SpawnTracker.SetBossSafeLane(safeLaneIndex, currentWindup + currentRecovery + 1.2f);

            // Clean up any remaining obstacle in safe lane corridor so escape is 100% fair
            ClearObstaclesInSafeLane(safeLaneX);

            Debug.Log($"<color=red><b>[Boss Attack]</b></color> Boss telegraphing 2-lane projectile! (Safe Lane: {safeLaneX}{(isPhase3 ? " [ENRAGED]" : "")})");

            // Halt walk animation during attack stop
            if (animator != null)
            {
                animator.SetFloat(verticalParamHash, 0f);
            }

            // 2. Visual telegraph overlay matching attack trajectory & safe lane corridor
            float startZ = transform.position.z;
            float endZ = playerTransform.position.z - 5f;
            float telegraphDuration = currentWindup + 1.2f;
            BossAttackTelegraph.Create(targetedLanes, safeLaneX, startZ, endZ, telegraphDuration);

            // Yield crisp windup before firing
            yield return new WaitForSeconds(currentWindup);

            if (isDead)
            {
                SpawnTracker.ClearBossSafeLane();
                yield break;
            }

            // 3. Launch the 2-lane projectile barrage
            Vector3 spawnOrigin = transform.position;
            spawnOrigin.y += 0.8f;
            spawnOrigin.z -= 1.5f;

            BossProjectile.Spawn(targetedLanes, spawnOrigin, currentSpeed, projectileDamage, playerTransform, deathVFX);

            // 4. Post-shot recovery: short pause, then immediately resume charging
            yield return new WaitForSeconds(currentRecovery);

            SpawnTracker.ClearBossSafeLane();

            isAttacking = false;
            if (isBoss && !isDead)
            {
                UpdateLegCrippleState();
            }
        }

        private void Despawn()
        {
            if (isDead) return;
            isDead = true;

            VFXPerformanceManager.ClearTarget(GetInstanceID());

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
                    part.Initialize(this, health, mult, damageReduction);
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
                part.Initialize(this, health, mult, damageReduction);
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

            if (isBoss && UI.Control.UIManager.Instance != null)
            {
                float totalMax = (bossHeadHealth + (bossLegHealth * 2f));
                UI.Control.UIManager.Instance.UpdateBossHealth(currentHealth, totalMax > 0f ? totalMax : 1700f);
            }

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
            }
            else if (partType == BossBodyPartType.RightLeg)
            {
                if (isBroken && !rightLegBroken)
                {
                    rightLegBroken = true;
                    Debug.Log("<color=red>[Boss]</color> <b>Right Leg is now BROKEN!</b>");
                }
            }

            // Phase 3 Enraged Roar sequence (triggered once when both legs become broken)
            if (leftLegBroken && rightLegBroken && !hasTriggeredPhase3Roar)
            {
                hasTriggeredPhase3Roar = true;
                bool shouldRoarOnPhase3 = (myData == null || myData.playRoarOnPhase3);
                if (shouldRoarOnPhase3)
                {
                    TriggerPhase3RoarSequence();
                    return; // RoarIntroRoutine will call UpdateLegCrippleState() upon completion!
                }
            }

            UpdateLegCrippleState();
        }

        private void UpdateLegCrippleState()
        {
            if (isDead) return;

            // Coordinates in Bug Boss Blend Tree:
            // Idle:        Horizontal =  0, Vertical = 0
            // Walk:        Horizontal =  0, Vertical = 1
            // Cripple_L:   Horizontal = -1, Vertical = 2
            // Cripple R:   Horizontal =  1, Vertical = 2
            // Criple Both: Horizontal =  0, Vertical = 2

            if (leftLegBroken && rightLegBroken)
            {
                Debug.Log("<color=orange><b>[Boss] BOTH legs are broken! Playing 'Criple Both' animation (0.4x speed)</b></color>");
                if (animator != null)
                {
                    animator.SetFloat(horizontalParamHash, 0f);
                    animator.SetFloat(verticalParamHash, 2f);
                }
            }
            else if (leftLegBroken)
            {
                Debug.Log("<color=orange><b>[Boss] Left Leg broken! Playing Left Leg Cripple animation (0.75x speed)</b></color>");
                if (animator != null)
                {
                    animator.SetFloat(horizontalParamHash, 1f);
                    animator.SetFloat(verticalParamHash, 2f);
                }
            }
            else if (rightLegBroken)
            {
                Debug.Log("<color=orange><b>[Boss] Right Leg broken! Playing Right Leg Cripple animation (0.75x speed)</b></color>");
                if (animator != null)
                {
                    animator.SetFloat(horizontalParamHash, -1f);
                    animator.SetFloat(verticalParamHash, 2f);
                }
            }
            else
            {
                // Neither leg broken yet: walk normally!
                if (animator != null)
                {
                    animator.SetFloat(horizontalParamHash, 0f);
                    animator.SetFloat(verticalParamHash, 1f);
                }
            }
        }

        public void SpawnDeathVFX()
        {
            if (!VFXPerformanceManager.IsVFXEnabled) return; // Skip heavy GPU compute VFX on low-end hardware

            GameObject vfxPrefab = deathVFX;
            if (deathVFX != null && deathVFX_Variant != null)
            {
                vfxPrefab = (Random.value < 0.5f) ? deathVFX : deathVFX_Variant;
            }
            else if (vfxPrefab == null && deathVFX_Variant != null)
            {
                vfxPrefab = deathVFX_Variant;
            }

            if (vfxPrefab != null && PoolManager.Instance != null)
            {
                Vector3 spawnPos = transform.position;
                if (isGroundEnemy && groundYPosition != 0f)
                {
                    spawnPos.y = groundYPosition;
                }

                GameObject splash = PoolManager.Instance.SpawnFromPool(vfxPrefab, spawnPos, Quaternion.identity);
                if (splash != null)
                {
                    var mover = splash.GetComponent<VFX.VFXTrackMover>();
                    if (mover == null) mover = splash.AddComponent<VFX.VFXTrackMover>();
                    mover.enabled = true;
                }
                float lifetime = deathVFXLifetime > 0f ? deathVFXLifetime : 2.0f;
                PoolManager.Instance.ReturnToPoolAfterDelay(splash, vfxPrefab, lifetime);
            }
        }

        public void SpawnHitVFX(Vector3 hitPoint, Vector3 hitNormal, bool allowSameFrameBurst = true)
        {
            if (!VFXPerformanceManager.CanSpawnHitVFX(GetInstanceID(), allowSameFrameBurst)) return; // Rate-limiting & low-end safety

            GameObject vfxPrefab = hitVFX;
            if (vfxPrefab == null) return;

            Quaternion rot = (hitNormal != Vector3.zero) ? Quaternion.LookRotation(hitNormal) : Quaternion.identity;
            if (PoolManager.Instance != null)
            {
                GameObject vfxObj = PoolManager.Instance.SpawnFromPool(vfxPrefab, hitPoint, rot);
                float lifetime = hitVFXLifetime > 0f ? hitVFXLifetime : 1.0f;
                PoolManager.Instance.ReturnToPoolAfterDelay(vfxObj, vfxPrefab, lifetime);
            }
        }

        public void TakeDamage(float damage, Collider hitCollider = null, Vector3 hitPoint = default, Vector3 hitNormal = default, bool isCrit = false)
        {
            if (isDead) return;

            if (hitPoint != default)
            {
                SpawnHitVFX(hitPoint, hitNormal);
            }
            else
            {
                SpawnHitVFX(transform.position + Vector3.up * 0.5f, Vector3.up);
            }

            float reductionFactor = 1f - Mathf.Clamp01(damageReduction / 100f);

            if (isBoss && hitCollider != null)
            {
                BossBodyPart part = hitCollider.GetComponent<BossBodyPart>();
                if (part == null) part = hitCollider.GetComponentInParent<BossBodyPart>();
                if (part != null)
                {
                    part.TakeDamage(damage, hitPoint, hitNormal, isCrit);
                    return;
                }

                string colName = hitCollider.name.ToLower();
                if (colName.Contains("head"))
                {
                    bool bothLegsBroken = leftLegBroken && rightLegBroken;
                    if (!bothLegsBroken)
                    {
                        float headDmg = damage * 0.10f; // 90% reduction
                        OnPartDamaged(BossBodyPartType.Head, headDmg, false);
                        Vector3 spawnPos = (hitPoint != default) ? hitPoint : (transform.position + Vector3.up * 1.5f);
                        UI.DamageNumbers.DamageNumberManager.Instance?.SpawnBossDamage(headDmg, spawnPos, BossBodyPartType.Head, false, isCrit, true);
                        return;
                    }
                    else
                    {
                        float headDmg = (damage * 1.25f) * reductionFactor;
                        OnPartDamaged(BossBodyPartType.Head, headDmg, false);
                        Vector3 spawnPos = (hitPoint != default) ? hitPoint : (transform.position + Vector3.up * 1.5f);
                        UI.DamageNumbers.DamageNumberManager.Instance?.SpawnBossDamage(headDmg, spawnPos, BossBodyPartType.Head, false, isCrit, false);
                        return;
                    }
                }
                else if (colName.Contains("l_leg") || colName.Contains("left"))
                {
                    float factor = leftLegBroken ? 0.10f : reductionFactor;
                    float legDmg = damage * factor;
                    OnPartDamaged(BossBodyPartType.LeftLeg, legDmg, leftLegBroken);
                    Vector3 spawnPos = (hitPoint != default) ? hitPoint : (transform.position + Vector3.up * 0.5f);
                    UI.DamageNumbers.DamageNumberManager.Instance?.SpawnBossDamage(legDmg, spawnPos, BossBodyPartType.LeftLeg, leftLegBroken, isCrit, false);
                    return;
                }
                else if (colName.Contains("r_leg") || colName.Contains("right"))
                {
                    float factor = rightLegBroken ? 0.10f : reductionFactor;
                    float legDmg = damage * factor;
                    OnPartDamaged(BossBodyPartType.RightLeg, legDmg, rightLegBroken);
                    Vector3 spawnPos = (hitPoint != default) ? hitPoint : (transform.position + Vector3.up * 0.5f);
                    UI.DamageNumbers.DamageNumberManager.Instance?.SpawnBossDamage(legDmg, spawnPos, BossBodyPartType.RightLeg, rightLegBroken, isCrit, false);
                    return;
                }
            }

            float finalDamage = damage * reductionFactor;
            currentHealth -= finalDamage;
            if (debugMode) Debug.Log($"<color=red>[Enemy]</color> {enemyName} took {finalDamage:F1} damage (Incoming: {damage:F1}{(damageReduction > 0f ? $" -{damageReduction:F0}%" : "")}{(isCrit ? " <color=#C084FC>[CRIT]</color>" : "")})! Remaining HP: {currentHealth:F1}");

            // Floating damage numbers
            if (isBoss)
            {
                Vector3 bossSpawnPos = (hitPoint != default) ? hitPoint : (transform.position + Vector3.up * 1.5f);
                if (isCrit)
                {
                    UI.DamageNumbers.DamageNumberManager.Instance?.SpawnDamage(finalDamage, bossSpawnPos, UI.DamageNumbers.DamageStyle.Critical);
                }
                else
                {
                    UI.DamageNumbers.DamageNumberManager.Instance?.SpawnDamage(finalDamage, bossSpawnPos, UI.DamageNumbers.DamageStyle.BossGeneral);
                }
            }
            else
            {
                UI.DamageNumbers.DamageNumberManager.Instance?.SpawnEnemyDamage(finalDamage, transform.position, isCrit);
            }

            if (isBoss)
                EndlessRunner.Audio.AudioManager.Instance?.PlayBossSound(EndlessRunner.Audio.BossSoundType.Hurt, hitPoint != default ? hitPoint : transform.position);
            else
                EndlessRunner.Audio.AudioManager.Instance?.PlayBugSound(EndlessRunner.Audio.BugSoundType.Hurt, hitPoint != default ? hitPoint : transform.position);

            if (currentHealth <= 0)
            {
                Die();
            }
        }

        private void Die()
        {
            if (isDead) return;
            isDead = true;

            VFXPerformanceManager.ClearTarget(GetInstanceID());

            // Trigger Death Splash VFX
            SpawnDeathVFX();

            if (isBoss)
                EndlessRunner.Audio.AudioManager.Instance?.PlayBossSound(EndlessRunner.Audio.BossSoundType.Death, transform.position);
            else
                EndlessRunner.Audio.AudioManager.Instance?.PlayBugSound(EndlessRunner.Audio.BugSoundType.Death, transform.position);

            if (debugMode) Debug.Log($"<color=black><b>[Enemy]</b></color> {enemyName} has been defeated!");

            if (myManager != null) myManager.OnEnemyDied();

            if (isBoss)
            {
                if (bossCombatCoroutine != null)
                {
                    StopCoroutine(bossCombatCoroutine);
                    bossCombatCoroutine = null;
                }
                ForceDespawnAllMinions();
                SpawnTracker.ClearBossSafeLane();

                if (UI.Control.UIManager.Instance != null)
                {
                    UI.Control.UIManager.Instance.ShowBossHealth(false);
                }
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

        /// <summary>
        /// Triggers the cinematic roar sequence when both legs shatter and Phase 3 begins.
        /// Plays roar audio, triggers roar animation, applies camera shake and haptic feedback.
        /// </summary>
        private void TriggerPhase3RoarSequence()
        {
            Debug.Log("<color=red><b>[Boss Phase 3]</b></color> Both legs shattered! Triggering Phase 3 Enraged Roar sequence!");

            // 1. Play Roar / Phase Change Audio
            AudioClip roarClip = (myData != null && myData.bossSpawnRoarClip != null) ? myData.bossSpawnRoarClip : null;
            if (roarClip != null)
            {
                if (EndlessRunner.Audio.AudioManager.Instance != null)
                {
                    EndlessRunner.Audio.AudioManager.Instance.PlaySFXAtPosition(roarClip, transform.position, 1.0f);
                }
                else
                {
                    AudioSource.PlayClipAtPoint(roarClip, transform.position);
                }
            }
            else
            {
                // Fallback to PhaseChange sound or Roar sound from AudioDatabase
                if (EndlessRunner.Audio.AudioManager.Instance != null)
                {
                    EndlessRunner.Audio.AudioManager.Instance.PlayBossSound(EndlessRunner.Audio.BossSoundType.PhaseChange, transform.position);
                    EndlessRunner.Audio.AudioManager.Instance.PlayBossSound(EndlessRunner.Audio.BossSoundType.Roar, transform.position);
                }
            }

            // 2. Play Roar Animation & Brief Pause (pauses movement, then transitions to crawling)
            string roarTrigger = (myData != null && !string.IsNullOrEmpty(myData.bossRoarAnimationTrigger)) ? myData.bossRoarAnimationTrigger : "Roar";
            float roarDuration = (myData != null && myData.bossRoarAnimationDuration > 0f) ? myData.bossRoarAnimationDuration : 1.5f;
            bool pauseMovement = (myData != null) ? myData.pauseMovementDuringRoar : true;
            TriggerRoarIntro(roarTrigger, roarDuration, pauseMovement);

            // 3. Cinematic Camera Shake & Haptics
            bool shake = (myData != null) ? myData.enableSpawnCameraShake : true;
            if (shake)
            {
                float duration = (myData != null && myData.spawnCameraShakeDuration > 0f) ? myData.spawnCameraShakeDuration : 0.8f;
                float intensity = (myData != null && myData.spawnCameraShakeIntensity > 0f) ? myData.spawnCameraShakeIntensity : 0.6f;
                CameraControl.CameraShake.ShakeCamera(duration, intensity);
            }

            bool haptics = (myData != null) ? myData.enableSpawnHaptics : true;
            if (haptics)
            {
                EndlessRunner.Core.HapticFeedbackManager.TriggerHeavyImpact();
            }
        }

        /// <summary>
        /// Triggers the cinematic roar intro animation for the boss on entrance.
        /// Safely checks if the trigger/parameter or state exists in the Animator before firing.
        /// </summary>
        public void TriggerRoarIntro(string triggerName = "Roar", float duration = 1.5f, bool pauseMovement = true)
        {
            if (isDead) return;
            StartCoroutine(RoarIntroRoutine(triggerName, duration, pauseMovement));
        }

        private System.Collections.IEnumerator RoarIntroRoutine(string triggerName, float duration, bool pauseMovement)
        {
            if (pauseMovement)
            {
                isAttacking = true;
                if (animator != null)
                {
                    animator.SetFloat(verticalParamHash, 0f);
                }
            }

            if (animator != null && !string.IsNullOrEmpty(triggerName))
            {
                if (HasAnimatorParameter(animator, triggerName))
                {
                    animator.SetTrigger(triggerName);
                }
                else
                {
                    // Fallback: check if an animation state with this name exists in layer 0
                    int stateHash = Animator.StringToHash(triggerName);
                    if (animator.HasState(0, stateHash))
                    {
                        animator.CrossFade(stateHash, 0.15f);
                    }
                }
            }

            if (duration > 0f)
            {
                yield return new WaitForSeconds(duration);
            }

            if (pauseMovement && !isDead)
            {
                isAttacking = false;
                UpdateLegCrippleState();
            }
        }

        private bool HasAnimatorParameter(Animator anim, string paramName)
        {
            if (anim == null || string.IsNullOrEmpty(paramName)) return false;
            foreach (var p in anim.parameters)
            {
                if (p.name == paramName) return true;
            }
            return false;
        }
    }
}