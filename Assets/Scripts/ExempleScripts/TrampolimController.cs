using System.Collections;
using UnityEngine;
using DG.Tweening;
using ThirdParty.StarterAssets.ThirdPersonController.Scripts;

/// <summary>
/// Controla o comportamento de trampolins no jogo.
/// Permite configurar diferentes tipos de trampolim com várias intensidades e efeitos.
/// Pode ser usado independentemente ou em conjunto com outros componentes como BoxInteractor.
/// </summary>
[AddComponentMenu("Gameplay/Trampolim Controller")]
public class TrampolimController : MonoBehaviour
{
    [Header("Configurações de Força")]
    [Tooltip("Força aplicada ao jogador ao usar o trampolim")]
    [Range(5f, 30f)]
    public float forcaImpulso = 10f;

    [Tooltip("Multiplicador de velocidade horizontal ao usar o trampolim (1 = mantém a velocidade atual)")]
    [Range(0.5f, 2f)]
    public float multiplicadorVelocidadeHorizontal = 1f;

    [Header("Configurações de Uso")]
    [Tooltip("Se marcado, o trampolim só pode ser usado uma vez")]
    public bool usoUnico = false;

    [Tooltip("Se marcado, o trampolim precisa de tempo para recarregar entre usos")]
    public bool tempoRecarga = false;

    [Tooltip("Tempo em segundos para o trampolim recarregar após o uso")]
    [Range(0.5f, 10f)]
    public float tempoDeRecarga = 2f;

    [Header("Feedback Visual")]
    [Tooltip("Se marcado, o trampolim terá animação visual ao ser usado")]
    public bool animacaoVisual = true;

    [Tooltip("Escala máxima durante a animação de compressão")]
    public Vector3 escalaCompressao = new Vector3(1.2f, 0.5f, 1.2f);

    [Tooltip("Escala máxima durante a animação de extensão")]
    public Vector3 escalaExtensao = new Vector3(0.8f, 1.5f, 0.8f);

    [Tooltip("Duração da animação completa em segundos")]
    [Range(0.1f, 1f)]
    public float duracaoAnimacao = 0.3f;

    [Header("Feedback de Som")]
    [Tooltip("Se marcado, o trampolim emitirá som ao ser usado")]
    public bool usarSom = true;

    [Tooltip("Som reproduzido quando o trampolim é acionado")]
    public AudioClip somTrampolim;

    [Range(0f, 1f)]
    public float volumeSom = 0.7f;

    [Header("Configurações Avançadas")]
    [Tooltip("Camadas que podem interagir com o trampolim")]
    public LayerMask camadasInterativas;

    [Tooltip("Ângulo de direção do impulso em graus (0 = para cima)")]
    [Range(-45f, 45f)]
    public float anguloDirecao = 0f;

    [Tooltip("Se marcado, o trampolim só é ativado quando o objeto está caindo sobre ele")]
    public bool apenasQuandoCaindo = true;

    // Variáveis privadas
    private Vector3 escalaOriginal;
    private bool trampolimAtivo = true;
    private Renderer rend;
    private Color corOriginal;
    private bool temRenderer;

    // Referência para o collider do trampolim
    private Collider trampolimCollider;

