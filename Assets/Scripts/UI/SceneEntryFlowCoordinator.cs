using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.Playables;

// Coordenador genérico e reutilizável para o fluxo de entrada da cena
// Fases: UI inicial -> Loading -> Cinemática -> Handoff para Gameplay
// Integra com Managers.InputContextCoordinator para trocar contextos de input.
// Todas as ações potencialmente acopladas (travar física, desabilitar controlador, triggers de animador) são opcionais e parametrizadas.
public class SceneEntryFlowCoordinator : MonoBehaviour
{
    public enum StartInputContext { None, Gameplay, UI }

    public enum Phase { StartingGame, EntryUI, Loading, Cinematic, Gameplay }

    [Header("Estado (Runtime)")]
    [SerializeField] private Phase currentPhase = Phase.EntryUI;
    [SerializeField] private bool isLoading;
    [SerializeField] private bool isCinematicPlaying;
    [SerializeField] private bool hasHandoffHappened;
    [Tooltip("Cooldown interno para evitar toque duplicado da cinemática em milissegundos.")]
    [SerializeField] private float cinematicPlayCooldownMs = 200f;
    private float _lastCinematicPlayTimeUnscaled;

    [Header("Configuração de Fases")]
    [Tooltip("Alterar o contexto de input ao iniciar.")]
    [SerializeField] private bool changeInputContextOnStart = true;

    [Tooltip("Contexto de input inicial quando a troca está habilitada.")]
    [SerializeField] private StartInputContext startInputContext = StartInputContext.UI;

    [Tooltip("Permite interações de UI (InputSystemUIInputModule) ao iniciar (apenas quando o contexto inicial for UI).")]
    [SerializeField] private bool enableUiInteractionsOnStart = true;

    [Tooltip("Bloqueia o input de gameplay ao iniciar.")]
    [SerializeField] private bool blockGameplayInputOnStart = true;

    [Tooltip("Se verdadeiro, após o Loading terminar, toca a Timeline automaticamente.")]
    [SerializeField] private bool autoPlayCinematicAfterLoading = true;

    [Header("Cinemática / Timeline")]
    [SerializeField] private GameObject cinematicRoot;
    [SerializeField] private PlayableDirector cinematicDirector;

    [Header("Loading")]
    [SerializeField] private Animator loadingAnimator;
    [SerializeField] private string loadingStartTrigger = "StartLoading";
    [SerializeField] private string loadingStopTrigger = "StopLoading";

    [Header("UI / Focus")]
    [SerializeField] private GameObject canvasUI;
    [SerializeField] private GameObject canvasHUD;
    [SerializeField] private GameObject defaultUiButton;
    [Tooltip("Define se a HUD inicia visível ou oculta.")]
    [SerializeField] private bool hudEnabledOnStart = false;
    
    [Header("Eventos")]
    [Tooltip("Invocado ao iniciar (Start). Use para acionar UI/HUD no início.")]
    [SerializeField] private UnityEvent onStart;

    [Header("Player / Física / Controlador")]
    [Tooltip("Trava a física do jogador (Rigidbody torna-se kinematic) ao iniciar.")]
    [SerializeField] private bool freezePlayerPhysicsOnStart = false;

    [Tooltip("Desabilita controlador específico do jogador ao iniciar (ex.: ECMSaciController).")]
    [SerializeField] private bool disablePlayerControllerOnStart = false;

    [Tooltip("Reativa física/controlador automaticamente ao fim da cinematica.")]
    [SerializeField] private bool restorePlayerControlOnCinematicEnd = true;

    [Header("Marcações / Estado de Jogo")]
    [Tooltip("Se verdadeiro, marca GameManager como estando em Start Menu durante o fluxo inicial.")]
    [SerializeField] private bool markGameAsInStartMenu = false;

    [Header("Controladores Opcionais")]
    [SerializeField] private UiPhaseController uiController;
    [SerializeField] private LoadingPhaseController loadingController;
    [SerializeField] private CinematicPhaseController cinematicController;
    [SerializeField] private PlayerControlGate playerGate;

    private Rigidbody playerRb;
    private Behaviour playerControllerBehaviour; // Ex.: ECMSaciController

