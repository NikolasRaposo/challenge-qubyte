using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using ExampleScripts;
using UnityEngine;
/// <summary>
/// A serializable class to hold all settings for the item spawn effect.
/// This allows for easy configuration in the Unity Inspector without modifying code.
/// </summary>
[System.Serializable]
public class ItemEffectSettings {
    [Header("Basic Settings")]
    [Tooltip("The number of items to create.")]
    public int quantity = 1;

    [Tooltip("If true, the items will spread out in a radial pattern.")]
    public bool spreadItems = true;

    [Header("Upward Animation")]
    [Tooltip("The maximum height the items will rise before falling.")]
    public float riseHeight = 2f;

    [Tooltip("The total time for the rise and fall animation (in seconds).")]
    public float riseFallDuration = 0.5f;

    [Header("Spreading Settings")]
    [Tooltip("The maximum distance items will spread from the center.")]
    public float spreadRadius = 2f;

    [Tooltip("The delay before the spreading animation begins (in seconds).")]
    public float delayBeforeSpread = 0.05f;

    [Tooltip("If true, the system will attempt to prevent items from overlapping.")]
    public bool avoidOverlap = true;

    [Tooltip("The minimum distance between items when 'avoidOverlap' is enabled.")]
    public float minDistance = 0.5f;
}

/// <summary>
/// Manages the effect of spawning and spreading items like coins or power-ups.
/// This script handles creating items, animating them, and arranging them in a radial pattern.
/// </summary>
public class ItemEffectController : MonoBehaviour {
    [Header("Item Settings")]
    [Tooltip("The prefab of the item that will be instantiated.")]
    public GameObject itemPrefab;

    [Tooltip("Configuration for the item spawning behavior.")]
    public ItemEffectSettings settings = new ItemEffectSettings();

    // A list to keep track of all created items.
    private readonly List<GameObject> _spawnedItems = new List<GameObject>();

    /// <summary>
    /// Creates the items, starts their rise animation, and schedules the spreading animation.
    /// This method is typically called by another script (e.g., BoxInteractor).
    /// </summary>
    public void CreateItems() {
        if (itemPrefab == null) {
            Debug.LogError("ItemPrefab is not assigned in the ItemEffectController!");
            return;
        }

        // Clear any previously spawned items.
        _spawnedItems.Clear();

        // Instantiate the specified quantity of items.
        for (int i = 0; i < settings.quantity; i++) {
            GameObject item = Instantiate(itemPrefab, transform.position, Quaternion.identity);
            item.name = itemPrefab.name + "_" + i; // Give it a simple, unique name.
            _spawnedItems.Add(item);
        }

        // Start the rise and fall animation.
        AnimateRiseAndFall();

        // Schedule the radial spread animation after a short delay.
        if (settings.spreadItems) {
            // Use DOVirtual.DelayedCall for a reliable, tween-based delay.
            DOVirtual.DelayedCall(settings.delayBeforeSpread, SpreadItemsRadially);
        }
    }

    /// <summary>
    /// Applies the rise and fall animation to all spawned items.
    /// </summary>
    private void AnimateRiseAndFall() {
        foreach (GameObject item in _spawnedItems) {
            if (item == null) continue;

            Vector3 startPos = item.transform.position;
            Vector3 peakPos = startPos + Vector3.up*settings.riseHeight;

            // Animate moving up, then on completion, animate moving back down.
            item.transform.DOMoveY(peakPos.y, settings.riseFallDuration/2f)
                .SetEase(Ease.OutSine)
                .OnComplete(() => {
                    // Ensure item still exists before starting the second part of the animation.
                    if (item != null) {
                        item.transform.DOMoveY(startPos.y, settings.riseFallDuration/2f)
                            .SetEase(Ease.InSine);
                    }
                });
        }
    }

    /// <summary>
    /// Spreads the items out in a radial pattern with random variations.
    /// Attempts to avoid item overlap and collisions with the environment.
    /// </summary>
    private void SpreadItemsRadially() {
        if (_spawnedItems.Count == 0) return;

        // Calculate the base angle between each item for even distribution.
        float angleIncrement = 360f/_spawnedItems.Count;
        List<Vector3> targetPositions = new List<Vector3>();

        for (int i = 0; i < _spawnedItems.Count; i++) {
            GameObject item = _spawnedItems[i];
            if (item == null) continue;

            // --- Calculate Target Position ---
            // Add a slight random variation to the angle and radius to make the pattern look more natural.
            float randomAngleOffset = Random.Range(-10f, 10f);
            float angle = (i*angleIncrement + randomAngleOffset)*Mathf.Deg2Rad;
            float variedRadius = settings.spreadRadius*Random.Range(0.8f, 1.2f);

            // Calculate direction and final destination.
            Vector3 direction = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
            Vector3 startPos = item.transform.position; // Use the item's current position as the origin.
            Vector3 destination = startPos + direction*variedRadius;

            // --- Environmental Collision Check ---
            // Cast a ray from the start position to the destination to check for walls or obstacles.
            if (Physics.Raycast(startPos, direction, out RaycastHit hit, variedRadius)) {
                // If we hit something, place the item slightly away from the hit point.
                destination = hit.point - direction*0.3f;
            }

            // --- Overlap Avoidance ---
            if (settings.avoidOverlap) {
                int attempts = 0;
                // Try a few times to find a position that isn't too close to another item's target position.
                while (IsTooCloseToOtherPositions(destination, targetPositions) && attempts < 5) {
                    // If too close, try a new random direction.
                    angle = Random.Range(0, 360)*Mathf.Deg2Rad;
                    direction = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                    destination = startPos + direction*variedRadius;
                    attempts++;
                }
            }

            targetPositions.Add(destination);

            // --- Animate to Target Position ---
            // Kill any previous movement tweens on this object to avoid conflicts.
            DOTween.Kill(item.transform);

            // Start the movement tween.
            item.transform.DOMove(destination, 0.4f)
                .SetEase(Ease.OutQuad)
                .OnComplete(() => {
                    // Notify the item (if it has a CoinPickup script) that the spreading animation is complete.
                    if (item != null) {
                        item.GetComponent<CoinPickup>()?.OnSpreadComplete();
                    }
                });
        }
    }

    /// <summary>
    /// Checks if a given position is too close to any of the already calculated target positions.
    /// </summary>
    /// <param name="position">The position to check.</param>
    /// <param name="otherPositions">A list of existing positions.</param>
    /// <returns>True if the position is too close to any other position, otherwise false.</returns>
    private bool IsTooCloseToOtherPositions(Vector3 position, List<Vector3> otherPositions) {
        return otherPositions.Any(pos => Vector3.Distance(position, pos) < settings.minDistance);
    }

    /// <summary>
    /// Called when the GameObject is being destroyed.
    /// Cleans up any running DOTween animations to prevent errors.
    /// </summary>
    private void OnDestroy() {
        // Safely kill all tweens associated with the spawned items.
        foreach (GameObject item in _spawnedItems.Where(item => item != null)) {
            DOTween.Kill(item.transform);
        }
    }
}