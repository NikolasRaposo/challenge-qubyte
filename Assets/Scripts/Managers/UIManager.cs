using System.Collections;
using Player.Powers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace Managers {
    /// <summary>
    /// Manages all UI elements in the game. Listens to events from the GameManager
    /// and other systems to update the display accordingly.
    /// </summary>
    public class UIManager : MonoBehaviour {
        public static UIManager Instance { get; private set; }

        [Header("UI Panels")]
        [SerializeField] private GameObject hudPanel;
        [SerializeField] private GameObject pauseMenuPanel;
        [SerializeField] private GameObject respawnPanel;
        [SerializeField] private GameObject gameOverPanel;

        [Header("HUD Elements")]
        [SerializeField] private TextMeshProUGUI coinsText;
        [SerializeField] private TextMeshProUGUI livesText;
        [SerializeField] private Image tornadoCooldownImage;

        [Header("Pause Menu Elements")]
        [SerializeField] private TextMeshProUGUI pauseCoinsText;
        [SerializeField] private TextMeshProUGUI pauseEnemiesDefeatedText;

        [Header("Respawn Elements")]
        [SerializeField] private TextMeshProUGUI respawnCountdownText;
        
        private TornadoAttack _playerTornadoAttack;

        private void Awake() {
            if (Instance != null && Instance != this) {
                Destroy(gameObject);
            } else {
                Instance = this;
            }
        }

        private void Start() {
            // Find the player's tornado script.
            // This assumes the player has a "Player" tag.
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) {
                _playerTornadoAttack = player.GetComponent<TornadoAttack>();
            }
            // Subscribe to events from the GameManager
            GameManager.Instance.OnCoinsUpdated += UpdateCoinText;
            GameManager.Instance.OnLivesUpdated += UpdateLivesText;
            GameManager.Instance.OnEnemiesDefeatedUpdated += UpdateEnemiesDefeatedText;

            // Ensure the initial state of the UI is correct
            pauseMenuPanel.SetActive(false);
            respawnPanel.SetActive(false);
            gameOverPanel.SetActive(false);
            hudPanel.SetActive(true);
        }

        private void Update() {
            // Continuously update the tornado cooldown UI
            if (_playerTornadoAttack) {
                tornadoCooldownImage.fillAmount = _playerTornadoAttack.GetCooldownProgress();
            }
        }

        /// <summary>
        /// Event handler for when the coin count changes.
        /// </summary>
        private void UpdateCoinText(int newCoinAmount) {
            coinsText.text = $"{newCoinAmount}";
            pauseCoinsText.text = $"{newCoinAmount}";
        }
    
        /// <summary>
        /// Event handler for when the player's lives change.
        /// </summary>
        private void UpdateLivesText(int newLivesAmount) {
            livesText.text = $"{newLivesAmount}";
        }
        /// <summary>
        /// Event handler for when enemies are defeated count change.
        /// </summary>
        private void UpdateEnemiesDefeatedText(int enemiesDefeatedAmount) {
            pauseEnemiesDefeatedText.text = $"{enemiesDefeatedAmount}";
        }

        public void TogglePauseMenu(bool isPaused) {
            pauseMenuPanel.SetActive(isPaused);
            hudPanel.SetActive(!isPaused);
        }
        /// <summary>
        /// Shows the Game Over screen.
        /// </summary>
        public void ShowGameOverScreen() {
            hudPanel.SetActive(false);
            gameOverPanel.SetActive(true);
        }
        /// <summary>
        /// Starts the respawn countdown visual sequence.
        /// </summary>
        /// <param name="countdownDuration">How long the countdown should last.</param>
        public IEnumerator StartRespawnCountdown(float countdownDuration) {
            hudPanel.SetActive(false);
            respawnPanel.SetActive(true);

            float timer = countdownDuration;
            while (timer > 0) {
                respawnCountdownText.text = $"Reaparecendo em... {Mathf.CeilToInt(timer)}";
                timer -= Time.unscaledDeltaTime; // Use unscaled time to work even when game is paused
                yield return null;
            }

            respawnPanel.SetActive(false);
            hudPanel.SetActive(true);
        }
        
        private void OnDestroy() {
            // Unsubscribe from events to prevent memory leaks
            if (GameManager.Instance == null)
                return;
            GameManager.Instance.OnCoinsUpdated -= UpdateCoinText;
            GameManager.Instance.OnLivesUpdated -= UpdateLivesText;
            GameManager.Instance.OnEnemiesDefeatedUpdated -= UpdateEnemiesDefeatedText;
        }
    }
}