using UnityEngine;

namespace Player.Control
{
    [CreateAssetMenu(fileName = "PlayerDatabase", menuName = "Player/Player Database")]
    public class PlayerDatabase : ScriptableObject
    {
        [Header("Health & Defense")]
        public float baseHealth = 100f;
        public float baseArmor = 0f;

        [Header("Movement (Lane Changing)")]
        [Tooltip("How far apart the lanes are on the X-axis.")]
        public float laneDistance = 2.0f;
        
        [Tooltip("How fast the player physically moves between lanes.")]
        public float movementSpeed = 10f;
        
        [Tooltip("Smoothing for the lane-change animation.")]
        public float strafeAnimationSmoothing = 8f;

        [Header("Roguelike Multipliers")]
        [Tooltip("multiplier for lane change movement speed.")]
        public float moveSpeedMultiplier = 1.0f;

        // Hardcoded Layer Settings
        public LayerMask PlayerLayer => LayerMask.GetMask("Player");
        public LayerMask EnemyLayer => LayerMask.GetMask("Enemy");
    }
}
