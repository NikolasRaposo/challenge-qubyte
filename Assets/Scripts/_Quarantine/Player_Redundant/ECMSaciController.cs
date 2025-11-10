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

        [Header("Movimento (Suavização)")]
        [Tooltip("Tempo de suavização do input de movimento (segundos).")]
        [SerializeField] private float _moveInputSmoothTime = 0.08f;
        private Vector3 _smoothedMoveDirection = Vector3.zero;
        private Vector3 _moveInputVelocity = Vector3.zero;

        [Header("Queda (Free Fall)")]
        [Tooltip("Aplica um pequeno atraso antes de marcar FreeFall.")]
        [SerializeField] private bool _useFallTimeout = true;
        [Tooltip("Tempo para entrar em FreeFall ao sair do chão.")]
        [SerializeField] private float _fallTimeout = 0.15f;
        private float _fallTimeoutDelta = 0f;

        [Header("Pulo (Mid-Air)")]
        [Tooltip("Número máximo de pulos no ar (1 = double jump).")]
        [SerializeField] private int _maxMidAirJumpsLocal = 1;
        private int _prevMidAirJumpCount = 0;

        [Header("Pulo (Input Buffer)")]
        [Tooltip("Tempo que um toque de pulo fica em buffer para ser consumido no FixedUpdate.")]
        [SerializeField] private float _jumpInputBufferTime = 0.1f;
        private float _jumpInputBufferTimer = 0f;

        [Header("Pulo (Supressão)")]
        [Tooltip("Quando verdadeiro, suprime completamente o processamento de pulo/jump enquanto ativo.")]
        [SerializeField] private bool _suppressJumpInput = false;

        [Header("Pulo (Tolerâncias)")]
        [Tooltip("Janela para pressionar pulo antes de encostar no chão e ainda validar.")]
        [SerializeField] private float _jumpPreGroundedToleranceLocal = 0.35f;
        [Tooltip("Janela para pressionar pulo após sair do chão (coyote time).")]
        [SerializeField] private float _jumpPostGroundedToleranceLocal = 0.2f;

        [Header("Pulo (Alturas)")]
        [Tooltip("Altura base do primeiro pulo (m).")]
        [SerializeField] private float _groundJumpHeight = 1.8f;
        [Tooltip("Altura base do pulo no ar (m).")]
        [SerializeField] private float _midAirJumpHeight = 1.5f;

        [Header("Modo Spline Path")]
        [Tooltip("Quando ativo, o Saci segue uma spline e desativa o movimento normal.")]
        [SerializeField] private bool _inSplinePathMode = false;

        [Header("Pulo (Cooldowns)")]
        [Tooltip("Tempo mínimo (s) entre pulo no chão e pulo duplo.")]
        [SerializeField] private float _midAirJumpCooldownAfterGroundJump = 0.3f;
        private float _lastGroundJumpTime = -9999f;

        // Métricas de impacto no chão (expostas para VFX e gameplay)
        private bool _prevGroundedRuntime;
        private float _prevVerticalSpeedRuntime;
        private float _ungroundedTimeRuntime;
        private float _lastGroundImpactDownwardSpeed;
        private float _lastGroundImpactTime;

        // Propriedades públicas de leitura
        public float GroundImpactDownwardSpeed => _lastGroundImpactDownwardSpeed;
        public float UngroundedTime => _ungroundedTimeRuntime;
        public float PrevVerticalSpeed => _prevVerticalSpeedRuntime;
        public float LastGroundImpactTime => _lastGroundImpactTime;

        [Header("Debug")]
        [Tooltip("Quando ligado, imprime logs de velocidade vertical e impactos no console.")]
        [SerializeField] private bool _logVerticalVelocityDebug = false;
        [Tooltip("Intervalo mínimo entre logs enquanto no ar (segundos).")]
        [SerializeField] private float _debugLogInterval = 0.25f;
        private float _nextDebugLogTime = 0f;
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

            // Runtime: métricas de impacto no chão e tempo no ar
            bool groundedNow = movement.isGrounded;
            if (!groundedNow)
            {
                _ungroundedTimeRuntime += Time.deltaTime;
            }
            // Transição ar -> chão: captura velocidade de impacto descendente
            if (!_prevGroundedRuntime && groundedNow)
            {
                float impactSpeedDown = _prevVerticalSpeedRuntime < 0f ? -_prevVerticalSpeedRuntime : 0f;
                _lastGroundImpactDownwardSpeed = impactSpeedDown;
                _lastGroundImpactTime = Time.time;
                _ungroundedTimeRuntime = 0f;

                // Fallback de segurança: ao tocar o chão, libere qualquer supressão externa de pulo
                if (_suppressJumpInput)
                    SetExternalJumpSuppression(false);

                if (_logVerticalVelocityDebug)
                {
                    Debug.Log($"[SaciDebug] Ground Impact | speedDown={_lastGroundImpactDownwardSpeed:F2} | sinceLastImpact={(Time.time - _lastGroundImpactTime):F2}");
                }
            }

            // FreeFall: precisa ser mais preciso que apenas sair do chão
            // Critério: no ar e movendo verticalmente (|vy| > deadzone). 
            // Comportamento:
            // - Se há impulso externo (não está em estado de jump), ativa FreeFall imediatamente.
            // - Caso contrário, aplica pequeno atraso (_fallTimeout) antes de marcar FreeFall.
            bool freeFall = false;
            if (!movement.isGrounded)
            {
                if (Mathf.Abs(vy) > _verticalVelocityDeadzone)
                {
                    if (_useFallTimeout)
                    {
                        // Bypass do atraso se não está no estado de jump (empurrão/impulso externo)
                        if (!isJumping)
                        {
                            freeFall = true;
                        }
                        else
                        {
                            if (_fallTimeoutDelta > 0f)
                                _fallTimeoutDelta -= Time.deltaTime;
                            else
                                freeFall = true;
                        }
                    }
                    else
                    {
                        freeFall = true;
                    }
                }
            }
            else
            {
                // Reset do atraso ao tocar o chão
                _fallTimeoutDelta = _fallTimeout;
            }
            animator.SetBool("FreeFall", freeFall);

            // Mid-air jump helpers para o Animator
            bool canDoubleJump = !movement.isGrounded && _midAirJumpCount < maxMidAirJumps
                                  && (Time.time - _lastGroundJumpTime) >= _midAirJumpCooldownAfterGroundJump;
            animator.SetBool("CanDoubleJump", canDoubleJump);
            animator.SetInteger("MidAirJumpCount", _midAirJumpCount);
            if (!movement.isGrounded && _midAirJumpCount > _prevMidAirJumpCount)
            {
                animator.SetTrigger("DoubleJump");
                _prevMidAirJumpCount = _midAirJumpCount;
            }
            if (movement.isGrounded)
            {
                _prevMidAirJumpCount = 0;
                animator.ResetTrigger("DoubleJump");
            }

            // Placeholder para futuro: manter IsCrouching alinhado ao plano
            animator.SetBool("IsCrouching", isCrouching);

            // Atualiza caches para próxima iteração
            _prevGroundedRuntime = groundedNow;
            _prevVerticalSpeedRuntime = movement.velocity.y;

            // Logs em runtime enquanto no ar (com throttle)
            if (_logVerticalVelocityDebug && !groundedNow && Time.time >= _nextDebugLogTime)
            {
                Debug.Log($"[SaciDebug] Airborne | vy={movement.velocity.y:F2} | ungroundedTime={_ungroundedTimeRuntime:F2}");
                _nextDebugLogTime = Time.time + Mathf.Max(0.05f, _debugLogInterval);
            }
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

            // Modo spline: congela movimento e pulo enquanto segue a animação da spline
            if (_inSplinePathMode)
            {
                moveDirection = Vector3.zero;
                jump = false;
                if (inputs != null) inputs.jump = false;
                return;
            }

            // Direção de movimento a partir do InputManager (fallback para StarterAssetsInputs se necessário)
            Vector2 move;
            if (Managers.InputManager.Instance != null)
                move = Managers.InputManager.Instance.Move;
            else
                move = inputs != null ? inputs.move : Vector2.zero;
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
            // Suaviza o input para reduzir tremedeiras quando não há root motion
            if (_moveInputSmoothTime > 0f)
                _smoothedMoveDirection = Vector3.SmoothDamp(_smoothedMoveDirection, worldDir, ref _moveInputVelocity, _moveInputSmoothTime);
            else
                _smoothedMoveDirection = worldDir;
            if (_smoothedMoveDirection.sqrMagnitude < 0.0004f)
                _smoothedMoveDirection = Vector3.zero;
            moveDirection = _smoothedMoveDirection;

            // Rotação aplicada em UpdateRotation (override) para evitar duplicidade

            // Pular com buffer para evitar perda entre Update/FixedUpdate e facilitar re-jumps ao tocar o chão
            if (_suppressJumpInput)
            {
                // Enquanto suprimido, zera input e buffer para impedir pulo/duplo-pulo
                if (inputs != null) inputs.jump = false;
                _jumpInputBufferTimer = 0f;
                jump = false;
            }
            else
            {
                // Lê o pulo de forma unificada: prioriza InputManager e mantém fallback para StarterAssetsInputs
                bool rawJump = false;
                if (Managers.InputManager.Instance != null)
                    rawJump = Managers.InputManager.Instance.Jump;
                else if (inputs != null)
                    rawJump = inputs.jump;

                if (rawJump)
                {
                    _jumpInputBufferTimer = _jumpInputBufferTime;
                    // Consome o pulo na fonte correspondente
                    if (Managers.InputManager.Instance != null)
                        Managers.InputManager.Instance.ConsumeJumpInput();
                    if (inputs != null)
                        inputs.jump = false;
                }
                jump = _jumpInputBufferTimer > 0f;
                if (_jumpInputBufferTimer > 0f)
                    _jumpInputBufferTimer -= Time.deltaTime;
            }
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

            _smoothedMoveDirection = Vector3.zero;
            _moveInputVelocity = Vector3.zero;

            // Configura mid-air jumps (double jump)
            maxMidAirJumps = Mathf.Max(0, _maxMidAirJumpsLocal);
            _prevMidAirJumpCount = 0;

            // Inicializa o atraso do FreeFall
            _fallTimeoutDelta = _fallTimeout;

            // Ajusta tolerâncias de pulo (pré/pós chão) para melhorar responsividade de re-jumps
            jumpPreGroundedToleranceTime = _jumpPreGroundedToleranceLocal;
            jumpPostGroundedToleranceTime = _jumpPostGroundedToleranceLocal;

            // Mantém a altura base do pulo do ECM alinhada ao pulo no chão
            baseJumpHeight = Mathf.Max(0f, _groundJumpHeight);

            // Ativa avisos do Animator para ajudar a detectar parâmetros faltantes
            if (animator != null)
                animator.logWarnings = true;

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

        // Entrar no modo spline: pausa o ECM e congela física via Pause()
        public void EnterSplinePathMode()
        {
            _inSplinePathMode = true;
            // Não restaurar velocidades ao sair (evita reintroduzir velocidade residual)
            restoreVelocityOnResume = false;
            pause = true;

            // Atualiza Animator para refletir modo spline
            if (animator != null)
                animator.SetBool("InSplineGameMode", true);
        }

        // Sair do modo spline: retoma o ECM via Pause(false)
        public void ExitSplinePathMode()
        {
            // Ao sair do modo spline, não restaurar a velocidade salva
            // para evitar reintroduzir velocidade residual da entrada
            restoreVelocityOnResume = false;
            _inSplinePathMode = false;
            pause = false;

            // Atualiza Animator para refletir saída do modo spline
            if (animator != null)
                animator.SetBool("InSplineGameMode", false);
        }

        // --- API pública para supressão/consumo de pulo ---
        // Ativa/desativa supressão de pulo por integradores externos (ex.: trampolim)
        public void SetExternalJumpSuppression(bool suppress)
        {
            _suppressJumpInput = suppress;
            if (suppress)
            {
                // Ao ativar, consome qualquer estado de pulo remanescente
                if (inputs != null) inputs.jump = false;
                _jumpInputBufferTimer = 0f;
                jump = false;
            }
        }

        // Consome imediatamente o input de pulo e limpa o buffer do controlador
        public void ClearJumpBufferAndConsumeInput()
        {
            if (inputs != null) inputs.jump = false;
            _jumpInputBufferTimer = 0f;
            jump = false;
        }

        // Permite reabilitar duplo-pulo imediatamente após impulsos externos (ex.: trampolim)
        public void ResetGroundJumpCooldown()
        {
            // Coloca lastGroundJump suficientemente no passado para não bloquear mid-air jump
            _lastGroundJumpTime = Time.time - (_midAirJumpCooldownAfterGroundJump + 1f);
        }

        // Restaura a disponibilidade de pulos no ar após impulsos externos (ex.: stomp/trampolim)
        public void ResetMidAirJumpCount()
        {
            _midAirJumpCount = 0;
            _prevMidAirJumpCount = 0;
            if (animator != null)
                animator.ResetTrigger("DoubleJump");
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

            // Clampa timeout de queda
            _fallTimeout = Mathf.Max(0f, _fallTimeout);

            // Aplica tolerâncias locais de pulo
            jumpPreGroundedToleranceTime = _jumpPreGroundedToleranceLocal;
            jumpPostGroundedToleranceTime = _jumpPostGroundedToleranceLocal;

            _moveInputSmoothTime = Mathf.Max(0f, _moveInputSmoothTime);

            // Clampa alturas de pulo e sincroniza o pulo no chão com o ECM
            _groundJumpHeight = Mathf.Max(0f, _groundJumpHeight);
            _midAirJumpHeight = Mathf.Max(0f, _midAirJumpHeight);
            baseJumpHeight = _groundJumpHeight;

            // Clampa cooldown para evitar valores negativos
            _midAirJumpCooldownAfterGroundJump = Mathf.Max(0f, _midAirJumpCooldownAfterGroundJump);

            // Ativa avisos do Animator para ajudar a detectar parâmetros faltantes
            if (animator != null)
                animator.logWarnings = true;

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

        protected override void UpdateRotation()
        {
            // Se usando root motion com rotação pelo animator, delega ao base
            if (useRootMotion && useRootMotionRotation)
            {
                base.UpdateRotation();
                return;
            }

            if (_rotateToMoveDirection && !useRootMotion)
            {
                Vector3 flatDir = moveDirection; flatDir.y = 0f;
                if (flatDir.sqrMagnitude > 0.0004f)
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
            else
            {
                base.UpdateRotation();
            }
        }

        private float GroundJumpImpulse => Mathf.Sqrt(2f * Mathf.Max(0f, _groundJumpHeight) * movement.gravity.magnitude);
        private float MidAirJumpImpulse  => Mathf.Sqrt(2f * Mathf.Max(0f, _midAirJumpHeight)  * movement.gravity.magnitude);

        // Usa impulso distinto para pulo no chão
        protected override void Jump()
        {
            if (isJumping)
            {
                if (!movement.wasGrounded && movement.isGrounded)
                    _isJumping = false;
            }

            if (movement.isGrounded)
                _jumpUngroundedTimer = 0.0f;
            else
                _jumpUngroundedTimer += Time.deltaTime;

            if (!_jump || !_canJump)
                return;

            if (_jumpButtonHeldDownTimer > jumpPreGroundedToleranceTime)
                return;

            if (!movement.isGrounded && _jumpUngroundedTimer > jumpPostGroundedToleranceTime)
                return;

            _canJump = false;
            _isJumping = true;
            _updateJumpTimer = true;

            _jumpUngroundedTimer = jumpPostGroundedToleranceTime;

            // Marca tempo do último pulo no chão para cooldown do pulo duplo
            _lastGroundJumpTime = Time.time;

            movement.ApplyVerticalImpulse(GroundJumpImpulse);
            movement.DisableGrounding();
        }

        // Usa impulso distinto para pulo no ar
        protected override void MidAirJump()
        {
            if (_midAirJumpCount > 0 && movement.isGrounded)
                _midAirJumpCount = 0;

            if (!_jump || !_canJump)
                return;

            if (movement.isGrounded)
                return;

            if (_midAirJumpCount >= maxMidAirJumps)
                return;

            // Respeita cooldown entre pulo no chão e pulo duplo
            if ((Time.time - _lastGroundJumpTime) < _midAirJumpCooldownAfterGroundJump)
                return;

            _midAirJumpCount++;

            _canJump = false;
            _isJumping = true;
            _updateJumpTimer = true;

            movement.ApplyVerticalImpulse(MidAirJumpImpulse);
            movement.DisableGrounding();
        }
    }
}