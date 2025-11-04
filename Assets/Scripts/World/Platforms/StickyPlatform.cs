using Player;
using UnityEngine;
namespace Gameplay.Platform {
    /// <summary>
    /// This script makes any object with the "Player" tag that enters its trigger
    /// move along with the platform. It should be added to the moving platform object.
    /// It also provides the platform's velocity to the player controller.
    /// </summary>
    public class StickyPlatform : MonoBehaviour, IPlatformVelocityProvider {
        [Tooltip("The transform the player will be parented to. This is the part of the platform the player stands on.")]
        [SerializeField] private Transform platform;
        [Tooltip("The transform that is being animated or moved (often the root or a parent of the platform). If null, it will use this object's transform to calculate velocity.")]
        [SerializeField] private Transform animatedTransform;

        private Vector3 _lastPosition;
        private Vector3 _currentVelocity;
        private Transform _trackedTransform;

        /// <summary>
        /// Initializes the component by validating references and setting up the tracking transform.
        /// </summary>
        private void Start() {
            // REFACTORED: Added a null check for the critical 'platform' reference.
            if (platform == null) {
                Debug.LogError($"[StickyPlatform] The 'Platform' transform is not assigned on {gameObject.name}. This component will be disabled.", this);
                enabled = false; // Disable the component to prevent runtime errors.
                return;
            }
            // Decide which transform to track for velocity calculations.
            _trackedTransform = animatedTransform != null ? animatedTransform : transform;
            // Store the initial position to calculate velocity in the first FixedUpdate frame.
            _lastPosition = _trackedTransform.position;
        }

        /// <summary>
        /// Called at a fixed time interval to calculate the platform's current velocity.
        /// </summary>
        // REFACTORED: Moved velocity calculation from Update to FixedUpdate for physics consistency.
        private void FixedUpdate() {
            // REFACTORED: Simplified guard clause, removing the need for '_isInitialized'.
            if (!_trackedTransform) return;
            Vector3 currentPosition = _trackedTransform.position;
            // REFACTORED: Using Time.fixedDeltaTime to align with FixedUpdate.
            _currentVelocity = (currentPosition - _lastPosition) / Time.fixedDeltaTime;
            _lastPosition = currentPosition;
        }

        /// <summary>
        /// Called when another collider enters the platform's trigger.
        /// </summary>
        private void OnTriggerEnter(Collider other) {
            if (!TryGetPlayerController(other, out ThirdPersonController controller)) return;
            other.transform.SetParent(platform);
            controller.OnEnterPlatform(this);
        }

        /// <summary>
        /// Called when a collider exits the platform's trigger.
        /// </summary>
        private void OnTriggerExit(Collider other) {
            if (!TryGetPlayerController(other, out ThirdPersonController controller)) return;
            other.transform.SetParent(null);
            controller.OnExitPlatform();
        }

        /// <summary>
        /// Checks if the collider belongs to the player and retrieves its controller component.
        /// </summary>
        // REFACTORED: New helper method to avoid code repetition (DRY principle).
        private static bool TryGetPlayerController(Collider other, out ThirdPersonController controller) {
            controller = null;
            if (!other.CompareTag("Player")) return false;
            controller = other.GetComponent<ThirdPersonController>();
            return controller != null;
        }

        #region IPlatformVelocityProvider Implementation
        /// <summary>
        /// Provides the platform's current velocity.
        /// </summary>
        public Vector3 GetPlatformVelocity() {
            return _currentVelocity;
        }

        /// <summary>
        /// Checks if the platform is currently moving.
        /// </summary>
        public bool IsMoving() {
            // Use a small threshold to account for floating-point inaccuracies.
            return _currentVelocity.sqrMagnitude > 0.0001f; // Using sqrMagnitude is slightly more performant.
        }
        #endregion
    }
}