using ThirdParty.StarterAssets.InputSystem;
using UnityEngine;
using UnityEngine.InputSystem;
using ExampleScripts;

namespace ThirdParty.StarterAssets.ThirdPersonController.Scripts
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerInput))]
    public class ThirdPersonController : MonoBehaviour
    {
        #region Fields

        // --- Serialized Fields (Ajustáveis no Inspector) ---

        [Header("Player Movement")]
        [Tooltip("Move speed of the character in m/s")]
        [SerializeField] private float _moveSpeed = 5.0f;
        [Tooltip("How much control the player has in the air. 1.0 is full control, 0.1 is very little.")]
        [Range(0.0f, 1.0f)]
        [SerializeField] private float _airControlFactor = 0.5f;
        [Tooltip("Acceleration and deceleration")]
        [SerializeField] private float _speedChangeRate = 10.0f;
        [Tooltip("How fast the character turns to face movement direction on ground")]
        [Range(0.0f, 0.3f)]
        [SerializeField] private float _rotationSmoothTimeGround = 0.12f;
        [Tooltip("How fast the character turns to face movement direction in air")]
        [Range(0.0f, 0.3f)]
        [SerializeField] private float _rotationSmoothTimeAir = 0.08f;
        [Tooltip("How fast the momentum decays while in air. 0 = no decay, 1 = instant decay")]
        [Range(0.0f, 1.0f)]
        [SerializeField] private float _momentumDecayRate = 0.1f;

        [Header("Player Jump & Gravity")]
        [Tooltip("The height the player can jump")]
        [SerializeField] private float _jumpHeight = 1.2f;
        [Tooltip("The height the player's air-jump (double jump)")]
        [SerializeField] private float _doubleJumpHeight = 1.0f;
        [Tooltip("The character's own gravity value. The engine default is -9.81f")]
        [SerializeField] private float _gravity = -15.0f;
        [Tooltip("Time required to pass before being able to jump again.")]
        [SerializeField] private float _jumpTimeout = 0.50f;
        [Tooltip("Time required to pass before entering the fall state.")]
        [SerializeField] private float _fallTimeout = 0.15f;
        
        [Header("Player Grounded Check")]
        [Tooltip("Useful for rough ground")]
        [SerializeField] private float _groundedOffset = -0.14f;
        [Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
        [SerializeField] private float _groundedRadius = 0.28f;
        [Tooltip("What layers the character uses as ground")]
        [SerializeField] private LayerMask _groundLayers;
        
        [Header("Cinemachine")]
        [Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
        [SerializeField] private GameObject _cinemachineCameraTarget;
        [Tooltip("How far in degrees can you move the camera up")]
        [SerializeField] private float _topClamp = 70.0f;
        [Tooltip("How far in degrees can you move the camera down")]
        [SerializeField] private float _bottomClamp = -30.0f;
        [Tooltip("Additional degrees to override the camera. Useful for fine tuning camera position when locked")]
        [SerializeField] private float _cameraAngleOverride = 0.0f;
        [Tooltip("For locking the camera position on all axis")]
        [SerializeField] private bool _lockCameraPosition = false;

        [Header("Audio")]
        [SerializeField] private AudioClip _landingAudioClip;
        [SerializeField] private AudioClip[] _footstepAudioClips;
        [Range(0, 1)] [SerializeField] private float _footstepAudioVolume = 0.5f;

        // --- Private Fields ---

        // Cinemachine
        private float _cinemachineTargetYaw;
        private float _cinemachineTargetPitch;

        // Player state
        private float _speed;
        private float _animationBlend;
        private float _targetRotation = 0.0f;
        private float _rotationVelocity;
        private float _verticalVelocity;
        private float _terminalVelocity = 53.0f;
        private int _airJumpsRemaining = 0;
        private bool _isDoubleJumpEnabled = true;
        private bool _isGravityOverridden = false;
        
        // Momentum system for air control
        private Vector3 _baseMomentum = Vector3.zero;
        private bool _wasGroundedLastFrame = true;
        
        // Platform velocity tracking
        private IPlatformVelocityProvider _currentPlatform;
        private Vector3 _platformVelocity = Vector3.zero;
        private float _platformExitTime = -1f;
        private const float PlatformMemoryDuration = 0.2f; // Tempo para manter referência da plataforma após sair

        // Timers
        private float _jumpTimeoutDelta;
        private float _fallTimeoutDelta;
        private float _lastJumpTime = -Mathf.Infinity;
        private const float MinJumpInterval = 0.1f;

        // Component references
        private Animator _animator;
        private CharacterController _controller;
        private StarterAssetsInputs _input;
        private PlayerInput _playerInput;
        private GameObject _mainCamera;
        private bool _hasAnimator;

        // Animation IDs
        private readonly int _animIDSpeed = Animator.StringToHash("Speed");
        private readonly int _animIDGrounded = Animator.StringToHash("Grounded");
        private readonly int _animIDJump = Animator.StringToHash("Jump");
        private readonly int _animIDFreeFall = Animator.StringToHash("FreeFall");
        private readonly int _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
        private readonly int _animIDDoubleJump = Animator.StringToHash("DJump");
        
        #endregion

        #region Properties

        public bool Grounded { get; private set; } = true;

        private bool IsCurrentDeviceMouse => _playerInput.currentControlScheme == "KeyboardMouse";
        
        #endregion

        #region Unity Lifecycle Methods

        private void Awake()
        {
            if (_mainCamera == null)
            {
                _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
            }
        }

        private void Start()
        {
            _cinemachineTargetYaw = _cinemachineCameraTarget.transform.rotation.eulerAngles.y;
            
            _hasAnimator = TryGetComponent(out _animator);
            _controller = GetComponent<CharacterController>();
            _input = GetComponent<StarterAssetsInputs>();
            _playerInput = GetComponent<PlayerInput>();

            // Reset timers
            _jumpTimeoutDelta = _jumpTimeout;
            _fallTimeoutDelta = _fallTimeout;
        }

        private void Update()
        {
            GroundedCheck();
            JumpAndGravity();
            Move();
        }

        private void LateUpdate()
        {
            CameraRotation();
        }

        #endregion

        #region Core Logic Methods

        private void GroundedCheck()
        {
            Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - _groundedOffset, transform.position.z);
            Grounded = Physics.CheckSphere(spherePosition, _groundedRadius, _groundLayers, QueryTriggerInteraction.Ignore);

            if (_hasAnimator)
            {
                _animator.SetBool(_animIDGrounded, Grounded);
            }
        }
        
            private void JumpAndGravity()
    {
        if (Grounded)
        {
            // --- ESTADO: NO CHÃO ---
            _fallTimeoutDelta = _fallTimeout;

            if (_hasAnimator)
            {
                _animator.SetBool(_animIDJump, false);
                _animator.SetBool(_animIDFreeFall, false);
            }

            // Só reabastece o pulo e reseta a velocidade se realmente POUSAMOS.
            if (_verticalVelocity < 0.0f)
            {
                _verticalVelocity = -2f;
                _airJumpsRemaining = 1;
            }

            // Lógica para pular do chão
            // --- CORREÇÃO FINAL APLICADA AQUI ---
            // Adicionada a checagem "_verticalVelocity <= 0.0f" para prevenir pulos-fantasma
            // que acontecem durante um "falso positivo" do GroundedCheck.
            if (_input.jump && _jumpTimeoutDelta <= 0.0f && _verticalVelocity <= 0.0f)
            {
                _verticalVelocity = Mathf.Sqrt(_jumpHeight * -2f * _gravity);
                if (_hasAnimator) _animator.SetBool(_animIDJump, true);
                
                _lastJumpTime = Time.time;
                _jumpTimeoutDelta = _jumpTimeout;
                _input.jump = false;
            }
            
            if (_jumpTimeoutDelta > 0.0f)
            {
                _jumpTimeoutDelta -= Time.deltaTime;
            }
        }
        else // Se não estamos no chão...
        {
            // --- ESTADO: NO AR ---
            if (_fallTimeoutDelta > 0.0f)
            {
                _fallTimeoutDelta -= Time.deltaTime;
            }
            else
            {
                if (_hasAnimator) _animator.SetBool(_animIDFreeFall, true);
            }

            // Lógica do Pulo no Ar (Pulo Duplo)
            if (_input.jump && _airJumpsRemaining > 0 && _isDoubleJumpEnabled && Time.time >= _lastJumpTime + MinJumpInterval)
            {
                _airJumpsRemaining--;

                _verticalVelocity = Mathf.Sqrt(_doubleJumpHeight * -2f * _gravity);
                if (_hasAnimator) _animator.SetTrigger(_animIDDoubleJump);

                _lastJumpTime = Time.time;
                _input.jump = false;
            }
        }
        
        // Limpa qualquer input de pulo que não tenha sido usado.
        if (_input.jump)
        {
            _input.jump = false;
        }
        
        // Aplica a gravidade constantemente.
        if (!_isGravityOverridden)
        {
            if (_verticalVelocity < _terminalVelocity)
            {
                _verticalVelocity += _gravity * Time.deltaTime;
            }
        }
    }
        
        
        private void Move()
        {
            // Atualiza a velocidade da plataforma se estivermos em uma
            UpdatePlatformVelocity();
            
            // Captura o momentum base quando sai do chão
            if (_wasGroundedLastFrame && !Grounded)
            {
                Vector3 currentVelocity = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z);
                Debug.Log($"[ThirdPersonController] Velocidade atual do jogador: {currentVelocity}");
                
                // Quando o jogador pula, o OnTriggerExit remove o parenting imediatamente,
                // então precisamos somar a velocidade da plataforma manualmente
                if (_currentPlatform != null)
                {
                    Vector3 platformHorizontalVelocity = new Vector3(_platformVelocity.x, 0.0f, _platformVelocity.z);
                    Debug.Log($"[ThirdPersonController] Velocidade da plataforma: {_platformVelocity} | Horizontal: {platformHorizontalVelocity}");
                    currentVelocity += platformHorizontalVelocity;
                    Debug.Log($"[ThirdPersonController] Velocidade combinada (jogador + plataforma): {currentVelocity}");
                }
                else
                {
                    Debug.Log($"[ThirdPersonController] Nenhuma plataforma detectada");
                }
                
                _baseMomentum = currentVelocity;
                Debug.Log($"[ThirdPersonController] Momentum base definido: {_baseMomentum}");
            }
            
            // Reseta o momentum quando toca o chão
            if (!_wasGroundedLastFrame && Grounded)
            {
                _baseMomentum = Vector3.zero;
            }
            
            _wasGroundedLastFrame = Grounded;
            
            float targetSpeed = _moveSpeed;
            if (_input.move == Vector2.zero) targetSpeed = 0.0f;

            float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;
            float desiredSpeed = targetSpeed * inputMagnitude;
            
            if (Grounded)
            {
                // Lógica normal no chão
                float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;
                float currentAcceleration = _speedChangeRate;

                if (Mathf.Abs(currentHorizontalSpeed - desiredSpeed) > 0.1f)
                {
                    _speed = Mathf.Lerp(currentHorizontalSpeed, desiredSpeed, Time.deltaTime * currentAcceleration);
                    _speed = Mathf.Round(_speed * 1000f) / 1000f;
                }
                else
                {
                    _speed = desiredSpeed;
                }
                
                // --- Rotation ---
                if (_input.move != Vector2.zero)
                {
                    _targetRotation = Mathf.Atan2(_input.move.x, _input.move.y) * Mathf.Rad2Deg + _mainCamera.transform.eulerAngles.y;
                    float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity, _rotationSmoothTimeGround);
                    transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
                }

                Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;
                _controller.Move(targetDirection.normalized * (_speed * Time.deltaTime) + new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);
            }
            else
            {
                // Aplica decaimento do momentum ao longo do tempo
                _baseMomentum = Vector3.Lerp(_baseMomentum, Vector3.zero, _momentumDecayRate * Time.deltaTime);
                
                // Lógica no ar com momentum preservado
                Vector3 finalHorizontalVelocity = _baseMomentum; // Preserva momentum (já com decaimento aplicado)
                
                if (_input.move != Vector2.zero)
                {
                    // Apenas aplica controle quando há input ativo
                    _targetRotation = Mathf.Atan2(_input.move.x, _input.move.y) * Mathf.Rad2Deg + _mainCamera.transform.eulerAngles.y;
                    Vector3 inputDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;
                    
                    // --- Air Rotation ---
                    float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity, _rotationSmoothTimeAir);
                    transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
                    
                    // Air control como força aditiva ao momentum, não substitutiva
                    // Calcula a força de input baseada na direção e intensidade do input
                    Vector3 inputForce = inputDirection * (desiredSpeed * _airControlFactor * _speedChangeRate * Time.deltaTime);
                    
                    // Adiciona a força de input ao momentum preservado
                    finalHorizontalVelocity = _baseMomentum + inputForce;
                    
                    // Limita a velocidade máxima para evitar aceleração infinita
                    float maxAirSpeed = _moveSpeed * 1.5f; // 50% a mais que a velocidade normal
                    if (finalHorizontalVelocity.magnitude > maxAirSpeed)
                    {
                        finalHorizontalVelocity = finalHorizontalVelocity.normalized * maxAirSpeed;
                    }
                }
                // Se não há input, mantém o momentum atual (finalHorizontalVelocity = _baseMomentum)
                
                // Atualiza o momentum base para o próximo frame
                _baseMomentum = finalHorizontalVelocity;
                
                // Aplica o movimento
                _controller.Move(finalHorizontalVelocity * Time.deltaTime + new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);
                
                // Atualiza _speed para animações (baseado na magnitude total)
                _speed = finalHorizontalVelocity.magnitude;
            }
            
            _animationBlend = Mathf.Lerp(_animationBlend, desiredSpeed, Time.deltaTime * _speedChangeRate);

            if (_hasAnimator)
            {
                _animator.SetFloat(_animIDSpeed, _animationBlend);
                _animator.SetFloat(_animIDMotionSpeed, inputMagnitude);
            }
        }

        private void CameraRotation()
        {
            const float threshold = 0.01f;
            if (_input.look.sqrMagnitude >= threshold && !_lockCameraPosition)
            {
                float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;
                _cinemachineTargetYaw += _input.look.x * deltaTimeMultiplier;
                _cinemachineTargetPitch += _input.look.y * deltaTimeMultiplier;
            }

            _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
            _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, _bottomClamp, _topClamp);

            _cinemachineCameraTarget.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch + _cameraAngleOverride, _cinemachineTargetYaw, 0.0f);
        }
        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            // Tenta encontrar o componente BoxInteractor no objeto em que colidimos
            if (hit.gameObject.TryGetComponent(out BoxInteractor box))
            {
                // Verifica se a interação por pulo está ativa na caixa
                // E se o personagem está colidindo com a parte de cima da caixa (a normal do ponto de colisão aponta para cima)
                if (box.canInteractOnJump && hit.normal.y > 0.7f)
                {
                    // Se as condições forem verdadeiras, chama o método público de interação da caixa
                    box.Interact(transform);
                }
            }
        }
        
        #endregion

        #region Helper & Public Methods

        private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
        {
            if (lfAngle < -360f) lfAngle += 360f;
            if (lfAngle > 360f) lfAngle -= 360f;
            return Mathf.Clamp(lfAngle, lfMin, lfMax);
        }

        // --- As funções abaixo são para interações com outros sistemas, se necessário ---
        
        public void SetDoubleJumpEnabled(bool isEnabled)
        {
            _isDoubleJumpEnabled = isEnabled;
        }

        public void ApplyUpwardForce(float force)
        {
            _verticalVelocity = force;
        }
        
        /// <summary>
        /// Aplica uma força em uma direção específica ao jogador.
        /// Útil para trampolins direcionais ou outros efeitos de impulso.
        /// </summary>
        /// <param name="forceVector">O vetor de força a ser aplicado</param>
        public void ApplyDirectionalForce(Vector3 forceVector)
        {
            // Separa a componente vertical da força
            _verticalVelocity = forceVector.y;
            
            // Aplica a componente horizontal como um impulso na direção desejada
            Vector3 horizontalForce = new Vector3(forceVector.x, 0, forceVector.z);
            if (horizontalForce.magnitude > 0.1f)
            {
                // Calcula a velocidade desejada baseada na força
                float horizontalForceMagnitude = horizontalForce.magnitude;
                Vector3 forceDirection = horizontalForce.normalized;
                
                // Rotaciona o personagem na direção da força
                _targetRotation = Mathf.Atan2(forceDirection.x, forceDirection.z) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0.0f, _targetRotation, 0.0f);
                
                // Aplica a velocidade horizontal
                _speed = horizontalForceMagnitude;
            }
        }
        
        /// <summary>
        /// Multiplica a velocidade horizontal atual do jogador pelo fator especificado.
        /// Útil para trampolins que mantêm, aumentam ou reduzem o momentum horizontal.
        /// </summary>
        /// <param name="factor">O fator de multiplicação (0 = parar, 1 = manter velocidade atual, >1 = aumentar)</param>
        public void MultiplyHorizontalVelocity(float factor)
        {
            if (factor >= 0)
            {
                _speed *= factor;
            }
        }
        
        /// <summary>
        /// Retorna a velocidade vertical atual do jogador.
        /// Útil para verificar se o jogador está subindo ou caindo.
        /// </summary>
        /// <returns>A velocidade vertical atual</returns>
        public float GetVerticalVelocity()
        {
            return _verticalVelocity;
        }
        
        /// <summary>
        /// Força o estado da animação de queda livre. Útil para eventos externos como tornados.
        /// </summary>
        public void SetFreeFallAnimation(bool isFalling)
        {
            if (_hasAnimator)
            {
                _animator.SetBool(_animIDFreeFall, isFalling);
            }
        }
        
        /// <summary>
        /// Permite que um script externo (como um tornado) desabilite a gravidade padrão do jogador.
        /// </summary>
        public void SetGravityOverride(bool isOverridden)
        {
            // Precisaremos de um novo campo booleano privado para rastrear isso.
            // Adicione 'private bool _isGravityOverridden = false;' no topo do seu script, na seção de campos privados.
            _isGravityOverridden = isOverridden;
        }
        
        /// <summary>
        /// Método chamado pela StickyPlatform quando o player entra na plataforma.
        /// </summary>
        /// <param name="platform">A plataforma que o player entrou</param>
        public void OnEnterPlatform(IPlatformVelocityProvider platform)
        {
            Debug.Log("[ThirdPersonController] Entrando na plataforma");
            _currentPlatform = platform;
            _platformExitTime = -1f; // Reset do tempo de saída
        }
        
        /// <summary>
        /// Método chamado pela StickyPlatform quando o player sai da plataforma.
        /// </summary>
        public void OnExitPlatform()
        {
            Debug.Log("[ThirdPersonController] Saindo da plataforma - mantendo referência temporariamente");
            _platformExitTime = Time.time;
            // Não limpa _currentPlatform imediatamente para preservar momentum
        }
        
        /// <summary>
        /// Atualiza a velocidade da plataforma atual se estivermos em uma.
        /// </summary>
        private void UpdatePlatformVelocity()
        {
            // Limpa referência da plataforma após o tempo de memória
            if (_platformExitTime > 0 && Time.time - _platformExitTime > PlatformMemoryDuration)
            {
                Debug.Log("[ThirdPersonController] Limpando referência da plataforma após tempo de memória");
                _currentPlatform = null;
                _platformVelocity = Vector3.zero;
                _platformExitTime = -1f;
            }
            
            if (_currentPlatform != null)
            {
                Vector3 previousVelocity = _platformVelocity;
                _platformVelocity = _currentPlatform.GetPlatformVelocity();
                
                // Debug: Log apenas quando a velocidade muda significativamente
                if (Vector3.Distance(previousVelocity, _platformVelocity) > 0.1f)
                {
                    Debug.Log($"[ThirdPersonController] Velocidade da plataforma atualizada: {_platformVelocity}");
                }
            }
            else
            {
                _platformVelocity = Vector3.zero;
            }
        }

        #endregion

        #region Gizmos & Events

        private void OnDrawGizmosSelected()
        {
            Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
            Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);
            Gizmos.color = Grounded ? transparentGreen : transparentRed;
            Gizmos.DrawSphere(new Vector3(transform.position.x, transform.position.y - _groundedOffset, transform.position.z), _groundedRadius);
        }
        
        // Animation Events
        private void OnFootstep(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f && _footstepAudioClips.Length > 0)
            {
                var index = Random.Range(0, _footstepAudioClips.Length);
                AudioSource.PlayClipAtPoint(_footstepAudioClips[index], transform.TransformPoint(_controller.center), _footstepAudioVolume);
            }
        }
        
        private void OnLand(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                AudioSource.PlayClipAtPoint(_landingAudioClip, transform.TransformPoint(_controller.center), _footstepAudioVolume);
            }
        }

        #endregion
    }
}