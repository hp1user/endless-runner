using UnityEngine;
using TMPro;

namespace UI.DamageNumbers
{
    public enum DamageStyle
    {
        Normal,         // Regular enemy hit (clean light yellow / white)
        BossGeneral,    // General boss hit (amber / yellow)
        BossHead,       // Boss Head critical hit (fiery red / gold with "HEAD" label)
        BossLeftLeg,    // Boss Left Leg hit (vivid orange with "L-LEG" label)
        BossRightLeg,   // Boss Right Leg hit (vivid orange with "R-LEG" label)
        BossBroken,     // Broken part hit (silver / grey)
        Critical        // High damage crit
    }

    /// <summary>
    /// Individual floating 3D TextMeshPro damage number.
    /// Handles camera billboarding, punchy pop animation, upward float, and fade-out.
    /// </summary>
    [RequireComponent(typeof(TextMeshPro))]
    public class DamageNumber : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private TextMeshPro tmpText;

        [Header("Motion Settings")]
        [SerializeField] private float floatSpeed = 1.8f;
        [SerializeField] private float driftSpeed = 0.45f;
        [SerializeField] private float duration = 0.75f;

        private Camera cachedCamera;
        private float timer = 0f;
        private float baseScale = 1.0f;
        private Color targetColor = Color.white;
        private Vector3 driftVelocity;
        private bool isPlaying = false;

        private void Awake()
        {
            if (tmpText == null) tmpText = GetComponent<TextMeshPro>();
        }

        public void Initialize(TextMeshPro tmp)
        {
            tmpText = tmp;
        }

        public void Play(string formattedText, Vector3 worldPosition, Color color, float scaleMultiplier = 1.0f, float customDuration = 0.75f, float customFloatSpeed = 1.8f, float customDriftSpeed = 0.45f)
        {
            if (tmpText == null) tmpText = GetComponent<TextMeshPro>();

            if (cachedCamera == null) cachedCamera = Camera.main;

            // Small random horizontal drift so rapid hits (shotguns / auto-fire) fan out cleanly
            float randomX = Random.Range(-0.6f, 0.6f);
            float randomY = Random.Range(0.2f, 0.8f);
            driftVelocity = new Vector3(randomX, randomY, 0f).normalized;

            transform.position = worldPosition;
            if (cachedCamera != null)
            {
                transform.rotation = cachedCamera.transform.rotation;
            }

            baseScale = scaleMultiplier;
            targetColor = color;
            duration = customDuration;
            floatSpeed = customFloatSpeed;
            driftSpeed = customDriftSpeed;
            timer = 0f;
            isPlaying = true;

            tmpText.text = formattedText;
            tmpText.color = targetColor;
            tmpText.alpha = 1.0f;

            // Start small for pop animation
            transform.localScale = Vector3.one * (baseScale * 0.45f);
            gameObject.SetActive(true);
        }

        private void Update()
        {
            if (!isPlaying) return;

            timer += Time.deltaTime;

            // 1. BILLBOARD: Always face camera perfectly
            if (cachedCamera == null) cachedCamera = Camera.main;
            if (cachedCamera != null)
            {
                transform.rotation = cachedCamera.transform.rotation;
            }

            // 2. POSITION: Float upwards with subtle horizontal drift
            transform.position += (Vector3.up * floatSpeed + driftVelocity * driftSpeed) * Time.deltaTime;

            // 3. SCALE POP: Snappy bounce punch (0.0s -> 0.10s -> 0.22s)
            float currentScale = baseScale;
            if (timer < 0.10f)
            {
                float t = timer / 0.10f;
                currentScale = Mathf.Lerp(baseScale * 0.45f, baseScale * 1.30f, t);
            }
            else if (timer < 0.22f)
            {
                float t = (timer - 0.10f) / 0.12f;
                currentScale = Mathf.Lerp(baseScale * 1.30f, baseScale * 1.0f, t);
            }
            else
            {
                currentScale = baseScale;
            }
            transform.localScale = Vector3.one * currentScale;

            // 4. ALPHA FADE: Smooth fade out over the final 40% of duration
            float fadeStartTime = duration * 0.55f;
            if (timer >= fadeStartTime)
            {
                float fadeT = (timer - fadeStartTime) / (duration - fadeStartTime);
                tmpText.alpha = Mathf.Lerp(1.0f, 0.0f, fadeT);
            }

            // 5. LIFETIME COMPLETION
            if (timer >= duration)
            {
                Stop();
            }
        }

        public void Stop()
        {
            isPlaying = false;
            gameObject.SetActive(false);
            DamageNumberManager.Instance?.ReturnToPool(this);
        }
    }
}

