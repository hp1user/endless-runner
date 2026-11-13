using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;
using Player.Control;

namespace Enemy.Control
{
    /// <summary>
    /// Multi-lane acid spit projectile fired by the Boss that travels down the bridge.
    /// Features multi-harmonic organic liquid wobbling, ToonLiquids dripping droplets,
    /// and a flat ground liquid effect laying continuous acid puddles on the road.
    /// </summary>
    public class BossProjectile : MonoBehaviour
    {
        private static GameObject cachedOrbPrefab = null;

        private float[] targetedLanes;
        private float travelSpeed = 14f;
        private float damage = 25f;
        private Transform playerTarget;
        private bool hasHitPlayer = false;
        private bool hasDetonated = false;
        private bool isFadingOut = false;
        private GameObject impactPrefab;
        private List<GameObject> visualOrbs = new List<GameObject>();
        private List<Renderer> cachedRenderers = new List<Renderer>();
        private List<VisualEffect> cachedVFX = new List<VisualEffect>();

        public static BossProjectile Spawn(float[] lanes, Vector3 startPos, float speed, float damageAmount, Transform target, GameObject impactVFX = null)
        {
            GameObject projObj = new GameObject("BossAcidBarrage");
            projObj.transform.position = startPos;

            BossProjectile projectile = projObj.AddComponent<BossProjectile>();
            projectile.Initialize(lanes, speed, damageAmount, target, impactVFX);
            return projectile;
        }

        public void Initialize(float[] lanes, float speed, float damageAmount, Transform target, GameObject impactVFX = null)
        {
            this.targetedLanes = lanes;
            this.travelSpeed = speed;
            this.damage = damageAmount;
            this.playerTarget = target;
            this.impactPrefab = impactVFX;
            this.hasHitPlayer = false;
            this.hasDetonated = false;
            this.isFadingOut = false;

            // 1. One-time static cached prefab load (0 GC after first load, fully compatible with Android APK)
            if (cachedOrbPrefab == null)
            {
                cachedOrbPrefab = Resources.Load<GameObject>("Boss_AcidOrb");
#if UNITY_EDITOR
                if (cachedOrbPrefab == null)
                {
                    cachedOrbPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/VFX/Boss_AcidOrb.prefab");
                }
#endif
            }

            // 2. Spawn Acid Orb for each targeted lane
            foreach (float laneX in lanes)
            {
                if (cachedOrbPrefab != null)
                {
                    GameObject orb = Instantiate(cachedOrbPrefab, transform);
                    orb.name = $"AcidOrb_Lane_{laneX}";
                    orb.transform.localPosition = new Vector3(laneX - transform.position.x, 0.6f, 0f);

                    // Pre-cache components at spawn time to avoid runtime GC allocations
                    Renderer[] rends = orb.GetComponentsInChildren<Renderer>();
                    cachedRenderers.AddRange(rends);

                    VisualEffect[] vfxs = orb.GetComponentsInChildren<VisualEffect>();
                    cachedVFX.AddRange(vfxs);

                    visualOrbs.Add(orb);
                    continue;
                }

                // Procedural Fallback ONLY if prefab is missing (Emergency safety net)
                GameObject procOrb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                procOrb.name = $"AcidOrb_Lane_{laneX}";
                procOrb.transform.SetParent(transform);
                procOrb.transform.localPosition = new Vector3(laneX - transform.position.x, 0.6f, 0f);
                procOrb.transform.localScale = new Vector3(1.35f, 1.35f, 1.45f);

                Collider col = procOrb.GetComponent<Collider>();
                if (col != null) Destroy(col);

                Renderer rend = procOrb.GetComponent<Renderer>();
                if (rend != null) cachedRenderers.Add(rend);

                visualOrbs.Add(procOrb);
            }
        }

        private void Update()
        {
            if (isFadingOut) return;

            // Move along Z toward the player (negative Z direction relative to the bridge arena)
            transform.position += Vector3.back * travelSpeed * Time.deltaTime;

            // Procedural fallback wobble & rotation (prefab orbs handle this internally on Visual_OrbMesh via BossAcidOrbVisual)
            float wobble = Mathf.Sin(Time.time * 13.5f) * 0.15f;
            foreach (GameObject orb in visualOrbs)
            {
                if (orb != null && orb.GetComponentInChildren<VFX.BossAcidOrbVisual>() == null)
                {
                    Transform visualChild = orb.transform.Find("Visual_OrbMesh") ?? orb.transform;
                    visualChild.localScale = new Vector3(1.35f + wobble, 1.35f - wobble, 1.45f + wobble * 0.5f);
                    visualChild.Rotate(new Vector3(35f, 55f, 180f) * Time.deltaTime, Space.Self);
                }
            }

            // Hit & Detonation detection when reaching player's plane
            if (playerTarget != null)
            {
                float playerZ = playerTarget.position.z;
                float currentZ = transform.position.z;

                // Trigger splash detonation when crossing player plane
                if (!hasDetonated && (currentZ <= playerZ + 0.8f))
                {
                    hasDetonated = true;
                    DetonateImpactSplashes(currentZ);
                }

                if (!hasHitPlayer && Mathf.Abs(currentZ - playerZ) <= 1.0f)
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

            // When well past the player, trigger graceful trail fadeout rather than instant destroy
            float despawnZ = (playerTarget != null) ? playerTarget.position.z - 18f : -25f;
            if (transform.position.z < despawnZ)
            {
                StartCoroutine(FadeAndCleanupRoutine());
            }
        }

        private System.Collections.IEnumerator FadeAndCleanupRoutine()
        {
            isFadingOut = true;

            // 1. Hide orb sphere meshes immediately (using cached list, 0 GC allocation)
            for (int i = 0; i < cachedRenderers.Count; i++)
            {
                if (cachedRenderers[i] != null) cachedRenderers[i].enabled = false;
            }

            // 2. Stop emitting new particles from VFX Graph instances (using cached list, 0 GC allocation)
            for (int i = 0; i < cachedVFX.Count; i++)
            {
                if (cachedVFX[i] != null) cachedVFX[i].Stop();
            }

            // 3. Wait for existing ground liquid puddles (3.5s) to naturally dissolve before destroying container
            yield return new WaitForSeconds(3.5f);

            Destroy(gameObject);
        }

        private void DetonateImpactSplashes(float impactZ)
        {
            if (impactPrefab == null || PoolManager.Instance == null) return;

            foreach (float laneX in targetedLanes)
            {
                Vector3 splashPos = new Vector3(laneX, 0.45f, impactZ);
                GameObject splash = PoolManager.Instance.SpawnFromPool(impactPrefab, splashPos, Quaternion.identity);
                if (splash != null)
                {
                    var mover = splash.GetComponent<VFX.VFXTrackMover>();
                    if (mover == null) mover = splash.AddComponent<VFX.VFXTrackMover>();
                    mover.enabled = true;
                }
                PoolManager.Instance.ReturnToPoolAfterDelay(splash, impactPrefab, 2.5f);
            }
        }
    }
}
