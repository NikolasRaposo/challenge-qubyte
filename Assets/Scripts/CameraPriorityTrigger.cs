using UnityEngine;
using Cinemachine;

// Este script define a prioridade de duas câmeras virtuais para
// dizer ao CinemachineBrain qual delas deve se tornar ativa.
[RequireComponent(typeof(Collider))]
public class CameraPriorityTrigger : MonoBehaviour
{
    [Header("Configuração de Prioridade")]
    [Tooltip("A câmera que deve se tornar ATIVA ao entrar neste trigger.")]
    public CinemachineVirtualCamera cameraToActivate;

    [Tooltip("A câmera que deve se tornar INATIVA ao entrar neste trigger.")]
    public CinemachineVirtualCamera cameraToDeactivate;

    [Header("Valores de Prioridade")]
    [Tooltip("Prioridade alta para a câmera ativa.")]
    private int activePriority = 20;

    [Tooltip("Prioridade baixa para a câmera inativa.")]
    private int inactivePriority = 10;

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Se a câmera a ser ativada já não for a mais importante, a ativamos.
            if (cameraToActivate.Priority < activePriority)
            {
                Debug.Log($"Ativando câmera: {cameraToActivate.name}", this);
                cameraToActivate.Priority = activePriority;
                cameraToDeactivate.Priority = inactivePriority;
            }
        }
    }
}