using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;
namespace Gameplay {
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

        [Header("Descent Animation")]
        [Tooltip("The duration for items to descend to original height after spreading.")]
        public float descentDuration = 0.3f;

        [Tooltip("The delay before descent animation begins after spreading is complete.")]
        public float delayBeforeDescent = 0.1f;
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
        
        // Store the original spawn height for descent animation
        private float _originalSpawnHeight;

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
            
            // Store the original spawn height
            _originalSpawnHeight = transform.position.y;

            // Instantiate the specified quantity of items.
            for (int i = 0; i < settings.quantity; i++) {
                GameObject item = Instantiate(itemPrefab, transform.position, Quaternion.identity);
                item.name = itemPrefab.name + "_" + i; // Give it a simple, unique name.
                _spawnedItems.Add(item);
            }

            // Start the unified animation that combines rise, spread and fall
        if (settings.spreadItems) {
            AnimateCoinsNaturally();
        } else {
            AnimateRiseAndFall();
        }
        }

        /// <summary>
    /// Applies the rise animation to all spawned items while they spread.
    /// </summary>
    private void AnimateRiseAndFall() {
        foreach (GameObject item in _spawnedItems) {
            if (item == null) continue;

            Vector3 startPos = item.transform.position;
            Vector3 peakPos = startPos + Vector3.up*settings.riseHeight;

            // Animate moving up then down in a natural arc
            item.transform.DOMoveY(peakPos.y, settings.riseFallDuration * 0.5f)
                .SetEase(Ease.OutSine)
                .OnComplete(() => {
                    if (item != null) {
                        item.transform.DOMoveY(_originalSpawnHeight, settings.riseFallDuration * 0.5f)
                            .SetEase(Ease.InSine)
                            .OnComplete(() => {
                                item.GetComponent<CoinPickup>()?.OnSpreadComplete();
                            });
                    }
                });
        }
    }
    
    /// <summary>
    /// Creates a natural coin animation that combines rise, spread and fall in a unified motion.
    /// Simulates the physics of tossing coins up and letting them fall naturally.
    /// </summary>
    private void AnimateCoinsNaturally() {
        if (_spawnedItems.Count == 0) return;

        // Calculate the base angle between each item for even distribution
        float angleIncrement = 360f / _spawnedItems.Count;
        float totalAnimationTime = settings.riseFallDuration + settings.descentDuration;
        
        for (int i = 0; i < _spawnedItems.Count; i++) {
            GameObject item = _spawnedItems[i];
            if (item == null) continue;

            // Calculate target spread position
            float randomAngleOffset = Random.Range(-10f, 10f);
            float angle = (i * angleIncrement + randomAngleOffset) * Mathf.Deg2Rad;
            float variedRadius = settings.spreadRadius * Random.Range(0.8f, 1.2f);
            
            Vector3 direction = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
            Vector3 startPos = item.transform.position;
            Vector3 horizontalTarget = startPos + direction * variedRadius;
            
            // Environmental collision check
            if (Physics.Raycast(startPos, direction, out RaycastHit hit, variedRadius)) {
                horizontalTarget = hit.point - direction * 0.3f;
            }
            
            // Create the natural coin trajectory using a sequence
            var sequence = DOTween.Sequence();
            
            // Phase 1: Rise while starting to spread (0 to 50% of time)
            float riseTime = totalAnimationTime * 0.4f;
            Vector3 peakPos = new Vector3(
                Mathf.Lerp(startPos.x, horizontalTarget.x, 0.3f),
                startPos.y + settings.riseHeight,
                Mathf.Lerp(startPos.z, horizontalTarget.z, 0.3f)
            );
            
            sequence.Append(item.transform.DOMove(peakPos, riseTime).SetEase(Ease.OutSine));
            
            // Phase 2: Continue spreading while falling (50% to 100% of time)
            float fallTime = totalAnimationTime * 0.6f;
            Vector3 finalPos = new Vector3(horizontalTarget.x, _originalSpawnHeight, horizontalTarget.z);
            
            sequence.Append(item.transform.DOMove(finalPos, fallTime).SetEase(Ease.InSine));
            
            // Notify when complete
            sequence.OnComplete(() => {
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
}