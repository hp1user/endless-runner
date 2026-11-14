using System.Collections.Generic;
using UnityEngine;

namespace AcidSpitAdvanced
{
    // Separate bounded pools: a burst of skimming patches cannot grow without limit.
    public static class AcidPool
    {
        sealed class Bucket { public readonly List<GameObject> Items = new List<GameObject>(); }
        static readonly Dictionary<GameObject, Bucket> buckets = new Dictionary<GameObject, Bucket>();
        static Transform root;
        public static System.Func<Vector3> SurfaceVelocity;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() { buckets.Clear(); root = null; SurfaceVelocity = null; }

        public static void Prewarm(GameObject prefab, int count)
        {
            if (!prefab) return;
            EnsureRoot();
            if (!buckets.TryGetValue(prefab, out var bucket)) buckets.Add(prefab, bucket = new Bucket());
            while (bucket.Items.Count < count)
            {
                var item = Object.Instantiate(prefab, root);
                item.SetActive(false);
                bucket.Items.Add(item);
            }
        }

        static void EnsureRoot()
        {
            if (!root) root = new GameObject("Acid VFX Pool (bounded)").transform;
        }

        public static GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation, int cap)
        {
            if (!prefab) return null;
            EnsureRoot();
            if (!buckets.TryGetValue(prefab, out var bucket)) buckets.Add(prefab, bucket = new Bucket());
            GameObject item = null;
            for (int i = 0; i < bucket.Items.Count; i++)
                if (bucket.Items[i] && !bucket.Items[i].activeSelf) { item = bucket.Items[i]; break; }
            if (!item)
            {
                // Discard stale entries when returning from another scene.
                bucket.Items.RemoveAll(x => !x);
                if (bucket.Items.Count >= cap) return null;
                item = Object.Instantiate(prefab, root);
                bucket.Items.Add(item);
            }
            item.transform.SetPositionAndRotation(position, rotation);
            item.transform.localScale = prefab.transform.localScale;
            item.SetActive(true);
            return item;
        }
    }
}
