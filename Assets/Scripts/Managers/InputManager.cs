using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Managers {
    public enum InputContext {
        Player,
        UI,
        BlockInput,
    }
    public class InputManager : MonoBehaviour {
        public static InputManager Instance { get; private set; }
        private StarterAssets _controls;
        public event Action OnTornado; 
        private void Awake() {
            if (Instance != null && Instance != this) Destroy(gameObject);
            else Instance = this;
            _controls = new StarterAssets();
            SetContext(InputContext.Player);
        }
        private void OnEnable() => _controls.Enable();
        private void OnDisable() => _controls.Disable();
        public void SetPlayerContext() {
            SetContext(InputContext.Player);
        }
        public void SetUiContext() {
            SetContext(InputContext.UI);
        }
        public void SetBlockInputContext() {
            SetContext(InputContext.BlockInput);
        }
        private void SetContext(InputContext context) {
            ClearAllBindings();
            _controls.Disable();
            switch (context) {
                case InputContext.Player:
                    _controls.Player.Tornado.performed += TornadoOnPerformed;
                    break;
                case InputContext.UI:
                    break;
                case InputContext.BlockInput:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(context), context, null);
            }
        }
        private void TornadoOnPerformed(InputAction.CallbackContext obj) => OnTornado?.Invoke();
        private void ClearAllBindings() {
            _controls.Player.Tornado.performed -= TornadoOnPerformed;
        }
    }
}
