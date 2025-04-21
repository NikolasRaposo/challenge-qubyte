using UnityEngine;
using DG.Tweening;
using System;
using System.Collections;

public class EmotionStateSystem : MonoBehaviour
{
    public enum EmotionState { Neutro, Feliz, Raiva, Triste, Super }

    [Header("Configurações Gerais")]
    public EmotionState estadoAtual = EmotionState.Neutro;
    public float tempoParaResetar = 5f;

    [Header("Referências de Sistema")]
    public Animator animator;
    public AudioSource audioSource;
    public Transform headBone;
    public Transform cameraTransform;

    [Header("Áudios por Emoção")]
    public AudioClip felizClip;
    public AudioClip raivaClip;
    public AudioClip tristeClip;
    public AudioClip superClip;

    [Header("Partículas por Emoção")]
    public ParticleSystem felizParticles;
    public ParticleSystem raivaParticles;
    public ParticleSystem tristeParticles;
    public ParticleSystem superParticles;

    [Header("Olhar Direcionado (quando aplicável)")]
    public bool usarOlharCurioso = true;
    public Transform objetoImportante;
    public float duracaoOlhar = 2f;

    private Coroutine resetCoroutine;
    private Coroutine olharCoroutine;
    private Action<EmotionState> onEmotionChange;

    private void Start()
    {
        AplicarFeedbacks();
    }

    public void RegistrarCallback(Action<EmotionState> callback)
    {
        onEmotionChange += callback;
    }

    public void SetarEstado(EmotionState novoEstado)
    {
        if (estadoAtual == novoEstado) return;

        estadoAtual = novoEstado;
        AplicarFeedbacks();
        onEmotionChange?.Invoke(novoEstado);

        if (resetCoroutine != null) StopCoroutine(resetCoroutine);
        resetCoroutine = StartCoroutine(ResetarEstadoAposTempo());

        if (novoEstado == EmotionState.Super)
        {
            // Poderia haver um boost de velocidade ou algo especial aqui
        }
    }

    private void AplicarFeedbacks()
    {
        TocarAudio();
        AtivarParticulas();
        AtivarAnimacaoCamada();
        DirecionarOlhar();
        ExpressarFisicamente(); // NOVO
    }

    private void TocarAudio()
    {
        if (!audioSource) return;

        AudioClip clip = null;
        switch (estadoAtual)
        {
            case EmotionState.Feliz: clip = felizClip; break;
            case EmotionState.Raiva: clip = raivaClip; break;
            case EmotionState.Triste: clip = tristeClip; break;
            case EmotionState.Super: clip = superClip; break;
        }

        if (clip != null) audioSource.PlayOneShot(clip);
    }

    private void AtivarParticulas()
    {
        felizParticles?.Stop();
        raivaParticles?.Stop();
        tristeParticles?.Stop();
        superParticles?.Stop();

        switch (estadoAtual)
        {
            case EmotionState.Feliz: felizParticles?.Play(); break;
            case EmotionState.Raiva: raivaParticles?.Play(); break;
            case EmotionState.Triste: tristeParticles?.Play(); break;
            case EmotionState.Super: superParticles?.Play(); break;
        }
    }

    private void AtivarAnimacaoCamada()
    {
        if (!animator) return;

        animator.SetTrigger("ResetEmocao");

        switch (estadoAtual)
        {
            case EmotionState.Feliz: animator.SetTrigger("Feliz"); break;
            case EmotionState.Raiva: animator.SetTrigger("Raiva"); break;
            case EmotionState.Triste: animator.SetTrigger("Triste"); break;
            case EmotionState.Super: animator.SetTrigger("Super"); break;
        }
    }

    private void DirecionarOlhar()
    {
        if (!usarOlharCurioso || objetoImportante == null || headBone == null) return;

        if (olharCoroutine != null) StopCoroutine(olharCoroutine);
        olharCoroutine = StartCoroutine(OlharParaObjeto());
    }

    private IEnumerator OlharParaObjeto()
    {
        Quaternion rotAlvo = Quaternion.LookRotation(objetoImportante.position - headBone.position);
        headBone.DORotateQuaternion(rotAlvo, duracaoOlhar).SetEase(Ease.InOutSine);

        yield return new WaitForSeconds(duracaoOlhar + 1f);

        // Volta a olhar para frente ou para a direção da câmera
        Quaternion rotVoltar = Quaternion.LookRotation(cameraTransform.forward);
        headBone.DORotateQuaternion(rotVoltar, duracaoOlhar).SetEase(Ease.InOutSine);
    }

    private void ExpressarFisicamente()
    {
        if (headBone == null) return;

        switch (estadoAtual)
        {
            case EmotionState.Feliz:
                headBone.DOLocalRotate(new Vector3(0, 0, 10), 0.1f)
                    .SetLoops(4, LoopType.Yoyo)
                    .SetEase(Ease.InOutSine);
                break;

            case EmotionState.Raiva:
                headBone.DOShakeRotation(0.4f, new Vector3(0, 20, 0), 10, 90)
                    .SetEase(Ease.OutBounce);
                break;

            case EmotionState.Triste:
                headBone.DOLocalRotate(new Vector3(15, 0, 0), 0.2f)
                    .SetEase(Ease.OutCubic);
                break;

            case EmotionState.Super:
                headBone.DOShakeRotation(0.6f, new Vector3(15, 30, 15), 20, 90)
                    .SetEase(Ease.InOutElastic);
                break;
        }
    }

    private IEnumerator ResetarEstadoAposTempo()
    {
        yield return new WaitForSeconds(tempoParaResetar);
        SetarEstado(EmotionState.Neutro);
    }

    // Eventos externos
    public void JogadaCerta() => SetarEstado(EmotionState.Feliz);
    public void TomouDano() => SetarEstado(EmotionState.Raiva);
    public void CaiuNaLava() => SetarEstado(EmotionState.Triste);
    public void AtivouCombo() => SetarEstado(EmotionState.Super);
}
