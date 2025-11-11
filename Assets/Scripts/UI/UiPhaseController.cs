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

    private bool _inUi;

    public void EnterUi(Managers.InputContextCoordinator icc)
    {
        if (_inUi)
        {
            Debug.LogWarning("[UiPhaseController] EnterUi duplicado ignorado.");
            return;
        }
        var coordinator = icc ?? Managers.InputContextCoordinator.Instance;
        // Entra no contexto de UI sem habilitar interações imediatamente; controle manual abaixo.
        coordinator?.SetUiContext(enableUiInteractions: false);
        coordinator?.EnableUiInteractions(defaultButton);
        Focus(defaultButton);
        _inUi = true;
        OnEnterUi?.Invoke();
    }

    public void ExitUi(Managers.InputContextCoordinator icc)
    {
        if (!_inUi)
        {
            Debug.LogWarning("[UiPhaseController] ExitUi chamado sem estar em UI.");
            return;
        }
        var coordinator = icc ?? Managers.InputContextCoordinator.Instance;
        coordinator?.DisableUiInteractions();
        EventSystem.current?.SetSelectedGameObject(null);
        _inUi = false;
        OnExitUi?.Invoke();
    }

    private static void Focus(GameObject go)
    {
        if (go == null) return;
        var es = EventSystem.current;
        if (es != null) es.SetSelectedGameObject(go);
    }
}