using Enemies;
using UnityEngine;

// Este script n�o � um MonoBehaviour, e sim um StateMachineBehaviour.
// Ele n�o vai em um GameObject, mas sim em um estado do Animator.
public class AttackStateExitBehaviour : StateMachineBehaviour
{
    // OnStateExit � chamado pela engine do Unity quando a anima��o do estado TERMINA
    // e o Animator est� fazendo a transi��o para outro estado.
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // 1. Precisamos encontrar o script principal do nosso chefe.
        //    O GetComponent no animator vai procurar no mesmo GameObject onde o Animator est�.
        //    Lembre-se de substituir "BossController" pelo nome real do seu script de chefe.
        BossContext bossContextController = animator.GetComponent<BossContext>();

        // 2. Se encontramos o script, chamamos um m�todo p�blico nele para avisar
        //    que a anima��o de ataque acabou.
        if (bossContextController != null)
        {
            bossContextController.OnAttackAnimationFinished();
        }
        else
        {
            Debug.LogError("N�o foi poss�vel encontrar o script 'BossController' no objeto " + animator.gameObject.name);
        }
    }
}