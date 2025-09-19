using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MasterStylizedProjectile
{
    [System.Serializable]
    public class EffectsGroup
    {
        public string EffectName;
        public float Speed = 20;
        public ParticleSystem ChargeParticles;
        public float ChargeParticleTime;
        public AudioClip ChargeClip;
        public ParticleSystem StartParticles;
        public ParticleSystem BulletParticles;
        public ParticleSystem HitParticles;
        public AudioClip startClip;
        public AudioClip bulletClip;
        public AudioClip hitClip;
        public bool isTargeting;
        public float RotSpeed;
    }
    
    /// <summary>
    /// Sistema de ataque automático que dispara projéteis contra o player
    /// </summary>
    public class BulletShooter : MonoBehaviour
    {
        [Header("Configurações de Ataque")]
        [Tooltip("Dados dos efeitos de projéteis disponíveis")]
        public BulletDatas datas;
        
        [Tooltip("Índice do efeito atual a ser usado")]
        public int Index = 0;
        
        [Tooltip("Transform de onde os projéteis são disparados")]
        public Transform StartNodeTrans;
        
        [Header("Configurações de Tempo")]
        [Tooltip("Intervalo entre ataques em segundos")]
        public float attackInterval = 2f;
        
        [Tooltip("Variação aleatória no intervalo de ataque (±)")]
        [Range(0f, 1f)]
        public float attackIntervalVariation = 0.3f;
        
        [Header("Configurações de Precisão")]
        [Tooltip("Precisão do disparo (0 = impreciso, 1 = perfeito)")]
        [Range(0f, 1f)]
        public float accuracy = 0.8f;
        
        [Tooltip("Raio máximo de dispersão em unidades")]
        public float maxSpreadRadius = 2f;
        
        [Header("Configurações de Alvo")]
        [Tooltip("Tag do player para busca automática")]
        public string playerTag = "Player";
        
        [Tooltip("Distância máxima para detectar o player")]
        public float detectionRange = 20f;
        
        [Header("Configurações Visuais")]
        [Tooltip("Ativar/desativar efeitos de carregamento")]
        public bool useChargeEffects = true;
        
        [Tooltip("Ativar/desativar efeitos de início")]
        public bool useStartEffects = true;
        
        [Tooltip("Raio do colisor dos projéteis")]
        public float bulletColliderRadius = 0.6f;
        
        [Header("Configurações de Colisão")]
        [Tooltip("Layer do collider que o projétil deve colidir (collider com tag TriggerDamage)")]
        public LayerMask targetLayer = -1;
        
        // Propriedades privadas
        public EffectsGroup CurEffect => datas != null && datas.Effects.Count > Index ? datas.Effects[Index] : null;
        private Transform playerTransform;
        private float lastAttackTime;
        private float nextAttackTime;
        private bool isAttacking = false;
        void Start()
        {
            // Inicializa o sistema de ataque
            CalculateNextAttackTime();
            FindPlayer();
        }

        void Update()
        {
            // Verifica se é hora de atacar
            if (Time.time >= nextAttackTime && !isAttacking)
            {
                // Procura o player se não foi encontrado ainda
                if (playerTransform == null)
                {
                    FindPlayer();
                }
                
                // Ataca se o player estiver no alcance
                if (CanAttackPlayer())
                {
                    StartCoroutine(AttackSequence());
                }
                else
                {
                    // Se não pode atacar, agenda próximo ataque
                    CalculateNextAttackTime();
                }
            }
        }
        
        /// <summary>
        /// Procura o player na cena pela tag
        /// </summary>
        private void FindPlayer()
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag(playerTag);
            if (playerObj != null)
            {
                playerTransform = playerObj.transform;
            }
        }
        
        /// <summary>
        /// Verifica se pode atacar o player (se está no alcance)
        /// </summary>
        private bool CanAttackPlayer()
        {
            if (playerTransform == null || StartNodeTrans == null)
                return false;
                
            float distanceToPlayer = Vector3.Distance(StartNodeTrans.position, playerTransform.position);
            return distanceToPlayer <= detectionRange;
        }
        
        /// <summary>
        /// Calcula o próximo tempo de ataque com variação aleatória
        /// </summary>
        private void CalculateNextAttackTime()
        {
            float variation = Random.Range(-attackIntervalVariation, attackIntervalVariation);
            float actualInterval = attackInterval + (attackInterval * variation);
            nextAttackTime = Time.time + Mathf.Max(0.1f, actualInterval);
        }
        
        /// <summary>
        /// Sequência completa de ataque (carregamento + disparo)
        /// </summary>
        private IEnumerator AttackSequence()
        {
            isAttacking = true;
            
            // Fase de carregamento
            if (useChargeEffects)
            {
                yield return StartCoroutine(Charge());
            }
            
            // Disparo
            DoShoot();
            
            // Agenda próximo ataque
            CalculateNextAttackTime();
            isAttacking = false;
        }
        /// <summary>
        /// Método público para disparo manual (mantido para compatibilidade)
        /// </summary>
        public void Shoot()
        {
            if (!isAttacking)
            {
                StartCoroutine(AttackSequence());
            }
        }
        /// <summary>
        /// Executa a fase de carregamento do ataque
        /// </summary>
        public IEnumerator Charge()
        {
            if (CurEffect?.ChargeParticles != null && useChargeEffects)
            {
                var ChargePar = Instantiate(CurEffect.ChargeParticles, StartNodeTrans.position, Quaternion.identity);
                
                // Reproduz áudio de carregamento
                if (CurEffect.ChargeClip != null)
                {
                    GameObject AudioObj = new GameObject("ChargeAudio");
                    var audiosource = AudioObj.AddComponent<AudioSource>();
                    audiosource.clip = CurEffect.ChargeClip;
                    audiosource.Play();
                    
                    // Destroi o objeto de áudio após o clip terminar
                    Destroy(AudioObj, CurEffect.ChargeClip.length + 0.1f);
                }
                
                yield return new WaitForSeconds(CurEffect.ChargeParticleTime);
                
                if (ChargePar != null)
                {
                    Destroy(ChargePar.gameObject);
                }
            }
        }
        /// <summary>
        /// Executa o disparo do projétil com mira automática no player
        /// </summary>
        public void DoShoot()
        {
            if (CurEffect == null || StartNodeTrans == null)
                return;
                
            // Calcula direção para o player com variação de precisão
            Vector3 targetDir = GetPlayerTargetDirection();
            
            // Efeitos de início do disparo
            if (CurEffect.StartParticles != null && useStartEffects)
            {
                var StartPar = Instantiate(CurEffect.StartParticles, StartNodeTrans.position, Quaternion.identity);
                StartPar.transform.forward = targetDir;

                var onStart = StartPar.gameObject.AddComponent<AudioTrigger>();
                if (CurEffect.startClip != null)
                {
                    onStart.onClip = CurEffect.startClip;
                }
            }
            
            // Cria o projétil
            if (CurEffect.BulletParticles != null)
            {
                var bulletObj = Instantiate(CurEffect.BulletParticles, StartNodeTrans.position, Quaternion.identity);
                bulletObj.transform.forward = targetDir;

                var bullet = bulletObj.gameObject.AddComponent<Bullet>();
                bullet.OnHitEffect = CurEffect.HitParticles;
                bullet.Speed = CurEffect.Speed;
                bullet.isTargeting = CurEffect.isTargeting;
                
                // Configuração de targeting automático para o player
                if (CurEffect.isTargeting && playerTransform != null)
                {
                    bullet.rotSpeed = CurEffect.RotSpeed;
                    bullet.target = playerTransform;
                }

                // Configuração de áudio
                if (CurEffect.hitClip != null)
                {
                    bullet.onHitClip = CurEffect.hitClip;
                }
                if (CurEffect.bulletClip != null)
                {
                    bullet.bulletClip = CurEffect.bulletClip;
                }

                // Configuração do colisor
                var collider = bulletObj.gameObject.AddComponent<SphereCollider>();
                collider.isTrigger = true;
                collider.radius = bulletColliderRadius;
            }
        }
        
        /// <summary>
        /// Calcula a direção para o player com variação de precisão
        /// </summary>
        private Vector3 GetPlayerTargetDirection()
        {
            if (playerTransform == null || StartNodeTrans == null)
            {
                return transform.forward; // Direção padrão se não há player
            }
            
            Vector3 baseDirection = (playerTransform.position - StartNodeTrans.position).normalized;
            
            // Aplica variação de precisão
            if (accuracy < 1f)
            {
                float inaccuracy = 1f - accuracy;
                Vector3 randomOffset = Random.insideUnitSphere * (maxSpreadRadius * inaccuracy);
                randomOffset.y *= 0.5f; // Reduz variação vertical
                
                Vector3 targetPoint = playerTransform.position + randomOffset;
                baseDirection = (targetPoint - StartNodeTrans.position).normalized;
            }
            
            return baseDirection;
        }


        /// <summary>
        /// Encontra o objeto mais próximo com a tag especificada
        /// </summary>
        public GameObject FindNearestTarget(string tag)
        {
            var gameObjects = GameObject.FindGameObjectsWithTag(tag).ToList().OrderBy(
                (x) => Vector3.Distance(transform.position, x.transform.position));
            return gameObjects.FirstOrDefault();
        }
        
        /// <summary>
        /// Ativa/desativa o sistema de ataque automático
        /// </summary>
        public void SetAutoAttackEnabled(bool enabled)
        {
            this.enabled = enabled;
            if (!enabled)
            {
                isAttacking = false;
            }
        }
        
        /// <summary>
        /// Força um ataque imediato se possível
        /// </summary>
        public void ForceAttack()
        {
            if (CanAttackPlayer() && !isAttacking)
            {
                StartCoroutine(AttackSequence());
            }
        }
        
        /// <summary>
        /// Obtém informações de debug do sistema
        /// </summary>
        public string GetDebugInfo()
        {
            if (playerTransform == null)
                return "Player não encontrado";
                
            float distance = Vector3.Distance(StartNodeTrans.position, playerTransform.position);
            float timeToNextAttack = Mathf.Max(0, nextAttackTime - Time.time);
            
            return $"Distância: {distance:F1}m | Próximo ataque: {timeToNextAttack:F1}s | Atacando: {isAttacking}";
        }
    }

}
