using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

public class UiPhaseController : MonoBehaviour
{
    public GameObject canvasUI;
    public GameObject defaultButton;
    [Header("Eventos")]
    public UnityEvent OnEnterUi;
    public UnityEvent OnExitUi;

    public void EnterUi(Managers.InputContextCoordinator icc)
    {
        var coordinator = icc ?? Managers.InputContextCoordinator.Instance;
        // Entra no contexto de UI sem habilitar interações imediatamente; controle manual abaixo.
        coordinator?.SetUiContext(enableUiInteractions: false);
        coordinator?.EnableUiInteractions(defaultButton);
        Focus(defaultButton);
        OnEnterUi?.Invoke();
    }

    public void ExitUi(Managers.InputContextCoordinator icc)
    {
        var coordinator = icc ?? Managers.InputContextCoordinator.Instance;
        coordinator?.DisableUiInteractions();
        EventSystem.current?.SetSelectedGameObject(null);
        OnExitUi?.Invoke();
    }

    private static void Focus(GameObject go)
    {
        if (go == null) return;
        var es = EventSystem.current;
        if (es != null) es.SetSelectedGameObject(go);
    }
}