using UnityEngine;
using System.Collections.Generic;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using Enemy.Control;
using UI.Control;
using EndlessRunner.Data;

namespace Player.Control
{
    public class PlayerController : MonoBehaviour
    {
        private Animator animator;

        [Header("Character Profile")]
        public CharacterData currentCharacterData;

        [Header("Stat Configuration")]
        public PlayerDatabase playerStats;

        [Header("Movement Settings")]
        public float laneDistance = 2.0f;
        [Tooltip("Base speed for lane transitions.")]
        public float movementSpeed = 6.0f;
        public float strafeAnimationSmoothing = 8f;
        [Tooltip("If true, uses a smooth ease-in/ease-out transition between lanes instead of an abrupt linear step.")]
        public bool useSmoothLaneChange = true;
        private float laneChangeVelocity = 0f;

        [Header("Weapon Layer Settings")]
        public int weaponLayerIndex = 1;
        public float layerBlendSpeed = 10f;

        [Header("Aiming Settings")]
        public Transform aimOrigin;
        public Transform aimTarget;
        public int aimLayerIndex = 1;

        [Header("Weapon System")]
        public Transform weaponSocket;
        [Tooltip("How many backup magazines does a newly unlocked gun come with?")]
        public int startingReserveMags = 3;

        [Header("Effects Settings")]
        public Transform impactEffect;
        public AudioClip[] hitMarkerSounds;
        [Range(0f, 1f)]
        public float weaponVolume = 0.5f;

        [Header("Roguelike Upgrades")]
        public float damageMultiplier = 1.0f;
        public float armorPerKill = 0f;

        [Header("Skills and Ultimates")]
        public SkillData equippedSkill;
        public SkillData equippedUltimate;

        private LayerMask hitMask => (playerStats != null) ? playerStats.EnemyLayer : (LayerMask)LayerMask.GetMask("Enemy");

        private WeaponData currentWeaponData;
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
        private List<WeaponData> unlockedWeapons = new List<WeaponData>();
        // Tracks how much backup ammo we have for each category
        private Dictionary<WeaponCategory, int> reserveAmmo = new Dictionary<WeaponCategory, int>();
        // Tracks exactly how many bullets are currently loaded in the magazine of each specific gun
        private Dictionary<string, int> loadedAmmo = new Dictionary<string, int>();

        private int currentLane = 0;
        private float currentStrafeAnim;
        private float currentAimH, currentAimV;
        private float targetAimH, targetAimV;
        private float targetLayerWeight = 1f;
        private float fireCooldownTimer = 0f;
        private float fireAnimTimer = 0f;
        private float triggerHoldTimer = 0f;
        [Header("Firing Timing Settings")]
        [Tooltip("Delay in seconds of holding before continuous auto-fire starts. Below this threshold, only 1 single bullet is fired per tap/touch.")]
        public float autoFireHoldDelay = 0.22f;

        public bool IsChangingLane
        {
            get
            {
                float targetX = currentLane * laneDistance;
                return Mathf.Abs(targetX - transform.position.x) > 0.05f;
            }
        }
        private AnimatorStateInfo currentWeaponState;

        private int currentAmmo;
        private int shotsFiredThisTriggerPull = 0;
        private bool wasShootingLastFrame = false;
        private float lastShotTime = -999f;
        private float currentHealth;
        private float runtimeMaxHealth;
        private float currentArmor;
        private float runtimeMovementSpeed;
        private float runtimeStrafeSmoothing;

        private float iFrameDuration = 0.5f;
        private float lastDamageTime = -999f;
        private bool touchReloadRequested = false;

        // Skill Tracking
        private float currentSkillCooldownTimer = 0f;
        private float currentUltimateKillCount = 0f;
        private float activeRapidFireTimer = 0f;
        private bool isRapidFireActive = false;

        public static PlayerController Instance { get; private set; }
        public bool isDead { get; private set; } = false;
        public static event System.Action OnPlayerDeath;

        private void OnEnable()
        {
            TouchManager.OnSwipeLeft += MoveLeft;
            TouchManager.OnSwipeRight += MoveRight;
            TouchManager.OnSwipeUp += RequestReload;
            GameManager.OnEnemyKilled += HandleEnemyKilled;
        }

        private void OnDisable()
        {
            TouchManager.OnSwipeLeft -= MoveLeft;
            TouchManager.OnSwipeRight -= MoveRight;
            TouchManager.OnSwipeUp -= RequestReload;
            GameManager.OnEnemyKilled -= HandleEnemyKilled;
        }

        private void HandleEnemyKilled()
        {
            if (armorPerKill > 0f && !isDead)
            {
                AddArmor(armorPerKill);
                if (debugMode) Debug.Log($"<color=cyan>[Armor Siphon]</color> +{armorPerKill} Armor on kill! Current Armor: {currentArmor}");
            }

            if (equippedUltimate != null && equippedUltimate.isKillBasedRecharge)
            {
                if (currentUltimateKillCount < equippedUltimate.rechargeRequirement)
                {
                    currentUltimateKillCount++;
                    if (debugMode && currentUltimateKillCount >= equippedUltimate.rechargeRequirement)
                    {
                        Debug.Log("<color=cyan>[Ultimate]</color> Ultimate is READY!");
                    }
                }
            }
        }

        private void RequestReload() => touchReloadRequested = true;

