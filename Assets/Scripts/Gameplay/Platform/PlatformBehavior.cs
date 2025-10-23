using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using UnityEngine;
namespace Gameplay.Platform {
    /// <summary>
    /// Manages the behavior of various types of platforms, such as moving, sinking, disappearing, or rotating platforms.
    /// Requires a Collider component on the same GameObject.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class PlatformBehavior : MonoBehaviour {
        [Header("General Settings")]
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

        [Header("Moving Platform")]
        [Tooltip("The direction of movement relative to the platform's starting position.")]
        public Vector3 moveDirection = Vector3.right;
        [Tooltip("The distance the platform will travel in the specified direction.")]
        public float moveDistance = 2f;
        [Tooltip("The duration of one leg of the movement (from start to end).")]
        public float moveDuration = 2f;
        [Tooltip("If true, the platform will move back and forth (YoYo). If false, it will loop back to the start.")]
        public bool pingPongMovement = true;

        [Header("Sinking Platform")]
        [Tooltip("How far down the platform sinks when stepped on.")]
        public float sinkDepth = 0.5f;
        [Tooltip("How quickly the platform sinks and rises.")]
        public float sinkSpeed = 0.3f;

        [Header("Disappearing / Falling")]
        [Tooltip("The delay in seconds before the platform disappears after contact.")]
        public float disappearDelay = 1f;
        [Tooltip("The delay in seconds before the platform falls after contact.")]
        public float fallDelay = 1f;

        [Header("Respawning")]
        [Tooltip("The time in seconds before the platform reappears.")]
        public float respawnTime = 3f;

        [Header("Rotation")]
        [Tooltip("The axis around which the platform will rotate.")]
        public Vector3 rotationAxis = Vector3.up;
        [Tooltip("The speed of rotation in degrees per second.")]
        public float rotationSpeed = 45f;

        [Header("Waypoints")]
        [Tooltip("A list of transforms that the platform will move between.")]
        public Transform[] waypoints;
        [Tooltip("The time it takes to travel between each waypoint.")]
        public float timePerWaypoint = 1.5f;
        [Tooltip("If true, the platform will loop through the waypoints continuously.")]
        public bool loopWaypoints = true;

        [Header("Trampoline")]
        [Tooltip("The force applied to the player when they jump on the platform.")]
        public float reboundForce = 10f;

        [Header("Visual Feedback")]
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
        
        [Header("Debugging")]
        [Tooltip("Enable this to see detailed log messages in the console during gameplay.")]
        public bool enableDebugLogs;

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
            
            // Critical setup logs - these should always appear if there's a problem.
            if (_platformCollider == null) {
                Debug.LogError($"[PlatformBehavior] Platform '{gameObject.name}' is missing a Collider component!", this);
            } else if (!_platformCollider.isTrigger) {
                Debug.LogWarning($"[PlatformBehavior] Collider on '{gameObject.name}' is not set to 'Is Trigger'. This is required for contact detection.", this);
            }

            ValidateConflicts();
        }

        /// <summary>
        /// Sets up the platform's continuous behaviors (movement, rotation) at the start of the game.
        /// </summary>
        private void Start() {
            if (isMovingPlatform) {
                Vector3 destination = transform.position + moveDirection.normalized * moveDistance;
                TweenerCore<Vector3, Vector3, VectorOptions> moveTween = transform.DOMove(destination, moveDuration).SetEase(Ease.InOutSine);
                moveTween.SetLoops(-1, pingPongMovement ? LoopType.Yoyo : LoopType.Restart);
            }
            
            if (rotates) {
                float duration = 360f / rotationSpeed;
                transform.DORotate(rotationAxis * 360, duration, RotateMode.FastBeyond360)
                    .SetLoops(-1, LoopType.Incremental)
                    .SetEase(Ease.Linear);
            }
            
            if (!followsWaypoints || waypoints.Length <= 1) return;
            _waypointSequence = DOTween.Sequence();
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
            if (enableDebugLogs) 
                Debug.Log($"[PlatformBehavior] Trigger entered by '{other.gameObject.name}' which has tag '{other.tag}'.", this);

            if (!other.CompareTag("Player")) return;
            
            if (enableDebugLogs)
                Debug.Log($"[PlatformBehavior] Player detected on platform '{gameObject.name}'.", this);

            if (sinksUnderWeight) {
                transform.DOMoveY(_initialPosition.y - sinkDepth, sinkSpeed);
            }
            
            if (disappearsOnContact) {
                Invoke(nameof(Disappear), disappearDelay);
            }
            
            if (fallsOnContact) {
                if (enableDebugLogs)
                    Debug.Log($"[PlatformBehavior] 'fallsOnContact' is TRUE. Scheduling Fall() in {fallDelay} seconds.", this);
                Invoke(nameof(Fall), fallDelay);
            } else {
                if (enableDebugLogs)
                    Debug.LogWarning($"[PlatformBehavior] Player detected, but 'fallsOnContact' is FALSE. Check the inspector.", this);
            }
            
            if (!isTrampoline) return;
            Rigidbody rb = other.attachedRigidbody;
            if (rb == null) return;
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z); 
            rb.AddForce(Vector3.up * reboundForce, ForceMode.VelocityChange);
        }

        /// <summary>
        /// Detects when the player exits the platform's trigger.
        /// </summary>
        private void OnTriggerExit(Collider other) {
            if (!other.CompareTag("Player")) return;
            if (sinksUnderWeight) {
                transform.DOMoveY(_initialPosition.y, sinkSpeed);
            }
        }

        /// <summary>
        /// Makes the platform disappear.
        /// </summary>
        private void Disappear() {
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
            if (enableDebugLogs)
                Debug.Log($"[PlatformBehavior] Executing Fall() method on '{gameObject.name}'. Platform should now fall.", this);

            if (shakeBeforeFalling) {
                PlayShake(() => {
                    if (GetComponent<Rigidbody>() == null) gameObject.AddComponent<Rigidbody>();
                    _platformCollider.isTrigger = false;
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
                transform.DOScale(Vector3.one, respawnAnimationDuration).SetEase(Ease.OutBack);
            } else {
                _platformRenderer.enabled = true;
            }
        }

        /// <summary>
        /// Respawns a platform that fell.
        /// </summary>
        private void RespawnAfterFall() {
            if (GetComponent<Rigidbody>() != null) Destroy(GetComponent<Rigidbody>());

            transform.position = _initialPosition;
            transform.rotation = Quaternion.identity;
            _platformCollider.enabled = true;
            _platformCollider.isTrigger = true;

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