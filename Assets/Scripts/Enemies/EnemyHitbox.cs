using Player;
using System.Collections.Generic;
using UnityEngine;

namespace Enemy
{
    /// <summary>
    /// Um trigger simples que causa dano ao jogador.
    /// Deve ser ativado e desativado pelo script principal do inimigo (ex: ChasingEnemy).
    /// Usa uma lista interna para garantir que o jogador só tome dano uma vez por ataque.
    /// </summary>
    public class EnemyHitbox : MonoBehaviour
    {
        // Lista de quem já tomou dano *neste* ataque
        private readonly List<Collider> _alreadyHit = new List<Collider>();

        /// <summary>
        /// Chamado pelo Unity quando este trigger colide com outro collider.
        /// </summary>
        private void OnTriggerEnter(Collider other)
        {
            // Já acertamos esse objeto?
            if (_alreadyHit.Contains(other)) return;

            // É o jogador?
            if (other.gameObject.CompareTag("Player"))
            {
                if (other.TryGetComponent(out PlayerHealth playerHealth))
                {
                    Debug.LogWarning($"[EnemyHitbox] Acertou o jogador: {other.name}");
                    playerHealth.Die();
                    _alreadyHit.Add(other); // Adiciona na lista para não dar dano duplo
                }
            }
        }

        /// <summary>
        /// Limpa a lista de alvos atingidos.
        /// Deve ser chamado pelo inimigo principal *antes* de ativar a hitbox.
        /// </summary>
        public void ResetHitbox()
        {
            _alreadyHit.Clear();
        }
    }
}