    private void Awake()
    {
        // Captura referências opcionais do PlayableDirector.
        if (cinematicRoot != null && cinematicDirector == null)
        {
            cinematicDirector = cinematicRoot.GetComponent<PlayableDirector>();
        }

        // Evita tocar automaticamente ao ativar o objeto da cinematica
        if (cinematicDirector != null)
        {
            cinematicDirector.playOnAwake = false;
        }

        // Busca player via GameManager se disponível.
        var gm = Managers.GameManager.Instance;
        if (gm != null && gm.player != null)
        {
            playerRb = gm.player.GetComponent<Rigidbody>();
            // Tenta localizar um controlador comum (ex.: ECMSaciController)
            playerControllerBehaviour = gm.player.GetComponent<Behaviour>();
        }
    }

    private void Start()
    {
        if (markGameAsInStartMenu)
        {
            Managers.GameManager.Instance?.SetStartMenuActive(true);
        }

        if (changeInputContextOnStart)
        {
            switch (startInputContext)
            {
                case StartInputContext.None:
                    // Não altera o contexto de input
                    break;
                case StartInputContext.Gameplay:
                    Managers.InputContextCoordinator.Instance?.SetPlayerContext();
                    Managers.InputContextCoordinator.Instance?.DisableUiInteractions();
                    break;
                case StartInputContext.UI:
                    // Troca para contexto de UI sem habilitar interações imediatamente.
                    Managers.InputContextCoordinator.Instance?.SetUiContext(enableUiInteractions: false);
                    if (enableUiInteractionsOnStart)
                    {
                        Managers.InputContextCoordinator.Instance?.EnableUiInteractions();
                        FocusDefaultButtonNextFrame();
                    }
                    break;
            }
        }

        if (blockGameplayInputOnStart)
        {
            Managers.InputContextCoordinator.Instance?.SetBlockInputContext();
        }

        if (freezePlayerPhysicsOnStart)
        {
            if (playerGate != null)
            {
                playerGate.FreezePhysics();
            }
            else
            {
                FreezePlayerPhysics();
            }
        }

        if (disablePlayerControllerOnStart)
        {
            if (playerGate != null)
            {
                playerGate.DisableController();
            }
            else
            {
                DisablePlayerController();
            }
        }

        // Não ativar/forçar pais da cinematica no Start para evitar início precoce
        EnsureParentsActive(canvasUI);
        EnsureParentsActive(canvasHUD);

        // Estado inicial da HUD
        if (canvasHUD != null)
        {
            canvasHUD.SetActive(hudEnabledOnStart);
        }

        // Dispara eventos de início
        onStart?.Invoke();

        // Fase inicial: quando não for gameplay direto, iniciamos em StartingGame
        switch (startInputContext)
        {
            case StartInputContext.Gameplay:
                currentPhase = Phase.Gameplay;
                break;
            case StartInputContext.UI:
            case StartInputContext.None:
            default:
                currentPhase = Phase.StartingGame;
                break;
        }
    }

    // Chamado pelo sistema de loading quando terminar
    public void OnLoadingFinished()
    {
        LogWithContext("Loading finalizado");
        DeactivateLoadingUI();
        
        // Se configurado, ativa e toca a cinemática como uma única operação
        if (autoPlayCinematicAfterLoading)
        {
            ActivateAndPlayCinematic();
        }
        else
        {
            // Sem autoplay, permanecemos em EntryUI para permitir interação
            if (!isCinematicPlaying)
            {
                currentPhase = Phase.EntryUI;
            }
        }
    }

    // Chamado pelo SignalReceiver/SMB ao fim da cinematica
    public void OnCinematicFinished()
    {
        LogWithContext("Cinemática finalizada");
        isCinematicPlaying = false;
        hasHandoffHappened = true;
        currentPhase = Phase.Gameplay;

        // Entrega o controle ao jogador
        Managers.InputContextCoordinator.Instance?.SetPlayerContext();
        Managers.InputContextCoordinator.Instance?.DisableUiInteractions();

        if (restorePlayerControlOnCinematicEnd)
        {
            if (playerGate != null)
            {
                playerGate.UnfreezePhysics();
                playerGate.EnableController();
            }
            else
            {
                UnfreezePlayerPhysics();
                EnablePlayerController();
            }
        }

        if (markGameAsInStartMenu)
        {
            Managers.GameManager.Instance?.SetStartMenuActive(false);
        }
    }

    // Chamado ao fim da animação inicial (logo/intro) para habilitar o Main Menu
    public void OnStartingGameFinished()
    {
        LogWithContext("StartingGame finalizado, entrando em EntryUI");
        EnterUiContextWithFocus(defaultUiButton);
        currentPhase = Phase.EntryUI;
    }

