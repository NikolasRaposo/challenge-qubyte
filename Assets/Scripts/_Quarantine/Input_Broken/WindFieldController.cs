using UnityEngine;
namespace Gameplay {
    /// <summary>
    /// Creates a spherical field of force that can attract or repel objects.
    /// It affects any object within its radius that has a WindReceiver component.
    /// </summary>
    public class WindFieldController : MonoBehaviour {
        /// <summary>
        /// Defines the behavior of the wind field.
        /// </summary>
        public enum Polarity { Attract, Repel }

        [Header("Field Settings")]
        [Tooltip("The current behavior of the field: attract or repel.")]
        public Polarity currentPolarity = Polarity.Attract;

        [Tooltip("The strength of the force applied by the field.")]
        public float force = 20f;

        [Tooltip("The radius of the field's influence.")]
        public float radius = 10f;

        [Tooltip("The physics layers that will be affected by this field.")]
        public LayerMask affectedLayers;

        [Header("Controls")]
        [Tooltip("The key used to switch the field's polarity.")]
        public KeyCode switchPolarityKey = KeyCode.Q;

        /// <summary>
        /// Called every frame to check for input and apply the wind force.
        /// </summary>
        private void Update() {
            // Check for player input to switch polarity.
            if (Input.GetKeyDown(switchPolarityKey)) {
                currentPolarity = (currentPolarity == Polarity.Attract) ? Polarity.Repel : Polarity.Attract;
            }
        }
    
        /// <summary>
        /// Called every fixed frame-rate frame. Ideal for physics calculations.
        /// </summary>
        private void FixedUpdate() {
            ApplyForce();
        }

        /// <summary>
        /// Finds all affected objects within the radius and applies the appropriate force.
        /// </summary>
        private void ApplyForce() {
            // Find all colliders within the sphere that match the affected layer mask.
            Collider[] targets = Physics.OverlapSphere(transform.position, radius, affectedLayers);

            foreach (Collider target in targets) {
                // Check if the target has a WindReceiver component.
                if (!target.TryGetComponent(out WindReceiver receiver)) continue;
                // Calculate the direction from the field's center to the target.
                Vector3 direction = (target.transform.position - transform.position).normalized;
                // If attracting, reverse the direction to pull the object inwards.
                if (currentPolarity == Polarity.Attract) {
                    direction *= -1;
                }

                // Tell the receiver to apply the calculated wind force.
                receiver.ApplyWind(direction * force);
            }
        }

        /// <summary>
        /// Draws gizmos in the editor to visualize the wind field's radius and polarity.
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            // Set color based on polarity for easy identification.
            Gizmos.color = (currentPolarity == Polarity.Attract) ? new Color(0,0,1,0.25f) : new Color(1,0,0,0.25f);
            Gizmos.DrawSphere(transform.position, radius);
        }
    }
}