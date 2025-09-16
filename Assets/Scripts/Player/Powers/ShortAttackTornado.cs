using System.Collections;
using UnityEngine;
using Managers;

namespace Player.Powers
{
    /// <summary>
    /// Manages the player's short-range tornado attack.
    /// It triggers an animation and activates a local effect object.
    /// </summary>
    public class ShortAttackTornado : MonoBehaviour
    {
        // Caches the "Attack" animator parameter's hash for performance.
        private static readonly int Attack = Animator.StringToHash("Attack");

        [Header("References")]
        [Tooltip("The character's Animator component. Can be assigned here or will be found automatically.")]
        public Animator animator;
        [Tooltip("Drag the child GameObject that contains the tornado effect here.")]
        public GameObject tornadoEffectObject;

        [Header("Attack Settings")]
        [Tooltip("Duration in seconds that the tornado effect will remain active.")]
        public float effectDuration = 2.0f;
        [Tooltip("The minimum time in seconds between each attack.")]
        public float cooldown = 1.5f;
        [Tooltip("The TornadoTrigger script that is on the tornado effect object.")]
        public TornadoTrigger tornadoTrigger;

        // Tracks the time of the last attack to manage the cooldown.
        private float _lastAttackTime = -Mathf.Infinity;

        /// <summary>
        /// Called when the script instance is being loaded.
        /// </summary>
        private void Awake()
        {
            // If the Animator was not assigned in the Inspector, try to get it from this same GameObject.
            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }

            if (animator == null)
            {
                Debug.LogError("Animator component NOT FOUND! The attack will not work.", gameObject);
            }
        }

        /// <summary>
        /// Called on the frame when a script is enabled just before any of the Update methods are called the first time.
        /// </summary>
        private void Start()
        {
            if (InputManager.Instance != null)
            {
                // Subscribe to the input event.
                InputManager.Instance.OnTornado += HandleAttackInput;
            }
            else
            {
                Debug.LogError("InputManager.Instance not found! The attack input will not work.");
            }

            if (tornadoEffectObject != null)
            {
                // Ensures the effect object is disabled at the start.
                tornadoEffectObject.SetActive(false);
            }
        }

        /// <summary>
        /// Called when the corresponding input action is triggered.
        /// </summary>
        private void HandleAttackInput()
        {
            // Check if the attack is on cooldown.
            if (Time.time < _lastAttackTime + cooldown)
            {
                return; // On cooldown
            }

            PerformAttack();

            // Record the time of this attack.
            _lastAttackTime = Time.time;
        }

        /// <summary>
        /// Triggers the attack animation and the visual effect.
        /// </summary>
        private void PerformAttack()
        {
            if (animator == null || tornadoEffectObject == null || tornadoTrigger == null) return;

            animator.SetTrigger(Attack);

            StartCoroutine(TornadoEffectCoroutine());
        }

        /// <summary>
        /// A coroutine that activates the tornado effect for a set duration.
        /// </summary>
        private IEnumerator TornadoEffectCoroutine()
        {
            // Clears the list of targets from the previous attack to allow hitting them again.
            tornadoTrigger.ResetHitTargets();

            // Activate the child effect object.
            tornadoEffectObject.SetActive(true);

            // Wait for the effect's duration.
            yield return new WaitForSeconds(effectDuration);

            // Deactivate the child effect object.
            tornadoEffectObject.SetActive(false);
        }

        /// <summary>
        /// Called when the object is destroyed. Unsubscribes from events to prevent memory leaks.
        /// </summary>
        private void OnDestroy()
        {
            if (InputManager.Instance != null)
            {
                InputManager.Instance.OnTornado -= HandleAttackInput;
            }
        }
    }
}