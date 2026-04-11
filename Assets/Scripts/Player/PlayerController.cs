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
    /// Handles discrete horizontal movement (Starf), weapon layers, and reloading.
    /// Uses GetComponent for Animator and discrete input for strafing.
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        private Animator animator;

        [Header("Movement Settings")]
        [Tooltip("How far apart the lanes are on the X-axis.")]
        public float laneDistance = 2.0f;
        
        [Tooltip("How fast the player physically moves between lanes.")]
        public float movementSpeed = 10f;

        [Tooltip("How fast the animaor's Starf value reach the target.")]
        public float strafeAnimationSmoothing = 8f;

        [Header("Weapon Layer Settings")]
        [Tooltip("The index of the weapon animation layer (e.g., 1 for Assault Rifle).")]
        public int weaponLayerIndex = 1;

        [Tooltip("How fast the layer weight blends to 1 when switching weapons.")]
        public float layerBlendSpeed = 10f;

        [Tooltip("The Transform to calculate the aiming direction from (e.g., Chest or Head). If null, a point above the feet will be used.")]
        public Transform aimOrigin;

        [Tooltip("The Transform that the player should aim at (e.g. moved by mouse).")]
        public Transform aimTarget;

        [Tooltip("Index of the spine aiming layer.")]
        public int aimLayerIndex = 1;

        [Tooltip("How fast the aim values follow the target.")]
        public float aimSmoothing = 10f;

        [Tooltip("Maximum angle for horizontal aiming (maps to 1.0).")]
        public float maxAimAngleHorizontal = 45f;

        [Tooltip("Maximum angle for vertical aiming (maps to 1.0).")]
        public float maxAimAngleVertical = 30f;

        [Tooltip("Flip the horizontal aim direction.")]
        public bool invertHorizontal = false;

        [Tooltip("Flip the vertical aim direction.")]
        public bool invertVertical = false;

        [Header("Weapon System")]
        [Tooltip("The central database containing all weapon data assets.")]
        public WeaponDatabase weaponDatabase;

        [Header("Weapon Parent & Sockets")]
        [Tooltip("The Transform (hand bone or socket) where weapons will be parented.")]
        public Transform weaponSocket;

        [Header("Effects Settings")]
        [Tooltip("Prefab to spawn at the location of a bullet impact.")]
        public Transform impactEffect;

        [Tooltip("Which layers should the bullets collide with?")]
        public LayerMask hitMask;

        [Tooltip("The volume of the weapon sounds.")]
        [Range(0f, 1f)]
        public float weaponVolume = 0.5f;

        private WeaponEntry currentWeaponData;
        private GameObject currentWeaponInstance;
        private int lastWeaponLayerIndex = -1;

        // Animator Parameter Names
        private string strafeParameter = "Starf";
        private string reloadParameter = "Reload";
        private string fireParameter = "isFiring";
        private string fireMultiplierParameter = "FireSpeedMultiplier";
        private string aimHorizontalParameter = "Aim_Horizontal";
        private string aimVerticalParameter = "Aim_Vertical";
        private string reloadStateName = "Reload";
        private string reloadStateTag = "Reload";

        // Private Hashes for Performance
        private int strafeParamHash;
        private int reloadParamHash;
        private int fireParamHash;
        private int fireMultiplierParamHash;
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
        private float lastFireSoundTime;
        private float fireCooldownTimer;
        private AnimatorStateInfo currentWeaponState;
        private int currentAmmo;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            audioSource = GetComponent<AudioSource>();

            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            strafeParamHash = Animator.StringToHash(strafeParameter);
            reloadParamHash = Animator.StringToHash(reloadParameter);
            fireParamHash = Animator.StringToHash(fireParameter);
            fireMultiplierParamHash = Animator.StringToHash(fireMultiplierParameter);
            aimHorizontalHash = Animator.StringToHash(aimHorizontalParameter);
            aimVerticalHash = Animator.StringToHash(aimVerticalParameter);
            reloadStateHash = Animator.StringToHash(reloadStateName);
            reloadTagHash = Animator.StringToHash(reloadStateTag);

            if (animator == null)
            {
                Debug.LogWarning("[PlayerController] No Animator found!");
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
            if (currentWeaponInstance != null)
            {
                Destroy(currentWeaponInstance);
            }

            if (currentWeaponData == null || currentWeaponData.weaponPrefab == null || weaponSocket == null) return;

            currentWeaponInstance = Instantiate(currentWeaponData.weaponPrefab.gameObject, weaponSocket);
            currentWeaponInstance.transform.localPosition = currentWeaponData.holdPosition;
            currentWeaponInstance.transform.localRotation = Quaternion.Euler(currentWeaponData.holdRotation);
            currentWeaponInstance.transform.localScale = currentWeaponData.localScale;
            
            // Initialize Ammo
            currentAmmo = currentWeaponData.magSize;
            UpdateAmmoUI();

            if (debugMode) Debug.Log($"[PlayerController] Spawned weapon: {currentWeaponData.weaponName}");
        }

        private void HandleMovement()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                if (Keyboard.current.aKey.wasPressedThisFrame || Keyboard.current.leftArrowKey.wasPressedThisFrame) 
                    currentLane = Mathf.Clamp(currentLane + 1, -1, 1);
                else if (Keyboard.current.dKey.wasPressedThisFrame || Keyboard.current.rightArrowKey.wasPressedThisFrame) 
                    currentLane = Mathf.Clamp(currentLane - 1, -1, 1);
            }
