using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Events;

public class CinematicPhaseController : MonoBehaviour
{
    public PlayableDirector director;
    [Header("Eventos")]
    public UnityEvent OnPlay;
    public UnityEvent OnCinematicFinishedEvent;
    public event Action OnCinematicFinished; // retrocompatibilidade

    [SerializeField] private float playCooldownMs = 200f;
    private bool _isPlaying;
    private float _lastPlayUnscaledMs;

    public void Play()
    {
        var nowMs = Time.unscaledTime * 1000f;
        if (_isPlaying && (nowMs - _lastPlayUnscaledMs) < playCooldownMs)
        {
            Debug.LogWarning("[CinematicPhaseController] Play ignorado (cooldown/idempotência).");
            return;
        }

        if (director == null)
            director = GetComponent<PlayableDirector>();
        if (director != null)
        {
            director.enabled = true;
            try { director.time = 0; } catch { /* alguns diretors podem não permitir set */ }
            director.Play();
            _isPlaying = true;
            _lastPlayUnscaledMs = nowMs;
            OnPlay?.Invoke();
        }
        else
        {
            Debug.LogWarning("[CinematicPhaseController] PlayableDirector não encontrado.");
        }
    }

    public void NotifyEnd()
    {
        _isPlaying = false;
        OnCinematicFinishedEvent?.Invoke();
        OnCinematicFinished?.Invoke();
    }
}