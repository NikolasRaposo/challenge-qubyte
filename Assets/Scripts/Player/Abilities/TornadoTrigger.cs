using System.Collections.Generic;
using Enemies;
using Enemy;
using Gameplay;
using UnityEngine;

namespace Player.Abilities {
    /// <summary>
    /// Detects when objects enter the tornado's area of effect and interacts with them.
    /// Looks for components like BoxInteractor or enemy scripts.
    /// </summary>
    public class TornadoTrigger : MonoBehaviour {
        private readonly List<Collider> _alreadyHit = new List<Collider>();
        private void OnTriggerEnter(Collider other) {
            if (_alreadyHit.Contains(other)) return;
            if (other.gameObject.TryGetComponent(out BoxInteractor box)) {
                box.Interact(transform);
                _alreadyHit.Add(other);
                RumbleManager.Instance?.PlayBossHitRumble();
            }
            if (other.gameObject.TryGetComponent(out BossContext boss)) {
                Debug.Log($"Tornado hit the BOSS: {other.name}");
                boss.TakeDamage();
                _alreadyHit.Add(other);
                RumbleManager.Instance?.PlayBossHitRumble();
            }
            if (other.gameObject.TryGetComponent(out ChasingEnemy sapo)) {
                if (sapo.IsPreparingAttack || sapo.IsVulnerable) {
                    Debug.LogWarning($"COUNTER-ATTACK! Tornado hit ChasingEnemy: {other.name}");
                    sapo.Defeat();
                    RumbleManager.Instance?.PlayBossHitRumble();
                } else {
                    Debug.Log($"Tornado hit ChasingEnemy, but it was idle/chasing.");
                }
                _alreadyHit.Add(other);
            }
            if (other.gameObject.TryGetComponent(out Projectile projectile)) {
                projectile.Reflect();
                _alreadyHit.Add(other);
                RumbleManager.Instance?.PlayBossHitRumble();
            }
        }
        public void ResetHitTargets() {
            _alreadyHit.Clear();
        }
    }
}