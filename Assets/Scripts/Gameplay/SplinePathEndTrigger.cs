using UnityEngine;

// Componente simples para encaminhar OnTriggerEnter do EndTrigger ao controlador da spline
[RequireComponent(typeof(Collider))]
public class SplinePathEndTrigger : MonoBehaviour
{
    [Tooltip("Controller principal que gerencia entrada/saída do modo spline.")]
    public SplinePathGamePlayController controller;

    private Collider _col;

    private void Awake()
    {
        _col = GetComponent<Collider>();

        // Se não atribuído, tenta achar no pai
        if (controller == null)
            controller = GetComponentInParent<SplinePathGamePlayController>();

        // Aviso se o collider não for trigger
        if (_col != null && !_col.isTrigger)
            Debug.LogWarning($"{nameof(SplinePathEndTrigger)}: O collider não está como trigger. Defina 'Is Trigger' para funcionar corretamente.");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (controller == null)
            return;
        controller.HandleEndTriggerEnter(other);
    }
}