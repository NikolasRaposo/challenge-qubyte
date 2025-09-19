using UnityEngine;
using UnityEngine.Events; // Necessário para usar UnityEvent

public class AnimationEndHandler : MonoBehaviour
{
    // Esta é a sua função personalizável, igual ao OnClick de um botão!
    // Você pode até renomeá-la para OnAnimEnd no Inspector se quiser.
    public UnityEvent OnAnimationFinished;

    // Este método será chamado pelo nosso outro script (o State Machine Behaviour)
    public void TriggerAnimationEndEvent()
    {
        Debug.Log("Animação do estado terminou. Disparando evento!");
        OnAnimationFinished.Invoke();
    }
}