using Player;
using UnityEngine;

namespace Enemy
{
    /// <summary>
    /// Controla um projétil arremessado.
    /// Move-se reto. Pode ser "rebatido" (Reflect).
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(Collider))]
    public class Projectile : MonoBehaviour
    {
        [Tooltip("Velocidade com que o projétil se move.")]
        [SerializeField] private float speed = 15f;
        
        [Tooltip("Prefab de VFX para quando o projétil é rebatido.")]
        [SerializeField] private GameObject reflectVFX;

        // --- Estado Interno ---
        private Rigidbody _rb;
        private RangedEnemy _originEnemy; // Quem atirou
        private bool _isReflected = false;
        
        private const float MaxLifetime = 10f;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.isKinematic = true; 
            _rb.useGravity = false;
            
            Collider col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
            
            Destroy(gameObject, MaxLifetime); 
        }

        /// <summary>
        /// Configura o projétil. Chamado pelo RangedEnemy.
        /// </summary>
        public void Initialize(RangedEnemy origin)
        {
            _originEnemy = origin;
            // O projétil já é instanciado na direção correta pelo RangedEnemy.
        }

        private void Update()
        {
            // Apenas move para frente
            transform.Translate(Vector3.forward * speed * Time.deltaTime);
        }

        /// <summary>
        /// Chamada pelo TornadoTrigger para rebater o projétil.
        /// </summary>
        public void Reflect()
        {
            if (_isReflected) return;

            Debug.LogWarning("PROJÉTIL REBATIDO!");
            _isReflected = true;
            
            // O novo alvo é quem nos atirou!
            // Gira 180 graus para voltar
            if (_originEnemy != null)
            {
                // Mira de volta no inimigo
                Vector3 directionToEnemy = (_originEnemy.transform.position + Vector3.up * 1.0f - transform.position).normalized;
                transform.rotation = Quaternion.LookRotation(directionToEnemy);
            }
            else
            {
                // Se o inimigo já morreu, apenas vira para trás
                transform.Rotate(0, 180, 0);
            }
            
            if (reflectVFX != null)
            {
                Instantiate(reflectVFX, transform.position, Quaternion.identity);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            // Se o projétil ainda NÃO foi rebatido
            if (!_isReflected)
            {
                if (other.CompareTag("Player"))
                {
                    if (other.TryGetComponent(out PlayerHealth playerHealth))
                    {
                        playerHealth.Die();
                    }
                    Destroy(gameObject);
                }
            }
            // Se o projétil FOI rebatido
            else
            {
                // Acertou o inimigo que o atirou?
                if (other.gameObject.TryGetComponent(out RangedEnemy enemy))
                {
                    if (enemy == _originEnemy) 
                    {
                        enemy.Defeat(); 
                        Destroy(gameObject); 
                    }
                }
            }
        }
    }
}