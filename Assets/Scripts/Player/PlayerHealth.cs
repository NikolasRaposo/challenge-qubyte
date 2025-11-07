using System.Collections;
using Gameplay;
using Managers;
using UnityEngine;
using System.Collections.Generic;

namespace Player {
    /// <summary>
    /// Manages the player's health state, handling death and coordinating the respawn process.
    /// </summary>
    public class PlayerHealth : MonoBehaviour {
        [Header("Effects")]
        [Tooltip("Particle system to instantiate when the player dies.")]
        public GameObject deathVFX;

        [Tooltip("Sound to play when the player dies.")]
        public AudioClip deathSfx;
        
        [Tooltip("The main visual representation of the player that will be hidden upon death.")]
        public GameObject playerModel;
        
        private bool _isDead;
        public bool IsDead => _isDead;

        // Guarda informações de VFX persistentes que podem ser desparentados na morte
        private struct VfxAttachmentInfo
        {
            public Vfx_GraphController controller;
            public Transform originalParent;
            public Vector3 originalLocalPosition;
            public Quaternion originalLocalRotation;
        }
        private VfxAttachmentInfo[] _vfxAttachments;

        private void Awake()
        {
            // Captura VFX Graph Controllers filhos do player e registra seu parent original
            var controllers = GetComponentsInChildren<Vfx_GraphController>(true);
            if (controllers != null && controllers.Length > 0)
            {
                _vfxAttachments = new VfxAttachmentInfo[controllers.Length];
                for (int i = 0; i < controllers.Length; i++)
                {
                    var c = controllers[i];
                    var t = c.transform;
                    _vfxAttachments[i] = new VfxAttachmentInfo
                    {
                        controller = c,
                        originalParent = t.parent,
                        originalLocalPosition = t.localPosition,
                        originalLocalRotation = t.localRotation
                    };

                    // Define alvo padrão para o VFX caso esteja vazio
                    if (c.targetObject == null)
                        c.targetObject = transform;
                }
            }
        }

        /// <summary>
        /// Triggers the player's death sequence.
        /// </summary>
        public void Die() {
            if (_isDead) return;
            _isDead = true;
            RumbleManager.Instance?.PlayPlayerDeathRumble();
            var vfxController = GetComponent<Player.ECMSaciVfxController>();
            if (vfxController != null) {
                vfxController.OnDeath();
            } else if (deathVFX != null) {
                Instantiate(deathVFX, transform.position, Quaternion.identity);
            }
            if (deathSfx != null) {
                AudioSource.PlayClipAtPoint(deathSfx, transform.position);
            }
            if (playerModel != null) {
                playerModel.SetActive(false);
            } else {
                GetComponentInChildren<Renderer>().enabled = false;
            }

            // Desativa colliders e pausa física para impedir interação com trampolim enquanto morto
            var allColliders = GetComponentsInChildren<Collider>(true);
            foreach (var col in allColliders) col.enabled = false;

            // Desativa CharacterController(s) caso existam (projetos híbridos)
            var characterControllers = GetComponentsInChildren<CharacterController>(true);
            foreach (var cc in characterControllers) cc.enabled = false;

            // Pausa o Rigidbody para não gerar colisões/impulsos
            var rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.isKinematic = true;
            }

            StartCoroutine(NotifyGameManagerOfRespawn());
        }
        private static IEnumerator NotifyGameManagerOfRespawn() {
            yield return new WaitForSeconds(2f);
            if (GameManager.Instance) {
                GameManager.Instance.RespawnPlayer();
            }
        }

        /// <summary>
        /// Resets the player's state for respawning.
        /// </summary>
        public void PrepareForRespawn() {
            if (playerModel) {
                playerModel.SetActive(true);
            } else {
                GetComponentInChildren<Renderer>().enabled = true;
            }

            // Reativa colliders e física do player
            var allColliders = GetComponentsInChildren<Collider>(true);
            foreach (var col in allColliders) col.enabled = true;

            var characterControllers = GetComponentsInChildren<CharacterController>(true);
            foreach (var cc in characterControllers) cc.enabled = true;

            var rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.linearVelocity = Vector3.zero;
            }

            // Restaura parent e posição local de VFX persistentes para evitar aparecer no local da morte anterior
            RestorePersistentVfxAttachments();

            _isDead = false;
        }

        private void RestorePersistentVfxAttachments()
        {
            if (_vfxAttachments == null) return;
            for (int i = 0; i < _vfxAttachments.Length; i++)
            {
                var att = _vfxAttachments[i];
                if (att.controller == null) continue;
                var t = att.controller.transform;
                if (att.originalParent != null)
                {
                    t.SetParent(att.originalParent, false);
                }
                t.localPosition = att.originalLocalPosition;
                t.localRotation = att.originalLocalRotation;

                // Garante que o alvo do VFX siga o player novamente
                if (att.controller.targetObject == null)
                    att.controller.targetObject = transform;
            }
        }
    }
}