using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using UnityEngine;
namespace Gameplay {
    /// <summary>
    /// Manages the behavior of various types of platforms, such as moving, sinking, disappearing, or rotating platforms.
    /// Requires a Collider component on the same GameObject.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class PlatformBehavior : MonoBehaviour {
        [Header("🔧 General Settings")]
        [Tooltip("Enable this for a platform that moves back and forth or follows waypoints.")]
        public bool isMovingPlatform;
        [Tooltip("Enable this for a platform that sinks when the player stands on it.")]
        public bool sinksUnderWeight;
        [Tooltip("Enable this for a platform that disappears after the player touches it.")]
        public bool disappearsOnContact;
        [Tooltip("Enable this for a platform that falls after the player touches it.")]
        public bool fallsOnContact;
        [Tooltip("If the platform disappears or falls, should it reappear after a delay?")]
        public bool respawnsAfterDelay;
        [Tooltip("Enable this for a platform that rotates continuously.")]
        public bool rotates;
        [Tooltip("Enable this for a platform that follows a set of waypoints.")]
        public bool followsWaypoints;
        [Tooltip("Enable this to make the platform act like a trampoline.")]
        public bool isTrampoline;

        [Header("🎯 Moving Platform")]
        [Tooltip("The direction of movement relative to the platform's starting position.")]
        public Vector3 moveDirection = Vector3.right;
        [Tooltip("The distance the platform will travel in the specified direction.")]
        public float moveDistance = 2f;
        [Tooltip("The duration of one leg of the movement (from start to end).")]
        public float moveDuration = 2f;
        [Tooltip("If true, the platform will move back and forth (YoYo). If false, it will loop back to the start.")]
        public bool pingPongMovement = true;

        [Header("📦 Sinking Platform")]
        [Tooltip("How far down the platform sinks when stepped on.")]
        public float sinkDepth = 0.5f;
        [Tooltip("How quickly the platform sinks and rises.")]
        public float sinkSpeed = 0.3f;

        [Header("👻 Disappearing / Falling")]
        [Tooltip("The delay in seconds before the platform disappears after contact.")]
        public float disappearDelay = 1f;
        [Tooltip("The delay in seconds before the platform falls after contact.")]
        public float fallDelay = 1f;

        [Header("♻️ Respawning")]
        [Tooltip("The time in seconds before the platform reappears.")]
        public float respawnTime = 3f;

        [Header("🔁 Rotation")]
        [Tooltip("The axis around which the platform will rotate.")]
        public Vector3 rotationAxis = Vector3.up;
        [Tooltip("The speed of rotation in degrees per second.")]
        public float rotationSpeed = 45f;

        [Header("🧭 Waypoints")]
        [Tooltip("A list of transforms that the platform will move between.")]
        public Transform[] waypoints;
        [Tooltip("The time it takes to travel between each waypoint.")]
        public float timePerWaypoint = 1.5f;
        [Tooltip("If true, the platform will loop through the waypoints continuously.")]
        public bool loopWaypoints = true;

        [Header("🦘 Trampoline")]
        [Tooltip("The force applied to the player when they jump on the platform.")]
        public float reboundForce = 10f;

        [Header("✨ Visual Feedback")]
        [Tooltip("If true, the platform will shake before it falls or disappears.")]
        public bool shakeBeforeFalling = true;
        [Tooltip("The duration of the shake animation.")]
        public float shakeDuration = 0.5f;
        [Tooltip("The intensity of the positional shake.")]
        public float shakeIntensity = 0.05f;
        [Tooltip("If true, play an animation when the platform respawns.")]
        public bool animateRespawn = true;
        [Tooltip("The duration of the respawn animation.")]
        public float respawnAnimationDuration = 0.6f;

        // --- Private State Variables ---
        private Vector3 _initialPosition;
        private Sequence _waypointSequence;
        private Renderer _platformRenderer;
        private Collider _platformCollider;

        /// <summary>
        /// Initializes components and validates settings.
        /// </summary>
        private void Awake() {
            _platformRenderer = GetComponent<Renderer>();
            _platformCollider = GetComponent<Collider>();
            _initialPosition = transform.position;
            ValidateConflicts();
        }

        /// <summary>
        /// Sets up the platform's continuous behaviors (movement, rotation) at the start of the game.
        /// </summary>
        private void Start() {
            // Setup for a simple moving platform.
            if (isMovingPlatform) {
                Vector3 destination = transform.position + moveDirection.normalized * moveDistance;
                TweenerCore<Vector3, Vector3, VectorOptions> moveTween = transform.DOMove(destination, moveDuration).SetEase(Ease.InOutSine);
                moveTween.SetLoops(-1, pingPongMovement ? LoopType.Yoyo : LoopType.Restart);
            }

            // Setup for a rotating platform.
            if (rotates) {
                float duration = 360f / rotationSpeed; // Calculate duration based on speed.
                transform.DORotate(rotationAxis * 360, duration, RotateMode.FastBeyond360)
                    .SetLoops(-1, LoopType.Incremental) // Use Incremental for continuous rotation.
                    .SetEase(Ease.Linear);
            }

            // Setup for a waypoint-following platform.
            if (!followsWaypoints || waypoints.Length <= 1) return;
            _waypointSequence = DOTween.Sequence();
            // Append a move command for each waypoint in the list.
            foreach (Transform waypoint in waypoints) {
                _waypointSequence.Append(transform.DOMove(waypoint.position, timePerWaypoint).SetEase(Ease.InOutSine));
            }
            if (loopWaypoints) _waypointSequence.SetLoops(-1);
            _waypointSequence.Play();
        }

