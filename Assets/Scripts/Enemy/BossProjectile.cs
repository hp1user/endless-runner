using System.Collections.Generic;
using UnityEngine;
using Player.Control;

namespace Enemy.Control
{
    /// <summary>
    /// Multi-lane projectile fired by the Boss that travels down the bridge.
    /// Deals damage if the player is in one of the targeted lanes when the projectile crosses them.
    /// </summary>
    public class BossProjectile : MonoBehaviour
    {
        private float[] targetedLanes;
        private float travelSpeed = 14f;
        private float damage = 25f;
        private Transform playerTarget;
        private bool hasHitPlayer = false;
        private List<GameObject> visualOrbs = new List<GameObject>();

        public static BossProjectile Spawn(float[] lanes, Vector3 startPos, float speed, float damageAmount, Transform target)
        {
            GameObject projObj = new GameObject("BossAcidBarrage");
            projObj.transform.position = startPos;

            BossProjectile projectile = projObj.AddComponent<BossProjectile>();
            projectile.Initialize(lanes, speed, damageAmount, target);
            return projectile;
        }

        public void Initialize(float[] lanes, float speed, float damageAmount, Transform target)
        {
            this.targetedLanes = lanes;
            this.travelSpeed = speed;
            this.damage = damageAmount;
            this.playerTarget = target;
            this.hasHitPlayer = false;

            // Material for glowing acid projectile
            Material acidMat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color"));
            acidMat.color = new Color(0.2f, 1f, 0.3f, 0.9f); // Neon acid green

            // Spawn visual projectile sphere for each targeted lane
            foreach (float laneX in lanes)
            {
                GameObject orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                orb.name = $"AcidOrb_Lane_{laneX}";
                orb.transform.SetParent(transform);
                orb.transform.localPosition = new Vector3(laneX - transform.position.x, 0.6f, 0f);
                orb.transform.localScale = new Vector3(1.3f, 1.3f, 1.3f);

                // Disable physics collider on the visual sphere (we do custom lane checking)
                Collider col = orb.GetComponent<Collider>();
                if (col != null) Destroy(col);

                Renderer rend = orb.GetComponent<Renderer>();
                if (rend != null) rend.material = acidMat;

                // Add trailing particle / trail renderer
                TrailRenderer trail = orb.AddComponent<TrailRenderer>();
                trail.material = acidMat;
                trail.startColor = new Color(0.4f, 1f, 0.2f, 0.8f);
                trail.endColor = new Color(0.1f, 0.8f, 0.1f, 0f);
                trail.startWidth = 0.9f;
                trail.endWidth = 0.1f;
                trail.time = 0.35f;

                visualOrbs.Add(orb);
            }
        }

        private void Update()
        {
            // Move along Z toward the player (negative Z direction relative to the bridge arena)
            transform.position += Vector3.back * travelSpeed * Time.deltaTime;

            if (playerTarget != null && !hasHitPlayer)
            {
                float playerZ = playerTarget.position.z;
                float currentZ = transform.position.z;

                // When projectile reaches player's Z plane
                if (Mathf.Abs(currentZ - playerZ) <= 1.0f)
                {
                    float playerX = playerTarget.position.x;
                    foreach (float laneX in targetedLanes)
                    {
                        if (Mathf.Abs(playerX - laneX) <= 1.0f)
                        {
                            hasHitPlayer = true;
                            PlayerController playerCtrl = playerTarget.GetComponent<PlayerController>();
                            if (playerCtrl != null)
                            {
                                playerCtrl.TakeDamage(damage);
                                Debug.Log($"<color=red>[Boss Projectile]</color> Hit player on lane {laneX:F1}! Dealt {damage} damage.");
                            }
                            break;
                        }
                    }
                }
            }

            // Despawn once well past the player
            float despawnZ = (playerTarget != null) ? playerTarget.position.z - 15f : -20f;
            if (transform.position.z < despawnZ)
            {
                Destroy(gameObject);
            }
        }
    }
}
