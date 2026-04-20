using UnityEngine;
using System.Collections.Generic;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using Enemy.Control;
using UI.Control;

namespace Player.Control
{
    /// <summary>
    /// Player Controller for an endless runner.
    /// Handles discrete horizontal movement (Starf), weapon layers, and strict input prioritization.
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        private Animator animator;

        [Header("Stat Configuration")]
        [Tooltip("The base stats for the player character.")]
        public PlayerDatabase playerStats;

        [Header("Movement Settings (Overrides)")]
        [Tooltip("How far apart the lanes are on the X-axis.")]
        public float laneDistance = 2.0f;

        [Tooltip("How fast the player physically moves between lanes.")]
        public float movementSpeed = 10f;

        [Tooltip("How fast the animator's Starf value reaches the target.")]
        public float strafeAnimationSmoothing = 8f;

        [Header("Weapon Layer Settings")]
        [Tooltip("The index of the weapon animation layer (e.g., 1 for Assault Rifle).")]
        public int weaponLayerIndex = 1;

        [Tooltip("How fast the layer weight blends to 1 when switching weapons.")]
        public float layerBlendSpeed = 10f;

        [Header("Aiming Settings")]
        [Tooltip("The Transform to calculate the aiming direction from (e.g., Chest or Head).")]
        public Transform aimOrigin;

        [Tooltip("The Transform that the player should aim at (e.g. moved by touch).")]
        public Transform aimTarget;

        [Tooltip("Index of the spine aiming layer.")]
        public int aimLayerIndex = 1;

        [Header("Weapon System")]
        [Tooltip("The central database containing all weapon data assets.")]
        public WeaponDatabase weaponDatabase;

        [Header("Weapon Parent & Sockets")]
        [Tooltip("The Transform (hand bone or socket) where weapons will be parented.")]
        public Transform weaponSocket;

        [Header("Effects Settings")]
        [Tooltip("Prefab to spawn at the location of a bullet impact.")]
        public Transform impactEffect;

        [Tooltip("The volume of the weapon sounds.")]
        [Range(0f, 1f)]
        public float weaponVolume = 0.5f;

        // Hidden Layers
        private LayerMask hitMask => (playerStats != null) ? playerStats.EnemyLayer : (LayerMask)LayerMask.GetMask("Enemy");

        private WeaponEntry currentWeaponData;
        private GameObject currentWeaponInstance;
        private int lastWeaponLayerIndex = -1;

        // Animator Parameter Names
        private string strafeParameter = "Starf";
        private string reloadParameter = "Reload";
        private string fireParameter = "isFiring";
        private string fireMultiplierParameter = "FireSpeedMultiplier";
        private string reloadMultiplierParameter = "ReloadSpeedMultiplier";
        private string aimHorizontalParameter = "Aim_Horizontal";
        private string aimVerticalParameter = "Aim_Vertical";
        private string reloadStateName = "Reload";
        private string reloadStateTag = "Reload";

        // Private Hashes for Performance
        private int strafeParamHash;
        private int reloadParamHash;
        private int fireParamHash;
        private int fireMultiplierParamHash;
        private int reloadMultiplierParamHash;
        private int aimHorizontalHash;
        private int aimVerticalHash;
        private int reloadStateHash;
        private int reloadTagHash;

        private AudioSource audioSource;

        [Header("Debug")]
        public bool debugMode = false;
        [SerializeField] private float debugAimH;
        [SerializeField] private float debugAimV;

        // Internal State
        private int currentLane = 0;
        private float currentStrafeAnim;
        private float currentAimH;
        private float currentAimV;
        private float targetAimH;
        private float targetAimV;
        private float targetLayerWeight = 1f;
        private float fireCooldownTimer;
        private AnimatorStateInfo currentWeaponState;
        private int currentAmmo;
        private int shotsFiredThisTriggerPull = 0;

        // Player Stats
        private float currentHealth;
        private float currentArmor;
        private float runtimeMovementSpeed;
        private float runtimeStrafeSmoothing;

        // Damage Cooldown (I-Frames)
        private float iFrameDuration = 0.5f;
        private float lastDamageTime = -999f;

        private bool touchReloadRequested = false;

        public static PlayerController Instance { get; private set; }

