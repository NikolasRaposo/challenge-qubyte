using Managers;
using Player;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace Enemy {
    /// <summary>
    /// Controla o "Sapo" que persegue, prepara, e pula.
    /// USA UMA HITBOX SEPARADA para dar dano.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(CapsuleCollider))]
    [RequireComponent(typeof(Rigidbody))]
    public class ChasingEnemy : MonoBehaviour {
        
        // ... (Campos de AI Settings, Attack Settings) ...
        [Header("AI Settings")]
        [Tooltip("O Transform do jogador. Se nulo, tentará encontrar a tag 'Player'.")]
        [SerializeField] private Transform playerTarget;
        [SerializeField] private float chaseSpeed = 3.5f;

        [Header("Detection Settings")]
        [Tooltip("Distância máxima para começar a perseguir o jogador.")]
        [SerializeField] private float detectionRadius = 10f;

        [Header("Attack Settings")]
        [Tooltip("A que distância do player o inimigo para e se prepara para pular.")]
        [SerializeField] private float attackStopDistance = 1.5f;
        [Tooltip("A distância total que o inimigo pula para frente no ataque.")]
        [SerializeField] private float lungeDistance = 3f;
        [Tooltip("A duração (em segundos) do pulo de ataque (Lunge).")]
        [SerializeField] private float lungeDuration = 0.5f;
        [Tooltip("O tempo (em segundos) que o inimigo espera *antes* de pular (preparação).")]
        [SerializeField] private float prepareDuration = 0.7f;
        [Tooltip("O tempo (em segundos) que o inimigo espera *depois* de pular, antes de perseguir de novo.")]
        [SerializeField] private float attackCooldown = 1.5f;

        [Header("Referências de Ataque")] // <-- MUDANÇA
        [Tooltip("Arraste o GameObject 'filho' (Hitbox) que contém o script EnemyHitbox.")]
        [SerializeField] private EnemyHitbox attackHitbox; // <-- MUDANÇA

        [Header("Effects")]
        [Tooltip("VFX ao ser derrotado.")]
        public GameObject deathVFX;
        [Tooltip("SFX ao ser derrotado.")]
        public AudioClip deathSFX;
        
        // --- Referências de Componentes ---
        private NavMeshAgent _navAgent;
        private Animator _animator;
        private Collider _collider;
        private Rigidbody _rigidbody;
        private PlayerHealth _playerHealth;

        // --- Flags de Estado ---
        private bool _isAttacking = false;
        private bool _isDefeated = false;
        private Coroutine _attackCoroutine;
        private bool _lungePauseFlag = false;

        public bool IsPreparingAttack { get; private set; }
        public bool IsVulnerable { get; private set; } 

        // --- Animator Hashes ---
        private static readonly int Attack = Animator.StringToHash("Attack");

        private void Awake() {
            _navAgent = GetComponent<NavMeshAgent>();
            _animator = GetComponent<Animator>();
            _collider = GetComponent<Collider>();
            _rigidbody = GetComponent<Rigidbody>(); 

            _rigidbody.isKinematic = true; 
            _rigidbody.useGravity = false;
            _rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;


            if (playerTarget == null) {
                GameObject player = GameObject.FindWithTag("Player");
                if (player != null) playerTarget = player.transform;
            }
            if (playerTarget != null) {
                _playerHealth = playerTarget.GetComponent<PlayerHealth>();
            }

            // Garante que a Hitbox está desligada no começo
            if (attackHitbox == null) {
                Debug.LogError("Attack Hitbox não foi assignada no ChasingEnemy!", this);
            } else {
                attackHitbox.gameObject.SetActive(false);
            }
        }

        private void OnEnable()
        {
            if (Managers.GameManager.Instance != null)
            {
                Managers.GameManager.Instance.OnPlayerDied += HandlePlayerDied;
                Managers.GameManager.Instance.OnPlayerRespawn += HandlePlayerRespawn;
            }
        }

        private void OnDisable()
        {
            if (Managers.GameManager.Instance != null)
            {
                Managers.GameManager.Instance.OnPlayerDied -= HandlePlayerDied;
                Managers.GameManager.Instance.OnPlayerRespawn -= HandlePlayerRespawn;
            }
        }

        private void Start() {
            _navAgent.speed = chaseSpeed;
            _navAgent.stoppingDistance = attackStopDistance;
            if (_animator != null) _animator.speed = 1.0f;
        }

        private void Update() {
            // Pausa lógica se o inimigo estiver derrotado, já atacando, sem alvo ou se o jogador estiver morto
            if (_isDefeated || _isAttacking || playerTarget == null || (_playerHealth != null && _playerHealth.IsDead)) {
                if (_navAgent.enabled && !_isAttacking) {
                    _navAgent.isStopped = true;
                }
                return;
            }

            // Só persegue se dentro do raio de detecção
            float sqrDist = (playerTarget.position - transform.position).sqrMagnitude;
            float sqrDetect = detectionRadius * detectionRadius;
            if (sqrDist > sqrDetect)
            {
                if (_navAgent.enabled) _navAgent.isStopped = true;
                return;
            }

            if (_navAgent.enabled) _navAgent.isStopped = false;
            _navAgent.SetDestination(playerTarget.position);
            if (!_navAgent.pathPending && _navAgent.remainingDistance <= _navAgent.stoppingDistance)
            {
                _attackCoroutine = StartCoroutine(AttackCoroutine());
            }
        }

        private IEnumerator AttackCoroutine()
        {
            _isAttacking = true;
            _navAgent.isStopped = true; 

            Vector3 directionToPlayer = (playerTarget.position - transform.position).normalized;
            Quaternion lookRotation = Quaternion.LookRotation(new Vector3(directionToPlayer.x, 0, directionToPlayer.z));
            transform.rotation = lookRotation; 

            // --- JANELA DE PREPARAÇÃO ---
            IsPreparingAttack = true;
            yield return new WaitForSeconds(prepareDuration);
            IsPreparingAttack = false;

            if (_isDefeated || (_playerHealth != null && _playerHealth.IsDead)) { _animator.speed = 1.0f; _isAttacking = false; _attackCoroutine = null; yield break; }

            // --- JANELA DE PULO (LUNGE) ---
            IsVulnerable = true; 
            _lungePauseFlag = false;
            _animator.SetTrigger(Attack);
            
            // Aguarda o momento do pulo ou aborta se morrer
            yield return new WaitUntil(() => _lungePauseFlag || _isDefeated || (_playerHealth != null && _playerHealth.IsDead));

            if (_isDefeated || (_playerHealth != null && _playerHealth.IsDead)) { _animator.speed = 1.0f; _isAttacking = false; _attackCoroutine = null; yield break; }

            // *** MUDANÇA: Ativa a Hitbox ANTES de mover ***
            attackHitbox.ResetHitbox(); // Limpa a lista de "já atingidos"
            attackHitbox.gameObject.SetActive(true);

            // 4. Move o personagem
            Vector3 startPos = transform.position;
            Vector3 targetPos = transform.position + transform.forward * lungeDistance;
            
            float timeElapsed = 0f;
            while (timeElapsed < lungeDuration)
            {
                transform.position = Vector3.Lerp(startPos, targetPos, timeElapsed / lungeDuration);
                timeElapsed += Time.deltaTime;
                yield return null; 
            }
            transform.position = targetPos; 
            
            // --- FIM DO PULO ---
            IsVulnerable = false; 

            // *** MUDANÇA: Desativa a Hitbox DEPOIS de mover ***
            attackHitbox.gameObject.SetActive(false);
            
            // 5. RETOMA a animação
            _animator.speed = 1.0f;

            yield return new WaitForSeconds(attackCooldown);

            _navAgent.isStopped = false;
            _isAttacking = false;
            _attackCoroutine = null;
        }

        private void HandlePlayerDied()
        {
            // Interrompe ataque, desativa hitbox e para o navmesh ao morrer o jogador
            if (_attackCoroutine != null)
            {
                StopCoroutine(_attackCoroutine);
                _attackCoroutine = null;
            }
            IsPreparingAttack = false;
            IsVulnerable = false;
            _isAttacking = false;
            if (attackHitbox != null) attackHitbox.gameObject.SetActive(false);
            if (_navAgent.enabled) _navAgent.isStopped = true;
            if (_animator != null) _animator.speed = 1.0f; // garante estado consistente
        }

        private void HandlePlayerRespawn()
        {
            if (_navAgent.enabled) _navAgent.isStopped = false;
        }

        public void AnimationEvent_PauseAndLunge()
        {
            _animator.speed = 0f;
            _lungePauseFlag = true;
        }

        public void Defeat() {
            // ... (Defeat não muda)
            if (_isDefeated) return; 
            _isDefeated = true;
            IsVulnerable = false; 
            IsPreparingAttack = false;
            _animator.speed = 1.0f; 

            if (_attackCoroutine != null) {
                StopCoroutine(_attackCoroutine);
            }
            
            // *** MUDANÇA: Garante que a hitbox desligue ao morrer ***
            if (attackHitbox != null) {
                attackHitbox.gameObject.SetActive(false);
            }

            if(_navAgent.isOnNavMesh) {
                _navAgent.isStopped = true;
            }
            _navAgent.enabled = false;
            
            if (deathVFX != null) {
                Instantiate(deathVFX, transform.position, Quaternion.identity);
            }
            if (deathSFX != null) {
                AudioSource.PlayClipAtPoint(deathSFX, transform.position);
            }
            
            _collider.enabled = false;
            _rigidbody.isKinematic = true; 
            
            foreach (Renderer r in GetComponentsInChildren<Renderer>()) {
                r.enabled = false;
            }
            
            GameManager.Instance.IncrementDefeatedEnemies();
            Destroy(gameObject, 2f);
        }

        private void OnDrawGizmosSelected() {
            // ... (Gizmos não mudam)
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, detectionRadius);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, attackStopDistance);
            Gizmos.color = Color.red;
            Vector3 lungeStart = transform.position;
            Vector3 lungeEnd = transform.position + (transform.forward * lungeDistance);
            Gizmos.DrawLine(lungeStart, lungeEnd);
            Gizmos.DrawWireSphere(lungeEnd, 0.2f); 
        }
    }
}