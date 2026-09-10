using UnityEngine;
using Player.Control;

namespace CameraControl
{
    /// <summary>
    /// Manages switching between different Cinemachine Virtual Cameras during Boss phases.
    /// To use this, create two Virtual Cameras in your scene (one facing the player's front, one facing the back).
    /// Assign them here, and the Cinemachine Brain will automatically handle the smooth blending between them!
    /// </summary>
    public class BossCameraController : MonoBehaviour
    {
        [Header("Cinemachine Cameras")]
        [Tooltip("The default camera looking at the front of the player.")]
        public GameObject standardFrontCamera;

        [Tooltip("The camera used during boss battles, looking from behind the player.")]
        public GameObject bossBackCamera;

        private void OnEnable()
        {
            GameManager.OnBossFightStarted += SwitchToBossCamera;
            GameManager.OnBossDefeated += SwitchToStandardCamera;
            PlayerController.OnPlayerDeath += HandlePlayerDeath;
        }

        private void OnDisable()
        {
            GameManager.OnBossFightStarted -= SwitchToBossCamera;
            GameManager.OnBossDefeated -= SwitchToStandardCamera;
            PlayerController.OnPlayerDeath -= HandlePlayerDeath;
        }

        private void Start()
        {
            // Ensure we start with the standard camera active
            SwitchToStandardCamera();
        }

        private void SwitchToBossCamera()
        {
            if (standardFrontCamera != null) standardFrontCamera.SetActive(false);
            if (bossBackCamera != null) bossBackCamera.SetActive(true);
            
            Debug.Log("<color=yellow>[BossCamera]</color> Switching to Boss Back Camera!");
        }

        private void SwitchToStandardCamera()
        {
            if (standardFrontCamera != null) standardFrontCamera.SetActive(true);
            if (bossBackCamera != null) bossBackCamera.SetActive(false);
            
            Debug.Log("<color=yellow>[BossCamera]</color> Switching to Standard Front Camera!");
        }

        private void HandlePlayerDeath()
        {
            // Optional: You could switch to a specific death camera here if you had one.
            // For now, we will just leave the active camera as is.
        }
    }
}
