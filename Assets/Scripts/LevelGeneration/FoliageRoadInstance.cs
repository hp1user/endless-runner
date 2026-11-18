using UnityEngine;

namespace EndlessRunner.LevelGen
{
    /// <summary>
    /// Applies a seamless chunk sequence noise offset to the Road Foliage Material via MaterialPropertyBlock.
    /// This ensures every chunk has a unique, evolving moss and gravel distribution, while remaining
    /// 100% seamless at chunk boundaries and 100% immune to texture sliding during movement.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class FoliageRoadInstance : MonoBehaviour
    {
        [Tooltip("Chunk sequence offset along the highway (e.g. chunkIndex * 40m).")]
        public float chunkSequenceOffset = 0f;

        [Tooltip("Auto-detect chunk index from parent/root name (e.g. Chunk_3 -> 80m)")]
        public bool autoDetectFromHierarchy = true;

        private static MaterialPropertyBlock sharedMpb;
        private static readonly int NoiseOffsetProp = Shader.PropertyToID("_NoiseOffset");

        private void Awake()
        {
            ApplyRoadOffset();
        }

        private void OnEnable()
        {
            ApplyRoadOffset();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ApplyRoadOffset();
        }
#endif

        public void ApplyRoadOffset()
        {
            Renderer r = GetComponent<Renderer>();
            if (r == null) return;

            if (sharedMpb == null) sharedMpb = new MaterialPropertyBlock();
            r.GetPropertyBlock(sharedMpb);

            float offset = chunkSequenceOffset;

            if (autoDetectFromHierarchy)
            {
                Transform root = transform.root;
                if (root != null)
                {
                    string rootName = root.name;
                    int lastUnder = rootName.LastIndexOf('_');
                    if (lastUnder >= 0)
                    {
                        string numPart = rootName.Substring(lastUnder + 1).Replace("(Clone)", "").Trim();
                        if (int.TryParse(numPart, out int chunkNum))
                        {
                            offset = (chunkNum - 1) * 40f;
                        }
                    }
                    else if (root.position.sqrMagnitude > 0.01f)
                    {
                        offset = root.position.z;
                    }
                }
            }

            sharedMpb.SetFloat(NoiseOffsetProp, offset);
            r.SetPropertyBlock(sharedMpb);
        }

        public void SetManualOffset(float offset)
        {
            autoDetectFromHierarchy = false;
            chunkSequenceOffset = offset;
            ApplyRoadOffset();
        }
    }
}

