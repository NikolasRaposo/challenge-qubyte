using System;
using UnityEngine;
using UnityEngine.Events;
using Managers;

namespace UI
{
    /// <summary>
    /// Controla o avanço provisório do MainMenu via confirmação (Submit),
    /// mas SOMENTE quando explicitamente habilitado (ex.: por AnimationEnd).
    /// 
    /// Use os métodos públicos EnableProvisionalAdvance e DisableProvisionalAdvance
    /// em eventos da sua animação para abrir/fechar a janela de avanço.
    /// Configure a ação de avanço no UnityEvent 'OnAdvance'.
    /// </summary>
    [System.Obsolete("StartMenuProvisionalAdvance foi descontinuado. Remova do GameObject e use StartMenuControlAnim + InputContextCoordinator.", false)]
    public class StartMenuProvisionalAdvance : MonoBehaviour
    {
        [Tooltip("Se verdadeiro, habilita automaticamente no Start. Recomenda-se manter FALSO e habilitar via AnimationEnd.")]
        [SerializeField] private bool autoEnableOnStart = false;

        [Tooltip("Ação executada ao receber Submit enquanto habilitado.")]
        public UnityEvent OnAdvance;

        [Header("Roteamento consolidado")]
        [Tooltip("Se verdadeiro, ao receber Submit chama diretamente OnStartButtonClicked() do StartMenuControlAnim.")]
        [SerializeField] private bool routeToOnStartButtonClicked = true;

        [Tooltip("Referência ao StartMenuControlAnim para roteamento direto do Submit.")]
        [SerializeField] private StartMenuControlAnim startMenuControlAnim;

        private bool _enabled;

        private void Start()
        {
            Debug.LogWarning("[StartMenuProvisionalAdvance] ESTE COMPONENTE ESTÁ OBSOLETO. Remova-o do GameObject e use StartMenuControlAnim.OnStartButtonClicked e InputContextCoordinator.");
            if (autoEnableOnStart)
                EnableProvisionalAdvance();
        }

        private void OnDisable()
        {
            DisableProvisionalAdvance();
        }

        public void EnableProvisionalAdvance()
        {
            if (_enabled) return;
            var im = InputManager.Instance;
            if (im == null)
            {
                Debug.LogWarning("[StartMenuProvisionalAdvance] InputManager.Instance indisponível. Não foi possível habilitar.");
                return;
            }
            im.OnUiSubmit += HandleUiSubmit;
            _enabled = true;
            // Mantém centralização: não troca o contexto aqui. O coordenador controla UI interactions.
        }

        public void DisableProvisionalAdvance()
        {
            if (!_enabled) return;
            var im = InputManager.Instance;
            if (im != null)
                im.OnUiSubmit -= HandleUiSubmit;
            _enabled = false;
        }

        private void HandleUiSubmit()
        {
            try
            {
                if (routeToOnStartButtonClicked && startMenuControlAnim != null)
                {
                    Debug.Log("[StartMenuProvisionalAdvance] Submit recebido -> roteando para StartMenuControlAnim.OnStartButtonClicked()");
                    startMenuControlAnim.OnStartButtonClicked();
                }
                else
                {
                    Debug.Log("[StartMenuProvisionalAdvance] Submit recebido -> invocando UnityEvent OnAdvance");
                    OnAdvance?.Invoke();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[StartMenuProvisionalAdvance] Falha ao avançar: {e}");
            }
        }
    }
}