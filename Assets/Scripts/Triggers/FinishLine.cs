using Managers;
using UnityEngine;
namespace Triggers {
    /// <summary>
    /// A trigger that completes the level when the player enters it.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class FinishLine : MonoBehaviour{
        [Header("Effects")]
        [Tooltip("Particle effect to play when the finish line is crossed.")]
        public ParticleSystem celebrationVFX;

        [Tooltip("Sound to play when the finish line is crossed.")]
        public AudioClip celebrationSfx;

        private bool _isFinished;

        private void Awake() {
            // Ensure the collider is a trigger to allow the player to pass through
            GetComponent<Collider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other) {
            // Check if the level is not already finished and if the object is the player
            if (!_isFinished && other.CompareTag("Player")) {
                _isFinished = true; // Prevents the trigger from firing multiple times

                // Play effects if they are assigned
                if (celebrationVFX != null) {
                    celebrationVFX.Play();
                }

                if (celebrationSfx != null) {
                    AudioSource.PlayClipAtPoint(celebrationSfx, transform.position);
                }

                // Tell the GameManager to complete the level
                if (GameManager.Instance != null) {
                    GameManager.Instance.CompleteLevel();
                }
            }
        }
    }
}