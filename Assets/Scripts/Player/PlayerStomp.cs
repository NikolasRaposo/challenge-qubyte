using Enemy;
using Gameplay;
using UnityEngine;

namespace Player
{
    /// <summary>
    /// Detecta quando o jogador pula na cabeça de um inimigo.
    /// Este script deve ser colocado em um GameObject "filho" nos pés do jogador,
    /// com um Collider marcado como 'Is Trigger'.
    /// </summary>
    public class PlayerStomp : MonoBehaviour
    {
        [Header("Configuração Geral")]
        [Tooltip("Liga ou desliga a funcionalidade de 'stomp' (pisão).")]
        [SerializeField] private bool stompEnabled = true;

        [Header("Referências")]
        [Tooltip("Arraste o GameObject principal do Jogador (que tem o ECMSaciController) aqui.")]
        [SerializeField] private ECMSaciController saciController;

        [Header("Filtro de Trigger")]
        [Tooltip("Somente colliders nessas layers serão considerados para stomp (0 = desativado).")]
        [SerializeField] private LayerMask stompableLayers = 0;

        [Header("Debug")]
        [Tooltip("Quando ligado, imprime logs detalhados de stomp (falhas e sucessos).")]
        [SerializeField] private bool verboseLogs = false;

        [Header("Restrições de Estado")]
        [Tooltip("Quando ligado, o stomp só funciona quando fora do chão.")]
        [SerializeField] private bool requireAirborne = true;
        [Tooltip("Quando ligado, o stomp só funciona enquanto em queda (free fall).")]
        [SerializeField] private bool requireFreeFall = false;
        [Tooltip("Velocidade mínima descendente para considerar estado de queda (m/s).")]
        [SerializeField] private float minFallSpeed = 0.1f;

        [Header("Configuração do Pulo")]
        [Tooltip("A força do 'quique' que o jogador dá ao pular em um inimigo.")]
        [SerializeField] private float bounceForce = 15f;
        
        // Cache da última vez que pulou para evitar "pulos" múltiplos no mesmo frame
        private float _lastStompTime;
        private const float StompCooldown = 0.1f;

        private void Awake()
        {
            // Tenta encontrar o controlador automaticamente se não foi arrastado
            if (saciController == null)
            {
                saciController = GetComponentInParent<ECMSaciController>();
            }

            if (saciController == null)
            {
                Debug.LogError("PlayerStomp ERRO: Não foi possível encontrar o 'ECMSaciController' nos pais!", this);
                enabled = false;
            }
        }

        /// <summary>
        /// Chamado pelo Unity quando algo entra neste Trigger.
        /// </summary>
        private void OnTriggerEnter(Collider other)
        {
            // 1) Gate principal
            if (!stompEnabled) return;

            // 2) Filtro opcional por Layer (silencia colisões não relevantes)
            if (stompableLayers.value != 0 && (stompableLayers.value & (1 << other.gameObject.layer)) == 0)
                return;

            // 3) Identifique alvos válidos de stomp (inimigos OU interativos de stomp)
            SimpleEnemy simpleEnemy = null;
            ChasingEnemy chasingEnemy = null;
            RangedEnemy rangedEnemy = null;
            BoxInteractor boxInteractor = null;
            bool isStompTarget = false;

            if (other.TryGetComponent(out simpleEnemy)) isStompTarget = true;
            else if (other.TryGetComponent(out chasingEnemy)) isStompTarget = true;
            else if (other.TryGetComponent(out rangedEnemy)) isStompTarget = true;
            else if (other.TryGetComponent(out boxInteractor)) isStompTarget = true;

            if (!isStompTarget)
                return; // silencioso para objetos não relevantes

            if (verboseLogs)
                Debug.Log($"[PlayerStomp] Trigger com inimigo: {other.name}", other.gameObject);

            // 4) Verificações de estado / velocidade / cooldown APENAS para alvos válidos
            bool isGrounded = saciController.movement.isGrounded;
            float playerVerticalVelocity = saciController.movement.velocity.y;

            // Exigir estar fora do chão (por padrão)
            if (requireAirborne && isGrounded)
            {
                if (verboseLogs)
                    Debug.Log($"[PlayerStomp] Cancelado (Estado): jogador no chão.", this);
                return;
            }

            // Exigir queda (free fall): não estar no chão e velocidade descendente suficiente
            if (requireFreeFall)
            {
                if (isGrounded || playerVerticalVelocity > -minFallSpeed)
                {
                    if (verboseLogs)
                        Debug.Log($"[PlayerStomp] Cancelado (Estado): requer queda (Vy: {playerVerticalVelocity:F2}).", this);
                    return;
                }
            }
            else
            {
                // Se não exigir queda, ao menos não permitir stomp enquanto subindo
                if (playerVerticalVelocity > 0.1f)
                {
                    if (verboseLogs)
                        Debug.Log($"[PlayerStomp] Cancelado (Velocidade): subindo (Vy: {playerVerticalVelocity:F2}).", this);
                    return;
                }
            }

            // Cooldown
            if (Time.time < _lastStompTime + StompCooldown)
            {
                if (verboseLogs)
                    Debug.Log($"[PlayerStomp] Cancelado (Cooldown): ainda em cooldown.", this);
                return;
            }

            // 5) Aplicar stomp no alvo detectado
            _lastStompTime = Time.time;

            if (simpleEnemy != null)
            {
                if (verboseLogs)
                    Debug.Log($"[PlayerStomp] SUCESSO! SimpleEnemy: {other.name}.", other.gameObject);
                simpleEnemy.Defeat();
                PerformBounce();
                return;
            }

            if (chasingEnemy != null)
            {
                if (verboseLogs)
                    Debug.Log($"[PlayerStomp] SUCESSO! ChasingEnemy: {other.name}.", other.gameObject);
                chasingEnemy.Defeat();
                PerformBounce();
                return;
            }

            if (rangedEnemy != null)
            {
                if (verboseLogs)
                    Debug.Log($"[PlayerStomp] SUCESSO! RangedEnemy: {other.name}.", other.gameObject);
                rangedEnemy.Defeat();
                PerformBounce();
                return;
            }

            if (boxInteractor != null)
            {
                if (verboseLogs)
                    Debug.Log($"[PlayerStomp] Interagindo com BoxInteractor: {other.name}.", other.gameObject);
                // Passa o transform deste filho (layer PlayerStomp) para permitir que o BoxInteractor
                // identifique corretamente interações de stomp e aplique trampolim apenas nestes casos.
                boxInteractor.Interact(transform);
                // Nota: o bounce (se houver) é gerenciado pelo BoxInteractor via ApplyTrampolineEffect.
                return;
            }
        }

        /// <summary>
        /// Aplica o 'quique' (bounce) no jogador após um stomp bem-sucedido.
        /// </summary>
        private void PerformBounce() {
            RumbleManager.Instance?.PlayStompRumble();
            saciController.movement.ApplyVerticalImpulse(bounceForce);
            // Permite novo duplo-pulo após o impulso de stomp
            saciController.ResetMidAirJumpCount();
            saciController.ResetGroundJumpCooldown();
        }
    }
}