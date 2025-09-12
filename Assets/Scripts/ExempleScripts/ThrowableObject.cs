using UnityEngine;
using DG.Tweening;

public class ThrowableObject : MonoBehaviour
{
    public enum Alvo { Jogador, PontoFixo, Direcao }

        public interface ITriggeravel
    {
        void Ativar();
    }

    [Header("Destino")]
    public Alvo tipoDeAlvo = Alvo.PontoFixo;
    public Transform alvoTransform;
    public Vector3 pontoFixo;
    public Vector3 direcao = Vector3.forward;

    [Header("Arremesso")]
    public float alturaDoArco = 3f;
    public float tempoBaseNoAr = 1f;
    public bool tempoDinamico = true;
    public float tempoPorUnidade = 0.15f;

    [Header("Curva do Arco")]
    public bool usarCurvaCustomizada = false;
    public AnimationCurve curvaDeAltura;

    [Header("Rotação")]
    public bool girar = true;
    public Vector3 eixosDeRotacao = new Vector3(0, 360, 0);

    [Header("Bumerangue")]
    public bool retornarAposImpacto = false;
    public Transform pontoRetorno;

    [Header("Teleguiado")]
    public bool teleguiado = false;
    public float anguloMaximoPorSegundo = 60f; // limite de curva por segundo

    [Header("Impacto")]
    public bool destruirAoImpactar = true;
    public GameObject efeitoImpacto;
    public AudioClip somImpacto;

    [Header("Interações")]
    public bool causarDano = false;
    public int dano = 1;
    public bool ativarTrigger = false;

    [Header("Ricochete")]
    public bool podeRicochetear = false;
    public int ricochetesMaximos = 2;

    [Header("Extras")]
    public bool mostrarSombra = true;
    public GameObject sombraPrefab;

    private int ricochetes = 0;
    private GameObject sombraInstanciada;
    private Vector3 destinoAtual;
    private Tween vooTween;
    private bool emVoo = false;

    public void Arremessar()
    {
        destinoAtual = CalcularDestino();
        float distancia = Vector3.Distance(transform.position, destinoAtual);
        float tempo = tempoDinamico ? distancia * tempoPorUnidade : tempoBaseNoAr;

        if (mostrarSombra && sombraPrefab)
            sombraInstanciada = Instantiate(sombraPrefab, transform.position, Quaternion.identity);

        emVoo = true;

        vooTween = DOTween.To(() => 0f, t =>
        {
            // Se teleguiado, atualiza o destino
            if (teleguiado)
            {
                Vector3 novoDestino = CalcularDestino();
                Vector3 dirAtual = (destinoAtual - transform.position).normalized;
                Vector3 dirNovo = (novoDestino - transform.position).normalized;

                float maxDelta = anguloMaximoPorSegundo * Time.deltaTime;
                destinoAtual = transform.position + Vector3.RotateTowards(dirAtual, dirNovo, Mathf.Deg2Rad * maxDelta, 0f) * distancia;
            }

            Vector3 pos = Vector3.Lerp(transform.position, destinoAtual, t);
            float altura = usarCurvaCustomizada ? curvaDeAltura.Evaluate(t) * alturaDoArco : Mathf.Sin(t * Mathf.PI) * alturaDoArco;
            pos.y += altura;

            transform.position = pos;

            if (sombraInstanciada)
            {
                if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 100f))
                    sombraInstanciada.transform.position = hit.point + Vector3.up * 0.1f;
            }

        }, 1f, tempo).SetEase(Ease.Linear).OnComplete(() =>
        {
            emVoo = false;

            if (efeitoImpacto)
                Instantiate(efeitoImpacto, transform.position, Quaternion.identity);

            if (somImpacto)
                AudioSource.PlayClipAtPoint(somImpacto, transform.position);

            if (retornarAposImpacto && pontoRetorno != null)
            {
                tipoDeAlvo = Alvo.PontoFixo;
                pontoFixo = pontoRetorno.position;
                Arremessar();
                return;
            }

            if (destruirAoImpactar)
                Destroy(gameObject);

            if (sombraInstanciada)
                Destroy(sombraInstanciada);
        });

        if (girar)
        {
            transform.DORotate(eixosDeRotacao, tempo, RotateMode.FastBeyond360)
                .SetEase(Ease.Linear)
                .SetLoops(-1, LoopType.Restart);
        }
    }

    private Vector3 CalcularDestino()
    {
        switch (tipoDeAlvo)
        {
            case Alvo.Jogador:
                GameObject jogador = GameObject.FindWithTag("Player");
                if (jogador != null)
                    return jogador.transform.position;
                break;

            case Alvo.PontoFixo:
                return pontoFixo;

            case Alvo.Direcao:
                return transform.position + direcao.normalized * 5f;
        }
        return transform.position;
    }

    private void OnCollisionEnter(Collision col)
    {
        if (!emVoo) return;

        if (podeRicochetear && ricochetes < ricochetesMaximos)
        {
            ricochetes++;
            Vector3 refletido = Vector3.Reflect(direcao, col.contacts[0].normal);
            direcao = refletido;
            Arremessar();
        }
        else
        {
            vooTween?.Kill();

            if (causarDano && col.gameObject.CompareTag("Player"))
            {
                // col.gameObject.GetComponent<Vida>().ReceberDano(dano);
            }

            if (ativarTrigger && col.gameObject.TryGetComponent(out ITriggeravel trigger))
            {
                trigger.Ativar();
            }

            if (efeitoImpacto)
                Instantiate(efeitoImpacto, transform.position, Quaternion.identity);

            if (somImpacto)
                AudioSource.PlayClipAtPoint(somImpacto, transform.position);

            if (retornarAposImpacto && pontoRetorno != null)
            {
                tipoDeAlvo = Alvo.PontoFixo;
                pontoFixo = pontoRetorno.position;
                Arremessar();
            }
            else if (destruirAoImpactar)
            {
                Destroy(gameObject);
            }

            if (sombraInstanciada)
                Destroy(sombraInstanciada);

            emVoo = false;
        }
    }
}
