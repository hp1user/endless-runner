using UnityEngine;

namespace CameraControl
{
    /// <summary>
    /// High-performance camera shake controller using trauma-decay and Perlin noise.
    /// Runs with execution order 1000 so it executes in LateUpdate after Cinemachine has positioned the camera.
    /// Supports automatic lazy-initialization via CameraShake.ShakeCamera().
    /// </summary>
    [DefaultExecutionOrder(1000)]
    public class CameraShake : MonoBehaviour
    {
        public static CameraShake Instance { get; private set; }

        [Header("Perlin Noise Tuning")]
        [Tooltip("Frequency speed of the Perlin noise oscillation.")]
        [SerializeField] private float frequency = 24f;

        [Tooltip("Maximum rotational angular deviation in degrees.")]
        [SerializeField] private float maxRotationAngle = 2.5f;

        private float trauma = 0f;
        private float shakeDuration = 0.8f;
        private float shakeIntensity = 0.6f;
        private float currentTimer = 0f;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(this);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>
        /// Ensures a CameraShake instance exists in the scene, automatically attaching to Camera.main if needed.
        /// </summary>
        public static void EnsureInstance()
        {
            if (Instance != null) return;

            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                Instance = mainCam.GetComponent<CameraShake>();
                if (Instance == null)
                {
                    Instance = mainCam.gameObject.AddComponent<CameraShake>();
                }
            }
            else
            {
                GameObject autoShake = new GameObject("CameraShake_Auto");
                Instance = autoShake.AddComponent<CameraShake>();
            }
        }

        /// <summary>
        /// Global helper to shake the active camera with given duration and intensity.
        /// </summary>
        public static void ShakeCamera(float duration = 0.8f, float intensity = 0.6f)
        {
            EnsureInstance();
            if (Instance != null)
            {
                Instance.Shake(duration, intensity);
            }
        }

        /// <summary>
        /// Triggers a camera shake with quadratic trauma decay.
        /// </summary>
        public void Shake(float duration = 0.8f, float intensity = 0.6f)
        {
            shakeDuration = Mathf.Max(0.05f, duration);
            shakeIntensity = Mathf.Max(shakeIntensity, intensity);
            currentTimer = shakeDuration;
            trauma = 1f;
        }

        private void LateUpdate()
        {
            if (currentTimer <= 0f && trauma <= 0f) return;

            currentTimer -= Time.deltaTime;
            trauma = Mathf.Clamp01(currentTimer / shakeDuration);

            // Quadratic falloff (trauma^2) gives a visceral, sharp impact with a smooth settling tail
            float shakePower = trauma * trauma * shakeIntensity;

            if (shakePower <= 0.0005f)
            {
                currentTimer = 0f;
                trauma = 0f;
                return;
            }

            Camera cam = Camera.main;
            if (cam == null) return;

            // Compute smooth non-repeating Perlin noise offsets
            float seed = Time.time * frequency;
            float offsetX = (Mathf.PerlinNoise(seed, 0f) - 0.5f) * 2f * shakePower;
            float offsetY = (Mathf.PerlinNoise(0f, seed) - 0.5f) * 2f * shakePower;
            float offsetZ = (Mathf.PerlinNoise(seed, seed) - 0.5f) * 2f * (shakePower * 0.4f);

            Vector3 posOffset = cam.transform.right * offsetX + cam.transform.up * offsetY + cam.transform.forward * offsetZ;
            cam.transform.position += posOffset;

            // Subtle angular camera roll and pitch for cinematic weight
            float yaw = (Mathf.PerlinNoise(seed + 15f, 0f) - 0.5f) * 2f * maxRotationAngle * shakePower;
            float pitch = (Mathf.PerlinNoise(0f, seed + 25f) - 0.5f) * 2f * maxRotationAngle * shakePower;
            float roll = (Mathf.PerlinNoise(seed + 35f, seed + 45f) - 0.5f) * 2f * (maxRotationAngle * 0.5f) * shakePower;

            cam.transform.rotation = cam.transform.rotation * Quaternion.Euler(pitch, yaw, roll);
        }
    }
}

