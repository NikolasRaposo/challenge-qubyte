using UnityEngine;

public class StartMenuControlAnim : MonoBehaviour
{
    [Header("Animator References")]
    [Tooltip("Arraste aqui o GameObject que contém o Animator do Logo")]
    [SerializeField] private Animator logoGroupAnimator;

    [Tooltip("Arraste aqui o GameObject que contém o Animator dos painéis de UI")]
    [SerializeField] private Animator uiPanelsAnimator;
    
    [Tooltip("Arraste aqui o GameObject que contém o Animator do Loading")]
    [SerializeField] private Animator LoadingAnimator;

    // --- Métodos para o Animator 'LogoGroup' ---

    public void AtivarMainMenuStart()
    {
        if (logoGroupAnimator != null)
        {
            logoGroupAnimator.SetTrigger("MainMenuStart");
        }
        else
        {
            Debug.LogWarning("O Animator do LogoGroup não foi atribuído no Inspector!");
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
            Debug.LogWarning("O Animator do LogoGroup não foi atribuído no Inspector!");
        }
    }

    // --- Métodos para o Animator 'UIPanels' ---

    public void AtivarMainMenuLateStart()
    {
        if (uiPanelsAnimator != null)
        {
            uiPanelsAnimator.SetTrigger("MainMenuLateStart");
        }
        else
        {
            Debug.LogWarning("O Animator do UIPanels não foi atribuído no Inspector!");
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
            Debug.LogWarning("O Animator do UIPanels não foi atribuído no Inspector!");
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
            Debug.LogWarning("O Animator do UIPanels não foi atribuído no Inspector!");
        }
    }
    
    // --- Métodos para o Animator 'Loading' ---
    
    public void AtivarLoadingScreen()
    {
        if (LoadingAnimator != null)
        {
            LoadingAnimator.SetTrigger("StartLoading");
        }
        else
        {
            Debug.LogWarning("O Animator do Loading não foi atribuído no Inspector!");
        }
    }
    
    public void DesativarLoadingScreen()
    {
        if (LoadingAnimator != null)
        {
            LoadingAnimator.SetTrigger("StopLoading");
        }
        else
        {
            Debug.LogWarning("O Animator do Loading não foi atribuído no Inspector!");
        }
    }
    
}