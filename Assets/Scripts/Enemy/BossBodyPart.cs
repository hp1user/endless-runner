using UnityEngine;

namespace Enemy.Control
{
    public enum BossBodyPartType
    {
        Head,
        LeftLeg,
        RightLeg
    }

    /// <summary>
    /// Represents a specific hit zone on the Boss (Head, Left Leg, Right Leg).
    /// Handles localized health, breaking state, and reports hits to EnemyController.
    /// </summary>
    public class BossBodyPart : MonoBehaviour
    {
        [Header("Part Settings")]
        public BossBodyPartType partType = BossBodyPartType.Head;
        public float maxHealth = 30f;
        public float currentHealth;
        public bool isBroken = false;
        public float damageMultiplier = 1.0f;

        private EnemyController bossController;

        private void Awake()
        {
            if (maxHealth <= 0f) maxHealth = (partType == BossBodyPartType.Head) ? 600f : 200f;
            currentHealth = maxHealth;
            isBroken = false;
            if (bossController == null)
            {
                bossController = GetComponentInParent<EnemyController>();
            }
        }

        public void Initialize(EnemyController boss, float health, float multiplier)
        {
            bossController = boss;
            maxHealth = health;
            currentHealth = health;
            damageMultiplier = multiplier;
            isBroken = false;
        }

        public void TakeDamage(float damage)
        {
            if (bossController == null)
            {
                bossController = GetComponentInParent<EnemyController>();
            }

            if (bossController != null && bossController.IsDead) return;

            // Failsafe: If currentHealth wasn't initialized yet
            if (currentHealth <= 0f && !isBroken)
            {
                currentHealth = maxHealth > 0f ? maxHealth : ((partType == BossBodyPartType.Head) ? 600f : 200f);
            }

            float effectiveDamage = damage * damageMultiplier;

            if (isBroken)
            {
                Debug.Log($"<color=yellow>[BossPart]</color> {partType} (Broken) took {effectiveDamage:F1} DMG!");
                if (bossController != null)
                {
                    bossController.OnPartDamaged(partType, effectiveDamage, true);
                }
                return;
            }

            currentHealth -= effectiveDamage;
            Debug.Log($"<color=yellow>[BossPart]</color> {partType} took {effectiveDamage:F1} damage. Remaining Part HP: {Mathf.Max(0f, currentHealth):F1}/{maxHealth:F1}");

            if (currentHealth <= 0f)
            {
                currentHealth = 0f;
                isBroken = true;
                Debug.Log($"<color=red>[BossPart]</color> <b>{partType} is now BROKEN! (Depleted all {maxHealth:F1} HP)</b>");
            }

            if (bossController != null)
            {
                bossController.OnPartDamaged(partType, effectiveDamage, isBroken);
            }
        }
    }
}
