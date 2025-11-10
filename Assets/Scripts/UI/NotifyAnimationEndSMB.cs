using UnityEngine;

/// <summary>
/// Dispara o evento de fim de animação pelo AnimationEndHandler.
/// Corrige a limitação do OnStateExit (que só é chamado quando há transição),
/// permitindo disparar também quando o estado termina mas permanece ativo.
/// </summary>
public class NotifyAnimationEndSMB : StateMachineBehaviour
{
    [Tooltip("Disparar ao sair do estado (requer transição)")]
    public bool triggerOnStateExit = true;

    [Tooltip("Disparar quando o tempo normalizado atingir o limiar no estado")]
    public bool triggerOnNormalizedTime = true;

    [Range(0f, 100f)]
    [Tooltip("Percentual do progresso da animação para disparar (0..100) — vale para loop e não-loop")]
    public float percentThreshold = 98f;

    [Tooltip("Chave do evento nomeado a ser disparado no AnimationEndHandler (vazio usa evento padrão)")]
    public string eventKey = "";

    [Tooltip("Garantir que o evento seja invocado apenas uma vez por ciclo")]
    public bool invokeOnce = true;

    private bool _invoked;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        _invoked = false;
        Debug.Log($"[NotifyAnimationEndSMB] Enter state '{stateInfo.shortNameHash}' | loop={stateInfo.loop} | speed={animator.speed} | cull={animator.cullingMode}");
    }

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (!triggerOnNormalizedTime) return;
        if (invokeOnce && _invoked) return;

        // Para estados não-loop, normalizedTime geralmente satura em 1 e não há transição.
        // Para estados em loop, usamos o ciclo (normalizedTime % 1).
        float nt = stateInfo.normalizedTime;
        float cycle = nt % 1f;
        float threshold01 = Mathf.Clamp(percentThreshold, 0f, 100f) / 100f;
        bool finishedNonLoop = !stateInfo.loop && Mathf.Clamp01(nt) >= threshold01;
        bool finishedLoopCycle = stateInfo.loop && cycle >= threshold01;
        bool notInTransition = !animator.IsInTransition(layerIndex);

        if ((finishedNonLoop || finishedLoopCycle) && notInTransition)
        {
            var handler = FindHandler(animator);
            if (handler != null)
            {
                handler.TriggerAnimationEventByKey(eventKey);
                _invoked = true;
                Debug.Log($"[NotifyAnimationEndSMB] Disparado por normalizedTime. key='{eventKey}' nt={nt:F2} cycle={cycle:F2} loop={stateInfo.loop} thr={threshold01:F2}");
            }
            else
            {
                Debug.LogWarning("[NotifyAnimationEndSMB] AnimationEndHandler não encontrado no GameObject do Animator.");
            }
        }
    }

    // Este método só é chamado quando o Animator realmente sai do estado (há transição)
    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (!triggerOnStateExit) return;
        if (invokeOnce && _invoked) return;

        var handler = FindHandler(animator);
        if (handler != null)
        {
            handler.TriggerAnimationEventByKey(eventKey);
            _invoked = true;
            Debug.Log($"[NotifyAnimationEndSMB] Disparado por OnStateExit (houve transição). key='{eventKey}'");
        }
        else
        {
            Debug.LogWarning("[NotifyAnimationEndSMB] AnimationEndHandler não encontrado no GameObject do Animator.");
        }
    }

    private AnimationEndHandler FindHandler(Animator animator)
    {
        var go = animator.gameObject;
        var handler = go.GetComponent<AnimationEndHandler>();
        if (handler == null) handler = go.GetComponentInChildren<AnimationEndHandler>(true);
        if (handler == null) handler = go.GetComponentInParent<AnimationEndHandler>();
        return handler;
    }
}