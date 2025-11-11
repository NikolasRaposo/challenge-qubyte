using UnityEngine;

// Utilitário simples para disparar triggers de Animator via UnityEvents/Animation Events
[AddComponentMenu("UI/Animator Trigger Invoker")]
public class AnimatorTriggerInvoker : MonoBehaviour
{
    [Header("Referências")]
    [Tooltip("Animator alvo para receber o trigger")]
    [SerializeField] private Animator animator;

    [Header("Validações e Garantias")]
    [Tooltip("Validar se o parâmetro Trigger existe no Animator antes de disparar")] 
    [SerializeField] private bool validateParameter = true;

    [Header("Garantias opcionais")]
    [Tooltip("Garante que toda a cadeia de pais do objeto do Animator esteja ativa antes de disparar")] 
    [SerializeField] private bool ensureParentsActive = true;
    [Tooltip("Habilita o componente Animator se estiver desabilitado")] 
    [SerializeField] private bool enableAnimatorIfDisabled = true;

    // Método sem parâmetros para usar direto em UnityEvent/Animation Event
    // Removido por clareza visual do fluxo: use InvokeTriggerByName(string)

    // Permite passar o nome do trigger diretamente pelo UnityEvent
    public void InvokeTriggerByName(string name)
    {
        if (animator == null)
        {
            Debug.LogWarning("[AnimatorTriggerInvoker] Animator não atribuído");
            return;
        }
        if (string.IsNullOrEmpty(name))
        {
            Debug.LogWarning("[AnimatorTriggerInvoker] Nome de trigger vazio");
            return;
        }
        InvokeInternal(animator, name, null);
    }

    // Permite disparar pelo id/hash diretamente (menos comum em UnityEvent)
    public void InvokeTriggerById(int id)
    {
        if (animator == null)
        {
            Debug.LogWarning("[AnimatorTriggerInvoker] Animator não atribuído");
            return;
        }
        InvokeInternal(animator, null, id);
    }

    // Permite disparar em um Animator diferente, mantendo as garantias
    public void InvokeOnAnimatorByName(Animator target, string name)
    {
        if (target == null)
        {
            Debug.LogWarning("[AnimatorTriggerInvoker] Animator alvo é nulo");
            return;
        }
        if (string.IsNullOrEmpty(name))
        {
            Debug.LogWarning("[AnimatorTriggerInvoker] Nome de trigger vazio");
            return;
        }
        InvokeInternal(target, name, null);
    }

    private void InvokeInternal(Animator target, string name, int? id)
    {
        if (target == null)
        {
            Debug.LogWarning("[AnimatorTriggerInvoker] Animator alvo não atribuído");
            return;
        }

        if (ensureParentsActive)
        {
            EnsureParentsActive(target.gameObject);
        }

        if (enableAnimatorIfDisabled && !target.enabled)
        {
            target.enabled = true;
        }

        if (id.HasValue)
        {
            target.SetTrigger(id.Value);
            return;
        }

        if (string.IsNullOrEmpty(name))
        {
            Debug.LogWarning("[AnimatorTriggerInvoker] Nome do trigger não definido");
            return;
        }

        if (validateParameter && !HasAnimatorTrigger(target, name))
        {
            Debug.LogError($"[AnimatorTriggerInvoker] Parâmetro Trigger '{name}' não encontrado no Animator '{target.name}'.");
            return;
        }

        target.SetTrigger(name);
    }

    private static bool HasAnimatorTrigger(Animator target, string trigger)
    {
        if (target == null || string.IsNullOrEmpty(trigger)) return false;
        try
        {
            foreach (var p in target.parameters)
            {
                if (p.type == AnimatorControllerParameterType.Trigger && p.name == trigger)
                    return true;
            }
        }
        catch { }
        return false;
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
}