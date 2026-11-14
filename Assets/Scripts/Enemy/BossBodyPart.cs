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
        [Tooltip("Percentage damage reduction (e.g. 40% for Bosses). Defaults to 40% if this is a boss part.")]
        [Range(0f, 100f)]
        public float damageReduction = 40f;

        private EnemyController bossController;

        private void Awake()
        {
            if (maxHealth <= 0f) maxHealth = (partType == BossBodyPartType.Head) ? 1000f : 350f;
            currentHealth = maxHealth;
            isBroken = false;
            if (bossController == null)
            {
                bossController = GetComponentInParent<EnemyController>();
            }
        }

        public void Initialize(EnemyController boss, float health, float multiplier, float reduction = 40f)
        {
            bossController = boss;
            maxHealth = health;
            currentHealth = health;
            damageMultiplier = multiplier;
            damageReduction = reduction;
            isBroken = false;
        }

        public void TakeDamage(float damage, Vector3 hitPoint = default, Vector3 hitNormal = default)
        {
            if (bossController == null)
            {
                bossController = GetComponentInParent<EnemyController>();
            }

            if (bossController != null && bossController.IsDead) return;

            // Trigger hit VFX on Boss at impact point
            if (bossController != null)
            {
                Vector3 pt = (hitPoint != default) ? hitPoint : transform.position;
                Vector3 n = (hitNormal != default) ? hitNormal : Vector3.up;
                bossController.SpawnHitVFX(pt, n);
                EndlessRunner.Audio.AudioManager.Instance?.PlayBossSound(EndlessRunner.Audio.BossSoundType.Hurt, pt);
            }

            // Failsafe: If currentHealth wasn't initialized yet
            if (currentHealth <= 0f && !isBroken)
            {
                currentHealth = maxHealth > 0f ? maxHealth : ((partType == BossBodyPartType.Head) ? 1000f : 350f);
            }

            float reduction = damageReduction;
            if (reduction <= 0f && bossController != null && bossController.DamageReduction > 0f)
            {
                reduction = bossController.DamageReduction;
            }
            float reductionFactor = 1f - Mathf.Clamp01(reduction / 100f);
            float effectiveDamage = damage * damageMultiplier * reductionFactor;

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
            string redInfo = (reduction > 0f) ? $" (-{reduction:F0}% reduction)" : "";
            string multInfo = (damageMultiplier != 1.0f) ? $" (Part Multiplier: {damageMultiplier:F2}x)" : "";
            Debug.Log($"<color=yellow>[BossPart]</color> {partType} took {effectiveDamage:F1} damage (Incoming: {damage:F1}{redInfo}{multInfo}). Remaining Part HP: {Mathf.Max(0f, currentHealth):F1}/{maxHealth:F1}");

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
