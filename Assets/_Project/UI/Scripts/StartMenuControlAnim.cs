using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class StartMenuControlAnim : MonoBehaviour
{
    [Header("Cinematic")]
    [SerializeField] private GameObject cinematicObject; // ativada após loading
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
            // Mantém o jogador imóvel, mas deixa a UI clicável
            Managers.InputManager.Instance?.SetUiContext();
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
        var im = Managers.InputManager.Instance;
        if (im != null)
        {
            im.OnUiSubmit += TriggerStartFromInput;
        }
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

    public void AtivarMainMenuStart()
    {
        if (logoGroupAnimator != null)
        {
            logoGroupAnimator.SetTrigger("MainMenuStart");

            // Garante que a UI esteja em foco e o cursor visível
            Managers.InputManager.Instance?.SetUiContext();
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
            uiPanelsAnimator.SetTrigger("StartButtonPressed");
        }
        else
        {
            Debug.LogWarning("O Animator do UIPanels n�o foi atribu�do no Inspector!");
        }
    }

    private void TriggerStartFromInput()
    {
        if (_startSequenceTriggered) return;
        _startSequenceTriggered = true;
        // Emula clique no botão Start
        BotaoDeStartPressionado();
        AtivarLoadingScreen();
    }
    
    // --- M�todos para o Animator 'Loading' ---
    
    public void AtivarLoadingScreen()
    {
        if (LoadingAnimator != null)
        {
            // A partir do clique em Start, bloqueia todo input de gameplay e UI
            Managers.InputManager.Instance?.SetBlockInputContext();
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
        // Após o loading, ativa a cinematica e mantém input bloqueado
        if (cinematicObject != null)
            cinematicObject.SetActive(true);
        else
            Debug.LogWarning("Nenhuma 'cinematicObject' atribuída. A cinematica não será ativada.");
    }
    
    // Conecte o AnimationEndHandler do estado final da Cinematica para chamar este método
    public void OnCinematicFinished()
    {
        // Libera o jogador após terminar a cinematica
        Managers.InputManager.Instance?.SetPlayerContext();
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
}