        private void Awake()
        {
            Instance = this;
            animator = GetComponent<Animator>();
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

            if (currentCharacterData != null)
            {
                ApplyCharacterData(currentCharacterData, false, false);
            }
            else if (playerStats != null)
            {
                InitializeStats();
            }
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

            if (WeaponDatabase.Instance != null)
            {
                WeaponData starterPistol = WeaponDatabase.Instance.GetWeaponByID("D Eagle");
                if (starterPistol != null)
                {
                    unlockedWeapons.Add(starterPistol);
                    if (WeaponWheelToolkitManager.Instance != null) WeaponWheelToolkitManager.Instance.AddWeaponToWheel(starterPistol);
                    weaponLayerIndex = starterPistol.animatorLayer;

                    //reserveAmmo[starterPistol.category] += starterPistol.magSize * startingReserveMags;
                    // Give the player unlimited pistol ammo so they are never completely defenseless
                    reserveAmmo[starterPistol.category] = 999;
                }
            }
        }

        public void InitializeStats()
        {
            if (playerStats == null) return;
            currentHealth = playerStats.baseHealth;
            runtimeMaxHealth = playerStats.baseHealth;
            currentArmor = playerStats.baseArmor;
            laneDistance = playerStats.laneDistance;
            runtimeMovementSpeed = playerStats.movementSpeed * playerStats.moveSpeedMultiplier;
            runtimeStrafeSmoothing = playerStats.strafeAnimationSmoothing;
            UpdatePlayerUI();
        }

        /// <summary>
        /// Applies all attributes from a CharacterData profile (stats, skills, and model) to this PlayerController.
        /// </summary>
        public void ApplyCharacterData(CharacterData character, bool updateModel = true, bool respawnWeapon = true)
        {
            if (character == null) return;
            currentCharacterData = character;

            // 1. Assign Skills
            equippedSkill = character.activeSkill;
            equippedUltimate = character.activeUltimate;

            // 2. Assign Stats
            if (playerStats != null)
            {
                playerStats.baseHealth = character.baseHealth;
                playerStats.baseArmor = character.baseArmor;
                playerStats.movementSpeed = character.movementSpeed;
                playerStats.moveSpeedMultiplier = character.moveSpeedMultiplier;
                playerStats.strafeAnimationSmoothing = character.strafeAnimationSmoothing;
                playerStats.laneDistance = character.laneDistance;
            }

            laneDistance = character.laneDistance;
            movementSpeed = character.movementSpeed;
            runtimeMovementSpeed = character.movementSpeed * character.moveSpeedMultiplier;
            runtimeStrafeSmoothing = character.strafeAnimationSmoothing;
            currentHealth = character.baseHealth;
            runtimeMaxHealth = character.baseHealth;
            currentArmor = character.baseArmor;

            UpdatePlayerUI();

            // 3. Update 3D Model & Rigging
            if (updateModel && character.modelPrefab != null)
            {
                SwapCharacterModel(character);
            }

            // 4. Respawn Weapon if playing
            if (respawnWeapon && currentWeaponData != null)
            {
                SpawnWeaponModel();
            }

            Debug.Log($"<color=green>[PlayerController]</color> Applied character profile '{character.characterName}' (HP: {character.baseHealth}, Armor: {character.baseArmor}, Speed: {character.movementSpeed}).");
        }

        /// <summary>
        /// Replaces the current visual 3D model with the one specified in the CharacterData.
        /// </summary>
        public void SwapCharacterModel(CharacterData character)
        {
            if (character == null || character.modelPrefab == null) return;

#if UNITY_EDITOR
            if (!Application.isPlaying && UnityEditor.PrefabUtility.IsPartOfPrefabInstance(gameObject))
            {
                UnityEditor.PrefabUtility.UnpackPrefabInstance(gameObject, UnityEditor.PrefabUnpackMode.Completely, UnityEditor.InteractionMode.AutomatedAction);
            }
#endif

            Transform aimTrans = transform.Find("Aim");
            if (aimTrans == null)
            {
                GameObject aimGo = new GameObject("Aim");
                aimGo.transform.SetParent(transform, false);
                aimGo.transform.localPosition = new Vector3(0, 1.618f, 1.144f);
                aimTrans = aimGo.transform;
            }
            aimTarget = aimTrans;

            // Remove existing model children (preserve Aim)
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                if (child != aimTrans)
                {
#if UNITY_EDITOR
                    if (!Application.isPlaying)
                        DestroyImmediate(child.gameObject);
                    else
                        Destroy(child.gameObject);
#else
                    Destroy(child.gameObject);
#endif
                }
            }

            GameObject sourceToInstantiate = character.modelPrefab;

            // If modelPrefab is a configured character prefab containing its own PlayerController,
            // find its visual model child instead of nesting another PlayerController!
            if (sourceToInstantiate.GetComponent<PlayerController>() != null)
            {
                foreach (Transform child in sourceToInstantiate.transform)
                {
                    if (child.name != "Aim" && (child.GetComponentInChildren<SkinnedMeshRenderer>() != null || child.Find("mixamorig:Hips") != null))
                    {
                        sourceToInstantiate = child.gameObject;
                        break;
                    }
                }
            }

            // Instantiate new model under player
            GameObject modelInst = Instantiate(sourceToInstantiate, transform);
            modelInst.name = "Character_Model";
            modelInst.transform.localPosition = Vector3.zero;
            modelInst.transform.localRotation = Quaternion.identity;
            modelInst.transform.localScale = Vector3.one;

