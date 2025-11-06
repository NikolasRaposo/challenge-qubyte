using System.Collections;
using Gameplay;
using Managers;
using UnityEngine;

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
        public GameObject playerModel;
        
        private bool _isDead;

        /// <summary>
        /// Triggers the player's death sequence.
        /// </summary>
        public void Die() {
            if (_isDead) return;
            _isDead = true;
            RumbleManager.Instance?.PlayPlayerDeathRumble();
            var vfxController = GetComponent<Player.ECMSaciVfxController>();
            if (vfxController != null) {
                vfxController.OnDeath();
            } else if (deathVFX != null) {
                Instantiate(deathVFX, transform.position, Quaternion.identity);
            }
            if (deathSfx != null) {
                AudioSource.PlayClipAtPoint(deathSfx, transform.position);
            }
            if (playerModel != null) {
                playerModel.SetActive(false);
            } else {
                GetComponentInChildren<Renderer>().enabled = false;
            }
            StartCoroutine(NotifyGameManagerOfRespawn());
        }
        private static IEnumerator NotifyGameManagerOfRespawn() {
            yield return new WaitForSeconds(2f);
            if (GameManager.Instance) {
                GameManager.Instance.RespawnPlayer();
            }
        }

        /// <summary>
        /// Resets the player's state for respawning.
        /// </summary>
        public void PrepareForRespawn() {
            if (playerModel) {
                playerModel.SetActive(true);
            } else {
                GetComponentInChildren<Renderer>().enabled = true;
            }
            _isDead = false;
        }
    }
}