using System;
using UnityEngine;
using UnityEngine.Events;

public class LoadingPhaseController : MonoBehaviour
{
    public Animator loadingAnimator;
    public string startTrigger = "StartLoading";
    public string stopTrigger  = "StopLoading";
    [Header("Eventos")]
    public UnityEvent OnStartedLoading;
    public UnityEvent OnStoppedLoading;
    public UnityEvent OnLoadingFinishedEvent;
    public event Action OnLoadingFinished; // retrocompatibilidade para código

    private bool _isLoading;

    public void StartLoading(Managers.InputContextCoordinator icc)
    {
        if (_isLoading)
        {
            Debug.LogWarning("[LoadingPhaseController] StartLoading duplicado ignorado.");
            return;
        }
        _isLoading = true;
        var coordinator = icc ?? Managers.InputContextCoordinator.Instance;
        coordinator?.SetBlockInputContext();
        coordinator?.DisableUiInteractions();
        if (loadingAnimator != null)
        {
            EnsureParentsActive(loadingAnimator.gameObject);
            loadingAnimator.enabled = true;
            if (HasAnimatorTrigger(loadingAnimator, startTrigger))
                loadingAnimator.SetTrigger(startTrigger);
            else
                Debug.LogWarning($"[LoadingPhaseController] Trigger '{startTrigger}' ausente.");
            OnStartedLoading?.Invoke();
        }
        else
        {
            Debug.LogWarning("[LoadingPhaseController] Animator não atribuído.");
        }
    }

    public void StopLoading()
    {
        if (!_isLoading)
        {
            Debug.LogWarning("[LoadingPhaseController] StopLoading chamado sem loading ativo.");
            return;
        }
        if (loadingAnimator != null)
        {
            if (HasAnimatorTrigger(loadingAnimator, stopTrigger))
                loadingAnimator.SetTrigger(stopTrigger);
            OnStoppedLoading?.Invoke();
        }
        _isLoading = false;
    }

    public void NotifyFinished()
    {
        _isLoading = false;
        OnLoadingFinishedEvent?.Invoke();
        OnLoadingFinished?.Invoke();
    }

    private static void EnsureParentsActive(GameObject leaf)
    {
        if (leaf == null) return;
        var t = leaf.transform;
        while (t != null)
        {
            var go = t.gameObject;
            if (!go.activeSelf) go.SetActive(true);
            t = t.parent;
        }
    }

    private static bool HasAnimatorTrigger(Animator animator, string triggerName)
    {
        if (animator == null || string.IsNullOrEmpty(triggerName)) return false;
        foreach (var p in animator.parameters)
        {
            if (p.type == AnimatorControllerParameterType.Trigger && p.name == triggerName)
                return true;
        }
        return false;
    }
}