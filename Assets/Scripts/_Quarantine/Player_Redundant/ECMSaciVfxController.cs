using ECM.Controllers;
using UnityEngine;
using Managers;

namespace Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ECMSaciController))]
    public sealed class ECMSaciVfxController : MonoBehaviour
    {
        [Header("Referências")]
        [Tooltip("Controlador ECM do Saci.")]
        [SerializeField] private ECMSaciController saci;
        [Tooltip("Ponto de spawn preferencial para VFX (ex: pés). Se vazio usa transform.")]
        [SerializeField] private Transform vfxSpawnPoint;

        [Header("VFX (One-shots - na hierarquia)")]
        [Tooltip("ParticleSystem para pulo duplo em one-shot (já presente).")]
        [SerializeField] private ParticleSystem doubleJumpVfx;
        [Tooltip("ParticleSystem para aterrissagem em one-shot (já presente).")]
        [SerializeField] private ParticleSystem landVfx;
        [Tooltip("ParticleSystem para morte em one-shot (já presente).")]
        [SerializeField] private ParticleSystem deathVfx;

        [Header("Poeira de Caminhada")]
        [Tooltip("Particle System em looping com rastro/tail, ativado só no chão.")]
        [SerializeField] private ParticleSystem walkDust;
        [Tooltip("Limiar de velocidade lateral para ativar poeira de caminhada.")]
        [SerializeField] private float fastWalkSpeedThreshold = 2.0f;

        [Header("Configurações de One-shot")]
        [Tooltip("Quando ligado, mantém posição e rotação locais do ParticleSystem (não re-posiciona ao tocar).")]
        [SerializeField] private bool useLocalTransformForOneShots = true;

        [Header("Pulo Duplo Direcional")]
        [Tooltip("Alinha rotação do pulo duplo ao vetor de impulso/velocidade, relativo à rotação local inicial.")]
        [SerializeField] private bool alignDoubleJumpToImpulse = true;
        [Tooltip("Ângulo máximo (graus) desviado da rotação local inicial.")]
        [SerializeField] private float doubleJumpMaxYawOffset = 30f;
        [SerializeField] private float doubleJumpInputMultiplier = 1.5f;
        [Tooltip("Ângulo máximo de pitch (graus) no eixo X para o pulo duplo.")]
        [SerializeField] private float doubleJumpMaxPitchOffset = 30f;

        [Header("Detecção de Eventos")]
        [Tooltip("Usar contador do Animator (MidAirJumpCount) para detectar pulo duplo.")]
        [SerializeField] private bool useAnimatorCounterForDoubleJump = true;
        [Tooltip("Limiar de variação de velocidade vertical para detectar pulo duplo (fallback).")]
        [SerializeField] private float doubleJumpImpulseDeltaThreshold = 3.5f;
        [Tooltip("Tempo mínimo no ar para considerar aterrissagem válida (anti-spam).")]
        [SerializeField] private float minUngroundedTimeForLand = 0.1f;
        [Tooltip("Velocidade vertical mínima para disparar aterrissagem.")]
        [SerializeField] private float minLandingDownwardSpeed = 2.0f;
        [Tooltip("Cooldown mínimo (s) entre execuções do VFX de aterrissagem.")]
        [SerializeField] private float minLandCooldown = 0.25f;
        [Tooltip("Tempo mínimo desde o último pulo no ar para permitir VFX de aterrissagem.")]
        [SerializeField] private float minTimeSinceMidAirJumpForLand = 0.2f;

        private int _prevAnimatorMidAirJumpCount;
        private bool _prevGrounded;
        private float _prevVerticalSpeed;
        private float _ungroundedTime;
        private float _lastLandTime;
        private float _lastMidAirJumpTime;
        private Quaternion _doubleJumpInitialLocalRotation;
        private Transform _landOriginalParent;
        private Vector3 _landInitialLocalPosition;
        private Quaternion _landInitialLocalRotation;
        private Vector3 _landInitialLocalScale;
        private Transform _deathOriginalParent;
        private Vector3 _deathInitialLocalPosition;
        private Quaternion _deathInitialLocalRotation;
        private Vector3 _deathInitialLocalScale;

        private void Awake()
        {
            if (saci == null)
                saci = GetComponent<ECMSaciController>();

            if (vfxSpawnPoint == null)
                vfxSpawnPoint = transform;

            _prevGrounded = saci != null && saci.movement != null && saci.movement.isGrounded;
            _prevVerticalSpeed = saci != null && saci.movement != null ? saci.movement.velocity.y : 0f;
            _prevAnimatorMidAirJumpCount = saci != null && saci.animator != null
                ? saci.animator.GetInteger("MidAirJumpCount")
                : 0;

            if (doubleJumpVfx != null)
                _doubleJumpInitialLocalRotation = doubleJumpVfx.transform.localRotation;

            if (landVfx != null)
            {
                var lt = landVfx.transform;
                _landOriginalParent = lt.parent;
                _landInitialLocalPosition = lt.localPosition;
                _landInitialLocalRotation = lt.localRotation;
                _landInitialLocalScale = lt.localScale;
            }

            if (deathVfx != null)
            {
                var dt = deathVfx.transform;
                _deathOriginalParent = dt.parent;
                _deathInitialLocalPosition = dt.localPosition;
                _deathInitialLocalRotation = dt.localRotation;
                _deathInitialLocalScale = dt.localScale;
            }
        }

        private void Update()
        {
            if (saci == null || saci.movement == null)
                return;

            UpdateWalkDust();
            CheckLanding();
            CheckDoubleJump();

            _prevGrounded = saci.movement.isGrounded;
            _prevVerticalSpeed = saci.movement.velocity.y;
        }

        private void UpdateWalkDust()
        {
            if (walkDust == null)
                return;

            var move = saci.movement.velocity;
            var lateral = Vector3.ProjectOnPlane(move, transform.up);
            bool shouldEmit = saci.movement.isGrounded && lateral.magnitude >= fastWalkSpeedThreshold;

            var emission = walkDust.emission;
            emission.enabled = shouldEmit;

            // Garantir estado coerente do sistema se alternarmos rapidamente
            if (shouldEmit)
            {
                if (!walkDust.isPlaying)
                    walkDust.Play();
            }
            else
            {
                if (walkDust.isPlaying)
                    walkDust.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }

        private void CheckLanding()
        {
            bool groundedNow = saci.movement.isGrounded;
            float vy = saci.movement.velocity.y;
            // Tempo no ar é preferencialmente lido do controlador ECM do Saci

            // Checa transição: estava no ar e agora está no chão
            if (!_prevGrounded && groundedNow)
            {
                float timeAir = saci != null ? saci.UngroundedTime : _ungroundedTime; // guarda tempo no ar antes de resetar
                // Impacto: usa velocidade descendente capturada pelo controlador do Saci
                float impactSpeed = saci != null ? saci.GroundImpactDownwardSpeed : (_prevVerticalSpeed < 0f ? -_prevVerticalSpeed : 0f);
                float sinceLastLand = Time.time - _lastLandTime;
                float sinceLastMidAirJump = Time.time - _lastMidAirJumpTime;

                if (landVfx != null
                    && timeAir >= minUngroundedTimeForLand
                    && impactSpeed >= minLandingDownwardSpeed
                    && sinceLastLand >= minLandCooldown
                    && sinceLastMidAirJump >= minTimeSinceMidAirJumpForLand)
                {
                    PlayOneShot(landVfx, saci.movement.groundPoint, Quaternion.LookRotation(Vector3.ProjectOnPlane(transform.forward, saci.movement.groundNormal), saci.movement.groundNormal));
                    _lastLandTime = Time.time;
                }

                // Reset é feito no controlador do Saci; mantemos fallback local apenas quando saci for nulo
                if (saci == null) _ungroundedTime = 0f;
            }
        }

        private void CheckDoubleJump()
        {
            if (!useAnimatorCounterForDoubleJump)
            {
                // Fallback por delta de velocidade vertical
                if (!saci.movement.isGrounded)
                {
                    float deltaVy = saci.movement.velocity.y - _prevVerticalSpeed;
                    if (deltaVy >= doubleJumpImpulseDeltaThreshold)
                    {
                        PlayDoubleJumpAligned();
                    }
                }
                return;
            }

            if (saci.animator == null)
                return;

            int current = saci.animator.GetInteger("MidAirJumpCount");
            if (!saci.movement.isGrounded && current > _prevAnimatorMidAirJumpCount)
            {
                PlayDoubleJumpAligned();
            }
            _prevAnimatorMidAirJumpCount = saci.movement.isGrounded ? 0 : current;
        }

        private void PlayDoubleJumpAligned()
        {
            if (doubleJumpVfx == null)
                return;

            // Marca o instante do último pulo no ar para filtrar aterrissagens suaves
            _lastMidAirJumpTime = Time.time;

            if (alignDoubleJumpToImpulse)
            {
                var t = doubleJumpVfx.transform;
                // Converte input bruto para direção em mundo (relativa à câmera se disponível)
                Vector3 inputDirWorld = Vector3.zero;
                {
                    Vector2 rawMove = InputManager.Instance != null ? InputManager.Instance.Move : Vector2.zero;
                    if (rawMove.sqrMagnitude > 0.0001f)
                    {
                        Transform cam = Camera.main != null ? Camera.main.transform : null;
                        if (cam != null)
                        {
                            Vector3 camForward = cam.forward; camForward.y = 0f; camForward.Normalize();
                            Vector3 camRight = cam.right;   camRight.y = 0f;   camRight.Normalize();
                            inputDirWorld = camRight * rawMove.x + camForward * rawMove.y;
                        }
                        else
                        {
                            inputDirWorld = new Vector3(rawMove.x, 0f, rawMove.y);
                        }
                        inputDirWorld = Vector3.ProjectOnPlane(inputDirWorld, transform.up);
                    }
                }

                float pitchOffset = 0f;
                if (inputDirWorld.sqrMagnitude > 0.0001f)
                {
                    var desiredDir = inputDirWorld.normalized;
                    // Componente para frente/trás relativo ao personagem (leva em conta rotação atual)
                    float forwardComponent = Mathf.Clamp(Vector3.Dot(desiredDir, transform.forward), -1f, 1f);
                    pitchOffset = Mathf.Clamp(forwardComponent * doubleJumpInputMultiplier * doubleJumpMaxPitchOffset,
                        -doubleJumpMaxPitchOffset, doubleJumpMaxPitchOffset);
                }

                // Aplica SEMPRE em coordenada local e somente no eixo X
                t.localRotation = _doubleJumpInitialLocalRotation * Quaternion.AngleAxis(pitchOffset, Vector3.right);
            }

            // Respeita configuração local; não reposiciona/rotaciona se marcado
            PlayOneShot(doubleJumpVfx, vfxSpawnPoint != null ? vfxSpawnPoint.position : transform.position,
                vfxSpawnPoint != null ? vfxSpawnPoint.rotation : transform.rotation);

            // Garante que a rotação permaneça em espaço local mesmo após PlayOneShot
            if (alignDoubleJumpToImpulse)
            {
                var t = doubleJumpVfx.transform;
                // Recalcula pitch após possível override de rotação mundial
                Vector3 inputDirWorld = Vector3.zero;
                {
                    Vector2 rawMove = InputManager.Instance != null ? InputManager.Instance.Move : Vector2.zero;
                    if (rawMove.sqrMagnitude > 0.0001f)
                    {
                        Transform cam = Camera.main != null ? Camera.main.transform : null;
                        if (cam != null)
                        {
                            Vector3 camForward = cam.forward; camForward.y = 0f; camForward.Normalize();
                            Vector3 camRight = cam.right;   camRight.y = 0f;   camRight.Normalize();
                            inputDirWorld = camRight * rawMove.x + camForward * rawMove.y;
                        }
                        else
                        {
                            inputDirWorld = new Vector3(rawMove.x, 0f, rawMove.y);
                        }
                        inputDirWorld = Vector3.ProjectOnPlane(inputDirWorld, transform.up);
                    }
                }
                float pitchOffset = 0f;
                if (inputDirWorld.sqrMagnitude > 0.0001f)
                {
                    var desiredDir = inputDirWorld.normalized;
                    float forwardComponent = Mathf.Clamp(Vector3.Dot(desiredDir, transform.forward), -1f, 1f);
                    pitchOffset = Mathf.Clamp(forwardComponent * doubleJumpInputMultiplier * doubleJumpMaxPitchOffset,
                        -doubleJumpMaxPitchOffset, doubleJumpMaxPitchOffset);
                }
                t.localRotation = _doubleJumpInitialLocalRotation * Quaternion.AngleAxis(pitchOffset, Vector3.right);
            }
        }

        private void PlayOneShot(ParticleSystem ps, Vector3 position, Quaternion rotation)
        {
            if (ps == null) return;

            // Comportamento especial para aterrissagem: parar, reposicionar no local inicial relativo ao jogador, tocar e desparentar
            if (ps == landVfx)
            {
                var t = ps.transform;

                // Se estiver tocando, pare e limpe para evitar acumular partículas
                if (ps.isPlaying || ps.IsAlive(true))
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

                // Reposiciona sob o pai original e restaura transform local inicial
                if (_landOriginalParent != null)
                    t.SetParent(_landOriginalParent, false);
                t.localPosition = _landInitialLocalPosition;
                t.localRotation = _landInitialLocalRotation;
                t.localScale = _landInitialLocalScale;

                // Toca novamente
                ps.Play(true);

                // Desparenta após disparar para não seguir movimento do jogador
                t.SetParent(null, true);
                return;
            }

            // Comportamento especial para morte: restaurar posição relativa ao jogador, tocar e desparentar
            if (ps == deathVfx)
            {
                var t = ps.transform;
                // Se houver execução anterior, pare e limpe para evitar sobreposição
                if (ps.isPlaying || ps.IsAlive(true))
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

                // Reposiciona sob o pai original e restaura transform local inicial
                if (_deathOriginalParent != null)
                    t.SetParent(_deathOriginalParent, false);
                t.localPosition = _deathInitialLocalPosition;
                t.localRotation = _deathInitialLocalRotation;
                t.localScale = _deathInitialLocalScale;

                // Toca novamente
                ps.Play(true);

                // Desparenta após disparar para permanecer visível no mundo no ponto de morte
                t.SetParent(null, true);
                return;
            }

            if (!useLocalTransformForOneShots)
            {
                var t = ps.transform;
                t.SetPositionAndRotation(position, rotation);
            }

            ps.Play(true);
        }

        // Método opcional para integrar VFX de morte num único componente
        public void OnDeath()
        {
            if (deathVfx != null)
                // Preserva a posição/rotação atuais do VFX ao desparentar
                PlayOneShot(deathVfx, deathVfx.transform.position, deathVfx.transform.rotation);

            // Parar poeira imediatamente
            if (walkDust != null)
            {
                var emission = walkDust.emission;
                emission.enabled = false;
                if (walkDust.isPlaying)
                    walkDust.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }
    }
}