using UnityEngine;
using DG.Tweening; // We'll use DOTween to maintain consistency with our other platforms

namespace Gameplay.Platform
{
    /// <summary>
    /// A platform that sinks when the player steps on it, and rises back after a delay.
    /// </summary>
    public class SinkingPlatform : PlatformBase {
        [Header("Sinking Settings")]
        [Tooltip("How far the platform sinks down.")]
        [SerializeField] private float sinkDistance = 1.0f;
        [Tooltip("How long the sinking animation takes.")]
        [SerializeField] private float sinkDuration = 1.5f;
        [Tooltip("How long the rising animation takes.")]
        [SerializeField] private float riseDuration = 1.0f;
        [Tooltip("Delay in seconds after being stepped on before the sinking starts.")]
        [SerializeField] private float activationDelay = 0.5f;
        [Tooltip("Delay in seconds after sinking before the platform starts to rise back up.")]
        [SerializeField] private float resetDelay = 2.0f;

        [Header("Sinking Particles")]
        [Tooltip("Particles that play while the platform is in its idle state.")]
        [SerializeField] private ParticleSystem idleParticles;
        [Tooltip("Particles that play as a warning before the platform starts to sink.")]
        [SerializeField] private ParticleSystem warningParticles;
        [Tooltip("Particles that play while the platform is sinking.")]
        [SerializeField] private ParticleSystem sinkParticles;

        /// <summary>
        /// Called when the script instance is being loaded.
        /// Plays the idle particles if they are assigned.
        /// </summary>
        private void Start() {
            if (idleParticles != null)
                idleParticles.Play();
        }

        /// <summary>
        /// Activates the full sink-and-rise sequence for the platform.
        /// This method is called by the base class when the player is detected.
        /// </summary>
        protected override void ActivatePlatform() {
            if (isDeactivated) return;

            isDeactivated = true;

            // Stop the idle particles and start the warning particles.
            if (idleParticles != null) idleParticles.Stop();
            if (warningParticles != null) warningParticles.Play();

            // We use a DOTween Sequence to create the complete animation.
            DOTween.Sequence()
                // 1. Wait for the activation delay.
                .AppendInterval(activationDelay)
                
                // 2. Play the sinking particles and move the platform downwards.
                .AppendCallback(() => {
                    if (warningParticles != null) warningParticles.Stop();
                    if (sinkParticles != null) sinkParticles.Play();
                })
                .Append(transform.DOMoveY(initialPosition.y - sinkDistance, sinkDuration).SetEase(Ease.InOutSine))
                
                // 3. Wait for the reset delay.
                .AppendInterval(resetDelay)
                
                // 4. Move the platform back to its initial position.
                .Append(transform.DOMoveY(initialPosition.y, riseDuration).SetEase(Ease.OutQuad))
                
                // 5. When the sequence is complete, reset the state.
                .OnComplete(() => {
                    if (sinkParticles != null) sinkParticles.Stop();
                    if (idleParticles != null) idleParticles.Play();
                    isDeactivated = false;
                    if (enableDebugLogs) Debug.Log($"[SinkingPlatform] Sequence complete. Platform is active again.", this);
                });
        }
    }
}