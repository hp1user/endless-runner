using UnityEngine;

namespace Player.Control
{
    public enum SkillType
    {
        RapidFire,
        AoEKill,
        Heal,
        ArmorBuff
    }

    [CreateAssetMenu(fileName = "NewSkillData", menuName = "Player/Skill Data")]
    public class SkillData : ScriptableObject
    {
        [Header("Basic Info")]
        public string skillName = "New Skill";
        [TextArea]
        public string description = "Skill description goes here.";
        public Sprite icon;
        public SkillType skillType;

        [Header("Recharge Settings")]
        [Tooltip("If true, recharges based on enemies killed. If false, recharges based on time (seconds).")]
        public bool isKillBasedRecharge = false;
        
        [Tooltip("Cooldown time in seconds (if time-based) OR number of kills required (if kill-based).")]
        public float rechargeRequirement = 10f;

        [Header("Effect Properties")]
        [Tooltip("Duration of the skill (e.g. for Rapid Fire buff).")]
        public float effectDuration = 5f;
        
        [Tooltip("Multiplier for Rapid Fire or general value.")]
        public float effectValue = 2f; 

        [Tooltip("Radius for AoE skills.")]
        public float effectRadius = 10f;

        [Header("Damage Settings (For AoE / Attack Skills)")]
        [Tooltip("Damage dealt to regular enemies (default 500 kills regular enemies).")]
        public float damage = 500f;

        [Tooltip("Damage dealt specifically to Bosses by this skill/ultimate (default 250 = 25% of 1000 HP).")]
        public float bossDamage = 250f;

        [Header("Visuals (Optional)")]
        public GameObject visualEffectPrefab;
    }
}
