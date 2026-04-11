using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class EnemyEntry
{
    public string enemyName = "Basic Enemy";
    
    [Header("Visuals")]
    [Tooltip("The model/prefab for this enemy type.")]
    public Transform prefab;
    
    [Header("Stats")]
    public float maxHealth = 30f;
    public float moveSpeed = 5f;
}

[CreateAssetMenu(fileName = "EnemyDatabase", menuName = "Enemy/Enemy Database")]
public class EnemyDatabase : ScriptableObject
{
    public List<EnemyEntry> enemyTypes = new List<EnemyEntry>();

    public EnemyEntry GetRandomEnemy()
    {
        if (enemyTypes == null || enemyTypes.Count == 0) return null;
        return enemyTypes[Random.Range(0, enemyTypes.Count)];
    }
}
