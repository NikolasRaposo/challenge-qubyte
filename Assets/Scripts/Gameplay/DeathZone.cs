using Player;
using UnityEngine;
namespace Gameplay {
    /// <summary>
    /// A trigger volume that causes any object with a PlayerHealth component to die upon entry.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class DeathZone : MonoBehaviour {
        private void Awake() {
            // Ensure the collider is set to be a trigger
            GetComponent<Collider>().isTrigger = true;
        }
        private void OnTriggerEnter(Collider other) {
            // Check if the entering object has a PlayerHealth component
            if (other.TryGetComponent(out PlayerHealth playerHealth)) {
                // Trigger the player's death sequence
                playerHealth.Die();
            }
        }
    }
}