    // Ativa a raiz da cinemática (garantindo pais ativos) e dispara o Play
    public void ActivateAndPlayCinematic()
    {
        // Idempotência e debouncing
        var nowMs = Time.unscaledTime * 1000f;
        if (isCinematicPlaying)
        {
            Debug.LogWarning("[SceneEntryFlowCoordinator] Tentativa duplicada de iniciar cinemática ignorada: já tocando.");
            return;
        }
        if ((nowMs - _lastCinematicPlayTimeUnscaled) < cinematicPlayCooldownMs)
        {
            Debug.LogWarning("[SceneEntryFlowCoordinator] Tentativa muito próxima de iniciar cinemática ignorada (cooldown).");
            return;
        }

        if (cinematicRoot == null && cinematicController == null && cinematicDirector == null)
        {
            Debug.LogWarning("[SceneEntryFlowCoordinator] Nenhuma cinemática configurada para ativar/tocar.");
            return;
        }

        if (cinematicRoot != null)
        {
            EnsureParentsActive(cinematicRoot);
            if (!cinematicRoot.activeSelf)
                cinematicRoot.SetActive(true);
        }

        // Marca estado e fase atual
        isCinematicPlaying = true;
        currentPhase = Phase.Cinematic;
        _lastCinematicPlayTimeUnscaled = nowMs;
        LogWithContext("Iniciando cinemática.");

        if (cinematicController != null)
        {
            cinematicController.Play();
            return;
        }

        if (cinematicDirector != null)
        {
            // Reinicia a timeline no início e toca
            try { cinematicDirector.time = 0; } catch { /* alguns diretors podem não suportar set de time */ }
            cinematicDirector.Play();
        }
    }

    // Ações de Loading
    public void ActivateLoadingUI()
    {
        if (isLoading)
        {
            Debug.LogWarning("[SceneEntryFlowCoordinator] Loading já ativo, chamada duplicada ignorada.");
            return;
        }
        isLoading = true;
        currentPhase = Phase.Loading;
        LogWithContext("Ativando Loading UI");

        if (loadingController != null)
        {
            loadingController.StartLoading(Managers.InputContextCoordinator.Instance);
            // O controlador já cuida de bloquear input e desabilitar UI.
        }
        else if (loadingAnimator != null && HasAnimatorTrigger(loadingAnimator, loadingStartTrigger))
        {
            loadingAnimator.SetTrigger(loadingStartTrigger);
            Managers.InputContextCoordinator.Instance?.SetBlockInputContext();
            Managers.InputContextCoordinator.Instance?.DisableUiInteractions();
        }
        else
        {
            Debug.LogWarning("[SceneEntryFlowCoordinator] LoadingAnimator/Trigger não configurados.");
        }
    }

    public void DeactivateLoadingUI()
    {
        if (!isLoading)
        {
            Debug.LogWarning("[SceneEntryFlowCoordinator] Loading já desativado, chamada duplicada ignorada.");
            return;
        }
        isLoading = false;
        LogWithContext("Desativando Loading UI");

        if (loadingController != null)
        {
            loadingController.StopLoading();
        }
        else if (loadingAnimator != null && HasAnimatorTrigger(loadingAnimator, loadingStopTrigger))
        {
            loadingAnimator.SetTrigger(loadingStopTrigger);
        }
    }

    // Utilitários de contexto
    public void EnterUiContextWithFocus(GameObject preferred)
    {
        // Entra em contexto de UI sem habilitar interações, para controlar o foco manualmente.
        Managers.InputContextCoordinator.Instance?.SetUiContext(enableUiInteractions: false);
        Managers.InputContextCoordinator.Instance?.EnableUiInteractions();
        FocusButton(preferred != null ? preferred : defaultUiButton);
    }

    public void EnterPlayerContext()
    {
        // Troca para contexto de gameplay; o coordinator já desabilita interações de UI.
        Managers.InputContextCoordinator.Instance?.SetPlayerContext();
        Managers.InputContextCoordinator.Instance?.DisableUiInteractions();
    }

    public void EnterBlockInputContext()
    {
        Managers.InputContextCoordinator.Instance?.SetBlockInputContext();
    }

    // HUD control
    public void SetHUDVisible(bool visible)
    {
        if (canvasHUD != null)
        {
            canvasHUD.SetActive(visible);
        }
    }

