using Managers;
using Player;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace Enemy
{
    /// <summary>
    /// Inimigo de longa distância (Macaco) que fica parado e arremessa projéteis.
    /// Dispara o projétil usando um Evento de Animação.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(CapsuleCollider))]
    [RequireComponent(typeof(Rigidbody))]
    public class RangedEnemy : MonoBehaviour
    {
        [Header("AI Settings")]
        [Tooltip("O Transform do jogador. Se nulo, tentará encontrar a tag 'Player'.")]
        [SerializeField] private Transform playerTarget;
        
        [Tooltip("A que distância o inimigo começa a atacar.")]
        [SerializeField] private float attackRange = 15f;
        
        [Tooltip("Tempo (em segundos) entre cada arremesso.")]
        [SerializeField] private float attackCooldown = 3f;

        [Header("Attack Config")]
        [Tooltip("O prefab do projétil que será arremessado.")]
        [SerializeField] private GameObject projectilePrefab;
        
        [Tooltip("O ponto de onde o projétil é disparado (ex: a mão do Macaco).")]
        [SerializeField] private Transform firePoint;
        
        // CAMPO REMOVIDO:
        // [SerializeField] private float animationWindUpTime = 0.5f;

        [Header("Effects")]
        [Tooltip("VFX ao ser derrotado.")]
        public GameObject deathVFX;
        [Tooltip("SFX ao ser derrotado.")]
        public AudioClip deathSFX;

        // --- Componentes e Estado ---
        private Animator _animator;
        private Collider _collider;
        private Rigidbody _rigidbody;
        private bool _isOnCooldown = false; // Flag para gerenciar o cooldown
        private bool _isDefeated = false;
        private PlayerHealth _playerHealth;

        // --- Animator Hashes ---
        // Você terá 3 estados: Idle, PreAttack, Attack
        private static readonly int PreAttack = Animator.StringToHash("PreAttack");

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            _collider = GetComponent<Collider>();
            _rigidbody = GetComponent<Rigidbody>();
            
            _rigidbody.isKinematic = true;
            _rigidbody.useGravity = false;

            if (playerTarget == null)
            {
                GameObject player = GameObject.FindWithTag("Player");
                if (player != null) playerTarget = player.transform;
            }
            if (playerTarget != null)
            {
                _playerHealth = playerTarget.GetComponent<PlayerHealth>();
            }

            if (firePoint == null)
            {
                firePoint = transform;
            }
        }

        private void Update()
        {
            // Se estiver morto, em cooldown, sem alvo ou se o jogador estiver morto, não faz nada
            if (_isDefeated || _isOnCooldown || playerTarget == null || (_playerHealth != null && _playerHealth.IsDead))
            {
                return;
            }

            // Verifica se o jogador está ao alcance
            float distance = Vector3.Distance(transform.position, playerTarget.position);
            if (distance <= attackRange)
            {
                // Inicia a sequência de ataque
                StartAttackSequence();
            }
        }

        private void StartAttackSequence()
        {
            // 1. Entra em cooldown
            _isOnCooldown = true;

            // 2. Vira para o Jogador
            Vector3 directionToPlayer = (playerTarget.position - transform.position).normalized;
            Quaternion lookRotation = Quaternion.LookRotation(new Vector3(directionToPlayer.x, 0, directionToPlayer.z));
            transform.rotation = lookRotation; 

            // 3. Dispara o gatilho da animação "PreAttack"
            // O Animator vai cuidar de ir de "PreAttack" para "Attack"
            _animator.SetTrigger(PreAttack);
            
            // 4. Inicia a contagem do cooldown
            StartCoroutine(CooldownCoroutine());
        }

        /// <summary>
        /// ESTA FUNÇÃO É CHAMADA PELO EVENTO DE ANIMAÇÃO
        /// (Coloque este evento no frame da animação 'Attack' onde o braço está esticado)
        /// </summary>
        public void AnimationEvent_FireProjectile()
        {
            if (projectilePrefab == null || _isDefeated || (_playerHealth != null && _playerHealth.IsDead)) return;
            
            Vector3 fireDirection;
            if (playerTarget != null)
            {
                // Mira no centro do jogador (Y=1.0f) no momento do disparo
                fireDirection = (playerTarget.position + Vector3.up * 1.0f - firePoint.position).normalized;
            }
            else
            {
                fireDirection = transform.forward; // Dispara reto se o alvo sumir
            }
            
            // Rotação exata para o projétil
            Quaternion fireRotation = Quaternion.LookRotation(fireDirection);

            // Spawna o projétil
            GameObject projectileGO = Instantiate(projectilePrefab, firePoint.position, fireRotation);
            
            // Configura o projétil (passa a si mesmo como o "atirador")
            Projectile projectileScript = projectileGO.GetComponent<Projectile>();
            if (projectileScript != null)
            {
                projectileScript.Initialize(this); 
            }
        }

        /// <summary>
        /// Coroutine simples para controlar o tempo entre ataques.
        /// </summary>
        private IEnumerator CooldownCoroutine()
        {
            yield return new WaitForSeconds(attackCooldown);
            _isOnCooldown = false; // Permite um novo ataque
        }

        public void Defeat()
        {
            if (_isDefeated) return;
            _isDefeated = true;
            _isOnCooldown = true; // Garante que não vai tentar atacar enquanto morre
            
            StopAllCoroutines(); // Para o CooldownCoroutine

            // Efeitos e destruição
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

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }
    }
}