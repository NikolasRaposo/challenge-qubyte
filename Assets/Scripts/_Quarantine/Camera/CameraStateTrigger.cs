using UnityEngine;
using Cinemachine;
using System.Collections;

// Este script tem UMA ÚNICA RESPONSABILIDADE: aplicar um conjunto de configurações de câmera
// quando o jogador entra em seu trigger.
[RequireComponent(typeof(Collider))]
public class CameraStateTrigger : MonoBehaviour
{
    [Header("Configuração Geral")]
    [Tooltip("A Câmera Virtual Cinemachine que queremos controlar.")]
    public CinemachineVirtualCamera virtualCamera;

    [Header("Configurações a Aplicar na Entrada")]
    [Tooltip("Marque se deseja alterar o estado do Auto Dolly.")]
    public bool controlDolly = false;
    [Tooltip("O estado para o qual o Auto Dolly será definido.")]
    public bool dollyEnabledState = true;

    [Tooltip("Marque se deseja alterar a Posição no Path.")]
    public bool controlPathPosition = false;
    [Tooltip("A posição alvo no Path.")]
    public float targetPathPosition = 0f;
    [Tooltip("A duração da transição do Path.")]
    public float pathTransitionDuration = 1.5f;

    [Tooltip("Marque se deseja alterar o Offset do Auto Dolly.")]
    public bool controlOffset = false;
    [Tooltip("O offset alvo.")]
    public float targetOffset = 0f;
    [Tooltip("A duração da transição do Offset.")]
    public float offsetTransitionDuration = 1.5f;


    private CinemachineTrackedDolly trackedDolly;
    private static Coroutine offsetCoroutine;
    private static Coroutine pathCoroutine;

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
        if (virtualCamera == null) { Debug.LogError("Câmera Virtual não atribuída!", this); return; }
        trackedDolly = virtualCamera.GetCinemachineComponent<CinemachineTrackedDolly>();
        if (trackedDolly == null) { Debug.LogError("Câmera Virtual não tem um Body 'Tracked Dolly'!", this); }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (controlDolly) SetAutoDollyState(dollyEnabledState);
            if (controlPathPosition) TransitionPathPosition(targetPathPosition, pathTransitionDuration);
            if (controlOffset) TransitionOffset(targetOffset, offsetTransitionDuration);
        }
    }

    // --- MÉTODOS DE CONTROLE ---

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