    public void ShowHUD() => SetHUDVisible(true);
    public void HideHUD() => SetHUDVisible(false);

    // Método para ser chamado pela Timeline/SignalReceiver ao fim da cinematica
    public void ActivateHUDFromCinematicEnd()
    {
        // Entrega controle ao jogador e exibe HUD
        Managers.InputContextCoordinator.Instance?.SetPlayerContext();
        SetHUDVisible(true);
    }

    // Player physics/control
    private void FreezePlayerPhysics()
    {
        if (playerRb != null)
        {
            playerRb.linearVelocity = Vector3.zero;
            playerRb.angularVelocity = Vector3.zero;
            playerRb.isKinematic = true;
            Debug.Log("[SceneEntryFlowCoordinator] Física do jogador travada");
        }
    }

    private void UnfreezePlayerPhysics()
    {
        if (playerRb != null)
        {
            playerRb.isKinematic = false;
            Debug.Log("[SceneEntryFlowCoordinator] Física do jogador destravada");
        }
    }

    private void DisablePlayerController()
    {
        if (playerControllerBehaviour != null)
        {
            playerControllerBehaviour.enabled = false;
            Debug.Log("[SceneEntryFlowCoordinator] Controlador do jogador desabilitado");
        }
    }

    private void EnablePlayerController()
    {
        if (playerControllerBehaviour != null)
        {
            playerControllerBehaviour.enabled = true;
            Debug.Log("[SceneEntryFlowCoordinator] Controlador do jogador reabilitado");
        }
    }

    // Focus helper
    private void FocusDefaultButtonNextFrame()
    {
        if (defaultUiButton == null) return;
        StartCoroutine(FocusNextFrame(defaultUiButton));
    }

    private System.Collections.IEnumerator FocusNextFrame(GameObject go)
    {
        yield return null; // esperar próximo frame para o EventSystem
        FocusButton(go);
    }

    private void FocusButton(GameObject go)
    {
        if (go == null) return;
        var es = EventSystem.current;
        if (es != null)
        {
            es.SetSelectedGameObject(go);
        }
    }

    // Helpers
    private static void EnsureParentsActive(GameObject child)
    {
        if (child == null) return;
        var t = child.transform;
        while (t != null)
        {
            if (!t.gameObject.activeSelf) t.gameObject.SetActive(true);
            t = t.parent;
        }
    }

    private static bool HasAnimatorTrigger(Animator animator, string triggerName)
    {
        if (animator == null || string.IsNullOrEmpty(triggerName)) return false;
        try
        {
            foreach (var p in animator.parameters)
            {
                if (p.type == AnimatorControllerParameterType.Trigger && p.name == triggerName)
                {
                    return true;
                }
            }
        }
        catch (Exception)
        {
            // Alguns animators podem não expor parâmetros em runtime
        }
        return false;
    }

    public Phase GetCurrentPhase() => currentPhase;

    private void LogWithContext(string message)
    {
        Debug.Log($"[SceneEntryFlowCoordinator][{currentPhase}] {message}");
    }

    // Centralizador de transições de fase: use este método como ponto único
    public void RequestTransitionTo(Phase target)
    {
        switch (target)
        {
            case Phase.StartingGame:
                LogWithContext("RequestTransitionTo → StartingGame");
                // Entra em contexto de UI sem habilitar interações; animações de abertura decidem quando finalizar
                Managers.InputContextCoordinator.Instance?.SetUiContext(enableUiInteractions: false);
                currentPhase = Phase.StartingGame;
                break;
            case Phase.EntryUI:
                LogWithContext("RequestTransitionTo → EntryUI");
                EnterUiContextWithFocus(defaultUiButton);
                currentPhase = Phase.EntryUI;
                break;
            case Phase.Loading:
                LogWithContext("RequestTransitionTo → Loading");
                ActivateLoadingUI();
                break;
            case Phase.Cinematic:
                LogWithContext("RequestTransitionTo → Cinematic");
                ActivateAndPlayCinematic();
                break;
            case Phase.Gameplay:
                LogWithContext("RequestTransitionTo → Gameplay");
                Managers.InputContextCoordinator.Instance?.SetPlayerContext();
                Managers.InputContextCoordinator.Instance?.DisableUiInteractions();
                currentPhase = Phase.Gameplay;
                break;
        }
    }
}