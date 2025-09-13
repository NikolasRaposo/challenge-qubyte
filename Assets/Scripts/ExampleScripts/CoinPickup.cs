using DG.Tweening;
using UnityEngine;
namespace ExampleScripts {
    /// <summary>
    /// Controls the behavior of collectible coins, including rotation, magnetism, and collection effects.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class CoinPickup : MonoBehaviour {
        [Header("Collection Effects")]
        [Tooltip("The particle system that will be activated when the coin is collected.")]
        public ParticleSystem collectionEffect;
        [Tooltip("The sound that will be played when the coin is collected.")]
        public AudioClip collectionSound;
        [Tooltip("Time in seconds before destroying the object after being collected.")]
        public float destroyDelay = 1f;

        [Header("Animation")]
        [Tooltip("Rotation speed in degrees per second.")]
        public float rotationSpeed = 180f;

        [Header("Magnetism")]
        [Tooltip("The speed at which the coin moves towards the player when attracted.")]
        public float magnetismSpeed = 5f;
        [Tooltip("The minimum distance to automatically collect the coin.")]
        public float minCollectionDistance = 0.5f;
        [Tooltip("A custom attraction point on the player (optional). If not set, it will use the player's transform.")]
        public Transform customAttractionPoint;
        [Tooltip("The delay in seconds before the coin can be affected by magnetism. Prevents it from being attracted immediately after spawning.")]
        public float magnetismDelay = 0.6f;
        [Tooltip("If enabled, ignores the magnetism delay. Useful for coins placed directly in the scene.")]
        public bool ignoreDelay;
        [Tooltip("Time in seconds to disable touch collection during the spawn/spread animation.")]
        public float touchCollectionDisableTime = 2f;

        // --- Private State Variables ---
        private bool _isCollected;
        private bool _isMagnetismActive;
        private bool _canBeAttracted;
        private bool _canBeTouched = true;
        private bool _spreadIsComplete;
        private float _spawnTime;
        private Transform _targetPlayer;
        private Transform _currentAttractionPoint;
        private Collider _lastMagneticTrigger;
        private string _rotationTweenId;

        /// <summary>
        /// Called when the script instance is being loaded. Initializes the coin.
        /// </summary>
        private void Awake() {
            // Ensure the collider is set to be a trigger.
            GetComponent<Collider>().isTrigger = true;

            // Record the spawn time to manage the magnetism delay.
            _spawnTime = Time.time;

            // Temporarily disable touch collection to allow spawn animations to play out.
            _canBeTouched = false;
            Invoke(nameof(EnableTouchCollection), touchCollectionDisableTime);

            // If 'ignoreDelay' is checked, enable magnetism immediately.
            if (!ignoreDelay) return;
            _canBeAttracted = true;
            _spreadIsComplete = true; // Assume spread is complete for pre-placed coins.
        }

        /// <summary>
        /// Called on the frame when a script is enabled before any of the Update methods are called the first time.
        /// </summary>
        private void Start() {
            // Start the continuous rotation animation.
            StartRotation();
        }

        /// <summary>
        /// Starts the continuous rotation animation of the coin using DOTween.
        /// </summary>
        private void StartRotation() {
            if (transform == null) return; // Safety check

            // Calculate the duration for one full 360-degree rotation.
            float rotationDuration = 360f / rotationSpeed;

            // Create a unique ID for this tween to prevent it from being accidentally killed by other operations.
            _rotationTweenId = "CoinRotation_" + gameObject.GetInstanceID();

            // Start the rotation tween.
            transform.DORotate(new Vector3(0, 360, 0), rotationDuration, RotateMode.FastBeyond360)
                .SetEase(Ease.Linear)
                .SetLoops(-1, LoopType.Restart) // Loop indefinitely.
                .SetId(_rotationTweenId);
        }
    
        /// <summary>
        /// Called every frame. Manages magnetism state and movement.
        /// </summary>
        private void Update() {
            // Check if the magnetism delay has passed.
            if (!_canBeAttracted && Time.time >= _spawnTime + magnetismDelay && (_spreadIsComplete || ignoreDelay)) {
                _canBeAttracted = true;
                // If the coin is already inside a magnetic field, activate magnetism now.
                if (_lastMagneticTrigger) {
                    ActivateMagnetism(_lastMagneticTrigger.transform.parent);
                }
            }
        
            // If magnetism is active, move the coin towards the target.
            if (_isCollected || !_isMagnetismActive || !_currentAttractionPoint) return;
            // Check if the attraction point is still valid.
            if (!_currentAttractionPoint) {
                _isMagnetismActive = false;
                return;
            }

            // Move towards the attraction point.
            transform.position = Vector3.MoveTowards(transform.position, _currentAttractionPoint.position, magnetismSpeed * Time.deltaTime);

            // If close enough, collect the coin.
            if (Vector3.Distance(transform.position, _currentAttractionPoint.position) <= minCollectionDistance) {
                Collect();
            }
        }

