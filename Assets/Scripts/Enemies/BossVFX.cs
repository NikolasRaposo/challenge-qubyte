using System.Collections;
using UnityEngine;

namespace Enemies {
    /// <summary>
    /// Manages all Visual Effects (VFX) for the boss.
    /// Handles instantiation of impact prefabs, activation of persistent effects (like stun),
    /// and material flashing for vulnerability.
    /// </summary>
    public class BossVFX : MonoBehaviour {
        [Header("Impact Prefabs")]
        [Tooltip("VFX prefab to instantiate on punch impact.")]
        [SerializeField] private GameObject punchImpactPrefab;
        [Tooltip("VFX prefab to instantiate on spin impact.")]
        [SerializeField] private GameObject spinImpactPrefab;

        [Header("Attached Effects")]
        [Tooltip("A child GameObject to activate/deactivate for the stun effect (e.g., 'stars').")]
        [SerializeField] private GameObject stunEffectObject;

        [Header("Material Flash")]
        [Tooltip("An array of all renderers on the boss model.")]
        [SerializeField] private Renderer[] bossRenderers;
        [Tooltip("The color to flash when vulnerable.")]
        [SerializeField] private Color vulnerableColor = Color.yellow;
        [Tooltip("The speed of the vulnerable flash (e.g., 0.3s).")]
        [SerializeField] private float vulnerableFlashRate = 0.3f;

        // Property ID for emission color shader property
        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

        private Coroutine _vulnerableFlashCoroutine;
        private Coroutine _invulnerableFlashCoroutine;
        
        /// <summary>
        /// Caches and deactivates persistent effects on start.
        /// </summary>
        private void Awake() {
            if (stunEffectObject != null) {
                stunEffectObject.SetActive(false);
            }
        }
        
        /// <summary>
        /// Instantiates the punch impact VFX at the specified location.
        /// </summary>
        public void PlayPunchImpact(Vector3 position, Quaternion rotation) {
            if (punchImpactPrefab != null) {
                Instantiate(punchImpactPrefab, position, rotation);
            }
        }

        /// <summary>
        /// Instantiates the spin impact VFX at the specified location.
        /// </summary>
        public void PlaySpinImpact(Vector3 position, Quaternion rotation) {
            if (spinImpactPrefab != null) {
                Instantiate(spinImpactPrefab, position, rotation);
            }
        }

        /// <summary>
        /// Shows the persistent stun effect (e.g., 'stars' on head).
        /// </summary>
        public void ShowStunEffect() {
            if (stunEffectObject != null) {
                stunEffectObject.SetActive(true);
            }
        }

        /// <summary>
        /// Hides the persistent stun effect.
        /// </summary>
        public void HideStunEffect() {
            if (stunEffectObject != null) {
                stunEffectObject.SetActive(false);
            }
        }

        /// <summary>
        /// Starts the flashing 'vulnerable' loop.
        /// </summary>
        public void StartVulnerableLoop() {
            StopAllFlashes(); // Stop any other flash first
            _vulnerableFlashCoroutine = StartCoroutine(VulnerableFlashLoop());
        }

        /// <summary>
        /// Plays a single 'invulnerable' flash.
        /// </summary>
        public void StartInvulnerableFlash() {
            // Only flash if not already flashing invulnerable
            if (_invulnerableFlashCoroutine == null) {
                _invulnerableFlashCoroutine = StartCoroutine(InvulnerableFlashRoutine());
            }
        }

        /// <summary>
        /// Stops all material flashing coroutines and resets the material.
        /// </summary>
        public void StopAllFlashes() {
            if (_vulnerableFlashCoroutine != null) {
                StopCoroutine(_vulnerableFlashCoroutine);
                _vulnerableFlashCoroutine = null;
            }
            if (_invulnerableFlashCoroutine != null) {
                StopCoroutine(_invulnerableFlashCoroutine);
                _invulnerableFlashCoroutine = null;
            }
            ResetFlashMaterial();
        }

        /// <summary>
        /// Stops all running VFX and coroutines.
        /// </summary>
        public void StopAllVFX() {
            HideStunEffect();
            StopAllFlashes();
        }

        // --- Coroutines & Helpers ---
        
        private IEnumerator VulnerableFlashLoop() {
            while (true) {
                foreach (var r in bossRenderers) {
                    r.material.EnableKeyword("_EMISSION");
                    r.material.SetColor(EmissionColor, vulnerableColor);
                }
                yield return new WaitForSeconds(vulnerableFlashRate);
                ResetFlashMaterial();
                yield return new WaitForSeconds(vulnerableFlashRate);
            }
        }
        
        private IEnumerator InvulnerableFlashRoutine() {
            foreach (var r in bossRenderers) {
                r.material.EnableKeyword("_EMISSION");
                r.material.SetColor(EmissionColor, Color.white);
            }
            yield return new WaitForSeconds(0.1f);
            ResetFlashMaterial();
            _invulnerableFlashCoroutine = null;
        }

        private void ResetFlashMaterial() {
            foreach (var r in bossRenderers) {
                r.material.SetColor(EmissionColor, Color.black);
            }
        }
    }
}