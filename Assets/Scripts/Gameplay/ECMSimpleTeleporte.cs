using UnityEngine;
using Player;
using System.Collections;

// Teleportador simples: ao detectar o jogador, move-o instantaneamente para um destino.
// Compatível com ECM (ECMSaciController), evitando conflitos comuns de runtime.
[DefaultExecutionOrder(100)]
public class ECMSimpleTeleporte : MonoBehaviour
{
    [Header("Destino")]
    [Tooltip("Transform de destino para onde o jogador será teleportado.")]
    public Transform destination;

    [Header("Opções")]
    [Tooltip("Se verdadeiro, aplica também a rotação do destino ao jogador.")]
    public bool applyDestinationRotation = true;
    [Tooltip("Se verdadeiro, impede múltiplos teleportes consecutivos pelo mesmo trigger.")]
    public bool oneShot = true;
    [Tooltip("Tag esperada para detectar o jogador (fallback caso não encontre ECMSaciController).")]
    public string playerTag = "Player";

    [Header("ECM - Segurança de Estado")]
    [Tooltip("Zera a velocidade do jogador antes de teleportar (recomendado para ECM).")]
    public bool resetVelocity = true;
    [Tooltip("Desabilita o grounding logo após teleportar para evitar snap/jitter.")]
    public bool disableGroundingAfterTeleport = true;
    [Tooltip("Limpa buffer de pulo para evitar acionamentos indesejados após o teleporte.")]
    public bool clearJumpBuffer = true;

    [Header("Ativação")]
    [Tooltip("Atraso mínimo (s) após carregar/ativar antes de permitir teleporte.")]
    [Min(0f)] public float activationDelay = 0f;

    private float _enabledTime;

    [Header("Hold pós-teleporte")]
    [Tooltip("Se verdadeiro, mantém o jogador travado no destino por um tempo antes de liberar.")]
    public bool holdAfterTeleport = false;
    [Tooltip("Tempo (segundos) que o jogador permanecerá travado no destino antes de ser liberado.")]
    [Min(0f)] public float holdDuration = 0.5f;

    [Header("Debug")]
    [Tooltip("Quando ligado, imprime logs detalhados do fluxo de teleporte (espera de pausa, execução, hold, etc.).")]
    public bool verboseLogs = false;

    private bool _consumed = false;

    private void OnEnable()
    {
        _enabledTime = Time.time;
    }

    private bool IsActive()
    {
        return Time.time >= _enabledTime + activationDelay;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Proteção contra teleporte imediato no spawn (opcional via activationDelay)
        if (!IsActive())
        {
            if (destination != null)
                Debug.Log($"[ECMSimpleTeleporte] Ignorado (delay ativo {activationDelay:F2}s). '{other.name}' no {Time.time - _enabledTime:F2}s. Destino {destination.position}", this);
            return;
        }

        if (_consumed && oneShot)
            return;

        // Tenta detectar o controlador ECM do jogador (mais robusto do que apenas comparar tag)
        ECMSaciController saci = other.GetComponentInParent<ECMSaciController>();
        if (saci == null && !string.IsNullOrEmpty(playerTag) && !other.CompareTag(playerTag))
            return;

        // Se não encontramos o ECMSaciController, tentamos no próprio objeto com a tag
        if (saci == null)
            saci = other.GetComponentInParent<ECMSaciController>();

        // Validação de destino
        if (destination == null)
        {
            Debug.LogWarning($"{nameof(ECMSimpleTeleporte)}: Nenhum 'destination' atribuído. Teleporte cancelado.", this);
            return;
        }

        // Inicia fluxo de teleporte com garantia de ordem (espera sair do modo spline/pausa)
        Debug.Log($"[ECMSimpleTeleporte] Triggerado por '{other.name}'. Preparando teleporte para {destination.position}", this);
        StartCoroutine(TeleportFlow(saci != null ? saci.gameObject : other.gameObject, saci));
        if (oneShot)
            _consumed = true;
    }

    [Header("Ordem de Execução")]
    [Tooltip("Aguarda 'pause == false' no ECM antes de teleportar (saída do modo spline).")]
    public bool waitUntilECMUnpaused = true;
    [Tooltip("Tempo máximo para aguardar ECM sair de pausa (segundos).")]
    public float maxWaitUnpauseSeconds = 0.25f;
    [Tooltip("Deferir o teleporte para o final do frame atual (garante que outros OnTriggerEnter executem primeiro).")]
    public bool deferTeleportToEndOfFrame = false;

