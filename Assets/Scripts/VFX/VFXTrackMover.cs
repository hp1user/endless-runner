using UnityEngine;

namespace VFX
{
    /// <summary>
    /// Synchronizes the movement of world-space visual effects (e.g., ground death splashes, blood puddles)
    /// with the scrolling environment chunks in LevelManager.
    /// Ensures that ground splatters stay anchored to the moving road and travel backward out of view.
    /// </summary>
    public class VFXTrackMover : MonoBehaviour
    {
        [Tooltip("Fallback movement speed if LevelManager is not found in the scene.")]
        public float fallbackSpeed = 15f;

        private void Update()
        {
            float dt = Mathf.Min(Time.deltaTime, 0.1f);
            float speed = fallbackSpeed;
            Vector3 dir = Vector3.back;

            if (LevelManager.Instance != null)
            {
                speed = LevelManager.Instance.isTransitioningToBridge 
                    ? LevelManager.Instance.transitionSpeed 
                    : LevelManager.Instance.worldMoveSpeed;

                dir = (LevelManager.Instance.chunkDirection == LevelManager.MoveDirection.Backward) 
                    ? Vector3.back 
                    : Vector3.forward;
            }

            transform.position += dir * speed * dt;
        }
    }
}

