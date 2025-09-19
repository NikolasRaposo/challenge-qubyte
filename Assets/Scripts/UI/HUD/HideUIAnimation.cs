using System;
using UnityEngine;
using DG.Tweening;

namespace UI.HUD
{
    public class HideUIAnimation : MonoBehaviour
    {
        [Header("HUD Settings")] 
        [SerializeField] private float moveDistance = 200f;
        [SerializeField] private float duration = 0.5f;
        [SerializeField] private Ease ease = Ease.InOutCubic;

        private RectTransform _rectTransform;
        private Vector2 startPos;

        void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            startPos = _rectTransform.anchoredPosition;
        }
        
        /// <summary>
        /// Anima a HUD aparecendo (descendo).
        /// </summary>
        public void PlayShow()
        {
            gameObject.SetActive(true);
            
            // começa acima da posição original
            _rectTransform.anchoredPosition = startPos + new Vector2(0, moveDistance);

            _rectTransform
                .DOAnchorPos(startPos, duration)
                .SetEase(ease);
        }
        
        /// <summary>
        /// Anima a HUD sumindo (subindo).
        /// </summary>
        public void PlayHide(Action onComplete = null)
        {
            _rectTransform
                .DOAnchorPosY(_rectTransform.anchoredPosition.y + moveDistance, duration)
                .SetEase(ease)
                .OnComplete(() =>
                {
                    gameObject.SetActive(false);
                    // Reseta posição para próxima vez que aparecer
                    _rectTransform.anchoredPosition -= new Vector2(0, moveDistance);
                    onComplete?.Invoke();
                });
        }
    }
}