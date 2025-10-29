using System.Collections;
using DG.Tweening;
using Player;
using UnityEngine;

namespace Gameplay
{
    [AddComponentMenu("Gameplay/ECM Trampoline Controller")]
    public class ECMTrampolineController : MonoBehaviour
    {
        [Header("Activation")]
        [Tooltip("Habilita/desabilita o trampolim via Inspector.")]
        [SerializeField] private bool trampolineEnabled = true;
        [Header("Debug")]
        [Tooltip("Quando ligado, imprime logs de interação e impulso do trampolim.")]
        [SerializeField] private bool debugLogs = false;
        [Header("Contact Detection")]
        [Tooltip("Limiar mínimo do dot(normal, up) para considerar contato por cima.")]
        [Range(0f, 1f)]
        [SerializeField] private float minTopContactDot = 0.2f;
        [Tooltip("Velocidade mínima de aproximação descendente para considerar contato por cima (usando relativeVelocity).")]
        [Range(0f, 10f)]
        [SerializeField] private float minApproachDownwardSpeed = 0.25f;
        [Tooltip("Ignora checagem por cima e aplica sempre que houver contato (útil para calibração).")]
        [SerializeField] private bool ignoreTopContactCheck = false;
        [Tooltip("Altura mínima acima do plano do trampolim (transform.up) para considerar contato por cima.")]
        [Range(0f, 1f)]
        [SerializeField] private float minAboveHeight = 0.05f;
        [Header("Force Settings")]
        [Tooltip("Impulso vertical aplicado ao player ao usar o trampolim (ECM).")]
        [Range(5f, 30f)]
        public float launchForce = 10f;

        [Tooltip("Impulso mínimo aplicado ao player (clamp por velocidade de descida).")]
        [Range(0f, 30f)]
        public float minLaunchForce = 6f;

        [Tooltip("Multiplicador da velocidade horizontal ao usar o trampolim (1 = mantém).")]
        [Range(0.5f, 2f)]
        public float horizontalVelocityMultiplier = 1f;

        [Header("Usage Settings")]
        [Tooltip("Se marcado, o trampolim só pode ser usado uma vez.")]
        public bool singleUse;

        [Tooltip("Se marcado, o trampolim precisa recarregar entre usos.")]
        public bool hasCooldown;

        [Tooltip("Tempo de recarga (s) após uso.")]
        [Range(0.5f, 10f)]
        public float cooldownTime = 2f;

        [Header("Visual Feedback")]
        [Tooltip("Se marcado, toca animação visual ao usar.")]
        public bool useVisualAnimation = true;

        [Tooltip("Escala máxima durante a compressão.")]
        public Vector3 compressionScale = new Vector3(1.2f, 0.5f, 1.2f);

        [Tooltip("Escala máxima durante a extensão.")]
        public Vector3 extensionScale = new Vector3(0.8f, 1.5f, 0.8f);

        [Tooltip("Duração total da animação (s).")]
        [Range(0.1f, 1f)]
        public float animationDuration = 0.3f;

        [Header("Sound Feedback")]
        [Tooltip("Se marcado, toca som ao usar.")]
        public bool useSound = true;

        [Tooltip("Som tocado ao ativar o trampolim.")]
        public AudioClip trampolineSound;

        [Range(0f, 1f)]
        public float soundVolume = 0.7f;

        [Header("Advanced Settings")]
        [Tooltip("Layers que podem interagir com o trampolim.")]
        public LayerMask interactiveLayers;

        [Tooltip("Ângulo de lançamento em graus (0 = para cima).")]
        [Range(-45f, 45f)]
        public float launchAngle;

        [Header("Impact Scaling")]
        [Tooltip("Velocidade vertical mínima de descida para começar a escalar o impulso.")]
        [Range(0f, 30f)]
        public float minDownwardSpeed = 0.5f;

        [Tooltip("Velocidade vertical máxima de descida para atingir o impulso máximo.")]
        [Range(0.5f, 50f)]
        public float maxDownwardSpeed = 15f;

