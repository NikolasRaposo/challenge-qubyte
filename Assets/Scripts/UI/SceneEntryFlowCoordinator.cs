using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.Playables;
using ECM.Controllers; // BaseCharacterController para fallback de pausa ECM
using ECM.Components; // CharacterMovement para fallback de pausa ECM

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
    [Tooltip("Raiz do UMainMenu (Entry UI). Use este objeto para controlar a visibilidade do menu principal e telas iniciais.")]
    [SerializeField] private GameObject entryUiRoot;
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

    [Header("Dev Overrides")]
    [SerializeField] private bool overrideStartPhase = false;
    [SerializeField] private Phase startPhaseOverride = Phase.Gameplay;

    [Header("Dev Start Checkpoint")]
    [Tooltip("Se verdadeiro, ao usar override para Gameplay, posiciona o player em um checkpoint.")]
    [SerializeField] private bool startAtCheckpoint = false;
    [Tooltip("Nome exato do GameObject marcado com a tag de checkpoint (prioritário sobre índice).")]
    [SerializeField] private string startCheckpointName;
    [Tooltip("Índice do checkpoint descoberto pelo CheckpointManager (use -1 para ignorar).")]
    [SerializeField] private int startCheckpointIndex = -1;

    [Header("Controladores Opcionais")]
    [SerializeField] private UiPhaseController uiController;
    [SerializeField] private LoadingPhaseController loadingController;
    [SerializeField] private CinematicPhaseController cinematicController;
    [SerializeField] private PlayerControlGate playerGate;

    private Rigidbody playerRb;
    private Behaviour playerControllerBehaviour; // Ex.: ECMSaciController
    private BaseCharacterController playerEcmController; // Fallback para pausa ECM
    private CharacterMovement playerMovement; // Fallback para pausa ECM

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
            // Captura referências ECM para fallback de pausa/retomada
            playerEcmController = gm.player.GetComponent<BaseCharacterController>();
            playerMovement = gm.player.GetComponent<CharacterMovement>();
        }
    }

    private void Start()
    {
        if (overrideStartPhase)
        {
            Managers.GameManager.Instance?.SetStartMenuActive(false);
            if (startPhaseOverride == Phase.Gameplay && startAtCheckpoint)
            {
                // Aplica spawn imediato em checkpoint antes de entrar em Gameplay
                var applied = Managers.CheckpointManager.Instance?.ApplyStartCheckpoint(startCheckpointName, startCheckpointIndex) ?? false;
                if (!applied)
                {
                    Debug.LogWarning("[SceneEntryFlowCoordinator] Falha ao aplicar StartAtCheckpoint. Verifique nome/índice e se há CheckpointManager na cena.");
                }
            }
            RequestTransitionTo(startPhaseOverride);
            return;
        }
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
                playerGate.FreezePhysicsAndPauseECM(restoreVelocityOnResume: false);
            }
            else
            {
                FreezePlayerPhysics();
                PauseEcmFallback(restoreVelocityOnResume: false);
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

        // Garante que raízes de UI estejam com pais ativos para permitir exibição quando necessário
        EnsureParentsActive(entryUiRoot);
        EnsureParentsActive(canvasHUD);

        // Estado inicial da HUD
        if (canvasHUD != null)
        {
            canvasHUD.SetActive(hudEnabledOnStart);
            Managers.UIManager.Instance?.NotifyUiChange(
                source: nameof(SceneEntryFlowCoordinator),
                action: $"HUD.SetActive({hudEnabledOnStart})",
                target: canvasHUD,
                details: "Estado inicial da HUD"
            );
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
                playerGate.UnfreezePhysicsAndResumeECM(restoreVelocityOnResume: false);
                playerGate.EnableController();
            }
            else
            {
                ResumeEcmFallback(restoreVelocityOnResume: false);
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
            Managers.UIManager.Instance?.NotifyUiChange(
                source: nameof(SceneEntryFlowCoordinator),
                action: "LoadingController.StartLoading",
                target: entryUiRoot != null ? entryUiRoot : canvasUI,
                details: "Ativando Loading via controlador"
            );
        }
        else if (loadingAnimator != null && HasAnimatorTrigger(loadingAnimator, loadingStartTrigger))
        {
            loadingAnimator.SetTrigger(loadingStartTrigger);
            Managers.InputContextCoordinator.Instance?.SetBlockInputContext();
            Managers.InputContextCoordinator.Instance?.DisableUiInteractions();
            Managers.UIManager.Instance?.NotifyUiChange(
                source: nameof(SceneEntryFlowCoordinator),
                action: $"Animator.SetTrigger({loadingStartTrigger})",
                target: loadingAnimator.gameObject,
                details: "Ativando Loading UI"
            );
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
            Managers.UIManager.Instance?.NotifyUiChange(
                source: nameof(SceneEntryFlowCoordinator),
                action: "LoadingController.StopLoading",
                target: entryUiRoot != null ? entryUiRoot : canvasUI,
                details: "Desativando Loading via controlador"
            );
        }
        else if (loadingAnimator != null && HasAnimatorTrigger(loadingAnimator, loadingStopTrigger))
        {
            loadingAnimator.SetTrigger(loadingStopTrigger);
            Managers.UIManager.Instance?.NotifyUiChange(
                source: nameof(SceneEntryFlowCoordinator),
                action: $"Animator.SetTrigger({loadingStopTrigger})",
                target: loadingAnimator.gameObject,
                details: "Desativando Loading UI"
            );
        }
    }

    // Utilitários de contexto
    public void EnterUiContextWithFocus(GameObject preferred)
    {
        // Entra em contexto de UI sem habilitar interações, para controlar o foco manualmente.
        Managers.InputContextCoordinator.Instance?.SetUiContext(enableUiInteractions: false);
        Managers.InputContextCoordinator.Instance?.EnableUiInteractions();
        FocusButton(preferred != null ? preferred : defaultUiButton);
        Managers.UIManager.Instance?.NotifyUiChange(
            source: nameof(SceneEntryFlowCoordinator),
            action: "EnterUiContextWithFocus",
            target: preferred != null ? preferred : defaultUiButton,
            details: "Entrada em UI com foco"
        );
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
            Managers.UIManager.Instance?.NotifyUiChange(
                source: nameof(SceneEntryFlowCoordinator),
                action: $"HUD.SetActive({visible})",
                target: canvasHUD,
                details: "Controle bruto de HUD"
            );
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

    // ECM pause/resume fallback (quando PlayerControlGate não estiver disponível)
    private void PauseEcmFallback(bool restoreVelocityOnResume)
    {
        if (playerMovement != null)
        {
            // Pausa imediata a movimentação, evita escrita de velocidade em RB kinematic
            playerMovement.Pause(true, restoreVelocity: false);
        }
        if (playerEcmController != null)
        {
            playerEcmController.restoreVelocityOnResume = restoreVelocityOnResume;
            playerEcmController.pause = true;
        }
    }

    private void ResumeEcmFallback(bool restoreVelocityOnResume)
    {
        if (playerMovement != null)
        {
            playerMovement.Pause(false, restoreVelocityOnResume);
        }
        if (playerEcmController != null)
        {
            playerEcmController.restoreVelocityOnResume = restoreVelocityOnResume;
            playerEcmController.pause = false;
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
            Managers.UIManager.Instance?.NotifyUiChange(
                source: nameof(SceneEntryFlowCoordinator),
                action: "FocusButton",
                target: go
            );
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
        Managers.UIManager.Instance?.NotifyUiChange(
            source: nameof(SceneEntryFlowCoordinator),
            action: $"RequestTransitionTo({target})",
            target: entryUiRoot != null ? entryUiRoot : canvasUI,
            details: "Centralizador de transição"
        );
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
                SetEntryUiActive(true);
                EnterUiContextWithFocus(defaultUiButton);
                currentPhase = Phase.EntryUI;
                break;
            case Phase.Loading:
                LogWithContext("RequestTransitionTo → Loading");
                SetEntryUiActive(true);
                ActivateLoadingUI();
                break;
            case Phase.Cinematic:
                LogWithContext("RequestTransitionTo → Cinematic");
                SetEntryUiActive(false);
                ActivateAndPlayCinematic();
                break;
            case Phase.Gameplay:
                LogWithContext("RequestTransitionTo → Gameplay");
                SetEntryUiActive(false);
                Managers.InputContextCoordinator.Instance?.SetPlayerContext();
                Managers.InputContextCoordinator.Instance?.DisableUiInteractions();
                currentPhase = Phase.Gameplay;
                break;
        }
    }

    private void SetEntryUiActive(bool active)
    {
        // Se o UIManager conhecer o MainMenu, delega a visibilidade a ele
        if (Managers.UIManager.Instance != null)
        {
            Managers.UIManager.Instance.SetMainMenuVisible(active);
            return;
        }

        // Fallback: controla diretamente a raiz do UMainMenu quando disponível
        var root = entryUiRoot != null ? entryUiRoot : canvasUI;
        if (root == null) return;
        if (root.activeSelf == active) return;
        root.SetActive(active);
        Managers.UIManager.Instance?.NotifyUiChange(
            source: nameof(SceneEntryFlowCoordinator),
            action: $"EntryUI.SetActive({active})",
            target: root,
            details: active ? "Ativando UI de entrada" : "Desativando UI de entrada"
        );
    }
}