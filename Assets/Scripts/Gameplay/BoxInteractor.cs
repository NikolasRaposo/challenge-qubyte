using System.Collections;
using DG.Tweening;
using UnityEngine;
namespace Gameplay {
    /// <summary>
    /// Controls the behavior of interactive boxes in the game.
    /// Allows for various interactions like breaking, dropping items, acting as a trampoline, exploding, and more.
    /// </summary>
    public class BoxInteractor : MonoBehaviour {
        [Header("General Settings")]
        [Tooltip("Can the box be triggered by the player jumping on it from above?")]
        public bool canInteractOnJump = true;
        [Tooltip("If true, the box can only be interacted with once.")]
        public bool interactOnce = true;
    
        [Header("Box Actions")]
        [Tooltip("If true, the box will visually break and become disabled.")]
        public bool breakOnInteract = true;
        [Tooltip("If true, the box will disappear. Overridden by 'breakOnInteract'.")]
        public bool disappearOnInteract;
        [Tooltip("If true, the box will reappear after a set time.")]
        public bool respawnAfterTime;
        [Tooltip("The time in seconds it takes for the box to respawn.")]
        public float respawnTime = 3f;

        [Header("Item Spawning")]
        [Tooltip("If true, the box will spawn an item upon interaction.")]
        public bool spawnItem;
        [Tooltip("The prefab of the item to spawn.")]
        public GameObject itemPrefab;
        [Tooltip("The specific point where the item will be spawned. If null, uses the box's position.")]
        public Transform spawnPoint;
        [Tooltip("Settings that define how the spawned items behave (e.g., quantity, spread pattern).")]
        public ItemEffectSettings itemEffectSettings = new ItemEffectSettings();
    
        [Header("Explosion Effect")]
        [Tooltip("If true, the box will create an explosion effect.")]
        public bool explodeOnBreak;
        [Tooltip("The force of the explosion applied to fragments.")]
        public float explosionForce = 300f;
        [Tooltip("A prefab consisting of broken pieces of the box to instantiate on explosion.")]
        public GameObject fragmentsPrefab;

        [Header("Trampoline")]
        [Tooltip("If true, the box will act as a trampoline, launching the player upwards.")]
        public bool isTrampoline;
        [Tooltip("The upward force applied to the player.")]
        public float trampolineForce = 10f;
    
        [Header("Visual Feedback")]
        [Tooltip("If true, the box will shake when interacted with.")]
        public bool useVisualFeedback = true;
        [Tooltip("How much the box shakes (positional).")]
        public float shakeIntensity = 0.05f;
        [Tooltip("How long the shake animation lasts.")]
        public float shakeDuration = 0.3f;

        // --- Private State Variables ---
        private bool _hasBeenInteracted;
        private Vector3 _originalScale;
        private Renderer _boxRenderer;
        private Collider _boxCollider;

        /// <summary>
        /// Initializes components and stores original values.
        /// </summary>
        private void Start() {
            _originalScale = transform.localScale;
            _boxRenderer = GetComponent<Renderer>();
            _boxCollider = GetComponent<Collider>();
        }

        /// <summary>
        /// The main method to process an interaction with the box.
        /// </summary>
        /// <param name="interactor">The Transform of the object that interacted with the box (e.g., the player).</param>
        public void Interact(Transform interactor) {
            // If the box is single-use and has already been used, do nothing.
            if (interactOnce && _hasBeenInteracted) return;
            _hasBeenInteracted = true;

            // Play visual feedback if enabled.
            if (useVisualFeedback)
                PlayShakeFeedback();

            // Spawn items if configured.
            if (spawnItem && itemPrefab != null)
                SpawnItemsWithEffect();

            // Apply trampoline effect if configured.
            if (isTrampoline && interactor != null)
                ApplyTrampolineEffect(interactor);

            // Handle the box's destruction or disappearance.
            if (explodeOnBreak)
                Explode();
            else if (breakOnInteract)
                StartCoroutine(Break());
            else if (disappearOnInteract)
                StartCoroutine(Disappear());
        }

