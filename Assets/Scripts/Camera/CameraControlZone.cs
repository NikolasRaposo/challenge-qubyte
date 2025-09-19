using UnityEngine;
using Cinemachine;
using System.Collections;

[RequireComponent(typeof(Collider))]
public class CameraControlZone : MonoBehaviour
{
    [Header("Configura��o Geral")]
    [Tooltip("A C�mera Virtual Cinemachine que queremos controlar.")]
    public CinemachineVirtualCamera virtualCamera;
    // --- COMPORTAMENTO NA DIRE��O 'FORWARD' (Z AZUL) ---
    [Header("Dire��o: Para Frente (Forward)")]
    [Tooltip("Controla o Auto Dolly ao ir para FRENTE?")]
    public bool controlDollyOnForward = false;
    [Tooltip("Estado do Auto Dolly ao ir para FRENTE (true=Ligado).")]
    public bool forwardDollyState = true;

    [Tooltip("Controla o Path Position ao ir para FRENTE?")]
    public bool controlPathOnForward = false;
    [Tooltip("Posi��o alvo no Path ao ir para FRENTE.")]
    public float forwardPathPosition = 0f;
    [Tooltip("Dura��o da transi��o do Path ao ir para FRENTE.")]
    public float forwardPathTransitionDuration = 1.5f;

    [Tooltip("Controla o Offset ao ir para FRENTE?")]
    public bool controlOffsetOnForward = false;
    [Tooltip("Offset alvo ao ir para FRENTE.")]
    public float forwardOffset = 0f;
    [Tooltip("Dura��o da transi��o do Offset ao ir para FRENTE.")]
    public float forwardOffsetTransitionDuration = 1.5f;

    // --- COMPORTAMENTO NA DIRE��O 'BACKWARD' (CONTRA Z AZUL) ---
    [Header("Dire��o: Para Tr�s (Backward)")]
    [Tooltip("Controla o Auto Dolly ao ir para TR�S?")]
    public bool controlDollyOnBackward = false;
    [Tooltip("Estado do Auto Dolly ao ir para TR�S (true=Ligado).")]
    public bool backwardDollyState = true;

    [Tooltip("Controla o Path Position ao ir para TR�S?")]
    public bool controlPathOnBackward = false;
    [Tooltip("Posi��o alvo no Path ao ir para TR�S.")]
    public float backwardPathPosition = 0f;
    [Tooltip("Dura��o da transi��o do Path ao ir para TR�S.")]
    public float backwardPathTransitionDuration = 1.5f;

    [Tooltip("Controla o Offset ao ir para TR�S?")]
    public bool controlOffsetOnBackward = false;
    [Tooltip("Offset alvo ao ir para TR�S.")]
    public float backwardOffset = -0.46f;
    [Tooltip("Dura��o da transi��o do Offset ao ir para TR�S.")]
    public float backwardOffsetTransitionDuration = 1.5f;


    private CinemachineTrackedDolly trackedDolly;
    private static Coroutine offsetCoroutine;
    private static Coroutine pathCoroutine;

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
        if (virtualCamera == null) { Debug.LogError("C�mera Virtual n�o atribu�da!", this); return; }
        trackedDolly = virtualCamera.GetCinemachineComponent<CinemachineTrackedDolly>();
        if (trackedDolly == null) { Debug.LogError("C�mera Virtual n�o tem um Body 'Tracked Dolly'!", this); }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            CharacterController playerController = other.GetComponent<CharacterController>();
            if (playerController == null || playerController.velocity.sqrMagnitude < 0.1f) return;

            float dot = Vector3.Dot(transform.forward, playerController.velocity.normalized);

            if (dot >= 0) // Movendo para FRENTE
            {
                if (controlDollyOnForward) SetAutoDollyState(forwardDollyState);
                if (controlPathOnForward) TransitionPathPosition(forwardPathPosition, forwardPathTransitionDuration);
                if (controlOffsetOnForward) TransitionOffset(forwardOffset, forwardOffsetTransitionDuration);
            }
            else // Movendo para TR�S
            {
                if (controlDollyOnBackward) SetAutoDollyState(backwardDollyState);
                if (controlPathOnBackward) TransitionPathPosition(backwardPathPosition, backwardPathTransitionDuration);
                if (controlOffsetOnBackward) TransitionOffset(backwardOffset, backwardOffsetTransitionDuration);
            }
        }
    }

    // --- M�TODOS DE CONTROLE ---

    private void SetAutoDollyState(bool isEnabled)
    {
        if (trackedDolly == null) return;
        var autoDolly = trackedDolly.m_AutoDolly;
        if (autoDolly.m_Enabled == isEnabled) return;
        autoDolly.m_Enabled = isEnabled;
        trackedDolly.m_AutoDolly = autoDolly;
    }

    private void TransitionOffset(float targetValue, float duration)
    {
        if (offsetCoroutine != null) StopCoroutine(offsetCoroutine);
        offsetCoroutine = StartCoroutine(SmoothlyChangeOffset(targetValue, duration));
    }

    private void TransitionPathPosition(float targetValue, float duration)
    {
        if (pathCoroutine != null) StopCoroutine(pathCoroutine);
        pathCoroutine = StartCoroutine(SmoothlyChangePathPosition(targetValue, duration));
    }

    // --- COROUTINES ---

    private IEnumerator SmoothlyChangeOffset(float targetValue, float duration)
    {
        if (trackedDolly == null) yield break;
        float startValue = trackedDolly.m_AutoDolly.m_PositionOffset;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            var currentDolly = trackedDolly.m_AutoDolly;
            currentDolly.m_PositionOffset = Mathf.Lerp(startValue, targetValue, timer / duration);
            trackedDolly.m_AutoDolly = currentDolly;
            yield return null;
        }
        var finalDolly = trackedDolly.m_AutoDolly;
        finalDolly.m_PositionOffset = targetValue;
        trackedDolly.m_AutoDolly = finalDolly;
    }

    private IEnumerator SmoothlyChangePathPosition(float targetValue, float duration)
    {
        if (trackedDolly == null) yield break;
        float startValue = trackedDolly.m_PathPosition;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            trackedDolly.m_PathPosition = Mathf.Lerp(startValue, targetValue, timer / duration);
            yield return null;
        }
        trackedDolly.m_PathPosition = targetValue;
    }
}