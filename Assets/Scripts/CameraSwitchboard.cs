using System.Collections.Generic;
using System.Linq;
using Cinemachine;
using UnityEngine;

/// <summary>
/// Controlador simples para alternar entre CinemachineVirtualCameras.
/// - Liste as câmeras no inspetor e marque 'active' em UMA delas para ativá-la.
/// - Garante que apenas uma fique ativa por vez (ajustando prioridades).
/// - Expõe métodos públicos para outros scripts ativarem por índice, nome ou referência.
/// </summary>
public class CameraSwitchboard : MonoBehaviour
{
    [System.Serializable]
    public class CameraEntry
    {
        [Tooltip("Câmera virtual a ser controlada.")]
        public CinemachineVirtualCamera camera;
        [Tooltip("Marque para ativar esta câmera (as outras serão desativadas).")]
        public bool active;
    }

    [Header("Câmeras Virtuais")]
    [SerializeField] private List<CameraEntry> cameras = new List<CameraEntry>();

    [Header("Prioridades")]
    [Tooltip("Prioridade aplicada à câmera ativa.")]
    [SerializeField] private int activePriority = 20;
    [Tooltip("Prioridade aplicada às câmeras inativas.")]
    [SerializeField] private int inactivePriority = 10;

    [Header("Opções")]
    [Tooltip("Mostra logs de ativação/desativação no Console.")]
    [SerializeField] private bool debugLogs = false;

    // Último índice selecionado via toggles (ajuda a resolver múltiplos 'true' no OnValidate)
    [SerializeField, HideInInspector] private int lastSelectedIndex = -1;

    private void Awake()
    {
        // Garantir estado consistente em runtime
        ApplyFromFlags();
    }

    private void OnValidate()
    {
        // Em edição/play, manter somente uma câmera com 'active == true'
        EnforceSingleSelection();
        ApplyFromFlags();
    }

    private void EnforceSingleSelection()
    {
        int selected = -1;
        for (int i = 0; i < cameras.Count; i++)
        {
            var entry = cameras[i];
            if (entry != null && entry.active)
            {
                if (selected == -1)
                {
                    selected = i;
                }
                else
                {
                    // Desmarca extras para garantir apenas um true
                    entry.active = false;
                }
            }
        }

        if (selected != -1)
            lastSelectedIndex = selected;
    }

    private void ApplyFromFlags()
    {
        int selected = -1;
        for (int i = 0; i < cameras.Count; i++)
        {
            var entry = cameras[i];
            bool isActive = entry != null && entry.active && entry.camera != null;
            if (isActive)
            {
                selected = i;
                break; // já garantimos apenas um 'true'
            }
        }

        if (selected >= 0)
            ApplyActivation(selected);
        else
            ApplyAllInactive();
    }

    private void ApplyAllInactive()
    {
        foreach (var entry in cameras)
        {
            if (entry?.camera == null) continue;
            entry.camera.Priority = inactivePriority;
        }
        if (debugLogs)
            Debug.Log($"[CameraSwitchboard] Nenhuma câmera ativa selecionada. Todas definidas como prioridade {inactivePriority}.", this);
    }

    private void ApplyActivation(int index)
    {
        for (int i = 0; i < cameras.Count; i++)
        {
            var entry = cameras[i];
            if (entry?.camera == null) continue;

            bool makeActive = i == index;
            entry.active = makeActive;
            entry.camera.Priority = makeActive ? activePriority : inactivePriority;
        }

        if (debugLogs)
        {
            var activeCam = cameras[index]?.camera;
            Debug.Log($"[CameraSwitchboard] Ativada: '{(activeCam != null ? activeCam.name : "<null>")}' (prio {activePriority}). Outras em {inactivePriority}.", this);
        }
    }

    // API pública

    /// <summary>
    /// Ativa uma câmera pela referência. As demais são desativadas.
    /// </summary>
    public void SetActiveCamera(CinemachineVirtualCamera camera)
    {
        if (camera == null) return;
        int idx = cameras.FindIndex(e => e != null && e.camera == camera);
        if (idx < 0) return;
        ApplyActivation(idx);
    }

    /// <summary>
    /// Ativa uma câmera pelo índice na lista. As demais são desativadas.
    /// </summary>
    public void SetActiveCameraByIndex(int index)
    {
        if (index < 0 || index >= cameras.Count) return;
        ApplyActivation(index);
    }

    /// <summary>
    /// Ativa uma câmera pelo nome (exato). As demais são desativadas.
    /// </summary>
    public void SetActiveCameraByName(string cameraName)
    {
        if (string.IsNullOrEmpty(cameraName)) return;
        int idx = cameras.FindIndex(e => e != null && e.camera != null && e.camera.name == cameraName);
        if (idx < 0) return;
        ApplyActivation(idx);
    }

    /// <summary>
    /// Retorna a câmera atualmente ativa (com 'active' true), ou null se nenhuma.
    /// </summary>
    public CinemachineVirtualCamera GetActiveCamera()
    {
        var entry = cameras.FirstOrDefault(e => e != null && e.active && e.camera != null);
        return entry?.camera;
    }
}