using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX; // Import necessário para trabalhar com o VFX Graph

[ExecuteAlways] // Permite que o script seja executado no editor e em tempo de execução
public class Vfx_GraphController : MonoBehaviour
{
    public VisualEffect vfxGraph; // Referência ao VFX Graph
    public Transform targetObject; // Objeto cuja posição será usada como TargetPosition

    // Update é chamado a cada frame
    void Update()
    {
        UpdateTargetPosition();
    }

    private void UpdateTargetPosition()
    {
        if (vfxGraph != null && targetObject != null)
        {
            // Converte a posição do target para o espaço local do VFX Graph
            Vector3 localPosition = vfxGraph.transform.InverseTransformPoint(targetObject.position);

            // Atualiza a variável TargetPosition no VFX Graph
            vfxGraph.SetVector3("TargetPosition", localPosition);
        }
    }
}