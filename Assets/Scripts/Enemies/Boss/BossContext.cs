using System;
using System.Collections;
using Enemies.BossStates;
using Player;
using UnityEngine;
using UnityEngine.AI;
// Required for the NavMeshAgent

namespace Enemies {
    /// <summary>
    /// Controls the behavior for the Capelobo boss fight.
    /// Manages states, movement, attacks, and vulnerability phases.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent), typeof(Animator))]
    public class BossContext : MonoBehaviour {
        public static readonly int IsWalking = Animator.StringToHash("IsWalking");
        public static readonly int Defeated = Animator.StringToHash("Defeated");
        public static readonly int PunchAttack = Animator.StringToHash("PunchAttack");
        public static readonly int SpinAttack = Animator.StringToHash("SpinAttack");
        public static readonly int Taunt = Animator.StringToHash("Taunt");
        public static readonly int Hit = Animator.StringToHash("Hit");
        public static readonly int Stunned = Animator.StringToHash("Stunned");
        public static readonly int GetUp = Animator.StringToHash("GetUp");
        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

        [Header("Debugging")]
        [Tooltip("Enable to print detailed state changes and events to the console.")]
        [SerializeField]
        private bool enableDebugLogs = true;

        public event Action OnBossDefeated;

        [Header("State & References")]
        [Tooltip("The player's transform, used as the target for chasing and attacks.")]
        [SerializeField]
        private Transform playerTarget;

        [Header("Movement")]
        [Tooltip("The movement speed of the boss when chasing the player.")]
        [SerializeField]
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

        [Tooltip("The duration of the taunt animation, during which the boss is vulnerable.")]
        [SerializeField]
        private float tauntDuration = 4f;

        [Tooltip("How long the boss remains stunned after being hit during a taunt.")]
        [SerializeField]
        private float stunDuration = 2f;
        
        [Tooltip("How long the boss pauses in Idle after a stun before chasing again.")]
        [SerializeField]
        private float stunRecoveryTime = 1.5f;

        [Tooltip("The maximum health of the boss. The player must hit him this many times.")]
        [SerializeField]
        private int maxHealth = 3;

        [Header("Attack Details")]
        [Tooltip("A point in front of the boss to check for punch hits.")]
        [SerializeField]
        private Transform punchHitbox;

        [Tooltip("The size of the punch hitbox check.")]
        [SerializeField]
        private Vector3 punchHitboxSize = new Vector3(1, 1, 1);

        [Tooltip("The radius of the spin attack check.")]
        [SerializeField]
        private float spinAttackRadius = 3f;
        
        public Transform PlayerTarget => playerTarget;
        public float ChaseSpeed => chaseSpeed;
        public float AttackRange => attackRange;
        public float AttackCooldown => attackCooldown;
        public int AttacksBeforeTaunt => attacksBeforeTaunt;
        public float TauntDuration => tauntDuration;
        public float StunDuration => stunDuration;
        public float StunRecoveryTime => stunRecoveryTime;
        public bool EnableDebugLogs => enableDebugLogs; 
        public NavMeshAgent NavAgent { get; private set; }
        public Animator Animator { get; private set; }
        public BossAudio Audio { get; private set; }
        public BossVFX VFX { get; private set; }
        public int CurrentHealth { get; set; }
        public int CurrentAttackCount { get; set; }
        public BossState IdleState { get; private set; }
        public BossState ChasingState { get; private set; }
        public BossState AttackingState { get; private set; }
        public BossState CooldownState { get; private set; }
        public BossState TauntingState { get; private set; }
        public BossState StunnedState { get; private set; }
        public BossState RecoveringState { get; private set; }
        public BossState DefeatedState { get; private set; }

        private BossState _currentState;
        private Vector3 _startPosition;
        private Quaternion _startRotation;

        /// <summary>
        /// Initializes components and creates all state instances.
        /// </summary>
        private void Awake()
        {
            NavAgent = GetComponent<NavMeshAgent>();
            Animator = GetComponent<Animator>();
            Audio = GetComponent<BossAudio>();
            VFX = GetComponent<BossVFX>();
            
            _startPosition = transform.position;
            _startRotation = transform.rotation;

            // Create instances of all possible states
            IdleState = new BossIdleState(this);
            ChasingState = new BossChasingState(this);
            AttackingState = new BossAttackingState(this);
            CooldownState = new BossCooldownState(this);
            TauntingState = new BossTauntingState(this);
            StunnedState = new BossStunnedState(this);
            RecoveringState = new BossRecoveringState(this);
            DefeatedState = new BossDefeatedState(this);
            
            if (enableDebugLogs) Debug.Log($"[BossContext] Awake: All states initialized.");
        }

        /// <summary>
        /// Sets up the boss's initial health and enters the Idle state.
        /// </summary>
        private void Start() {
            CurrentHealth = maxHealth;
            NavAgent.speed = chaseSpeed;
            NavAgent.isStopped = true;

            // Set the initial state
            _currentState = IdleState;
            _currentState.Enter();
        }

        /// <summary>
        /// The main logic loop. Delegates all logic to the current state.
        /// </summary>
        private void Update() {
            // The entire Update loop is just this one line!
            _currentState?.Tick();
        }

