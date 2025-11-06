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
            yield return new WaitForSeconds(duration);
            _gamepad?.SetMotorSpeeds(0f, 0f); // Para a vibração
            _stopRumbleCoroutine = null;
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