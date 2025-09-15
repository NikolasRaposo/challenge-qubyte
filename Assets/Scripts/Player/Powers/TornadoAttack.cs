using Managers;
using UnityEngine;

namespace Player.Powers {
    /// <summary>
    /// Manages the player's ability to launch a tornado. It listens for input
    /// and handles the spawning and cooldown of the tornado attack.
    /// </summary>
    public class TornadoAttack : MonoBehaviour {
        [Tooltip("The tornado GameObject to be spawned.")]
        public GameObject tornadoPrefab;
        [Tooltip("The point from which the tornado will be spawned.")]
        public Transform spawnPoint;
        [Tooltip("The minimum time in seconds between tornado launches.")]
        public float cooldown = 2f;
        
        // Tracks the time when the last tornado was launched to manage the cooldown.
        private float _lastTornadoTime = -Mathf.Infinity;

        /// <summary>
        /// Called on start to subscribe to the input event.
        /// </summary>
        private void Start() {
            // Subscribe to the OnTornado event from the InputManager singleton.
            // The 'HandleTornadoInput' method will be called whenever the event is triggered.
            InputManager.Instance.OnTornado += HandleTornadoInput;
        }

        /// <summary>
        /// Event handler that is called when the tornado input is detected.
        /// </summary>
        private void HandleTornadoInput() {
            // Check if enough time has passed since the last launch.
            if (!(Time.time >= _lastTornadoTime + cooldown)) return;
            // If the cooldown is over, launch a new tornado.
            LaunchTornado();
            // Record the time of this launch.
            _lastTornadoTime = Time.time;
        }

        /// <summary>
        /// Instantiates the tornado prefab at the designated spawn point.
        /// </summary>
        private void LaunchTornado() {
            // A safety check to ensure the prefab and spawn point are assigned.
            if (tornadoPrefab == null || spawnPoint == null) {
                Debug.LogError("Tornado Prefab or Spawn Point is not assigned in the inspector!");
                return;
            }
            // Create a new instance of the tornado.
            Instantiate(tornadoPrefab, spawnPoint.position, transform.rotation);
        }
        /// <summary>
        /// Returns the cooldown progress as a value between 0 (ready) and 1 (just used).
        /// </summary>
        public float GetCooldownProgress()
        {
            float timeSinceLast = Time.time - _lastTornadoTime;
            if (timeSinceLast >= cooldown)
            {
                return 1f;
            }
            return timeSinceLast / cooldown;
        }
        
        /// <summary>
        /// Unsubscribe from the event when this object is destroyed to prevent memory leaks.
        /// </summary>
        private void OnDestroy() {
            if (InputManager.Instance != null) {
                InputManager.Instance.OnTornado -= HandleTornadoInput;
            }
        }
    }
}