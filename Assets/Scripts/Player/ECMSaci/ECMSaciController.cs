using ECM.Common;
using ECM.Controllers;
using ECM.Helpers;
using UnityEngine;
using UnityEngine.InputSystem;
using ThirdParty.StarterAssets.InputSystem;

namespace Player
{
    // Controlador ECM para o Saci, integrando StarterAssetsInputs e animação
    public sealed class ECMSaciController : BaseCharacterController
    {
        [Header("Referências")]
        [Tooltip("Fonte de input (StarterAssetsInputs) do player.")]
        public StarterAssetsInputs inputs;

        // Referência de câmera para movimento relativo
        [Tooltip("Transform da câmera usada para movimento relativo.")]
        [SerializeField] private Transform playerCamera;
        
        // Gerenciador de modo de input (Player/UI)
        private InputModeManager _inputModeManager;

        [Header("Velocidades")]
        [SerializeField] private float _walkSpeed = 2.5f;
        [SerializeField] private float _runSpeed = 5.0f;

        [Header("Opções")]
        [SerializeField] private bool _useRootMotionController = false;

        [Header("Filtros")]
        [SerializeField] private float _speedDeadzone = 0.05f;
        [SerializeField] private float _verticalVelocityDeadzone = 0.01f;

        [Header("Rotação")]
        [Tooltip("Se verdadeiro, rotaciona o personagem para onde está indo.")]
        [SerializeField] private bool _rotateToMoveDirection = true;
        [Tooltip("Tempo de suavização (segundos) da rotação para a direção de movimento.")]
        [SerializeField] private float _rotationSmoothTime = 0.1f;
        private float _rotationYawVelocity = 0f;

        [Header("Pulo (Mid-Air)")]
        [Tooltip("Número máximo de pulos no ar (1 = double jump).")]
        [SerializeField] private int _maxMidAirJumpsLocal = 1;
        private int _prevMidAirJumpCount = 0;
        // Ajusta a velocidade alvo com base no estado (caminhando/correndo)
        protected override Vector3 CalcDesiredVelocity()
        {
            // Define a velocidade com base no sprint (StarterAssetsInputs)
            speed = (inputs != null && inputs.sprint) ? _runSpeed : _walkSpeed;
            return base.CalcDesiredVelocity();
        }

        // Atualiza parâmetros do Animator conforme guia Saci_ECM_Animator
        protected override void Animate()
        {
            if (animator == null)
                return;

            // Velocidade horizontal para o Blend Tree (Idle/Walk/Run)
            var horizontalVelocity = movement.velocity;
            horizontalVelocity.y = 0f;
            var speedValue = horizontalVelocity.magnitude;
            if (speedValue < _speedDeadzone) speedValue = 0f;
            animator.SetFloat("Speed", speedValue, 0.1f, Time.deltaTime);

            // Estados principais
            animator.SetBool("IsGrounded", movement.isGrounded);
            animator.SetBool("IsJumping", isJumping);

            // VerticalVelocity: no chão, force 0; no ar, aplique deadzone e envie imediato (sem damping)
            float vy = movement.isGrounded ? 0f : movement.velocity.y;
            if (Mathf.Abs(vy) < _verticalVelocityDeadzone)
                vy = 0f;
            animator.SetFloat("VerticalVelocity", vy);

            // Mid-air jump helpers para o Animator
            bool canDoubleJump = !movement.isGrounded && _midAirJumpCount < maxMidAirJumps;
            animator.SetBool("CanDoubleJump", canDoubleJump);
            animator.SetInteger("MidAirJumpCount", _midAirJumpCount);
            if (_midAirJumpCount > _prevMidAirJumpCount)
            {
                animator.SetTrigger("DoubleJump");
                _prevMidAirJumpCount = _midAirJumpCount;
            }
            if (movement.isGrounded)
                _prevMidAirJumpCount = _midAirJumpCount; // normalmente 0 ao aterrar

            // Placeholder para futuro: manter IsCrouching alinhado ao plano
            animator.SetBool("IsCrouching", isCrouching);
        }

