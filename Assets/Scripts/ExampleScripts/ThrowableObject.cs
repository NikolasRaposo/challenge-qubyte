using System;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// Controls a versatile throwable object. It can be thrown in an arc towards a target,
/// with optional behaviors like homing, boomerang return, ricochet, and impact effects.
/// </summary>
public class ThrowableObject : MonoBehaviour {
    /// <summary>
    /// Defines the type of target the object will be thrown towards.
    /// </summary>
    public enum TargetType { Player, FixedPoint, Direction }
    /// <summary>
    /// An interface for objects that can be activated by this throwable object on impact.
    /// </summary>
    private interface ITriggerable {
        void Activate();
    }

    [Header("Destination")]
    [Tooltip("The type of target to aim for.")]
    public TargetType targetType = TargetType.FixedPoint;
    [Tooltip("The transform to follow (used for Player or dynamic FixedPoint targeting).")]
    public Transform targetTransform;
    [Tooltip("A fixed world-space coordinate to throw towards.")]
    public Vector3 fixedPoint;
    [Tooltip("A direction to throw the object in (relative to its starting orientation).")]
    public Vector3 direction = Vector3.forward;

    [Header("Throw Trajectory")]
    [Tooltip("The peak height of the arc during the throw.")]
    public float arcHeight = 3f;
    [Tooltip("The base time the object spends in the air.")]
    public float baseAirTime = 1f;
    [Tooltip("If true, air time is calculated based on distance.")]
    public bool dynamicAirTime = true;
    [Tooltip("The amount of time per unit of distance for dynamic air time calculation.")]
    public float timePerUnit = 0.15f;

    [Header("Arc Curve")]
    [Tooltip("If true, use a custom AnimationCurve to define the arc's height over time.")]
    public bool useCustomCurve;
    [Tooltip("The curve defining the arc's height (X-axis is time [0,1], Y-axis is height multiplier).")]
    public AnimationCurve heightCurve;

    [Header("Rotation")]
    [Tooltip("Should the object spin while in the air?")]
    public bool rotateInAir = true;
    [Tooltip("The rotation to apply over the course of the throw (e.g., 360 on Y for a spin).")]
    public Vector3 rotationAxes = new Vector3(0, 360, 0);

    [Header("Boomerang")]
    [Tooltip("If true, the object will return after hitting its target.")]
    public bool returnAfterImpact;
    [Tooltip("The transform the object will return to.")]
    public Transform returnPoint;

    [Header("Homing")]
    [Tooltip("If true, the object will adjust its trajectory mid-flight to follow a moving target.")]
    public bool isHoming;
    [Tooltip("The maximum turning speed in degrees per second.")]
    public float maxTurnAnglePerSecond = 60f;

    [Header("Impact")]
    [Tooltip("If true, the object is destroyed upon impact.")]
    public bool destroyOnImpact = true;
    [Tooltip("The particle effect prefab to instantiate on impact.")]
    public GameObject impactEffect;
    [Tooltip("The sound to play on impact.")]
    public AudioClip impactSound;

    [Header("Interactions")]
    [Tooltip("If true, the object will deal damage on impact.")]
    public bool causeDamage;
    public int damage = 1;
    [Tooltip("If true, the object will activate objects that implement the ITriggerable interface.")]
    public bool activateTrigger;

    [Header("Ricochet")]
    [Tooltip("If true, the object can bounce off surfaces.")]
    public bool canRicochet;
    public int maxRicochets = 2;

    [Header("Extras")]
    [Tooltip("If true, a simple shadow will be projected below the object.")]
    public bool showShadow = true;
    public GameObject shadowPrefab;

    private int _ricochetCount;
    private GameObject _instantiatedShadow;
    private Vector3 _currentDestination;
    private Tween _flightTween;
    private bool _isInFlight;

