using Managers;
using Player;
using UnityEngine;

namespace Enemy {
    /// <summary>
    /// Controls a simple enemy that patrols between two points and damages the player on contact.
    /// Can have its rotation locked to always face the same direction.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))] // Garante que o inimigo sempre tenha um Rigidbody
    public class SimpleEnemy : MonoBehaviour {
        [Header("Patrol Settings")]
        [Tooltip("The first point in the patrol path.")]
        public Transform patrolPointA;

        [Tooltip("The second point in the patrol path.")]
        public Transform patrolPointB;

        [Tooltip("The movement speed of the enemy.")]
        public float speed = 2f;

        [Tooltip("Se marcado, o inimigo n�o rotacionar�, mesmo que sofra colis�es f�sicas.")]
        public bool lockRotation = false; // <-- SUA NOVA OP��O AQUI
        
        [Header("Effects")]
        [Tooltip("The particle effect to spawn when defeated.")]
        public GameObject deathVFX;
        [Tooltip("The sound effect to play when defeated.")]
        public AudioClip deathSFX;

        private const float DistanceThreshold = 0.1f;

        private Transform _currentTarget;
        private bool _isDefeated = false;
        private Rigidbody _rigidbody;

        private void Awake() {
            // Pega a refer�ncia do Rigidbody para uso futuro.
            _rigidbody = GetComponent<Rigidbody>();
        }

        private void Start() {
            // Safety check to ensure patrol points are set
            if (patrolPointA == null || patrolPointB == null) {
                Debug.LogError("Patrol points are not set for this enemy!", this);
                enabled = false; // Disable the script if not configured
                return;
            }

            // APLICA A RESTRI��O DE ROTA��O AQUI
            if (lockRotation) {
                _rigidbody.constraints = RigidbodyConstraints.FreezeRotation;
            }
            
            // Start by moving towards Point B
            _currentTarget = patrolPointB;
        }

        private void Update() {
            if (_isDefeated) return;

            // Em vez de manipular transform.position, � melhor usar o Rigidbody para movimento f�sico.
            // Isso resulta em intera��es mais realistas e est�veis com outros objetos.
            Vector3 targetDirection = (_currentTarget.position - transform.position).normalized;
            Vector3 newVelocity = targetDirection * speed;
            _rigidbody.linearVelocity = new Vector3(newVelocity.x, _rigidbody.linearVelocity.y, newVelocity.z);
            
            // Check if we have reached the target
            if (!(Vector3.Distance(transform.position, _currentTarget.position) < DistanceThreshold)) return;

            // Switch target
            _currentTarget = _currentTarget == patrolPointB ? patrolPointA : patrolPointB;
        }

        // This function is called when this collider/rigidbody has begun touching another rigidbody/collider.
        private void OnCollisionEnter(Collision collision) {
            // If the enemy is already defeated, do nothing.
            if (_isDefeated) return;
            // Check if the object we collided with has the "Player" tag
            if (!collision.gameObject.CompareTag("Player")) return;
            // Try to get the PlayerHealth component from the collided object
            if (collision.gameObject.TryGetComponent(out PlayerHealth playerHealth)) {
                // Só chama Die() se o jogador ainda não estiver morto
                if (!playerHealth.IsDead) {
                    playerHealth.Die();
                }
            }
        }
        /// <summary>
        /// This is called by the PlayerController when the enemy is jumped on.
        /// </summary>
        public void Defeat() {
            _isDefeated = true;
            // Desativa o movimento f�sico ao ser derrotado
            _rigidbody.isKinematic = true;
            _rigidbody.linearVelocity = Vector3.zero;

            // Play visual and audio feedback.
            if (deathVFX != null) {
                Instantiate(deathVFX, transform.position, Quaternion.identity);
            }
            if (deathSFX != null) {
                AudioSource.PlayClipAtPoint(deathSFX, transform.position);
            }
            // Disable components to make the enemy disappear and stop interacting.
            GetComponent<Collider>().enabled = false;
            // Hides all renderers in children objects as well.
            foreach (Renderer enemyRenderer in GetComponentsInChildren<Renderer>())
            {
                enemyRenderer.enabled = false;
            }
            GameManager.Instance.IncrementDefeatedEnemies();
            // Destroy the GameObject after a short delay to allow effects to play.
            Destroy(gameObject, 2f);
        }
    }
}