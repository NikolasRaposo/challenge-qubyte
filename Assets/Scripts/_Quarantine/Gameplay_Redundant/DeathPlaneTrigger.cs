using UnityEngine;

// Este script deve ser colocado em um objeto com um Collider configurado como "Is Trigger".
// Exemplo: um plano ou cubo invisível posicionado abaixo do cenário.

public class DeathPlaneTrigger : MonoBehaviour
{
    // A função OnTriggerEnter é chamada pelo motor da Unity sempre que
    // outro Collider entra no trigger deste objeto.
    private void OnTriggerEnter(Collider other)
    {
        // 1. Verificamos se o objeto que entrou no trigger tem a tag "Player".
        // Isso evita que itens, inimigos ou outros objetos ativem a morte.
        if (other.CompareTag("Player"))
        {
            // 2. Tentamos pegar o componente "PlayerHealth" no objeto do jogador.
            // Etapa 1: Tenta pegar o componente
            Player.PlayerHealth playerHealth = other.GetComponent<Player.PlayerHealth>();

            // 3. Verificamos se o componente foi realmente encontrado (para evitar erros).
            if (playerHealth != null)
            {
                // 4. Se tudo estiver certo, chamamos a função Die().
                playerHealth.Die();
            }
            else
            {
                // Opcional: Um aviso no console se o objeto com tag "Player" não tiver o script PlayerHealth.
                Debug.LogWarning("Objeto com tag 'Player' entrou na zona de morte, mas não possui o componente PlayerHealth.");
            }
        }
    }
}