        /// <summary>
        /// Plays a shake animation on the box for visual feedback.
        /// </summary>
        private void PlayShakeFeedback() {
            // Use DOTween to create a quick shake effect.
            transform.DOShakePosition(shakeDuration, shakeIntensity);
            transform.DOShakeRotation(shakeDuration, new Vector3(5f, 5f, 5f));
        }

        /// <summary>
        /// Spawns items using the ItemEffectController.
        /// </summary>
        private void SpawnItemsWithEffect() {
            // Determine the spawn position.
            Vector3 spawnPosition = (spawnPoint != null) ? spawnPoint.position : transform.position + Vector3.up;

            // Create a temporary GameObject to host the ItemEffectController.
            GameObject effectControllerObject = new GameObject("ItemEffectController_Temp") {
                transform = {
                    position = spawnPosition,
                },
            };

            // Add and configure the controller.
            ItemEffectController controller = effectControllerObject.AddComponent<ItemEffectController>();
            controller.itemPrefab = this.itemPrefab;
            controller.settings = this.itemEffectSettings;
        
            // Start the item creation process.
            controller.CreateItems();

            // Clean up the temporary controller object after a few seconds.
            Destroy(effectControllerObject, 5f);
        }
    
        /// <summary>
        /// Applies an upward force to the target, creating a trampoline effect.
        /// </summary>
        /// <param name="target">The Transform of the object to launch.</param>
        private void ApplyTrampolineEffect(Transform target) {
            // Try to get the player controller component to apply a controlled force.
            if (target.TryGetComponent(out ThirdPersonController player)) {
                player.ApplyUpwardForce(trampolineForce);
            }
            // As a fallback, try to apply force to a Rigidbody.
            else if (target.TryGetComponent(out Rigidbody rb)) {
                // Reset vertical velocity to ensure a consistent jump height.
                rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
                rb.AddForce(Vector3.up * trampolineForce, ForceMode.VelocityChange);
            }
        }

        /// <summary>
        /// Handles the explosion effect.
        /// </summary>
        private void Explode() {
            // Instantiate the fragments prefab if it's assigned.
            if (fragmentsPrefab != null) {
                GameObject fragments = Instantiate(fragmentsPrefab, transform.position, transform.rotation);
                // Apply an explosion force to all Rigidbody components in the fragments.
                foreach (Rigidbody rb in fragments.GetComponentsInChildren<Rigidbody>()) {
                    rb.AddExplosionForce(explosionForce, transform.position, 2f);
                }
                // Destroy the fragments after a delay.
                Destroy(fragments, 5f);
            }

            // Immediately disable the original box.
            _boxRenderer.enabled = false;
            _boxCollider.enabled = false;

            // Schedule a respawn if configured.
            if (respawnAfterTime)
                Invoke(nameof(Respawn), respawnTime);
            else
                Destroy(gameObject, 0.5f); // Destroy the original box object.
        }

        /// <summary>
        /// Coroutine to handle breaking the box after a short delay.
        /// </summary>
        private IEnumerator Break() {
            yield return new WaitForSeconds(0.1f); // Short delay to allow other effects to start.
        
            _boxRenderer.enabled = false;
            _boxCollider.enabled = false;

            if (respawnAfterTime)
                Invoke(nameof(Respawn), respawnTime);
            else
                Destroy(gameObject, 0.5f);
        }

        /// <summary>
        /// Coroutine to handle making the box disappear.
        /// </summary>
        private IEnumerator Disappear() {
            yield return new WaitForSeconds(0.1f);
        
            _boxRenderer.enabled = false;
            _boxCollider.enabled = false;

            if (respawnAfterTime)
                Invoke(nameof(Respawn), respawnTime);
            else
                Destroy(gameObject, 0.5f);
        }

        /// <summary>
        /// Respawns the box, resetting its state and playing an animation.
        /// </summary>
        private void Respawn() {
            // Reset state.
            _hasBeenInteracted = false;
            _boxRenderer.enabled = true;
            _boxCollider.enabled = true;
            transform.localScale = Vector3.zero; // Start from zero scale for the animation.

            // Animate the respawn using DOTween.
            DOTween.Sequence()
                .Append(transform.DOScale(_originalScale, 0.5f).SetEase(Ease.OutBack))
                .Join(transform.DOShakePosition(0.3f, 0.05f));
        }
    }
}