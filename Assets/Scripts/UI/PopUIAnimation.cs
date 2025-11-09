using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;
using Managers;

public class PopUIAnimation : MonoBehaviour
{
    [Header("Referências da UI")] 
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text text;
    
    [Header("Punch Settings")]
    [SerializeField] private float punchStrength = 0.2f;
    [SerializeField] private float punchDuration = 0.3f;
    [SerializeField] private int vibrato = 5;
    [SerializeField] private float elasticity = 0.5f;
    
    private Vector3 _originalScale;
    private Vector3 _originalIconScale;
    private Vector3 _originalTextScale;

    private void Awake()
    {
        _originalScale = transform.localScale;
        _originalIconScale = icon.rectTransform.localScale;
        _originalTextScale = text.rectTransform.localScale;
    }

    /// <summary>
    /// Executa a animação de pop.
    /// </summary>
    public void PlayPop()
    {
        // Só permite animação quando HUD foi explicitamente ativada
        if (UIManager.Instance != null && !UIManager.Instance.HudAnimationsEnabled)
            return;

        // Cancela qualquer animação atual
        icon.rectTransform.DOKill();
        text.rectTransform.DOKill();

        // Reseta escala antes do punch
        icon.rectTransform.localScale = _originalIconScale;
        text.rectTransform.localScale = _originalTextScale;
        
        // Pop no ícone
        icon.rectTransform.DOPunchScale(Vector3.one * punchStrength, punchDuration, vibrato, elasticity);
        
        // Pop no texto
        text.rectTransform.DOPunchScale(Vector3.one * punchStrength, punchDuration, vibrato, elasticity);

        
    }
}
