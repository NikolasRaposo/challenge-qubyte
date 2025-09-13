using UnityEngine;
namespace ExampleScripts {
    /// <summary>
    /// Example script to configure a custom attraction point for coins.
    /// Add this script to the player object alongside PlayerMagneticField.
    /// This script works in conjunction with PlayerMagneticField, avoiding the creation of duplicate triggers.
    /// </summary>
    public class PlayerCoinAttractor : MonoBehaviour {
        [Tooltip("The Transform that will be used as the attraction point for coins.")]
        public Transform attractionPoint;

        [Tooltip("The color of the gizmo that shows the connection to the attraction point in the editor.")]
        public Color gizmoColor = new Color(0, 0.5f, 1f, 0.3f); // Semi-transparent blue

        // Reference to the magnetic field created by PlayerMagneticField.
        private PlayerMagneticField _magneticFieldScript;

        /// <summary>
        /// Called when the script instance is being loaded.
        /// </summary>
        private void Awake() {
            // Find the PlayerMagneticField component on the same GameObject.
            _magneticFieldScript = GetComponent<PlayerMagneticField>();

            if (_magneticFieldScript == null) {
                Debug.LogWarning("PlayerCoinAttractor requires a PlayerMagneticField component on the same GameObject!");
            }

            // If no attraction point is assigned, create a default one.
            if (attractionPoint != null) return;
            GameObject attractionPointObj = new GameObject("CoinAttractionPoint");
            attractionPointObj.transform.SetParent(transform);
            attractionPointObj.transform.localPosition = new Vector3(0, 1.5f, 0); // Position it slightly above the player.
            attractionPoint = attractionPointObj.transform;
        }

        /// <summary>
        /// Called on the frame when a script is enabled before any of the Update methods are called the first time.
        /// </summary>
        private void Start() {
            // Find the trigger object created by PlayerMagneticField.
            GameObject magneticFieldObj = transform.Find("MagneticField")?.gameObject;

            if (magneticFieldObj != null) {
                // Add the MagneticTriggerHandler component to the magnetic field object.
                MagneticTriggerHandler handler = magneticFieldObj.GetComponent<MagneticTriggerHandler>();
                if (handler == null) {
                    handler = magneticFieldObj.AddComponent<MagneticTriggerHandler>();
                }

                // Configure the handler with our custom attraction point.
                handler.attractionPoint = this.attractionPoint;

                Debug.Log("PlayerCoinAttractor configured successfully using the existing magnetic field!");
            } else {
                Debug.LogError("Magnetic field not found! Ensure PlayerMagneticField is on the same GameObject.");
            }
        }

        /// <summary>
        /// Draws gizmos in the scene view for easier visualization.
        /// </summary>
        private void OnDrawGizmos() {
            // Draw the attraction point.
            if (attractionPoint == null) return;
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(attractionPoint.position, 0.1f);
            Gizmos.DrawLine(transform.position, attractionPoint.position);

            // Draw a dotted line to indicate the connection.
            Gizmos.color = gizmoColor;
            Vector3 direction = (attractionPoint.position - transform.position).normalized;
            for (float i = 0; i < Vector3.Distance(transform.position, attractionPoint.position); i += 0.2f) {
                Vector3 point = transform.position + direction * i;
                Gizmos.DrawSphere(point, 0.02f);
            }
        }
    }

    /// <summary>
    /// Helper component that manages the interactions of the magnetic trigger.
    /// This component is added automatically to the magnetic field GameObject.
    /// </summary>
    public class MagneticTriggerHandler : MonoBehaviour {
        // Hides this variable from the Inspector but keeps it public for other scripts to access.
        [HideInInspector]
        public Transform attractionPoint;

        /// <summary>
        /// Called when another Collider enters this trigger.
        /// </summary>
        /// <param name="other">The other Collider involved in this collision.</param>
        private void OnTriggerEnter(Collider other) {
            // Check if the object that entered the trigger has a CoinPickup component.
            CoinPickup coin = other.GetComponent<CoinPickup>();
            if (coin != null && attractionPoint != null)
            {
                // Set the custom attraction point for the coin.
                coin.SetCustomAttractionPoint(attractionPoint);
                Debug.Log("Custom attraction point set for: " + coin.gameObject.name);
            }
        }
    }
}