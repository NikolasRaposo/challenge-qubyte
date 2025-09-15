using UnityEngine;
using UnityEngine.InputSystem; // Importante!
using ThirdParty.StarterAssets.InputSystem;

// Este script precisa do PlayerInput e do StarterAssetsInputs para funcionar.
[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(StarterAssetsInputs))]
public class InputModeManager : MonoBehaviour
{
    private PlayerInput _playerInput;
    private StarterAssetsInputs _starterAssetsInputs;

    private void Awake()
    {
        _playerInput = GetComponent<PlayerInput>();
        _starterAssetsInputs = GetComponent<StarterAssetsInputs>();
    }

    // Use este m�todo para quando o menu estiver ativo
    public void SetUIMode()
    {
        // Troca o mapa de a��es para "UI"
        _playerInput.SwitchCurrentActionMap("UI");

        // Libera o cursor e o torna vis�vel
        _starterAssetsInputs.cursorLocked = false;
        _starterAssetsInputs.cursorInputForLook = false; // MUITO IMPORTANTE!
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        Debug.Log("Input mode switched to UI.");
    }

    // Use este m�todo para quando o jogo come�ar
    public void SetPlayerMode()
    {
        // Troca o mapa de a��es de volta para "Player"
        _playerInput.SwitchCurrentActionMap("Player");

        // Prende o cursor e o esconde para o controle da c�mera
        _starterAssetsInputs.cursorLocked = true;
        _starterAssetsInputs.cursorInputForLook = true;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        Debug.Log("Input mode switched to PLAYER.");
    }
}