using UnityEngine;

namespace Enemies {
    /// <summary>
    /// Manages all audio playback for the boss.
    /// It holds references to AudioSources and Clips,
    /// providing simple public methods for the state machine to call.
    /// </summary>
    public class BossAudio : MonoBehaviour {
        [Header("Audio Sources")]
        [Tooltip("AudioSource for one-shot effects like attacks, grunts, and impacts.")]
        [SerializeField] private AudioSource sfxSource;
        [Tooltip("AudioSource for looped effects like the stun sound.")]
        [SerializeField] private AudioSource statusSource;
        
        [Header("Movement SFX")] 
        [Tooltip("Sounds for the boss's footsteps. Will pick one at random.")]
        [SerializeField] private AudioClip[] footstepClips;

        [Header("Attack SFX")]
        [Tooltip("The 'whoosh' or 'grunt' when the boss starts a punch.")]
        [SerializeField] private AudioClip punchAttackClip;
        [Tooltip("The 'whoosh' or 'grunt' when the boss starts a spin.")]
        [SerializeField] private AudioClip spinAttackClip;
        [Tooltip("Sound to play on a successful punch hit.")]
        [SerializeField] private AudioClip punchImpactClip;
        [Tooltip("Sound to play on a successful spin attack hit.")]
        [SerializeField] private AudioClip spinImpactClip;
        
        [Header("State SFX")]
        [Tooltip("Sound to play when the boss enters the Taunt state.")]
        [SerializeField] private AudioClip tauntClip;
        [Tooltip("Sound to play when the boss is successfully hit while vulnerable.")]
        [SerializeField] private AudioClip vulnerableHitClip;
        [Tooltip("Looping sound to play while the boss is stunned.")]
        [SerializeField] private AudioClip stunLoopClip;
        [Tooltip("Sound to play when the boss starts getting up from stun.")]
        [SerializeField] private AudioClip getUpClip;
        [Tooltip("Sound to play when the boss is defeated.")]
        [SerializeField] private AudioClip defeatedClip;
        /// <summary>
        /// Plays a random footstep sound from the array.
        /// </summary>
        public void PlayFootstep() {
            if (footstepClips == null || footstepClips.Length == 0) return;
            // Pick a random clip from the array
            AudioClip clip = footstepClips[Random.Range(0, footstepClips.Length)];
            PlayOneShot(clip);
        }
        /// <summary>
        /// Plays the punch attack 'whoosh' sound.
        /// </summary>
        public void PlayPunchAttack() {
            PlayOneShot(punchAttackClip);
        }
        /// <summary>
        /// Plays the spin attack 'whoosh' sound.
        /// </summary>
        public void PlaySpinAttack() {
            PlayOneShot(spinAttackClip);
        }
        
        /// <summary>
        /// Plays the punch impact sound effect.
        /// </summary>
        public void PlayPunchImpact() {
            PlayOneShot(punchImpactClip);
        }

        /// <summary>
        /// Plays the spin impact sound effect.
        /// </summary>
        public void PlaySpinImpact() {
            PlayOneShot(spinImpactClip);
        }

        /// <summary>
        /// Plays the taunt sound effect.
        /// </summary>
        public void PlayTaunt() {
            PlayOneShot(tauntClip);
        }

        /// <summary>
        /// Plays the sound for being hit while vulnerable.
        /// </summary>
        public void PlayVulnerableHit() {
            PlayOneShot(vulnerableHitClip);
        }
        
        /// <summary>
        /// Plays the get up sound effect.
        /// </summary>
        public void PlayGetUp() {
            PlayOneShot(getUpClip);
        }

        /// <summary>
        /// Plays the boss defeated sound effect.
        /// </summary>
        public void PlayDefeated() {
            PlayOneShot(defeatedClip);
        }

        /// <summary>
        /// Plays the looping stun sound.
        /// </summary>
        public void PlayStunLoop() {
            PlayLoop(stunLoopClip);
        }

        /// <summary>
        /// Stops any currently playing looped sound.
        /// </summary>
        public void StopStatusLoop() {
            if (!statusSource) return;
            statusSource.Stop();
            statusSource.loop = false;
        }
        
        /// <summary>
        /// Stops all audio from this boss.
        /// </summary>
        public void StopAllAudio() {
            if (sfxSource) sfxSource.Stop();
            if (statusSource) statusSource.Stop();
        }

        /// <summary>
        /// Helper to safely play a one-shot clip on the SFX source.
        /// </summary>
        private void PlayOneShot(AudioClip clip) {
            if (sfxSource && clip) {
                // Use sfxSource.PlayOneShot(clip) para permitir sobreposição de sons
                sfxSource.PlayOneShot(clip); 
            }
        }

        /// <summary>
        /// Helper to safely play a looping clip on the status source.
        /// </summary>
        private void PlayLoop(AudioClip clip) {
            if (!statusSource || !clip) return;
            statusSource.clip = clip;
            statusSource.loop = true;
            statusSource.Play();
        }
    }
}