        /// <summary>
        /// Called when another collider enters this object's trigger.
        /// </summary>
        /// <param name="other">The other collider.</param>
        private void OnTriggerEnter(Collider other) {
            if (_isCollected) return;

            // Direct collection by player contact.
            if (other.CompareTag("Player") && _canBeTouched) {
                Collect();
            }
            // Entering a magnetic field.
            else if (other.CompareTag("MagneticTrigger")) {
                _lastMagneticTrigger = other; // Store reference to the trigger.
                if (_canBeAttracted) {
                    ActivateMagnetism(other.transform.parent); // The player is the parent of the trigger.
                }
            }
        }

        /// <summary>
        /// Called when the coin stays within a trigger.
        /// </summary>
        private void OnTriggerStay(Collider other) {
            if (_isCollected) return;
        
            if (other.CompareTag("MagneticTrigger")) {
                // Keep the trigger reference updated. This helps if the player moves away and comes back.
                _lastMagneticTrigger = other;
            }
        }

        /// <summary>
        /// Called when a collider exits the trigger.
        /// </summary>
        private void OnTriggerExit(Collider other) {
            // If we exited the trigger we were tracking, clear the reference.
            if (other == _lastMagneticTrigger) {
                _lastMagneticTrigger = null;
            }
        }

        /// <summary>
        /// Activates the magnetic attraction towards a target.
        /// </summary>
        /// <param name="player">The player's transform.</param>
        private void ActivateMagnetism(Transform player) {
            if (_isCollected || _isMagnetismActive) return;

            _isMagnetismActive = true;
            _targetPlayer = player;
        
            // Use the custom attraction point if available, otherwise use the player's transform.
            _currentAttractionPoint = customAttractionPoint ? customAttractionPoint : _targetPlayer;
        }

        /// <summary>
        /// Executes the collection logic, playing effects and destroying the coin.
        /// </summary>
        private void Collect() {
            if (_isCollected) return; // Prevent double collection.
            _isCollected = true;

            // Stop all coin-specific tweens.
            DOTween.Kill(_rotationTweenId);
        
            // Deactivate magnetism logic.
            _isMagnetismActive = false;
            _targetPlayer = null;
            _currentAttractionPoint = null;
            _lastMagneticTrigger = null;

            // Play collection effects.
            if (collectionEffect) {
                collectionEffect.Play();
            }
            if (collectionSound) {
                // Play sound at the coin's position.
                AudioSource.PlayClipAtPoint(collectionSound, transform.position);
            }

            // Hide the coin's visual components.
            foreach (Transform child in transform) {
                child.gameObject.SetActive(false);
            }
            GetComponent<Collider>().enabled = false;

            // Schedule the GameObject for destruction.
            Destroy(gameObject, destroyDelay);
        }
    
        /// <summary>
        /// Public method called by other scripts (like ItemEffectController) to signal that the spawn animation is done.
        /// </summary>
        public void OnSpreadComplete() {
            _spreadIsComplete = true;
        }

        /// <summary>
        /// Sets a custom attraction point from another script.
        /// </summary>
        /// <param name="newAttractionPoint">The new transform to be attracted to.</param>
        public void SetCustomAttractionPoint(Transform newAttractionPoint) {
            customAttractionPoint = newAttractionPoint;
            // If magnetism is already active, update the target immediately.
            if (_isMagnetismActive && _targetPlayer != null) {
                _currentAttractionPoint = customAttractionPoint != null ? customAttractionPoint : _targetPlayer;
            }
        }

        /// <summary>
        /// Re-enables collection by direct touch after the initial delay.
        /// </summary>
        private void EnableTouchCollection() {
            _canBeTouched = true;
        }

        /// <summary>
        /// Called when the GameObject is destroyed. Ensures all tweens are cleaned up.
        /// </summary>
        private void OnDestroy() {
            // A final safety check to kill any running tweens associated with this coin.
            DOTween.Kill(_rotationTweenId);
        }
    }
}