    /// <summary>
    /// Initiates the throw sequence.
    /// </summary>
    private void Throw() {
        if (_isInFlight) return; // Prevent re-throwing while already in the air.
        _isInFlight = true;
        Vector3 startPosition = transform.position;
        _currentDestination = CalculateDestination();
        float distance = Vector3.Distance(startPosition, _currentDestination);
        float airTime = dynamicAirTime ? distance * timePerUnit : baseAirTime;
        // Create the shadow if enabled.
        if (showShadow && shadowPrefab && _instantiatedShadow == null) {
            _instantiatedShadow = Instantiate(shadowPrefab, startPosition, Quaternion.identity);
        }
        // Main flight tween using DOTween.To for full control over the update loop.
        _flightTween = DOTween.To(
            getter: () => 0f, // Virtual timer from 0
            setter: t =>      // 't' is the elapsed time percentage (0 to 1)
            {
                // Homing logic: continuously update the destination if needed.
                if (isHoming && targetTransform != null) {
                    Vector3 newDestination = CalculateDestination();
                    // Smoothly turn towards the new destination.
                    _currentDestination = Vector3.RotateTowards(
                        current: (_currentDestination - transform.position).normalized,
                        target: (newDestination - transform.position).normalized,
                        maxRadiansDelta: Mathf.Deg2Rad * maxTurnAnglePerSecond * Time.deltaTime,
                        maxMagnitudeDelta: 0f
                    ) * distance + transform.position;
                }

                // Interpolate position and add arc height.
                Vector3 currentPos = Vector3.Lerp(startPosition, _currentDestination, t);
                float height = useCustomCurve ? heightCurve.Evaluate(t) * arcHeight : Mathf.Sin(t * Mathf.PI) * arcHeight;
                currentPos.y += height;
                transform.position = currentPos;

                // Update shadow position by raycasting down.
                if (!_instantiatedShadow) return;
                if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 100f)) {
                    _instantiatedShadow.transform.position = hit.point + Vector3.up * 0.05f; // Small offset from the ground
                }
            },
            endValue: 1f, // to 1
            duration: airTime)
        .SetEase(Ease.Linear)
        .OnComplete(HandleImpact);

        // Rotation tween.
        if (rotateInAir) {
            transform.DORotate(rotationAxes, airTime, RotateMode.FastBeyond360)
                .SetRelative() // Rotates *by* this amount, not *to* this amount.
                .SetEase(Ease.Linear);
        }
    }

    /// <summary>
    /// Handles the logic for when the object reaches its destination or collides.
    /// </summary>
    private void HandleImpact() {
        _isInFlight = false;
        
        // Play impact effects.
        if (impactEffect) Instantiate(impactEffect, transform.position, Quaternion.identity);
        if (impactSound) AudioSource.PlayClipAtPoint(impactSound, transform.position);

        // Handle boomerang return logic.
        if (returnAfterImpact && returnPoint != null) {
            // Reconfigure and re-throw towards the return point.
            targetType = TargetType.FixedPoint;
            targetTransform = returnPoint; // Use transform for potential moving return point.
            returnAfterImpact = false; // Prevent infinite returns.
            Throw();
            return; // Exit to avoid destruction.
        }

        // Cleanup.
        if (_instantiatedShadow) Destroy(_instantiatedShadow);
        if (destroyOnImpact) Destroy(gameObject);
    }
    
    /// <summary>
    /// Determines the target destination based on the current settings.
    /// </summary>
    private Vector3 CalculateDestination() {
        switch (targetType) {
            case TargetType.Player:
                // Find the player by tag. A more robust system might use a direct reference.
                if (targetTransform == null) targetTransform = GameObject.FindWithTag("Player")?.transform;
                return targetTransform != null ? targetTransform.position : transform.position;
            case TargetType.FixedPoint:
                return targetTransform != null ? targetTransform.position : fixedPoint;
            case TargetType.Direction:
                return transform.position + transform.TransformDirection(direction.normalized * 10f); // 10f is arbitrary distance.
            default:
                return transform.position;
        }
    }

    /// <summary>
    /// Handles physical collisions during flight.
    /// </summary>
    private void OnCollisionEnter(Collision col) {
        if (!_isInFlight) return;
        // Handle ricochet logic.
        if (canRicochet && _ricochetCount < maxRicochets) {
            _ricochetCount++;
            // Reflect the current direction and re-throw.
            direction = Vector3.Reflect(_currentDestination - transform.position, col.contacts[0].normal).normalized;
            targetType = TargetType.Direction;
            _flightTween?.Kill(); // Stop the current tween.
            _isInFlight = false;
            Throw();
        } else {
            // Handle damage and triggers on collision.
            if (causeDamage && col.gameObject.CompareTag("Player")) {
                // Example: col.gameObject.GetComponent<HealthSystem>()?.TakeDamage(damage);
            }
            if (activateTrigger && col.gameObject.TryGetComponent(out ITriggerable trigger)) {
                trigger.Activate();
            }
            // Stop the tween and trigger the final impact logic.
            _flightTween?.Kill();
            HandleImpact();
        }
    }

    private void OnDestroy() {
        // Ensure all tweens and the shadow are destroyed when the object is destroyed.
        _flightTween?.Kill();
        if (_instantiatedShadow) Destroy(_instantiatedShadow);
    }
}