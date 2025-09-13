using UnityEngine;
using Cinemachine;
using System.Collections;

[RequireComponent(typeof(Collider))]
public class CameraVelocityTrigger : MonoBehaviour
{
    [Header("Configuração da Câmera")]
    [Tooltip("A Câmera Virtual Cinemachine que queremos controlar.")]
    public CinemachineVirtualCamera virtualCamera;

    [Header("Configuração Direcional de Offset")]
    [Tooltip("O offset aplicado quando a VELOCIDADE do jogador está na direção 'para frente' (Z azul) do trigger.")]
    public float forwardEntryOffset = 0f;

    [Tooltip("O offset aplicado quando a VELOCIDADE do jogador está na direção 'para trás' (contra o Z azul) do trigger.")]
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
        // Primeiro, garantimos que é o jogador.
        if (other.CompareTag("Player"))
        {
            // Tentamos pegar o CharacterController do jogador.
            CharacterController playerController = other.GetComponent<CharacterController>();
            if (playerController == null)
            {
                Debug.LogWarning("O objeto do jogador entrou no trigger mas não tem um CharacterController.", other);
                return;
            }

            // Pegamos a velocidade do jogador.
            Vector3 playerVelocity = playerController.velocity;

            // Ignoramos movimentos muito pequenos para evitar acionamentos acidentais se o jogador estiver "tremendo".
            if (playerVelocity.sqrMagnitude < 0.1f)
            {
                // Se o jogador não está se movendo, não fazemos nada para evitar um estado ambíguo.
                return;
            }

            // A LÓGICA CORRETA: Comparamos o 'forward' do trigger com a direção da velocidade do jogador.
            float dot = Vector3.Dot(transform.forward, playerVelocity.normalized);

            if (dot >= 0)
            {
                // A velocidade aponta para frente em relação ao trigger.
                TransitionTo(forwardEntryOffset);
            }
            else
            {
                // A velocidade aponta para trás em relação ao trigger.
                TransitionTo(backwardEntryOffset);
            }
        }
    }

    private void TransitionTo(float targetValue)
    {
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