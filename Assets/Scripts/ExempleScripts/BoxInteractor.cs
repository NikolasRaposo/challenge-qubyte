using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class BoxInteractor : MonoBehaviour
{
    [Header("🔧 Configurações Gerais")]
    public bool podeInteragirComPulo = true;
    public bool podeInteragirComAtaque = false;
    public bool apenasUmaVez = true;

    [Header("🧨 Ações da Caixa")]
    public bool quebraAoInteragir = true;
    public bool someAoInteragir = false;
    public bool reapareceDepois = false;
    public float tempoParaReaparecer = 3f;

    [Header("🎁 Soltar Item")]
    public bool soltaItem = false;
    public GameObject itemPrefab;
    public int quantidadeItens = 1;
    public Transform pontoSpawn;

    [Header("💥 Efeito de Explosão")]
    public bool explodeAoQuebrar = false;
    public float forcaExplosao = 300f;
    public GameObject prefabPedaços;

    [Header("🪂 Trampolim")]
    public bool funcionaComoTrampolim = false;
    public float forcaTrampolim = 10f;

    [Header("🎨 Feedback Visual")]
    public bool feedbackVisual = true;
    public float tremorIntensidade = 0.05f;
    public float tremorDuracao = 0.3f;

    private bool interagida = false;
    private Vector3 escalaOriginal;
    private Renderer rend;
    private Collider col;

    void Start()
    {
        escalaOriginal = transform.localScale;
        rend = GetComponent<Renderer>();
        col = GetComponent<Collider>();
    }

    public void Interagir(Transform interagidor = null)
    {
        if (apenasUmaVez && interagida) return;

        interagida = true;

        if (feedbackVisual)
            TremorVisual();

        if (soltaItem && itemPrefab != null)
            SoltarComEfeito();

        if (funcionaComoTrampolim && interagidor != null)
            AplicarTrampolim(interagidor);

        if (explodeAoQuebrar)
            Explodir();

        if (quebraAoInteragir)
            StartCoroutine(Quebrar());

        if (someAoInteragir && !quebraAoInteragir)
            StartCoroutine(Sumir());
    }

    void TremorVisual()
    {
        transform.DOShakePosition(tremorDuracao, tremorIntensidade);
        transform.DOShakeRotation(tremorDuracao, new Vector3(5f, 5f, 5f));
    }

    void SoltarComEfeito()
    {
        GameObject efeito = new GameObject("ItemEffect");
        efeito.transform.position = pontoSpawn ? pontoSpawn.position : transform.position + Vector3.up;
        ItemEffectController effect = efeito.AddComponent<ItemEffectController>();

        effect.itemPrefab = itemPrefab;
        effect.quantidade = quantidadeItens;
        effect.itensEspalhar = itemPrefab.CompareTag("Moeda"); // usar tag para detectar moedas
        effect.CriarItens();

        Destroy(efeito, 5f); // limpa depois
    }

    void AplicarTrampolim(Transform alvo)
    {
        if (alvo.TryGetComponent(out Rigidbody rb))
        {
            rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z); // zera y antes
            rb.AddForce(Vector3.up * forcaTrampolim, ForceMode.VelocityChange);
        }
    }

    void Explodir()
    {
        if (prefabPedaços != null)
        {
            GameObject fragmentos = Instantiate(prefabPedaços, transform.position, transform.rotation);
            foreach (var rb in fragmentos.GetComponentsInChildren<Rigidbody>())
            {
                rb.AddExplosionForce(forcaExplosao, transform.position, 2f);
            }
        }

        rend.enabled = false;
        col.enabled = false;

        if (reapareceDepois)
            Invoke(nameof(Reaparecer), tempoParaReaparecer);
    }

    IEnumerator Quebrar()
    {
        yield return new WaitForSeconds(0.2f);
        rend.enabled = false;
        col.enabled = false;

        if (reapareceDepois)
            Invoke(nameof(Reaparecer), tempoParaReaparecer);
    }

    IEnumerator Sumir()
    {
        yield return new WaitForSeconds(0.2f);
        rend.enabled = false;
        col.enabled = false;

        if (reapareceDepois)
            Invoke(nameof(Reaparecer), tempoParaReaparecer);
    }

    void Reaparecer()
    {
        rend.enabled = true;
        col.enabled = true;
        transform.localScale = Vector3.zero;

        Sequence reaparecer = DOTween.Sequence();
        reaparecer.Append(transform.DOScale(escalaOriginal, 0.5f).SetEase(Ease.OutBack));
        reaparecer.Join(transform.DOShakePosition(0.3f, 0.05f));
    }

    // Exemplo de trigger
    private void OnCollisionEnter(Collision collision)
    {
        if (!podeInteragirComPulo) return;

        foreach (ContactPoint contact in collision.contacts)
        {
            if (Vector3.Dot(contact.normal, Vector3.down) > 0.5f)
            {
                Interagir(collision.transform);
                break;
            }
        }
    }
}
