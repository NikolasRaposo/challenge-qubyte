using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Managers
{
    /// <summary>
    /// Coordenador central de contexto de input.
    /// - ÚNICO ponto que chama SetUiContext / SetPlayerContext / SetBlockInputContext no InputManager.
    /// - Também controla o InputSystemUIInputModule para habilitar/desabilitar interações de UI.
    /// </summary>
    public class InputContextCoordinator : MonoBehaviour
    {
        public static InputContextCoordinator Instance { get; private set; }

        private EventSystem _eventSystem;
        private InputSystemUIInputModule _uiModule;
        private InputContext _currentContext = InputContext.Player;
        private bool _uiInteractionsEnabled;
        private GameObject _defaultFocus;
        private GameObject _lastSelected;

        private float _lastMouseActivityTime;
        private float _lastGamepadActivityTime;
        private const float ActivityWindow = 1.25f; // segundos

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            var root = transform.root != null ? transform.root.gameObject : gameObject;
            DontDestroyOnLoad(root);
        }

        private void OnEnable()
        {
            EnsureUiRefs();
        }

        private void EnsureUiRefs()
        {
            if (_eventSystem == null)
                _eventSystem = FindFirstObjectByType<EventSystem>();
            if (_uiModule == null)
            {
                if (_eventSystem != null)
                    _uiModule = _eventSystem.GetComponent<InputSystemUIInputModule>();
                if (_uiModule == null)
                    _uiModule = FindFirstObjectByType<InputSystemUIInputModule>();
            }
        }

        /// <summary>
        /// Entra em contexto de UI. Por padrão, controla interações do módulo de UI.
        /// </summary>
        [Qubyte.Tracking.TrackableCall]
        public void SetUiContext(bool enableUiInteractions = true, GameObject defaultFocus = null)
        {
            InputManager.Instance?.SetUiContext();
            EnsureUiRefs();
            _currentContext = InputContext.UI;
            _uiInteractionsEnabled = enableUiInteractions;
            _defaultFocus = defaultFocus != null ? defaultFocus : _defaultFocus;
            if (_uiModule != null) _uiModule.enabled = enableUiInteractions;
            // Por padrão mantenha cursor escondido ao entrar em UI; será mostrado ao detectar atividade de mouse
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            if (enableUiInteractions)
            {
                GuaranteeUiFocus();
            }
        }

        /// <summary>
        /// Entra em contexto de gameplay e desabilita interações de UI.
        /// </summary>
        [Qubyte.Tracking.TrackableCall]
        public void SetPlayerContext()
        {
            InputManager.Instance?.SetPlayerContext();
            EnsureUiRefs();
            if (_uiModule != null)
                _uiModule.enabled = false;
            if (_eventSystem != null)
                _eventSystem.SetSelectedGameObject(null);
            _currentContext = InputContext.Player;
            _uiInteractionsEnabled = false;
            // Em gameplay, cursor escondido e travado
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        /// <summary>
        /// Bloqueia todo input e desabilita interações de UI.
        /// </summary>
        [Qubyte.Tracking.TrackableCall]
        public void SetBlockInputContext()
        {
            InputManager.Instance?.SetBlockInputContext();
            EnsureUiRefs();
            if (_uiModule != null)
                _uiModule.enabled = false;
            if (_eventSystem != null)
                _eventSystem.SetSelectedGameObject(null);
            _currentContext = InputContext.BlockInput;
            _uiInteractionsEnabled = false;
            // Em bloqueio, garanta cursor escondido e travado
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        /// <summary>
        /// Apenas habilita/desabilita interações de UI (sem trocar contextos de mapa).
        /// </summary>
        [Qubyte.Tracking.TrackableCall]
        public void EnableUiInteractions(GameObject defaultFocus = null)
        {
            EnsureUiRefs();
            _uiInteractionsEnabled = true;
            _defaultFocus = defaultFocus != null ? defaultFocus : _defaultFocus;
            if (_uiModule != null) _uiModule.enabled = true;
            GuaranteeUiFocus();
            // Ao habilitar UI, mantemos o cursor escondido até atividade de mouse
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        [Qubyte.Tracking.TrackableCall]
        public void DisableUiInteractions()
        {
            EnsureUiRefs();
            _uiInteractionsEnabled = false;
            if (_uiModule != null) _uiModule.enabled = false;
            if (_eventSystem != null) _eventSystem.SetSelectedGameObject(null);
            // Cursor escondido em UI desabilitada
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Update()
        {
            // Somente gerenciamos cursor/foco dinamicamente no contexto de UI com interações habilitadas
            if (_currentContext != InputContext.UI || !_uiInteractionsEnabled)
                return;

            TrackDeviceActivity();
            bool mouseActive = Time.unscaledTime - _lastMouseActivityTime < ActivityWindow;
            bool gamepadActive = Time.unscaledTime - _lastGamepadActivityTime < ActivityWindow;

            if (mouseActive && (!gamepadActive || _lastMouseActivityTime >= _lastGamepadActivityTime))
            {
                // Mouse domina: mostra cursor e foca elemento sob ponteiro
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                FocusUiUnderPointerOrDefault();
            }
            else if (gamepadActive)
            {
                // Gamepad domina: oculta cursor e garante um foco navegável
                // Para garantir ocultação também no Editor, travamos o cursor
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                GuaranteeUiFocus();
            }
            else
            {
                // Sem atividade recente: mantenha foco atual ou padrão
                GuaranteeUiFocus();
            }
        }

        private void TrackDeviceActivity()
        {
            var mouse = Mouse.current;
            if (mouse != null)
            {
                var delta = mouse.delta.ReadValue();
                if (delta.sqrMagnitude > 0.0001f || mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame || mouse.scroll.ReadValue().sqrMagnitude > 0.0001f)
                {
                    _lastMouseActivityTime = Time.unscaledTime;
                }
            }

            var gamepad = Gamepad.current;
            if (gamepad != null)
            {
                bool sticksMoved = gamepad.leftStick.ReadValue().sqrMagnitude > 0.0001f || gamepad.rightStick.ReadValue().sqrMagnitude > 0.0001f;
                bool dpadMoved = gamepad.dpad.ReadValue().sqrMagnitude > 0.0001f;
                bool triggersMoved = gamepad.leftTrigger.ReadValue() > 0.2f || gamepad.rightTrigger.ReadValue() > 0.2f;
                bool buttonsPressed =
                    gamepad.buttonSouth.wasPressedThisFrame ||
                    gamepad.buttonEast.wasPressedThisFrame ||
                    gamepad.buttonWest.wasPressedThisFrame ||
                    gamepad.buttonNorth.wasPressedThisFrame ||
                    gamepad.leftShoulder.wasPressedThisFrame ||
                    gamepad.rightShoulder.wasPressedThisFrame ||
                    gamepad.leftStickButton.wasPressedThisFrame ||
                    gamepad.rightStickButton.wasPressedThisFrame ||
                    gamepad.startButton.wasPressedThisFrame ||
                    gamepad.selectButton.wasPressedThisFrame;
                if (sticksMoved || dpadMoved || triggersMoved || buttonsPressed || gamepad.wasUpdatedThisFrame)
                {
                    _lastGamepadActivityTime = Time.unscaledTime;
                }
            }
        }

        private void GuaranteeUiFocus()
        {
            if (_eventSystem == null) return;
            var current = _eventSystem.currentSelectedGameObject;
            if (current == null)
            {
                // Tenta foco sob ponteiro; se não houver, usa padrão; se não houver, primeiro Selectable
                if (!FocusUiUnderPointerOrDefault())
                {
                    if (_defaultFocus != null)
                    {
                        _eventSystem.SetSelectedGameObject(_defaultFocus);
                        _lastSelected = _defaultFocus;
                    }
                    else
                    {
                        var anySelectable = FindFirstObjectByType<Selectable>();
                        if (anySelectable != null)
                        {
                            _eventSystem.SetSelectedGameObject(anySelectable.gameObject);
                            _lastSelected = anySelectable.gameObject;
                        }
                    }
                }
            }
            else
            {
                _lastSelected = current;
            }
        }

        private bool FocusUiUnderPointerOrDefault()
        {
            if (_eventSystem == null) return false;
            var mouse = Mouse.current;
            if (mouse == null) return false;
            var pos = mouse.position.ReadValue();
            var ped = new PointerEventData(_eventSystem) { position = pos };
            var results = new System.Collections.Generic.List<RaycastResult>();
            _eventSystem.RaycastAll(ped, results);
            for (int i = 0; i < results.Count; i++)
            {
                var go = results[i].gameObject;
                var selectable = go.GetComponent<Selectable>();
                if (selectable != null && selectable.IsInteractable())
                {
                    _eventSystem.SetSelectedGameObject(go);
                    _lastSelected = go;
                    return true;
                }
            }
            // Se não encontrou nada sob ponteiro, tenta padrão
            if (_defaultFocus != null)
            {
                _eventSystem.SetSelectedGameObject(_defaultFocus);
                _lastSelected = _defaultFocus;
                return true;
            }
            return false;
        }
    }
}