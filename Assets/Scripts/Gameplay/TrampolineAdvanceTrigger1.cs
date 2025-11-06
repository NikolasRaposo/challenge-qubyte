using Player;
using System.Collections.Generic;
using UnityEngine;

namespace Gameplay
{
    [AddComponentMenu("Gameplay/ECM Trampoline Advance Trigger")]
    public class TrampolineAdvanceTrigger1 : MonoBehaviour
    {
        [Tooltip("Referência ao ECMTrampolineController alvo.")]
        public ECMTrampolineController trampoline;

        [Tooltip("Duração opcional da janela adiantada (s). 0 = usa padrão do controller.")]
        [Range(0f, 1f)]
        public float advanceDurationOverride = 0f;

        // Rastreia jogadores atualmente dentro do trigger para liberar supressão em desativação
        private readonly HashSet<ECMSaciController> _insidePlayers = new HashSet<ECMSaciController>();

        private void Awake()
        {
            if (trampoline == null)
            {
                // Tenta encontrar automaticamente o controller no pai ou nos filhos
                trampoline = GetComponentInParent<ECMTrampolineController>();
                if (trampoline == null)
                    trampoline = GetComponentInChildren<ECMTrampolineController>();
            }

            // Garante que este collider seja trigger
            if (TryGetComponent(out Collider col))
                col.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (trampoline == null)
                return;

            var saci = other.GetComponentInParent<ECMSaciController>();
            if (saci == null)
                return;

            _insidePlayers.Add(saci);
            if (advanceDurationOverride > 0f)
                trampoline.NotifyAdvanceTriggerEnter(saci, advanceDurationOverride);
            else
                trampoline.NotifyAdvanceTriggerEnter(saci);
        }

        private void OnTriggerExit(Collider other)
        {
            if (trampoline == null)
                return;

            var saci = other.GetComponentInParent<ECMSaciController>();
            if (saci == null)
                return;
            _insidePlayers.Remove(saci);
            trampoline.NotifyAdvanceTriggerExit(saci);
        }

        private void OnDisable()
        {
            if (_insidePlayers.Count == 0)
                return;

            foreach (var saci in _insidePlayers)
            {
                if (saci == null) continue;
                if (trampoline != null)
                    trampoline.NotifyAdvanceTriggerExit(saci);
                else
                    saci.SetExternalJumpSuppression(false);
            }

            _insidePlayers.Clear();
        }
    }
}