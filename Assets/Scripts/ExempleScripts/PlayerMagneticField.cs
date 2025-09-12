using UnityEngine;

/// <summary>
/// Adiciona um campo magnético ao jogador para atrair moedas automaticamente.
/// Este script deve ser adicionado ao mesmo GameObject que contém o ThirdPersonController.
/// </summary>
[RequireComponent(typeof(ThirdParty.StarterAssets.ThirdPersonController.Scripts.ThirdPersonController))]
public class PlayerMagneticField : MonoBehaviour
{
    [Tooltip("Raio do campo magnético ao redor do jogador")]
    public float raioMagnetico = 3f;
    
    [Tooltip("Cor do gizmo que mostra o campo magnético no editor")]
    public Color corGizmo = new Color(0, 0.5f, 1f, 0.2f); // Azul semi-transparente
    
    [Tooltip("Offset vertical do campo magnético em relação ao jogador")]
    public float offsetVertical = 0.5f;
    
    // Referência ao collider do campo magnético
    private SphereCollider campoMagnetico;
    
    /// <summary>
    /// Inicializa o campo magnético ao redor do jogador.
    /// </summary>
    private void Start()
    {
        // Cria um GameObject filho para o campo magnético
        GameObject campoMagneticoObj = new GameObject("CampoMagnetico");
        campoMagneticoObj.transform.SetParent(transform);
        campoMagneticoObj.transform.localPosition = new Vector3(0, offsetVertical, 0);
        
        // Adiciona um SphereCollider configurado como trigger
        campoMagnetico = campoMagneticoObj.AddComponent<SphereCollider>();
        campoMagnetico.isTrigger = true;
        campoMagnetico.radius = raioMagnetico;
        
        // Define a tag correta para interagir com as moedas
        campoMagneticoObj.tag = "MagneticTrigger";
        
        // Adiciona um Rigidbody para garantir que o trigger funcione corretamente
        Rigidbody rb = campoMagneticoObj.AddComponent<Rigidbody>();
        rb.isKinematic = true; // Não afetado pela física
        rb.useGravity = false; // Sem gravidade
        
        Debug.Log("Campo magnético criado com sucesso!");
    }
    
    /// <summary>
    /// Atualiza o tamanho do campo magnético se o raio for alterado no inspetor.
    /// </summary>
    private void Update()
    {
        if (campoMagnetico != null && campoMagnetico.radius != raioMagnetico)
        {
            campoMagnetico.radius = raioMagnetico;
        }
    }
    
    /// <summary>
    /// Desenha gizmos no editor para visualizar o campo magnético.
    /// </summary>
    private void OnDrawGizmos()
    {
        Gizmos.color = corGizmo;
        Gizmos.DrawSphere(transform.position + new Vector3(0, offsetVertical, 0), raioMagnetico);
    }
}