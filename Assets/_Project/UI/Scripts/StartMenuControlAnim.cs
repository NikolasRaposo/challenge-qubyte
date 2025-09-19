using System.Collections;
using UnityEngine;

public class StartMenuControlAnim : MonoBehaviour
{
    [Header("HUD References")]
    [SerializeField] private GameObject canvasUI;
    [SerializeField] private GameObject canvasHUD;
    
    [Header("Animator References")]
    [Tooltip("Arraste aqui o GameObject que cont�m o Animator do Logo")]
    [SerializeField] private Animator logoGroupAnimator;

    [Tooltip("Arraste aqui o GameObject que cont�m o Animator dos pain�is de UI")]
    [SerializeField] private Animator uiPanelsAnimator;
    
    [Tooltip("Arraste aqui o GameObject que cont�m o Animator do Loading")]
    [SerializeField] private Animator LoadingAnimator;

    private void Start()
    {
        canvasUI.SetActive(false);
        canvasHUD.SetActive(false);
    }

    // --- M�todos para o Animator 'LogoGroup' ---

    public void AtivarMainMenuStart()
    {
        if (logoGroupAnimator != null)
        {
            logoGroupAnimator.SetTrigger("MainMenuStart");
        }
        else
        {
            Debug.LogWarning("O Animator do LogoGroup n�o foi atribu�do no Inspector!");
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
    
    // --- M�todos para o Animator 'Loading' ---
    
    public void AtivarLoadingScreen()
    {
        if (LoadingAnimator != null)
        {
            LoadingAnimator.SetTrigger("StartLoading");
            StartCoroutine(EnableHUDDelayed(3f));
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
}