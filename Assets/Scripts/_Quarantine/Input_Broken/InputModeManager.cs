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

    // Estado atual do modo de input (Player/UI)
    public bool IsPlayerMode { get; private set; }
    public bool IsUIMode => !IsPlayerMode;

    // Helper: deve processar input de gameplay?
    public bool ShouldProcessGameplayInput()
    {
        return IsPlayerMode && enabled;
    }

    private void Awake()
    {
        _playerInput = GetComponent<PlayerInput>();
        _starterAssetsInputs = GetComponent<StarterAssetsInputs>();

        // Inicializa estado com base no action map atual
        IsPlayerMode = _playerInput != null &&
                       _playerInput.currentActionMap != null &&
                       _playerInput.currentActionMap.name == "Player";
    }

    // Use este método para quando o menu estiver ativo
    public void SetUIMode()
    {
        // Troca o mapa de ações para "UI"
        _playerInput.SwitchCurrentActionMap("UI");
        IsPlayerMode = false;

        // Libera o cursor e o torna visível
        _starterAssetsInputs.cursorLocked = false;
        _starterAssetsInputs.cursorInputForLook = false; // MUITO IMPORTANTE!
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // Zera estados para evitar travamentos ao alternar para UI
        _starterAssetsInputs.move = Vector2.zero;
        _starterAssetsInputs.SprintInput(false);
        _starterAssetsInputs.JumpInput(false);
        _starterAssetsInputs.LookInput(Vector2.zero);

        Debug.Log("Input mode switched to UI.");
    }

    // Use este método para quando o jogo começar
    public void SetPlayerMode()
    {
        // Troca o mapa de ações de volta para "Player"
        _playerInput.SwitchCurrentActionMap("Player");
        IsPlayerMode = true;

        // Prende o cursor e o esconde para o controle da câmera
        _starterAssetsInputs.cursorLocked = true;
        _starterAssetsInputs.cursorInputForLook = true;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        // Zera estados pendentes no retorno ao jogo
        _starterAssetsInputs.move = Vector2.zero;
        _starterAssetsInputs.SprintInput(false);
        _starterAssetsInputs.JumpInput(false);

        Debug.Log("Input mode switched to PLAYER.");
    }
}