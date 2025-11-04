using UnityEngine;
using Managers;
using UnityEngine.Events; // Namespace necessário para usar UnityEvent

/// <summary>
/// Este script deve ser anexado a um objeto com um Collider configurado como Trigger.
/// Quando o jogador entra no trigger pela primeira vez, ele atualiza a posição do último checkpoint no GameManager,
/// dispara um evento customizável e depois desativa a si mesmo para não ser executado novamente.
/// O Collider permanece ativo para uso de outros scripts.
/// </summary>
[RequireComponent(typeof(Collider))]
public class CheckpointTrigger : MonoBehaviour {
    [Header("Configurações do Checkpoint")]
    [Tooltip("O deslocamento (offset) a partir do centro deste objeto que definirá o ponto de respawn exato.")]
    [SerializeField] private Vector3 checkpointOffset = Vector3.zero;
    
    [Header("Eventos")]
    [Tooltip("Evento que será disparado APENAS na primeira vez que o jogador entrar no trigger.")]
    public UnityEvent onFirstTriggerEnter;
    
    [Header("Debug")]
    [Tooltip("Se marcado, exibe mensagens de log no console quando o checkpoint é ativado.")]
    [SerializeField] private bool enableDebugLogs = false;

    // Esta variável agora controla se o checkpoint já foi ativado.
    private bool _hasBeenTriggered = false;

    private void OnTriggerEnter(Collider other) {
        // 1. Condição de guarda: se já foi ativado ou se não é o jogador, não faz mais nada.
        if (_hasBeenTriggered || !other.CompareTag("Player")) {
            return;
        }
        
        // Se chegou até aqui, é a primeira vez que o jogador entra.
        
        // Marca que o checkpoint foi ativado para não repetir a lógica.
        _hasBeenTriggered = true; 

        // Dispara o evento customizável
        onFirstTriggerEnter.Invoke();
        
        // Calcula a posição final do checkpoint aplicando o offset
        Vector3 checkpointPosition = transform.position + checkpointOffset;

        // Acessa a instância do GameManager (Singleton) e atualiza o checkpoint
        if (GameManager.Instance != null) {
            GameManager.Instance.UpdateCheckpoint(checkpointPosition);
        } else {
            Debug.LogWarning("GameManager.Instance não encontrado! O checkpoint não foi salvo.");
        }
        
        // Log para confirmar que o checkpoint foi ativado, se a opção estiver habilitada
        if (enableDebugLogs) {
            Debug.Log($"Checkpoint ativado em {gameObject.name}. Nova posição de respawn: {checkpointPosition}");
        }

        // 2. A MUDANÇA PRINCIPAL: Desativa este componente de script para que o OnTriggerEnter não seja mais chamado.
        // O GameObject e seu Collider continuarão ativos.
        this.enabled = false; 
    }

    /// <summary>
    /// A função OnDrawGizmosSelected é chamada pelo Unity no editor sempre que o objeto é selecionado.
    /// Usamos isso para desenhar ajudas visuais (gizmos) na cena para facilitar o design de níveis.
    /// </summary>
    private void OnDrawGizmosSelected() {
        // Calcula a posição final do checkpoint para visualização
        Vector3 targetPosition = transform.position + checkpointOffset;

        // Define a cor do gizmo
        Gizmos.color = Color.cyan;

        // Desenha uma linha do centro do trigger até o ponto final de respawn
        Gizmos.DrawLine(transform.position, targetPosition);

        // Desenha uma esfera de arame no ponto exato do respawn para melhor visualização
        Gizmos.DrawWireSphere(targetPosition, 0.5f);
        
        // Escreve um texto informativo acima do ponto de respawn
        #if UNITY_EDITOR
        UnityEditor.Handles.Label(targetPosition + Vector3.up * 0.7f, "Ponto de Respawn");
        #endif
    }
}