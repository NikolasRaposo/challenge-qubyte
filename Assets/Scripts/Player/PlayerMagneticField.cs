using UnityEngine;
namespace Player {
    /// <summary>
    /// Adds a magnetic field to the player to automatically attract coins.
    /// This script should be attached to the same GameObject that contains the ThirdPersonController.
    /// </summary>
// This line ensures that the script requires a ThirdPersonController to be on the same GameObject.
// If you add this script to an object without the controller, Unity will add it automatically.
    [RequireComponent(typeof(ThirdPersonController))]
    public class PlayerMagneticField : MonoBehaviour {
        [Tooltip("The radius of the magnetic field around the player.")]
        public float magneticRadius = 3f;

        [Tooltip("The color of the gizmo that displays the magnetic field in the editor.")]
        public Color gizmoColor = new Color(0, 0.5f, 1f, 0.2f); // Semi-transparent blue

        [Tooltip("The vertical offset of the magnetic field relative to the player's position.")]
        public float verticalOffset = 0.5f;

        // A reference to the magnetic field's sphere collider.
        private SphereCollider _magneticFieldCollider;

        /// <summary>
        /// Initializes the magnetic field around the player.
        /// This method is called once when the script instance is being loaded.
        /// </summary>
        private void Start() {
            // Create a new child GameObject to host the magnetic field components.
            GameObject magneticFieldObject = new GameObject("MagneticField");
            magneticFieldObject.transform.SetParent(transform);
            magneticFieldObject.transform.localPosition = new Vector3(0, verticalOffset, 0);

            // Add a SphereCollider and configure it as a trigger.
            _magneticFieldCollider = magneticFieldObject.AddComponent<SphereCollider>();
            _magneticFieldCollider.isTrigger = true;
            _magneticFieldCollider.radius = magneticRadius;

            // Set the appropriate tag to interact with coins.
            magneticFieldObject.tag = "MagneticTrigger";

            // Add a Rigidbody to ensure that trigger events (OnTriggerEnter) are reliably detected.
            Rigidbody rb = magneticFieldObject.AddComponent<Rigidbody>();
            rb.isKinematic = true; // Prevents the field from being affected by physics forces.
            rb.useGravity = false; // Disables gravity for this object.

            Debug.Log("Magnetic field created successfully!");
        }

        /// <summary>
        /// Updates the size of the magnetic field if the radius is changed in the Inspector during runtime.
        /// This method is called every frame.
        /// </summary>
        private void Update() {
            // Check if the collider exists and if its radius differs from the public variable.
            if (_magneticFieldCollider && !Mathf.Approximately(_magneticFieldCollider.radius, magneticRadius)) {
                // Update the collider's radius to match the new value.
                _magneticFieldCollider.radius = magneticRadius;
            }
        }

        /// <summary>
        /// Draws gizmos in the editor to visualize the magnetic field.
        /// Gizmos are only drawn in the Scene view and are useful for debugging.
        /// </summary>
        private void OnDrawGizmos() {
            // Set the color for the gizmo.
            Gizmos.color = gizmoColor;
            // Draw a sphere that represents the magnetic field's radius and position.
            Gizmos.DrawSphere(transform.position + new Vector3(0, verticalOffset, 0), magneticRadius);
        }
    }
}