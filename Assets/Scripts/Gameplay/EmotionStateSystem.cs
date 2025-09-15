using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
namespace Gameplay {
    /// <summary>
    /// Manages the emotional state of a character, triggering various feedback mechanisms like animations,
    /// particles, sounds, and physical reactions based on the current emotion.
    /// </summary>
    public class EmotionStateSystem : MonoBehaviour {
        private static readonly int ResetEmotion = Animator.StringToHash("ResetEmotion");
        private static readonly int Happy = Animator.StringToHash("Happy");
        private static readonly int Angry = Animator.StringToHash("Angry");
        private static readonly int Sad = Animator.StringToHash("Sad");
        private static readonly int Super = Animator.StringToHash("Super");
        /// <summary>
        /// Defines the possible emotional states for the character.
        /// </summary>
        public enum EmotionState { Neutral, Happy, Angry, Sad, Super }

        [Header("General Settings")]
        [Tooltip("The character's current emotional state.")]
        public EmotionState currentState = EmotionState.Neutral;
        [Tooltip("Time in seconds before the emotion resets to Neutral.")]
        public float timeToReset = 5f;

        [Header("System References")]
        [Tooltip("The character's Animator component.")]
        public Animator animator;
        [Tooltip("The AudioSource for playing emotional sounds.")]
        public AudioSource audioSource;
        [Tooltip("The character's head bone transform for physical expressions and look-at logic.")]
        public Transform headBone;
        [Tooltip("Reference to the main camera, used for the head to look forward.")]
        public Transform cameraTransform;

        [Header("Audio Clips per Emotion")]
        public AudioClip happyClip;
        public AudioClip angryClip;
        public AudioClip sadClip;
        public AudioClip superClip;

        [Header("Particle Systems per Emotion")]
        public ParticleSystem happyParticles;
        public ParticleSystem angryParticles;
        public ParticleSystem sadParticles;
        public ParticleSystem superParticles;

        [Header("Directed Look (When applicable)")]
        [Tooltip("If true, the character's head will turn to look at an important object.")]
        public bool useCuriousLook = true;
        [Tooltip("The object of interest for the character to look at.")]
        public Transform importantObject;
        [Tooltip("The duration of the look-at animation.")]
        public float lookDuration = 2f;

        // --- Private State Variables ---
        private Coroutine _resetCoroutine;
        private Coroutine _lookCoroutine;
        // An event that other scripts can subscribe to, to be notified of emotion changes.

        /// <summary>
        /// Initializes the system with the starting emotion's feedback.
        /// </summary>
        private void Start() {
            ApplyAllFeedback();
        }

        /// <summary>
        /// Sets the character's emotional state and triggers all associated feedback.
        /// </summary>
        /// <param name="newState">The new emotional state to set.</param>
        private void SetState(EmotionState newState) {
            // Do nothing if the state is already the same.
            if (currentState == newState) return;
            currentState = newState;
            ApplyAllFeedback();
            // Stop any existing reset timer.
            if (_resetCoroutine != null) StopCoroutine(_resetCoroutine);
            // Start a new timer to reset the state to Neutral (unless it's already Neutral).
            if (newState != EmotionState.Neutral) {
                _resetCoroutine = StartCoroutine(ResetStateAfterDelay());
            }
        }

