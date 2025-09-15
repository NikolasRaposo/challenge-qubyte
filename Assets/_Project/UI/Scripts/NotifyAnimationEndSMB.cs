using UnityEngine;

public class NotifyAnimationEndSMB : StateMachineBehaviour
{
    // Este método é chamado automaticamente pela Unity quando a animação do estado termina
    // e a transição para outro estado (ou para Exit) começa.
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // Procura pelo nosso script no GameObject que tem o Animator
        // e chama a função para disparar o evento.
        // O "?." é um operador seguro que evita erros se o componente não for encontrado.
        animator.GetComponent<AnimationEndHandler>()?.TriggerAnimationEndEvent();
    }
}