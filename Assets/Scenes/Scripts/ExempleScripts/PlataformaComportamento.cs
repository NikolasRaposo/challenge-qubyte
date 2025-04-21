using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(Collider))]
public class PlataformaComportamento : MonoBehaviour
{
    [Header("🔧 Configurações Gerais")]
    public bool plataformaMovel;
    public bool afundaComPeso;
    public bool desapareceAposContato;
    public bool caiAposContato;
    public bool reapareceDepois;
    public bool gira;
    public bool seguePontos;
    public bool trampolim;

    [Header("🎯 Plataforma Móvel")]
    public Vector3 direcaoMovimento = Vector3.right;
    public float distanciaMovimento = 2f;
    public float duracaoMovimento = 2f;
    public bool movimentoPingPong = true;

    [Header("📦 Plataforma que Afunda")]
    public float profundidadeAfundar = 0.5f;
    public float velocidadeAfundar = 0.3f;

    [Header("👻 Desaparecer / Cair")]
    public float delayDesaparecer = 1f;
    public float delayCair = 1f;

    [Header("♻️ Reaparecer")]
    public float tempoParaReaparecer = 3f;

    [Header("🔁 Rotação")]
    public Vector3 eixoRotacao = Vector3.up;
    public float velocidadeRotacao = 45f;

    [Header("🧭 Pontos de Caminho")]
    public Transform[] pontos;
    public float tempoPorPonto = 1.5f;
    public bool loopPontos = true;

    [Header("🦘 Trampolim")]
    public float forcaRebote = 10f;

    private Vector3 posicaoInicial;
    private Sequence caminhoSequencia;
    private bool estaComJogador = false;
    private Renderer rend;
    private Collider col;

    [Header("✨ Feedback Visual")]
    public bool feedbackTremorAntesDeCair = true;
    public float duracaoTremor = 0.5f;
    public float intensidadeTremor = 0.05f;
    public float intensidadeRotacaoTremor = 5f;

    public bool animacaoReaparecer = true;
    public float duracaoReaparecer = 0.6f;

    private void Awake()
    {
        rend = GetComponent<Renderer>();
        col = GetComponent<Collider>();
        posicaoInicial = transform.position;
        ValidarConflitos();
    }

    private void Start()
    {
        if (plataformaMovel)
        {
            Vector3 destino = transform.position + direcaoMovimento.normalized * distanciaMovimento;
            if (movimentoPingPong)
            {
                transform.DOMove(destino, duracaoMovimento)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetEase(Ease.InOutSine);
            }
            else
            {
                transform.DOMove(destino, duracaoMovimento).SetLoops(-1).SetEase(Ease.Linear);
            }
        }

        if (gira)
        {
            transform.DORotate(eixoRotacao * 360, 10f, RotateMode.FastBeyond360)
                .SetLoops(-1)
                .SetEase(Ease.Linear);
        }

        if (seguePontos && pontos.Length > 1)
        {
            caminhoSequencia = DOTween.Sequence();
            foreach (Transform ponto in pontos)
            {
                caminhoSequencia.Append(transform.DOMove(ponto.position, tempoPorPonto).SetEase(Ease.InOutSine));
            }

            if (loopPontos) caminhoSequencia.SetLoops(-1);
            caminhoSequencia.Play();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        estaComJogador = true;

        if (afundaComPeso)
        {
            transform.DOMoveY(posicaoInicial.y - profundidadeAfundar, velocidadeAfundar);
        }

        if (desapareceAposContato)
        {
            Invoke(nameof(Desaparecer), delayDesaparecer);
        }

        if (caiAposContato)
        {
            Invoke(nameof(Cair), delayCair);
        }

        if (trampolim)
        {
            Rigidbody rb = other.attachedRigidbody;
            if (rb != null)
            {
                rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z); // zera y pra não somar
                rb.AddForce(Vector3.up * forcaRebote, ForceMode.VelocityChange);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        estaComJogador = false;

        if (afundaComPeso)
        {
            transform.DOMoveY(posicaoInicial.y, velocidadeAfundar);
        }
    }

    private void Desaparecer()
    {
        if (feedbackTremorAntesDeCair)
        {
            FazerTremor(() =>
            {
                rend.enabled = false;
                col.enabled = false;
                if (reapareceDepois)
                    Invoke(nameof(Reaparecer), tempoParaReaparecer);
            });
        }
        else
        {
            rend.enabled = false;
            col.enabled = false;
            if (reapareceDepois)
                Invoke(nameof(Reaparecer), tempoParaReaparecer);
        }
    }

    private void Cair()
    {
        if (feedbackTremorAntesDeCair)
        {
            FazerTremor(() =>
            {
                Rigidbody rb = gameObject.AddComponent<Rigidbody>();
                col.enabled = false;
                if (reapareceDepois)
                    Invoke(nameof(ReaparecerCaindo), tempoParaReaparecer);
            });
        }
        else
        {
            Rigidbody rb = gameObject.AddComponent<Rigidbody>();
            col.enabled = false;
            if (reapareceDepois)
                Invoke(nameof(ReaparecerCaindo), tempoParaReaparecer);
        }
    }

    private void Reaparecer()
    {
        transform.position = posicaoInicial;
        col.enabled = true;

        if (animacaoReaparecer)
        {
            transform.localScale = Vector3.zero;
            rend.enabled = true;

            Sequence reaparecer = DOTween.Sequence();
            reaparecer.Append(transform.DOScale(Vector3.one, duracaoReaparecer).SetEase(Ease.OutBack));
            reaparecer.Join(transform.DOShakePosition(0.3f, 0.05f));
            reaparecer.Play();
        }
        else
        {
            rend.enabled = true;
        }
    }

    private void ReaparecerCaindo()
    {
        Destroy(GetComponent<Rigidbody>());
        transform.position = posicaoInicial;
        col.enabled = true;

        if (animacaoReaparecer)
        {
            transform.localScale = Vector3.zero;
            rend.enabled = true;

            Sequence reaparecer = DOTween.Sequence();
            reaparecer.Append(transform.DOScale(Vector3.one, duracaoReaparecer).SetEase(Ease.OutBack));
            reaparecer.Join(transform.DOShakePosition(0.3f, 0.05f));
            reaparecer.Play();
        }
        else
        {
            rend.enabled = true;
        }
    }

    private void ValidarConflitos()
    {
        if (desapareceAposContato && caiAposContato)
        {
            Debug.LogWarning($"[PlataformaComportamento] Conflito: 'desapareceAposContato' e 'caiAposContato' estão ativos. Apenas 'desaparecer' será usado.");
            caiAposContato = false;
        }

        if (plataformaMovel && seguePontos)
        {
            Debug.LogWarning($"[PlataformaComportamento] Conflito: 'plataformaMovel' e 'seguePontos' estão ativos. Apenas 'seguePontos' será usado.");
            plataformaMovel = false;
        }
    }

    private void FazerTremor(System.Action aoFinalizar)
    {
        Vector3 originalPos = transform.position;
        Vector3 originalRot = transform.eulerAngles;

        Sequence tremor = DOTween.Sequence();
        for (int i = 0; i < 5; i++)
        {
            tremor.Append(transform.DOShakePosition(0.05f, intensidadeTremor, 10, 90, false, true));
            tremor.Join(transform.DOShakeRotation(0.05f, new Vector3(0, 0, intensidadeRotacaoTremor)));
        }
        tremor.AppendCallback(() =>
        {
            transform.position = originalPos;
            transform.eulerAngles = originalRot;
            aoFinalizar?.Invoke();
        });
    }
}
