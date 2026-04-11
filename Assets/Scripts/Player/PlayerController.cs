using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Player.Control
{
    /// <summary>
    /// Player Controller for an endless runner.
    /// Handles discrete horizontal movement (Starf), weapon layers, and reloading.
    /// UsesGetComponent for Animator and discrete input for strafing.
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        private Animator animator;

        [Header("Movement Settings")]
        [Tooltip("The name of the float parameter for horizontal movement in the animator.")]
        public string strafeParameter = "Starf";
        
        [Tooltip("How fast the Starf value reaches the target (-1, 0, 1).")]
        public float strafeSmoothing = 8f;

        [Header("Weapon Layer Settings")]
        [Tooltip("The index of the weapon animation layer (e.g., 1 for Assault Rifle).")]
        public int weaponLayerIndex = 1;

        [Tooltip("How fast the layer weight blends to 1 when switching weapons.")]
        public float layerBlendSpeed = 10f;

        [Tooltip("The Transform to calculate the aiming direction from (e.g., Chest or Head). If null, a point above the feet will be used.")]
        public Transform aimOrigin;

        [Tooltip("The Transform that the player should aim at (e.g. moved by mouse).")]
        public Transform aimTarget;

        [Tooltip("Name of the horizontal aim parameter.")]
        public string aimHorizontalParameter = "Aim_Horizon";

        [Tooltip("Name of the vertical aim parameter.")]
        public string aimVerticalParameter = "Aim_Vertical";

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

        // Private Hashes for Performance (Mobile Optimization)
        private int strafeParamHash;
        private int reloadParamHash;
        private int fireParamHash;
        private int aimHorizontalHash;
        private int aimVerticalHash;
        private int reloadStateHash;
        private int reloadTagHash;

        // Private Settings (Hardcoded as requested)
        private string reloadParameter = "Reload"; // Updated to Capital R
        private string fireParameter = "isFiring";
        private string reloadStateName = "Reload";
        private string reloadStateTag = "Reload";

        [Header("Debug")]
        public bool debugMode = false;
        [SerializeField] private float debugAimH;
        [SerializeField] private float debugAimV;

        // Internal State
        private float currentStrafe;
        private float targetStrafe;
        private float currentAimH;
        private float currentAimV;
        private float targetAimH; // Persistent target for horizontal aiming
        private float targetAimV; // Persistent target for vertical aiming
        private float targetLayerWeight = 1f; // Always active for persistent weapon layers
        private float lastActionTime;
        private AnimatorStateInfo currentWeaponState;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            
            // Pre-calculate hashes to avoid string comparisons in Update (Essential for mobile)
            strafeParamHash = Animator.StringToHash(strafeParameter);
            reloadParamHash = Animator.StringToHash(reloadParameter);
            fireParamHash = Animator.StringToHash(fireParameter);
            aimHorizontalHash = Animator.StringToHash(aimHorizontalParameter);
            aimVerticalHash = Animator.StringToHash(aimVerticalParameter);
            
            // We use fullPathHash or shortNameHash for state checks. 
            // Since we know the layer, shortNameHash is efficient if unique.
            reloadStateHash = Animator.StringToHash(reloadStateName);
            reloadTagHash = Animator.StringToHash(reloadStateTag);

            if (animator == null)
            {
                Debug.LogWarning("[PlayerController] No Animator found on the same GameObject!");
            }
            else if (debugMode)
            {
                LogAnimatorParameters();
            }
        }

        private void Start()
        {
            // Guaranteed log to prove the script is active
            Debug.Log($"<color=green>[PlayerController]</color> Optimized Script is ACTIVE on <b>{gameObject.name}</b>.");
        }

        private void LogAnimatorParameters()
        {
            Debug.Log($"[PlayerController] Scanning Animator Parameters on {gameObject.name}:");
            for (int i = 0; i < animator.parameterCount; i++)
            {
                var p = animator.parameters[i];
                Debug.Log($" - Parameter {i}: '{p.name}' (Type: {p.type})");
            }
        }

        private void Update()
        {
            if (animator == null) return;

            // Cache the state once per frame for all check methods
            if (weaponLayerIndex >= 0 && weaponLayerIndex < animator.layerCount)
            {
                currentWeaponState = animator.GetCurrentAnimatorStateInfo(weaponLayerIndex);
            }

            HandleMovement();
            HandleAiming();
            HandleActions();
            UpdateLayerWeights();
        }

        private void HandleMovement()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                if (Keyboard.current.aKey.wasPressedThisFrame || Keyboard.current.leftArrowKey.wasPressedThisFrame) 
                    targetStrafe = -1f;
                else if (Keyboard.current.dKey.wasPressedThisFrame || Keyboard.current.rightArrowKey.wasPressedThisFrame) 
                    targetStrafe = 1f;
            }
#else
            if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
                targetStrafe = -1f;
            else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
                targetStrafe = 1f;