            // Remove nested Animator from model child if present
            Animator childAnim = modelInst.GetComponent<Animator>();
            if (childAnim != null)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying) DestroyImmediate(childAnim);
                else Destroy(childAnim);
#else
                Destroy(childAnim);
#endif
            }

            // Find mixamorig:RightHand and create Weapon_socket
            Transform rHand = null;
            foreach (Transform t in modelInst.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "mixamorig:RightHand")
                {
                    rHand = t;
                    break;
                }
            }

            if (rHand != null)
            {
                GameObject socketGo = new GameObject("Weapon_socket");
                socketGo.transform.SetParent(rHand, false);
                socketGo.transform.localPosition = Vector3.zero;
                socketGo.transform.localRotation = Quaternion.identity;
                socketGo.transform.localScale = Vector3.one;
                weaponSocket = socketGo.transform;
            }

            // Setup Root Animator
            if (animator == null) animator = GetComponent<Animator>();
            if (animator != null)
            {
                Avatar avatar = character.GetAvatar();
                if (avatar != null) animator.avatar = avatar;

                if (character.customAnimatorController != null)
                {
                    animator.runtimeAnimatorController = character.customAnimatorController;
                }
                else if (animator.runtimeAnimatorController == null)
                {
#if UNITY_EDITOR
                    var defaultCtrl = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/3d/Characters/Rough/Animations/Controller/Man Controller.controller");
                    if (defaultCtrl != null) animator.runtimeAnimatorController = defaultCtrl;
#endif
                }
                animator.applyRootMotion = false;
                animator.Rebind();
                animator.Update(0f);
            }

            // Update AimTargetController
            var aimCtrl = GetComponent<Animation.Tools.AimTargetController>();
            if (aimCtrl != null)
            {
                aimCtrl.targetTransform = aimTrans;
                aimCtrl.rayOriginOverride = weaponSocket;
            }

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                UnityEditor.SceneView.RepaintAll();
            }
#endif
        }
        private void UpdatePlayerUI()
        {
            if (UIManager.Instance != null)
            {
                float maxHP = (playerStats != null) ? runtimeMaxHealth : 100f;
                float maxArmor = (playerStats != null) ? playerStats.baseArmor : 50f;
                if (currentCharacterData != null)
                {
                    maxHP = currentCharacterData.baseHealth;
                    maxArmor = currentCharacterData.baseArmor;
                }
                UIManager.Instance.UpdateHealth(currentHealth, maxHP);
                UIManager.Instance.UpdateArmor(currentArmor, maxArmor);
            }
        }

        private void Update()
        {
            if (isDead) return;

            if (animator == null) return;

            if (weaponLayerIndex >= 0 && weaponLayerIndex < animator.layerCount)
            {
                currentWeaponState = animator.GetCurrentAnimatorStateInfo(weaponLayerIndex);
                UpdateCurrentWeaponData();
            }

            HandleMovement();
            HandleAiming();
            HandleActions();
            HandleWeaponHotkeys();
            UpdateLayerWeights();
            UpdateSkills();
        }

        private void UpdateSkills()
        {
            // Time-based skill cooldown
            if (equippedSkill != null && !equippedSkill.isKillBasedRecharge && currentSkillCooldownTimer > 0)
            {
                currentSkillCooldownTimer -= Time.deltaTime;
            }

            // Rapid Fire duration tracking
            if (isRapidFireActive)
            {
                activeRapidFireTimer -= Time.deltaTime;
                if (activeRapidFireTimer <= 0)
                {
                    isRapidFireActive = false;
                    if (debugMode) Debug.Log("<color=green>[Skill]</color> Rapid Fire ended.");
                }
            }
        }

        private void UpdateCurrentWeaponData()
        {
            if (WeaponDatabase.Instance != null && weaponLayerIndex != lastWeaponLayerIndex)
            {
                lastWeaponLayerIndex = weaponLayerIndex;
                currentWeaponData = WeaponDatabase.Instance.GetEntryByLayer(weaponLayerIndex);
                SpawnWeaponModel();
            }
        }

        public void SpawnWeaponModel()
        {
            if (currentWeaponInstance != null)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying) DestroyImmediate(currentWeaponInstance);
                else Destroy(currentWeaponInstance);
#else
                Destroy(currentWeaponInstance);