    void Start()
    {
        // Armazenar a escala original para animações
        escalaOriginal = transform.localScale;

        // Tentar obter o renderer para feedback visual
        temRenderer = TryGetComponent(out rend);
        if (temRenderer)
        {
            corOriginal = rend.material.color;
        }

        // Obter o collider do trampolim
        trampolimCollider = GetComponent<Collider>();
        if (trampolimCollider == null)
        {
            Debug.LogWarning("Trampolim sem collider! Adicione um collider ao objeto para que o trampolim funcione corretamente.");
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        // Verificar se o objeto que colidiu está nas camadas interativas
        if (((1 << collision.gameObject.layer) & camadasInterativas) == 0)
            return;

        // Verificar se o trampolim está ativo
        if (!trampolimAtivo)
            return;

        // Verificar se o objeto está caindo (se a configuração estiver ativada)
        if (apenasQuandoCaindo)
        {
            // Verificar se a colisão veio de cima (usando a normal da colisão)
            ContactPoint contato = collision.GetContact(0);
            if (contato.normal.y < 0.7f) // Se a normal não apontar para cima o suficiente
                return;

            // Verificar se o objeto está se movendo para baixo
            Rigidbody rb = collision.gameObject.GetComponent<Rigidbody>();
            if (rb != null && rb.velocity.y > 0)
                return;
        }

        // Aplicar o efeito do trampolim
        AplicarEfeitoTrampolim(collision.transform);

        // Gerenciar o uso do trampolim
        if (usoUnico)
        {
            trampolimAtivo = false;
        }
        else if (tempoRecarga)
        {
            StartCoroutine(RecarregarTrampolim());
        }
    }

    /// <summary>
    /// Aplica o efeito de trampolim ao objeto que colidiu
    /// </summary>
    /// <param name="objetoColisor">Transform do objeto que colidiu com o trampolim</param>
    public void AplicarEfeitoTrampolim(Transform objetoColisor)
    {
        // Calcular a direção do impulso baseada no ângulo configurado
        Vector3 direcaoImpulso = Quaternion.Euler(anguloDirecao, transform.eulerAngles.y, 0) * Vector3.up;

        // Verificar se é o jogador (ThirdPersonController)
        if (objetoColisor.TryGetComponent(out ThirdPersonController player))
        {
            // Usar o método específico do ThirdPersonController
            player.ApplyUpwardForce(forcaImpulso);
        }
        // Caso contrário, verificar se tem um Rigidbody para aplicar força física
        else if (objetoColisor.TryGetComponent(out Rigidbody rb))
        {
            // Preservar parte da velocidade horizontal se configurado
            Vector3 velocidadeHorizontal = new Vector3(rb.velocity.x, 0, rb.velocity.z) * multiplicadorVelocidadeHorizontal;
            
            // Zerar a velocidade atual e aplicar o impulso na direção calculada
            rb.velocity = velocidadeHorizontal;
            rb.AddForce(direcaoImpulso * forcaImpulso, ForceMode.Impulse);
        }

        // Executar feedback visual se configurado
        if (animacaoVisual)
        {
            ExecutarAnimacaoTrampolim();
        }

        // Executar feedback sonoro se configurado
        if (usarSom && somTrampolim != null)
        {
            AudioSource.PlayClipAtPoint(somTrampolim, transform.position, volumeSom);
        }
    }

    /// <summary>
    /// Executa a animação visual do trampolim usando DOTween
    /// </summary>
    private void ExecutarAnimacaoTrampolim()
    {
        // Criar uma sequência de animação
        Sequence sequencia = DOTween.Sequence();

        // Primeira parte: compressão
        sequencia.Append(transform.DOScale(escalaCompressao, duracaoAnimacao * 0.3f).SetEase(Ease.OutQuad));
        
        // Segunda parte: extensão
        sequencia.Append(transform.DOScale(escalaExtensao, duracaoAnimacao * 0.3f).SetEase(Ease.OutBack));
        
        // Terceira parte: retorno à escala original
        sequencia.Append(transform.DOScale(escalaOriginal, duracaoAnimacao * 0.4f).SetEase(Ease.OutElastic));

        // Feedback visual adicional com cor, se tiver renderer
        if (temRenderer)
        {
            // Mudar para uma cor mais clara durante a animação
            Color corDestaque = new Color(
                Mathf.Clamp01(corOriginal.r + 0.2f),
                Mathf.Clamp01(corOriginal.g + 0.2f),
                Mathf.Clamp01(corOriginal.b + 0.2f),
                corOriginal.a
            );
            
            // Animar a cor junto com a escala
            sequencia.Join(rend.material.DOColor(corDestaque, duracaoAnimacao * 0.3f));
            sequencia.Join(rend.material.DOColor(corOriginal, duracaoAnimacao * 0.7f).SetDelay(duracaoAnimacao * 0.3f));
        }
    }

    /// <summary>
    /// Coroutine para recarregar o trampolim após o uso
    /// </summary>
    private IEnumerator RecarregarTrampolim()
    {
        // Desativar o trampolim
        trampolimAtivo = false;

        // Feedback visual de desativado
        if (temRenderer)
        {
            // Escurecer o trampolim quando desativado
            Color corDesativado = new Color(
                corOriginal.r * 0.6f,
                corOriginal.g * 0.6f,
                corOriginal.b * 0.6f,
                corOriginal.a
            );
            rend.material.DOColor(corDesativado, 0.3f);
        }

        // Esperar o tempo de recarga
        yield return new WaitForSeconds(tempoDeRecarga);

        // Reativar o trampolim
        trampolimAtivo = true;

        // Feedback visual de reativado
        if (temRenderer)
        {
            // Restaurar a cor original com uma animação
            rend.material.DOColor(corOriginal, 0.5f).SetEase(Ease.OutFlash, 2, 0);
        }
    }

    /// <summary>
    /// Método público para ativar o trampolim via script
    /// </summary>
    /// <param name="objetoAlvo">Objeto que receberá o efeito do trampolim</param>
    public void AtivarTrampolim(Transform objetoAlvo)
    {
        if (trampolimAtivo)
        {
            AplicarEfeitoTrampolim(objetoAlvo);
            
            // Gerenciar o uso do trampolim
            if (usoUnico)
            {
                trampolimAtivo = false;
            }
            else if (tempoRecarga)
            {
                StartCoroutine(RecarregarTrampolim());
            }
        }
    }

    /// <summary>
    /// Método público para resetar um trampolim de uso único
    /// </summary>
    public void ResetarTrampolim()
    {
        trampolimAtivo = true;

        // Feedback visual de reativado
        if (temRenderer)
        {
            rend.material.DOColor(corOriginal, 0.5f).SetEase(Ease.OutFlash, 2, 0);
        }
    }

    // Desenhar gizmos para visualizar a direção do impulso no editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Vector3 direcao = Quaternion.Euler(anguloDirecao, transform.eulerAngles.y, 0) * Vector3.up;
        Gizmos.DrawRay(transform.position, direcao * 2);
        
        // Desenhar uma esfera no topo da linha
        Gizmos.DrawWireSphere(transform.position + direcao * 2, 0.2f);
    }
}