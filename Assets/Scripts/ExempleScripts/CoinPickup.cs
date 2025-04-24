using UnityEngine;
using DG.Tweening; // DOTween namespace

[RequireComponent(typeof(Collider))]
public class CoinPickup : MonoBehaviour
{
    public ParticleSystem coletadoEfeito;
    public AudioClip somColeta;
    public float tempoDestruir = 1f;

    [Header("Magnetismo")]
    public float velocidadeMagnetismo = 5f;
    public float distanciaMinimaParaColetar = 0.5f;
    public Ease easeDoMagnetismo = Ease.OutQuad;

    private AudioSource audioSource;
    private bool coletado = false;

    public float velocidadeRotacao = 180f; // graus por segundo


    private void Start()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        GetComponent<Collider>().isTrigger = true;

        // Iniciar rotação contínua no eixo Y usando DOTween
        float tempoRotacao = 360f / velocidadeRotacao; // tempo para uma volta completa
        transform
            .DORotate(new Vector3(0, 360, 0), tempoRotacao, RotateMode.FastBeyond360)
            .SetEase(Ease.Linear)
            .SetLoops(-1, LoopType.Restart); // rotação infinita
    }

    private void OnTriggerEnter(Collider other)
    {
        if (coletado) return;

        if (other.CompareTag("Player"))
        {
            Coletar();
        }
        else if (other.CompareTag("MagnetTrigger"))
        {
            Transform jogador = other.transform;
            MoverAteJogadorDOTween(jogador);
        }
    }

    private void MoverAteJogadorDOTween(Transform jogador)
    {
        if (coletado) return;

        float distancia = Vector3.Distance(transform.position, jogador.position);
        float duracao = Mathf.Max(0.1f, distancia / velocidadeMagnetismo);

        transform
            .DOMove(jogador.position, duracao)
            .SetEase(easeDoMagnetismo)
            .OnUpdate(() =>
            {
                // Se o jogador se mover, atualize o destino (efeito de "grudar")
                if (jogador != null)
                {
                    transform.DOMove(jogador.position, 0.1f).SetEase(Ease.Linear);
                }
            })
            .OnComplete(() =>
            {
                if (!coletado)
                {
                    Coletar();
                }
            });
    }

    private void Coletar()
    {
        if (coletado) return;
        coletado = true;

        DOTween.Kill(transform); // cancela qualquer tween ativo

        if (coletadoEfeito != null)
            coletadoEfeito.Play();

        if (somColeta != null)
        {
            audioSource.clip = somColeta;
            audioSource.Play();
        }

        foreach (Transform child in transform)
            child.gameObject.SetActive(false);

        Destroy(gameObject, tempoDestruir);
    }
}
