using DG.Tweening;
using UnityEngine;

namespace Gameplay.Platform {
    /// <summary>
    /// Abstract base class for all platform types.
    /// Contains shared logic for respawning, visual references, and player detection.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public abstract class PlatformBase : MonoBehaviour {
        [Header("Base Platform Settings")]
        [Tooltip("If true, the platform will respawn after being used.")]
        [SerializeField] protected bool respawns = true;
        [Tooltip("The time in seconds before the platform reappears.")]
        [SerializeField] protected float respawnTime = 3f;
        [Tooltip("If true, play an animation when the platform respawns.")]
        [SerializeField] protected bool animateRespawn = true;
        [Tooltip("The duration of the respawn animation.")]
        [SerializeField] protected float respawnAnimationDuration = 0.6f;

        [Header("Base Component References")]
        [Tooltip("The visual part of the platform that has the Renderer component. This is usually the child object.")]
        [SerializeField] protected Renderer platformVisuals;

        [Header("Base Debugging")]
        [Tooltip("Enable this to see detailed log messages in the console during gameplay.")]
        [SerializeField] protected bool enableDebugLogs;

        protected Vector3 initialPosition;
        protected Quaternion initialRotation;
        protected Vector3 initialScale;
        protected Collider platformCollider;
        protected bool isDeactivated;

        /// <summary>
        /// Virtual Awake method to initialize shared components and state.
        /// Subclasses can override this to add their own initialization logic.
        /// </summary>
        protected virtual void Awake() {
            if (enableDebugLogs) Debug.Log($"[PlatformBase.Awake] Initializing '{gameObject.name}'.", this);

            platformCollider = GetComponent<Collider>();
            initialPosition = transform.position;
            initialRotation = transform.rotation;
            initialScale = transform.localScale;
            
            if (enableDebugLogs) Debug.Log($"[PlatformBase.Awake] Stored initial state: Pos={initialPosition}, Rot={initialRotation.eulerAngles}, Scale={initialScale}", this);

            if (platformVisuals == null)
                Debug.LogError($"[PlatformBase] The 'Platform Visuals' field is not set on '{gameObject.name}'.", this);
            if (!platformCollider.isTrigger)
                Debug.LogWarning($"[PlatformBase] Collider on '{gameObject.name}' is not a Trigger. Player interaction might not work as expected.", this);
        }

        /// <summary>
        /// Detects player entering the trigger zone.
        /// </summary>
        protected virtual void OnTriggerEnter(Collider other) {
            if(enableDebugLogs) Debug.Log($"[PlatformBase.OnTriggerEnter] Trigger entered by '{other.gameObject.name}'. Checking conditions...", this);

            if (isDeactivated) {
                if(enableDebugLogs) Debug.LogWarning($"[PlatformBase.OnTriggerEnter] Platform is deactivated. Ignoring trigger.", this);
                return;
            }
            if (!other.CompareTag("Player")) {
                if(enableDebugLogs) Debug.Log($"[PlatformBase.OnTriggerEnter] Object tag is '{other.tag}', not 'Player'. Ignoring.", this);
                return;
            }
            
            if(enableDebugLogs) Debug.Log($"[PlatformBase.OnTriggerEnter] Player detected on '{gameObject.name}'. Calling ActivatePlatform().", this);
            ActivatePlatform();
        }

        /// <summary>
        /// The core logic of the platform. Must be implemented by each subclass.
        /// </summary>
        protected abstract void ActivatePlatform();
        
        /// <summary>
        /// Etapa 1 do Respawn: Reseta o estado da plataforma de forma invisível.
        /// </summary>
        protected void PrepareForRespawn() {
            if(enableDebugLogs) Debug.Log($"[PlatformBase.PrepareForRespawn] Starting invisible respawn prep for '{gameObject.name}'.", this);

            if (TryGetComponent<Rigidbody>(out var rb)) {
                rb.isKinematic = true;
                rb.useGravity = false;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            if (platformVisuals != null) {
                if(enableDebugLogs) Debug.Log($"[PlatformBase.PrepareForRespawn] Disabling visuals.", this);
                platformVisuals.enabled = false;
            }
            if(enableDebugLogs) Debug.Log($"[PlatformBase.PrepareForRespawn] Disabling platform collider.", this);
            platformCollider.enabled = false;

            if(enableDebugLogs) Debug.Log($"[PlatformBase.PrepareForRespawn] Resetting transform to initial state.", this);
            transform.position = initialPosition;
            transform.rotation = initialRotation;
            transform.localScale = initialScale;

            if(enableDebugLogs) Debug.Log($"[PlatformBase.PrepareForRespawn] Preparation complete. Calling ReactivatePlatform().", this);
            ReactivatePlatform();
        }
        
        /// <summary>
        /// Etapa 2 do Respawn: Reativa a plataforma, tornando-a visível e interativa.
        /// </summary>
        protected virtual void ReactivatePlatform() {
            if(enableDebugLogs) Debug.Log($"[PlatformBase.ReactivatePlatform] Starting visible reactivation for '{gameObject.name}'.", this);

            if(enableDebugLogs) Debug.Log($"[PlatformBase.ReactivatePlatform] Enabling collider and setting IsTrigger=true.", this);
            platformCollider.enabled = true;
            platformCollider.isTrigger = true;

            if(enableDebugLogs) Debug.Log($"[PlatformBase.ReactivatePlatform] Resetting 'isDeactivated' flag to false.", this);
            isDeactivated = false;

            if (platformVisuals != null) {
                if(enableDebugLogs) Debug.Log($"[PlatformBase.ReactivatePlatform] Enabling visuals.", this);
                platformVisuals.enabled = true;
            }

            if (animateRespawn) {
                if(enableDebugLogs) Debug.Log($"[PlatformBase.ReactivatePlatform] Playing respawn animation.", this);
                transform.localScale = Vector3.zero;
                transform.DOScale(initialScale, respawnAnimationDuration).SetEase(DG.Tweening.Ease.OutBack);
            } else {
                if(enableDebugLogs) Debug.Log($"[PlatformBase.ReactivatePlatform] Setting scale directly (no animation).", this);
                transform.localScale = initialScale;
            }
        }
    }
}