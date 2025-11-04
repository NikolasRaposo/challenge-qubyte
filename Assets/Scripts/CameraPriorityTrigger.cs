using System;
using UnityEngine;
using Cinemachine;

/// <summary>
/// Define a prioridade de câmeras virtuais do Cinemachine quando o jogador entra em um trigger.
/// Possui um modo especial para checkpoints que busca e desativa a câmera atualmente ativa.
/// </summary>
[RequireComponent(typeof(Collider))]
public class CameraPriorityTrigger : MonoBehaviour
{
    [Header("Configuração de Câmeras")]
    [Tooltip("A câmera que deve se tornar ATIVA ao entrar neste trigger.")]
    public CinemachineVirtualCamera cameraToActivate;

    [Tooltip("A câmera que deve se tornar INATIVA ao entrar neste trigger. (Ignorado se 'For Checkpoint' estiver marcado)")]
    public CinemachineVirtualCamera cameraToDeactivate;

    [Header("Modo de Operação")]
    [Tooltip("Se marcado, o script irá procurar por QUALQUER câmera com prioridade alta (20) e a desativará, ativando a 'cameraToActivate'. Útil para checkpoints onde a câmera anterior é desconhecida.")]
    public bool forCheckpoint = false;

    [Header("Valores de Prioridade")]
    [Tooltip("Prioridade alta para a câmera ativa.")]
    private int activePriority = 20;

    [Tooltip("Prioridade baixa para a câmera inativa.")]
    private int inactivePriority = 10;

    private void Awake()
    {
        // Garante que o collider seja um trigger
        GetComponent<Collider>().isTrigger = true;
    }

    [Obsolete("Obsolete")]
    private void OnTriggerEnter(Collider other)
    {
        // A lógica só é executada se o objeto com a tag "Player" entrar no trigger
        if (other.CompareTag("Player"))
        {
            if (forCheckpoint)
            {
                // MODO CHECKPOINT: Encontra a câmera ativa e a desativa
                if (cameraToActivate == null) return;

                // Busca todas as Câmeras Virtuais na cena
                CinemachineVirtualCamera[] allCameras = FindObjectsOfType<CinemachineVirtualCamera>();

                foreach (var cam in allCameras)
                {
                    // Se encontrar a câmera que está atualmente ativa, diminui sua prioridade
                    if (cam.Priority == activePriority)
                    {
                        cam.Priority = inactivePriority;
                        break; // Assume que apenas uma câmera tem prioridade alta por vez
                    }
                }

                // Ativa a câmera designada para este trigger/checkpoint
                cameraToActivate.Priority = activePriority;
            }
            else
            {
                // MODO PADRÃO: Usa as câmeras definidas nos campos
                if (cameraToActivate == null || cameraToDeactivate == null) return;

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
}
