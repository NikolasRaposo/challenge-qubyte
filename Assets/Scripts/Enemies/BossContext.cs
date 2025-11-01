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
        [Tooltip("An array of all the renderers on the boss model. Used for visual effects like flashing.")]
        [SerializeField]
        private Renderer[] bossRenderers;
        [Header("Audio Sources")]
        [Tooltip("AudioSource para sons de 'um-shot' (impactos, gritos, etc.).")]
        [SerializeField]
        private AudioSource sfxAudioSource;
        [Tooltip("AudioSource para sons em 'loop' (ex: stun).")]
        [SerializeField]
        private AudioSource statusEffectAudioSource;
        
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

        [Header("Attack Effects")]
        [Tooltip("VFX a ser instanciado no local do soco.")]
        [SerializeField]
        private GameObject punchImpactVFX;
        [Tooltip("SFX a ser tocado no impacto do soco.")]
        [SerializeField]
        private AudioClip punchImpactSFX;
        
        [Tooltip("VFX a ser instanciado na posição do chefe no ataque giratório.")]
        [SerializeField]
        private GameObject spinImpactVFX;
        [Tooltip("SFX a ser tocado no impacto do ataque giratório.")]
        [SerializeField]
        private AudioClip spinImpactSFX;

        [Header("Stun Effects")]
        [Tooltip("Objeto filho que contém o VFX de 'estrelinhas' do stun. Será ativado/desativado.")]
        public GameObject stunVFX; // Feito público para os Estados
        [Tooltip("SFX de 'passarinhos' em loop para tocar durante o stun.")]
        public AudioClip stunSFX; // Feito público para os Estados

        [Header("VFX Settings")]
        [Tooltip("Cor para o flash de 'vulnerável' (Taunt).")]
        [SerializeField]
        private Color vulnerableColor = Color.yellow;
        [Tooltip("Velocidade do pisca-pisca de vulnerabilidade (ex: 0.3s).")]
        [SerializeField]
        private float vulnerableFlashRate = 0.3f;
        
        // --- Component References (Exposed via properties) ---
        public NavMeshAgent NavAgent { get; private set; }
        public Animator Animator { get; private set; }
        public AudioSource StatusEffectAudioSource => statusEffectAudioSource;

        // --- State Variables (Exposed via properties) ---
        public Transform PlayerTarget => playerTarget;
        public float ChaseSpeed => chaseSpeed;
        public float AttackRange => attackRange;
        public float AttackCooldown => attackCooldown;
        public int AttacksBeforeTaunt => attacksBeforeTaunt;
        public float TauntDuration => tauntDuration;
        public float StunDuration => stunDuration;
        public float StunRecoveryTime => stunRecoveryTime;
        public int CurrentHealth { get; set; }
        public int CurrentAttackCount { get; set; }
        public bool IsFlashing { get; set; }
        public bool EnableDebugLogs => enableDebugLogs; // Public getter for states

        // --- State Machine ---
        private BossState _currentState;

        // --- State Instances ---
        public BossState IdleState { get; private set; }
        public BossState ChasingState { get; private set; }
        public BossState AttackingState { get; private set; }
        public BossState CooldownState { get; private set; }
        public BossState TauntingState { get; private set; }
        public BossState StunnedState { get; private set; }
        public BossState RecoveringState { get; private set; }
        public BossState DefeatedState { get; private set; }

        // --- Private Internal ---
        private Vector3 _startPosition;
        private Quaternion _startRotation;

        /// <summary>
        /// Initializes components and creates all state instances.
        /// </summary>
        private void Awake()
        {
            NavAgent = GetComponent<NavMeshAgent>();
            Animator = GetComponent<Animator>();

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
            
            if (stunVFX != null) stunVFX.SetActive(false);
            if (enableDebugLogs) Debug.Log($"[BossContext] Awake: All states initialized.");
        }

        /// <summary>
        /// Sets up the boss's initial health and enters the Idle state.
        /// </summary>
        private void Start()
        {
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
        private void Update()
        {
            // The entire Update loop is just this one line!
            _currentState?.Tick();
        }

        /// <summary>
        /// Centralized method to transition between states.
        /// </summary>
        /// <param name="newState">The state to transition to.</param>
        public void ChangeState(BossState newState)
        {
            if (_currentState == newState) return;
            
            if (enableDebugLogs) Debug.Log($"[BossContext] Changing state: {_currentState?.GetType().Name} -> {newState.GetType().Name}");

            // Call Exit logic on the old state
            _currentState?.Exit();

            // Set the new state
            _currentState = newState;

            // Call Enter logic on the new state
            _currentState.Enter();
        }

        /// <summary>
        /// Public method to be called by another script to start the fight.
        /// </summary>
        public void StartBattle()
        {
            if (enableDebugLogs) Debug.Log($"[BossContext] StartBattle() called. Current state: {_currentState.GetType().Name}");
            // Only start if we are currently idle
            if (_currentState == IdleState)
            {
                ChangeState(ChasingState);
            }
        }

        /// <summary>
        /// Public method to be called when the boss is hit.
        /// Delegates the "how to react" logic to the current state.
        /// </summary>
        public void TakeDamage()
        {
            if (enableDebugLogs) Debug.Log($"[BossContext] TakeDamage() called. Delegating to state: {_currentState.GetType().Name}");
            _currentState.OnTakeDamage();
        }

        /// <summary>
        /// Resets the boss to its initial state to restart the battle.
        /// </summary>
        public void ResetBattle()
        {
            if (enableDebugLogs) Debug.LogWarning($"[BossContext] RESET BATTLE called. Stopping coroutines and resetting all stats.");
            // Stop all running logic
            StopAllCoroutines();

            // Reset state variables
            CurrentHealth = maxHealth;
            CurrentAttackCount = 0;
            IsFlashing = false;

            // Teleport back to start position
            NavAgent.enabled = false; // Disable agent to teleport
            transform.position = _startPosition;
            transform.rotation = _startRotation;
            NavAgent.enabled = true;

            // Reset animator
            Animator.ResetTrigger(Hit);
            Animator.ResetTrigger(Defeated);
            Animator.ResetTrigger(Stunned);
            Animator.SetBool(IsWalking, false);

            // Reset to idle state
            ChangeState(IdleState);
        }

        // --- Public Methods (Called by Animation Events) ---

        /// <summary>
        /// Called by an Animation Event.
        /// Delegates the "what to do next" logic to the current state.
        /// </summary>
        public void OnAttackAnimationFinished()
        {
            if (enableDebugLogs) Debug.Log($"[BossContext] AnimationEvent: OnAttackAnimationFinished() received. Delegating to state: {_currentState.GetType().Name}");
            _currentState.OnAnimationFinished();
        }

        /// <summary>
        /// Called by an Animation Event to deal punch damage.
        /// </summary>
        public void DealPunchDamage() {
            if (enableDebugLogs) Debug.Log($"[BossContext] AnimationEvent: DealPunchDamage() at frame.");
            CurrentAttackCount++;
            
            Collider[] hits = Physics.OverlapBox(punchHitbox.position, punchHitboxSize / 2, transform.rotation);
            bool hasHit = false;
            
            foreach (Collider hit in hits) {
                if (hit.TryGetComponent(out PlayerHealth playerHealth)) {
                    if (enableDebugLogs) Debug.Log($"[BossContext] Punch HIT player: {hit.name}");
                    playerHealth.Die();
                    hasHit = true; 
                    break;
                }
            }

            if (hasHit) {
                if (punchImpactSFX != null && sfxAudioSource != null) {
                    sfxAudioSource.PlayOneShot(punchImpactSFX);
                }
                if (punchImpactVFX != null) {
                    Instantiate(punchImpactVFX, punchHitbox.position, punchHitbox.rotation);
                }
            }
        }

        /// <summary>
        /// Chamado pelo Animation Event no frame do giro
        /// </summary>
        public void DealSpinDamage() {
            if (enableDebugLogs) Debug.Log($"[BossContext] AnimationEvent: DealSpinDamage() at frame.");
            CurrentAttackCount++;
            Collider[] hits = Physics.OverlapSphere(transform.position, spinAttackRadius);
            bool hasHit = false;
            foreach (Collider hit in hits) {
                if (hit.TryGetComponent(out PlayerHealth playerHealth)) {
                    if (enableDebugLogs) Debug.Log($"[BossContext] Spin HIT player: {hit.name}");
                    playerHealth.Die();
                    hasHit = true;
                    break; 
                }
            }
            if (hasHit) {
                if (spinImpactSFX != null && sfxAudioSource != null) {
                    sfxAudioSource.PlayOneShot(spinImpactSFX);
                }
                if (spinImpactVFX != null) {
                    Instantiate(spinImpactVFX, transform.position, transform.rotation);
                }
            }
        }

        // --- Helper Methods (Used by States) ---

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
        /// A public coroutine that flashes the material.
        /// Can be called by any state.
        /// </summary>
        public IEnumerator InvulnerableFlashRoutine()
        {
            if (enableDebugLogs) Debug.Log($"[BossContext] Started InvulnerableFlashRoutine.");
            IsFlashing = true;

            foreach (var r in bossRenderers)
            {
                r.material.EnableKeyword("_EMISSION");
                r.material.SetColor(EmissionColor, Color.white);
            }

            yield return new WaitForSeconds(0.1f);

            foreach (var r in bossRenderers)
            {
                r.material.SetColor(EmissionColor, Color.black);
            }

            IsFlashing = false;
        }
        /// <summary>
        /// Coroutine para piscar o chefe (enquanto vulnerável).
        /// </summary>
        public IEnumerator VulnerableFlashLoop()
        {
            if (enableDebugLogs) Debug.Log($"[BossContext] Started VulnerableFlashLoop.");
            while (true)
            {
                // Liga o flash
                foreach (var r in bossRenderers)
                {
                    r.material.EnableKeyword("_EMISSION");
                    r.material.SetColor(EmissionColor, vulnerableColor);
                }
                yield return new WaitForSeconds(vulnerableFlashRate);
                
                // Desliga o flash
                ResetFlashMaterial();
                yield return new WaitForSeconds(vulnerableFlashRate);
            }
        }
        /// <summary>
        /// Reseta o material de flash para o padrão (preto).
        /// </summary>
        public void ResetFlashMaterial()
        {
            foreach (var r in bossRenderers)
            {
                r.material.SetColor(EmissionColor, Color.black);
            }
        }

        /// <summary>
        /// Public method for the Defeated state to invoke the event.
        /// </summary>
        public void InvokeOnBossDefeated()
        {
            if (enableDebugLogs) Debug.Log($"[BossContext] Invoking OnBossDefeated event.");
            OnBossDefeated?.Invoke();
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