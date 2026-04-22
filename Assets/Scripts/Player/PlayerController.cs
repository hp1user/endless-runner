using UnityEngine;
using System.Collections.Generic;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using Enemy.Control;
using UI.Control;

namespace Player.Control
{
    public class PlayerController : MonoBehaviour
    {
        private Animator animator;

        [Header("Stat Configuration")]
        public PlayerDatabase playerStats;

        [Header("Movement Settings")]
        public float laneDistance = 2.0f;
        public float movementSpeed = 10f;
        public float strafeAnimationSmoothing = 8f;

        [Header("Weapon Layer Settings")]
        public int weaponLayerIndex = 1;
        public float layerBlendSpeed = 10f;

        [Header("Aiming Settings")]
        public Transform aimOrigin;
        public Transform aimTarget;
        public int aimLayerIndex = 1;

        [Header("Weapon System")]
        public WeaponDatabase weaponDatabase;
        public Transform weaponSocket;
        [Tooltip("How many backup magazines does a newly unlocked gun come with?")]
        public int startingReserveMags = 3;

        [Header("Effects Settings")]
        public Transform impactEffect;
        [Range(0f, 1f)]
        public float weaponVolume = 0.5f;

        private LayerMask hitMask => (playerStats != null) ? playerStats.EnemyLayer : (LayerMask)LayerMask.GetMask("Enemy");

        private WeaponEntry currentWeaponData;
        private GameObject currentWeaponInstance;
        private int lastWeaponLayerIndex = -1;

        // Animator Hashes
        private int strafeParamHash = Animator.StringToHash("Starf");
        private int reloadParamHash = Animator.StringToHash("Reload");
        private int fireParamHash = Animator.StringToHash("isFiring");
        private int fireMultiplierParamHash = Animator.StringToHash("FireSpeedMultiplier");
        private int reloadMultiplierParamHash = Animator.StringToHash("ReloadSpeedMultiplier");
        private int aimHorizontalHash = Animator.StringToHash("Aim_Horizontal");
        private int aimVerticalHash = Animator.StringToHash("Aim_Vertical");
        private int reloadStateHash = Animator.StringToHash("Reload");
        private int reloadTagHash = Animator.StringToHash("Reload");

        private AudioSource audioSource;

        [Header("Debug")]
        public bool debugMode = false;

        // --- NEW INVENTORY & AMMO STATE ---
        private List<WeaponEntry> unlockedWeapons = new List<WeaponEntry>();
        // Tracks how much backup ammo we have for each category
        private Dictionary<WeaponCategory, int> reserveAmmo = new Dictionary<WeaponCategory, int>();
        // Tracks exactly how many bullets are currently loaded in the magazine of each specific gun
        private Dictionary<string, int> loadedAmmo = new Dictionary<string, int>();

        private int currentLane = 0;
        private float currentStrafeAnim;
        private float currentAimH, currentAimV;
        private float targetAimH, targetAimV;
        private float targetLayerWeight = 1f;
        private float fireCooldownTimer;
        private AnimatorStateInfo currentWeaponState;

        private int currentAmmo;
        private int shotsFiredThisTriggerPull = 0;
        private float currentHealth;
        private float currentArmor;
        private float runtimeMovementSpeed;
        private float runtimeStrafeSmoothing;

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

        private void RequestReload() => touchReloadRequested = true;

