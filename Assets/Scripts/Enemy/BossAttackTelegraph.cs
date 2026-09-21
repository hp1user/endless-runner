using UnityEngine;

namespace Enemy.Control
{
    /// <summary>
    /// Renders a pulsing red danger telegraph zone on the bridge floor across the targeted lanes
    /// during the Boss's projectile charging windup.
    /// </summary>
    public class BossAttackTelegraph : MonoBehaviour
    {
        private LineRenderer[] laneIndicators;
        private float duration;
        private float timer;

        public static BossAttackTelegraph Create(float[] targetedLanes, float startZ, float endZ, float duration)
        {
            GameObject go = new GameObject("BossAttackTelegraph");
            BossAttackTelegraph telegraph = go.AddComponent<BossAttackTelegraph>();
            telegraph.Initialize(targetedLanes, startZ, endZ, duration);
            return telegraph;
        }

        public void Initialize(float[] targetedLanes, float startZ, float endZ, float duration)
        {
            this.duration = duration;
            this.timer = 0f;

            laneIndicators = new LineRenderer[targetedLanes.Length];

            // Create default unlit / sprite material with red tint
            Material redMat = new Material(Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color"));
            redMat.color = new Color(1f, 0.15f, 0.15f, 0.6f);

            for (int i = 0; i < targetedLanes.Length; i++)
            {
                GameObject lineObj = new GameObject($"Telegraph_Lane_{i}");
                lineObj.transform.SetParent(transform);

                LineRenderer lr = lineObj.AddComponent<LineRenderer>();
                lr.material = redMat;
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

                laneIndicators[i] = lr;
            }
        }

        private void Update()
        {
            timer += Time.deltaTime;
            float pulse = 0.4f + Mathf.PingPong(timer * 6f, 0.4f);

            if (laneIndicators != null)
            {
                foreach (var lr in laneIndicators)
                {
                    if (lr != null)
                    {
                        lr.startColor = new Color(1f, 0.1f, 0.1f, pulse);
                        lr.endColor = new Color(1f, 0.05f, 0.05f, pulse * 0.7f);
                    }
                }
            }

            if (timer >= duration)
            {
                Destroy(gameObject);
            }
        }
    }
}