        /// <summary>
        /// Centralized method to transition between states.
        /// </summary>
        /// <param name="newState">The state to transition to.</param>
        public void ChangeState(BossState newState) {
            if (_currentState == newState) return;
            if (enableDebugLogs) Debug.Log($"[BossContext] Changing state: {_currentState?.GetType().Name} -> {newState.GetType().Name}");
            _currentState?.Exit();
            _currentState = newState;
            _currentState.Enter();
        }

        /// <summary>
        /// Public method to be called by another script to start the fight.
        /// </summary>
        public void StartBattle() {
            if (enableDebugLogs) Debug.Log($"[BossContext] StartBattle() called. Current state: {_currentState.GetType().Name}");
            // Only start if we are currently idle
            if (_currentState == IdleState) {
                ChangeState(ChasingState);
            }
        }

        /// <summary>
        /// Public method to be called when the boss is hit.
        /// Delegates the "how to react" logic to the current state.
        /// </summary>
        public void TakeDamage() {
            if (enableDebugLogs) Debug.Log($"[BossContext] TakeDamage() called. Delegating to state: {_currentState.GetType().Name}");
            _currentState.OnTakeDamage();
        }

        /// <summary>
        /// Resets the boss to its initial state to restart the battle.
        /// </summary>
        public void ResetBattle() {
            if (enableDebugLogs) Debug.LogWarning($"[BossContext] RESET BATTLE called. Stopping coroutines and resetting all stats.");
            // Stop all running logic
            StopAllCoroutines();
            Audio.StopAllAudio();
            VFX.StopAllVFX();
            CurrentHealth = maxHealth;
            CurrentAttackCount = 0;
            NavAgent.enabled = false;
            transform.position = _startPosition;
            transform.rotation = _startRotation;
            NavAgent.enabled = true;
            Animator.ResetTrigger(Hit);
            Animator.ResetTrigger(Defeated);
            Animator.ResetTrigger(Stunned);
            Animator.SetBool(IsWalking, false);
            ChangeState(IdleState);
        }

        /// <summary>
        /// Called by an Animation Event.
        /// Delegates the "what to do next" logic to the current state.
        /// </summary>
        public void OnAttackAnimationFinished() {
            if (enableDebugLogs) Debug.Log($"[BossContext] AnimationEvent: OnAttackAnimationFinished() received. Delegating to state: {_currentState.GetType().Name}");
            _currentState.OnAnimationFinished();
        }

        /// <summary>
        /// Called by an Animation Event to deal punch damage.
        /// </summary>
        public void DealPunchDamage() {
            if (enableDebugLogs) Debug.Log($"[BossContext] AnimationEvent: DealPunchDamage() at frame.");
            CurrentAttackCount++;
            var hits = Physics.OverlapBox(punchHitbox.position, punchHitboxSize / 2, transform.rotation);
            var hasHit = false;
            foreach (var hit in hits) {
                if (!hit.TryGetComponent(out PlayerHealth playerHealth)) continue;
                if (enableDebugLogs) Debug.Log($"[BossContext] Punch HIT player: {hit.name}");
                playerHealth.Die();
                hasHit = true; 
                break;
            }
            if (!hasHit) return;
            Audio.PlayPunchImpact();
            VFX.PlayPunchImpact(punchHitbox.position, punchHitbox.rotation);
        }

        /// <summary>
        /// Chamado pelo Animation Event no frame do giro
        /// </summary>
        public void DealSpinDamage() {
            if (enableDebugLogs) Debug.Log($"[BossContext] AnimationEvent: DealSpinDamage() at frame.");
            CurrentAttackCount++;
            var hits = Physics.OverlapSphere(transform.position, spinAttackRadius);
            var hasHit = false;
            foreach (var hit in hits) {
                if (!hit.TryGetComponent(out PlayerHealth playerHealth)) continue;
                if (enableDebugLogs) Debug.Log($"[BossContext] Spin HIT player: {hit.name}");
                playerHealth.Die();
                hasHit = true;
                break;
            }
            if (!hasHit) return;
            Audio.PlaySpinImpact();
            VFX.PlaySpinImpact(transform.position, transform.rotation);
        }
        
        /// <summary>
        /// Helper method to make the boss turn to face the player.
        /// </summary>
        public void FacePlayer()
        {
            if (!playerTarget) return;
            Vector3 direction = (playerTarget.position - transform.position).normalized;
            Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
            transform.rotation = lookRotation;
        }

        /// <summary>
        /// Public method for the Defeated state to invoke the event.
        /// </summary>
        public void InvokeOnBossDefeated() {
            if (enableDebugLogs) Debug.Log($"[BossContext] Invoking OnBossDefeated event.");
            OnBossDefeated?.Invoke();
        }
        /// <summary>
        /// Called by an Animation Event during the walk cycle.
        /// </summary>
        public void OnFootstep() {
            Audio.PlayFootstep();
        }

        /// <summary>
        /// Called by an Animation Event at the start of the punch.
        /// </summary>
        public void OnPunchAttackStart() {
            Audio.PlayPunchAttack();
        }

        /// <summary>
        /// Called by an Animation Event at the start of the spin.
        /// </summary>
        public void OnSpinAttackStart() {
            Audio.PlaySpinAttack();
        }

        /// <summary>
        /// Draws gizmos in the editor to visualize attack ranges.
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);

            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, spinAttackRadius);

            Gizmos.color = Color.yellow;
            if (punchHitbox == null) return;
            Gizmos.matrix = punchHitbox.localToWorldMatrix;
            Gizmos.DrawWireCube(Vector3.zero, punchHitboxSize);
        }
    }
}