using System;
using System.Collections;
using Player;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;
// Required for the NavMeshAgent

namespace Boss {
    /// <summary>
    /// Controls the behavior for the Capelobo boss fight.
    /// Manages states, movement, attacks, and vulnerability phases.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent), typeof(Animator))]
    public class CapeloboBoss : MonoBehaviour {
        private static readonly int IsWalking = Animator.StringToHash("IsWalking");
        private static readonly int Defeated = Animator.StringToHash("Defeated");
        private static readonly int PunchAttack = Animator.StringToHash("PunchAttack");
        private static readonly int SpinAttack = Animator.StringToHash("SpinAttack");
        private static readonly int Taunt = Animator.StringToHash("Taunt");
        private static readonly int Hit = Animator.StringToHash("Hit");
        private static readonly int Stunned = Animator.StringToHash("Stunned");

        // Enum to define the possible states of the boss.
        public enum BossState {
            Idle,
            Chasing,
            Attacking,
            Cooldown,
            Taunting,
            Stunned,
            Defeated
        }

        public event Action OnBossDefeated;

        [Header("State & References")]
        [Tooltip("The current state of the boss. Visible for debugging.")]
        [SerializeField]
        private BossState currentState = BossState.Idle;

        [Tooltip("The player's transform, used as the target for chasing and attacks.")] [SerializeField]
        private Transform playerTarget;

        [Tooltip("An array of all the renderers on the boss model. Used for visual effects like flashing.")]
        [SerializeField]
        private Renderer[] bossRenderers;

        [Header("Movement")] [Tooltip("The movement speed of the boss when chasing the player.")] [SerializeField]
        private float chaseSpeed = 4f;

        [Header("Combat Settings")]
        [Tooltip("The range within which the boss will stop chasing and start attacking.")]
        [SerializeField]
        private float attackRange = 2.5f;
        
        [Tooltip("The minimum time to wait after an attack before chasing again.")]
        [SerializeField]
        private float attackCooldown = 1.5f;
        
        [Tooltip("The number of attacks the boss performs before entering the taunting/vulnerable state.")]
        [SerializeField]
        private int attacksBeforeTaunt = 3;

        [Tooltip("The duration of the taunt animation, during which the boss is vulnerable.")] [SerializeField]
        private float tauntDuration = 4f;

        [Tooltip("How long the boss remains stunned after being hit during a taunt.")] [SerializeField]
        private float stunDuration = 2f;

        [Tooltip("The maximum health of the boss. The player must hit him this many times.")] [SerializeField]
        private int maxHealth = 3;

        [Header("Attack Details")] [Tooltip("A point in front of the boss to check for punch hits.")] [SerializeField]
        private Transform punchHitbox;

        [Tooltip("The size of the punch hitbox check.")] [SerializeField]
        private Vector3 punchHitboxSize = new Vector3(1, 1, 1);

        [Tooltip("The radius of the spin attack check.")] [SerializeField]
        private float spinAttackRadius = 3f;

        [Header("Effects")]
        [Tooltip("Particle effect prefab to spawn when the punch attack hits the player.")]
        [SerializeField]
        private GameObject punchImpactVFX;

        [Tooltip("Particle effect prefab to spawn when the spin attack hits the player.")] [SerializeField]
        private GameObject spinImpactVFX;

        // Private component references
        private NavMeshAgent _navAgent;
        private Animator _animator;

        // Private state variables
        private int _currentAttackCount;
        private int _currentHealth;
        private bool _isFlashing;
        private Vector3 _startPosition;
        private Quaternion _startRotation;

        /// <summary>
        /// Initializes components and sets the initial state of the boss.
        /// </summary>
        private void Awake()
        {
            _navAgent = GetComponent<NavMeshAgent>();
            _animator = GetComponent<Animator>();
            _startPosition = transform.position;
            _startRotation = transform.rotation;
        }

        /// <summary>
        /// Sets up the boss's initial health and ensures it is idle.
        /// </summary>
        private void Start() {
            _currentHealth = maxHealth;
            _navAgent.speed = chaseSpeed;
            _navAgent.isStopped = true;
        }

