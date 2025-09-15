using DG.Tweening;
using UnityEngine;

namespace Gameplay.Platform {
    /// <summary>
    /// A platform that falls after the player steps on it.
    /// </summary>
    public class FallingPlatform : PlatformBase {
        [Header("Falling Settings")]
        [Tooltip("The delay in seconds before the platform falls after contact.")]
        [SerializeField] private float fallDelay = 1f;
        [Tooltip("If true, the platform will shake before it falls.")]
        [SerializeField] private bool shakeBeforeFalling = true;
        [Tooltip("The duration of the shake animation.")]
        [SerializeField] private float shakeDuration = 0.5f;
        [Tooltip("The intensity of the positional shake.")]
        [SerializeField] private float shakeIntensity = 0.05f;

        /// <summary>
        /// Overrides the base activation method to implement the falling logic.
        /// </summary>
        protected override void ActivatePlatform() {
            if(enableDebugLogs) Debug.Log($"[FallingPlatform.ActivatePlatform] Activating fall sequence.", this);
            
            isDeactivated = true; // Mark as used to prevent re-triggering
            if(enableDebugLogs) Debug.Log($"[FallingPlatform.ActivatePlatform] 'isDeactivated' set to true.", this);

            if (shakeBeforeFalling) {
                if(enableDebugLogs) Debug.Log($"[FallingPlatform.ActivatePlatform] Shaking for {shakeDuration} seconds before falling.", this);
                transform.DOShakePosition(shakeDuration, shakeIntensity)
                    .OnComplete(StartFall); // Call StartFall after shaking is complete
            } else {
                if(enableDebugLogs) Debug.Log($"[FallingPlatform.ActivatePlatform] Waiting for {fallDelay} seconds before falling (no shake).", this);
                Invoke(nameof(StartFall), fallDelay);
            }
        }

        /// <summary>
        /// Contains the logic to make the platform fall.
        /// </summary>
        private void StartFall() {
            if (enableDebugLogs)
                Debug.Log($"[FallingPlatform.StartFall] Starting the fall.", this);
            Rigidbody rb = GetComponent<Rigidbody>();
            // Add a Rigidbody to make it affected by gravity.
            if (rb != null) {
                rb.isKinematic = false;
                rb.useGravity = true;
            } else {
                gameObject.AddComponent<Rigidbody>();
            }
            platformCollider.enabled = false;

            if (respawns) {
                if(enableDebugLogs) Debug.Log($"[FallingPlatform.StartFall] Scheduling respawn in {respawnTime} seconds.", this);
                Invoke(nameof(PrepareForRespawn), respawnTime);
            } else {
                if(enableDebugLogs) Debug.Log($"[FallingPlatform.StartFall] Platform does not respawn. Destroying.", this);
                Destroy(gameObject, 5f); // Destroy after 5 seconds to let it fall out of view
            }
        }
        
        /// <summary>
        /// Called by Unity's physics engine when this collider/rigidbody has begun touching another rigidbody/collider.
        /// </summary>
        /// <param name="collision">Detailed information about the collision contact.</param>
        private void OnCollisionEnter(Collision collision) {
            if (!isDeactivated || collision.gameObject.CompareTag("Player")) {
                return;
            }
            if (enableDebugLogs) 
                Debug.Log($"[FallingPlatform.OnCollisionEnter] Platform collided with '{collision.gameObject.name}'. Initiating early respawn.", this);

            if (platformVisuals != null) {
                platformVisuals.enabled = false;
            }
            platformCollider.enabled = false;
        }
    }
}