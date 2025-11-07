using System;
using System.Collections;
using Enemies;
using Player;
using UnityEngine;
using UnityEngine.SceneManagement;
using Gameplay;

namespace Managers {
    /// <summary>
    /// Manages the overall game state, such as coin count, player status, and level progress.
    /// Implemented as a Singleton to be easily accessed by other scripts.
    /// </summary>
    public class GameManager : MonoBehaviour {
        public static GameManager Instance { get; private set; }

        public event Action<int> OnCoinsUpdated;
        public event Action<int> OnLivesUpdated;
        public event Action<int> OnEnemiesDefeatedUpdated;
        public event Action OnPlayerRespawn;
        
        [Header("Player Stats")]
        public int playerLives = 3;
        
        [Header("Game State")]
        [Tooltip("A reference to the player's GameObject.")]
        public GameObject player;
        
        [Header("UI")]
        [Tooltip("The UI panel that is shown when the level is completed.")]
        public GameObject levelCompletePanel;
        
        [Header("Boss Battle")]
        [Tooltip("A reference to the boss controller script in the scene.")]
        public BossContext bossContextController;
        
        private Vector3 _lastCheckpointPosition;
        private PlayerHealth _playerHealth;
        private bool _isPaused;
        private int _collectedCoins;
        private int _enemiesDefeated;

        // The Awake method is called before any Start methods.
        private void Awake() {
            if (Instance != null && Instance != this) { Destroy(gameObject); }
            else { Instance = this; }
            
            if (levelCompletePanel != null) {
                levelCompletePanel.SetActive(false);
            }
        }
        private void Start() {
            Time.timeScale = 1f;
            if (player != null) {
                _lastCheckpointPosition = player.transform.position;
                _playerHealth = player.GetComponent<PlayerHealth>();
            } 
            if (bossContextController != null) {
                bossContextController.OnBossDefeated += HandleBossContextDefeated;
            }
            InputManager.Instance.OnPause += TogglePause;
            
            OnCoinsUpdated?.Invoke(_collectedCoins);
            OnLivesUpdated?.Invoke(playerLives);
            OnEnemiesDefeatedUpdated?.Invoke(_enemiesDefeated);
        }
        public void TogglePause() {
            _isPaused = !_isPaused;
            Time.timeScale = _isPaused ? 0f : 1f;
            UIManager.Instance.TogglePauseMenu(_isPaused);
            if (_isPaused) {
                InputManager.Instance.SetUiContext();
                // Failsafe: garante que qualquer vibração seja interrompida ao pausar
                RumbleManager.Instance?.StopAllRumble();
            } else {
                InputManager.Instance.SetPlayerContext();
            }
        }
        /// <summary>
        /// Adds a specified amount of coins to the counter.
        /// </summary>
        /// <param name="amount">The number of coins to add.</param>
        public void AddCoin(int amount = 1) {
            _collectedCoins += amount;
            OnCoinsUpdated?.Invoke(_collectedCoins);
        }
        public void IncrementDefeatedEnemies() {
            _enemiesDefeated++;
            OnEnemiesDefeatedUpdated?.Invoke(_enemiesDefeated);
        }
        private void OnDestroy() {
            if (InputManager.Instance != null) {
                InputManager.Instance.OnPause -= TogglePause;
            }
            if (bossContextController != null) {
                bossContextController.OnBossDefeated -= HandleBossContextDefeated;
            }
            // Failsafe: ao destruir o GameManager (troca de cena, etc.), interrompe vibração
            RumbleManager.Instance?.StopAllRumble();
        }
        /// <summary>
        /// Updates the position where the player will respawn.
        /// </summary>
        /// <param name="newPosition">The position of the checkpoint.</param>
        public void UpdateCheckpoint(Vector3 newPosition) {
            _lastCheckpointPosition = newPosition;
            Debug.Log("Checkpoint updated to: " + newPosition);
        }

        /// <summary>
        /// Handles the player respawn logic.
        /// </summary>
        public void RespawnPlayer() {
            if (!player || !_playerHealth) return;
            
            playerLives--;
            OnLivesUpdated?.Invoke(playerLives); 
            if (bossContextController) {
                bossContextController.ResetBattle();
            }
            if (playerLives > 0) {
                StartCoroutine(RespawnSequence());
            } else {
                GameOver();
            }
        }
        /// <summary>
        /// Coroutine that manages the sequence of the player respawning.
        /// </summary>
        private IEnumerator RespawnSequence() {
            // Block player input while the countdown is running.
            InputManager.Instance.SetBlockInputContext();
            // Failsafe: interrompe qualquer vibração antes do countdown de respawn
            RumbleManager.Instance?.StopAllRumble();
            // Tell the UIManager to show the countdown and wait for it to finish.
            yield return UIManager.Instance.StartRespawnCountdown(3f); // 3-second countdown
            // --- Actual Respawn Logic ---
            Debug.Log("Respawning player at: " + _lastCheckpointPosition);
            CharacterController characterController = player.GetComponent<CharacterController>();
            if (characterController) characterController.enabled = false;
            player.transform.position = _lastCheckpointPosition;
            if (characterController) characterController.enabled = true;
            // Reset player's state (re-enable model, controls, etc.)
            _playerHealth.PrepareForRespawn();
            // Give control back to the player.
            InputManager.Instance.SetPlayerContext();
            OnPlayerRespawn?.Invoke();
        }
        /// <summary>
        /// Handles the game over state.
        /// </summary>
        private void GameOver() {
            Debug.Log("Game Over!");
            // Failsafe: garante silêncio haptico ao entrar em Game Over
            RumbleManager.Instance?.StopAllRumble();
            // Tell the UIManager to show the final screen.
            UIManager.Instance.ShowGameOverScreen();
            // Block all player input permanently.
            InputManager.Instance.SetBlockInputContext();
        }
        /// <summary>
        /// Handles the logic for when the level is successfully completed.
        /// </summary>
        public void CompleteLevel() {
            Debug.Log("Level Completed!");
            // Failsafe: garante silêncio haptico ao completar nível
            RumbleManager.Instance?.StopAllRumble();
            // Show the level complete UI panel
            if (levelCompletePanel != null) {
                levelCompletePanel.SetActive(true);
            }
            // Freeze the game
            Time.timeScale = 0f;
            InputManager.Instance.SetUiContext();
        }
        public void RestartLevel() {
            // Reloads the currently active scene.
            //SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        [Obsolete("Obsolete")]
        public void GoToMainMenu() {
            // Loads the main menu scene. Make sure you have a scene named "MainMenu"
            // or change the string to the correct name.
            //SceneManager.LoadScene(0);
            Debug.Log("Voltando para o menu");

            // Pausa o jogo
            Time.timeScale = 0f;
            
            InputManager.Instance.SetUiContext();

            // Mostra a UI de menu
            var startMenu = FindObjectOfType<StartMenuControlAnim>(true);
            if (startMenu != null) {
                startMenu.gameObject.SetActive(true);
                startMenu.AtivarMainMenuStart(); // animação do logo
                startMenu.AtivarMainMenuLateStart(); // animação dos painéis
            } else {
                Debug.LogWarning("Nenhum StartMenuControlAnim encontrado na cena!");
            }
        }
            
        public void QuitGame() 
        {
            // Failsafe: interrompe vibração antes de sair
            RumbleManager.Instance?.StopAllRumble();
            Application.Quit();
        }
        private void HandleBossContextDefeated() {
            CompleteLevel();
        }
    }
}
