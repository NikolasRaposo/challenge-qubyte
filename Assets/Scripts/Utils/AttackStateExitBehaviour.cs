using UnityEngine;

// Este script não é um MonoBehaviour, e sim um StateMachineBehaviour.
// Ele não vai em um GameObject, mas sim em um estado do Animator.
public class AttackStateExitBehaviour : StateMachineBehaviour
{
    // OnStateExit é chamado pela engine do Unity quando a animação do estado TERMINA
    // e o Animator está fazendo a transição para outro estado.
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // 1. Precisamos encontrar o script principal do nosso chefe.
        //    O GetComponent no animator vai procurar no mesmo GameObject onde o Animator está.
        //    Lembre-se de substituir "BossController" pelo nome real do seu script de chefe.
        Boss.CapeloboBoss bossController = animator.GetComponent<Boss.CapeloboBoss>();

        // 2. Se encontramos o script, chamamos um método público nele para avisar
        //    que a animação de ataque acabou.
        if (bossController != null)
        {
            bossController.OnAttackAnimationFinished();
        }
        else
        {
            Debug.LogError("Não foi possível encontrar o script 'BossController' no objeto " + animator.gameObject.name);
        }
    }
}