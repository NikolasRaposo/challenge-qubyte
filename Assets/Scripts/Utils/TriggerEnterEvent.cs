using UnityEngine;
using UnityEngine.Events;

namespace Qubyte.Events
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    [AddComponentMenu("Qubyte/Events/Trigger Enter Event")]
    public class TriggerEnterEvent : MonoBehaviour
    {
        [Header("Filters")]
        [Tooltip("Se verdadeiro, apenas dispara quando o Collider possui a Tag requerida.")]
        public bool filterByTag = false;
        [Tooltip("Tag requerida para disparo quando 'filterByTag' estiver ativo.")]
        public string requiredTag = "Player";

        [Tooltip("Se verdadeiro, apenas dispara quando o Collider está em uma das camadas permitidas.")]
        public bool filterByLayer = false;
        [Tooltip("Camadas permitidas quando 'filterByLayer' estiver ativo.")]
        public LayerMask allowedLayers = ~0;

        [Tooltip("Se verdadeiro, apenas dispara quando o Collider corresponde ao especificado.")]
        public bool filterByCollider = false;
        [Tooltip("Collider específico requerido quando 'filterByCollider' estiver ativo.")]
        public Collider requiredCollider;

        [Header("Behavior")]
        [Tooltip("Força o Collider deste objeto a ser 'isTrigger' em OnValidate.")]
        public bool ensureIsTrigger = true;

        [Tooltip("Dispara apenas uma vez e então desabilita este componente.")]
        public bool oneShot = false;

        [Header("Events")]
        [Tooltip("Evento chamado ao entrar no Trigger, passando o Collider que entrou.")]
        public UnityEvent<Collider> OnEnter;

        [Tooltip("Evento chamado ao entrar no Trigger, sem parâmetros.")]
        public UnityEvent OnEnterSimple;

        private bool _hasTriggered;
        private Collider _selfCollider;

        private void Awake()
        {
            _selfCollider = GetComponent<Collider>();
            if (_selfCollider == null)
            {
                Debug.LogWarning($"[{nameof(TriggerEnterEvent)}] Nenhum Collider encontrado no GameObject '{name}'. O componente requer um Collider.", this);
            }
        }

        private void OnValidate()
        {
            // Garante que o collider é trigger, se solicitado
            if (ensureIsTrigger)
            {
                var col = GetComponent<Collider>();
                if (col != null && !col.isTrigger)
                {
                    col.isTrigger = true;
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!enabled) return;
            if (_hasTriggered && oneShot) return;

            if (filterByTag)
            {
                if (string.IsNullOrEmpty(requiredTag) || !other.CompareTag(requiredTag))
                    return;
            }

            if (filterByLayer)
            {
                int otherLayerBit = 1 << other.gameObject.layer;
                if ((allowedLayers.value & otherLayerBit) == 0)
                    return;
            }

            if (filterByCollider)
            {
                if (requiredCollider == null || !ReferenceEquals(requiredCollider, other))
                    return;
            }

            // Dispara eventos
            try
            {
                OnEnter?.Invoke(other);
                OnEnterSimple?.Invoke();
            }
            finally
            {
                if (oneShot)
                {
                    _hasTriggered = true;
                    enabled = false;
                }
            }
        }
    }
}