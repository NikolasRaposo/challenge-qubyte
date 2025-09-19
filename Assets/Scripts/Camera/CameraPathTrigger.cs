using UnityEngine;
using Cinemachine;
using System.Collections;

// Este script, ao ser acionado, troca o 'Dolly Track' (o Path) de uma câmera virtual
// e, ao mesmo tempo, faz a transição suave do 'Position Offset' do Auto Dolly.
[RequireComponent(typeof(Collider))]
public class CameraPathTrigger : MonoBehaviour
{
    [Header("Configuração da Câmera")]
    [Tooltip("A Câmera Virtual Cinemachine que queremos controlar.")]
    public CinemachineVirtualCamera virtualCamera;

    [Header("Configurações do Novo Path")]
    [Tooltip("O novo 'Dolly Track' (Cinemachine Smooth Path) para o qual a câmera deve mudar.")]
    public CinemachinePathBase newPath;

    [Tooltip("(Opcional) Marque para definir uma posição inicial específica no novo path.")]
    public bool setStartPositionOnNewPath = false;
    [Tooltip("A posição inicial no novo path. Só é usado se a opção acima estiver marcada.")]
    public float startPathPosition = 0f;

    [Header("Configurações do Auto Dolly")]
    [Tooltip("O novo valor para o 'Position Offset' do Auto Dolly.")]
    public float newPositionOffset = 0f;
    [Tooltip("A duração da transição suave para o novo offset.")]
    public float offsetTransitionDuration = 1.5f;

    private CinemachineTrackedDolly trackedDolly;
    private static Coroutine activeCoroutine;

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
        if (virtualCamera == null) { Debug.LogError("Câmera Virtual não atribuída!", this); return; }

        trackedDolly = virtualCamera.GetCinemachineComponent<CinemachineTrackedDolly>();
        if (trackedDolly == null) { Debug.LogError("A Câmera Virtual não possui um Body 'Tracked Dolly'!", this); }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Se já estivermos usando este path, não fazemos nada para evitar re-acionamentos.
            if (trackedDolly.m_Path == newPath) return;


            // 2. (Opcional) Define a posição inicial no novo path.
            if (setStartPositionOnNewPath)
            {
                trackedDolly.m_PathPosition = startPathPosition;
            }
            // 1. Troca o Path. Esta é uma mudança imediata.
            trackedDolly.m_Path = newPath;
            Debug.Log($"Câmera mudou para o Path: {newPath.name}", this);

            // 3. Inicia a transição suave do Offset.
            //TransitionOffset(newPositionOffset, offsetTransitionDuration);
        }
    }

    private void TransitionOffset(float targetValue, float duration)
    {
        if (activeCoroutine != null)
        {
            StopCoroutine(activeCoroutine);
        }
        activeCoroutine = StartCoroutine(SmoothlyChangeOffset(targetValue, duration));
    }

    private IEnumerator SmoothlyChangeOffset(float targetValue, float duration)
    {
        if (trackedDolly == null) yield break;

        var autoDolly = trackedDolly.m_AutoDolly;
        float startValue = autoDolly.m_PositionOffset;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float progress = Mathf.Clamp01(timer / duration);

            // É importante ler a struct a cada frame para não sobrescrever outras propriedades
            var currentDolly = trackedDolly.m_AutoDolly;
            currentDolly.m_PositionOffset = Mathf.Lerp(startValue, targetValue, progress);
            trackedDolly.m_AutoDolly = currentDolly;

            yield return null;
        }

        var finalDolly = trackedDolly.m_AutoDolly;
        finalDolly.m_PositionOffset = targetValue;
        trackedDolly.m_AutoDolly = finalDolly;
    }
}