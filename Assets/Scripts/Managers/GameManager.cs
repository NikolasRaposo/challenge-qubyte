using Player;
using UnityEngine;
namespace Managers {
    /// <summary>
    /// Manages the overall game state, such as coin count, player status, and level progress.
    /// Implemented as a Singleton to be easily accessed by other scripts.
    /// </summary>
    public class GameManager : MonoBehaviour {
        public static GameManager Instance { get; private set; }

        [Header("Player Stats")]
        [Tooltip("The amount of coins the player has collected in the current level.")]
        public int collectedCoins;
        [Header("Game State")]
        [Tooltip("A reference to the player's GameObject.")]
        public GameObject player;
        [Header("UI")]
        [Tooltip("The UI panel that is shown when the level is completed.")]
        public GameObject levelCompletePanel;
        
        private Vector3 _lastCheckpointPosition;
        private PlayerHealth _playerHealth;

        // The Awake method is called before any Start methods.
        private void Awake() {
            // Check if an instance of the GameManager already exists in the scene.
            if (Instance != null && Instance != this) {
                // If so, and it's not this one, destroy this object to ensure there is only one.
                Destroy(gameObject);
            } else {
                // If no instance exists, this one becomes the instance.
                Instance = this;
                // (Optional) Prevents the GameManager from being destroyed when loading a new scene.
                // Useful if you want to keep the coin count between levels.
                // DontDestroyOnLoad(gameObject); 
            }
            if (levelCompletePanel != null) {
                levelCompletePanel.SetActive(false);
            }
        }
        private void Start() {
            if (player != null) {
                _lastCheckpointPosition = player.transform.position;
                _playerHealth = player.GetComponent<PlayerHealth>();
            } else {
                Debug.LogError("Player reference is not set in the GameManager!");
            }
        }

        /// <summary>
        /// Adds a specified amount of coins to the counter.
        /// </summary>
        /// <param name="amount">The number of coins to add.</param>
        public void AddCoin(int amount = 1) {
            collectedCoins += amount;
            Debug.Log("Coins: " + collectedCoins);
        
            // In the future, you can invoke an event here to update the UI.
            // E.g., OnCoinCountChanged?.Invoke(collectedCoins);
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
            Debug.Log("Respawning player at: " + _lastCheckpointPosition);
            CharacterController characterController = player.GetComponent<CharacterController>();
            if (characterController) characterController.enabled = false;
            player.transform.position = _lastCheckpointPosition;
            if (characterController) characterController.enabled = true;
            // Reset player's state (re-enable model, controls, etc.)
            _playerHealth.PrepareForRespawn();
        }
        /// <summary>
        /// Handles the logic for when the level is successfully completed.
        /// </summary>
        public void CompleteLevel() {
            Debug.Log("Level Completed!");

            // Show the level complete UI panel
            if (levelCompletePanel != null) {
                levelCompletePanel.SetActive(true);
            }

            // Freeze the game
            Time.timeScale = 0f;

            // You might want to disable player input here as well
            if(player != null) {
                // This assumes you are using the new Input System with PlayerInput component
                player.GetComponent<UnityEngine.InputSystem.PlayerInput>().enabled = false;
            }
        }
    }
}