        private void OnEnable()
        {
            TouchManager.OnSwipeLeft += MoveLeft;
            TouchManager.OnSwipeRight += MoveRight;
            TouchManager.OnSwipeUp += RequestReload;
        }

        private void OnDisable()
        {
            TouchManager.OnSwipeLeft -= MoveLeft;
            TouchManager.OnSwipeRight -= MoveRight;
            TouchManager.OnSwipeUp -= RequestReload;
        }

        private void RequestReload()
        {
            touchReloadRequested = true;
        }

        private void Awake()
        {
            Instance = this;

            animator = GetComponent<Animator>();
            audioSource = GetComponent<AudioSource>();

            if (playerStats != null)
            {
                InitializeStats();
            }
            else
            {
                Debug.LogWarning("[PlayerController] No PlayerDatabase assigned! Using default values.");
                laneDistance = 2.0f;
                movementSpeed = 10f;
                strafeAnimationSmoothing = 8f;
            }

            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

            // Cache Hashes
            strafeParamHash = Animator.StringToHash(strafeParameter);
            reloadParamHash = Animator.StringToHash(reloadParameter);
            fireParamHash = Animator.StringToHash(fireParameter);
            fireMultiplierParamHash = Animator.StringToHash(fireMultiplierParameter);
            reloadMultiplierParamHash = Animator.StringToHash(reloadMultiplierParameter);
            aimHorizontalHash = Animator.StringToHash(aimHorizontalParameter);
            aimVerticalHash = Animator.StringToHash(aimVerticalParameter);
            reloadStateHash = Animator.StringToHash(reloadStateName);
            reloadTagHash = Animator.StringToHash(reloadStateTag);

            if (animator == null) Debug.LogWarning("[PlayerController] No Animator found!");

            if (WeaponWheelManager.Instance != null && weaponDatabase != null)
            {
                // Loop through every category in the WeaponCategory enum automatically
                foreach (WeaponCategory category in System.Enum.GetValues(typeof(WeaponCategory)))
                {
                    WeaponEntry weapon = weaponDatabase.GetWeaponByCategory(category);

                    // If a weapon exists for this category in the database, add it to the wheel!
                    if (weapon != null)
                    {
                        WeaponWheelManager.Instance.AddWeaponToWheel(weapon);
                    }
                }
            }
        }

        private void InitializeStats()
        {
            currentHealth = playerStats.baseHealth;
            currentArmor = playerStats.baseArmor;

            laneDistance = playerStats.laneDistance;
            runtimeMovementSpeed = playerStats.movementSpeed * playerStats.moveSpeedMultiplier;
            runtimeStrafeSmoothing = playerStats.strafeAnimationSmoothing;

            UpdatePlayerUI();

            if (debugMode) Debug.Log($"[PlayerController] Initialized with {currentHealth} HP and {runtimeMovementSpeed} Speed.");
        }

