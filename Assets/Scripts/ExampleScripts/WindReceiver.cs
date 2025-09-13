using UnityEngine;

/// <summary>
/// A component that allows a Rigidbody to be affected by forces from a WindField.
/// Attach this to any object that should react to wind.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class WindReceiver : MonoBehaviour {
    [Tooltip("A multiplier for the incoming wind force. Higher values mean the object is pushed more easily.")]
    [Range(0.1f, 5f)]
    public float sensitivity = 1f;
    
    private Rigidbody _rb;
    
    /// <summary>
    /// Caches the Rigidbody component on Awake.
    /// </summary>
    private void Awake() {
        _rb = GetComponent<Rigidbody>();
    }

    /// <summary>
    /// Public method called by a wind source to apply a force.
    /// </summary>
    /// <param name="forceDirection">The direction and magnitude of the wind force.</param>
    public void ApplyWind(Vector3 forceDirection) {
        // Apply the force, scaled by this object's sensitivity.
        // ForceMode.Acceleration ignores the object's mass, resulting in a more consistent "push".
        _rb.AddForce(forceDirection * sensitivity, ForceMode.Acceleration);
    }
}
