using UnityEngine;
using System.Collections.Generic;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

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
        [Tooltip("How long muzzle flash particles stay in the scene before being destroyed.")]
        public float muzzleFlashLifeTime = 0.5f;

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
        private string aimHorizontalParameter = "Aim_Horizontal";
        private string aimVerticalParameter = "Aim_Vertical";
        private string reloadStateName = "Reload";
        private string reloadStateTag = "Reload";

        // Private Hashes for Performance
        private int strafeParamHash;
        private int reloadParamHash;
        private int fireParamHash;
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

            // Use .gameObject to instantiate from the Transform slot
            currentWeaponInstance = Instantiate(currentWeaponData.weaponPrefab.gameObject, weaponSocket);
            currentWeaponInstance.transform.localPosition = currentWeaponData.holdPosition;
            currentWeaponInstance.transform.localRotation = Quaternion.Euler(currentWeaponData.holdRotation);
            currentWeaponInstance.transform.localScale = currentWeaponData.localScale;
            
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

            bool canFire = !isActuallyReloading;
            
            if (shootingInput && canFire)
            {
                if (fireCooldownTimer <= 0f) fireCooldownTimer = 0.15f;
            }
            
            if (fireCooldownTimer > 0f) fireCooldownTimer -= Time.deltaTime;
            
            bool targetFireState = shootingInput && canFire && (fireCooldownTimer > 0.05f || shootingInput);
            
            if (targetFireState && !animator.GetBool(fireParamHash))
            {
                if (audioSource != null && currentWeaponData != null && currentWeaponData.audioFire != null)
                {
                    if (Time.time - lastFireSoundTime > 0.1f)
                    {
                        audioSource.PlayOneShot(currentWeaponData.audioFire, weaponVolume);
                        lastFireSoundTime = Time.time;
                    }
                }
            }

            animator.SetBool(fireParamHash, targetFireState);

            if (reloadPressed && !isActuallyReloading)
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
            if (currentWeaponData == null) return;

            if (audioSource != null && currentWeaponData.audioFire != null) 
            {
                if (Time.time - lastFireSoundTime > 0.1f)
                {
                    audioSource.PlayOneShot(currentWeaponData.audioFire, weaponVolume);
                    lastFireSoundTime = Time.time;
                }
            }

            // 2. Handle Particles (Muzzle Flash)
            if (currentWeaponData.muzzleFlash != null && currentWeaponInstance != null)
            {
                // Calculate muzzle world position and rotation based on the gun's current transform and offsets
                Vector3 worldMuzzlePos = currentWeaponInstance.transform.TransformPoint(currentWeaponData.muzzlePosition);
                Quaternion worldMuzzleRot = currentWeaponInstance.transform.rotation * Quaternion.Euler(currentWeaponData.muzzleRotation);

                // Use .gameObject to instantiate from the Transform slot
                GameObject flash = Instantiate(currentWeaponData.muzzleFlash.gameObject, worldMuzzlePos, worldMuzzleRot, currentWeaponInstance.transform);
                Destroy(flash, muzzleFlashLifeTime);
            }
        }

        public void Reload(AnimationEvent ae)
        {
            if (audioSource == null || currentWeaponData == null) return;
            if (ae.stringParameter == "MagOut" && currentWeaponData.audioMagOut != null) audioSource.PlayOneShot(currentWeaponData.audioMagOut, weaponVolume);
            else if (ae.stringParameter == "MagIn" && currentWeaponData.audioMagIn != null) audioSource.PlayOneShot(currentWeaponData.audioMagIn, weaponVolume);
        }
    }
}
