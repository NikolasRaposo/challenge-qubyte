using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Managers {
    [RequireComponent(typeof(PlayerInput))]
    public class PlayerInputRouter : MonoBehaviour {
        public event Action<Vector2> OnMove;
        public event Action<Vector2> OnLook;
        public event Action<bool> OnJump;
        public event Action<bool> OnSprint;
        public event Action OnPause;
        public event Action OnTornado;
        public event Action OnProjetarTornado;
        public event Action OnUiSubmit;

        [SerializeField] private bool debugLogs = false;

        private PlayerInput _playerInput;
        private InputAction _move;
        private InputAction _look;
        private InputAction _jump;
        private InputAction _sprint;
        private InputAction _pause;
        private InputAction _tornado;
        private InputAction _projetarTornado;
        private InputAction _uiSubmit;

        private void Awake() {
            _playerInput = GetComponent<PlayerInput>();
        }

        private void OnEnable() {
            CacheActions();
            _playerInput.onActionTriggered += HandleAction;
        }

        private void OnDisable() {
            _playerInput.onActionTriggered -= HandleAction;
        }

        private void CacheActions() {
            var actions = _playerInput.actions;
            // Gameplay actions (renamed from Player)
            _move = actions.FindAction("Gameplay/Move") ?? actions.FindAction("Player/Move") ?? actions.FindAction("Move");
            _look = actions.FindAction("Gameplay/Look") ?? actions.FindAction("Player/Look") ?? actions.FindAction("Look");
            _jump = actions.FindAction("Gameplay/Jump") ?? actions.FindAction("Player/Jump") ?? actions.FindAction("Jump");
            _sprint = actions.FindAction("Gameplay/Sprint") ?? actions.FindAction("Player/Sprint") ?? actions.FindAction("Sprint");
            _tornado = actions.FindAction("Gameplay/Tornado") ?? actions.FindAction("Tornado");
            _projetarTornado = actions.FindAction("Gameplay/ProjetarTornado") ?? actions.FindAction("ProjetarTornado");
            // Pause can exist in both UI and Gameplay maps
            _pause = actions.FindAction("UI/Pause") ?? actions.FindAction("Gameplay/Pause") ?? actions.FindAction("Player/Pause") ?? actions.FindAction("Pause");
            // Optional UI submit
            _uiSubmit = actions.FindAction("UI/Submit") ?? actions.FindAction("Submit");
        }

        private void HandleAction(InputAction.CallbackContext ctx) {
            var action = ctx.action;

            if (action == _move) {
                var v = ctx.ReadValue<Vector2>();
                if (debugLogs && ctx.performed) Debug.Log($"[Input] Move {v}");
                OnMove?.Invoke(v);
                return;
            }

            if (action == _look) {
                var v = ctx.ReadValue<Vector2>();
                if (debugLogs && ctx.performed) Debug.Log($"[Input] Look {v}");
                OnLook?.Invoke(v);
                return;
            }

            if (action == _jump) {
                var pressed = ctx.performed;
                if (debugLogs && (ctx.started || ctx.performed || ctx.canceled)) Debug.Log($"[Input] Jump {(pressed ? "pressed" : "released")}");
                OnJump?.Invoke(pressed);
                return;
            }

            if (action == _sprint) {
                var held = ctx.ReadValue<float>() > 0.5f;
                if (debugLogs && (ctx.started || ctx.performed || ctx.canceled)) Debug.Log($"[Input] Sprint {(held ? "on" : "off")}");
                OnSprint?.Invoke(held);
                return;
            }

            if (action == _pause && ctx.performed) {
                if (debugLogs) Debug.Log("[Input] Pause");
                OnPause?.Invoke();
                return;
            }

            if (action == _tornado && ctx.performed) {
                if (debugLogs) Debug.Log("[Input] Tornado");
                OnTornado?.Invoke();
                return;
            }

            if (action == _projetarTornado && ctx.performed) {
                if (debugLogs) Debug.Log("[Input] ProjetarTornado");
                OnProjetarTornado?.Invoke();
                return;
            }

            if (action == _uiSubmit && ctx.performed) {
                if (debugLogs) Debug.Log("[Input] UI Submit");
                OnUiSubmit?.Invoke();
            }
        }

        public void SwitchActionMap(string mapName) {
            if (string.IsNullOrEmpty(mapName)) return;
            _playerInput.SwitchCurrentActionMap(mapName);
            if (debugLogs) Debug.Log($"[Input] Switched map to {mapName}");
        }
    }
}