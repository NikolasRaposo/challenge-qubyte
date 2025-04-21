using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class ItemEffectController : MonoBehaviour
{
    [Header("Configurações dos Itens")]
    public GameObject itemPrefab;
    public int quantidade = 5;
    public float alturaSobe = 2f;
    public float tempoSobeDesce = 0.5f;
    public float raioEspalhamento = 2f;
    public float tempoAntesDeEspalhar = 0.4f;
    public float velocidadeRotacao = 360f;

    [Tooltip("Se verdadeiro, os itens não se espalham e permanecem centralizados.")]
    public bool itensEspalhar = true;

    private List<GameObject> itens = new List<GameObject>();

    public void CriarItens()
    {
        itens.Clear();

        for (int i = 0; i < quantidade; i++)
        {
            GameObject item = Instantiate(itemPrefab, transform.position, Quaternion.identity);
            itens.Add(item);
        }

        SubirEDescer();

        if (itensEspalhar)
            DOVirtual.DelayedCall(tempoAntesDeEspalhar, EspalharRadial);
    }

    private void SubirEDescer()
    {
        foreach (var item in itens)
        {
            if (item == null) continue;

            Vector3 posOriginal = item.transform.position;
            Vector3 posAlvo = posOriginal + Vector3.up * alturaSobe;

            item.transform.DOMoveY(posAlvo.y, tempoSobeDesce / 2f)
                .SetEase(Ease.OutSine)
                .OnComplete(() =>
                {
                    item.transform.DOMoveY(posOriginal.y, tempoSobeDesce / 2f).SetEase(Ease.InSine);
                });

            item.transform.DORotate(new Vector3(0, 360, 0), 1f, RotateMode.FastBeyond360)
                .SetEase(Ease.Linear)
                .SetLoops(-1, LoopType.Restart);
        }
    }

    private void EspalharRadial()
    {
        float anguloEntre = 360f / quantidade;

        for (int i = 0; i < itens.Count; i++)
        {
            if (itens[i] == null) continue;

            float angulo = i * anguloEntre * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Cos(angulo), 0, Mathf.Sin(angulo));
            Vector3 origem = itens[i].transform.position;
            Vector3 destino = origem + dir * raioEspalhamento;

            if (Physics.Raycast(origem, dir, out RaycastHit hit, raioEspalhamento))
                destino = hit.point + hit.normal * 0.3f;

            itens[i].transform.DOMove(destino, 0.4f).SetEase(Ease.OutQuad);
        }
    }
}