        [Tooltip("Usa escala proporcional direta: impulso = min + k * downward (clamp em max).")]
        [SerializeField] private bool useProportionalImpulse = false;
        [Tooltip("Ganho por unidade de velocidade descendente quando escala proporcional está ativa.")]
        [Range(0f, 10f)]
        [SerializeField] private float impulsePerDownwardUnit = 1.0f;

        [Tooltip("Usa uma curva para mapear t (0..1) em escala do impulso.")]
        [SerializeField] private bool useImpactCurve = false;
        [SerializeField] private AnimationCurve impactCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [Header("Jump Assist Window")]
        [Tooltip("Quando ligado, abre uma pequena janela para somar o pulo do jogador ao impulso do trampolim.")]
        [SerializeField] private bool useJumpBoostWindow = true;
        [Tooltip("Duração da janela para detectar o pulo após o contato (segundos).")]
        [Range(0f, 1f)]
        [SerializeField] private float jumpBoostWindowSeconds = 0.15f;
        [Tooltip("Tempo adiantado (pré-contato) para capturar o pulo enquanto o jogador está descendo e prestes a tocar o trampolim.")]
        [Range(0f, 0.5f)]
        [SerializeField] private float preContactAdvanceSeconds = 0.12f;
        [Tooltip("Extensão extra da janela após o fim da animação do trampolim (segundos).")]
        [Range(0f, 0.5f)]
        [SerializeField] private float postAnimationExtraSeconds = 0.10f;
        [Tooltip("Distância vertical máxima acima do trampolim para iniciar a janela adiantada.")]
        [Range(0f, 1.5f)]
        [SerializeField] private float advanceMaxDistance = 0.6f;
        [Tooltip("Quando ligado, consome o input de pulo ao capturar na janela adiantada para evitar pulo duplo antes do contato.")]
        [SerializeField] private bool consumeJumpInputOnAdvance = true;
        [Tooltip("Multiplicador do impulso de pulo do jogador aplicado como boost.")]
        [Range(0f, 2f)]
        [SerializeField] private float jumpBoostImpulseFactor = 1.0f;
        [Tooltip("Quando ligado, consome o input de pulo ao aplicar o boost para evitar duplicidade com Jump().")]
        [SerializeField] private bool consumeJumpInputOnBoost = true;

        // Estado interno para janela adiantada (pré-contato)
        private readonly System.Collections.Generic.Dictionary<ECMSaciController, bool> queuedAdvanceBoost = new System.Collections.Generic.Dictionary<ECMSaciController, bool>();
        private readonly System.Collections.Generic.HashSet<ECMSaciController> advanceWindowOpen = new System.Collections.Generic.HashSet<ECMSaciController>();

        private Vector3 _originalScale;
        private bool _isTrampolineActive = true;
        private Renderer _objectRenderer;
        private Color _originalColor;
        private bool _hasRenderer;

