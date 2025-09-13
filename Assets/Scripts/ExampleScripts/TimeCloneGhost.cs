using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A "ghost" or "clone" that replays a recorded sequence of player inputs.
/// It uses a CharacterController to move based on the provided input frames.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class TimeCloneGhost : MonoBehaviour {
    [Header("Movement Settings")]
    [Tooltip("The movement speed of the clone.")]
    public float speed = 5f;
    [Tooltip("The upward force applied when a jump input is replayed.")]
    public float jumpForce = 5f;

    // --- Private State Variables ---
    private List<PlayerInputFrame> _replayInputs;
    private int _currentFrameIndex;
    private float _replayStartTime;
    private CharacterController _controller;
    private Vector3 _verticalVelocity;

    /// <summary>
    /// Caches the CharacterController component.
    /// </summary>
    private void Awake() {
        _controller = GetComponent<CharacterController>();
    }

    /// <summary>
    /// Loads the list of inputs to be replayed and starts the replay timer.
    /// </summary>
    /// <param name="inputs">The list of PlayerInputFrame to replay.</param>
    public void LoadInputs(List<PlayerInputFrame> inputs) {
        _replayInputs = inputs;
        _replayStartTime = Time.time;
        // Adjust start time to match the timestamp of the first frame.
        if (_replayInputs is { Count: > 0 }) {
            _replayStartTime -= _replayInputs[0].timestamp;
        }
    }

    /// <summary>
    /// Called every frame to process the replay sequence.
    /// </summary>
    private void Update() {
        // Stop if there are no inputs to replay or the sequence is finished.
        if (_replayInputs == null || _currentFrameIndex >= _replayInputs.Count) {
            // Optional: Destroy the clone after replay is done.
            // Destroy(gameObject, 1f); 
            return;
        }

        // Calculate how much time has passed since the replay began.
        float elapsedTime = Time.time - _replayStartTime;
        PlayerInputFrame currentFrame = _replayInputs[_currentFrameIndex];

        // Process all frames that should have occurred by now.
        while (elapsedTime >= currentFrame.timestamp) {
            ExecuteFrame(currentFrame);
            _currentFrameIndex++;
            if (_currentFrameIndex >= _replayInputs.Count) break;
            currentFrame = _replayInputs[_currentFrameIndex];
        }
        
        ApplyGravity();
    }
    
    /// <summary>
    /// Applies gravity to the character controller.
    /// </summary>
    private void ApplyGravity() {
        if (_controller.isGrounded && _verticalVelocity.y < 0) {
            // Apply a small downward force to keep the character grounded.
            _verticalVelocity.y = -2f;
        }

        // Apply gravity.
        _verticalVelocity.y += Physics.gravity.y * Time.deltaTime;
        _controller.Move(_verticalVelocity * Time.deltaTime);
    }


    /// <summary>
    /// Executes the actions defined in a single input frame.
    /// </summary>
    /// <param name="frame">The input frame to execute.</param>
    private void ExecuteFrame(PlayerInputFrame frame) {
        // Apply horizontal movement based on the recorded input.
        Vector3 move = new Vector3(frame.move.x, 0, frame.move.y);
        _controller.Move(move*(speed*Time.deltaTime));

        // Apply jump force if the jump button was pressed and the clone is grounded.
        if (frame.jump && _controller.isGrounded)
        {
            _verticalVelocity.y = Mathf.Sqrt(jumpForce * -2f * Physics.gravity.y);
        }
    }
}