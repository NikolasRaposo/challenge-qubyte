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
        public event Action OnTornado, OnPause; 
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
                    SetPlayerEvents();
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                    break;
                case InputContext.UI:
                    _controls.UI.Enable();
                    _controls.UI.Pause.performed += PauseOnPerformed;
                    
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                    break;
                case InputContext.BlockInput:
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(context), context, null);
            }
        }
        private void SetPlayerEvents() {
            _controls.Player.Enable();
            _controls.Player.Tornado.performed += TornadoOnPerformed;
            _controls.Player.Pause.performed += PauseOnPerformed;
        }
        private void SeUIEvents() {
            _controls.Player.Enable();
            _controls.Player.Tornado.performed += TornadoOnPerformed;
            _controls.Player.Pause.performed += PauseOnPerformed;
        }
        
        
        private void TornadoOnPerformed(InputAction.CallbackContext obj) => OnTornado?.Invoke();
        private void PauseOnPerformed(InputAction.CallbackContext obj) => OnPause?.Invoke();
        private void ClearAllBindings() {
            ClearPlayerBindings();
            ClearUiBindings();
        }
        private void ClearPlayerBindings() {
            _controls.Player.Tornado.performed -= TornadoOnPerformed;
            _controls.Player.Pause.performed -= PauseOnPerformed;
        }
        private void ClearUiBindings() {
            _controls.UI.Pause.performed -= PauseOnPerformed;
        }
    }
}
