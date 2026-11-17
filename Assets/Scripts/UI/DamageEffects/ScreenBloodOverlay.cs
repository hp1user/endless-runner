using UnityEngine;
using UnityEngine.UI;
using UI.Control;

namespace EndlessRunner.UI
{
    /// <summary>
    /// On-screen blood damage overlay. Displays a visceral peripheral blood splatter and red damage vignette
    /// whenever the player takes a hit, with an optional pulsing heartbeat vignette during critical health (< 30%).
    /// Automatically self-initializes under the active scene UI Canvas.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup), typeof(Image))]
    public class ScreenBloodOverlay : MonoBehaviour
    {
        public static ScreenBloodOverlay Instance { get; private set; }

        private static Sprite cachedProceduralSprite = null;

        [Header("Visual Configuration")]
        [Tooltip("Optional custom blood splatter / vignette sprite. If left empty, a procedural blood splatter texture is generated.")]
        [SerializeField] private Sprite bloodVignetteSprite;

        [Tooltip("Base tint color for the blood damage overlay.")]
        [SerializeField] private Color bloodColor = new Color(0.80f, 0.04f, 0.04f, 1f);

        [Tooltip("Time in seconds for the damage blood flash to fade back to transparent.")]
        [SerializeField] private float fadeDuration = 0.65f;

        [Header("Critical Health Heartbeat (< 30% HP)")]
        [Tooltip("Whether to pulse a subtle blood vignette when player is at critical low health.")]
        [SerializeField] private bool enableCriticalPulse = true;

        [Tooltip("Speed of the critical heartbeat pulse.")]
        [SerializeField] private float pulseSpeed = 4.2f;

        [Tooltip("Minimum alpha during critical low-health heartbeat pulse.")]
        [SerializeField] private float minPulseAlpha = 0.18f;

        [Tooltip("Maximum alpha during critical low-health heartbeat pulse.")]
        [SerializeField] private float maxPulseAlpha = 0.52f;

        private Image overlayImage;
        private CanvasGroup canvasGroup;
        private float currentFlashAlpha = 0f;
        private bool isCritical = false;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                SetupComponents();
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
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
        /// Automatically ensures a ScreenBloodOverlay instance exists under the active Canvas.
        /// </summary>
        public static void EnsureInstance()
        {
            if (Instance != null) return;

            Canvas canvas = null;
            if (UIManager.Instance != null)
            {
                canvas = UIManager.Instance.GetComponentInParent<Canvas>();
            }
            if (canvas == null)
            {
                canvas = FindFirstObjectByType<Canvas>();
            }

            if (canvas != null)
            {
                Instance = canvas.GetComponentInChildren<ScreenBloodOverlay>(true);
                if (Instance == null)
                {
                    GameObject overlayObj = new GameObject("ScreenBloodOverlay", typeof(RectTransform), typeof(CanvasGroup), typeof(Image), typeof(ScreenBloodOverlay));
                    overlayObj.transform.SetParent(canvas.transform, false);
                    overlayObj.transform.SetAsFirstSibling(); // Render beneath HUD panels
                    Instance = overlayObj.GetComponent<ScreenBloodOverlay>();
                }
            }
        }

        /// <summary>
        /// Public static trigger called when player takes damage.
        /// </summary>
        public static void ShowDamageFlash(float damage, float currentHp, float maxHp)
        {
            EnsureInstance();
            if (Instance != null)
            {
                Instance.Flash(damage, currentHp, maxHp);
            }
        }

        /// <summary>
        /// Updates the persistent health state (e.g. to toggle low-health critical heartbeat pulse).
        /// </summary>
        public static void UpdateHealthState(float currentHp, float maxHp)
        {
            EnsureInstance();
            if (Instance != null)
            {
                Instance.SetHealthState(currentHp, maxHp);
            }
        }

        private void SetupComponents()
        {
            RectTransform rect = GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }

            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
                canvasGroup.alpha = 0f;
            }

