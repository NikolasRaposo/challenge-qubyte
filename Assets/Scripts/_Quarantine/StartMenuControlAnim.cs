using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Playables;

public class StartMenuControlAnim : MonoBehaviour
{
    [Header("Cinematic")]
    [SerializeField] private GameObject cinematicObject; // ativada após loading
    [Tooltip("PlayableDirector responsável por tocar a timeline da cinemática, se aplicável")]
    [SerializeField] private PlayableDirector cinematicDirector;
    [SerializeField] private bool blockInputAtStart = true; // bloqueia input ao iniciar
    [Header("HUD References")]
    [SerializeField] private GameObject canvasUI;
    [SerializeField] private GameObject canvasHUD;
    
    [Header("UI Focus")]
    [Tooltip("Botão padrão para receber foco ao abrir o menu")] 
    [SerializeField] private GameObject defaultUiButton;
    
    [Header("Animator References")]
    [Tooltip("Arraste aqui o GameObject que cont�m o Animator do Logo")]
    [SerializeField] private Animator logoGroupAnimator;

    [Tooltip("Arraste aqui o GameObject que cont�m o Animator dos pain�is de UI")]
    [SerializeField] private Animator uiPanelsAnimator;
    
    [Tooltip("Arraste aqui o GameObject que cont�m o Animator do Loading")]
    [SerializeField] private Animator LoadingAnimator;
    private bool _startSequenceTriggered;

    private void Start()
    {
        // Garante que o tempo global esteja normal ao abrir o menu
        Time.timeScale = 1f;
        canvasUI.SetActive(false);
        canvasHUD.SetActive(false);
        
        if (blockInputAtStart)
        {
            // Mantém o jogador imóvel e BLOQUEIA interações de UI até AnimationEnd
            Managers.InputContextCoordinator.Instance?.SetUiContext(false);
        }

        // Mantém o player suspenso no ar: kinematic e controlador desativado
        var gm = Managers.GameManager.Instance;
        GameObject playerGO = gm != null ? gm.player : GameObject.FindGameObjectWithTag("Player");
        if (playerGO != null)
        {
            var rb = playerGO.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            var ecm = playerGO.GetComponent<Player.ECMSaciController>();
            if (ecm != null)
            {
                ecm.enabled = false;
            }
        }
    }

    private void OnEnable()
    {
        Managers.GameManager.Instance?.SetStartMenuActive(true);
        
    }

    private void OnDisable()
    {
        Managers.GameManager.Instance?.SetStartMenuActive(false);
        var im = Managers.InputManager.Instance;
        if (im != null)
        {
            im.OnUiSubmit -= TriggerStartFromInput;
        }
    }

    // --- M�todos para o Animator 'LogoGroup' ---

    // --- Controles Públicos de Contexto (para Animation Events / Botões) ---
    // Use estes métodos diretamente em eventos de animação ou OnClick de botões.
    public void EnterUiContextWithFocus()
    {
        Managers.InputContextCoordinator.Instance?.SetUiContext(true, defaultUiButton);
    }

    public void EnableUiInteractionsWithFocus()
    {
        Managers.InputContextCoordinator.Instance?.EnableUiInteractions(defaultUiButton);
    }

    public void DisableUiInteractionsPublic()
    {
        Managers.InputContextCoordinator.Instance?.DisableUiInteractions();
    }

    public void EnterPlayerContextPublic()
    {
        Managers.InputContextCoordinator.Instance?.SetPlayerContext();
    }

    public void EnterBlockInputContextPublic()
    {
        Managers.InputContextCoordinator.Instance?.SetBlockInputContext();
    }

    public void AtivarMainMenuStart()
    {
        if (logoGroupAnimator != null)
        {
            logoGroupAnimator.SetTrigger("MainMenuStart");

            // Garante contexto de UI mas mantém interações desabilitadas por enquanto
            Managers.InputContextCoordinator.Instance?.SetUiContext(false);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // Opcional: focar um botão padrão para evitar perda de foco
            if (defaultUiButton != null)
            {
                var es = EventSystem.current;
                if (es != null)
                {
                    es.SetSelectedGameObject(defaultUiButton);
                    // Reforça foco no próximo frame (pós-transições)
                    StartCoroutine(FocusDefaultButtonNextFrame());
                }
                else
                {
                    Debug.LogWarning("Nenhum EventSystem na cena — seleções de UI podem falhar.");
                }
            }
        }
        else
        {
            Debug.LogWarning("O Animator do LogoGroup n�o foi atribu�do no Inspector!");
        }
    }

