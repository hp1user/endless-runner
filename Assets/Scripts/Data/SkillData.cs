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

        [Header("Visuals (Optional)")]
        public GameObject visualEffectPrefab;
    }
}
