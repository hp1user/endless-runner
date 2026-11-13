using UnityEngine;
using UnityEngine.VFX;

namespace VFX
{
    public class EMPScaleGrowth : MonoBehaviour
    {
        [Header("Growth Settings")]
        [Tooltip("Transform that scales up. If null, uses this GameObject's transform.")]
        [SerializeField] private Transform visualTransform;

        [Tooltip("Initial scale when spawned.")]
        [SerializeField] private float startScale = 0.5f;

        [Tooltip("Maximum peak scale covering the 10m road width.")]
        [SerializeField] private float targetScale = 5.0f;

        [Tooltip("Time in seconds to expand from startScale to targetScale.")]
        [SerializeField] private float growthDuration = 1.2f;

        [Tooltip("Easing curve for growth expansion.")]
        [SerializeField] private AnimationCurve growthCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("VFX & Lighting")]
        [SerializeField] private VisualEffect visualEffect;
        [SerializeField] private Light flashLight;
        [SerializeField] private float maxLightIntensity = 6f;
        [SerializeField] private float maxLightRange = 16f;

        private float elapsed = 0f;
        private bool hasStoppedVFX = false;

        private void Awake()
        {
            if (visualTransform == null) visualTransform = transform;
            if (visualEffect == null) visualEffect = GetComponentInChildren<VisualEffect>();
            if (flashLight == null) flashLight = GetComponentInChildren<Light>();

            // Set initial starting scale
            visualTransform.localScale = Vector3.one * startScale;

            if (flashLight != null)
            {
                flashLight.intensity = maxLightIntensity;
                flashLight.range = 3f;
            }

            // Ensure VFX is running and continuously looping during the growth phase
            if (visualEffect != null)
            {
                visualEffect.SetBool("Loop", true);
                visualEffect.Play();
            }
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / growthDuration);
            float curveValue = (growthCurve != null) ? growthCurve.Evaluate(t) : Mathf.SmoothStep(0f, 1f, t);

            // Gradually grow scale from startScale to targetScale
            float currentScale = Mathf.Lerp(startScale, targetScale, curveValue);
            visualTransform.localScale = Vector3.one * currentScale;

            // Update dynamic light range and fade
            if (flashLight != null)
            {
                flashLight.range = Mathf.Lerp(3f, maxLightRange, curveValue);
                if (elapsed >= growthDuration)
                {
                    float fadeT = Mathf.Clamp01((elapsed - growthDuration) / 0.5f);
                    flashLight.intensity = Mathf.Lerp(maxLightIntensity, 0f, fadeT);
                }
            }

            // When growth finishes reaching full 10m radius, stop emitting new particles
            if (elapsed >= growthDuration && !hasStoppedVFX)
            {
                hasStoppedVFX = true;
                if (visualEffect != null)
                {
                    visualEffect.SetBool("Loop", false);
                    visualEffect.Stop();
                }
            }
        }
    }
}

