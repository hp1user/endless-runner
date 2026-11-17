using UnityEngine;

namespace Enemy.Control
{
    /// <summary>
    /// Renders a pulsing red danger telegraph zone on the bridge floor across the targeted lanes
    /// and a distinct glowing emerald corridor for the safe lane during the Boss's projectile charging windup.
    /// </summary>
    public class BossAttackTelegraph : MonoBehaviour
    {
        private LineRenderer[] dangerIndicators;
        private LineRenderer safeIndicator;
        private float duration;
        private float timer;

        private static Material sharedDangerMat;
        private static Material sharedSafeMat;

        public static BossAttackTelegraph Create(float[] targetedLanes, float safeLaneX, float startZ, float endZ, float duration)
        {
            GameObject go = new GameObject("BossAttackTelegraph");
            BossAttackTelegraph telegraph = go.AddComponent<BossAttackTelegraph>();
            telegraph.Initialize(targetedLanes, safeLaneX, startZ, endZ, duration);
            return telegraph;
        }

        public static BossAttackTelegraph Create(float[] targetedLanes, float startZ, float endZ, float duration)
        {
            return Create(targetedLanes, float.NaN, startZ, endZ, duration);
        }

        public void Initialize(float[] targetedLanes, float safeLaneX, float startZ, float endZ, float duration)
        {
            this.duration = duration;
            this.timer = 0f;

            // Reuse static shared materials to prevent native memory leaks during long boss battles
            if (sharedDangerMat == null)
            {
                Shader s = Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
                sharedDangerMat = new Material(s);
            }

            if (sharedSafeMat == null)
            {
                Shader s = Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
                sharedSafeMat = new Material(s);
            }

            dangerIndicators = new LineRenderer[targetedLanes.Length];
            for (int i = 0; i < targetedLanes.Length; i++)
            {
                GameObject lineObj = new GameObject($"Telegraph_DangerLane_{i}");
                lineObj.transform.SetParent(transform);

                LineRenderer lr = lineObj.AddComponent<LineRenderer>();
                lr.material = sharedDangerMat;
                lr.startColor = new Color(1f, 0.2f, 0.2f, 0.7f);
                lr.endColor = new Color(1f, 0.05f, 0.05f, 0.4f);
                lr.startWidth = 1.6f;
                lr.endWidth = 1.6f;
                lr.positionCount = 2;
                lr.useWorldSpace = true;

                float x = targetedLanes[i];
                float y = 0.08f; // slightly above bridge surface
                lr.SetPosition(0, new Vector3(x, y, startZ));
                lr.SetPosition(1, new Vector3(x, y, endZ));

                dangerIndicators[i] = lr;
            }

            // Safe lane corridor indicator
            if (!float.IsNaN(safeLaneX))
            {
                GameObject safeObj = new GameObject("Telegraph_SafeLane");
                safeObj.transform.SetParent(transform);

                safeIndicator = safeObj.AddComponent<LineRenderer>();
                safeIndicator.material = sharedSafeMat;
                safeIndicator.startColor = new Color(0.12f, 0.95f, 0.45f, 0.6f);
                safeIndicator.endColor = new Color(0.05f, 0.8f, 0.35f, 0.3f);
                safeIndicator.startWidth = 1.6f;
                safeIndicator.endWidth = 1.6f;
                safeIndicator.positionCount = 2;
                safeIndicator.useWorldSpace = true;

                float y = 0.07f;
                safeIndicator.SetPosition(0, new Vector3(safeLaneX, y, startZ));
                safeIndicator.SetPosition(1, new Vector3(safeLaneX, y, endZ));
            }
        }

        private void Update()
        {
            timer += Time.deltaTime;
            float dangerPulse = 0.4f + Mathf.PingPong(timer * 6f, 0.4f);
            float safePulse = 0.35f + Mathf.PingPong(timer * 4f, 0.35f);

            if (dangerIndicators != null)
            {
                foreach (var lr in dangerIndicators)
                {
                    if (lr != null)
                    {
                        lr.startColor = new Color(1f, 0.1f, 0.1f, dangerPulse);
                        lr.endColor = new Color(1f, 0.05f, 0.05f, dangerPulse * 0.7f);
                    }
                }
            }

            if (safeIndicator != null)
            {
                safeIndicator.startColor = new Color(0.12f, 0.95f, 0.45f, safePulse);
                safeIndicator.endColor = new Color(0.05f, 0.8f, 0.35f, safePulse * 0.6f);
            }

            if (timer >= duration)
            {
                Destroy(gameObject);
            }
        }
    }
}