#else
            if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
                currentLane = Mathf.Clamp(currentLane + 1, -1, 1);
            else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
                currentLane = Mathf.Clamp(currentLane - 1, -1, 1);
#endif

            float targetX = currentLane * laneDistance;
            Vector3 pos = transform.position;
            pos.x = Mathf.MoveTowards(pos.x, targetX, Time.deltaTime * movementSpeed);
            transform.position = pos;

            float moveDelta = targetX - pos.x;
            float animationTarget = (Mathf.Abs(moveDelta) > 0.01f) ? Mathf.Sign(moveDelta) : 0f;
            currentStrafeAnim = Mathf.MoveTowards(currentStrafeAnim, animationTarget, Time.deltaTime * strafeAnimationSmoothing);
            animator.SetFloat(strafeParamHash, currentStrafeAnim);
        }

        private void HandleAiming()
        {
            if (animator == null) return;

            bool isAiming = false;
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null && Mouse.current.leftButton.isPressed) isAiming = true;
#else
            if (Input.GetMouseButton(0)) isAiming = true;
#endif

            if (isAiming && aimTarget != null)
            {
                Vector3 origin = (aimOrigin != null) ? aimOrigin.position : transform.position + Vector3.up * 1.5f;
                Vector3 directionToTarget = (aimTarget.position - origin).normalized;
                Vector3 localDir = transform.InverseTransformDirection(directionToTarget);

                float yaw = Mathf.Atan2(localDir.x, localDir.z) * Mathf.Rad2Deg;
                float pitch = Mathf.Atan2(localDir.y, new Vector2(localDir.x, localDir.z).magnitude) * Mathf.Rad2Deg;

                targetAimH = Mathf.Clamp(-yaw / maxAimAngleHorizontal, -1f, 1f);
                targetAimV = Mathf.Clamp(pitch / maxAimAngleVertical, -1f, 1f);

                if (invertHorizontal) targetAimH *= -1f;
                if (invertVertical) targetAimV *= -1f;
            }

            currentAimH = Mathf.MoveTowards(currentAimH, targetAimH, Time.deltaTime * aimSmoothing);
            currentAimV = Mathf.MoveTowards(currentAimV, targetAimV, Time.deltaTime * aimSmoothing);
            
            debugAimH = currentAimH;
            debugAimV = currentAimV;

            animator.SetFloat(aimHorizontalHash, currentAimH);
            animator.SetFloat(aimVerticalHash, currentAimV);
        }

        private void HandleActions()
        {
            bool shootingInput = false;
            bool reloadPressed = false;
            bool isActuallyReloading = IsReloadingAnimationPlaying();

#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null && Mouse.current.leftButton.isPressed) shootingInput = true;
            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame) reloadPressed = true;
#else
            if (Input.GetMouseButton(0)) shootingInput = true;
            if (Input.GetKeyDown(KeyCode.R)) reloadPressed = true;
