using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
// Importante!

namespace Gameplay
{
    /// <summary>
    /// Gerenciador Singleton para controlar a vibração (rumble) do gamepad.
    /// Fornece presets fáceis de chamar de qualquer outro script.
    /// </summary>
    public class RumbleManager : MonoBehaviour {
        public static RumbleManager Instance { get; private set; }

        [Header("Configurações de Vibração")]
        [SerializeField] private bool _enableRumble = true;

        [Header("Regras de Dispositivo")]
        [SerializeField] private bool _onlyRumbleOnGamepad = true;
        [SerializeField] private PlayerInput _playerInput; // Opcional: arraste no Inspector. Se nulo, será buscado.

        // Presets de Vibração (ajuste no Inspector)
        [SerializeField] private RumbleSettings _stompPreset = new RumbleSettings(0.2f, 0.2f, 0.1f);
        [SerializeField] private RumbleSettings _reflectPreset = new RumbleSettings(0.1f, 0.4f, 0.2f);
        [SerializeField] private RumbleSettings _counterPreset = new RumbleSettings(0.5f, 0.5f, 0.25f);
        [SerializeField] private RumbleSettings _bossHitPreset = new RumbleSettings(0.8f, 0.8f, 0.3f);
        [SerializeField] private RumbleSettings _playerDeathPreset = new RumbleSettings(1.0f, 1.0f, 0.5f);

        private Coroutine _stopRumbleCoroutine;
        private Gamepad _gamepad;

        private void Awake() {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            _gamepad = Gamepad.current;
        }

        private void OnEnable()
        {
            var pi = GetOrFindPlayerInput();
            if (pi != null)
            {
                pi.onControlsChanged += HandleControlsChanged;
            }
        }

        private void OnDisable()
        {
            // Garante que motores sejam desligados ao desativar
            ForceStopRumble();
            if (_playerInput != null)
            {
                _playerInput.onControlsChanged -= HandleControlsChanged;
            }
        }

        private void OnDestroy()
        {
            // Garante que motores sejam desligados ao destruir
            ForceStopRumble();
            if (_playerInput != null)
            {
                _playerInput.onControlsChanged -= HandleControlsChanged;
            }
        }

        private void OnApplicationQuit()
        {
            // Em alguns ambientes, sair do Play Mode não envia comandos de haptics;
            // garantimos aqui que o motor pare.
            ForceStopRumble();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            // Ao perder o foco da janela, interrompe vibração como failsafe
            if (!hasFocus)
                ForceStopRumble();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            // Ao pausar a aplicação (minimizar/ocultar), interrompe vibração como failsafe
            if (pauseStatus)
                ForceStopRumble();
        }

        /// <summary>
        /// Método privado que inicia a vibração e agenda sua parada.
        /// </summary>
        private void StartRumble(float lowFreq, float highFreq, float duration)
        {
            // Se a vibração está desligada ou não há gamepad, não faz nada
            if (!_enableRumble) return;
        
            // Tenta pegar o gamepad se ele foi conectado agora
            if (_gamepad == null) _gamepad = Gamepad.current;
            if (_gamepad == null) return; // Ainda sem gamepad

            // Gate por esquema de controle: só vibra quando ativo for Gamepad
            if (_onlyRumbleOnGamepad)
            {
                var pi = GetOrFindPlayerInput();
                // Se não há PlayerInput, assume que não deve vibrar para evitar incômodo quando jogando no teclado
                if (pi == null) return;
                var scheme = pi.currentControlScheme;
                if (string.IsNullOrEmpty(scheme) || !string.Equals(scheme, "Gamepad", StringComparison.OrdinalIgnoreCase))
                    return;
            }

            // Para qualquer coroutine de "parada" anterior
            if (_stopRumbleCoroutine != null)
            {
                StopCoroutine(_stopRumbleCoroutine);
            }

            // Inicia a nova vibração
            _gamepad.SetMotorSpeeds(lowFreq, highFreq);

            // Agenda a parada
            _stopRumbleCoroutine = StartCoroutine(StopRumbleAfterDelay(duration));
        }

        private IEnumerator StopRumbleAfterDelay(float duration)
        {
            // Usa tempo real para garantir que a vibração pare mesmo com Time.timeScale = 0
            yield return new WaitForSecondsRealtime(duration);
            _gamepad?.SetMotorSpeeds(0f, 0f); // Para a vibração
            _stopRumbleCoroutine = null;
        }

        /// <summary>
        /// Interrompe imediatamente qualquer vibração ativa e cancela a parada agendada.
        /// Seguro para chamar múltiplas vezes.
        /// </summary>
        private void ForceStopRumble()
        {
            // Cancela a corrotina agendada, se houver
            if (_stopRumbleCoroutine != null)
            {
                try { StopCoroutine(_stopRumbleCoroutine); }
                catch { /* Ignora se já estiver parada */ }
                _stopRumbleCoroutine = null;
            }

            // Zera motores do gamepad atual
            // Usa Gamepad.current como fallback caso referência antiga tenha sido invalidada
            var current = Gamepad.current;
            if (_gamepad != null)
            {
                _gamepad.SetMotorSpeeds(0f, 0f);
            }
            else if (current != null)
            {
                current.SetMotorSpeeds(0f, 0f);
            }
        }

        private void HandleControlsChanged(PlayerInput pi)
        {
            if (!_onlyRumbleOnGamepad) return;
            var scheme = pi.currentControlScheme;
            if (string.IsNullOrEmpty(scheme) || !string.Equals(scheme, "Gamepad", StringComparison.OrdinalIgnoreCase))
            {
                // Se trocar para teclado/mouse, garante que qualquer vibração seja interrompida
                ForceStopRumble();
            }
        }

        private PlayerInput GetOrFindPlayerInput()
        {
            if (_playerInput != null) return _playerInput;
            // Tenta encontrar um PlayerInput na cena
            _playerInput = FindObjectOfType<PlayerInput>();
            return _playerInput;
        }

        /// <summary>
        /// API pública para interromper vibração manualmente de outros scripts.
        /// </summary>
        public void StopAllRumble()
        {
            ForceStopRumble();
        }

        // --- MÉTODOS PÚBLICOS (Presets) ---
        // Estes são os métodos que outros scripts irão chamar
    
        public void PlayStompRumble() => StartRumble(_stompPreset.Low, _stompPreset.High, _stompPreset.Duration);
        public void PlayReflectRumble() => StartRumble(_reflectPreset.Low, _reflectPreset.High, _reflectPreset.Duration);
        public void PlayCounterRumble() => StartRumble(_counterPreset.Low, _counterPreset.High, _counterPreset.Duration);
        public void PlayBossHitRumble() => StartRumble(_bossHitPreset.Low, _bossHitPreset.High, _bossHitPreset.Duration);
        public void PlayPlayerDeathRumble() => StartRumble(_playerDeathPreset.Low, _playerDeathPreset.High, _playerDeathPreset.Duration);

        /// <summary>
        /// Classe auxiliar para organizar os presets no Inspector
        /// </summary>
        [System.Serializable]
        private class RumbleSettings
        {
            public float Low;
            public float High;
            public float Duration;

            public RumbleSettings(float low, float high, float duration)
            {
                Low = low; High = high; Duration = duration;
            }
        }
    }
}