        private void UpdatePlayerUI()
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateHealth(currentHealth, playerStats != null ? playerStats.baseHealth : 100f);
                UIManager.Instance.UpdateArmor(currentArmor);
            }
        }

        private void Update()
        {
            if (animator == null) return;

            if (weaponLayerIndex >= 0 && weaponLayerIndex < animator.layerCount)
            {
                currentWeaponState = animator.GetCurrentAnimatorStateInfo(weaponLayerIndex);
                UpdateCurrentWeaponData();
            }

            HandleMovement();
            HandleAiming();
            HandleActions(); // The bulletproof state machine
            UpdateLayerWeights();
        }

        private void UpdateCurrentWeaponData()
        {
            if (weaponDatabase != null && weaponLayerIndex != lastWeaponLayerIndex)
            {
                lastWeaponLayerIndex = weaponLayerIndex;
                currentWeaponData = weaponDatabase.GetEntryByLayer(weaponLayerIndex);
                SpawnWeaponModel();
            }
        }

        private void SpawnWeaponModel()
        {
            if (currentWeaponInstance != null) Destroy(currentWeaponInstance);

            if (currentWeaponData == null || currentWeaponData.weaponPrefab == null || weaponSocket == null) return;

            currentWeaponInstance = Instantiate(currentWeaponData.weaponPrefab.gameObject, weaponSocket);
            currentWeaponInstance.transform.localPosition = currentWeaponData.holdPosition;
            currentWeaponInstance.transform.localRotation = Quaternion.Euler(currentWeaponData.holdRotation);
            currentWeaponInstance.transform.localScale = currentWeaponData.localScale;

            currentAmmo = currentWeaponData.magSize;
            UpdateAmmoUI();

            if (debugMode) Debug.Log($"[PlayerController] Spawned weapon: {currentWeaponData.weaponName}");
        }

        private void HandleMovement()
        {
            float targetX = currentLane * laneDistance;
            Vector3 pos = transform.position;
            pos.x = Mathf.MoveTowards(pos.x, targetX, Time.deltaTime * runtimeMovementSpeed);
            transform.position = pos;

            float moveDelta = targetX - pos.x;
            float animationTarget = (Mathf.Abs(moveDelta) > 0.01f) ? Mathf.Sign(moveDelta) : 0f;
            currentStrafeAnim = Mathf.MoveTowards(currentStrafeAnim, animationTarget, Time.deltaTime * runtimeStrafeSmoothing);
            animator.SetFloat(strafeParamHash, currentStrafeAnim);
        }

        private void HandleAiming()
        {
            if (animator == null) return;

            bool isAiming = TouchManager.IsShooting;

            if (isAiming && aimTarget != null)
            {
                Vector3 origin = (aimOrigin != null) ? aimOrigin.position : transform.position + Vector3.up * 1.5f;
                Vector3 directionToTarget = (aimTarget.position - origin).normalized;
                Vector3 localDir = transform.InverseTransformDirection(directionToTarget);

                float yaw = Mathf.Atan2(localDir.x, localDir.z) * Mathf.Rad2Deg;
                float pitch = Mathf.Atan2(localDir.y, new Vector2(localDir.x, localDir.z).magnitude) * Mathf.Rad2Deg;

                targetAimH = Mathf.Clamp(-yaw / 45f, -1f, 1f);
                targetAimV = Mathf.Clamp(pitch / 30f, -1f, 1f);
            }

            currentAimH = Mathf.MoveTowards(currentAimH, targetAimH, Time.deltaTime * 15f);
            currentAimV = Mathf.MoveTowards(currentAimV, targetAimV, Time.deltaTime * 15f);

            debugAimH = currentAimH;
            debugAimV = currentAimV;

            animator.SetFloat(aimHorizontalHash, currentAimH);
            animator.SetFloat(aimVerticalHash, currentAimV);
        }

        private void HandleActions()
        {
            bool shootingInput = TouchManager.IsShooting;

            // We capture the swipe request here, then immediately clear the original flag
            bool reloadPressed = touchReloadRequested;
            touchReloadRequested = false;

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame) reloadPressed = true;
#endif

            // 1. DYNAMIC SPEED SYNC (The "Spin-Up" Fix)
            if (currentWeaponData != null)
            {
                float targetFireSpeedMult = currentWeaponData.fireRate / 5f;
                float currentDynamicFireRate = 1f; // Default speed for quick taps

                if (shootingInput)
                {
                    if (currentWeaponData.fireMode == WeaponFireMode.Auto)
                    {
                        // Ramp up from 1x to Max Speed over 0.3 seconds
                        float rampProgress = Mathf.Clamp01(TouchManager.TouchHoldTime / 0.3f);
                        currentDynamicFireRate = Mathf.Lerp(1f, targetFireSpeedMult, rampProgress);
                    }
                    else
                    {
                        // Single and Burst fire modes stay at a readable speed
                        currentDynamicFireRate = Mathf.Min(targetFireSpeedMult, 1.5f);
                    }
                }

                animator.SetFloat(fireMultiplierParamHash, currentDynamicFireRate);
                animator.SetFloat(reloadMultiplierParamHash, currentWeaponData.reloadSpeedMult);
            }

            // 2. HARD LOCKOUT: Reloading
            if (IsReloadingAnimationPlaying())
            {
                animator.SetBool(fireParamHash, false);
                targetLayerWeight = 1f;
                return;
            }

            // 3. AUTO-RELOAD
            if (currentAmmo <= 0 && currentWeaponData != null)
            {
                animator.SetBool(fireParamHash, false);
                animator.SetBool(reloadParamHash, true);
                Invoke(nameof(ResetReloadParameter), 0.15f);
                targetLayerWeight = 1f;
                return;
            }

            // 4. MANUAL RELOAD (Quick Swipe Logic)
            int maxAmmo = currentWeaponData != null ? currentWeaponData.magSize : 0;

            // FIX: Use the 'reloadPressed' variable we captured at the top!
            if (reloadPressed && currentAmmo < maxAmmo)
            {
                // Allow reload if they are NOT holding the screen, OR if it's a very quick swipe (< 0.4s)
                if (!shootingInput || TouchManager.TouchHoldTime < 0.4f)
                {
                    animator.SetBool(fireParamHash, false);
                    animator.SetBool(reloadParamHash, true);
                    Invoke(nameof(ResetReloadParameter), 0.15f);
                    targetLayerWeight = 1f;
                    return;
                }
            }

            // 5. THE FIRING OVERRIDE (With Fire Modes!)
            if (shootingInput && currentWeaponData != null)
            {
                bool canShoot = true;

                if (currentWeaponData.fireMode == WeaponFireMode.Single && shotsFiredThisTriggerPull >= 1)
                    canShoot = false;

                if (currentWeaponData.fireMode == WeaponFireMode.Burst && shotsFiredThisTriggerPull >= currentWeaponData.burstCount)
                    canShoot = false;

                // THE "TAP CAP": Auto mode single-shot
                if (currentWeaponData.fireMode == WeaponFireMode.Auto && TouchManager.TouchHoldTime < 0.2f && shotsFiredThisTriggerPull >= 1)
                    canShoot = false;

                animator.SetBool(fireParamHash, canShoot);

                if (fireCooldownTimer > 0f) fireCooldownTimer -= Time.deltaTime;
                if (fireCooldownTimer <= 0f)
                {
                    fireCooldownTimer = 1.0f / currentWeaponData.fireRate;
                }

                targetLayerWeight = 1f;
                return;
            }

            // 6. TRIGGER RELEASED
            animator.SetBool(fireParamHash, false);
            shotsFiredThisTriggerPull = 0;
            targetLayerWeight = 1f;
        }

        private bool IsReloadingAnimationPlaying()
        {
            if (animator == null || weaponLayerIndex < 0 || weaponLayerIndex >= animator.layerCount) return false;

            bool isInReload = currentWeaponState.shortNameHash == reloadStateHash || currentWeaponState.tagHash == reloadTagHash;
            bool isTransitioning = animator.IsInTransition(weaponLayerIndex);

            if (isTransitioning)
            {
                AnimatorStateInfo nextState = animator.GetNextAnimatorStateInfo(weaponLayerIndex);
                if (nextState.shortNameHash == reloadStateHash || nextState.tagHash == reloadTagHash) return true;
                if (isInReload) return true;
            }

            if (isInReload) return currentWeaponState.normalizedTime < 0.99f;

            return false;
        }

        private void ResetReloadParameter() => animator.SetBool(reloadParamHash, false);

        private void UpdateLayerWeights()
        {
            if (animator == null) return;

            for (int i = 1; i < animator.layerCount; i++)
            {
                float target = (i == weaponLayerIndex || i == aimLayerIndex) ? targetLayerWeight : 0f;
                float current = animator.GetLayerWeight(i);
                if (!Mathf.Approximately(current, target))
                {
                    animator.SetLayerWeight(i, Mathf.MoveTowards(current, target, Time.deltaTime * layerBlendSpeed));
                }
            }
        }

        public void isFiring(AnimationEvent ae)
        {
            if (currentWeaponData == null || currentAmmo <= 0 || IsReloadingAnimationPlaying()) return;

            // THE HARD BLOCKER: If the animator glitches and tries to loop too fast, 
            // this physically stops the bullet from spawning!
            if (currentWeaponData.fireMode == WeaponFireMode.Single && shotsFiredThisTriggerPull >= 1) return;
            if (currentWeaponData.fireMode == WeaponFireMode.Burst && shotsFiredThisTriggerPull >= currentWeaponData.burstCount) return;

            // Log that a successful shot happened
            shotsFiredThisTriggerPull++;

            currentAmmo--;
            UpdateAmmoUI();

            if (audioSource != null && currentWeaponData.audioFire != null)
            {
                audioSource.pitch = Random.Range(0.95f, 1.05f);
                audioSource.PlayOneShot(currentWeaponData.audioFire, weaponVolume);
            }

            if (currentWeaponInstance != null)
            {
                Vector3 worldMuzzlePos = currentWeaponInstance.transform.TransformPoint(currentWeaponData.muzzlePosition);
                Quaternion worldMuzzleRot = currentWeaponInstance.transform.rotation * Quaternion.Euler(currentWeaponData.muzzleRotation);

                if (currentWeaponData.muzzleFlash != null)
                {
                    GameObject flashPrefab = currentWeaponData.muzzleFlash.gameObject;
                    GameObject flash = PoolManager.Instance.SpawnFromPool(flashPrefab, worldMuzzlePos, worldMuzzleRot, currentWeaponInstance.transform);
                    PoolManager.Instance.ReturnToPoolAfterDelay(flash, flashPrefab, currentWeaponData.flashLifetime);
                }

                PerformRaycastHit(worldMuzzlePos, worldMuzzleRot);
            }
        }

        private void PerformRaycastHit(Vector3 origin, Quaternion rotation)
        {
            if (currentWeaponData == null) return;

            Vector2 sPoint = TouchManager.CurrentTouchPosition;

            if (Camera.main == null) return;
            Ray ray = Camera.main.ScreenPointToRay(sPoint);

            if (debugMode) Debug.DrawRay(ray.origin, ray.direction * currentWeaponData.range, Color.red, 1.0f);

            if (Physics.Raycast(ray, out RaycastHit hit, currentWeaponData.range, hitMask, QueryTriggerInteraction.Collide))
            {
                if (debugMode) Debug.Log($"<color=cyan>[Combat]</color> HIT: {hit.collider.name} | Damage: {currentWeaponData.baseDamage}");

                if (impactEffect != null)
                {
                    GameObject impactPrefab = impactEffect.gameObject;
                    GameObject impact = PoolManager.Instance.SpawnFromPool(impactPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                    PoolManager.Instance.ReturnToPoolAfterDelay(impact, impactPrefab, currentWeaponData.impactLifetime);
                }

                EnemyController enemy = hit.collider.GetComponent<EnemyController>();
                if (enemy == null) enemy = hit.collider.GetComponentInParent<EnemyController>();
                if (enemy != null) enemy.TakeDamage(currentWeaponData.baseDamage);
            }
        }

        public void Reload(AnimationEvent ae)
        {
            if (audioSource == null || currentWeaponData == null) return;

            if (ae.stringParameter == "MagOut" && currentWeaponData.audioMagOut != null) audioSource.PlayOneShot(currentWeaponData.audioMagOut, weaponVolume);
            else if (ae.stringParameter == "MagIn" && currentWeaponData.audioMagIn != null) audioSource.PlayOneShot(currentWeaponData.audioMagIn, weaponVolume);

            if (ae.stringParameter == "Refill" || ae.stringParameter == "MagIn")
            {
                currentAmmo = currentWeaponData.magSize;
                UpdateAmmoUI();
            }
        }

        private void UpdateAmmoUI()
        {
            if (UIManager.Instance != null && currentWeaponData != null)
            {
                UIManager.Instance.UpdateAmmo(currentAmmo, currentWeaponData.magSize);
            }
        }

        public void TakeDamage(float damage)
        {
            if (Time.time < lastDamageTime + iFrameDuration) return;
            lastDamageTime = Time.time;

            if (currentArmor > 0)
            {
                float armorDamage = Mathf.Min(currentArmor, damage);
                currentArmor -= armorDamage;
                damage -= armorDamage;
            }

            if (damage > 0) currentHealth -= damage;

            currentHealth = Mathf.Max(currentHealth, 0);
            UpdatePlayerUI();

            if (debugMode) Debug.Log($"<color=orange>[Player]</color> Received {damage} damage! Remaining HP: {currentHealth}, Armor: {currentArmor}");

            if (currentHealth <= 0)
            {
                // Handle death logic here
            }
        }

        private void MoveLeft() => currentLane = Mathf.Clamp(currentLane + 1, -1, 1);
        private void MoveRight() => currentLane = Mathf.Clamp(currentLane - 1, -1, 1);

        public void EquipWeaponFromWheel(WeaponEntry selectedWeapon)
        {
            if (selectedWeapon == null) return;
            weaponLayerIndex = selectedWeapon.animatorLayer;
        }
    }
}