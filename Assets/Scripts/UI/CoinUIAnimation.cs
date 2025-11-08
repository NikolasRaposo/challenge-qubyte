using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

namespace UI.HUD
{
    public class CoinUIAnimation : MonoBehaviour
    {
        [Header("Referências da UI")] 
        [SerializeField] private Image coinIcon;
        [SerializeField] private TMP_Text coinText;

        [Header("Idle Flutuação")] [SerializeField]
        private float iconFloatAmplitude = 10f;

        [SerializeField] private float iconFloatDuration = 1.5f;

        [SerializeField] private float textSmallAmplitude = 3f;
        [SerializeField] private float textLargeAmplitude = 6f;
        [SerializeField] private float textFloatSpeed = 2f;

        // Removemos cache de vértices originais para evitar travar o texto ao mudar conteúdo

        void Start()
        {
            var iconStartPos = coinIcon.rectTransform.anchoredPosition;
            coinIcon.rectTransform
                .DOAnchorPosY(iconStartPos.y + iconFloatAmplitude, iconFloatDuration)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);
        }

        void Update()
        {
            AnimateTextFloat();
        }

        // Não armazenamos vértices; usamos a malha atual após ForceMeshUpdate

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

                // Aplica deslocamento sobre a malha atual (reconstruída a cada frame por ForceMeshUpdate)
                verts[vertexIndex + 0] += offset;
                verts[vertexIndex + 1] += offset;
                verts[vertexIndex + 2] += offset;
                verts[vertexIndex + 3] += offset;
            }

            // Atualiza a malha
            for (int i = 0; i < textInfo.meshInfo.Length; i++)
            {
                textInfo.meshInfo[i].mesh.vertices = textInfo.meshInfo[i].vertices;
                coinText.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
            }
        }
    }
}