        private void Awake()
        {
            Instance = this;
            animator = GetComponent<Animator>();
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

            if (playerStats != null) InitializeStats();
            else
            {
                laneDistance = 2.0f;
                movementSpeed = 10f;
                strafeAnimationSmoothing = 8f;
            }

            // --- SETUP AMMO BACKPACK ---
            foreach (WeaponCategory cat in System.Enum.GetValues(typeof(WeaponCategory)))
            {
                reserveAmmo[cat] = 0; // Start with 0 ammo for everything
            }

            // Give the player unlimited pistol ammo so they are never completely defenseless
            reserveAmmo[WeaponCategory.Pistol] = 9999;

            if (weaponDatabase != null)
            {
                WeaponEntry starterPistol = weaponDatabase.GetWeaponByID("D Eagle");
                if (starterPistol != null)
                {
                    unlockedWeapons.Add(starterPistol);
                    if (WeaponWheelManager.Instance != null) WeaponWheelManager.Instance.AddWeaponToWheel(starterPistol);
                    weaponLayerIndex = starterPistol.animatorLayer;

                    //reserveAmmo[starterPistol.category] += starterPistol.magSize * startingReserveMags;
                    // Give the player unlimited pistol ammo so they are never completely defenseless
                    reserveAmmo[starterPistol.category] = 999;
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
            HandleActions();
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

            // --- AMMO MEMORY LOGIC ---
            if (!loadedAmmo.ContainsKey(currentWeaponData.weaponID))
            {
                // First time equipping this gun? Give it a free full magazine to start!
                loadedAmmo[currentWeaponData.weaponID] = currentWeaponData.magSize;
            }

            // Pull the exact bullet count from memory
            currentAmmo = loadedAmmo[currentWeaponData.weaponID];
            UpdateAmmoUI();
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
            bool isAiming = TouchManager.IsShooting;
            if (isAiming && aimTarget != null)
            {
                Vector3 origin = (aimOrigin != null) ? aimOrigin.position : transform.position + Vector3.up * 1.5f;
                Vector3 localDir = transform.InverseTransformDirection((aimTarget.position - origin).normalized);

                float yaw = Mathf.Atan2(localDir.x, localDir.z) * Mathf.Rad2Deg;
                float pitch = Mathf.Atan2(localDir.y, new Vector2(localDir.x, localDir.z).magnitude) * Mathf.Rad2Deg;

                targetAimH = Mathf.Clamp(-yaw / 45f, -1f, 1f);
                targetAimV = Mathf.Clamp(pitch / 30f, -1f, 1f);
            }

            currentAimH = Mathf.MoveTowards(currentAimH, targetAimH, Time.deltaTime * 15f);
            currentAimV = Mathf.MoveTowards(currentAimV, targetAimV, Time.deltaTime * 15f);

            animator.SetFloat(aimHorizontalHash, currentAimH);
            animator.SetFloat(aimVerticalHash, currentAimV);
        }

        private void HandleActions()
        {
            bool shootingInput = TouchManager.IsShooting;
            bool reloadPressed = touchReloadRequested;
            touchReloadRequested = false;

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame) reloadPressed = true;
#endif

            if (currentWeaponData != null)
            {
                float targetFireSpeedMult = currentWeaponData.fireRate / 5f;
                float currentDynamicFireRate = 1f;

                if (shootingInput)
                {
                    if (currentWeaponData.fireMode == WeaponFireMode.Auto)
                    {
                        float rampProgress = Mathf.Clamp01(TouchManager.TouchHoldTime / 0.3f);
                        currentDynamicFireRate = Mathf.Lerp(1f, targetFireSpeedMult, rampProgress);
                    }
                    else
                    {
                        currentDynamicFireRate = Mathf.Min(targetFireSpeedMult, 1.5f);
                    }
                }

                animator.SetFloat(fireMultiplierParamHash, currentDynamicFireRate);
                animator.SetFloat(reloadMultiplierParamHash, currentWeaponData.reloadSpeedMult);
            }

            if (IsReloadingAnimationPlaying())
            {
                animator.SetBool(fireParamHash, false);
                targetLayerWeight = 1f;
                return;
            }

            int currentReserve = currentWeaponData != null ? reserveAmmo[currentWeaponData.category] : 0;
            int maxAmmo = currentWeaponData != null ? currentWeaponData.magSize : 0;

            // AUTO-RELOAD (Only if we have reserve ammo left!)
            if (currentAmmo <= 0 && currentWeaponData != null)
            {
                animator.SetBool(fireParamHash, false);

                if (currentReserve > 0)
                {
                    animator.SetBool(reloadParamHash, true);
                    Invoke(nameof(ResetReloadParameter), 0.15f);
                }
                else if (shootingInput && debugMode)
                {
                    Debug.Log("<color=red>OUT OF AMMO!</color> Need to pick up a supply crate!");
                    // TODO: Play a "Click Click" empty mag sound here!
                }

                targetLayerWeight = 1f;
                return;
            }

            // MANUAL RELOAD (Swipe up)
            if (reloadPressed && currentAmmo < maxAmmo && currentReserve > 0)
            {
                if (!shootingInput || TouchManager.TouchHoldTime < 0.4f)
                {
                    animator.SetBool(fireParamHash, false);
                    animator.SetBool(reloadParamHash, true);
                    Invoke(nameof(ResetReloadParameter), 0.15f);
                    targetLayerWeight = 1f;
                    return;
                }
            }

            if (shootingInput && currentWeaponData != null)
            {
                bool canShoot = true;

                if (currentWeaponData.fireMode == WeaponFireMode.Single && shotsFiredThisTriggerPull >= 1) canShoot = false;
                if (currentWeaponData.fireMode == WeaponFireMode.Burst && shotsFiredThisTriggerPull >= currentWeaponData.burstCount) canShoot = false;
                if (currentWeaponData.fireMode == WeaponFireMode.Auto && TouchManager.TouchHoldTime < 0.2f && shotsFiredThisTriggerPull >= 1) canShoot = false;

                animator.SetBool(fireParamHash, canShoot);

                if (fireCooldownTimer > 0f) fireCooldownTimer -= Time.deltaTime;
                if (fireCooldownTimer <= 0f) fireCooldownTimer = 1.0f / currentWeaponData.fireRate;

                targetLayerWeight = 1f;
                return;
            }

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

            if (currentWeaponData.fireMode == WeaponFireMode.Single && shotsFiredThisTriggerPull >= 1) return;
            if (currentWeaponData.fireMode == WeaponFireMode.Burst && shotsFiredThisTriggerPull >= currentWeaponData.burstCount) return;

            shotsFiredThisTriggerPull++;
            currentAmmo--;

            // Sync bullet count to memory so swapping weapons saves our state!
            loadedAmmo[currentWeaponData.weaponID] = currentAmmo;
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

            if (Physics.Raycast(ray, out RaycastHit hit, currentWeaponData.range, hitMask, QueryTriggerInteraction.Collide))
            {
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

            // --- THE NEW MATH: Pulling from Reserve Ammo ---
            if (ae.stringParameter == "Refill" || ae.stringParameter == "MagIn")
            {
                int bulletsNeeded = currentWeaponData.magSize - currentAmmo;
                int bulletsAvailable = reserveAmmo[currentWeaponData.category];

                // Take whichever is smaller: the bullets we need, or the bullets we actually have left
                int bulletsToLoad = Mathf.Min(bulletsNeeded, bulletsAvailable);

                currentAmmo += bulletsToLoad;
                reserveAmmo[currentWeaponData.category] -= bulletsToLoad; // Deduct from backpack!

                loadedAmmo[currentWeaponData.weaponID] = currentAmmo; // Sync to memory

                UpdateAmmoUI();
            }
        }

        private void UpdateAmmoUI()
        {
            if (UIManager.Instance != null && currentWeaponData != null)
            {
                int currentReserve = reserveAmmo[currentWeaponData.category];
                // Updates the UI to show: CurrentAmmo / ReserveAmmo
                UIManager.Instance.UpdateAmmo(currentAmmo, currentReserve);
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
        }

        private void MoveLeft() => currentLane = Mathf.Clamp(currentLane + 1, -1, 1);
        private void MoveRight() => currentLane = Mathf.Clamp(currentLane - 1, -1, 1);

        public void EquipWeaponFromWheel(WeaponEntry selectedWeapon)
        {
            if (selectedWeapon == null) return;
            weaponLayerIndex = selectedWeapon.animatorLayer;
        }

        // ==========================================
        // LOOT RECEIVER METHODS
        // ==========================================

        public void RestoreHealth(float amount)
        {
            float maxHealth = (playerStats != null) ? playerStats.baseHealth : 100f;
            currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
            UpdatePlayerUI();
        }

        public void AddArmor(float amount)
        {
            float maxArmor = (playerStats != null) ? playerStats.baseArmor : 50f;
            currentArmor = Mathf.Min(currentArmor + amount, maxArmor);
            UpdatePlayerUI();
        }

        public void AddAmmo(WeaponCategory category, int amount)
        {
            // 1. Add the bullets to the backpack
            reserveAmmo[category] += amount;

            // 2. If we are currently holding a gun of that category, update the UI instantly
            if (currentWeaponData != null && currentWeaponData.category == category)
            {
                UpdateAmmoUI();
                if (debugMode) Debug.Log($"<color=yellow>[Loot]</color> Picked up {amount} {category} ammo! Reserve is now: {reserveAmmo[category]}");
            }
            else if (debugMode)
            {
                Debug.Log($"<color=yellow>[Loot]</color> Picked up {category} ammo, safely stored in backpack.");
            }
        }

        public void UnlockWeapon(string incomingWeaponID)
        {
            if (weaponDatabase == null) return;

            WeaponEntry newGun = weaponDatabase.GetWeaponByID(incomingWeaponID);
            if (newGun == null) return;

            if (unlockedWeapons.Contains(newGun))
            {
                if (debugMode) Debug.Log($"<color=magenta>[Loot]</color> You already own the {newGun.weaponName}! Converting to ammo...");
                AddAmmo(newGun.category, newGun.magSize * 2);
                return;
            }

            unlockedWeapons.Add(newGun);

            if (WeaponWheelManager.Instance != null)
            {
                WeaponWheelManager.Instance.AddWeaponToWheel(newGun);
            }

            EquipWeaponFromWheel(newGun);
        }
    }
}