#endif

            bool canFire = !isActuallyReloading && currentAmmo > 0;
            
            // 1. STABLE ANIMATOR STATE (No Flicker)
            // This stays true as long as trigger is held, preventing animation glitches
            bool targetFireState = shootingInput && canFire;
            animator.SetBool(fireParamHash, targetFireState);

            // 2. SPEED SYNC (Multiplier)
            // Adjust animation speed to match the database fireRate
            if (currentWeaponData != null)
            {
                // Assuming base animation is timed for ~5 shots per second (0.2s)
                // We scale it so 1 loop = 1 database shot
                float speedMult = currentWeaponData.fireRate / 5f; 
                animator.SetFloat(fireMultiplierParamHash, speedMult);
            }

            // 3. ANIMATOR TRIGGERING (Timer-based)
            if (fireCooldownTimer > 0f) fireCooldownTimer -= Time.deltaTime;

            if (shootingInput && canFire && fireCooldownTimer <= 0f)
            {
                // We don't fire the bullet here anymore. 
                // We just let the Animator play, and the Animation Event will trigger the shot.
                fireCooldownTimer = (currentWeaponData != null) ? (1.0f / currentWeaponData.fireRate) : 0.2f;
            }

            // AUTO-RELOAD or MANUAL RELOAD
            bool shouldReload = (reloadPressed || (shootingInput && currentAmmo <= 0)) && !isActuallyReloading;
            if (shouldReload)
            {
                animator.SetBool(reloadParamHash, true);
                Invoke(nameof(ResetReloadParameter), 0.15f);
            }

            targetLayerWeight = 1f;
        }

        private bool IsReloadingAnimationPlaying()
        {
            if (animator == null || weaponLayerIndex < 0 || weaponLayerIndex >= animator.layerCount) return false;

            bool isInReload = currentWeaponState.shortNameHash == reloadStateHash || currentWeaponState.tagHash == reloadTagHash;
            
            if (animator.IsInTransition(weaponLayerIndex))
            {
                AnimatorStateInfo nextState = animator.GetNextAnimatorStateInfo(weaponLayerIndex);
                if (nextState.shortNameHash == reloadStateHash || nextState.tagHash == reloadTagHash) return true;
            }

            if (isInReload) return currentWeaponState.normalizedTime < 0.95f;
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
            if (currentWeaponData == null || currentAmmo <= 0) return;

            // 1. Ammo Consumption (Now synced with animation frame!)
            currentAmmo--;
            UpdateAmmoUI();

            // 2. Audio
            if (audioSource != null && currentWeaponData.audioFire != null) 
            {
                audioSource.pitch = Random.Range(0.95f, 1.05f);
                audioSource.PlayOneShot(currentWeaponData.audioFire, weaponVolume);
            }

            // 3. VFX & Shooting Logic
            if (currentWeaponInstance != null)
            {
                Vector3 worldMuzzlePos = currentWeaponInstance.transform.TransformPoint(currentWeaponData.muzzlePosition);
                Quaternion worldMuzzleRot = currentWeaponInstance.transform.rotation * Quaternion.Euler(currentWeaponData.muzzleRotation);

                if (currentWeaponData.muzzleFlash != null)
                {
                    GameObject flash = Instantiate(currentWeaponData.muzzleFlash.gameObject, worldMuzzlePos, worldMuzzleRot, currentWeaponInstance.transform);
                    Destroy(flash, currentWeaponData.flashLifetime);
                }

                // 4. Hit Detection (Now synced with animation frame!)
                PerformRaycastHit(worldMuzzlePos, worldMuzzleRot);
            }
        }

        private void PerformRaycastHit(Vector3 origin, Quaternion rotation)
        {
            if (currentWeaponData == null) return;

            // NEW: Tap-to-Shoot (Directly from Finger/Mouse)
            Vector2 sPoint = Vector2.zero;
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null) sPoint = Mouse.current.position.ReadValue();
#else
            sPoint = Input.mousePosition;
#endif

            if (Camera.main == null) return;
            Ray ray = Camera.main.ScreenPointToRay(sPoint);

            // VISUAL DEBUG: Current tap path
            Debug.DrawRay(ray.origin, ray.direction * currentWeaponData.raycastRange, Color.red, 1.0f);

            if (Physics.Raycast(ray, out RaycastHit hit, currentWeaponData.raycastRange, hitMask, QueryTriggerInteraction.Collide))
            {
                if (debugMode) Debug.Log($"<color=cyan>[Combat]</color> HIT: {hit.collider.name} | Damage: {currentWeaponData.baseDamage}");

                if (impactEffect != null)
                {
                    GameObject impact = Instantiate(impactEffect.gameObject, hit.point, Quaternion.LookRotation(hit.normal));
                    Destroy(impact, currentWeaponData.impactLifetime);
                }

                EnemyController enemy = hit.collider.GetComponent<EnemyController>();
                if (enemy == null) enemy = hit.collider.GetComponentInParent<EnemyController>();
                if (enemy != null) enemy.TakeDamage(currentWeaponData.baseDamage);
            }
        }

        public void Reload(AnimationEvent ae)
        {
            if (audioSource == null || currentWeaponData == null) return;
            
            // Audio Effects
            if (ae.stringParameter == "MagOut" && currentWeaponData.audioMagOut != null) audioSource.PlayOneShot(currentWeaponData.audioMagOut, weaponVolume);
            else if (ae.stringParameter == "MagIn" && currentWeaponData.audioMagIn != null) audioSource.PlayOneShot(currentWeaponData.audioMagIn, weaponVolume);

            // Refill Ammo Logic
            // Usually we refill on "MagIn" or at the end of the state
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
    }
}
