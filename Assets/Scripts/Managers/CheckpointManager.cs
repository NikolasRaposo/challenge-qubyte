using System;
using System.Collections.Generic;
using UnityEngine;
using ECM.Controllers;

namespace Managers {
    /// <summary>
    /// Gerencia checkpoints na cena e permite aplicar spawn direto em um checkpoint.
    /// Descobre objetos com a tag configurada e lê offsets do CheckpointTrigger.
    /// </summary>
    public class CheckpointManager : MonoBehaviour {
        public static CheckpointManager Instance { get; private set; }

        [Header("Discover")]
        [Tooltip("Tag usada para localizar checkpoints na cena.")]
        [SerializeField] private string checkpointTag = "CheckPoint";
        [Tooltip("Descobrir checkpoints automaticamente no Awake.")]
        [SerializeField] private bool autoDiscoverOnAwake = true;

        [Header("Runtime")]
        [SerializeField] private List<CheckpointInfo> discovered = new List<CheckpointInfo>();

        [Serializable]
        public class CheckpointInfo {
            public string name;
            public Transform transform;
            public Vector3 offset;
        }

        private void Awake() {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            if (autoDiscoverOnAwake) DiscoverCheckpoints();
        }

        /// <summary>
        /// Procura objetos com a tag e preenche a lista de checkpoints com nome, transform e offset.
        /// </summary>
        public void DiscoverCheckpoints() {
            discovered.Clear();
            var gos = GameObject.FindGameObjectsWithTag(checkpointTag);
            foreach (var go in gos) {
                var info = new CheckpointInfo {
                    name = go.name,
                    transform = go.transform,
                    offset = Vector3.zero
                };
                // Tenta ler offset do CheckpointTrigger, se presente
                var trigger = go.GetComponent<CheckpointTrigger>();
                if (trigger != null) {
                    info.offset = trigger.GetCheckpointOffset();
                }
                discovered.Add(info);
            }
            if (discovered.Count == 0) {
                Debug.LogWarning("[CheckpointManager] Nenhum checkpoint encontrado na cena com a tag '" + checkpointTag + "'.");
            } else {
                var names = string.Join(", ", discovered.ConvertAll(d => d.name));
                Debug.Log("[CheckpointManager] Checkpoints encontrados: " + names);
            }
        }

        public IReadOnlyList<CheckpointInfo> GetAll() => discovered;

        public bool TryGetByName(string name, out CheckpointInfo info) {
            info = discovered.Find(d => d.name == name);
            return info != null;
        }

        public bool TryGetByIndex(int index, out CheckpointInfo info) {
            info = null;
            if (index < 0 || index >= discovered.Count) return false;
            info = discovered[index];
            return true;
        }

        /// <summary>
        /// Aplica o spawn do jogador no checkpoint indicado por nome ou índice. Retorna sucesso.
        /// </summary>
        public bool ApplyStartCheckpoint(string checkpointName = null, int index = -1) {
            if (discovered.Count == 0) DiscoverCheckpoints();
            CheckpointInfo info = null;
            if (!string.IsNullOrEmpty(checkpointName)) {
                TryGetByName(checkpointName, out info);
            } else if (index >= 0) {
                TryGetByIndex(index, out info);
            }
            if (info == null) {
                Debug.LogWarning("[CheckpointManager] Checkpoint não encontrado. Informe name ou index válido.");
                return false;
            }
            var targetPosition = info.transform.position + info.offset;
            var gm = GameManager.Instance;
            if (gm == null || gm.player == null) {
                Debug.LogWarning("[CheckpointManager] GameManager/player indisponível para aplicar spawn.");
                return false;
            }
            // Usa PlayerControlGate se disponível; caso contrário, congela física e desabilita controladores temporariamente
            var gate = gm.player.GetComponent<PlayerControlGate>();
            var rb = gm.player.GetComponent<Rigidbody>();
            var bcc = gm.player.GetComponent<BaseCharacterController>();
            var cc = gm.player.GetComponent<CharacterController>();
            var cm = bcc != null ? bcc.movement : null;

            bool prevKinematic = rb != null && rb.isKinematic;
            bool disabledBcc = false;
            bool disabledCc = false;

            if (gate != null) {
                try { gate.FreezePhysics(); } catch { /* safe */ }
                try { gate.DisableController(); } catch { /* safe */ }
                // Integra pausa do ECM para garantir que não haja writes de velocity
                if (bcc != null)
                {
                    try { bcc.restoreVelocityOnResume = false; } catch { /* safe */ }
                    try { bcc.pause = true; } catch { /* safe */ }
                }
                if (cm != null)
                {
                    try { cm.Pause(true, restoreVelocity: false); } catch { /* safe */ }
                }
            } else {
                if (rb != null) {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                    rb.isKinematic = true;
                }
                // Preferir pausa do ECM em vez de desabilitar o componente
                if (bcc != null)
                {
                    try { bcc.restoreVelocityOnResume = false; } catch { /* safe */ }
                    try { bcc.pause = true; } catch { /* safe */ }
                }
                if (cm != null)
                {
                    try { cm.Pause(true, restoreVelocity: false); } catch { /* safe */ }
                }
                // Se necessário, desabilitar controladores clássicos
                if (bcc != null && bcc.enabled) { /* manter habilitado se possível */ }
                if (cc != null && cc.enabled) { cc.enabled = false; disabledCc = true; }
            }

            // Warp: ajusta tanto Transform quanto Rigidbody (quando presente)
            if (rb != null) rb.position = targetPosition;
            gm.player.transform.position = targetPosition;
            Physics.SyncTransforms();

            // Restaura estados
            if (gate != null) {
                try { gate.UnfreezePhysics(); } catch { /* safe */ }
                try { gate.EnableController(); } catch { /* safe */ }
                // Retoma ECM
                if (cm != null)
                {
                    try { cm.Pause(false, restoreVelocity: bcc != null && bcc.restoreVelocityOnResume); } catch { /* safe */ }
                }
                if (bcc != null)
                {
                    try { bcc.pause = false; } catch { /* safe */ }
                }
            } else {
                if (rb != null) rb.isKinematic = prevKinematic;
                if (cm != null)
                {
                    try { cm.Pause(false, restoreVelocity: bcc != null && bcc.restoreVelocityOnResume); } catch { /* safe */ }
                }
                if (bcc != null)
                {
                    try { bcc.pause = false; } catch { /* safe */ }
                }
                if (cc != null && disabledCc) cc.enabled = true;
            }
            gm.UpdateCheckpoint(targetPosition);
            Debug.Log($"[CheckpointManager] Player posicionado em checkpoint '{info.name}' → {targetPosition}");
            return true;
        }
    }
}