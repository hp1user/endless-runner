using UnityEngine;

namespace VFX
{
    /// <summary>
    /// Animates the Boss Acid Orb with multi-harmonic organic liquid wobble
    /// and 3-axis asymmetric fluid tumbling rotation.
    /// Simulates surface tension deformation of an incompressible liquid blob.
    /// </summary>
    public class BossAcidOrbVisual : MonoBehaviour
    {
        [Header("Harmonic Wobble Settings")]
        public float wobbleSpeed = 13.5f;
        public float wobbleAmount = 0.16f;
        public Vector3 baseScale = new Vector3(1.35f, 1.35f, 1.45f);

        [Header("Fluid Tumbling Spin")]
        public Vector3 spinVelocity = new Vector3(35f, 55f, 180f);

        private void Update()
        {
            // 1. Multi-harmonic sinusoidal fluid oscillation (Dual octave with golden ratio)
            // Primary wave simulates core liquid bulge; secondary wave adds chaotic fluid ripple
            float t = Time.time * wobbleSpeed;
            float wave1 = Mathf.Sin(t) * wobbleAmount;
            float wave2 = Mathf.Sin(t * 1.618f + 1.2f) * (wobbleAmount * 0.45f);
            float combinedPrimary = wave1 + wave2;

            float crossWave = Mathf.Cos(t * 1.25f) * (wobbleAmount * 0.35f);

            // Volume-preserving deformation: subtracting compression from Y while distributing outward to X & Z
            transform.localScale = new Vector3(
                baseScale.x + combinedPrimary,
                baseScale.y - combinedPrimary + crossWave,
                baseScale.z + combinedPrimary * 0.45f - crossWave * 0.45f
            );

            // 2. 3-Axis continuous asymmetric fluid tumbling (gives non-uniform churning motion)
            transform.Rotate(spinVelocity * Time.deltaTime, Space.Self);
        }
    }
}
