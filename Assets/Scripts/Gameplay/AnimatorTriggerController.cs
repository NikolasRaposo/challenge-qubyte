using UnityEngine;
using System.Collections.Generic; // Necessário para usar List

/// <summary>
/// Uma classe auxiliar serializável para aparecer no Inspector.
/// Contém a referência a um Animator e o nome de um trigger.
/// </summary>
[System.Serializable]
public class AnimatorTriggerTarget
{
    [Tooltip("O componente Animator que você quer controlar.")]
    public Animator animator;

    [Tooltip("O nome exato (sensível a maiúsculas) do parâmetro do tipo Trigger no Animator.")]
    public string triggerName;
}

/// <summary>
/// Um script flexível que ativa uma lista de triggers em diferentes Animators.
/// Pode ser chamado por UnityEvents, como o OnClick de um botão.
/// </summary>
public class AnimatorTriggerController : MonoBehaviour
{
    [Header("Lista de Triggers a Ativar")]
    [Tooltip("Adicione aqui todos os animators e os nomes dos triggers que você deseja disparar.")]
    public List<AnimatorTriggerTarget> triggersToActivate;

    /// <summary>
    /// Ativa TODOS os triggers configurados na lista de uma só vez.
    /// </summary>
    public void ActivateAllTriggers()
    {
        if (triggersToActivate == null || triggersToActivate.Count == 0)
        {
            Debug.LogWarning("Nenhum trigger para ativar foi configurado na lista.", this);
            return;
        }

        // Percorre cada item na lista configurada no Inspector
        foreach (var target in triggersToActivate)
        {
            Activate(target);
        }
    }

    /// <summary>
    /// Ativa um trigger específico da lista baseado no seu índice.
    /// O primeiro item da lista tem índice 0, o segundo tem índice 1, e assim por diante.
    /// </summary>
    /// <param name="index">O índice do trigger na lista que você deseja ativar.</param>
    public void ActivateTriggerByIndex(int index)
    {
        // Verifica se a lista existe e se o índice é válido
        if (triggersToActivate == null || index < 0 || index >= triggersToActivate.Count)
        {
            Debug.LogError($"Índice ({index}) inválido. A lista tem {triggersToActivate?.Count ?? 0} elementos.", this);
            return;
        }

        // Pega o item específico da lista usando o índice e o ativa
        Activate(triggersToActivate[index]);
    }

    /// <summary>
    /// Método privado auxiliar para ativar um único alvo.
    /// </summary>
    /// <param name="target">O alvo a ser ativado.</param>
    private void Activate(AnimatorTriggerTarget target)
    {
        // Verifica se o animator e o nome do trigger são válidos antes de tentar usá-los
        if (target != null && target.animator != null && !string.IsNullOrEmpty(target.triggerName))
        {
            // Ativa o trigger no animator especificado
            target.animator.SetTrigger(target.triggerName);
            Debug.Log($"Trigger '{target.triggerName}' ativado no animator '{target.animator.name}'.", this);
        }
        else
        {
            Debug.LogWarning("Um item na lista de triggers está incompleto (animator ou nome do trigger faltando).", this);
        }
    }
}

