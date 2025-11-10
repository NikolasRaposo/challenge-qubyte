using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Gameplay;

namespace Managers {
    public enum InputContext {
        Gameplay,
        UI,
        None,
    }

    public class InputManager : MonoBehaviour {
        public static InputManager Instance { get; private set; }

        [Header("Fonte de Input")]
        [Tooltip("Roteador central que expõe eventos tipados.")]
        public PlayerInputRouter inputRouter;
        [Tooltip("Componente PlayerInput usado para alternar mapas.")]
        public PlayerInput playerInput;

        public Vector2 Move { get; private set; }
        public Vector2 Look { get; private set; }
        public bool Jump { get; private set; }
        public bool Sprint { get; private set; }
        public event Action OnTornado, OnProjetarTornado;
        public event Action OnUiSubmit; // opcional

        private void Awake() {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            var root = transform.root != null ? transform.root.gameObject : gameObject;
            DontDestroyOnLoad(root);

            // Auto-binding: primeiro procura no mesmo GameObject; depois, fallback global
            if (inputRouter == null)
                inputRouter = GetComponent<PlayerInputRouter>() ?? FindFirstObjectByType<PlayerInputRouter>();

            if (playerInput == null)
                playerInput = GetComponent<PlayerInput>()
                               ?? (inputRouter != null ? inputRouter.GetComponent<PlayerInput>() : null)
                               ?? FindFirstObjectByType<PlayerInput>();
        }

        private void OnEnable() {
            if (inputRouter != null) {
                inputRouter.OnMove += HandleMove;
                inputRouter.OnLook += HandleLook;
                inputRouter.OnJump += HandleJump;
                inputRouter.OnSprint += HandleSprint;
                inputRouter.OnTornado += ForwardTornado;
                inputRouter.OnProjetarTornado += ForwardProjetarTornado;
                inputRouter.OnUiSubmit += ForwardUiSubmit;
            }
        }

        private void OnDisable() {
            if (inputRouter != null) {
                inputRouter.OnMove -= HandleMove;
                inputRouter.OnLook -= HandleLook;
                inputRouter.OnJump -= HandleJump;
                inputRouter.OnSprint -= HandleSprint;
                inputRouter.OnTornado -= ForwardTornado;
                inputRouter.OnProjetarTornado -= ForwardProjetarTornado;
                inputRouter.OnUiSubmit -= ForwardUiSubmit;
            }
            RumbleManager.Instance?.StopAllRumble();
        }

        // --- CONTEXTO ---
        public void SetGameplayContext() => SetContext(InputContext.Gameplay);
        public void SetUiContext() => SetContext(InputContext.UI);
        public void SetBlockInputContext() => SetContext(InputContext.None);

        private void SetContext(InputContext context) {
            if (playerInput == null) return;

            switch (context) {
                case InputContext.Gameplay:
                    SwitchMapSafe("Gameplay");
                    LockCursor();
                    break;
                case InputContext.UI:
                    SwitchMapSafe("UI");
                    UnlockCursor();
                    RumbleManager.Instance?.StopAllRumble();
                    break;
                case InputContext.None:
                    if (!SwitchMapSafe("None")) {
                        // Se não houver mapa "None", desabilita PlayerInput como fallback
                        playerInput.enabled = false;
                    }
                    UnlockCursor();
                    RumbleManager.Instance?.StopAllRumble();
                    break;
            }
        }

        private bool SwitchMapSafe(string mapName) {
            if (string.IsNullOrEmpty(mapName)) return false;
            var actions = playerInput.actions;
            if (actions == null) return false;
            var map = actions.FindActionMap(mapName, true);
            if (map == null) return false;
            playerInput.enabled = true;
            playerInput.SwitchCurrentActionMap(mapName);
            return true;
        }

        // --- Atualizadores ---
        private void HandleMove(Vector2 v) => Move = v;
        private void HandleLook(Vector2 v) => Look = v;
        private void HandleJump(bool pressed) { if (pressed) Jump = true; }
        private void HandleSprint(bool held) => Sprint = held;
        private void ForwardTornado() => OnTornado?.Invoke();
        private void ForwardProjetarTornado() => OnProjetarTornado?.Invoke();
        private void ForwardUiSubmit() => OnUiSubmit?.Invoke();

        // --- MÉTODO DE "CONSUMO" DE INPUT ---
        public void ConsumeJumpInput() => Jump = false;

        // --- Cursor ---
        private static void LockCursor() { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
        private static void UnlockCursor() { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
    }
}