        /// <summary>
        /// The main logic loop, which runs the state machine.
        /// </summary>
        private void Update() {
            switch (currentState) {
                case BossState.Chasing:
                    HandleChasingState();
                    break;
                case BossState.Idle:
                case BossState.Attacking:
                case BossState.Cooldown:
                case BossState.Taunting:
                case BossState.Stunned:
                case BossState.Defeated:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Public method to be called by another script (e.g., a cutscene manager) to start the fight.
        /// </summary>
        public void StartBattle()
        {
            if (currentState == BossState.Idle)
            {
                ChangeState(BossState.Chasing);
            }
        }

        /// <summary>
        /// Handles the logic for the Chasing state. Runs every frame via Update().
        /// </summary>
        private void HandleChasingState()
        {
            if (!playerTarget) return;

            _navAgent.isStopped = false;
            _navAgent.SetDestination(playerTarget.position);
            _animator.SetBool(IsWalking, true);

            // Check if the player is within attack range.
            if (Vector3.Distance(transform.position, playerTarget.position) <= attackRange)
            {
                ChangeState(BossState.Attacking);
            }
        }

        /// <summary>
        /// Centralized method to change the boss's state and handle transitions.
        /// </summary>
        /// <param name="newState">The new state to transition to.</param>
        private void ChangeState(BossState newState)
        {
            if (currentState == newState) return;
            
            currentState = newState;
            
            // Stop any current movement or animation states before starting a new one.
            _navAgent.isStopped = true;
            _animator.SetBool(IsWalking, false);

            switch (currentState)
            {
                case BossState.Idle:
                    break;
                case BossState.Chasing:
                    _navAgent.isStopped = false;
                    break;
                case BossState.Attacking:
                    FacePlayer();
                    _animator.SetTrigger(Random.Range(0, 2) == 0 ? PunchAttack : SpinAttack);
                    break;
                case BossState.Cooldown:
                    StartCoroutine(CooldownRoutine());
                    break;
                case BossState.Taunting:
                    StartCoroutine(TauntRoutine());
                    break;
                case BossState.Stunned:
                    StartCoroutine(StunRoutine());
                    break;
                case BossState.Defeated:
                    StopAllCoroutines();
                    _navAgent.isStopped = true;
                    _animator.SetTrigger(Defeated);
                    OnBossDefeated?.Invoke();
                    break;
            }
        }
        
        /// <summary>
        /// Coroutine that manages the taunting/vulnerable state.
        /// The boss will remain in this state until the player hits him or time runs out.
        /// </summary>
        private IEnumerator TauntRoutine()
        {
            FacePlayer();
            _animator.SetTrigger(Taunt);

            // REFACTOR: Added a failsafe duration in case the player never hits.
            // If you WANT the boss to taunt forever, remove this WaitForSeconds.
            yield return new WaitForSeconds(tauntDuration);

            // If the coroutine finishes (player didn't hit in time),
            // go back to chasing.
            if (currentState == BossState.Taunting)
            {
                ChangeState(BossState.Chasing);
            }
        }

        /// <summary>
        /// Coroutine that manages the stunned state after the boss is hit while taunting.
        /// </summary>
        private IEnumerator StunRoutine()
        {
            Debug.Log("Boss is stunned for " + stunDuration + " seconds.");

            // Trigger the stun animation.
            _animator.SetTrigger(Stunned);

            // Wait for the stun duration to pass.
            yield return new WaitForSeconds(stunDuration);

            // After the stun is over, reset the attack count and go back to chasing.
            _currentAttackCount = 0;
            ChangeState(BossState.Chasing);
        }
        
        /// <summary>
        /// Coroutine that manages the cooldown state after an attack.
        /// The boss waits for a short duration before chasing again.
        /// </summary>
        private IEnumerator CooldownRoutine()
        {
            // Wait for the specified cooldown duration
            yield return new WaitForSeconds(attackCooldown);

            // After the cooldown is over, go back to chasing the player.
            // We check the state in case something interrupted the cooldown (like a reset)
            if (currentState == BossState.Cooldown)
            {
                ChangeState(BossState.Chasing);
            }
        }

        /// <summary>
        /// Public method to be called by the player's attack script when the boss is hit.
        /// </summary>
        public void TakeDamage()
        {
            // If the boss is in the vulnerable (Taunting) state, deal damage.
            if (currentState == BossState.Taunting)
            {
                _currentHealth--;
                
                // Stop the TauntRoutine immediately.
                StopAllCoroutines(); 
                
                _animator.SetTrigger(Hit);
                
                ChangeState(_currentHealth <= 0 ? BossState.Defeated : BossState.Stunned);
            }
            // If not taunting and not already flashing, show an invulnerable flash.
            else if (!_isFlashing)
            {
                StartCoroutine(InvulnerableFlashRoutine());
            }
        }

        // These methods are called by Animation Events at the precise moment of the attack.
        public void DealPunchDamage()
        {
            _currentAttackCount++;
            Collider[] hits = Physics.OverlapBox(punchHitbox.position, punchHitboxSize / 2, transform.rotation);
            foreach (Collider hit in hits)
            {
                if (!hit.TryGetComponent(out PlayerHealth playerHealth)) continue;
                
                // DESIGN NOTE: This is an insta-kill. 
                // Consider changing to playerHealth.TakeDamage(punchDamage)
                playerHealth.Die();
                return;
            }
        }

        public void DealSpinDamage()
        {
            _currentAttackCount++;
            Collider[] hits = Physics.OverlapSphere(transform.position, spinAttackRadius);
            foreach (Collider hit in hits)
            {
                if (!hit.TryGetComponent(out PlayerHealth playerHealth)) continue;

                // DESIGN NOTE: This is an insta-kill.
                playerHealth.Die();
                return;
            }
        }

        /// <summary>
        /// Helper method to make the boss turn to face the player.
        /// </summary>
        private void FacePlayer()
        {
            if (!playerTarget) return;
            Vector3 direction = (playerTarget.position - transform.position).normalized;
            Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
            transform.rotation = lookRotation;
        }

        /// <summary>
        /// A coroutine that quickly flashes the boss's material to indicate invulnerability.
        /// </summary>
        private IEnumerator InvulnerableFlashRoutine()
        {
            // Set a flag to prevent multiple flashes at once.
            _isFlashing = true;

            // Store original colors (optional, but good practice)
            // This example just flashes white.
            foreach (var renderer in bossRenderers)
            {
                renderer.material.EnableKeyword("_EMISSION");
                renderer.material.SetColor("_EmissionColor", Color.white);
            }

            // Wait for a fraction of a second.
            yield return new WaitForSeconds(0.1f);

            // Revert all renderers to their normal state.
            foreach (var renderer in bossRenderers)
            {
                renderer.material.SetColor("_EmissionColor", Color.black);
                // If your material has a default emission, you might want to set it back to that color instead of black.
            }

            // Reset the flag.
            _isFlashing = false;
        }

        /// <summary>
        /// Resets the boss to its initial state to restart the battle.
        /// </summary>
        public void ResetBattle()
        {
            // Stop all running logic
            StopAllCoroutines();
            
            // Reset state variables
            _currentHealth = maxHealth;
            _currentAttackCount = 0;
            _isFlashing = false;

            // Teleport back to start position
            _navAgent.enabled = false; // Disable agent to teleport
            transform.position = _startPosition;
            transform.rotation = _startRotation;
            _navAgent.enabled = true;
            
            // Reset to idle state, waiting for the battle to start again
            ChangeState(BossState.Idle);
            
            // Reset animator (optional, but good for clearing triggers)
            _animator.ResetTrigger(Hit);
            _animator.ResetTrigger(Defeated);
            _animator.ResetTrigger(Stunned);

            Debug.Log("Capelobo battle has been reset.");
        }

        /// <summary>
        /// Draws gizmos in the editor to visualize attack ranges.
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            // Draw attack range
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);

            // Draw spin attack range
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, spinAttackRadius);
            
            // Draw punch hitbox
            Gizmos.color = Color.yellow;
            if (punchHitbox == null) return;
            Gizmos.matrix = punchHitbox.localToWorldMatrix;
            Gizmos.DrawWireCube(Vector3.zero, punchHitboxSize);
        }

        /// <summary>
        /// Public method.
        /// This method is public so the StateMachineBehaviour on an Animation Clip can access it.
        /// </summary>
        public void OnAttackAnimationFinished()
        {
            Debug.Log("Attack animation finished. Deciding next state.");

            // All logic that happened after the animation now lives here.

            // Se o contador de ataques atingiu o limite, vá para Taunt.
            if (_currentAttackCount >= attacksBeforeTaunt)
            {
                ChangeState(BossState.Taunting);
            }
            // Caso contrário, entre em Cooldown antes de perseguir novamente.
            else
            {
                ChangeState(BossState.Cooldown); // <--- MODIFIQUE ESTA LINHA (era BossState.Chasing)
            }
        }
    }
}