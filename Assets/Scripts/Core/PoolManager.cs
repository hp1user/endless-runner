using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoolManager : MonoBehaviour
{
    public static PoolManager Instance { get; private set; }

    // This dictionary holds multiple pools. 
    // Key = The original Prefab, Value = A Queue of recycled, invisible clones.
    private Dictionary<GameObject, Queue<GameObject>> poolDictionary = new Dictionary<GameObject, Queue<GameObject>>();

    private void Awake()
    {
        // Standard Singleton setup so the PlayerController can easily talk to it
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
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
        }

        GameObject objectToSpawn = null;

        // 2. Check if we have an invisible, recycled object waiting in the queue
        if (poolDictionary[prefab].Count > 0)
        {
            objectToSpawn = poolDictionary[prefab].Dequeue();
            objectToSpawn.transform.position = position;
            objectToSpawn.transform.rotation = rotation;
            objectToSpawn.transform.SetParent(parent);
            objectToSpawn.SetActive(true);
        }
        else
        {
            // 3. The pool is empty (or brand new), so we must Instantiate a new one
            objectToSpawn = Instantiate(prefab, position, rotation, parent);
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
        poolDictionary[originalPrefabKey].Enqueue(objToReturn);
    }

    /// <summary>
    /// A helper method to safely "Destroy" effects after their lifetime expires.
    /// </summary>
    public void ReturnToPoolAfterDelay(GameObject objToReturn, GameObject originalPrefabKey, float delay)
    {
        StartCoroutine(ReturnRoutine(objToReturn, originalPrefabKey, delay));
    }

    private IEnumerator ReturnRoutine(GameObject objToReturn, GameObject originalPrefabKey, float delay)
    {
        yield return new WaitForSeconds(delay);
        ReturnToPool(objToReturn, originalPrefabKey);
    }
}