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

    public void Play()
    {
        if (director == null)
            director = GetComponent<PlayableDirector>();
        if (director != null)
        {
            director.enabled = true;
            director.time = 0;
            director.Play();
            OnPlay?.Invoke();
        }
        else
        {
            Debug.LogWarning("[CinematicPhaseController] PlayableDirector não encontrado.");
        }
    }

    public void NotifyEnd()
    {
        OnCinematicFinishedEvent?.Invoke();
        OnCinematicFinished?.Invoke();
    }
}