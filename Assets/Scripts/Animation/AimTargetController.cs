using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Animation.Tools
{
    /// <summary>
    /// Controls a target transform by moving it to the clicked position in world space.
    /// Used for baked animation aiming (Blend Trees).
    /// </summary>
    public class AimTargetController : MonoBehaviour
    {
        [Header("Target Configuration")]
        [Tooltip("The transform (e.g., Target Mesh or IK Target) that will move where you click.")]
        public Transform targetTransform;

        [Tooltip("The camera used for screen-to-world conversion. If null, Camera.main will be used.")]
        public Camera mainCamera;

        [Tooltip("Optional: The starting point of the ray (e.g., Gun Nozzle). If null, the Camera is used as the start.")]
        public Transform rayOriginOverride;

        [Header("Movement Settings")]
        [Tooltip("If true, the target will follow the mouse continuously while the button is held.")]
        public bool continuousFollow = true;

        [Tooltip("The fixed Z coordinate for the target object.")]
        public float lockedZ = 0f;

        [Tooltip("Optional: Layers to hit with the raycast. If nothing is hit, it will fallback to a plane at lockedZ.")]
        public LayerMask raycastLayers = ~0;

        [Header("Game View Visualization")]
        [Tooltip("Optional: An object (e.g., a small sphere or crosshair) that will move to the hit point in Play mode.")]
        public Transform hitMarker;

        [Header("Debug Visualization")]
        public bool showDebugRay = true;
        public Color rayColor = Color.red;
        public float rayDisplayDuration = 0.1f;

        private Vector3 lastRayStart;
        private Vector3 lastHitPoint;
        private bool hasHitSomething;
        private bool isCurrentlyPressed;
        private Transform myTransform;

        public bool IsAiming => isCurrentlyPressed;

        private void Start()
        {
            myTransform = transform;

            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            // If no target is assigned, try to use this object's transform
            if (targetTransform == null)
            {
                targetTransform = myTransform;
            }

            // Set initial Z if not specified (optional, but good for keeping original depth)
            if (lockedZ == 0f && targetTransform != null)
            {
                lockedZ = targetTransform.position.z;
            }
        }

        private void Update()
        {
            isCurrentlyPressed = TouchManager.IsShooting;

            if (isCurrentlyPressed)
            {
                MoveTargetToMouse();
            }
        }

        private void MoveTargetToMouse()
        {
            if (mainCamera == null || targetTransform == null) return;

            // Fetch the coordinates from TouchManager
            Vector2 mousePos = TouchManager.CurrentTouchPosition;

            // ALWAYS raycast from camera to determine WHERE the user clicked in the world
            Ray cameraRay = mainCamera.ScreenPointToRay(mousePos);

            // The visual start point of our debug ray (e.g., Gun Nozzle)
            lastRayStart = (rayOriginOverride != null) ? rayOriginOverride.position : cameraRay.origin;
            
            // Try to hit something in the world through the camera view first
            if (Physics.Raycast(cameraRay, out RaycastHit hit, Mathf.Infinity, raycastLayers))
            {
                lastHitPoint = hit.point;
                hasHitSomething = true;
                
                Vector3 targetPos = hit.point;
                targetPos.z = lockedZ; // Lock Z strictly to X and Y
                targetTransform.position = targetPos;
            }
            else
            {
                // Fallback: If no hit, project screen point onto an imaginary plane at lockedZ
                Plane plane = new Plane(Vector3.forward, new Vector3(0, 0, lockedZ));
                if (plane.Raycast(cameraRay, out float enter))
                {
                    lastHitPoint = cameraRay.GetPoint(enter);
                    hasHitSomething = true;
                    
                    Vector3 targetPos = lastHitPoint;
                    targetPos.z = lockedZ;
                    targetTransform.position = targetPos;
                }
            }

            // Visual visualization from Nozzle to Hit Point (Scene View Only)
            if (showDebugRay && hasHitSomething)
            {
                Debug.DrawLine(lastRayStart, lastHitPoint, rayColor, rayDisplayDuration);
            }

            // Game View Visualization using Hit Marker
            if (hitMarker != null)
            {
                hitMarker.gameObject.SetActive(hasHitSomething);
                if (hasHitSomething)
                {
                    hitMarker.position = lastHitPoint;
                }
            }
        }

        private void LateUpdate()
        {
            // If the user is NOT pressing the button, disable the game-view visuals
            if (!isCurrentlyPressed)
            {
                if (hitMarker != null) hitMarker.gameObject.SetActive(false);
                hasHitSomething = false; // Reset hit state when released
            }
        }

        private void OnDrawGizmos()
        {
            if (!showDebugRay || !hasHitSomething || !Application.isPlaying) return;

            Gizmos.color = rayColor;
            // Draw a small sphere at the start (camera/ray origin)
            Gizmos.DrawWireSphere(lastRayStart, 0.2f);
            
            // Draw a sphere at the hit point
            Gizmos.DrawSphere(lastHitPoint, 0.15f);
            
            // Draw a line connecting them (Gizmos are more persistent in Scene view)
            Gizmos.DrawLine(lastRayStart, lastHitPoint);

            // Optional: Draw a marker for the locked Z target position
            if (targetTransform != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireCube(targetTransform.position, Vector3.one * 0.25f);
            }
        }

        /// <summary>
        /// Public method to manually set the locked Z depth.
        /// </summary>
        public void SetLockedZ(float newZ)
        {
            lockedZ = newZ;
        }
    }
}
