using UnityEngine;

namespace EndlessRunner.LevelGen
{
    /// <summary>
    /// Applies a deterministic chunk-relative noise offset to the Foliage Shader
    /// via MaterialPropertyBlock for modular highway props (dividers, railings).
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class FoliagePropInstance : MonoBehaviour
    {
        private static MaterialPropertyBlock sharedMpb;
        private static readonly int NoiseOffsetProp = Shader.PropertyToID("_NoiseOffset");

        private void Awake()
        {
            ApplyNoiseOffset();
        }

        private void OnEnable()
        {
            ApplyNoiseOffset();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ApplyNoiseOffset();
        }
#endif

        public void ApplyNoiseOffset()
        {
            Renderer r = GetComponent<Renderer>();
            if (r == null) return;

            if (sharedMpb == null) sharedMpb = new MaterialPropertyBlock();
            r.GetPropertyBlock(sharedMpb);

            Vector3 localPos = transform.localPosition;
            float hash = (localPos.z * 1.7315f) + (localPos.x * 5.1273f) + (localPos.y * 3.4191f);
            
            if (Mathf.Abs(localPos.z) < 0.001f && transform.parent != null)
            {
                Vector3 parentLocal = transform.parent.localPosition;
                hash += (parentLocal.z * 1.7315f) + (parentLocal.x * 5.1273f);
            }

            sharedMpb.SetFloat(NoiseOffsetProp, hash);
            r.SetPropertyBlock(sharedMpb);
        }
    }
}

