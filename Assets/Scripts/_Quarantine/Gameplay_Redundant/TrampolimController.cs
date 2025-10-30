using System.Collections;
using DG.Tweening;
using Player;
using UnityEngine;
namespace Gameplay {
    /// <summary>
    /// Controls the behavior of trampolines in the game. Allows configuration of launch force,
    /// usage limits, and visual/audio feedback. Can be used standalone or with other components like BoxInteractor.
    /// </summary>
    [AddComponentMenu("Gameplay/Trampoline Controller")]
    public class TrampolineController : MonoBehaviour {
        [Header("Force Settings")]
        [Tooltip("The upward force applied to the player when using the trampoline.")]
        [Range(5f, 30f)]
        public float launchForce = 10f;

        [Tooltip("Multiplier for horizontal velocity when using the trampoline (1 = maintain current speed).")]
        [Range(0.5f, 2f)]
        public float horizontalVelocityMultiplier = 1f;

        [Header("Usage Settings")]
        [Tooltip("If checked, the trampoline can only be used once.")]
        public bool singleUse;

        [Tooltip("If checked, the trampoline needs time to recharge between uses.")]
        public bool hasCooldown;

        [Tooltip("Time in seconds for the trampoline to recharge after use.")]
        [Range(0.5f, 10f)]
        public float cooldownTime = 2f;

        [Header("Visual Feedback")]
        [Tooltip("If checked, the trampoline will have a visual animation when used.")]
        public bool useVisualAnimation = true;

        [Tooltip("The maximum scale during the compression animation.")]
        public Vector3 compressionScale = new Vector3(1.2f, 0.5f, 1.2f);

        [Tooltip("The maximum scale during the extension animation.")]
        public Vector3 extensionScale = new Vector3(0.8f, 1.5f, 0.8f);

        [Tooltip("The duration of the complete animation in seconds.")]
        [Range(0.1f, 1f)]
        public float animationDuration = 0.3f;

        [Header("Sound Feedback")]
        [Tooltip("If checked, the trampoline will play a sound when used.")]
        public bool useSound = true;

        [Tooltip("The sound played when the trampoline is activated.")]
        public AudioClip trampolineSound;

        [Range(0f, 1f)]
        public float soundVolume = 0.7f;

        [Header("Advanced Settings")]
        [Tooltip("Layers that can interact with the trampoline.")]
        public LayerMask interactiveLayers;

        [Tooltip("The launch angle in degrees (0 = straight up).")]
        [Range(-45f, 45f)]
        public float launchAngle;

        [Tooltip("If checked, the trampoline only activates when an object is falling onto it.")]
        public bool onlyWhenFalling = true;

        // Private state variables
        private Vector3 _originalScale;
        private bool _isTrampolineActive = true;
        private Renderer _objectRenderer;
        private Color _originalColor;
        private bool _hasRenderer;

        /// <summary>
        /// Initializes the trampoline, storing original values for animations.
        /// </summary>
        private void Start() {
            _originalScale = transform.localScale;
            _hasRenderer = TryGetComponent(out _objectRenderer);
            if (_hasRenderer) {
                _originalColor = _objectRenderer.material.color;
            }
            if (GetComponent<Collider>() == null) {
                Debug.LogWarning("Trampoline has no collider! Add a collider for it to function correctly.", this);
            }
        }

        /// <summary>
        /// Detects collision with other objects to trigger the trampoline effect.
        /// </summary>
        private void OnCollisionEnter(Collision collision) {
            // Check if the trampoline is active.
            if (!_isTrampolineActive) return;
            // Check if the colliding object's layer is in our interactive layers mask.
            if ((interactiveLayers.value & (1 << collision.gameObject.layer)) == 0) return;
            // If 'onlyWhenFalling' is enabled, perform additional checks.
            if (onlyWhenFalling) {
                // Check if the collision came from above. The contact normal should point upwards.
                if (collision.GetContact(0).normal.y < 0.7f) return;
            }
            ApplyTrampolineEffect(collision.transform);
            // Manage the trampoline's state after use.
            if (singleUse) {
                _isTrampolineActive = false;
            } else if (hasCooldown) {
                StartCoroutine(RechargeTrampoline());
            }
        }

        /// <summary>
        /// Applies the trampoline effect to the colliding object.
        /// </summary>
        /// <param name="targetObject">The transform of the object that hit the trampoline.</param>
        private void ApplyTrampolineEffect(Transform targetObject) {
            // Try to get a player controller or a rigidbody to apply force.
            if (targetObject.TryGetComponent(out ThirdPersonController player)) {
                // Use the player controller's specific method for applying upward force.
                player.ApplyUpwardForce(launchForce);
            } else if (targetObject.TryGetComponent(out Rigidbody rb)) {
                // Preserve and multiply horizontal velocity.
                Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z) * horizontalVelocityMultiplier;
                // Calculate the launch direction based on the angle.
                Vector3 launchDirection = Quaternion.Euler(-launchAngle, 0, 0) * Vector3.up;
                // Apply the force as an impulse.
                rb.linearVelocity = horizontalVelocity;
                rb.AddForce(launchDirection * launchForce, ForceMode.Impulse);
            }
            // Trigger feedback effects.
            if (useVisualAnimation) PlayTrampolineAnimation();
            if (useSound && trampolineSound != null) AudioSource.PlayClipAtPoint(trampolineSound, transform.position, soundVolume);
        }

        /// <summary>
        /// Plays the visual squash-and-stretch animation using DOTween.
        /// </summary>
        private void PlayTrampolineAnimation() {
            // Use a sequence for multi-step animations.
            DOTween.Sequence()
                .Append(transform.DOScale(compressionScale, animationDuration * 0.3f).SetEase(Ease.OutQuad))
                .Append(transform.DOScale(extensionScale, animationDuration * 0.3f).SetEase(Ease.OutBack))
                .Append(transform.DOScale(_originalScale, animationDuration * 0.4f).SetEase(Ease.OutElastic));
        }

        /// <summary>
        /// Coroutine to handle the trampoline's recharge cooldown.
        /// </summary>
        private IEnumerator RechargeTrampoline() {
            _isTrampolineActive = false;
            // Visual feedback for deactivation (e.g., dim color).
            if (_hasRenderer) {
                _objectRenderer.material.DOColor(_originalColor * 0.5f, 0.3f);
            }
            yield return new WaitForSeconds(cooldownTime);
            _isTrampolineActive = true;
            // Visual feedback for reactivation (e.g., flash back to original color).
            if (_hasRenderer) {
                _objectRenderer.material.DOColor(_originalColor, 0.5f).SetEase(Ease.OutFlash, 2, 0);
            }
        }
    
        /// <summary>
        /// Public method to reset a single-use trampoline.
        /// </summary>
        public void ResetTrampoline() {
            _isTrampolineActive = true;
            if (_hasRenderer) {
                _objectRenderer.material.DOColor(_originalColor, 0.5f).SetEase(Ease.OutFlash, 2, 0);
            }
        }

        /// <summary>
        /// Draws gizmos in the editor to visualize the launch direction.
        /// </summary>
        private void OnDrawGizmosSelected() {
            Gizmos.color = Color.green;
            Vector3 direction = Quaternion.Euler(-launchAngle, 0, 0) * transform.up;
            Gizmos.DrawRay(transform.position, direction * 3);
            Gizmos.DrawWireSphere(transform.position + direction * 3, 0.2f);
        }
    }
}