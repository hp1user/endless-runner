using UnityEngine;
using System.Collections;

[RequireComponent(typeof(LineRenderer))]
public class BulletTrail : MonoBehaviour
{
    [Header("Tracer Settings")]
    public float bulletSpeed = 150f;
    public float bulletLength = 2f;

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
        lineRenderer.SetPosition(1, startPoint);

        StartCoroutine(TravelRoutine(startPoint, endPoint));
    }

    private IEnumerator TravelRoutine(Vector3 startPoint, Vector3 endPoint)
    {
        float distance = Vector3.Distance(startPoint, endPoint);
        // Guarantee at least 0.05s of travel time so it doesn't vanish in 1 frame on 30fps mobile screens
        float travelTime = Mathf.Max(0.05f, distance / bulletSpeed);
        float timer = 0f;

        // Ensure trail is visible and alpha is 1
        lineRenderer.startColor = new Color(lineRenderer.startColor.r, lineRenderer.startColor.g, lineRenderer.startColor.b, 1f);
        lineRenderer.endColor = new Color(lineRenderer.endColor.r, lineRenderer.endColor.g, lineRenderer.endColor.b, 1f);

        while (timer < travelTime)
        {
            timer += Time.deltaTime;
            float t = timer / travelTime;

            Vector3 currentHead = Vector3.Lerp(startPoint, endPoint, t);
            
            float tailDist = Mathf.Max(0f, Vector3.Distance(startPoint, currentHead) - bulletLength);
            Vector3 currentTail = startPoint + (endPoint - startPoint).normalized * tailDist;

            lineRenderer.SetPosition(0, currentTail);
            lineRenderer.SetPosition(1, currentHead);

            yield return null;
        }

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
