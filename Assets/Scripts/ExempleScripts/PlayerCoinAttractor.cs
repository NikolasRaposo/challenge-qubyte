using UnityEngine;

/// <summary>
/// Script de exemplo para configurar um ponto de atração personalizado para moedas.
/// Adicione este script ao objeto do jogador junto com PlayerMagneticField.
/// Este script funciona em conjunto com PlayerMagneticField, não criando triggers duplicados.
/// </summary>
public class PlayerCoinAttractor : MonoBehaviour
{
    [Tooltip("Transform que será usado como ponto de atração para as moedas")]
    public Transform pontoDeAtracao;
    
    [Tooltip("Cor do gizmo que mostra o campo magnético no editor")]
    public Color corGizmo = new Color(0, 0.5f, 1f, 0.3f); // Azul semi-transparente
    
    // Referência ao campo magnético criado pelo PlayerMagneticField
    private PlayerMagneticField campoMagneticoScript;
    
    private void Awake()
    {
        // Buscar o componente PlayerMagneticField no mesmo GameObject
        campoMagneticoScript = GetComponent<PlayerMagneticField>();
        
        if (campoMagneticoScript == null)
        {
            Debug.LogWarning("PlayerCoinAttractor requer PlayerMagneticField no mesmo GameObject!");
        }
        
        // Se não houver um ponto de atração definido, criar um
        if (pontoDeAtracao == null)
        {
            GameObject pontoAtracaoObj = new GameObject("PontoAtracaoMoedas");
            pontoAtracaoObj.transform.SetParent(transform);
            pontoAtracaoObj.transform.localPosition = new Vector3(0, 1.5f, 0); // Posição acima do jogador
            pontoDeAtracao = pontoAtracaoObj.transform;
        }
    }
    
    private void Start()
    {
        // Encontrar o trigger criado pelo PlayerMagneticField e adicionar o componente de atração
        GameObject campoMagneticoObj = transform.Find("CampoMagnetico")?.gameObject;
        
        if (campoMagneticoObj != null)
        {
            // Adicionar o componente MagneticTriggerHandler ao objeto do campo magnético
            MagneticTriggerHandler handler = campoMagneticoObj.GetComponent<MagneticTriggerHandler>();
            if (handler == null)
            {
                handler = campoMagneticoObj.AddComponent<MagneticTriggerHandler>();
            }
            
            // Configurar o handler com o ponto de atração
            handler.pontoDeAtracao = pontoDeAtracao;
            
            Debug.Log("PlayerCoinAttractor configurado com sucesso usando o campo magnético existente!");
        }
        else
        {
            Debug.LogError("Campo magnético não encontrado! Certifique-se de que PlayerMagneticField está no mesmo GameObject.");
        }
    }
    
    private void OnDrawGizmos()
    {
        // Desenhar o ponto de atração
        if (pontoDeAtracao != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(pontoDeAtracao.position, 0.1f);
            Gizmos.DrawLine(transform.position, pontoDeAtracao.position);
            
            // Desenhar uma linha pontilhada para indicar a conexão
            Gizmos.color = corGizmo;
            Vector3 direction = (pontoDeAtracao.position - transform.position).normalized;
            for (float i = 0; i < Vector3.Distance(transform.position, pontoDeAtracao.position); i += 0.2f)
            {
                Vector3 point = transform.position + direction * i;
                Gizmos.DrawSphere(point, 0.02f);
            }
        }
    }
}

/// <summary>
/// Componente auxiliar que gerencia as interações do trigger magnético.
/// Este componente é adicionado automaticamente ao GameObject do campo magnético.
/// </summary>
public class MagneticTriggerHandler : MonoBehaviour
{
    [HideInInspector]
    public Transform pontoDeAtracao;
    
    private void OnTriggerEnter(Collider other)
    {
        // Verificar se o objeto que entrou no trigger tem o componente CoinPickup
        CoinPickup moeda = other.GetComponent<CoinPickup>();
        if (moeda != null && pontoDeAtracao != null)
        {
            // Definir o ponto de atração personalizado para a moeda
            moeda.DefinirPontoAtracaoPersonalizado(pontoDeAtracao);
            Debug.Log("Ponto de atração personalizado definido para: " + moeda.gameObject.name);
        }
    }
}