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

        // Eventos para o FlowRouter acionar micro-blocos de UI/fluxo
        public event Action OnLevelComplete;
        public event Action OnGameOver;
        public event Action<float> OnRespawnRequested; // duração do countdown

        public event Action<int> OnCoinsUpdated;
        public event Action<int> OnLivesUpdated;
        public event Action<int> OnEnemiesDefeatedUpdated;
        public event Action OnPlayerRespawn;
        public event Action OnPlayerDied;
        
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
        // Removido: estado de pausa será gerenciado pelo PauseInputListener
        private int _collectedCoins;
        private int _enemiesDefeated;
        // Removido: bloqueio e desabilitação de pausa agora pertencem ao PauseInputListener

        // The Awake method is called before any Start methods.
        private void Awake() {
            if (Instance != null && Instance != this) { Destroy(gameObject); }
            else { Instance = this; }
            // Persiste o GameManager entre cenas para manter contadores e eventos.
            DontDestroyOnLoad(gameObject);
            
            if (levelCompletePanel != null) {
                levelCompletePanel.SetActive(false);
            }
        }
        private void Start() {
            if (player != null) {
                _lastCheckpointPosition = player.transform.position;
                _playerHealth = player.GetComponent<PlayerHealth>();
                Debug.Log($"[GameManager] Start. Player at {player.transform.position}. Checkpoint set to {_lastCheckpointPosition}");
            } 
            if (bossContextController != null) {
                bossContextController.OnBossDefeated += HandleBossContextDefeated;
            }
            // Removido: assinatura de pausa movida para PauseInputListener
            
            OnCoinsUpdated?.Invoke(_collectedCoins);
            OnLivesUpdated?.Invoke(playerLives);
            OnEnemiesDefeatedUpdated?.Invoke(_enemiesDefeated);
        }
        // Removido: SetStartMenuActive e TogglePause; pausa é responsabilidade do PauseInputListener
        public void NotifyPlayerDied()
        {
            OnPlayerDied?.Invoke();
        }
        /// <summary>
        /// Adds a specified amount of coins to the counter.
        /// </summary>
        /// <param name="amount">The number of coins to add.</param>
        public void AddCoin(int amount = 1) {
            _collectedCoins += amount;
            OnCoinsUpdated?.Invoke(_collectedCoins);
        }

        /// <summary>
        /// Adiciona vidas ao jogador e notifica a UI.
        /// </summary>
        /// <param name="amount">Quantidade de vidas a adicionar.</param>
        public void AddLife(int amount = 1)
        {
            playerLives += amount;
            OnLivesUpdated?.Invoke(playerLives);
        }
        public void IncrementDefeatedEnemies() {
            _enemiesDefeated++;
            OnEnemiesDefeatedUpdated?.Invoke(_enemiesDefeated);
        }

        // Propriedades públicas somente leitura para que outros sistemas possam ler estado atual
        public int CollectedCoins => _collectedCoins;
        public int Lives => playerLives;
        public int EnemiesDefeated => _enemiesDefeated;
        private void OnDestroy() {
            // Removido: unbind de pausa movido ao PauseInputListener
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
            // Gating de input durante countdown deve ser responsabilidade do bloco de respawn
            // Failsafe: interrompe qualquer vibração antes do countdown de respawn
            RumbleManager.Instance?.StopAllRumble();
            // Emite evento para o FlowRouter acionar um micro-bloco de respawn/countdown
            OnRespawnRequested?.Invoke(3f);
            // Aguarda o mesmo período usando tempo não escalado
            yield return new WaitForSecondsRealtime(3f);
            // --- Actual Respawn Logic ---
            Debug.Log("Respawning player at: " + _lastCheckpointPosition);
            CharacterController characterController = player.GetComponent<CharacterController>();
            if (characterController) characterController.enabled = false;
            player.transform.position = _lastCheckpointPosition;
            if (characterController) characterController.enabled = true;
            // Reset player's state (re-enable model, controls, etc.)
            _playerHealth.PrepareForRespawn();
            // Restauração de contexto de input será responsabilidade do fluxo/bloco
            OnPlayerRespawn?.Invoke();
        }
        /// <summary>
        /// Handles the game over state.
        /// </summary>
        private void GameOver() {
            Debug.Log("Game Over!");
            // Failsafe: garante silêncio haptico ao entrar em Game Over
            RumbleManager.Instance?.StopAllRumble();
            // Emite evento para o FlowRouter acionar micro-bloco de Game Over
            OnGameOver?.Invoke();
            // Controle de input será gerenciado integralmente pelo bloco de Game Over
        }
        /// <summary>
        /// Handles the logic for when the level is successfully completed.
        /// </summary>
        public void CompleteLevel() {
            Debug.Log("Level Completed!");
            // Failsafe: garante silêncio haptico ao completar nível
            RumbleManager.Instance?.StopAllRumble();
            // Emite evento para o FlowRouter acionar micro-bloco de conclusão de nível
            OnLevelComplete?.Invoke();
            // O bloco deve decidir congelar o jogo e alternar contexto de input
        }
        public void RestartLevel() {
            // Reloads the currently active scene.
            //SceneManager.LoadScene(SceneManager.GetActiveScene().name);
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

        // --- API pública para sinais da timeline/cinemática ---
        /// <summary>
        /// Reativa gameplay e exibe a HUD ao finalizar a cinemática.
        /// Método sem parâmetros para ser chamado por SignalReceiver na timeline.
        /// </summary>
        public void ActivateHUDFromCinematicEnd()
        {
        }
    }
}
