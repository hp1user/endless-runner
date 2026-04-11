using UnityEngine;

namespace Enemy.Control
{
    public class EnemyController : MonoBehaviour
    {
        [Header("Runtime Stats")]
        [SerializeField] private string enemyName;
        [SerializeField] private float currentHealth;
        [SerializeField] private float moveSpeed;

        [Header("Optimization")]
        [Tooltip("How often (in seconds) the enemy recalculates the path to the player. Higher = Better Performance.")]
        public float rethinkInterval = 0.2f;

        private Transform playerTransform;
        private Vector3 targetDirection;
        private float nextRethinkTime;
        private bool isDead = false;

        public void Initialize(EnemyEntry data, Transform target)
        {
            enemyName = data.enemyName;
            currentHealth = data.maxHealth;
            moveSpeed = data.moveSpeed;
            playerTransform = target;
        }

        private void Update()
        {
            if (isDead || playerTransform == null) return;

            // 1. PERFORMANCE: Only recalculate direction on an interval
            if (Time.time >= nextRethinkTime)
            {
                nextRethinkTime = Time.time + rethinkInterval;
                targetDirection = (playerTransform.position - transform.position).normalized;
                targetDirection.y = 0; // Keep grounded
            }

            // 2. MOVEMENT: Apply the cached direction every frame for smooth motion
            if (targetDirection != Vector3.zero)
            {
                transform.Translate(targetDirection * moveSpeed * Time.deltaTime, Space.World);
                
                // Rotation is also smoothed (optional: could also put this on the interval)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(targetDirection), Time.deltaTime * 5f);
            }
        }

        public void TakeDamage(float damage)
        {
            if (isDead) return;

            currentHealth -= damage;
            Debug.Log($"<color=red>[Enemy]</color> {enemyName} took {damage} damage! Remaining HP: {currentHealth}");

            if (currentHealth <= 0)
            {
                Die();
            }
        }

        private void Die()
        {
            isDead = true;
            Debug.Log($"<color=black><b>[Enemy]</b></color> {enemyName} has been defeated!");
            
            // Future: Play death animation/particles here
            
            Destroy(gameObject, 0.1f);
        }
    }
}
