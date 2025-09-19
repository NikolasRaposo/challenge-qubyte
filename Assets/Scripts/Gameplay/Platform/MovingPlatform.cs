using DG.Tweening;
using UnityEngine;
namespace Gameplay.Platform {
    /// <summary>
    /// A platform that moves back and forth or between waypoints.
    /// </summary>
    public class MovingPlatform : PlatformBase {
        [Header("Movement Settings")]
        [Tooltip("The direction of movement relative to the platform's starting position.")]
        [SerializeField] private Vector3 moveDirection = Vector3.right;
        [Tooltip("The distance the platform will travel in the specified direction.")]
        [SerializeField] private float moveDistance = 5f;
        [Tooltip("The duration of one leg of the movement (from start to end).")]
        [SerializeField] private float moveDuration = 2f;
        [Tooltip("Movement easing function.")]
        [SerializeField] private Ease easeType = Ease.InOutSine;
        
        // This platform activates on its own, so we don't need player interaction logic
        protected override void ActivatePlatform() {
            // This platform type might not need direct player activation.
            // You could, for example, make it start moving only when the player steps on it.
            // For now, we'll leave it empty as the movement starts in Start().
        }

        private void Start() {
            // We don't need to call base.Awake() because this class doesn't override Awake().
            // Start the movement loop automatically.
            Vector3 destination = initialPosition + moveDirection.normalized * moveDistance;
            transform.DOMove(destination, moveDuration)
                .SetEase(easeType)
                .SetLoops(-1, LoopType.Yoyo);
        }
        
        // We override OnTriggerEnter to do nothing, as the player should just
        // be parented/unparented without activating any special logic.
        // The StickyPlatform script would handle parenting.
        protected override void OnTriggerEnter(Collider other) {
            // Intentionally empty to prevent base activation logic.
        }
    }
}