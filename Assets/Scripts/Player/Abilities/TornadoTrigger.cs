using System.Collections.Generic;
using Enemies;
using Gameplay;
using UnityEngine;

namespace Player.Powers
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

        // Stores a list of colliders that have already been hit by this tornado instance
        // to prevent a single target from being hit multiple times in one attack.
        private readonly List<Collider> _alreadyHit = new List<Collider>();

        /// <summary>
        /// This method is called automatically by Unity whenever
        /// another Collider enters this object's trigger.
        /// </summary>
        /// <param name="other">The Collider of the object that entered the trigger.</param>
        private void OnTriggerEnter(Collider other)
        {
            // If we've already hit this collider in this attack, do nothing.
            if (_alreadyHit.Contains(other)) return;

            // --- INTERACTION LOGIC ---

            // Try to find a 'BoxInteractor' component on the collided object.
            if (other.gameObject.TryGetComponent(out BoxInteractor box))
            {
                box.Interact(transform);
                _alreadyHit.Add(other);
            }
            
            // Try to find an 'EnemyHealth' component.
            if (other.gameObject.TryGetComponent(out EnemyHealth enemy))
            {
                enemy.TakeDamage(tornadoDamage);
                _alreadyHit.Add(other);
            }
            
            // Try to find the 'CapeloboBoss' component.
            if (other.gameObject.TryGetComponent(out BossContext boss))
            {
                Debug.Log($"Tornado hit the BOSS: {other.name}");
                boss.TakeDamage();
                _alreadyHit.Add(other);
            }
        }

        /// <summary>
        /// Clears the list of already hit targets. This should be called at the beginning
        /// of each new tornado attack so it can hit targets again.
        /// </summary>
        public void ResetHitTargets()
        {
            _alreadyHit.Clear();
        }
    }

    // !! Example of how your enemy script could look. Ignore if you already have one. !!
    public class EnemyHealth : MonoBehaviour {
        public void TakeDamage(int damage)
        {
            Debug.Log($"Enemy {gameObject.name} took {damage} damage!");
            // Here you would put the logic for decreasing health, etc.
        }
    }
}