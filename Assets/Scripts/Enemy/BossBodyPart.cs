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

        public void TakeDamage(float damage, Vector3 hitPoint = default, Vector3 hitNormal = default, bool isCrit = false)
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

            bool bothLegsBroken = bossController != null && bossController.AreBothLegsBroken;

            // 1. Broken leg check (Non-head parts)
            if (isBroken && partType != BossBodyPartType.Head)
            {
                // User requirement: "Broken parts take LESS damage 90% percentage reduction for now"
                float brokenFactor = 0.10f; // 90% reduction
                float brokenDamage = damage * brokenFactor;

                Debug.Log($"<color=yellow>[BossPart]</color> {partType} (Broken) took {brokenDamage:F1} DMG (-90% reduction)! {(isCrit ? "<color=#C084FC>[CRIT]</color>" : "")}");
                if (bossController != null)
                {
                    bossController.OnPartDamaged(partType, brokenDamage, true);
                }
                Vector3 numPos = (hitPoint != default) ? hitPoint : transform.position;
                UI.DamageNumbers.DamageNumberManager.Instance?.SpawnBossDamage(brokenDamage, numPos, partType, true, isCrit, false);
                return;
            }

            // 2. Head logic
            if (partType == BossBodyPartType.Head)
            {
                if (!bothLegsBroken)
                {
                    // NOT in Phase 3 yet!
                    // User requirement: "boss will take very less damage on head like 80 to 90 % reduced damage... lets go with ARMOR tag when we are doing damage to head and not in phase 3"
                    float armoredFactor = 0.10f; // 90% reduction
                    float armoredDamage = damage * armoredFactor;

                    currentHealth -= armoredDamage;
                    if (currentHealth < 100f) currentHealth = 100f; // Head cannot die/break before legs!

                    Debug.Log($"<color=cyan>[BossPart]</color> HEAD [ARMOR] took {armoredDamage:F1} DMG (-90% reduction, legs intact)! {(isCrit ? "<color=#C084FC>[CRIT]</color>" : "")}");
                    if (bossController != null)
                    {
                        bossController.OnPartDamaged(partType, armoredDamage, false);
                    }
                    Vector3 numPos = (hitPoint != default) ? hitPoint : transform.position;
                    UI.DamageNumbers.DamageNumberManager.Instance?.SpawnBossDamage(armoredDamage, numPos, partType, false, isCrit, true);
                    return;
                }
                else
                {
                    // IN PHASE 3! Both legs are broken!
                    // User requirement: "go with option B head will take more damage in build damage multiplier of 1.25x as if legs were taking 10 damage now head will take 12.5 damage (with 40% reduction)"
                    float reduction = damageReduction > 0f ? damageReduction : (bossController != null && bossController.DamageReduction > 0f ? bossController.DamageReduction : 40f);
                    float reductionFactor = 1f - Mathf.Clamp01(reduction / 100f); // 0.60f
                    float weakpointMultiplier = 1.25f; // User specified 1.25x
                    float effectiveDamage = damage * weakpointMultiplier * reductionFactor;

                    currentHealth -= effectiveDamage;
                    string critInfo = isCrit ? " <color=#C084FC>[CRIT]</color>" : "";
                    Debug.Log($"<color=red>[BossPart]</color> HEAD [WEAKPOINT 1.25x] took {effectiveDamage:F1} damage! Remaining HP: {Mathf.Max(0f, currentHealth):F1}/{maxHealth:F1}{critInfo}");

                    if (currentHealth <= 0f)
                    {
                        currentHealth = 0f;
                        isBroken = true;
                        Debug.Log("<color=red><b>[BossPart] HEAD DESTROYED!</b></color>");
                    }

                    if (bossController != null)
                    {
                        bossController.OnPartDamaged(partType, effectiveDamage, isBroken);
                    }

                    Vector3 numPos = (hitPoint != default) ? hitPoint : transform.position;
                    UI.DamageNumbers.DamageNumberManager.Instance?.SpawnBossDamage(effectiveDamage, numPos, partType, false, isCrit, false);
                    return;
                }
            }

            // 3. Intact Leg logic (LeftLeg or RightLeg before breaking)
            float baseReduction = damageReduction > 0f ? damageReduction : (bossController != null && bossController.DamageReduction > 0f ? bossController.DamageReduction : 40f);
            float baseFactor = 1f - Mathf.Clamp01(baseReduction / 100f);
            float legDamage = damage * damageMultiplier * baseFactor;

            currentHealth -= legDamage;
            string legCritInfo = isCrit ? " <color=#C084FC>[CRIT]</color>" : "";
            Debug.Log($"<color=yellow>[BossPart]</color> {partType} took {legDamage:F1} damage (Remaining Leg HP: {Mathf.Max(0f, currentHealth):F1}/{maxHealth:F1}){legCritInfo}");

            if (currentHealth <= 0f)
            {
                currentHealth = 0f;
                isBroken = true;
                Debug.Log($"<color=red>[BossPart]</color> <b>{partType} is now BROKEN! (Depleted all {maxHealth:F1} HP)</b>");
            }

            if (bossController != null)
            {
                bossController.OnPartDamaged(partType, legDamage, isBroken);
            }

            Vector3 damagePos = (hitPoint != default) ? hitPoint : transform.position;
            UI.DamageNumbers.DamageNumberManager.Instance?.SpawnBossDamage(legDamage, damagePos, partType, isBroken, isCrit, false);
        }
    }
}
