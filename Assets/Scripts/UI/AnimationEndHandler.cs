using UnityEngine;
using UnityEngine.Events; // Necessário para usar UnityEvent
using System.Collections.Generic;

/// <summary>
/// Handler de fim de animação capaz de gerenciar múltiplos eventos nomeados.
/// Mantém compatibilidade com o evento padrão (OnAnimationFinished).
/// </summary>
public class AnimationEndHandler : MonoBehaviour
{
    [System.Serializable]
    public class NamedAnimEvent
    {
        [Tooltip("Chave única para identificar o evento")] public string key;
        [Tooltip("Evento associado à chave")] public UnityEvent onEvent;
    }

    [Header("Evento padrão (retrocompatibilidade)")]
    public UnityEvent OnAnimationFinished;

    [Header("Eventos nomeados")]
    [Tooltip("Lista de eventos que podem ser disparados por chave")] public List<NamedAnimEvent> events = new List<NamedAnimEvent>();

    /// <summary>
    /// Dispara o evento padrão (retrocompatibilidade).
    /// </summary>
    public void TriggerAnimationEndEvent()
    {
        Debug.Log("[AnimationEndHandler] Estado terminou. Disparando evento padrão.");
        OnAnimationFinished?.Invoke();
    }

    /// <summary>
    /// Dispara um evento nomeado pela chave. Se a chave for vazia ou não existir,
    /// cai no evento padrão quando disponível.
    /// </summary>
    public void TriggerAnimationEventByKey(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            Debug.Log("[AnimationEndHandler] Chave vazia/nula — usando evento padrão.");
            OnAnimationFinished?.Invoke();
            return;
        }

        var entry = events?.Find(e => e.key == key);
        if (entry != null && entry.onEvent != null)
        {
            Debug.Log($"[AnimationEndHandler] Disparando evento nomeado '{key}'.");
            entry.onEvent.Invoke();
        }
        else
        {
            Debug.LogWarning($"[AnimationEndHandler] Evento '{key}' não encontrado — usando padrão se disponível.");
            OnAnimationFinished?.Invoke();
        }
    }
}