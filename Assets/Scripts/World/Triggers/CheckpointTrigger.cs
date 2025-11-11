using UnityEngine;
using Managers;
using UnityEngine.Events; // Namespace necess�rio para usar UnityEvent

/// <summary>
/// Este script deve ser anexado a um objeto com um Collider configurado como Trigger.
/// Quando o jogador entra no trigger pela primeira vez, ele atualiza a posi��o do �ltimo checkpoint no GameManager,
/// dispara um evento customiz�vel e depois desativa a si mesmo para n�o ser executado novamente.
/// O Collider permanece ativo para uso de outros scripts.
/// </summary>
[RequireComponent(typeof(Collider))]
public class CheckpointTrigger : MonoBehaviour {
    [Header("Configura��es do Checkpoint")]
    [Tooltip("O deslocamento (offset) a partir do centro deste objeto que definir� o ponto de respawn exato.")]
    [SerializeField] private Vector3 checkpointOffset = Vector3.zero;
    
    [Header("Eventos")]
    [Tooltip("Evento que ser� disparado APENAS na primeira vez que o jogador entrar no trigger.")]
    public UnityEvent onFirstTriggerEnter;
    
    [Header("Debug")]
    [Tooltip("Se marcado, exibe mensagens de log no console quando o checkpoint � ativado.")]
    [SerializeField] private bool enableDebugLogs = false;

    // Esta vari�vel agora controla se o checkpoint j� foi ativado.
    private bool _hasBeenTriggered = false;

    // Exposto para que gerenciadores (ex.: CheckpointManager) possam ler o offset do ponto.
    public Vector3 GetCheckpointOffset() => checkpointOffset;

    private void OnTriggerEnter(Collider other) {
        // 1. Condi��o de guarda: se j� foi ativado ou se n�o � o jogador, n�o faz mais nada.
        if (_hasBeenTriggered || !other.CompareTag("Player")) {
            return;
        }
        
        // Se chegou at� aqui, � a primeira vez que o jogador entra.
        
        // Marca que o checkpoint foi ativado para n�o repetir a l�gica.
        _hasBeenTriggered = true; 

        // Dispara o evento customiz�vel
        onFirstTriggerEnter.Invoke();
        
        // Calcula a posi��o final do checkpoint aplicando o offset
        Vector3 checkpointPosition = transform.position + checkpointOffset;

        // Acessa a inst�ncia do GameManager (Singleton) e atualiza o checkpoint
        if (GameManager.Instance != null) {
            GameManager.Instance.UpdateCheckpoint(checkpointPosition);
        } else {
            Debug.LogWarning("GameManager.Instance n�o encontrado! O checkpoint n�o foi salvo.");
        }
        
        // Log para confirmar que o checkpoint foi ativado, se a op��o estiver habilitada
        if (enableDebugLogs) {
            Debug.Log($"Checkpoint ativado em {gameObject.name}. Nova posi��o de respawn: {checkpointPosition}");
        }

        // 2. A MUDAN�A PRINCIPAL: Desativa este componente de script para que o OnTriggerEnter n�o seja mais chamado.
        // O GameObject e seu Collider continuar�o ativos.
        this.enabled = false; 
    }

    /// <summary>
    /// A fun��o OnDrawGizmosSelected � chamada pelo Unity no editor sempre que o objeto � selecionado.
    /// Usamos isso para desenhar ajudas visuais (gizmos) na cena para facilitar o design de n�veis.
    /// </summary>
    private void OnDrawGizmosSelected() {
        // Calcula a posi��o final do checkpoint para visualiza��o
        Vector3 targetPosition = transform.position + checkpointOffset;

        // Define a cor do gizmo
        Gizmos.color = Color.cyan;

        // Desenha uma linha do centro do trigger at� o ponto final de respawn
        Gizmos.DrawLine(transform.position, targetPosition);

        // Desenha uma esfera de arame no ponto exato do respawn para melhor visualiza��o
        Gizmos.DrawWireSphere(targetPosition, 0.5f);
        
        // Escreve um texto informativo acima do ponto de respawn
        #if UNITY_EDITOR
        UnityEditor.Handles.Label(targetPosition + Vector3.up * 0.7f, "Ponto de Respawn");
        #endif
    }
}