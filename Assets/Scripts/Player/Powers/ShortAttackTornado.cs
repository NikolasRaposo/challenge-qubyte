using System.Collections;
using UnityEngine;
using Managers;

namespace Player.Powers
{
    public class ShortAttackTornado : MonoBehaviour
    {
        [Header("Referências")]
        [Tooltip("O componente Animator do personagem. Pode ser arrastado aqui ou será pego automaticamente.")]
        public Animator animator; 
        [Tooltip("Arraste aqui o GameObject FILHO que contém o efeito de tornado.")]
        public GameObject objetoEfeitoTornado; // Nome alterado para maior clareza

        [Header("Configurações do Ataque")]
        [Tooltip("Duração em segundos que o efeito do tornado permanecerá ativo.")]
        public float duracaoEfeito = 2.0f;
        [Tooltip("O tempo mínimo em segundos entre cada ataque.")]
        public float cooldown = 1.5f;
        
        // ADICIONE ESTA LINHA ABAIXO
        [Tooltip("O script de trigger que está no objeto de efeito do tornado.")]
        public TornadoTrigger tornadoTrigger; 

        private float _lastAttackTime = -Mathf.Infinity;

        private void Awake()
        {
            // Se o Animator não foi arrastado no Inspector, tenta pegá-lo no mesmo GameObject.
            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }

            if (animator == null)
            {
                Debug.LogError("Componente Animator NÃO ENCONTRADO! O ataque não funcionará.", this.gameObject);
            }
            
           
        }
        
        private void Start()
        {
            if (InputManager.Instance != null)
            {
                InputManager.Instance.OnTornado += HandleAttackInput;
            }
            else
            {
                Debug.LogError("InputManager.Instance não encontrado! O input de ataque não funcionará.");
            }
            
            if (objetoEfeitoTornado != null)
            {
                // Garante que o efeito comece desativado, como você pediu.
                objetoEfeitoTornado.SetActive(false);
            }
        }

        private void HandleAttackInput()
        {
            // DEBUG: Isso aparecerá no console se o evento do InputManager estiver funcionando.
            Debug.Log("Input de Tornado Recebido!");

            if (Time.time < _lastAttackTime + cooldown)
            {
                return; // Em cooldown
            }

            // DEBUG: Isso aparecerá se o cooldown passou e o ataque vai começar.
            Debug.Log("Cooldown OK. Executando PerformAttack...");
            PerformAttack();
            
            _lastAttackTime = Time.time;
        }

        private void PerformAttack()
        {
            if (animator == null || objetoEfeitoTornado == null) return;

            // DEBUG: A linha final antes de ativar o trigger.
            Debug.Log("Disparando gatilho 'Attack' no Animator!", animator.gameObject);
            animator.SetTrigger("Attack");

            StartCoroutine(TornadoEffectCoroutine());
        }

        private IEnumerator TornadoEffectCoroutine()
        {
            // ADICIONE ESTA LINHA PARA LIMPAR OS ALVOS DO ATAQUE ANTERIOR
            tornadoTrigger.ResetHitTargets();
            
            // Ativa o objeto filho
            objetoEfeitoTornado.SetActive(true);
            
            // Espera pela duração do EFEITO
            yield return new WaitForSeconds(duracaoEfeito);

            // Desativa o objeto filho
            objetoEfeitoTornado.SetActive(false);
        }

        private void OnDestroy()
        {
            if (InputManager.Instance != null)
            {
                InputManager.Instance.OnTornado -= HandleAttackInput;
            }
        }
    }
}