        /// <summary>
        /// Detects when the player enters the platform's trigger.
        /// </summary>
        private void OnTriggerEnter(Collider other) {
            if (!other.CompareTag("Player")) return;

            // Sinking behavior.
            if (sinksUnderWeight) {
                transform.DOMoveY(_initialPosition.y - sinkDepth, sinkSpeed);
            }

            // Disappearing behavior.
            if (disappearsOnContact) {
                Invoke(nameof(Disappear), disappearDelay);
            }

            // Falling behavior.
            if (fallsOnContact) {
                Invoke(nameof(Fall), fallDelay);
            }

            // Trampoline behavior.
            if (!isTrampoline) return;
            Rigidbody rb = other.attachedRigidbody;
            if (rb == null) return;
            // Reset vertical velocity for a consistent bounce height.
            rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z); 
            rb.AddForce(Vector3.up * reboundForce, ForceMode.VelocityChange);
        }

        /// <summary>
        /// Detects when the player exits the platform's trigger.
        /// </summary>
        private void OnTriggerExit(Collider other) {
            if (!other.CompareTag("Player")) return;
            // If the platform sinks, make it rise back up.
            if (sinksUnderWeight) {
                transform.DOMoveY(_initialPosition.y, sinkSpeed);
            }
        }

        /// <summary>
        /// Makes the platform disappear.
        /// </summary>
        private void Disappear() {
            // Optional shake feedback before disappearing.
            if (shakeBeforeFalling) {
                PlayShake(() => {
                    _platformRenderer.enabled = false;
                    _platformCollider.enabled = false;
                    if (respawnsAfterDelay)
                        Invoke(nameof(Respawn), respawnTime);
                });
            } else {
                _platformRenderer.enabled = false;
                _platformCollider.enabled = false;
                if (respawnsAfterDelay)
                    Invoke(nameof(Respawn), respawnTime);
            }
        }

        /// <summary>
        /// Makes the platform fall.
        /// </summary>
        private void Fall() {
            // Optional shake feedback before falling.
            if (shakeBeforeFalling) {
                PlayShake(() => {
                    // Add a Rigidbody to make it affected by gravity.
                    if (GetComponent<Rigidbody>() == null) gameObject.AddComponent<Rigidbody>();
                    _platformCollider.isTrigger = false; // Allow for physical collision as it falls.
                    if (respawnsAfterDelay)
                        Invoke(nameof(RespawnAfterFall), respawnTime);
                });
            } else {
                if (GetComponent<Rigidbody>() == null) gameObject.AddComponent<Rigidbody>();
                _platformCollider.isTrigger = false;
                if (respawnsAfterDelay)
                    Invoke(nameof(RespawnAfterFall), respawnTime);
            }
        }

        /// <summary>
        /// Respawns a platform that disappeared.
        /// </summary>
        private void Respawn() {
            transform.position = _initialPosition;
            _platformCollider.enabled = true;

            if (animateRespawn) {
                transform.localScale = Vector3.zero;
                _platformRenderer.enabled = true;
                // Play a scale-up animation.
                transform.DOScale(Vector3.one, respawnAnimationDuration).SetEase(Ease.OutBack);
            } else {
                _platformRenderer.enabled = true;
            }
        }

        /// <summary>
        /// Respawns a platform that fell.
        /// </summary>
        private void RespawnAfterFall() {
            // Clean up the Rigidbody component.
            if (GetComponent<Rigidbody>() != null) Destroy(GetComponent<Rigidbody>());

            transform.position = _initialPosition;
            transform.rotation = Quaternion.identity; // Reset rotation.
            _platformCollider.enabled = true;
            _platformCollider.isTrigger = true; // Set it back to a trigger.

            if (animateRespawn) {
                transform.localScale = Vector3.zero;
                _platformRenderer.enabled = true;
                transform.DOScale(Vector3.one, respawnAnimationDuration).SetEase(Ease.OutBack);
            } else {
                _platformRenderer.enabled = true;
            }
        }

        /// <summary>
        /// Checks for and resolves conflicting behavior settings.
        /// </summary>
        private void ValidateConflicts() {
            if (disappearsOnContact && fallsOnContact) {
                Debug.LogWarning($"[PlatformBehavior] Conflict: 'disappearsOnContact' and 'fallsOnContact' are both active. Defaulting to 'disappears'.", this);
                fallsOnContact = false;
            }
            if (!isMovingPlatform || !followsWaypoints) return;
            Debug.LogWarning($"[PlatformBehavior] Conflict: 'isMovingPlatform' and 'followsWaypoints' are both active. 'followsWaypoints' will take priority.", this);
            isMovingPlatform = false;
        }
    
        /// <summary>
        /// Plays a shake animation and executes an action upon completion.
        /// </summary>
        /// <param name="onComplete">The action to invoke after the shake finishes.</param>
        private void PlayShake(System.Action onComplete) {
            transform.DOShakePosition(shakeDuration, shakeIntensity)
                .OnComplete(() => onComplete?.Invoke());
        }
    }
}