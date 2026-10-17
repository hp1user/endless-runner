using UnityEngine;
using Player.Control;

namespace EndlessRunner.Data
{
    [CreateAssetMenu(fileName = "NewCharacter", menuName = "Player/Character Data", order = 10)]
    public class CharacterData : ScriptableObject
    {
        [Header("Character Identity")]
        public string characterID = "ch15";
        public string characterName = "Ch15 Cyber Agent";
        public string characterTitle = "Tactical Operative";
        [TextArea(2, 4)]
        public string description = "A high-agility cybernetic operative outfitted for rapid tactical deployment and precision firearm combat.";
        public Sprite characterIcon;

        [Header("3D Model & Animation")]
        [Tooltip("The 3D model or configured prefab for this character (FBX or Prefab).")]
        public GameObject modelPrefab;
        [Tooltip("Optional custom AnimatorController. If left empty, defaults to 'Man Controller'.")]
        public RuntimeAnimatorController customAnimatorController;

        [Header("Combat & Health Stats")]
        [Range(50f, 1000f)]
        public float baseHealth = 120f;
        [Range(0f, 200f)]
        public float baseArmor = 25f;

        [Header("Locomotion Stats")]
        [Range(5f, 30f)]
        public float movementSpeed = 10f;
        [Range(0.5f, 3.0f)]
        public float moveSpeedMultiplier = 1.0f;
        [Range(1f, 20f)]
        public float strafeAnimationSmoothing = 8f;
        [Range(1f, 4f)]
        public float laneDistance = 2.0f;

        [Header("Active Abilities")]
        [Tooltip("Primary tactical skill (e.g. Rapid Fire).")]
        public SkillData activeSkill;
        [Tooltip("Ultimate charge ability (e.g. Orbital AOE Bombardment).")]
        public SkillData activeUltimate;

        [Header("Starter Equipment")]
        [Tooltip("Starting weapon ID to equip at the start of a run (default: 'D Eagle').")]
        public string starterWeaponID = "D Eagle";

        /// <summary>
        /// Retrieves the Humanoid Avatar associated with this character's model.
        /// </summary>
        public Avatar GetAvatar()
        {
            if (modelPrefab == null) return null;

            // Check if modelPrefab already has an Animator with Avatar
            Animator anim = modelPrefab.GetComponent<Animator>();
            if (anim != null && anim.avatar != null && anim.avatar.isValid)
            {
                return anim.avatar;
            }

#if UNITY_EDITOR
            // Search all sub-assets inside the model/prefab file
            string path = UnityEditor.AssetDatabase.GetAssetPath(modelPrefab);
            if (!string.IsNullOrEmpty(path))
            {
                var allAssets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path);
                foreach (var asset in allAssets)
                {
                    if (asset is Avatar avatar && avatar.isValid)
                    {
                        return avatar;
                    }
                }
            }
#endif
            return null;
        }
    }
}
