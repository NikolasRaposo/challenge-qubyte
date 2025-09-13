using Managers;
using UnityEngine;
namespace Gameplay {
    /// <summary>
    /// When the player touches this trigger, it updates the GameManager with its position as the new respawn point.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class Checkpoint : MonoBehaviour {
        [Tooltip("Effect to play when the checkpoint is activated.")]
        public ParticleSystem activationEffect;
        private bool _isActivated;
        private void Awake() {
            // Ensure the collider is set to be a trigger
            GetComponent<Collider>().isTrigger = true;
        }
        private void OnTriggerEnter(Collider other) {
            // Check if the object is the player and if the checkpoint hasn't been activated yet
            if (_isActivated || !other.CompareTag("Player")) return;
            _isActivated = true;

            if (GameManager.Instance != null) {
                GameManager.Instance.UpdateCheckpoint(transform.position);
            }

            if (activationEffect != null) {
                activationEffect.Play();
            }
            
            // Optional: disable the checkpoint object after use
            // gameObject.SetActive(false);
        }
    }
}