    private IEnumerator FocusDefaultButtonNextFrame()
    {
        yield return null;
        var es = EventSystem.current;
        if (es != null && defaultUiButton != null)
        {
            es.SetSelectedGameObject(defaultUiButton);
        }
    }

    public void AtivarMainMenuEarlyClose()
    {
        if (logoGroupAnimator != null)
        {
            logoGroupAnimator.SetTrigger("MainMenuEarlyClose");
        }
        else
        {
            Debug.LogWarning("O Animator do LogoGroup n�o foi atribu�do no Inspector!");
        }
    }

    // --- M�todos para o Animator 'UIPanels' ---

    public void AtivarMainMenuLateStart()
    {
        if (uiPanelsAnimator != null)
        {
            uiPanelsAnimator.SetTrigger("MainMenuLateStart");
        }
        else
        {
            Debug.LogWarning("O Animator do UIPanels n�o foi atribu�do no Inspector!");
        }
    }
    
    public void DesativarMainMenuPanels()
    {
        if (uiPanelsAnimator != null)
        {
            uiPanelsAnimator.SetTrigger("MainMenuDisable");
        }
        else
        {
            Debug.LogWarning("O Animator do UIPanels n�o foi atribu�do no Inspector!");
        }
    }

    public void BotaoDeStartPressionado()
    {
        if (uiPanelsAnimator != null)
        {
            Debug.Log("[StartMenuControlAnim] BotaoDeStartPressionado() invocado");
            uiPanelsAnimator.SetTrigger("StartButtonPressed");
        }
        else
        {
            Debug.LogWarning("O Animator do UIPanels n�o foi atribu�do no Inspector!");
        }
    }

    // Método único para o Button OnClick: garante ordem e evita perda de chamadas
    public void OnStartButtonClicked()
    {
        // Evita duplo disparo se já tiver sido iniciado por Submit
        if (_startSequenceTriggered)
        {
            Debug.Log("[StartMenuControlAnim] OnStartButtonClicked() ignorado – sequência já iniciada");
            return;
        }
        Debug.Log("[StartMenuControlAnim] OnStartButtonClicked() invocado");
        _startSequenceTriggered = true;
        // Executa animação do botão e inicia loading
        BotaoDeStartPressionado();
        AtivarLoadingScreen();
    }

    // --- Habilitação explícita do avanço provisório via Submit ---
    // Chame estes métodos a partir de AnimationEnd (após os botões aparecerem)
    public void EnableProvisionalAdvance()
    {
        var im = Managers.InputManager.Instance;
        if (im != null)
        {
            // Habilita interações da UI e assina uma única vez
            Managers.InputContextCoordinator.Instance?.EnableUiInteractions(defaultUiButton);
            im.OnUiSubmit -= TriggerStartFromInput;
            im.OnUiSubmit += TriggerStartFromInput;
        }
    }

    public void DisableProvisionalAdvance()
    {
        var im = Managers.InputManager.Instance;
        if (im != null)
        {
            im.OnUiSubmit -= TriggerStartFromInput;
        }
    }

    private void TriggerStartFromInput()
    {
        // Ao receber o Submit, desabilita imediatamente novas assinaturas provisórias
        DisableProvisionalAdvance();
        Debug.Log("[StartMenuControlAnim] TriggerStartFromInput() invocado");
        OnStartButtonClicked();
    }
    
    // --- M�todos para o Animator 'Loading' ---
    
    public void AtivarLoadingScreen()
    {
        if (LoadingAnimator != null)
        {
            Debug.Log("[StartMenuControlAnim] AtivarLoadingScreen() invocado");
            // A partir do clique em Start, bloqueia todo input de gameplay e UI
            Managers.InputContextCoordinator.Instance?.SetBlockInputContext();
            Managers.InputContextCoordinator.Instance?.DisableUiInteractions();
            // Garante que toda a cadeia de pais do objeto de Loading esteja ativa
            EnsureParentsActive(LoadingAnimator.gameObject);
            // Garante que o componente Animator esteja habilitado
            if (!LoadingAnimator.enabled)
                LoadingAnimator.enabled = true;
            // Valida existência do parâmetro Trigger "StartLoading"
            if (!HasAnimatorTrigger(LoadingAnimator, "StartLoading"))
            {
                Debug.LogError("[StartMenuControlAnim] Parâmetro Trigger 'StartLoading' não encontrado no LoadingAnimator. Verifique o Animator Controller.");
            }
            LoadingAnimator.SetTrigger("StartLoading");
            // O término do loading deve chamar OnLoadingFinished via AnimationEndHandler
        }
        else
        {
            Debug.LogWarning("O Animator do Loading n�o foi atribu�do no Inspector!");
        }
    }
    