        private void Start()
        {
            _originalScale = transform.localScale;
            _hasRenderer = TryGetComponent(out _objectRenderer);
            if (_hasRenderer)
                _originalColor = _objectRenderer.material.color;

            var col = GetComponent<Collider>();
            if (col == null)
            {
                var childCol = GetComponentInChildren<Collider>();
                if (debugLogs)
                {
                    Debug.LogWarning("[TrampoDebug] Nenhum Collider no mesmo GameObject do script. Coloque o script no objeto com o Collider para receber OnCollision/OnTrigger.", this);
                    if (childCol != null)
                        Debug.LogWarning("[TrampoDebug] Foi encontrado um Collider em filho, mas eventos não são encaminhados automaticamente para o pai.", childCol);
                }
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!trampolineEnabled)
            {
                if (debugLogs) Debug.Log("[TrampoDebug] Trampolim desabilitado (trampolineEnabled=false)", this);
                return;
            }
            if (!_isTrampolineActive) return;
            // Se nenhuma layer foi configurada, aceita qualquer uma
            bool layerOk = interactiveLayers == 0 || ((interactiveLayers.value & (1 << collision.gameObject.layer)) != 0);
            if (!layerOk)
            {
                if (debugLogs) Debug.Log($"[TrampoDebug] Bloqueado por Layer: {LayerMask.LayerToName(collision.gameObject.layer)}", this);
                return;
            }

            var up = transform.up;
            var contacts = collision.contacts;
            // Avaliação robusta de contato por cima: normal do contato, velocidade relativa e vy do Saci
            float maxDot = -1f;
            for (int i = 0; i < contacts.Length; i++)
            {
                float dot = Vector3.Dot(contacts[i].normal, up);
                if (dot > maxDot) maxDot = dot;
            }
            float approachDot = Vector3.Dot(collision.relativeVelocity, up); // negativo = aproximando de cima
            float saciVy = float.NaN;
            if (collision.transform.TryGetComponent(out ECMSaciController saciForEval) && saciForEval.movement != null)
            {
                saciVy = Vector3.Dot(saciForEval.movement.velocity, saciForEval.transform.up);
            }

            // Fallback geométrico: posição relativa acima do plano do trampolim
            Transform evalTransform = (collision.transform.TryGetComponent(out ECMSaciController saciGeom) ? saciGeom.transform : collision.transform);
            float heightRelative = Vector3.Dot(evalTransform.position - transform.position, up);

            bool contactFromAbove = ignoreTopContactCheck
                || (maxDot >= minTopContactDot)
                || (!float.IsNaN(saciVy) && saciVy < 0f)
                || (approachDot < -minApproachDownwardSpeed)
                || (heightRelative >= minAboveHeight);

            if (debugLogs)
            {
                string result = contactFromAbove ? "Top" : "Side";
                Debug.Log($"[TrampoDebug] Avaliação contato | maxDot={maxDot:F2} | saciVy={(float.IsNaN(saciVy) ? "n/a" : saciVy.ToString("F2"))} | approachDot={approachDot:F2} | heightRel={heightRelative:F2} | resultado={result}", this);
            }

            if (!contactFromAbove) return;

            ApplyTrampolineEffect(collision.transform);

            if (singleUse)
                _isTrampolineActive = false;
            else if (hasCooldown)
                StartCoroutine(RechargeTrampoline());
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!trampolineEnabled)
            {
                if (debugLogs) Debug.Log("[TrampoDebug] Trampolim desabilitado (trampolineEnabled=false) [Trigger]", this);
                return;
            }
            if (!_isTrampolineActive) return;
            // Se nenhuma layer foi configurada, aceita qualquer uma
            bool layerOk = interactiveLayers == 0 || ((interactiveLayers.value & (1 << other.gameObject.layer)) != 0);
            if (!layerOk)
            {
                if (debugLogs) Debug.Log($"[TrampoDebug] Trigger bloqueado por Layer: {LayerMask.LayerToName(other.gameObject.layer)}", this);
                return;
            }

            // Para trigger, não há contatos; assume entrada válida e aplica efeito
            if (debugLogs) Debug.Log("[TrampoDebug] Trigger enter | aplicando efeito", this);
            ApplyTrampolineEffect(other.transform);

            if (singleUse)
                _isTrampolineActive = false;
            else if (hasCooldown)
                StartCoroutine(RechargeTrampoline());
        }