        /// <summary>
        /// A central method to trigger all feedback types for the current state.
        /// </summary>
        private void ApplyAllFeedback() {
            PlayAudio();
            ActivateParticles();
            ActivateAnimationLayer();
            DirectLook();
            ExpressPhysically();
        }
        /// <summary>
        /// Plays the appropriate sound clip for the current emotion.
        /// </summary>
        private void PlayAudio() {
            if (!audioSource) return;
            AudioClip clip = null;
            switch (currentState) {
                case EmotionState.Happy: clip = happyClip; break;
                case EmotionState.Angry: clip = angryClip; break;
                case EmotionState.Sad:   clip = sadClip;   break;
                case EmotionState.Super: clip = superClip; break;
                case EmotionState.Neutral:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            if (clip) audioSource.PlayOneShot(clip);
        }

        /// <summary>
        /// Activates the appropriate particle system for the current emotion.
        /// </summary>
        private void ActivateParticles() {
            // Stop all particle systems first to prevent overlap.
            happyParticles?.Stop();
            angryParticles?.Stop();
            sadParticles?.Stop();
            superParticles?.Stop();
            // Play the correct one.
            switch (currentState) {
                case EmotionState.Happy: happyParticles?.Play(); break;
                case EmotionState.Angry: angryParticles?.Play(); break;
                case EmotionState.Sad:   sadParticles?.Play();   break;
                case EmotionState.Super: superParticles?.Play(); break;
                case EmotionState.Neutral:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Triggers the appropriate animation state in the Animator.
        /// </summary>
        private void ActivateAnimationLayer() {
            if (!animator) return;

            // Use triggers to activate specific animation states.
            // A "Reset" trigger can be used to return to a neutral animation state.
            animator.SetTrigger(ResetEmotion);

            switch (currentState) {
                case EmotionState.Happy: animator.SetTrigger(Happy); break;
                case EmotionState.Angry: animator.SetTrigger(Angry); break;
                case EmotionState.Sad:   animator.SetTrigger(Sad);   break;
                case EmotionState.Super: animator.SetTrigger(Super); break;
                case EmotionState.Neutral:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Initiates the look-at behavior if applicable.
        /// </summary>
        private void DirectLook() {
            if (!useCuriousLook || !importantObject || !headBone) return;
            if (_lookCoroutine != null) StopCoroutine(_lookCoroutine);
            _lookCoroutine = StartCoroutine(LookAtObject());
        }

        /// <summary>
        /// Coroutine to handle the head turning towards an object and then back.
        /// </summary>
        private IEnumerator LookAtObject() {
            // Calculate the target rotation to look at the object.
            Quaternion targetRotation = Quaternion.LookRotation(importantObject.position - headBone.position);
            headBone.DORotateQuaternion(targetRotation, lookDuration).SetEase(Ease.InOutSine);
            yield return new WaitForSeconds(lookDuration + 1f);
            // After a delay, rotate back to look forward (aligned with the camera).
            if (!cameraTransform) yield break;
            Quaternion returnRotation = Quaternion.LookRotation(cameraTransform.forward);
            headBone.DORotateQuaternion(returnRotation, lookDuration).SetEase(Ease.InOutSine);
        }

        /// <summary>
        /// Triggers small physical animations (head tilts, shakes) for the current emotion.
        /// </summary>
        private void ExpressPhysically() {
            if (!headBone) return;
            // Kill any previous physical expression tweens to avoid conflicts.
            DOTween.Kill(headBone);
            switch (currentState) {
                case EmotionState.Happy:
                    // A happy head tilt.
                    headBone.DOLocalRotate(new Vector3(0, 0, 10), 0.1f)
                        .SetLoops(4, LoopType.Yoyo)
                        .SetEase(Ease.InOutSine);
                    break;
                case EmotionState.Angry:
                    // An angry head shake.
                    headBone.DOShakeRotation(0.4f, new Vector3(0, 20, 0))
                        .SetEase(Ease.OutBounce);
                    break;
                case EmotionState.Sad:
                    // A sad, drooping head.
                    headBone.DOLocalRotate(new Vector3(15, 0, 0), 0.2f)
                        .SetEase(Ease.OutCubic);
                    break;
                case EmotionState.Super:
                    // An excited, energetic shake.
                    headBone.DOShakeRotation(0.6f, new Vector3(15, 30, 15), 20)
                        .SetEase(Ease.InOutElastic);
                    break;
                case EmotionState.Neutral:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        /// <summary>
        /// Coroutine that waits for a set time before resetting the emotion to Neutral.
        /// </summary>
        private IEnumerator ResetStateAfterDelay() {
            yield return new WaitForSeconds(timeToReset);
            SetState(EmotionState.Neutral);
        }
    }
}