#endif

            // Smoothly interpolate the Starf parameter to the target value
            currentStrafe = Mathf.MoveTowards(currentStrafe, targetStrafe, Time.deltaTime * strafeSmoothing);
            animator.SetFloat(strafeParamHash, currentStrafe);

            // One-Shot Logic: Once we reach (or get very close to) our target lane, return to 0
            if (Mathf.Abs(currentStrafe - targetStrafe) < 0.05f && targetStrafe != 0f)
            {
                targetStrafe = 0f;
            }
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
                // Determine origin point (fallback to 1.5m above feet if not assigned)
                Vector3 origin = (aimOrigin != null) ? aimOrigin.position : transform.position + Vector3.up * 1.5f;

                // Calculate direction from origin to target in world space
                Vector3 directionToTarget = (aimTarget.position - origin).normalized;

                // Convert direction to local space relative to player's rotation
                Vector3 localDir = transform.InverseTransformDirection(directionToTarget);

                // Calculate angles (assuming Z forward)
                // Yaw is on the XZ plane
                float yaw = Mathf.Atan2(localDir.x, localDir.z) * Mathf.Rad2Deg;
                
                // Pitch is the angle away from the horizontal plane
                float pitch = Mathf.Atan2(localDir.y, new Vector2(localDir.x, localDir.z).magnitude) * Mathf.Rad2Deg;

                // Normalize to -1 to 1 range based on max angles
                // Swapped the horizontal sign to fix inversion (-yaw)
                targetAimH = Mathf.Clamp(-yaw / maxAimAngleHorizontal, -1f, 1f);
                targetAimV = Mathf.Clamp(pitch / maxAimAngleVertical, -1f, 1f);

                // Apply manual inversions if checked in inspector
                if (invertHorizontal) targetAimH *= -1f;
                if (invertVertical) targetAimV *= -1f;
            }

            // MoveTowards ensures we reach the target exactly (preventing tiny e-38 residues)
            currentAimH = Mathf.MoveTowards(currentAimH, targetAimH, Time.deltaTime * aimSmoothing);
            currentAimV = Mathf.MoveTowards(currentAimV, targetAimV, Time.deltaTime * aimSmoothing);
            
            // Note: We removed the reset/snap-to-zero logic to keep the aim "sticky"

            // Update debug fields for Inspector visibility
            debugAimH = currentAimH;
            debugAimV = currentAimV;

            // Set Animator parameters
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

            // Handle Firing State
            bool canFire = !isActuallyReloading;
            animator.SetBool(fireParamHash, shootingInput && canFire);

            // Handle Reload Input
            if (reloadPressed && !isActuallyReloading)
            {
                animator.SetBool(reloadParamHash, true);
                Invoke(nameof(ResetReloadParameter), 0.15f); // Slightly longer for safety
            }

            // Keep weapon layer fully active
            targetLayerWeight = 1f;
        }

        private bool IsReloadingAnimationPlaying()
        {
            if (animator == null || weaponLayerIndex < 0 || weaponLayerIndex >= animator.layerCount) return false;

            // Use the cached state info from Update to avoid multiple API calls per frame
            bool isInReload = currentWeaponState.shortNameHash == reloadStateHash || currentWeaponState.tagHash == reloadTagHash;
            
            // 2. Check next state (transitioning into)
            bool isTransitioningToReload = false;
            if (animator.IsInTransition(weaponLayerIndex))
            {
                AnimatorStateInfo nextState = animator.GetNextAnimatorStateInfo(weaponLayerIndex);
                isTransitioningToReload = nextState.shortNameHash == reloadStateHash || nextState.tagHash == reloadTagHash;
            }

            if (isInReload)
            {
                return currentWeaponState.normalizedTime < 0.99f;
            }
            
            return isTransitioningToReload;
        }

        private void ResetReloadParameter()
        {
            animator.SetBool(reloadParamHash, false);
        }

        private void UpdateLayerWeights()
        {
            if (animator == null) return;

            // We iterate through all layers starting from index 1 (leaving Base Layer 0 alone)
            for (int i = 1; i < animator.layerCount; i++)
            {
                // Determine target weight for this layer
                // Both Aim and Weapon layers should be active (weight 1)
                float target = (i == weaponLayerIndex || i == aimLayerIndex) ? targetLayerWeight : 0f;
                
                // Get current weight
                float current = animator.GetLayerWeight(i);
                
                // If we are not at the target, move towards it
                if (!Mathf.Approximately(current, target))
                {
                    float next = Mathf.MoveTowards(current, target, Time.deltaTime * layerBlendSpeed);
                    animator.SetLayerWeight(i, next);
                }
            }
        }

        public void SetWeaponLayer(int newIndex)
        {
            // Now we just change the index and the Update loop handles the smooth fading
            weaponLayerIndex = newIndex;
        }
    }
}
