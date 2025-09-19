using System.Collections;
using Player.Powers;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UI.HUD;

namespace Managers {
    /// <summary>
    /// Manages all UI elements in the game. Listens to events from the GameManager
    /// and other systems to update the display accordingly.
    /// </summary>
    public class UIManager : MonoBehaviour {
        public static UIManager Instance { get; private set; }

        [Header("UI Panels")]
        [SerializeField] private GameObject coinsPanel;
        [SerializeField] private GameObject livesPanel;
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
        
        [Header("HUD Settings")]
        [SerializeField] private float hudVisibleTime = 4f;
        [SerializeField] private PopUIAnimation  coinsPopAnimation;
        [SerializeField] private PopUIAnimation  livesPopAnimation;
        
        private TornadoAttack _playerTornadoAttack;
        private Coroutine _coinsCoroutine;
        private Coroutine _livesCoroutine;

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
            
            ShowPanel(coinsPanel);
            ShowPanel(livesPanel);
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
            ShowPanel(coinsPanel);

            if (coinsPopAnimation != null)
                coinsPopAnimation.PlayPop();
        }
    
        /// <summary>
        /// Event handler for when the player's lives change.
        /// </summary>
        private void UpdateLivesText(int newLivesAmount) {
            livesText.text = $"{newLivesAmount}";
            ShowPanel(livesPanel);
            
            if (livesPopAnimation != null)
                livesPopAnimation.PlayPop();
        }
        private void ShowPanel(GameObject panel)
        {
            bool isCoins = (panel == coinsPanel);
            bool isLives = (panel == livesPanel);

            // Reinicia a coroutine de esconder
            if (isCoins)
            {
                if (_coinsCoroutine != null) StopCoroutine(_coinsCoroutine);
                _coinsCoroutine = StartCoroutine(HideAfterDelay(panel));
            }
            else if (isLives)
            {
                if (_livesCoroutine != null) StopCoroutine(_livesCoroutine);
                _livesCoroutine = StartCoroutine(HideAfterDelay(panel));
            }

            var anim = panel.GetComponent<HideUIAnimation>();

            if (!panel.activeSelf) // só toca animação de entrada se estiver desativado
            {
                if (anim != null)
                    anim.PlayShow();
                else
                    panel.SetActive(true);
            }
            else
            {
                // já está ativo → não faz a animação de descida, apenas garante que fica visível
                panel.SetActive(true);
            }
        }
        private IEnumerator HideAfterDelay(GameObject panel) {
            yield return new WaitForSeconds(hudVisibleTime);
            
            var anim = panel.GetComponent<HideUIAnimation>();
            if (anim != null)
            {
                anim.PlayHide();
            }
            else
            {
                panel.SetActive(false); // fallback se não tiver animação
            }
        }
        /// <summary>
        /// Event handler for when enemies are defeated count change.
        /// </summary>
        private void UpdateEnemiesDefeatedText(int enemiesDefeatedAmount) {
            pauseEnemiesDefeatedText.text = $"{enemiesDefeatedAmount}";
        }

        public void TogglePauseMenu(bool isPaused) {
            pauseMenuPanel.SetActive(isPaused);
            coinsPanel.SetActive(!isPaused);
            livesPanel.SetActive(!isPaused);
        }
        /// <summary>
        /// Shows the Game Over screen.
        /// </summary>
        public void ShowGameOverScreen() {
            coinsPanel.SetActive(false);
            livesPanel.SetActive(false);
            gameOverPanel.SetActive(true);
        }
        /// <summary>
        /// Starts the respawn countdown visual sequence.
        /// </summary>
        /// <param name="countdownDuration">How long the countdown should last.</param>
        public IEnumerator StartRespawnCountdown(float countdownDuration) {
            coinsPanel.SetActive(false);
            livesPanel.SetActive(false);
            respawnPanel.SetActive(true);

            float timer = countdownDuration;
            while (timer > 0) {
                respawnCountdownText.text = $"Reaparecendo em... {Mathf.CeilToInt(timer)}";
                timer -= Time.unscaledDeltaTime; // Use unscaled time to work even when game is paused
                yield return null;
            }

            respawnPanel.SetActive(false);
            coinsPanel.SetActive(true);
            livesPanel.SetActive(true);
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