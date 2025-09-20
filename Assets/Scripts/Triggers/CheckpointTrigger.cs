using UnityEngine;
using Managers;
using UnityEngine.Events; // Namespace necessário para usar UnityEvent

/// <summary>
/// Este script deve ser anexado a um objeto com um Collider configurado como Trigger.
/// Quando o jogador entra no trigger, ele atualiza a posição do último checkpoint no GameManager
/// e pode disparar um evento customizável na primeira ativação.
/// </summary>
[RequireComponent(typeof(Collider))]
public class CheckpointTrigger : MonoBehaviour {
    [Header("Configurações do Checkpoint")]
    [Tooltip("O deslocamento (offset) a partir do centro deste objeto que definirá o ponto de respawn exato.")]
    [SerializeField] private Vector3 checkpointOffset = Vector3.zero;

    [Tooltip("Se marcado, o checkpoint será desativado após ser acionado uma vez.")]
    [SerializeField] private bool disableAfterUse = true;
    
    [Header("Eventos")]
    [Tooltip("Evento que será disparado APENAS na primeira vez que o jogador entrar no trigger.")]
    public UnityEvent onFirstTriggerEnter;
    
    [Header("Debug")]
    [Tooltip("Se marcado, exibe mensagens de log no console quando o checkpoint é ativado.")]
    [SerializeField] private bool enableDebugLogs = false;

    private bool _hasBeenTriggered = false;

    private void OnTriggerEnter(Collider other) {
        // Verifica se o objeto que entrou no trigger é o jogador (pela tag "Player")
        if (other.CompareTag("Player")) {
            
            // Verifica se esta é a primeira vez que o trigger é ativado
            if (!_hasBeenTriggered) {
                // Dispara o evento customizável
                onFirstTriggerEnter.Invoke();
                _hasBeenTriggered = true; // Marca que o evento já foi disparado
            }

            // Calcula a posição final do checkpoint aplicando o offset
            Vector3 checkpointPosition = transform.position + checkpointOffset;

            // Acessa a instância do GameManager (Singleton) e atualiza o checkpoint
            GameManager.Instance.UpdateCheckpoint(checkpointPosition);
            
            // Log para confirmar que o checkpoint foi ativado, se a opção estiver habilitada
            if (enableDebugLogs) {
                Debug.Log($"Checkpoint ativado em {gameObject.name}. Nova posição de respawn: {checkpointPosition}");
            }

            // Se a opção para desativar após o uso estiver marcada, desativa o GameObject
            if (disableAfterUse) {
                // Desativar o colisor é suficiente e mais otimizado do que desativar o objeto inteiro
                GetComponent<Collider>().enabled = false; 
                // Opcionalmente, você pode desativar o objeto inteiro se quiser que ele desapareça:
                // gameObject.SetActive(false);
            }
        }
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