        private void ApplyTrampolineEffect(Transform targetObject)
        {
            // Prioriza ECM
            if (targetObject.TryGetComponent(out ECMSaciController saci) && saci.movement != null)
            {
                var mv = saci.movement;
                var up = saci.transform.up;
                var verticalSpeed = Vector3.Dot(mv.velocity, up);
                // Calcula velocidade de impacto descendente preferindo métrica do controlador do Saci
                float downwardSpeed = saci != null ? Mathf.Max(0f, saci.GroundImpactDownwardSpeed) : Mathf.Max(0f, -verticalSpeed);
                float t = Mathf.InverseLerp(minDownwardSpeed, maxDownwardSpeed, downwardSpeed);
                float appliedImpulse;
                if (useProportionalImpulse)
                {
                    appliedImpulse = Mathf.Clamp(minLaunchForce + impulsePerDownwardUnit * downwardSpeed, minLaunchForce, launchForce);
                }
                else
                {
                    float curveT = (useImpactCurve && impactCurve != null) ? Mathf.Clamp01(impactCurve.Evaluate(Mathf.Clamp01(t))) : Mathf.Clamp01(t);
                    appliedImpulse = Mathf.Lerp(minLaunchForce, launchForce, curveT);
                }
                // Preserva e multiplica velocidade horizontal
                Vector3 lateral = Vector3.ProjectOnPlane(mv.velocity, up) * horizontalVelocityMultiplier;
                // Zera vertical para garantir altura consistente e aplica impulso vertical
                mv.velocity = new Vector3(lateral.x, 0f, lateral.z);
                mv.ApplyVerticalImpulse(appliedImpulse);
                // Libera grounding brevemente para permitir que o impulso "descole" do chão
                mv.DisableGrounding();
                // Boost imediato se o pulo já está em buffer no momento do contato
                bool hadQueuedAdvance = useJumpBoostWindow && queuedAdvanceBoost.ContainsKey(saci) && queuedAdvanceBoost[saci];
                if (useJumpBoostWindow && (saci.jump || hadQueuedAdvance))
                {
                    float boostImpulse = saci.jumpImpulse * jumpBoostImpulseFactor;
                    mv.ApplyVerticalImpulse(boostImpulse);
                    if (consumeJumpInputOnBoost) saci.jump = false;
                    if (hadQueuedAdvance)
                    {
                        queuedAdvanceBoost[saci] = false;
                    }
                    if (debugLogs)
                        Debug.Log($"[TrampoDebug] JumpBoost imediato | queuedAdvance={hadQueuedAdvance} | boostImpulse={boostImpulse:F2} | baseImpulse={appliedImpulse:F2}", this);
                }
                else if (useJumpBoostWindow)
                {
                    // Abre janela de tempo para detectar pulo e somar boost
                    StartCoroutine(TrampolineJumpBoostWindow(saci, appliedImpulse));
                    if (debugLogs)
                        Debug.Log($"[TrampoDebug] Janela de JumpBoost aberta por {(jumpBoostWindowSeconds + postAnimationExtraSeconds):F2}s (pós-contato)", this);
                }

                if (debugLogs)
                {
                    Debug.Log($"[TrampoDebug] ECM aplicado | downward={downwardSpeed:F2} | t={t:F2} | impulse={appliedImpulse:F2} | mode={(useProportionalImpulse ? "proportional" : (useImpactCurve ? "curve" : "linear"))} | lateral={lateral.magnitude:F2}", this);
                }
            }
            else if (targetObject.TryGetComponent(out Rigidbody rb))
            {
                // Fallback para objetos com Rigidbody padrão
                var up = transform.up;
                var rbVerticalSpeed = Vector3.Dot(rb.linearVelocity, up);
                float downwardSpeed = Mathf.Max(0f, -rbVerticalSpeed);
                float t = Mathf.InverseLerp(minDownwardSpeed, maxDownwardSpeed, downwardSpeed);
                float appliedImpulse;
                if (useProportionalImpulse)
                {
                    appliedImpulse = Mathf.Clamp(minLaunchForce + impulsePerDownwardUnit * downwardSpeed, minLaunchForce, launchForce);
                }
                else
                {
                    float curveT = (useImpactCurve && impactCurve != null) ? Mathf.Clamp01(impactCurve.Evaluate(Mathf.Clamp01(t))) : Mathf.Clamp01(t);
                    appliedImpulse = Mathf.Lerp(minLaunchForce, launchForce, curveT);
                }

                Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z) * horizontalVelocityMultiplier;
                Vector3 launchDirection = Quaternion.Euler(-launchAngle, 0, 0) * Vector3.up;
                rb.linearVelocity = horizontalVelocity;
                rb.AddForce(launchDirection * appliedImpulse, ForceMode.Impulse);

                if (debugLogs)
                {
                    Debug.Log($"[TrampoDebug] RB aplicado | downward={downwardSpeed:F2} | t={t:F2} | impulse={appliedImpulse:F2} | mode={(useProportionalImpulse ? "proportional" : (useImpactCurve ? "curve" : "linear"))} | lateral={horizontalVelocity.magnitude:F2}", this);
                }
            }

