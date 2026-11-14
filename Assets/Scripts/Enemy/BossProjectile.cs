using UnityEngine;
using Player.Control;
using AcidSpitAdvanced;

namespace Enemy.Control
{
    /// <summary>Compatibility bridge for the existing two-lane boss attack. Original orb prefabs are preserved.</summary>
    public static class BossProjectile
    {
        private static GameObject advancedPrefab;

        public static AcidProjectileController Spawn(float[] lanes, Vector3 startPos, float speed, float damageAmount, Transform target, GameObject impactVFX = null)
        {
            if (!advancedPrefab)
                advancedPrefab = Resources.Load<GameObject>("AcidSpitAdvanced/AcidProjectile_Advanced");
            if (!advancedPrefab)
            {
                Debug.LogError("Advanced acid prefab missing. Run Tools/VFX/Build Advanced Acid Spit.");
                return null;
            }
            AcidPool.SurfaceVelocity = GetRoadVelocity;
            AcidProjectileController first = null;
            bool hitRegistered = false; // Prevent double damage if player is mid-lane change between targeted lanes
            foreach (float lane in lanes)
            {
                Vector3 origin = new Vector3(lane, startPos.y + 0.35f, startPos.z);
                float plane = target ? target.position.z + 0.8f : startPos.z - 30f;
                var projectile = AcidProjectileController.Launch(advancedPrefab, origin, Vector3.back, speed, 0, plane);
                if (!projectile) continue;
                if (!first) first = projectile;
                // Enemy colliders cannot intercept their own attack. Damage remains owned by this adapter.
                projectile.collisionMask = LayerMask.GetMask("Player");
                projectile.groundMask = LayerMask.GetMask("Default", "Ground", "Road");
                projectile.groundSkimming = false;
                projectile.Impact += (source, hit) =>
                {
                    if (hitRegistered) return;
                    if (!target || Mathf.Abs(target.position.x - lane) > 1.0f) return;
                    hitRegistered = true;
                    var player = target.GetComponent<PlayerController>();
                    if (player) player.TakeDamage(damageAmount);
                };
            }
            return first;
        }

        static Vector3 GetRoadVelocity()
        {
            if (!LevelManager.Instance) return Vector3.zero;
            var lm = LevelManager.Instance;
            float speed = lm.isTransitioningToBridge ? lm.transitionSpeed : lm.worldMoveSpeed;
            return (lm.chunkDirection == LevelManager.MoveDirection.Backward ? Vector3.back : Vector3.forward) * speed;
        }
    }
}
