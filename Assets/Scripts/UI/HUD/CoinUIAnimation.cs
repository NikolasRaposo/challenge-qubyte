using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

public class CoinUIAnimation : MonoBehaviour
{
    [Header("Referências da UI")]
    [SerializeField] private Image coinIcon;
    [SerializeField] private TMP_Text coinText;

    [Header("Idle Flutuação")]
    [SerializeField] private float iconFloatAmplitude = 10f;
    [SerializeField] private float iconFloatDuration = 1.5f;

    [SerializeField] private float textSmallAmplitude = 3f;
    [SerializeField] private float textLargeAmplitude = 6f;
    [SerializeField] private float textFloatSpeed = 2f;
    
    private Vector3[][] originalVertices;
    
    void Start()
    {
        var iconStartPos = coinIcon.rectTransform.anchoredPosition;
        coinIcon.rectTransform
            .DOAnchorPosY(iconStartPos.y + iconFloatAmplitude, iconFloatDuration)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine);
        
        coinText.ForceMeshUpdate();
        StoreOriginalVertices();
    }
    
    void Update()
    {
        AnimateTextFloat();
    }
    
    private void StoreOriginalVertices()
    {
        var textInfo = coinText.textInfo;
        originalVertices = new Vector3[textInfo.meshInfo.Length][];
        for (int i = 0; i < textInfo.meshInfo.Length; i++)
        {
            originalVertices[i] = textInfo.meshInfo[i].vertices.Clone() as Vector3[];
        }
    }

    private void AnimateTextFloat()
    {
        coinText.ForceMeshUpdate();
        var textInfo = coinText.textInfo;

        for (int i = 0; i < textInfo.characterCount; i++)
        {
            if (!textInfo.characterInfo[i].isVisible) continue;

            var charInfo = textInfo.characterInfo[i];
            int meshIndex = charInfo.materialReferenceIndex;
            int vertexIndex = charInfo.vertexIndex;

            Vector3[] verts = textInfo.meshInfo[meshIndex].vertices;

            // Alterna amplitude entre caracteres pares e ímpares
            float amplitude = (i % 2 == 0) ? textSmallAmplitude : textLargeAmplitude;
            float offsetY = Mathf.Sin(Time.time * textFloatSpeed + i * 0.5f) * amplitude;

            // Pega posição base do caractere
            Vector3 offset = new Vector3(0, offsetY, 0);

            verts[vertexIndex + 0] = originalVertices[meshIndex][vertexIndex + 0] + offset;
            verts[vertexIndex + 1] = originalVertices[meshIndex][vertexIndex + 1] + offset;
            verts[vertexIndex + 2] = originalVertices[meshIndex][vertexIndex + 2] + offset;
            verts[vertexIndex + 3] = originalVertices[meshIndex][vertexIndex + 3] + offset;
        }

        // Atualiza a malha
        for (int i = 0; i < textInfo.meshInfo.Length; i++)
        {
            textInfo.meshInfo[i].mesh.vertices = textInfo.meshInfo[i].vertices;
            coinText.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
        }
    }

    /// <summary>
    /// Chamar esse método quando pegar moeda
    /// </summary>
    public void OnCollect(int newAmount)
    {
        // Atualiza o texto
        coinText.text = newAmount.ToString();
        coinText.ForceMeshUpdate();

        // Pop no texto
        coinText.rectTransform.DOPunchScale(Vector3.one * 0.25f, 0.3f, 5, 0.5f);

        // Pop no ícone
        coinIcon.rectTransform.DOPunchScale(Vector3.one * 0.25f, 0.3f, 5, 0.5f);
    }
}