            if (useVisualAnimation) PlayTrampolineAnimation();
            if (useSound && trampolineSound != null) AudioSource.PlayClipAtPoint(trampolineSound, transform.position, soundVolume);
        }

        private IEnumerator TrampolineJumpBoostWindow(ECMSaciController saci, float baseImpulse)
        {
            float start = Time.time;
            float duration = jumpBoostWindowSeconds + postAnimationExtraSeconds;
            while (Time.time - start <= duration)
            {
                if (saci == null || saci.movement == null)
                    yield break;

                // Detecta pulo dentro da janela (usa buffer do ECMSaciController)
                if (saci.jump)
                {
                    float boostImpulse = saci.jumpImpulse * jumpBoostImpulseFactor;
                    saci.movement.ApplyVerticalImpulse(boostImpulse);
                    if (consumeJumpInputOnBoost) saci.jump = false;
                    if (debugLogs)
                        Debug.Log($"[TrampoDebug] JumpBoost (janela pós-contato) | dt={(Time.time - start):F3}s | boostImpulse={boostImpulse:F2} | baseImpulse={baseImpulse:F2}", this);
                    yield break;
                }
                yield return null;
            }
        }

        private void OnTriggerStay(Collider other)
        {
            if (!useJumpBoostWindow) return;
            var saci = other.GetComponentInParent<ECMSaciController>();
            if (saci == null || saci.movement == null) return;

            // Verifica aproximação descendente e proximidade vertical ao topo do trampolim para abrir janela adiantada
            float vy = saci.movement.velocity.y;
            float heightRel = saci.transform.position.y - transform.position.y;
            bool approaching = vy < 0f;
            bool nearTop = heightRel > 0f && heightRel <= advanceMaxDistance;

            if (approaching && nearTop && !advanceWindowOpen.Contains(saci))
            {
                advanceWindowOpen.Add(saci);
                StartCoroutine(TrampolineAdvanceJumpWindow(saci));
                if (debugLogs)
                    Debug.Log($"[TrampoDebug] Janela de JumpBoost adiantada aberta por {preContactAdvanceSeconds:F2}s | heightRel={heightRel:F2} | vy={vy:F2}", this);
            }
        }

        private IEnumerator TrampolineAdvanceJumpWindow(ECMSaciController saci)
        {
            float start = Time.time;
            while (Time.time - start <= preContactAdvanceSeconds)
            {
                if (saci == null || saci.movement == null)
                {
                    advanceWindowOpen.Remove(saci);
                    yield break;
                }

                if (saci.jump)
                {
                    queuedAdvanceBoost[saci] = true;
                    if (consumeJumpInputOnAdvance) saci.jump = false;
                    if (debugLogs)
                        Debug.Log($"[TrampoDebug] JumpBoost adiantado (pré-contato) | dt={(Time.time - start):F3}s | queuedAdvance=true", this);
                    advanceWindowOpen.Remove(saci);
                    yield break;
                }
                yield return null;
            }
            advanceWindowOpen.Remove(saci);
        }

        private void PlayTrampolineAnimation()
        {
            DOTween.Sequence()
                .Append(transform.DOScale(compressionScale, animationDuration * 0.3f).SetEase(Ease.OutQuad))
                .Append(transform.DOScale(extensionScale, animationDuration * 0.3f).SetEase(Ease.OutBack))
                .Append(transform.DOScale(_originalScale, animationDuration * 0.4f).SetEase(Ease.OutElastic));
        }

        private IEnumerator RechargeTrampoline()
        {
            _isTrampolineActive = false;
            if (_hasRenderer)
                _objectRenderer.material.DOColor(_originalColor * 0.5f, 0.3f);
            yield return new WaitForSeconds(cooldownTime);
            _isTrampolineActive = true;
            if (_hasRenderer)
                _objectRenderer.material.DOColor(_originalColor, 0.5f).SetEase(Ease.OutFlash, 2, 0);
        }

        public void ResetTrampoline()
        {
            _isTrampolineActive = true;
            if (_hasRenderer)
                _objectRenderer.material.DOColor(_originalColor, 0.5f).SetEase(Ease.OutFlash, 2, 0);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Vector3 direction = Quaternion.Euler(-launchAngle, 0, 0) * transform.up;
            Gizmos.DrawRay(transform.position, direction * 3);
            Gizmos.DrawWireSphere(transform.position + direction * 3, 0.2f);
        }
    }
}