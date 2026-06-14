using UnityEngine;
using System.Collections;

[RequireComponent(typeof(LineRenderer))]
public class BulletTrail : MonoBehaviour
{
    private LineRenderer lineRenderer;
    private GameObject originalPrefab;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
    }

    public void Initialize(Vector3 startPoint, Vector3 endPoint, float duration, GameObject prefabKey)
    {
        gameObject.SetActive(true); // Ensure it's active!

        if (lineRenderer == null)
        {
            lineRenderer = GetComponent<LineRenderer>();
        }

        originalPrefab = prefabKey;

        // Ensure LineRenderer has exactly 2 points
        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, startPoint);
        lineRenderer.SetPosition(1, endPoint);

        StartCoroutine(FadeOutRoutine(duration));
    }

    private IEnumerator FadeOutRoutine(float duration)
    {
        float timer = 0f;

        // Capture initial colors
        Color startColor = lineRenderer.startColor;
        Color endColor = lineRenderer.endColor;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float normalizedTime = timer / duration;

            // Fade alpha from 1 to 0 over duration
            float alpha = Mathf.Lerp(1f, 0f, normalizedTime);

            lineRenderer.startColor = new Color(startColor.r, startColor.g, startColor.b, alpha);
            lineRenderer.endColor = new Color(endColor.r, endColor.g, endColor.b, alpha);

            yield return null;
        }

        // Restore alpha for the next time it spawns from the pool
        lineRenderer.startColor = new Color(startColor.r, startColor.g, startColor.b, 1f);
        lineRenderer.endColor = new Color(endColor.r, endColor.g, endColor.b, 1f);

        Recycle();
    }

    private void Recycle()
    {
        if (originalPrefab != null && PoolManager.Instance != null)
        {
            PoolManager.Instance.ReturnToPool(gameObject, originalPrefab);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
