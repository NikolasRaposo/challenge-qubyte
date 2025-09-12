using UnityEngine;
using Cinemachine;
using System.Collections;

[RequireComponent(typeof(Collider))]
public class DirectionalCameraTrigger : MonoBehaviour
{
    [Header("Configuração da Câmera")]
    [Tooltip("A Câmera Virtual Cinemachine que queremos controlar.")]
    public CinemachineVirtualCamera virtualCamera;

    [Header("Configuração Direcional de Offset")]
    [Tooltip("O offset aplicado quando o jogador entra movendo-se na direção 'para frente' (Z azul) do trigger.")]
    public float forwardEntryOffset = 0f;

    [Tooltip("O offset aplicado quando o jogador entra movendo-se na direção 'para trás' (contra o Z azul) do trigger.")]
    public float backwardEntryOffset = -0.46f;

    [Header("Configuração da Transição")]
    [Tooltip("A duração da transição suave da câmera.")]
    public float transitionDuration = 1.5f;

    private CinemachineTrackedDolly trackedDolly;
    private static Coroutine activeCoroutine;

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
        if (virtualCamera == null)
        {
            Debug.LogError("A Câmera Virtual não foi atribuída!", this);
            return;
        }
        trackedDolly = virtualCamera.GetCinemachineComponent<CinemachineTrackedDolly>();
        if (trackedDolly == null)
        {
            Debug.LogError("A Câmera Virtual não possui um Body 'Tracked Dolly'!", this);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Calcula um vetor do centro do trigger para a posição do jogador.
            Vector3 directionToPlayer = other.transform.position - transform.position;

            // Usa o Produto Escalar para saber se o jogador está na frente ou atrás do trigger.
            // O transform.forward é a direção do eixo Z azul do objeto do trigger.
            float dot = Vector3.Dot(transform.forward, directionToPlayer.normalized);

            // Se o 'dot product' for positivo, o jogador está se movendo na mesma direção do Z azul.
            if (dot >= 0)
            {
                // O jogador veio de trás e está entrando pela "porta dos fundos", indo para frente.
                // Aplicamos o offset de "entrada para frente".
                TransitionTo(forwardEntryOffset);
            }
            else // Se for negativo, ele está vindo da direção oposta.
            {
                // O jogador veio da frente e está entrando pela "porta da frente", voltando.
                // Aplicamos o offset de "entrada para trás".
                TransitionTo(backwardEntryOffset);
            }
        }
    }

    private void TransitionTo(float targetValue)
    {
        // Se já houver uma transição acontecendo, podemos pará-la, mas
        // o Cinemachine é inteligente o suficiente para apenas começar a nova.
        if (activeCoroutine != null)
        {
            StopCoroutine(activeCoroutine);
        }
        activeCoroutine = StartCoroutine(SmoothlyChangeAutoDollyOffset(targetValue));
    }

    private IEnumerator SmoothlyChangeAutoDollyOffset(float targetValue)
    {
        if (trackedDolly == null) yield break;

        var autoDolly = trackedDolly.m_AutoDolly;
        float startValue = autoDolly.m_PositionOffset;
        float timer = 0f;

        while (timer < transitionDuration)
        {
            timer += Time.deltaTime;
            float progress = Mathf.Clamp01(timer / transitionDuration);
            float newOffsetValue = Mathf.Lerp(startValue, targetValue, progress);

            autoDolly.m_PositionOffset = newOffsetValue;
            trackedDolly.m_AutoDolly = autoDolly;

            yield return null;
        }

        var finalDolly = trackedDolly.m_AutoDolly;
        finalDolly.m_PositionOffset = targetValue;
        trackedDolly.m_AutoDolly = finalDolly;
    }
}