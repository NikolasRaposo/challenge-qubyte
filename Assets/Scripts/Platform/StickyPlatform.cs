using UnityEngine;
namespace Platform {
    /// <summary>
    /// This script makes any object with the "Player" tag that enters its trigger
    /// move along with the platform. It should be added to the moving platform object.
    /// It also provides the platform's velocity to the player controller.
    /// </summary>
    public class StickyPlatform : MonoBehaviour, IPlatformVelocityProvider {
        [Tooltip("The transform the player will be parented to. This is the part of the platform the player stands on.")]
        [SerializeField] private Transform platform;
        [Tooltip("The transform that is being animated or moved (often the root or a parent of the platform). If null, it will use the platform transform itself to calculate velocity.")]
        [SerializeField] private Transform animatedTransform;
    
        // Variables for calculating the platform's velocity.
        private Vector3 _lastPosition;
        private Vector3 _currentVelocity;
        private bool _isInitialized = false;
        private Transform _trackedTransform;
    
        /// <summary>
        /// Initializes the component by determining which transform to track for velocity calculations.
        /// </summary>
        private void Start() {
            // Decide which transform to track: the animated parent if available, otherwise the platform itself.
            _trackedTransform = animatedTransform != null ? animatedTransform : platform;
            if (_trackedTransform != null) {
                // Store the initial position to calculate velocity in the first Update frame.
                _lastPosition = _trackedTransform.position;
                _isInitialized = true;
            } else {
                Debug.LogError("StickyPlatform requires at least the 'Platform' transform to be assigned!", this);
            }
        }
    
        /// <summary>
        /// Called every frame to calculate the platform's current velocity.
        /// </summary>
        private void Update() {
            // Only calculate if the component has been properly initialized.
            if (!_isInitialized || !_trackedTransform) return;
            // Velocity is the change in position divided by the change in time.
            Vector3 currentPosition = _trackedTransform.position;
            _currentVelocity = (currentPosition - _lastPosition) / Time.deltaTime;
            _lastPosition = currentPosition;
        }

        /// <summary>
        /// Called when another collider enters the platform's trigger.
        /// </summary>
        private void OnTriggerEnter(Collider other) {
            // Check if the object that entered is the player.
            if (!other.gameObject.CompareTag("Player")) return;
            // Parent the player to the platform so they move together.
            other.gameObject.transform.SetParent(platform);
            
            // Notify the ThirdPersonController that it has entered a platform.
            ThirdPersonController controller = other.GetComponent<ThirdPersonController>();
            if (controller != null) {
                // Pass a reference to this script so the controller can query its velocity.
                controller.OnEnterPlatform(this);
            }
        }

        /// <summary>
        /// Called when a collider exits the platform's trigger.
        /// </summary>
        private void OnTriggerExit(Collider other) {
            if (!other.gameObject.CompareTag("Player")) return;
            // Unparent the player when they leave the platform.
            other.gameObject.transform.SetParent(null);
            
            // Notify the ThirdPersonController that it has exited the platform.
            ThirdPersonController controller = other.GetComponent<ThirdPersonController>();
            if (controller != null) {
                controller.OnExitPlatform();
            }
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
            return _currentVelocity.magnitude > 0.01f;
        }
        #endregion
    }
}