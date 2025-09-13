using System.Collections;
using Managers;
using UnityEngine;
using UnityEngine.Serialization;
namespace Player {
    /// <summary>
    /// Manages the player's health state, handling death and coordinating the respawn process.
    /// </summary>
    public class PlayerHealth : MonoBehaviour {
        [Header("Effects")]
        [Tooltip("Particle system to instantiate when the player dies.")]
        public GameObject deathVFX;

        [Tooltip("Sound to play when the player dies.")]
        public AudioClip deathSfx;

        [Tooltip("The main visual representation of the player that will be hidden upon death.")]
        public GameObject playerModel; // Arraste o modelo 3D do Saci aqui

        private bool _isDead;

        /// <summary>
        /// Triggers the player's death sequence.
        /// </summary>
        public void Die() {
            // Prevent the Die() function from being called multiple times
            if (_isDead) return;
            _isDead = true;

            // Play visual effects
            if (deathVFX != null) {
                Instantiate(deathVFX, transform.position, Quaternion.identity);
            }

            // Play sound effects
            if (deathSfx != null) {
                AudioSource.PlayClipAtPoint(deathSfx, transform.position);
            }

            // Hide the player model
            if (playerModel != null) {
                playerModel.SetActive(false);
            } else {
                // Fallback: try to disable the renderer on this object
                GetComponentInChildren<Renderer>().enabled = false;
            }

            // Disable player controls
            // Assumes you are using the ThirdPersonController from StarterAssets
            GetComponent<ThirdPersonController>().enabled = false;
            GetComponent<CharacterController>().enabled = false;

            // Tell the GameManager to start the respawn process after a delay
            StartCoroutine(NotifyGameManagerOfRespawn());
        }

        private IEnumerator NotifyGameManagerOfRespawn() {
            // Wait for a moment before respawning
            yield return new WaitForSeconds(2f);

            if (GameManager.Instance != null) {
                GameManager.Instance.RespawnPlayer();
            }
        }

        /// <summary>
        /// Resets the player's state for respawning.
        /// </summary>
        public void PrepareForRespawn() {
            // Show the player model again
            if (playerModel != null) {
                playerModel.SetActive(true);
            } else {
                GetComponentInChildren<Renderer>().enabled = true;
            }
        
            // Re-enable controls
            GetComponent<ThirdPersonController>().enabled = true;
            GetComponent<CharacterController>().enabled = true;

            // Reset the death flag
            _isDead = false;
        }
    }
}