        // Mapeia input direto do StarterAssetsInputs
        protected override void HandleInput()
        {
            // Toggle de pausa (opcional)
            if (Keyboard.current != null && Keyboard.current.pKey.wasPressedThisFrame)
                pause = !pause;

            // Gate de processamento de input baseado no InputModeManager
            if (_inputModeManager != null && !_inputModeManager.ShouldProcessGameplayInput())
            {
                moveDirection = Vector3.zero;
                jump = false;
                if (inputs != null) inputs.jump = false; // garante que não fique travado ao sair para UI
                return;
            }

            // Direção de movimento a partir do StarterAssetsInputs (relativa à câmera se disponível)
            Vector2 move = inputs != null ? inputs.move : Vector2.zero;
            Vector3 worldDir;
            if (playerCamera != null)
            {
                Vector3 camForward = playerCamera.forward; camForward.y = 0f; camForward.Normalize();
                Vector3 camRight = playerCamera.right;   camRight.y = 0f;   camRight.Normalize();
                worldDir = camRight * move.x + camForward * move.y;
            }
            else
            {
                worldDir = new Vector3(move.x, 0f, move.y);
            }
            moveDirection = worldDir;

            // Rotaciona para a direção de movimento (apenas quando não usando root motion)
            if (_rotateToMoveDirection && !useRootMotion)
            {
                Vector3 flatDir = worldDir; flatDir.y = 0f;
                if (flatDir.sqrMagnitude > 0.0001f)
                {
                    float targetYaw = Mathf.Atan2(flatDir.x, flatDir.z) * Mathf.Rad2Deg;
                    float currentYaw = transform.eulerAngles.y;
                    if (_rotationSmoothTime > 0f)
                    {
                        float newYaw = Mathf.SmoothDampAngle(currentYaw, targetYaw, ref _rotationYawVelocity, _rotationSmoothTime);
                        transform.rotation = Quaternion.Euler(0f, newYaw, 0f);
                    }
                    else
                    {
                        transform.rotation = Quaternion.Euler(0f, targetYaw, 0f);
                    }
                }
            }

            // Pular (consumir como trigger de um frame)
            jump = inputs != null && inputs.jump;
            if (inputs != null) inputs.jump = false; // consome o pulo para não ficar travado
        }

        // Inicialização: cacheia referências e configura RootMotion quando desejado
        public override void Awake()
        {
            base.Awake();

            if (inputs == null)
                inputs = GetComponent<StarterAssetsInputs>();

            _inputModeManager = GetComponent<InputModeManager>();

            // Inicializa câmera padrão se não atribuída
            if (playerCamera == null && Camera.main != null)
                playerCamera = Camera.main.transform;

            // Configura mid-air jumps (double jump)
            maxMidAirJumps = Mathf.Max(0, _maxMidAirJumpsLocal);
            _prevMidAirJumpCount = 0;

            // Sincroniza flag de root motion com a configuração local
            useRootMotion = _useRootMotionController;

            // Se root motion estiver habilitado, garanta a existência do RootMotionController
            if (useRootMotion && rootMotionController == null)
                rootMotionController = GetComponentInChildren<RootMotionController>();

            // Se o RootMotionController não foi encontrado, desabilitar root motion para evitar NullReference
            if (useRootMotion && rootMotionController == null)
            {
                useRootMotion = false;
                Debug.LogWarning($"{nameof(ECMSaciController)}: useRootMotion habilitado, mas RootMotionController não encontrado. Desabilitando root motion.");
            }
        }

        // Validação de campos expostos
        public override void OnValidate()
        {
            base.OnValidate();

            _walkSpeed = Mathf.Max(0f, _walkSpeed);
            _runSpeed = Mathf.Max(_walkSpeed, _runSpeed);

            // Mantém a flag do ECM sincronizada com a configuração local
            useRootMotion = _useRootMotionController;

            // Aplica configuração local de mid-air jumps
            maxMidAirJumps = Mathf.Max(0, _maxMidAirJumpsLocal);
        }
    }
}