    public void DesativarLoadingScreen()
    {
        if (LoadingAnimator != null)
        {
            LoadingAnimator.SetTrigger("StopLoading");
            //StartCoroutine(EnableHUDDelayed(0.5f));
        }
        else
        {
            Debug.LogWarning("O Animator do Loading n�o foi atribu�do no Inspector!");
        }
    }
    
    private IEnumerator EnableHUDDelayed(float delay)
    {
        yield return new WaitForSeconds(delay);
        canvasUI.SetActive(true);
        canvasHUD.SetActive(true);
    }
    
    // --- Callbacks de término ---
    // Conecte o AnimationEndHandler do estado final do Loading para chamar este método
    public void OnLoadingFinished()
    {
        Debug.Log("[StartMenuControlAnim] OnLoadingFinished() invocado");
        // Após o loading, ativa a cinematica e mantém input bloqueado
        if (cinematicObject != null)
        {
            // Garante que toda a cadeia de pais da cinematica esteja ativa
            EnsureParentsActive(cinematicObject);
            var before = cinematicObject.activeSelf;
            cinematicObject.SetActive(true);
            Debug.Log($"[StartMenuControlAnim] Cinemática '{cinematicObject.name}' ativa: {before} -> {cinematicObject.activeSelf}");

            // Se houver PlayableDirector configurado, dispara a timeline da cinemática
            if (cinematicDirector == null)
                cinematicDirector = cinematicObject.GetComponent<PlayableDirector>();
            if (cinematicDirector != null)
            {
                if (!cinematicDirector.enabled) cinematicDirector.enabled = true;
                cinematicDirector.time = 0;
                cinematicDirector.Play();
                Debug.Log("[StartMenuControlAnim] PlayableDirector.Play() disparado para a cinemática");
            }
            else
            {
                Debug.Log("[StartMenuControlAnim] Nenhum PlayableDirector encontrado/atribuído na cinemática — apenas ativado.");
            }
        }
        else
        {
            Debug.LogWarning("[StartMenuControlAnim] Nenhuma 'cinematicObject' atribuída. A cinematica não será ativada.");
        }
    }

    // Utilitário público opcional para forçar ativação e reproduzir a cinemática
    public void ForceActivateAndPlayCinematic()
    {
        OnLoadingFinished();
    }
    
    // Conecte o AnimationEndHandler do estado final da Cinematica para chamar este método
    public void OnCinematicFinished()
    {
        // Libera o jogador após terminar a cinematica
        Managers.InputContextCoordinator.Instance?.SetPlayerContext();
        Managers.GameManager.Instance?.SetStartMenuActive(false);

        // Restaura física e controlador para o player cair e seguir jogo
        var gm = Managers.GameManager.Instance;
        GameObject playerGO = gm != null ? gm.player : GameObject.FindGameObjectWithTag("Player");
        if (playerGO != null)
        {
            var rb = playerGO.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
            }

            var ecm = playerGO.GetComponent<Player.ECMSaciController>();
            if (ecm != null)
            {
                ecm.enabled = true;
            }
        }
    }

    // --- Utilitário local ---
    private void EnsureParentsActive(GameObject leaf)
    {
        if (leaf == null) return;
        var t = leaf.transform;
        while (t != null)
        {
            var go = t.gameObject;
            if (!go.activeSelf) go.SetActive(true);
            t = t.parent;
        }
    }

    private static bool HasAnimatorTrigger(Animator animator, string triggerName)
    {
        if (animator == null || string.IsNullOrEmpty(triggerName)) return false;
        foreach (var p in animator.parameters)
        {
            if (p.type == AnimatorControllerParameterType.Trigger && p.name == triggerName)
                return true;
        }
        return false;
    }
}