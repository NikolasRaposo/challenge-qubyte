using Boss;
using Managers;
using UnityEngine;
namespace World.Triggers
{
    /// <summary>
    /// A simple trigger volume that starts the boss battle when the player enters it.
    /// Deactivates itself after being used once.
    /// This script requires a Collider component set to 'Is Trigger'.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class StartBattleTrigger : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Drag the boss GameObject here. It must have the CapeloboBoss script on it.")]
        [SerializeField] private CapeloboBoss bossController;

        // Flag to ensure the trigger only fires once.
        private bool _hasBeenTriggered;
        private Collider _triggerCollider;
        
        private void Awake() {
            // Cache the reference to the collider.
            _triggerCollider = GetComponent<Collider>();
        }

        private void Start() {
            if (GameManager.Instance != null) {
                GameManager.Instance.OnPlayerRespawn += ResetTrigger;
            }
        }

        /// <summary>
        /// Subscribes to the respawn event when the object is enabled.
        /// </summary>
        private void OnEnable() {
            if (GameManager.Instance != null) {
                GameManager.Instance.OnPlayerRespawn += ResetTrigger;
            }
        }
        /// <summary>
        /// Unsubscribes from the respawn event when the object is disabled.
        /// </summary>
        private void OnDisable() {
            if (GameManager.Instance != null) {
                GameManager.Instance.OnPlayerRespawn -= ResetTrigger;
            }
        }

        
        /// <summary>
        /// Called by Unity when another collider enters this object's trigger.
        /// </summary>
        /// <param name="other">The collider that entered the trigger zone.</param>
        private void OnTriggerEnter(Collider other) {
            // Check if the trigger has already been used or if the object that entered is not the player.
            if (_hasBeenTriggered || !other.CompareTag("Player")) {
                return;
            }

            // A safety check to ensure the boss has been assigned in the Inspector.
            if (bossController == null) {
                Debug.LogError("Boss Controller has not been assigned to the StartBattleTrigger!", gameObject);
                return;
            }

            // Mark as triggered to prevent it from running again.
            _hasBeenTriggered = true;
            
            Debug.Log("Player entered the battle arena. Starting the boss fight!");
            
            // Call the public method on the boss script to start the battle.
            bossController.StartBattle();

            _triggerCollider.enabled = false;
        }
        /// <summary>
        /// Resets the trigger to its initial state. Called by the GameManager's event.
        /// </summary>
        private void ResetTrigger() {
            _hasBeenTriggered = false;
            _triggerCollider.enabled = true;
        }
        /// <summary>
        /// Draws a gizmo in the editor to make the trigger volume visible.
        /// </summary>
        private void OnDrawGizmos() {
            // Draws a semi-transparent green box to represent the trigger area.
            Gizmos.color = new Color(0, 1, 0, 0.25f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(Vector3.zero, Vector3.one);
        }
    }
}