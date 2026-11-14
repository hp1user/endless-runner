using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoolManager : MonoBehaviour
{
    public static PoolManager Instance { get; private set; }

    // This dictionary holds multiple pools. 
    // Key = The original Prefab, Value = A Queue of recycled, invisible clones.
    private Dictionary<GameObject, Queue<GameObject>> poolDictionary = new Dictionary<GameObject, Queue<GameObject>>();

    // Maximum instances allowed per prefab pool to prevent runaway memory usage during rapid fire
    private const int MAX_POOL_CAP_PER_PREFAB = 12;
    private Dictionary<GameObject, int> totalSpawnedCounts = new Dictionary<GameObject, int>();

    private void Awake()
    {
        // Standard Singleton setup so systems can easily talk to it
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Prewarms a specific prefab by instantiating instances ahead of time and running
    /// a single-frame simulation off-screen. This forces the GPU / Vulkan driver to compile
    /// compute shader kernels and Pipeline State Objects (PSOs) at startup, eliminating the
    /// 1-second first-hit stutter on mobile devices like Pixel 7a.
    /// </summary>
    public void PrewarmPool(GameObject prefab, int count, bool warmupShaders = true)
    {
        if (prefab == null || count <= 0) return;

        if (!poolDictionary.ContainsKey(prefab))
        {
            poolDictionary.Add(prefab, new Queue<GameObject>());
            totalSpawnedCounts.Add(prefab, 0);
        }

        Vector3 offscreenPos = new Vector3(0f, -500f, 0f);

        for (int i = 0; i < count; i++)
        {
            GameObject obj = Instantiate(prefab, offscreenPos, Quaternion.identity, this.transform);
            totalSpawnedCounts[prefab]++;

            if (warmupShaders)
            {
                obj.SetActive(true);

                var vfxArray = obj.GetComponentsInChildren<UnityEngine.VFX.VisualEffect>(true);
                for (int v = 0; v < vfxArray.Length; v++)
                {
                    if (vfxArray[v] != null)
                    {
                        vfxArray[v].Reinit();
                        vfxArray[v].Play();
                        vfxArray[v].Simulate(0.016f); // Triggers driver compute & PSO compilation
                        vfxArray[v].Stop();
                    }
                }

                var psArray = obj.GetComponentsInChildren<ParticleSystem>(true);
                for (int p = 0; p < psArray.Length; p++)
                {
                    if (psArray[p] != null)
                    {
                        psArray[p].Simulate(0.016f, true, true);
                        psArray[p].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    }
                }
            }

            obj.SetActive(false);
            poolDictionary[prefab].Enqueue(obj);
        }

        Debug.Log($"<color=cyan>[PoolManager]</color> Pre-warmed {count} instances of '{prefab.name}' (PSO compiled).");
    }

    /// <summary>
    /// Grabs an inactive object from the pool, or creates a new one if the pool is empty.
    /// </summary>
    public GameObject SpawnFromPool(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        if (prefab == null) return null;

        // 1. If this is the first time seeing this prefab, create a new empty Queue for it
        if (!poolDictionary.ContainsKey(prefab))
        {
            poolDictionary.Add(prefab, new Queue<GameObject>());
            totalSpawnedCounts.Add(prefab, 0);
        }

        GameObject objectToSpawn = null;

        // 2. Check if we have an invisible, recycled object waiting in the queue
        if (poolDictionary[prefab].Count > 0)
        {
            objectToSpawn = poolDictionary[prefab].Dequeue();
            if (objectToSpawn != null)
            {
                objectToSpawn.transform.position = position;
                objectToSpawn.transform.rotation = rotation;
                objectToSpawn.transform.SetParent(parent);
                objectToSpawn.SetActive(true);
            }
        }
        else
        {
            // 3. The pool is empty. Check if we reached the maximum instance cap
            if (totalSpawnedCounts[prefab] < MAX_POOL_CAP_PER_PREFAB)
            {
                objectToSpawn = Instantiate(prefab, position, rotation, parent);
                totalSpawnedCounts[prefab]++;
            }
            else
            {
                // Pool cap reached: Reuse the oldest inactive object if available, or force spawn if necessary
                objectToSpawn = Instantiate(prefab, position, rotation, parent);
            }
        }

        // 4. Ensure recycled VFX Graph and ParticleSystem effects restart properly
        if (objectToSpawn != null)
        {
            var vfxArray = objectToSpawn.GetComponentsInChildren<UnityEngine.VFX.VisualEffect>();
            for (int i = 0; i < vfxArray.Length; i++)
            {
                vfxArray[i].Reinit();
                vfxArray[i].Play();
            }

            var psArray = objectToSpawn.GetComponentsInChildren<ParticleSystem>();
            for (int i = 0; i < psArray.Length; i++)
            {
                psArray[i].Clear();
                psArray[i].Play();
            }
        }

        return objectToSpawn;
    }

    /// <summary>
    /// Turns the object invisible and puts it back in line to be recycled later.
    /// </summary>
    public void ReturnToPool(GameObject objToReturn, GameObject originalPrefabKey)
    {
        if (objToReturn == null) return; // Prevent MissingReferenceException if object was destroyed

        objToReturn.SetActive(false);
        objToReturn.transform.SetParent(this.transform); // Keep the hierarchy clean

        if (!poolDictionary.ContainsKey(originalPrefabKey))
        {
            poolDictionary.Add(originalPrefabKey, new Queue<GameObject>());
            totalSpawnedCounts[originalPrefabKey] = 1;
        }

        poolDictionary[originalPrefabKey].Enqueue(objToReturn);
    }

    /// <summary>
    /// A helper method to safely "Destroy" effects after their lifetime expires.
    /// </summary>
    public void ReturnToPoolAfterDelay(GameObject objToReturn, GameObject originalPrefabKey, float delay)
    {
        if (objToReturn == null || originalPrefabKey == null) return;
        StartCoroutine(ReturnRoutine(objToReturn, originalPrefabKey, delay));
    }

    private IEnumerator ReturnRoutine(GameObject objToReturn, GameObject originalPrefabKey, float delay)
    {
        yield return new WaitForSeconds(delay);
        ReturnToPool(objToReturn, originalPrefabKey);
    }
}
