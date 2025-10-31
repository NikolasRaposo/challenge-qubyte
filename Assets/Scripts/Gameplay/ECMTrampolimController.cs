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
        [Header("Debug Toggles")]
        [Tooltip("Logs detalhados de avaliação de contato (Top/Side, dot, vy, etc.)")]
        [SerializeField] private bool debugContactLogs = false;
        [Tooltip("Logs de medição e uso da altura pré-contato (raycast e impulso por altura)")]
        [SerializeField] private bool debugHeightLogs = false;
        [Tooltip("Logs de JumpBoost (captura adiantada e aplicação imediata)")]
        [SerializeField] private bool debugJumpBoostLogs = false;
        [Tooltip("Logs do fallback para Rigidbody (impulso aplicado, lateral, etc.)")]
        [SerializeField] private bool debugRBLogs = false;
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

        [Header("Height-Based Impulse")]
        [Tooltip("Escala o impulso com base na altura máxima medida antes do contato via raycast.")]
        [SerializeField] private bool useHeightBasedImpulse = true;
        [Tooltip("Altura máxima considerada (m). A altura 0 m mapeia para 'minLaunchForce' e esta altura mapeia para 'launchForce'.")]
        [Range(1f, 100f)]
        [SerializeField] private float heightMaxMeters = 5f;
        [Tooltip("Usa curva para mapear altura normalizada (0..1) para ganho entre min e max impulse.")]
        [SerializeField] private bool useHeightCurve = false;
        [SerializeField] private AnimationCurve heightCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [Header("Boost Tuning")]
        [Tooltip("Multiplicador do teto de 'launchForce' quando NÃO houve boost (0.7 = reduz 30%).")]
        [Range(0f, 1f)]
        [SerializeField] private float noBoostMaxMultiplier = 0.7f;
        [Tooltip("Garante que o impulso final não ultrapasse 'launchForce' mesmo com boost.")]
        [SerializeField] private bool clampFinalImpulseToMax = true;

        [Header("Jump Assist Window")]
        [Tooltip("Quando ligado, abre uma pequena janela para somar o pulo do jogador ao impulso do trampolim.")]
        [SerializeField] private bool useJumpBoostWindow = true;
        [Tooltip("Quando verdadeiro, desativa a heurística de OnTriggerStay e usa apenas o trigger dedicado de avanço.")]
        [SerializeField] private bool useAdvanceTriggerOnly = true;
        [Tooltip("Permite duplo-pulo imediato após contato com o trampolim (ignora cooldown entre pulo no chão e duplo-pulo).")]
        [SerializeField] private bool allowImmediateDoubleJumpAfterTrampoline = true;
        [Tooltip("Tempo adiantado (pré-contato) para capturar o pulo enquanto o jogador está descendo e prestes a tocar o trampolim.")]
        [Range(0f, 0.5f)]
        [SerializeField] private float preContactAdvanceSeconds = 0.12f;
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
        [Tooltip("Fail-safe: tempo máximo para manter supressão após capturar pulo adiantado sem impacto. Após esse tempo a supressão é liberada.")]
        [Range(0.05f, 1.0f)]
        [SerializeField] private float advanceSuppressionFailSafeSeconds = 0.35f;

        // Estado interno para janela adiantada (pré-contato)
        private readonly System.Collections.Generic.Dictionary<ECMSaciController, bool> queuedAdvanceBoost = new System.Collections.Generic.Dictionary<ECMSaciController, bool>();
        private readonly System.Collections.Generic.HashSet<ECMSaciController> advanceWindowOpen = new System.Collections.Generic.HashSet<ECMSaciController>();
        // Debounce: evita aplicar efeito múltiplas vezes no mesmo frame por colisões repetidas
        private readonly System.Collections.Generic.Dictionary<ECMSaciController, int> lastEffectFrame = new System.Collections.Generic.Dictionary<ECMSaciController, int>();
        private readonly System.Collections.Generic.Dictionary<ECMSaciController, float> lastEffectTime = new System.Collections.Generic.Dictionary<ECMSaciController, float>();
        [Tooltip("Lockout curto entre usos por jogador para impedir múltiplas ativações em sequência.")]
        [Range(0f, 0.5f)]
        [SerializeField] private float effectLockoutSeconds = 0.08f;
        // Medição de altura pré-contato reportada pelo player (raycast)
        private readonly System.Collections.Generic.Dictionary<ECMSaciController, float> preContactMaxHeight = new System.Collections.Generic.Dictionary<ECMSaciController, float>();
        // Timestamp da última vez que a altura foi reportada para cada jogador
        private readonly System.Collections.Generic.Dictionary<ECMSaciController, float> lastHeightReportTime = new System.Collections.Generic.Dictionary<ECMSaciController, float>();
        [Tooltip("Tempo máximo (segundos) para manter altura armazenada sem novos reports. Após esse tempo, a altura é resetada.")]
        [Range(0.5f, 5f)]
        [SerializeField] private float heightStorageTimeoutSeconds = 2f;

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

        private void Update()
        {
            // Limpa alturas armazenadas que expiraram por timeout
            CleanupExpiredHeightStorage();
        }

        private void CleanupExpiredHeightStorage()
        {
            var keysToRemove = new System.Collections.Generic.List<ECMSaciController>();
            foreach (var kvp in lastHeightReportTime)
            {
                if (Time.time - kvp.Value > heightStorageTimeoutSeconds)
                {
                    keysToRemove.Add(kvp.Key);
                }
            }
            
            foreach (var key in keysToRemove)
            {
                preContactMaxHeight.Remove(key);
                lastHeightReportTime.Remove(key);
                if (debugHeightLogs)
                    Debug.Log($"[TrampoDebug] Altura armazenada expirou para jogador após {heightStorageTimeoutSeconds}s sem reports", this);
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!trampolineEnabled)
            {
                if (debugContactLogs) Debug.Log("[TrampoDebug] Trampolim desabilitado (trampolineEnabled=false)", this);
                return;
            }
            if (!_isTrampolineActive) return;
            // Se nenhuma layer foi configurada, aceita qualquer uma
            bool layerOk = interactiveLayers == 0 || ((interactiveLayers.value & (1 << collision.gameObject.layer)) != 0);
            if (!layerOk)
            {
                if (debugContactLogs) Debug.Log($"[TrampoDebug] Bloqueado por Layer: {LayerMask.LayerToName(collision.gameObject.layer)}", this);
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

            if (debugContactLogs)
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
                if (debugContactLogs) Debug.Log("[TrampoDebug] Trampolim desabilitado (trampolineEnabled=false) [Trigger]", this);
                return;
            }
            if (!_isTrampolineActive) return;
            // Se nenhuma layer foi configurada, aceita qualquer uma
            bool layerOk = interactiveLayers == 0 || ((interactiveLayers.value & (1 << other.gameObject.layer)) != 0);
            if (!layerOk)
            {
                if (debugContactLogs) Debug.Log($"[TrampoDebug] Trigger bloqueado por Layer: {LayerMask.LayerToName(other.gameObject.layer)}", this);
                return;
            }

            // Para trigger, não há contatos; assume entrada válida e aplica efeito
            if (debugContactLogs) Debug.Log("[TrampoDebug] Trigger enter | aplicando efeito", this);
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
                // Evita efeitos duplicados causados por múltiplas colisões no mesmo frame
                if (lastEffectFrame.TryGetValue(saci, out var frame) && frame == Time.frameCount)
                    return;
                lastEffectFrame[saci] = Time.frameCount;

                // Lockout curto entre frames consecutivos para evitar re-aplicação
                if (lastEffectTime.TryGetValue(saci, out var lastT) && (Time.time - lastT) < effectLockoutSeconds)
                    return;
                lastEffectTime[saci] = Time.time;

                var mv = saci.movement;
                var up = saci.transform.up;
                // Determina se houve boost adiantado
                bool hadQueuedAdvance = useJumpBoostWindow && queuedAdvanceBoost.ContainsKey(saci) && queuedAdvanceBoost[saci];
                float effectiveMax = hadQueuedAdvance ? launchForce : (launchForce * noBoostMaxMultiplier);
                float appliedImpulse;
                bool usedHeight = false;
                // Preferência: impulso baseado na altura máxima medida antes do contato
                if (useHeightBasedImpulse && preContactMaxHeight.TryGetValue(saci, out var maxH))
                {
                    float hClamped = Mathf.Clamp(maxH, 0f, heightMaxMeters);
                    float hT = heightMaxMeters > 0f ? (hClamped / heightMaxMeters) : 0f;
                    float curveT = (useHeightCurve && heightCurve != null) ? Mathf.Clamp01(heightCurve.Evaluate(Mathf.Clamp01(hT))) : Mathf.Clamp01(hT);
                    appliedImpulse = Mathf.Lerp(minLaunchForce, effectiveMax, curveT);
                    usedHeight = true;
                    if (debugHeightLogs)
                        Debug.Log($"[TrampoDebug] Impulso por Altura | maxH={maxH:F2} | hClamped={hClamped:F2} | hT={hT:F2} | curveT={curveT:F2} | effMax={effectiveMax:F2} | impulse={appliedImpulse:F2}", this);
                    preContactMaxHeight.Remove(saci);
                }
                else
                {
                    // Sem medição de altura, aplica impulso mínimo para segurança
                    appliedImpulse = minLaunchForce;
                }
                // Preserva e multiplica velocidade horizontal
                Vector3 lateral = Vector3.ProjectOnPlane(mv.velocity, up) * horizontalVelocityMultiplier;
                // Zera vertical para garantir altura consistente e aplica impulso vertical
                mv.velocity = new Vector3(lateral.x, 0f, lateral.z);
                // Calcula boost opcional e clampa ao teto absoluto se configurado
                float finalImpulse = appliedImpulse;
                if (useJumpBoostWindow && hadQueuedAdvance)
                {
                    float boostImpulse = saci.jumpImpulse * jumpBoostImpulseFactor;
                    finalImpulse = appliedImpulse + boostImpulse;
                    if (clampFinalImpulseToMax)
                        finalImpulse = Mathf.Min(launchForce, finalImpulse);
                    if (consumeJumpInputOnBoost) saci.jump = false;
                    // Consome e limpa o estado da janela adiantada
                    queuedAdvanceBoost.Remove(saci);
                    advanceWindowOpen.Remove(saci);
                    if (debugJumpBoostLogs)
                        Debug.Log($"[TrampoDebug] JumpBoost aplicado | boostImpulse={boostImpulse:F2} | finalImpulse={finalImpulse:F2} | baseImpulse={appliedImpulse:F2}", this);
                }

                mv.ApplyVerticalImpulse(finalImpulse);
                // Libera grounding brevemente para permitir que o impulso "descole" do chão
                mv.DisableGrounding();
                // Caso não haja pulo adiantado, não abre janela pós-contato (removido para evitar impulso no ar)

                // Em qualquer contato, encerramos a janela adiantada caso ainda esteja aberta
                advanceWindowOpen.Remove(saci);

                // Ao tocar o trampolim, remove supressão externa de pulo para normalizar controles
                saci.SetExternalJumpSuppression(false);

                // Reabilita duplo-pulo imediatamente, se configurado
                if (allowImmediateDoubleJumpAfterTrampoline)
                    saci.ResetGroundJumpCooldown();

                if (debugContactLogs)
                {
                    string mode = usedHeight ? "height" : "min";
                    Debug.Log($"[TrampoDebug] ECM aplicado | impulseFinal={finalImpulse:F2} | mode={mode} | hadBoost={hadQueuedAdvance} | lateral={lateral.magnitude:F2}", this);
                }
            }
            else if (targetObject.TryGetComponent(out Rigidbody rb))
            {
                // Fallback para objetos com Rigidbody padrão
                var up = transform.up;
                float appliedImpulse = launchForce;

                Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z) * horizontalVelocityMultiplier;
                Vector3 launchDirection = Quaternion.Euler(-launchAngle, 0, 0) * Vector3.up;
                rb.linearVelocity = horizontalVelocity;
                rb.AddForce(launchDirection * appliedImpulse, ForceMode.Impulse);

                if (debugRBLogs)
                {
                    Debug.Log($"[TrampoDebug] RB aplicado | impulse={appliedImpulse:F2} | lateral={horizontalVelocity.magnitude:F2}", this);
                }
            }

            if (useVisualAnimation) PlayTrampolineAnimation();
            if (useSound && trampolineSound != null) AudioSource.PlayClipAtPoint(trampolineSound, transform.position, soundVolume);
        }

        // Report da altura pré-contato vindo do player (raycast constante)
        public void ReportPreContactHeight(ECMSaciController saci, float distance)
        {
            if (saci == null) return;
            float clamped = Mathf.Clamp(distance, 0f, heightMaxMeters);
            if (!preContactMaxHeight.TryGetValue(saci, out var current))
                preContactMaxHeight[saci] = clamped;
            else
                preContactMaxHeight[saci] = Mathf.Max(current, clamped);
            
            // Atualiza timestamp do último report
            lastHeightReportTime[saci] = Time.time;
            
            if (debugHeightLogs)
                Debug.Log($"[TrampoDebug] ReportPreContactHeight | dist={distance:F2} | storedMax={preContactMaxHeight[saci]:F2}", this);
        }

        // Reseta altura armazenada para um jogador específico (chamado quando aterrissa fora do trampolim)
        public void ResetStoredHeight(ECMSaciController saci)
        {
            if (saci == null) return;
            bool hadStoredHeight = preContactMaxHeight.ContainsKey(saci);
            preContactMaxHeight.Remove(saci);
            lastHeightReportTime.Remove(saci);
            if (debugHeightLogs && hadStoredHeight)
                Debug.Log("[TrampoDebug] Altura armazenada resetada (aterrissagem fora do trampolim)", this);
        }

        // Janela pós-contato removida: todo boost deve ser capturado apenas via trigger adiantado

        // Notificação externa (via trigger de avanço) para abrir janela adiantada
        public void NotifyAdvanceTriggerEnter(ECMSaciController saci)
        {
            NotifyAdvanceTriggerEnter(saci, -1f);
        }

        // Permite definir uma duração customizada para a janela adiantada
        public void NotifyAdvanceTriggerEnter(ECMSaciController saci, float overrideDurationSeconds)
        {
            if (!useJumpBoostWindow) return;
            if (saci == null || saci.movement == null) return;

            if (advanceWindowOpen.Contains(saci)) return;
            advanceWindowOpen.Add(saci);

            if (overrideDurationSeconds > 0f)
                StartCoroutine(TrampolineAdvanceJumpWindowCustom(saci, overrideDurationSeconds));
            else
                StartCoroutine(TrampolineAdvanceJumpWindow(saci));

            if (debugJumpBoostLogs)
                Debug.Log($"[TrampoDebug] Janela de JumpBoost adiantada via Trigger aberta por {(overrideDurationSeconds > 0f ? overrideDurationSeconds : preContactAdvanceSeconds):F2}s", this);
        }

        // Versão customizada da janela adiantada (via trigger), com duração configurável
        private IEnumerator TrampolineAdvanceJumpWindowCustom(ECMSaciController saci, float duration)
        {
            float start = Time.time;
            while (Time.time - start <= duration)
            {
                if (saci == null || saci.movement == null)
                {
                    advanceWindowOpen.Remove(saci);
                    // Ao sair por invalidação, libera supressão (se estiver ativa)
                    saci.SetExternalJumpSuppression(false);
                    yield break;
                }

                if (saci.jump)
                {
                    queuedAdvanceBoost[saci] = true;
                    if (consumeJumpInputOnAdvance)
                    {
                        // Consome e suprime pulo até o contato para evitar duplo-pulo no ar
                        saci.ClearJumpBufferAndConsumeInput();
                        saci.SetExternalJumpSuppression(true);
                    }
                    if (debugJumpBoostLogs)
                        Debug.Log($"[TrampoDebug] JumpBoost adiantado (via Trigger) | dt={(Time.time - start):F3}s | queuedAdvance=true", this);
                    advanceWindowOpen.Remove(saci);
                    // Fail-safe: se não houver impacto em breve, libere supressão automaticamente
                    StartCoroutine(AdvanceSuppressionFailSafe(saci));
                    yield break;
                }
                yield return null;
            }
            advanceWindowOpen.Remove(saci);
            // Janela expirou sem contato/pulo: libera supressão para restaurar controles
            saci.SetExternalJumpSuppression(false);
        }

        private void OnTriggerStay(Collider other)
        {
            if (!useJumpBoostWindow || useAdvanceTriggerOnly) return;
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
                if (debugJumpBoostLogs)
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
                    // Ao sair por invalidação, libera supressão (se estiver ativa)
                    saci.SetExternalJumpSuppression(false);
                    yield break;
                }

                if (saci.jump)
                {
                    queuedAdvanceBoost[saci] = true;
                    if (consumeJumpInputOnAdvance)
                    {
                        // Consome e suprime pulo até o contato para evitar duplo-pulo no ar
                        saci.ClearJumpBufferAndConsumeInput();
                        saci.SetExternalJumpSuppression(true);
                    }
                    if (debugJumpBoostLogs)
                        Debug.Log($"[TrampoDebug] JumpBoost adiantado (pré-contato) | dt={(Time.time - start):F3}s | queuedAdvance=true", this);
                    advanceWindowOpen.Remove(saci);
                    // Fail-safe: se não houver impacto em breve, libere supressão automaticamente
                    StartCoroutine(AdvanceSuppressionFailSafe(saci));
                    yield break;
                }
                yield return null;
            }
            advanceWindowOpen.Remove(saci);
            // Janela expirou sem contato/pulo: libera supressão para restaurar controles
            saci.SetExternalJumpSuppression(false);
        }

        // Fail-safe: se a captura adiantada ocorrer mas não houver impacto em curto prazo,
        // libere a supressão e limpe o estado enfileirado para evitar travas.
        private IEnumerator AdvanceSuppressionFailSafe(ECMSaciController saci)
        {
            float start = Time.time;
            while (Time.time - start <= advanceSuppressionFailSafeSeconds)
            {
                // Se o estado de fila foi removido (impacto aplicado ou saída do trigger), encerramos
                if (!queuedAdvanceBoost.ContainsKey(saci))
                    yield break;
                yield return null;
            }
            // Tempo esgotado sem impacto: libera supressão e limpa fila
            saci.SetExternalJumpSuppression(false);
            if (queuedAdvanceBoost.ContainsKey(saci))
                queuedAdvanceBoost.Remove(saci);
            if (debugJumpBoostLogs)
                Debug.Log($"[TrampoDebug] Fail-safe: supressão liberada por timeout ({advanceSuppressionFailSafeSeconds:F2}s) sem impacto", this);
        }

        // Notificação de saída do trigger dedicado: encerra janela e libera supressão/janela
        public void NotifyAdvanceTriggerExit(ECMSaciController saci)
        {
            if (saci == null) return;
            advanceWindowOpen.Remove(saci);
            if (queuedAdvanceBoost.ContainsKey(saci))
                queuedAdvanceBoost.Remove(saci);
            saci.SetExternalJumpSuppression(false);
            if (debugJumpBoostLogs)
                Debug.Log("[TrampoDebug] Saída do trigger adiantado: supressão liberada e janela encerrada", this);
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