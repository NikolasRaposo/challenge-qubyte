using System.Collections.Generic;
using Enemies;
using Enemy;
using Gameplay;
using UnityEngine;
// <- Adicionado para reconhecer o BossContext
// <- ADICIONADO PARA RECONHECER O CHASINGENEMY

namespace Player.Abilities
{
    /// <summary>
    /// Detects when objects enter the tornado's area of effect and interacts with them.
    /// Looks for components like BoxInteractor or enemy scripts.
    /// </summary>
    public class TornadoTrigger : MonoBehaviour
    {
        [Header("Damage Configuration")]
        [Tooltip("The amount of damage the tornado deals to enemies.")]
        public int tornadoDamage = 10;

        private readonly List<Collider> _alreadyHit = new List<Collider>();

        private void OnTriggerEnter(Collider other)
        {
            if (_alreadyHit.Contains(other)) return;

            // ... (lógica do BoxInteractor, EnemyHealth, BossContext) ...

            if (other.gameObject.TryGetComponent(out BoxInteractor box))
            {
                box.Interact(transform);
                _alreadyHit.Add(other);
            }
            
            if (other.gameObject.TryGetComponent(out BossContext boss))
            {
                Debug.Log($"Tornado hit the BOSS: {other.name}");
                boss.TakeDamage();
                _alreadyHit.Add(other);
            }
            
            if (other.gameObject.TryGetComponent(out Projectile projectile))
            {
                // Se encontrarmos um projétil, chama a função de rebater!
                projectile.Reflect();
                
                // Adiciona à lista para não rebater o mesmo projétil várias vezes
                _alreadyHit.Add(other);
            }

            // --- LÓGICA DO SAPO (CONTRA-ATAQUE) ---
            if (other.gameObject.TryGetComponent(out ChasingEnemy sapo))
            {
                // Checa se o Sapo está se PREPARANDO (IsPreparingAttack)
                // OU se está no meio do PULO (IsVulnerable)
                if (sapo.IsPreparingAttack || sapo.IsVulnerable)
                {
                    Debug.LogWarning($"COUNTER-ATTACK! Tornado hit ChasingEnemy: {other.name}");
                    sapo.Defeat(); // Derrota o sapo
                }
                else
                {
                    // Opcional: O tornado pode não fazer nada se o sapo não estiver vulnerável
                    Debug.Log($"Tornado hit ChasingEnemy, but it was idle/chasing.");
                }
                
                // Adiciona à lista para não checar de novo nesta passagem
                _alreadyHit.Add(other);
            }
        }

        public void ResetHitTargets()
        {
            _alreadyHit.Clear();
        }
    }
}