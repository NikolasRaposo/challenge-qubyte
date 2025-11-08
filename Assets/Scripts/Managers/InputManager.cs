using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Gameplay;

namespace Managers {
    public enum InputContext {
        Player,
        UI,
        BlockInput,
    }

    public class InputManager : MonoBehaviour {
        public static InputManager Instance { get; private set; }
        private StarterAssets _controls;
        public Vector2 Move { get; private set; }
        public Vector2 Look { get; private set; }
        public bool Jump { get; private set; }
        public bool Sprint { get; private set; }
        public event Action OnTornado, OnPause, OnProjetarTornado;
        private float _pauseInputLockUntil; // bloqueia pause por curto período após iniciar/trocar contexto

        private void Awake() {
            if (Instance != null && Instance != this) Destroy(gameObject);
            else {
                Instance = this;
                // Usa o root para evitar o warning "DontDestroyOnLoad only works for root GameObjects"
                var root = transform.root != null ? transform.root.gameObject : gameObject;
                DontDestroyOnLoad(root);
            }
            _controls = new StarterAssets();
            SetContext(InputContext.Player);
            // Evita que um input residual (ex: tecla pressionada ao entrar no Play) pause o jogo imediatamente
            _pauseInputLockUntil = Time.unscaledTime + 0.35f;
        }

        private void OnEnable() => _controls.Enable();
        private void OnDisable()
        {
            _controls.Disable();
            // Failsafe: ao desabilitar o gerenciador de input, interrompe vibração
            RumbleManager.Instance?.StopAllRumble();
        }

        // --- MÉTODOS PÚBLICOS DE CONTROLE DE CONTEXTO ---
        public void SetPlayerContext() => SetContext(InputContext.Player);
        public void SetUiContext() => SetContext(InputContext.UI);
        public void SetBlockInputContext() => SetContext(InputContext.BlockInput);

        // --- LÓGICA DE MUDANÇA DE CONTEXTO ---
        private void SetContext(InputContext context) {
            ClearAllBindings();
            _controls.Disable();
            switch (context) {
                case InputContext.Player:
                    SetPlayerEvents();
                    LockCursor();
                    // Debounce curto ao alternar contexto para evitar pause acidental
                    _pauseInputLockUntil = Time.unscaledTime + 0.15f;
                    break;
                case InputContext.UI:
                    SeUIEvents();
                    UnlockCursor();
                    // Failsafe: ao entrar no contexto de UI, interrompe vibração
                    RumbleManager.Instance?.StopAllRumble();
                    _pauseInputLockUntil = Time.unscaledTime + 0.15f;
                    break;
                case InputContext.BlockInput:
                    // Não faz bind de nada e libera o cursor
                    UnlockCursor();
                    // Failsafe: ao bloquear input, interrompe vibração
                    RumbleManager.Instance?.StopAllRumble();
                    _pauseInputLockUntil = Time.unscaledTime + 0.15f;
                    break;
            }
        }

        private void SetPlayerEvents() {
            _controls.Player.Enable();
            
            // Binds de Eventos (Ações de 1 frame)
            _controls.Player.Tornado.performed += TornadoOnPerformed;
            _controls.Player.ProjetarTornado.performed += ProjetarTornadoOnPerformed;
            _controls.Player.Pause.performed += PauseOnPerformed;
            _controls.Player.Jump.performed += JumpOnPerformed;

            // Binds de Estado Contínuo (Valores que mudam)
            _controls.Player.Move.performed += MoveOnPerformed;
            _controls.Player.Move.canceled += MoveOnCanceled;
            _controls.Player.Look.performed += LookOnPerformed;
            _controls.Player.Look.canceled += LookOnCanceled;
            _controls.Player.Sprint.performed += SprintOnPerformed;
            _controls.Player.Sprint.canceled += SprintOnCanceled;
        }

        private void SeUIEvents() {
            _controls.UI.Enable();
            _controls.UI.Pause.performed += PauseOnPerformed;
            // Garante que o botão de pause do gamepad funcione também no contexto de UI
            _controls.Player.Pause.Enable();
            _controls.Player.Pause.performed += PauseOnPerformed;
        }

        // --- Handlers de Input (Atualizam os valores) ---
        private void MoveOnPerformed(InputAction.CallbackContext ctx) => Move = ctx.ReadValue<Vector2>();
        private void MoveOnCanceled(InputAction.CallbackContext ctx) => Move = Vector2.zero;
        private void LookOnPerformed(InputAction.CallbackContext ctx) => Look = ctx.ReadValue<Vector2>();
        private void LookOnCanceled(InputAction.CallbackContext ctx) => Look = Vector2.zero;
        private void SprintOnPerformed(InputAction.CallbackContext ctx) => Sprint = true;
        private void SprintOnCanceled(InputAction.CallbackContext ctx) => Sprint = false;
        private void JumpOnPerformed(InputAction.CallbackContext ctx) => Jump = true;
        
        // --- Handlers de Eventos (Disparam os C# Events) ---
        private void TornadoOnPerformed(InputAction.CallbackContext obj) => OnTornado?.Invoke();
        private void ProjetarTornadoOnPerformed(InputAction.CallbackContext obj) => OnProjetarTornado?.Invoke();
        private void PauseOnPerformed(InputAction.CallbackContext obj) {
            // Ignora eventos de pause durante janela de bloqueio
            if (Time.unscaledTime < _pauseInputLockUntil) return;
            OnPause?.Invoke();
            // Debounce curto para evitar dupla invocação no mesmo frame
            _pauseInputLockUntil = Time.unscaledTime + 0.1f;
        }

        // --- MÉTODO DE "CONSUMO" DE INPUT ---
        // O ThirdPersonController vai chamar isso após pular
        public void ConsumeJumpInput() => Jump = false;
        
        // --- Limpeza ---
        private void ClearAllBindings() {
            ClearPlayerBindings();
            ClearUiBindings();
        }

        private void ClearPlayerBindings() {
            _controls.Player.Tornado.performed -= TornadoOnPerformed;
            _controls.Player.ProjetarTornado.performed -= ProjetarTornadoOnPerformed;
            _controls.Player.Pause.performed -= PauseOnPerformed;
            _controls.Player.Jump.performed -= JumpOnPerformed;
            _controls.Player.Move.performed -= MoveOnPerformed;
            _controls.Player.Move.canceled -= MoveOnCanceled;
            _controls.Player.Look.performed -= LookOnPerformed;
            _controls.Player.Look.canceled -= LookOnCanceled;
            _controls.Player.Sprint.performed -= SprintOnPerformed;
            _controls.Player.Sprint.canceled -= SprintOnCanceled;
        }
        
        private void ClearUiBindings() {
           _controls.UI.Pause.performed -= PauseOnPerformed;
        }
        
        // --- Gerenciamento do Cursor ---
        private static void LockCursor() {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        private static void UnlockCursor() {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}