#endif
            }
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

            if (useSmoothLaneChange)
            {
                // Smooth ease-in / ease-out transition (approx 0.28s - 0.35s)
                float effectiveSpeed = Mathf.Max(runtimeMovementSpeed, 1f);
                float smoothTime = Mathf.Clamp(1.8f / effectiveSpeed, 0.18f, 0.38f);
                pos.x = Mathf.SmoothDamp(pos.x, targetX, ref laneChangeVelocity, smoothTime, effectiveSpeed * 1.5f, Time.deltaTime);
            }
            else
            {
                pos.x = Mathf.MoveTowards(pos.x, targetX, Time.deltaTime * runtimeMovementSpeed);
            }
            transform.position = pos;

            float moveDelta = targetX - pos.x;
            float animationTarget = (Mathf.Abs(moveDelta) > 0.05f) ? Mathf.Sign(moveDelta) : 0f;
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
            // 1. Update cooldown and animation timers every frame
            if (fireCooldownTimer > 0f) fireCooldownTimer -= Time.deltaTime;
            if (fireAnimTimer > 0f) fireAnimTimer -= Time.deltaTime;

            bool isTouching = TouchManager.IsShooting || TouchManager.WasTapThisFrame;
            bool shootingInput = isTouching || isRapidFireActive;
            bool reloadPressed = touchReloadRequested;
            touchReloadRequested = false;

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame) reloadPressed = true;
#endif

            // 2. Weapon dynamic fire/reload multiplier animation updates
            if (currentWeaponData != null)
            {
                float targetFireSpeedMult = currentWeaponData.fireRate / 5f;
                float currentDynamicFireRate = targetFireSpeedMult;

                if (shootingInput)
                {
                    // Apply Rapid Fire Buff
                    if (isRapidFireActive && equippedSkill != null && equippedSkill.skillType == SkillType.RapidFire)
                    {
                        currentDynamicFireRate *= equippedSkill.effectValue;
                    }
                }

                animator.SetFloat(fireMultiplierParamHash, currentDynamicFireRate);
                animator.SetFloat(reloadMultiplierParamHash, currentWeaponData.reloadSpeedMult);
            }

            // 3. RELOAD LOGIC (Always evaluated so manual swipe-up and auto-reloads execute reliably!)
            if (IsReloadingAnimationPlaying())
            {
                shotsFiredThisTriggerPull = 0;
                triggerHoldTimer = 0f;
                if (fireAnimTimer <= 0f) animator.SetBool(fireParamHash, false);
                targetLayerWeight = 1f;
                return;
            }

            int currentReserve = currentWeaponData != null ? reserveAmmo[currentWeaponData.category] : 0;
            int maxAmmo = currentWeaponData != null ? currentWeaponData.magSize : 0;

            // Auto-Reload on empty magazine
            if (currentAmmo <= 0 && currentWeaponData != null && !isRapidFireActive)
            {
                shotsFiredThisTriggerPull = 0;
                triggerHoldTimer = 0f;
                animator.SetBool(fireParamHash, false);

                if (currentReserve > 0)
                {
                    animator.SetBool(reloadParamHash, true);
                    Invoke(nameof(ResetReloadParameter), 0.15f);
                }
                else if (currentWeaponData.category != WeaponCategory.Pistol)
                {
                    AutoSwitchWeapon();
                }
                else if (shootingInput && debugMode)
                {
                    Debug.Log("<color=red>OUT OF AMMO!</color> Need to pick up a supply crate!");
                }

                targetLayerWeight = 1f;
                return;
            }

            // Manual Reload (Swipe up or 'R' key)
            if (reloadPressed && currentAmmo < maxAmmo && currentReserve > 0)
            {
                shotsFiredThisTriggerPull = 0;
                triggerHoldTimer = 0f;
                animator.SetBool(fireParamHash, false);
                animator.SetBool(reloadParamHash, true);
                Invoke(nameof(ResetReloadParameter), 0.15f);
                targetLayerWeight = 1f;
                return;
            }

            // 4. TRIGGER HOLD TRACKING
            bool isNewTriggerPress = shootingInput && !wasShootingLastFrame;
            if (isNewTriggerPress)
            {
                shotsFiredThisTriggerPull = 0;
                triggerHoldTimer = 0f;
            }
            else if (shootingInput)
            {
                triggerHoldTimer += Time.deltaTime;
            }
            else
            {
                shotsFiredThisTriggerPull = 0;
                triggerHoldTimer = 0f;
            }
            wasShootingLastFrame = shootingInput;

            // 5. SHOOTING LOGIC
            if (currentWeaponData != null)
            {
                float activeFireRate = Mathf.Max(currentWeaponData.fireRate, 1f);
                if (isRapidFireActive && equippedSkill != null && equippedSkill.skillType == SkillType.RapidFire)
                {
                    activeFireRate *= equippedSkill.effectValue;
                }
                float fireInterval = 1.0f / activeFireRate;

                // Fire mode rules:
                bool canShoot = false;

                if (isRapidFireActive)
                {
                    canShoot = true;
                }
                else if (currentWeaponData.fireMode == WeaponFireMode.Single)
                {
                    // Single shot weapons (Pistol, Shotgun, Sniper): strictly 1 shot per trigger press
                    if (shotsFiredThisTriggerPull < 1) canShoot = true;
                }
                else if (currentWeaponData.fireMode == WeaponFireMode.Burst)
                {
                    // Burst weapons: up to burstCount shots per trigger press
                    if (shotsFiredThisTriggerPull < currentWeaponData.burstCount) canShoot = true;
                }
                else if (currentWeaponData.fireMode == WeaponFireMode.Auto)
                {
                    // Auto weapons (Assault Rifle, SMG):
                    // First shot fires on tap (shotsFiredThisTriggerPull == 0).
                    // Continuous auto-fire ONLY engages once held past autoFireHoldDelay!
                    if (shotsFiredThisTriggerPull == 0) canShoot = true;
                    else if (triggerHoldTimer >= autoFireHoldDelay) canShoot = true;
                }

                // STRICT SAFETY: Do NOT fire while actively swiping on screen!
                // Firing bullets while the character is physically moving across lanes IS allowed.
                bool isShootingBlocked = TouchManager.IsSwiping;
                if (isShootingBlocked)
                {
                    canShoot = false;
                    shotsFiredThisTriggerPull = 0;
                    triggerHoldTimer = 0f;
                }

                bool wantsToShoot = shootingInput && canShoot;

                if (wantsToShoot && fireCooldownTimer <= 0f && (currentAmmo > 0 || isRapidFireActive))
                {
                    ShootBullet();
                    shotsFiredThisTriggerPull++;
                    lastShotTime = Time.time;
                    fireCooldownTimer = fireInterval;

                    animator.SetBool(fireParamHash, true);
                    fireAnimTimer = Mathf.Clamp(fireInterval, 0.08f, 0.18f);
                }

                if (!shootingInput && fireAnimTimer <= 0f)
                {
                    animator.SetBool(fireParamHash, false);
                }
                else if (shootingInput && !canShoot && fireAnimTimer <= 0f)
                {
                    animator.SetBool(fireParamHash, false);
                }

                targetLayerWeight = 1f;
                return;
            }

            if (fireAnimTimer <= 0f)
            {
                animator.SetBool(fireParamHash, false);
            }
            targetLayerWeight = 1f;
        }

        private void AutoSwitchWeapon()
        {
            if (unlockedWeapons.Count <= 1) return;

            WeaponData bestChoice = null;
            WeaponData fallbackPistol = null;

            foreach (var weapon in unlockedWeapons)
            {
                if (weapon == currentWeaponData) continue;

                if (weapon.category == WeaponCategory.Pistol)
                {
                    fallbackPistol = weapon;
                    continue;
                }

                int ammoInMag = loadedAmmo.ContainsKey(weapon.weaponID) ? loadedAmmo[weapon.weaponID] : weapon.magSize;
                int reserve = reserveAmmo.ContainsKey(weapon.category) ? reserveAmmo[weapon.category] : 0;

                if (ammoInMag > 0 || reserve > 0)
                {
                    bestChoice = weapon;
                    break;
                }
            }

            if (bestChoice == null && fallbackPistol != null)
            {
                bestChoice = fallbackPistol;
            }

            if (bestChoice != null)
            {
                if (debugMode) Debug.Log($"<color=cyan>[Auto-Switch]</color> Out of ammo! Switching to {bestChoice.weaponName}");
                EquipWeaponFromWheel(bestChoice);
            }
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

        /// <summary>
        /// AnimationEvent hook retained for legacy animation clips so Unity doesn't log missing method warnings.
        /// Weapon firing is now handled directly by code in HandleActions() and ShootBullet() with zero latency.
        /// </summary>
        public void isFiring(AnimationEvent ae)
        {
            // No-op: shooting logic is driven by code timers for instant zero-latency response.
        }

        public void ShootBullet()
        {
            if (currentWeaponData == null) return;

            if (!isRapidFireActive)
            {
                currentAmmo--;
                // Sync bullet count to memory so swapping weapons saves our state!
                loadedAmmo[currentWeaponData.weaponID] = currentAmmo;
                UpdateAmmoUI();
            }

            if (audioSource != null && currentWeaponData.audioFire != null)
            {
                audioSource.pitch = Random.Range(0.95f, 1.05f);
                audioSource.PlayOneShot(currentWeaponData.audioFire, weaponVolume);
            }

            Vector3 worldMuzzlePos;
            Quaternion worldMuzzleRot;

            if (currentWeaponInstance != null)
            {
                worldMuzzlePos = currentWeaponInstance.transform.TransformPoint(currentWeaponData.muzzlePosition);
                worldMuzzleRot = currentWeaponInstance.transform.rotation * Quaternion.Euler(currentWeaponData.muzzleRotation);

                if (currentWeaponData.muzzleFlash != null)
                {
                    GameObject flashPrefab = currentWeaponData.muzzleFlash.gameObject;
                    GameObject flash = PoolManager.Instance.SpawnFromPool(flashPrefab, worldMuzzlePos, worldMuzzleRot, currentWeaponInstance.transform);
                    PoolManager.Instance.ReturnToPoolAfterDelay(flash, flashPrefab, currentWeaponData.flashLifetime);
                }
            }
            else
            {
                worldMuzzlePos = transform.position + Vector3.up * 1.5f + transform.forward * 0.5f;
                worldMuzzleRot = transform.rotation;
            }

            PerformRaycastHit(worldMuzzlePos, worldMuzzleRot);
        }

        private void PerformRaycastHit(Vector3 origin, Quaternion rotation)
        {
            if (currentWeaponData == null) return;

            Vector2 sPoint = TouchManager.CurrentTouchPosition;
            if (Camera.main == null) return;
            Ray ray = Camera.main.ScreenPointToRay(sPoint);

            // Aim exactly at where the player's IK is pointing (the AimTarget).
            // This prevents steep camera rays from calculating a point behind the player's muzzle.
            Vector3 targetPos = (aimTarget != null) ? aimTarget.position : ray.GetPoint(100f);
            
            bool isBossFight = (GameManager.Instance != null && GameManager.Instance.isBossFightActive);

            // Absolute failsafe: Never shoot backwards
            if (targetPos.z <= origin.z)
            {
                targetPos.z = origin.z + (isBossFight ? 20f : 50f);
            }

            Vector3 shootDir = (targetPos - origin).normalized;
            Vector3 endPoint = origin + shootDir * currentWeaponData.range;

            RaycastHit hit;
            bool didHit = Physics.Raycast(ray, out hit, currentWeaponData.range, hitMask, QueryTriggerInteraction.Collide);
            if (!didHit)
            {
                // Fallback to SphereCast for rapid/auto firing weapons targeting thin moving limbs
                didHit = Physics.SphereCast(ray, 0.35f, out hit, currentWeaponData.range, hitMask, QueryTriggerInteraction.Collide);
            }

            if (didHit)
            {
                endPoint = hit.point;
                if (impactEffect != null)
                {
                    GameObject impactPrefab = impactEffect.gameObject;
                    GameObject impact = PoolManager.Instance.SpawnFromPool(impactPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                    PoolManager.Instance.ReturnToPoolAfterDelay(impact, impactPrefab, currentWeaponData.impactLifetime);
                }

                bool hitEnemy = false;
                BossBodyPart bodyPart = hit.collider.GetComponent<BossBodyPart>();
                if (bodyPart != null)
                {
                    bodyPart.TakeDamage(currentWeaponData.baseDamage * damageMultiplier);
                    hitEnemy = true;
                }
                else
                {
                    EnemyController enemy = hit.collider.GetComponent<EnemyController>();
                    if (enemy == null) enemy = hit.collider.GetComponentInParent<EnemyController>();
                    if (enemy != null)
                    {
                        enemy.TakeDamage(currentWeaponData.baseDamage * damageMultiplier, hit.collider);
                        hitEnemy = true;
                    }
                }

                if (hitEnemy && hitMarkerSounds != null && hitMarkerSounds.Length > 0 && audioSource != null)
                {
                    AudioClip clip = hitMarkerSounds[Random.Range(0, hitMarkerSounds.Length)];
                    if (clip != null)
                    {
                        audioSource.pitch = 1.0f; // Reset pitch in case gun shots randomized it
                        audioSource.PlayOneShot(clip, weaponVolume);
                    }
                }
            }

            SpawnBulletTrail(origin, endPoint);
        }

        private void SpawnBulletTrail(Vector3 startPos, Vector3 endPos)
        {
            if (currentWeaponData.bulletTrailPrefab == null)
            {
                // Dynamically generate a trail prefab if empty
                GameObject generatedTrail = new GameObject("GeneratedBulletTrail");
                generatedTrail.SetActive(false); 
                
                LineRenderer lr = generatedTrail.AddComponent<LineRenderer>();
                lr.startWidth = 0.05f;
                lr.endWidth = 0.01f;
                Material trailMat = new Material(Shader.Find("Sprites/Default"));
                lr.material = trailMat;
                lr.startColor = new Color(1f, 0.8f, 0.2f, 1f); // Glowing Orange
                lr.endColor = new Color(1f, 0.8f, 0.2f, 1f);

                generatedTrail.AddComponent<BulletTrail>();

                // Save it to the WeaponData so it acts as a prefab key for the PoolManager
                currentWeaponData.bulletTrailPrefab = generatedTrail;
                DontDestroyOnLoad(generatedTrail); 
            }

            GameObject trailInstance = PoolManager.Instance.SpawnFromPool(currentWeaponData.bulletTrailPrefab, startPos, Quaternion.identity);
            BulletTrail trailScript = trailInstance.GetComponent<BulletTrail>();
            if (trailScript != null)
            {
                trailScript.Initialize(startPos, endPos, currentWeaponData.trailDuration, currentWeaponData.bulletTrailPrefab);
            }
        }

        public void Reload(AnimationEvent ae)
        {
            if (Time.timeSinceLevelLoad < 0.5f) return;
            if (ae.floatParameter < 0.1f) return;

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
                UIManager.Instance.UpdateWeapon(currentWeaponData);
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

            if (currentHealth <= 0 && !isDead)
            {
                Die();
            }
        }

        public void Kill()
        {
            if (isDead) return;
            currentHealth = 0;
            currentArmor = 0;
            UpdatePlayerUI();
            Die();
        }

        private void Die()
        {
            isDead = true;

            // Stop aiming and running animations
            animator.SetFloat(strafeParamHash, 0);
            animator.SetFloat(aimHorizontalHash, 0);
            animator.SetFloat(aimVerticalHash, 0);

            // Turn off upper body layers so the full body death animation plays cleanly
            if (aimLayerIndex >= 0 && aimLayerIndex < animator.layerCount)
                animator.SetLayerWeight(aimLayerIndex, 0f);
            
            if (weaponLayerIndex >= 0 && weaponLayerIndex < animator.layerCount)
                animator.SetLayerWeight(weaponLayerIndex, 0f);

            // Trigger the death animation
            animator.SetTrigger("Death");

            // Shout to the rest of the game that the player is dead!
            OnPlayerDeath?.Invoke();

            if (debugMode) Debug.Log("<color=red>GAME OVER!</color> Player has died.");
        }

        private void MoveLeft()
        {
            if (GameManager.Instance != null && GameManager.Instance.isBossFightActive)
                currentLane = Mathf.Clamp(currentLane - 1, -1, 1);
            else
                currentLane = Mathf.Clamp(currentLane + 1, -1, 1);

            shotsFiredThisTriggerPull = 0;
            triggerHoldTimer = 0f;
            if (animator != null) animator.SetBool(fireParamHash, false);
        }

        private void MoveRight()
        {
            if (GameManager.Instance != null && GameManager.Instance.isBossFightActive)
                currentLane = Mathf.Clamp(currentLane + 1, -1, 1);
            else
                currentLane = Mathf.Clamp(currentLane - 1, -1, 1);

            shotsFiredThisTriggerPull = 0;
            triggerHoldTimer = 0f;
            if (animator != null) animator.SetBool(fireParamHash, false);
        }

        public void EquipWeaponFromWheel(WeaponData selectedWeapon)
        {
            if (selectedWeapon == null) return;
            weaponLayerIndex = selectedWeapon.animatorLayer;
            lastWeaponLayerIndex = weaponLayerIndex;
            currentWeaponData = selectedWeapon;
            fireCooldownTimer = 0.08f;
            shotsFiredThisTriggerPull = 0;
            triggerHoldTimer = 0f;
            SpawnWeaponModel();
            Debug.Log($"<color=cyan>[Weapon]</color> Equipped: {selectedWeapon.weaponName}");
        }

        public WeaponData CurrentWeaponData => currentWeaponData;
        public List<WeaponData> GetUnlockedWeapons() => unlockedWeapons;
        public bool HasWeapon(string weaponID)
        {
            if (unlockedWeapons == null || string.IsNullOrEmpty(weaponID)) return false;
            return unlockedWeapons.Exists(w => w != null && w.weaponID == weaponID);
        }

        private void HandleWeaponHotkeys()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current == null) return;

            var kb = Keyboard.current;
            if (kb.tabKey.wasPressedThisFrame)
            {
                if (WeaponWheelToolkitManager.Instance != null)
                {
                    if (WeaponWheelToolkitManager.Instance.IsOpen)
                        WeaponWheelToolkitManager.Instance.CloseWheel();
                    else
                        WeaponWheelToolkitManager.Instance.OpenWheel();
                }
            }
            if (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame) SwitchToSlot(0);
            else if (kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame) SwitchToSlot(1);
            else if (kb.digit3Key.wasPressedThisFrame || kb.numpad3Key.wasPressedThisFrame) SwitchToSlot(2);
            else if (kb.digit4Key.wasPressedThisFrame || kb.numpad4Key.wasPressedThisFrame) SwitchToSlot(3);
            else if (kb.digit5Key.wasPressedThisFrame || kb.numpad5Key.wasPressedThisFrame) SwitchToSlot(1);
            else if (kb.qKey.wasPressedThisFrame) CycleUnlockedWeapons(-1);
            else if (kb.eKey.wasPressedThisFrame) CycleUnlockedWeapons(1);
#endif
        }

        public void SwitchToSlot(int slotIndex)
        {
            var match = unlockedWeapons.Find(w => w != null && WeaponWheelToolkitManager.GetSlotIndexForCategory(w.category) == slotIndex);
            if (match != null && match != currentWeaponData)
            {
                EquipWeaponFromWheel(match);
            }
        }

        public void SwitchToCategory(WeaponCategory category)
        {
            var match = unlockedWeapons.Find(w => w.category == category);
            if (match != null && match != currentWeaponData)
            {
                EquipWeaponFromWheel(match);
            }
        }

        public void CycleUnlockedWeapons(int step)
        {
            if (unlockedWeapons.Count <= 1) return;
            int currentIndex = unlockedWeapons.IndexOf(currentWeaponData);
            if (currentIndex < 0) currentIndex = 0;
            int nextIndex = (currentIndex + step + unlockedWeapons.Count) % unlockedWeapons.Count;
            EquipWeaponFromWheel(unlockedWeapons[nextIndex]);
        }

        public List<WeaponCategory> GetOwnedWeaponCategories()
        {
            List<WeaponCategory> categories = new List<WeaponCategory>();
            foreach (WeaponData weapon in unlockedWeapons)
            {
                if (!categories.Contains(weapon.category))
                {
                    categories.Add(weapon.category);
                }
            }
            return categories;
        }

        public void ActivateSkill()
        {
            if (equippedSkill == null) return;

            if (!equippedSkill.isKillBasedRecharge)
            {
                if (currentSkillCooldownTimer > 0)
                {
                    if (debugMode) Debug.Log("<color=green>[SKILL]</color> Skill on cooldown!");
                    return;
                }
            }

            if (debugMode) Debug.Log($"<color=green>[SKILL]</color> {equippedSkill.skillName} activated!");
            
            // Apply Skill Effect
            if (equippedSkill.skillType == SkillType.RapidFire)
            {
                isRapidFireActive = true;
                activeRapidFireTimer = equippedSkill.effectDuration;
            }
            
            if (equippedSkill.visualEffectPrefab != null)
            {
                GameObject vfx = Instantiate(equippedSkill.visualEffectPrefab, transform.position, Quaternion.identity, transform);
                if (equippedSkill.effectDuration > 0)
                {
                    Destroy(vfx, equippedSkill.effectDuration);
                }
            }

            // Reset Cooldown
            if (!equippedSkill.isKillBasedRecharge)
            {
                currentSkillCooldownTimer = equippedSkill.rechargeRequirement;
            }
        }

        public void ActivateUltimate()
        {
            if (equippedUltimate == null) return;

            if (equippedUltimate.isKillBasedRecharge)
            {
                if (currentUltimateKillCount < equippedUltimate.rechargeRequirement)
                {
                    if (debugMode) Debug.Log($"<color=cyan>[ULTIMATE]</color> Ultimate not ready! Kills: {currentUltimateKillCount}/{equippedUltimate.rechargeRequirement}");
                    return;
                }
            }

            if (debugMode) Debug.Log($"<color=cyan>[ULTIMATE]</color> {equippedUltimate.skillName} activated!");

            // Apply Ultimate Effect
            if (equippedUltimate.skillType == SkillType.AoEKill)
            {
                Collider[] enemies = Physics.OverlapSphere(transform.position, equippedUltimate.effectRadius, hitMask);
                System.Collections.Generic.HashSet<EnemyController> hitEnemies = new System.Collections.Generic.HashSet<EnemyController>();
                foreach (var col in enemies)
                {
                    EnemyController enemy = col.GetComponent<EnemyController>();
                    if (enemy == null) enemy = col.GetComponentInParent<EnemyController>();
                    
                    if (enemy != null && !hitEnemies.Contains(enemy) && !enemy.IsDead)
                    {
                        hitEnemies.Add(enemy);
                        if (enemy.IsBoss)
                        {
                            float bossDmg = (equippedUltimate.bossDamage > 0f) ? equippedUltimate.bossDamage : 250f;
                            enemy.TakeDamage(bossDmg);
                            Debug.Log($"<color=cyan>[ULTIMATE]</color> Dealt {bossDmg} damage to Boss! Remaining Boss HP: {enemy.CurrentHealth}");
                        }
                        else
                        {
                            float normalDmg = (equippedUltimate.damage > 0f) ? equippedUltimate.damage : 500f;
                            enemy.TakeDamage(normalDmg);
                        }
                    }
                }
            }

            if (equippedUltimate.visualEffectPrefab != null)
            {
                GameObject vfx = Instantiate(equippedUltimate.visualEffectPrefab, transform.position, Quaternion.identity, transform);
                if (equippedUltimate.effectDuration > 0)
                {
                    Destroy(vfx, equippedUltimate.effectDuration);
                }
            }

            // Reset Cooldown
            if (equippedUltimate.isKillBasedRecharge)
            {
                currentUltimateKillCount = 0;
            }
        }

        public void RestoreHealth(float amount)
        {
            float maxHealth = (playerStats != null) ? runtimeMaxHealth : 100f;
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
            if (!reserveAmmo.ContainsKey(category)) reserveAmmo[category] = 0;
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
            if (WeaponDatabase.Instance == null) return;

            WeaponData newGun = WeaponDatabase.Instance.GetWeaponByID(incomingWeaponID);
            if (newGun == null) return;

            // 1. If player already owns this exact gun, convert drop to ammo
            if (unlockedWeapons.Contains(newGun))
            {
                if (debugMode) Debug.Log($"<color=magenta>[Loot]</color> You already own the {newGun.weaponName}! Converting to ammo...");
                AddAmmo(newGun.category, newGun.magSize * 2);
                return;
            }

            // 2. Option C: Direct Category / Slot Replacement
            int targetSlot = WeaponWheelToolkitManager.GetSlotIndexForCategory(newGun.category);

            // Find if any currently owned weapon already occupies this slot
            WeaponData oldWeaponInSlot = unlockedWeapons.Find(w => w != null && WeaponWheelToolkitManager.GetSlotIndexForCategory(w.category) == targetSlot);

            if (oldWeaponInSlot != null)
            {
                Debug.Log($"<color=yellow>[Weapon Upgrade]</color> Slot {targetSlot + 1} ({oldWeaponInSlot.weaponName}) replaced with {newGun.weaponName}!");
                unlockedWeapons.Remove(oldWeaponInSlot);
            }
            else
            {
                Debug.Log($"<color=green>[Weapon Unlock]</color> Unlocked new {newGun.weaponName} into Slot {targetSlot + 1}!");
            }

            unlockedWeapons.Add(newGun);

            // 3. Grant starting reserve ammo for the newly unlocked/upgraded weapon (at least 2 full magazines)
            if (!reserveAmmo.ContainsKey(newGun.category) || reserveAmmo[newGun.category] == 0)
            {
                reserveAmmo[newGun.category] = newGun.magSize * 2;
            }

            // 4. Update the Weapon Wheel UI
            if (WeaponWheelToolkitManager.Instance != null)
            {
                WeaponWheelToolkitManager.Instance.AddWeaponToWheel(newGun);
                WeaponWheelToolkitManager.Instance.SyncUnlockedWeapons();
            }

            // 5. Equip the newly unlocked weapon
            EquipWeaponFromWheel(newGun);
        }

        public void ApplyUpgrade(UpgradeCard card)
        {
            if (card == null || card.effects == null) return;
            
            foreach (var effect in card.effects)
            {
                switch (effect.upgradeType)
                {
                    case UpgradeType.MaxHealth:
                        runtimeMaxHealth += effect.upgradeValue; // Increase the cap
                        currentHealth += effect.upgradeValue;    // Heal the player
                        UpdatePlayerUI();                      // Update the health bar!
                        break;

                    case UpgradeType.SpeedBoost:
                        runtimeMovementSpeed += effect.upgradeValue; // Make them run faster!
                        break;

                    case UpgradeType.DamageBoost:
                        damageMultiplier += (effect.upgradeValue / 100f);
                        break;
                        
                    case UpgradeType.WeaponUnlock:
                        if (!string.IsNullOrEmpty(effect.weaponID))
                        {
                            UnlockWeapon(effect.weaponID);
                        }
                        else
                        {
                            Debug.LogWarning("WeaponUnlock card effect is missing a Weapon ID!");
                        }
                        break;

                    case UpgradeType.ArmorPerKill:
                        armorPerKill += effect.upgradeValue;
                        if (debugMode) Debug.Log($"<color=cyan>[Upgrade]</color> Armor Per Kill increased by +{effect.upgradeValue}! Total: {armorPerKill}/kill");
                        break;
                }
            }
        }

        public float GetSkillRechargeRatio()
        {
            if (equippedSkill == null || equippedSkill.rechargeRequirement <= 0) return 0f;
            if (equippedSkill.isKillBasedRecharge) return 0f;
            return 1f - Mathf.Clamp01(currentSkillCooldownTimer / equippedSkill.rechargeRequirement);
        }

        public float GetUltimateRechargeRatio()
        {
            if (equippedUltimate == null || equippedUltimate.rechargeRequirement <= 0) return 0f;
            if (!equippedUltimate.isKillBasedRecharge) return 0f;
            return Mathf.Clamp01(currentUltimateKillCount / equippedUltimate.rechargeRequirement);
        }
    }
}