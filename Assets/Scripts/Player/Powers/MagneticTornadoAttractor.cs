using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Gameplay {
    /// <summary>
    /// Controls a "magnetic tornado" that can either attract or repel objects with a specific tag.
    /// Attracted objects will orbit the tornado and can be launched forward.
    /// </summary>
    public class MagneticTornadoController : MonoBehaviour {
        /// <summary>
        /// Defines the two possible modes of the tornado.
        /// </summary>
        public enum Polarity { Attract, Repel }
    
        [Header("Tornado Settings")]
        [Tooltip("The current behavior of the tornado.")]
        public Polarity currentPolarity = Polarity.Attract;
        [Tooltip("The outer radius where objects begin to orbit.")]
        public float outerRadius = 5f;
        [Tooltip("The inner radius where objects orbit tightly.")]
        public float innerRadius = 0.5f;
        [Tooltip("The speed at which objects orbit the tornado (degrees per second).")]
        public float angularSpeed = 360f;
        [Tooltip("The force used to launch objects when repelling or performing the final action.")]
        public float launchForce = 15f;

        [Header("Launch Action Settings")]
        [Tooltip("The minimum angle (left) for the launch cone.")]
        [SerializeField] private float minLaunchAngle = -30f; // Left
        [Tooltip("The maximum angle (right) for the launch cone.")]
        [SerializeField] private float maxLaunchAngle = 30f;   // Right

        [Header("Controls")]
        [Tooltip("The key used to switch the tornado's polarity.")]
        public KeyCode switchPolarityKey = KeyCode.Q;
        [Tooltip("The key used to activate the launch action for orbiting objects.")]
        public KeyCode activateFinalActionKey = KeyCode.E;

        // --- Private State Variables ---
        private bool _finalActionActivated;
        private readonly Dictionary<Rigidbody, Coroutine> _orbitingObjects = new Dictionary<Rigidbody, Coroutine>();
        private readonly HashSet<Rigidbody> _objectsInsideTornado = new HashSet<Rigidbody>();

        /// <summary>
        /// Called every frame to check for player input.
        /// </summary>
        private void Update() {
            // Check for polarity switch input.
            if (Input.GetKeyDown(switchPolarityKey)) {
                currentPolarity = (currentPolarity == Polarity.Attract) ? Polarity.Repel : Polarity.Attract;
                // When switching to Repel, we may want to immediately push objects away. This is handled in OnTriggerEnter.
            }
            // Check for the final action input.
            if (Input.GetKeyDown(activateFinalActionKey)) {
                _finalActionActivated = true;
            }
        }

        /// <summary>
        /// Called after Update. Resets the final action flag.
        /// </summary>
        private void LateUpdate() {
            // Reset the flag after one frame so the action only triggers once per key press.
            _finalActionActivated = false;
        }

        /// <summary>
        /// Called when a collider enters the tornado's trigger area.
        /// </summary>
        private void OnTriggerEnter(Collider other) {
            Rigidbody rb = other.attachedRigidbody;
            // Check if the object has a Rigidbody, the correct tag, and isn't already being affected.
            if (rb == null || !other.CompareTag("Magnetic") || _orbitingObjects.ContainsKey(rb)) return;
            _objectsInsideTornado.Add(rb);

            if (currentPolarity == Polarity.Attract) {
                // If set to attract, start the orbiting coroutine.
                Coroutine orbitCoroutine = StartCoroutine(OrbitObject(rb));
                _orbitingObjects[rb] = orbitCoroutine;
            } else {
                // If set to repel, push the object away from the center.
                Vector3 direction = (rb.position - transform.position).normalized;
                rb.linearVelocity = direction * launchForce;
            }
        }

        /// <summary>
        /// Called when a collider exits the tornado's trigger area.
        /// </summary>
        private void OnTriggerExit(Collider other) {
            Rigidbody rb = other.attachedRigidbody;
            if (rb == null || !_objectsInsideTornado.Contains(rb)) return;
            // Remove the object from tracking lists.
            _objectsInsideTornado.Remove(rb);

            // If it was orbiting, stop the coroutine.
            if (_orbitingObjects.ContainsKey(rb)) {
                StopCoroutine(_orbitingObjects[rb]);
                _orbitingObjects.Remove(rb);
            }
            // Restore gravity.
            rb.useGravity = true;
        }

        /// <summary>
        /// Coroutine that manages an object's orbiting behavior.
        /// </summary>
        /// <param name="rb">The Rigidbody of the object to orbit.</param>
        private IEnumerator OrbitObject(Rigidbody rb) {
            // Prepare the object for orbiting.
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            float currentAngle = Random.Range(0f, 360f);
            float time = 0f;

            // Main orbit loop.
            while (true) {
                // Exit conditions: if the object is destroyed, polarity changes, or it leaves the trigger.
                if (!rb || currentPolarity != Polarity.Attract || !_objectsInsideTornado.Contains(rb)) {
                    break;
                }

                // Check if the launch action has been triggered.
                if (_finalActionActivated) {
                    Vector3 directionToObject = (rb.position - transform.position).normalized;
                    // Calculate the angle between the tornado's forward direction and the object.
                    float angle = Vector3.SignedAngle(transform.forward, directionToObject, Vector3.up);

                    // If the object is within the launch cone, launch it.
                    if (angle >= minLaunchAngle && angle <= maxLaunchAngle) {
                        rb.linearVelocity = transform.forward * launchForce;
                        rb.useGravity = true;
                        // Stop tracking this object and exit the coroutine.
                        _orbitingObjects.Remove(rb);
                        _objectsInsideTornado.Remove(rb);
                        yield break; 
                    }
                }

                // --- Update Orbit Position ---
                time += Time.deltaTime;
                currentAngle += angularSpeed * Time.deltaTime;

                // Use PingPong to move the object between the outer and inner radii.
                float progress = Mathf.PingPong(time, 1f);
                float radius = Mathf.Lerp(outerRadius, innerRadius, progress);
                float rad = currentAngle * Mathf.Deg2Rad;

                // Calculate the new position on the circle.
                Vector3 offset = new Vector3(Mathf.Cos(rad), 0, Mathf.Sin(rad)) * radius;
                Vector3 targetPosition = transform.position + offset;

                // Smoothly adjust the Y position to match the tornado's center.
                targetPosition.y = Mathf.Lerp(rb.position.y, transform.position.y, Time.deltaTime * 5f);
                rb.MovePosition(targetPosition);

                yield return null; // Wait for the next frame.
            }
            // --- Cleanup after loop exits ---
            // Restore gravity and stop tracking the object if it's still valid.
            if (!rb) yield break;
            rb.useGravity = true;
            _orbitingObjects.Remove(rb);
            _objectsInsideTornado.Remove(rb);
        }

        /// <summary>
        /// Draws gizmos in the editor to visualize the tornado's radius.
        /// </summary>
        private void OnDrawGizmosSelected() {
            Gizmos.color = (currentPolarity == Polarity.Attract) ? Color.cyan : Color.magenta;
            Gizmos.DrawWireSphere(transform.position, outerRadius);
            Gizmos.DrawWireSphere(transform.position, innerRadius);
        }
    }
}