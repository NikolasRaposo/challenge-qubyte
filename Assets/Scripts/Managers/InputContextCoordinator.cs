using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

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
                _eventSystem = FindObjectOfType<EventSystem>();
            if (_uiModule == null)
            {
                if (_eventSystem != null)
                    _uiModule = _eventSystem.GetComponent<InputSystemUIInputModule>();
                if (_uiModule == null)
                    _uiModule = FindObjectOfType<InputSystemUIInputModule>();
            }
        }

        /// <summary>
        /// Entra em contexto de UI. Por padrão, controla interações do módulo de UI.
        /// </summary>
        public void SetUiContext(bool enableUiInteractions = true, GameObject defaultFocus = null)
        {
            InputManager.Instance?.SetUiContext();
            EnsureUiRefs();
            if (_uiModule != null)
                _uiModule.enabled = enableUiInteractions;
            if (enableUiInteractions && defaultFocus != null && _eventSystem != null)
            {
                _eventSystem.SetSelectedGameObject(defaultFocus);
            }
        }

        /// <summary>
        /// Entra em contexto de gameplay e desabilita interações de UI.
        /// </summary>
        public void SetPlayerContext()
        {
            InputManager.Instance?.SetPlayerContext();
            EnsureUiRefs();
            if (_uiModule != null)
                _uiModule.enabled = false;
            if (_eventSystem != null)
                _eventSystem.SetSelectedGameObject(null);
        }

        /// <summary>
        /// Bloqueia todo input e desabilita interações de UI.
        /// </summary>
        public void SetBlockInputContext()
        {
            InputManager.Instance?.SetBlockInputContext();
            EnsureUiRefs();
            if (_uiModule != null)
                _uiModule.enabled = false;
            if (_eventSystem != null)
                _eventSystem.SetSelectedGameObject(null);
        }

        /// <summary>
        /// Apenas habilita/desabilita interações de UI (sem trocar contextos de mapa).
        /// </summary>
        public void EnableUiInteractions(GameObject defaultFocus = null)
        {
            EnsureUiRefs();
            if (_uiModule != null)
                _uiModule.enabled = true;
            if (defaultFocus != null && _eventSystem != null)
                _eventSystem.SetSelectedGameObject(defaultFocus);
        }

        public void DisableUiInteractions()
        {
            EnsureUiRefs();
            if (_uiModule != null)
                _uiModule.enabled = false;
            if (_eventSystem != null)
                _eventSystem.SetSelectedGameObject(null);
        }
    }
}