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
            // 1. O 'stomp' está ligado?
            if (!stompEnabled) return;
            
            // 2. O Trigger foi ativado? (Este é o log mais importante)
            Debug.Log($"[PlayerStomp] OnTriggerEnter com: {other.name}", other.gameObject);

            // 3. O jogador está caindo?
            float playerVerticalVelocity = saciController.movement.velocity.y;
            if (playerVerticalVelocity > 0.1f)
            {
                Debug.LogWarning($"[PlayerStomp] FALHA (Velocidade): Jogador está subindo (Vel Y: {playerVerticalVelocity}). Stomp cancelado.", this);
                return;
            }
            
            // 4. O 'stomp' está em cooldown?
            if (Time.time < _lastStompTime + StompCooldown)
            {
                Debug.LogWarning($"[PlayerStomp] FALHA (Cooldown): Stomp ainda em cooldown.", this);
                return;
            }
            
            Debug.Log($"[PlayerStomp] PASSOU (Velocidade: {playerVerticalVelocity}). Procurando scripts de inimigo...", this);

            // 5. É um Inimigo Simples?
            if (other.TryGetComponent(out SimpleEnemy simpleEnemy))
            {
                Debug.Log($"[PlayerStomp] SUCESSO! Pisou em SimpleEnemy: {other.name}. Quicando!", other.gameObject);
                _lastStompTime = Time.time;
                simpleEnemy.Defeat();
                PerformBounce();
                return;
            }

            // 6. É o Sapo (ChasingEnemy)?
            if (other.TryGetComponent(out ChasingEnemy chasingEnemy))
            {
                Debug.Log($"[PlayerStomp] SUCESSO! Pisou em ChasingEnemy: {other.name}. Quicando!", other.gameObject);
                _lastStompTime = Time.time;
                chasingEnemy.Defeat();
                PerformBounce();
                return;
            }
            if (other.TryGetComponent(out RangedEnemy rangedEnemy))
            {
                Debug.Log($"[PlayerStomp] SUCESSO! Pisou em RangedEnemy: {other.name}. Quicando!", other.gameObject);
                _lastStompTime = Time.time;
                rangedEnemy.Defeat();
                PerformBounce();
                return;
            }
            // 7. Se chegou aqui, bateu em algo que não é um inimigo
            Debug.LogWarning($"[PlayerStomp] FALHA (Componente): Bateu em {other.name}, mas não encontrou script 'SimpleEnemy' ou 'ChasingEnemy'.", other.gameObject);
        }

        /// <summary>
        /// Aplica o 'quique' (bounce) no jogador após um stomp bem-sucedido.
        /// </summary>
        private void PerformBounce() {
            RumbleManager.Instance?.PlayStompRumble();
            saciController.movement.ApplyVerticalImpulse(bounceForce);
            saciController.ResetGroundJumpCooldown();
        }
    }
}