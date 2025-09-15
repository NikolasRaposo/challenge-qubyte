using UnityEngine;
using Gameplay; // Namespace do seu BoxInteractor
using System.Collections.Generic; // Necessário para usar Listas

// Se seus inimigos estiverem em um namespace, adicione-o aqui.
// using Enemies; 

/// <summary>
/// Detecta quando objetos entram na área de efeito do tornado e interage com eles.
/// Procura por componentes como BoxInteractor ou scripts de inimigos.
/// </summary>
public class TornadoTrigger : MonoBehaviour
{
    [Header("Configurações de Dano")]
    [Tooltip("A quantidade de dano que o tornado causa aos inimigos.")]
    public int tornadoDamage = 10;

    // Guarda uma lista de objetos que já foram atingidos por este tornado
    // para evitar que o mesmo alvo seja atingido várias vezes em um único ataque.
    private List<Collider> _alreadyHit = new List<Collider>();

    /// <summary>
    /// Este método é chamado automaticamente pelo Unity sempre que
    /// outro Collider entra no trigger deste objeto.
    /// </summary>
    /// <param name="other">O Collider do objeto que entrou no trigger.</param>
    private void OnTriggerEnter(Collider other)
    {
        // Se já atingimos este objeto neste ataque, não fazemos nada.
        if (_alreadyHit.Contains(other))
        {
            return;
        }

        // --- LÓGICA DE INTERAÇÃO ---

        // Tenta encontrar um componente 'BoxInteractor' no objeto que colidiu.
        if (other.gameObject.TryGetComponent(out BoxInteractor box))
        {
            Debug.Log($"Tornado atingiu uma caixa: {other.name}");
            // Chama o método público 'Interact' da caixa.
            // Não precisamos da checagem de "pulo" (hit.normal.y), como você mencionou.
            box.Interact(transform);
            
            // Adiciona a caixa à lista de já atingidos.
            _alreadyHit.Add(other);
        }

        // Tenta encontrar um componente de vida do inimigo.
        // !! IMPORTANTE: Substitua 'EnemyHealth' pelo nome real do seu script de inimigo !!
        if (other.gameObject.TryGetComponent(out EnemyHealth enemy))
        {
            Debug.Log($"Tornado atingiu um inimigo: {other.name}");
            // Chama um método público no script do inimigo para causar dano.
            // !! IMPORTANTE: Substitua 'TakeDamage' pelo nome real do seu método de dano !!
            enemy.TakeDamage(tornadoDamage);
            
            // Adiciona o inimigo à lista de já atingidos.
            _alreadyHit.Add(other);
        }
    }

    /// <summary>
    /// Limpa a lista de alvos já atingidos. Deve ser chamado no início
    /// de cada novo ataque de tornado para que ele possa atingir alvos novamente.
    /// </summary>
    public void ResetHitTargets()
    {
        _alreadyHit.Clear();
    }
}

// !! Exemplo de como seu script de inimigo poderia ser. Ignore se você já tem um. !!
public class EnemyHealth : MonoBehaviour
{
    public void TakeDamage(int damage)
    {
        Debug.Log($"Inimigo {gameObject.name} tomou {damage} de dano!");
        // Aqui você colocaria a lógica de diminuir a vida, etc.
    }
}