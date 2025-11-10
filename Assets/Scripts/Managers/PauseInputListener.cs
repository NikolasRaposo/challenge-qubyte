using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Managers
{
    // Centraliza o controle de pausa baseado em input.
    // Responsabilidades:
    // - Ouvir a ação de Pause do Input System
    // - Alternar UI de pausa via UIManager
    // - Trocar contexto de input via InputManager
    // Não gerencia gameplay; apenas UI/contexto.
    public class PauseInputListener : MonoBehaviour
    {
        [Header("Fonte de Input")]
        [Tooltip("Roteador central de input (comportamento 'Invoke C# Events').")]
        public PlayerInputRouter inputRouter;

        [Header("Configuração")]
        [Tooltip("Se true, ignora tentativas de pausar.")]
        public bool disablePause = false;

        [Tooltip("Cooldown para evitar múltiplos toggles acidentais (ms).")]
        public int toggleCooldownMs = 300;

        private bool _isPaused;
        private long _lockUntilUnixMs;

        private UIManager _ui => UIManager.Instance;
        private InputManager _input => InputManager.Instance;

        private void OnEnable()
        {
            // Auto-binding: primeiro no mesmo GameObject; depois fallback global
            if (inputRouter == null)
                inputRouter = GetComponent<PlayerInputRouter>() ?? FindObjectOfType<PlayerInputRouter>();

            if (inputRouter != null)
            {
                inputRouter.OnPause += HandlePauseEvent;
            }
        }

        private void OnDisable()
        {
            if (inputRouter != null)
            {
                inputRouter.OnPause -= HandlePauseEvent;
            }
        }

        private void HandlePauseEvent()
        {
            if (disablePause) return;

            var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (nowMs < _lockUntilUnixMs) return;

            _lockUntilUnixMs = nowMs + toggleCooldownMs;
            SetPaused(!_isPaused);
        }

        public void SetPaused(bool paused)
        {
            _isPaused = paused;

            if (_ui != null)
            {
                _ui.TogglePauseMenu(_isPaused);
            }

            if (_input != null)
            {
                if (_isPaused)
                {
                    _input.SetUiContext();
                }
                else
                {
                    _input.SetGameplayContext();
                }
            }
        }

        public void EnablePause() => disablePause = false;
        public void DisablePause() => disablePause = true;
    }
}