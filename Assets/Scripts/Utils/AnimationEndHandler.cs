using UnityEngine;
using UnityEngine.Events; // Necess�rio para usar UnityEvent

public class AnimationEndHandler : MonoBehaviour
{
    // Esta � a sua fun��o personaliz�vel, igual ao OnClick de um bot�o!
    // Voc� pode at� renome�-la para OnAnimEnd no Inspector se quiser.
    public UnityEvent OnAnimationFinished;

    // Este m�todo ser� chamado pelo nosso outro script (o State Machine Behaviour)
    public void TriggerAnimationEndEvent()
    {
        Debug.Log("Anima��o do estado terminou. Disparando evento!");
        OnAnimationFinished.Invoke();
    }
}