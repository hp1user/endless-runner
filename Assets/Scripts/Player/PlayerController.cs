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

        // Private Settings (Hardcoded as requested)
        private string reloadParameter = "Reload"; // Updated to Capital R
        private string fireParameter = "isFiring";
        private string reloadStateName = "Reload";
        private string reloadStateTag = "Reload";

        [Header("Debug")]
        public bool debugMode = false;

        // Internal State
        private float currentStrafe;
        private float targetStrafe;
        private float targetLayerWeight = 1f; // Always active for persistent weapon layers
        private float currentLayerWeight;
        private float lastActionTime;

        private void Awake()
        {
            animator = GetComponent<Animator>();
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
            Debug.Log($"<color=green>[PlayerController]</color> Script is ACTIVE on <b>{gameObject.name}</b>. Press R to Reload.");
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
            HandleMovement();
            HandleActions();
            UpdateLayerWeights();
        }

        private void HandleMovement()
        {
            if (animator == null) return;

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
            animator.SetFloat(strafeParameter, currentStrafe);

            // One-Shot Logic: Once we reach (or get very close to) our target lane, return to 0
            if (Mathf.Abs(currentStrafe - targetStrafe) < 0.05f && targetStrafe != 0f)
            {
                targetStrafe = 0f;
            }
        }

        private void HandleActions()
        {
            if (animator == null) return;

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

            // EMERGENCY DEBUG: If you press R, we want to know even if isActuallyReloading is true
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame && debugMode)
                Debug.Log("[PlayerController] RAW KEYBOARD 'R' DETECTED.");
#else
            if (Input.GetKeyDown(KeyCode.R) && debugMode)
                Debug.Log("[PlayerController] RAW KEYBOARD 'R' DETECTED.");
#endif

            // Handle Firing State
            bool canFire = !isActuallyReloading;
            animator.SetBool(fireParameter, shootingInput && canFire);

            // Handle Reload Input
            if (reloadPressed)
            {
                if (!isActuallyReloading)
                {
                    if (debugMode) Debug.Log($"[PlayerController] EXECUTE RELOAD. Setting Bool '{reloadParameter}' to TRUE.");
                    animator.SetBool(reloadParameter, true);
                    Invoke(nameof(ResetReloadParameter), 0.15f); // Slightly longer for safety
                }
                else if (debugMode)
                {
                    Debug.LogWarning($"[PlayerController] RELOAD BLOCKED. isActuallyReloading is TRUE.");
                }
            }

            // Keep weapon layer fully active
            targetLayerWeight = 1f;
        }

        private bool IsReloadingAnimationPlaying()
        {
            if (animator == null || weaponLayerIndex < 0 || weaponLayerIndex >= animator.layerCount) return false;

            // 1. Check current state
            AnimatorStateInfo currentState = animator.GetCurrentAnimatorStateInfo(weaponLayerIndex);
            bool isInReload = currentState.IsName(reloadStateName) || (!string.IsNullOrEmpty(reloadStateTag) && currentState.IsTag(reloadStateTag));
            
            // 2. Check next state (transitioning into)
            bool isTransitioningToReload = false;
            if (animator.IsInTransition(weaponLayerIndex))
            {
                AnimatorStateInfo nextState = animator.GetNextAnimatorStateInfo(weaponLayerIndex);
                isTransitioningToReload = nextState.IsName(reloadStateName) || (!string.IsNullOrEmpty(reloadStateTag) && nextState.IsTag(reloadStateTag));
            }

            bool result = isInReload || isTransitioningToReload;

            // Special debug logs to help identify the "1 second" issue
            if (debugMode && !result && Time.time < lastActionTime + 2.0f && Time.time > lastActionTime)
            {
                // We just pressed reload but it's not detected yet
                Debug.LogWarning($"[PlayerController] Reload triggered but not yet detected in Layer {weaponLayerIndex}. " + 
                                 $"Current State Full Path: {currentState.fullPathHash}");
            }

            if (result && isInReload)
            {
                return currentState.normalizedTime < 0.99f;
            }
            
            return result;
        }

        private void ResetReloadParameter()
        {
            animator.SetBool(reloadParameter, false);
        }

        private void UpdateLayerWeights()
        {
            if (animator == null) return;

            // We iterate through all layers starting from index 1 (leaving Base Layer 0 alone)
            // This ensures that when you switch weapons, the old ones fade out and the new one fades in.
            for (int i = 1; i < animator.layerCount; i++)
            {
                // Determine target weight for this layer
                float target = (i == weaponLayerIndex) ? targetLayerWeight : 0f;
                
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