            overlayImage = GetComponent<Image>();
            if (overlayImage != null)
            {
                overlayImage.raycastTarget = false;
                overlayImage.color = bloodColor;

                if (bloodVignetteSprite != null)
                {
                    overlayImage.sprite = bloodVignetteSprite;
                }
                else
                {
                    if (cachedProceduralSprite == null)
                    {
                        cachedProceduralSprite = GenerateProceduralBloodSprite();
                    }
                    overlayImage.sprite = cachedProceduralSprite;
                }
            }
        }

        public void Flash(float damage, float currentHp, float maxHp)
        {
            SetHealthState(currentHp, maxHp);

            // Scale flash opacity with damage received
            float hitIntensity = Mathf.Clamp(0.55f + (damage / 60f) * 0.45f, 0.55f, 1.0f);
            currentFlashAlpha = Mathf.Max(currentFlashAlpha, hitIntensity);

            if (canvasGroup != null)
            {
                canvasGroup.alpha = currentFlashAlpha;
            }
        }

        public void SetHealthState(float currentHp, float maxHp)
        {
            if (maxHp <= 0f) return;
            float ratio = currentHp / maxHp;
            isCritical = enableCriticalPulse && (ratio <= 0.30f && currentHp > 0f);
        }

        private void Update()
        {
            if (canvasGroup == null) return;

            // Decay damage flash
            if (currentFlashAlpha > 0f)
            {
                currentFlashAlpha = Mathf.MoveTowards(currentFlashAlpha, 0f, Time.deltaTime / Mathf.Max(0.1f, fadeDuration));
            }

            // Calculate target alpha
            float targetAlpha = currentFlashAlpha;

            if (isCritical)
            {
                // Heartbeat pulse curve: quick double-bump or quadratic sine pulse
                float sine = Mathf.Sin(Time.time * pulseSpeed);
                float pulse = Mathf.Pow(Mathf.Clamp01(sine * 0.5f + 0.5f), 1.6f);
                float critAlpha = Mathf.Lerp(minPulseAlpha, maxPulseAlpha, pulse);
                targetAlpha = Mathf.Max(currentFlashAlpha, critAlpha);
            }

            canvasGroup.alpha = targetAlpha;
        }

        /// <summary>
        /// Generates a procedural high-resolution blood vignette and splatter texture in memory.
        /// Leaves the center 100% transparent while creating realistic blood splatters around screen borders.
        /// </summary>
        private static Sprite GenerateProceduralBloodSprite()
        {
            int size = 512;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.name = "ProceduralBloodVignette";
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;

            Color[] pixels = new Color[size * size];
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            float maxRadius = center.magnitude;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = (float)x / size;
                    float v = (float)y / size;
                    float dist = Vector2.Distance(new Vector2(x, y), center) / maxRadius;

                    // Clear viewport center (transparent), smooth gradient towards borders
                    float vignette = Mathf.SmoothStep(0.38f, 0.98f, dist);

                    // Organic splatter shapes along perimeter
                    float noise1 = Mathf.PerlinNoise(u * 10f + 5.2f, v * 10f + 8.7f);
                    float noise2 = Mathf.PerlinNoise(u * 22f + 14.1f, v * 22f + 3.9f);
                    float splatterMask = Mathf.Clamp01((vignette * 0.82f) + (noise1 * 0.22f) + (noise2 * 0.18f * vignette));

                    // Peripheral droplets and coagulated blood spots
                    if (dist > 0.65f && noise2 > 0.62f)
                    {
                        splatterMask = Mathf.Min(1.0f, splatterMask + 0.35f);
                    }

                    // Deep arterial crimson shading
                    float edgeDepth = Mathf.Clamp01((dist - 0.45f) * 2.2f);
                    Color col = Color.Lerp(
                        new Color(0.85f, 0.05f, 0.05f),
                        new Color(0.35f, 0.01f, 0.01f),
                        edgeDepth * 0.65f
                    );
                    col.a = Mathf.Clamp01(splatterMask * vignette);

                    pixels[y * size + x] = col;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }
    }
}