    private IEnumerator TeleportFlow(GameObject playerObject, ECMSaciController saci)
    {
        // Opcionalmente esperar o fim do frame para permitir que outros handlers processem (ex.: EndTrigger)
        if (deferTeleportToEndOfFrame)
        {
            if (verboseLogs)
                Debug.Log("[ECMSimpleTeleporte] Deferindo para fim do frame antes de teleportar.", this);
            yield return new WaitForEndOfFrame();
        }

        // Se estiver em modo spline / ECM pausado, aguardar breve janela até sair
        if (waitUntilECMUnpaused && saci != null)
        {
            float deadline = Time.time + Mathf.Max(0f, maxWaitUnpauseSeconds);
            // 'pause' é usado pelo controlador durante o modo spline
            bool initialPause = saci.pause;
            float waitStart = Time.time;
            while (saci.pause && Time.time < deadline)
                yield return null; // aguarda próximo frame
            if (verboseLogs)
            {
                float waited = Time.time - waitStart;
                Debug.Log($"[ECMSimpleTeleporte] Espera por ECM sair de pausa: inicial={initialPause}, final={saci.pause}, aguardado={waited:F3}s (limite={maxWaitUnpauseSeconds:F2}s)", this);
            }
        }

        bool teleported = TeleportPlayer(playerObject, saci);

        if (!teleported)
        {
            if (verboseLogs)
                Debug.LogWarning("[ECMSimpleTeleporte] Teleporte não executado (player/destination nulos).", this);
            yield break;
        }

        // Se solicitado, manter o jogador travado por holdDuration e liberar ao final (apenas se teleporte ocorreu)
        if (holdAfterTeleport && saci != null && teleported)
        {
            // Evita restaurar velocidades antigas ao sair de pausa
            saci.restoreVelocityOnResume = false;
            saci.pause = true;

            // Garante posição exata no destino ao iniciar o hold
            if (applyDestinationRotation)
                saci.transform.SetPositionAndRotation(destination.position, destination.rotation);
            else
                saci.transform.position = destination.position;

            yield return new WaitForSeconds(Mathf.Max(0f, holdDuration));
            saci.pause = false;
            if (verboseLogs)
                Debug.Log($"[ECMSimpleTeleporte] Hold ECM concluído ({holdDuration:F2}s). pause=false.", this);
        }
        else if (holdAfterTeleport && saci == null && teleported)
        {
            // Fallback: se não há ECMSaciController disponível, tenta segurar usando Rigidbody
            var rb = playerObject.GetComponent<Rigidbody>();
            if (rb != null)
            {
                if (verboseLogs)
                    Debug.Log($"[ECMSimpleTeleporte] Hold via Rigidbody por {holdDuration:F2}s (ECM não encontrado).", this);
                var prevVel = rb.linearVelocity;
                bool prevKinematic = rb.isKinematic;

                // Evita setar velocidade em corpo cinemático
                if (!prevKinematic)
                    rb.linearVelocity = Vector3.zero;

                rb.isKinematic = true;
                yield return new WaitForSeconds(Mathf.Max(0f, holdDuration));

                rb.isKinematic = prevKinematic;
                if (!prevKinematic)
                    rb.linearVelocity = prevVel;
            }
            else if (verboseLogs)
            {
                Debug.LogWarning("[ECMSimpleTeleporte] Hold solicitado, mas nem ECM nem Rigidbody encontrado para fallback.", this);
            }
        }
    }

    /// <summary>
    /// Teleporta o jogador (compatível com ECM se o controlador for fornecido).
    /// </summary>
    /// <param name="playerObject">GameObject do jogador a ser teleportado.</param>
    /// <param name="saci">Controlador ECM do jogador (opcional, para aplicar resets com segurança).</param>
    public bool TeleportPlayer(GameObject playerObject, ECMSaciController saci = null)
    {
        if (playerObject == null || destination == null)
            return false;

        Vector3 fromPos = playerObject.transform.position;

        // Compatibilidade ECM: reset de velocidade e grounding para evitar conflitos
        if (saci != null && saci.movement != null)
        {
            if (resetVelocity)
            {
                var rb = saci.movement.cachedRigidbody;
                if (rb != null && rb.isKinematic)
                {
                    // Evita erro ao tentar setar velocidade em corpo cinemático
                    saci.restoreVelocityOnResume = false;
                    if (verboseLogs)
                        Debug.Log("[ECMSimpleTeleporte] ECM Rigidbody está kinematic; pulando resetVelocity para evitar erro.", this);
                }
                else
                {
                    saci.movement.velocity = Vector3.zero;
                }
            }

            if (clearJumpBuffer)
                saci.ClearJumpBufferAndConsumeInput();

            if (disableGroundingAfterTeleport)
                saci.movement.DisableGrounding();
        }

        // Reposiciona instantaneamente (usa SetPositionAndRotation para minimizar side-effects)
        if (applyDestinationRotation)
            playerObject.transform.SetPositionAndRotation(destination.position, destination.rotation);
        else
            playerObject.transform.position = destination.position;

        Debug.Log($"[ECMSimpleTeleporte] Teleportando '{playerObject.name}' de {fromPos} para {destination.position}", this);

        return true;
    }

    // Gizmos para depuração visual do destino
    private void OnDrawGizmosSelected()
    {
        if (destination == null)
            return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(destination.position, 0.25f);
        Gizmos.DrawLine(transform.position, destination.position);
    }
}
