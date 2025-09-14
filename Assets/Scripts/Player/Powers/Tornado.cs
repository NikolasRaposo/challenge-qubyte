using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Player.Powers {
    /// <summary>
    /// Controls the behavior of a tornado projectile. It moves forward for a set duration
    /// and applies an upward force to any player that enters its trigger, making them float.
    /// </summary>
    public class Tornado : MonoBehaviour {
        
        [Header("Tornado Settings")]
        [Tooltip("The total time in seconds the tornado will exist before being destroyed.")]
        public float lifeTime = 3f;
        
        [Tooltip("The initial upward force applied to the player upon entering the tornado.")]
        public float upwardForce = 5f;
        
        // A collection to keep track of players currently inside the tornado's influence.
        private readonly HashSet<ThirdPersonController> _playersInTornado = new HashSet<ThirdPersonController>();

        /// <summary>
        /// Called when the script instance is being loaded.
        /// </summary>
        private void Start() {
            // The tornado currently doesn't move after spawning, but this could be used in Update.
            // transform.Translate(Vector3.forward * speed * Time.deltaTime);

            // Schedule the tornado GameObject to be destroyed after its lifetime expires.
            Destroy(gameObject, lifeTime);
        }

        /// <summary>
        /// Called when another collider enters this object's trigger.
        /// </summary>
        private void OnTriggerEnter(Collider other) {
            // Check if the object is tagged as "Player".
            if (!other.CompareTag("Player")) return;
            ThirdPersonController playerController = other.GetComponent<ThirdPersonController>();
            if (playerController == null) return;
            // Add the player to the tracking set.
            _playersInTornado.Add(playerController);
            // Apply an initial burst of upward force to lift the player.
            playerController.ApplyUpwardForce(upwardForce);
            // Set a custom animation state (e.g., floating/falling).
            playerController.SetFreeFallAnimation(true);
            // Override the player's gravity to make them float inside the tornado.
            playerController.SetGravityOverride(true);
        }

        /// <summary>
        /// Called when another collider exits this object's trigger.
        /// </summary>
        private void OnTriggerExit(Collider other) {
            if (!other.CompareTag("Player")) return;

            if (!other.TryGetComponent(out ThirdPersonController playerController) || !_playersInTornado.Contains(playerController)) return;
            // Restore the player's normal state.
            RestorePlayerState(playerController);
            _playersInTornado.Remove(playerController);
        }

        /// <summary>
        /// Called when the GameObject is being destroyed.
        /// This ensures any players still inside the tornado are safely returned to their normal state.
        /// </summary>
        private void OnDestroy() {
            foreach (ThirdPersonController playerController in _playersInTornado.Where(playerController => playerController != null)) {
                // Restore the player's state.
                playerController.SetFreeFallAnimation(false);
                playerController.SetGravityOverride(false);
            }
            // Clear the set for good measure.
            _playersInTornado.Clear();
        }
        
        /// <summary>
        /// Reverts the player to their normal state (with gravity and default animations).
        /// </summary>
        private void RestorePlayerState(ThirdPersonController playerController)
        {
            playerController.SetFreeFallAnimation(false);
            playerController.SetGravityOverride(false);
        }
    }
}