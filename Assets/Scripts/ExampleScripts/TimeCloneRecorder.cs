using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Records player inputs over a specified duration. The recorded inputs can then be
/// used by a TimeCloneGhost to create a "time clone" that replays the player's actions.
/// </summary>
public class TimeCloneRecorder : MonoBehaviour
{
    [Header("Recording Settings")]
    [Tooltip("The duration of the input recording in seconds. The recorder will only store the most recent inputs up to this duration.")]
    public float recordingDuration = 5f;
    [Tooltip("The prefab for the time clone ghost, which will be instantiated to replay the actions.")]
    public GameObject clonePrefab;

    // A list to store the frames of recorded input.
    private readonly List<PlayerInputFrame> _recordedInputs = new List<PlayerInputFrame>();
    private const bool IsRecording = true;
    private float _recordingStartTime;

    private void Start() {
        _recordingStartTime = Time.time;
    }

    /// <summary>
    /// Called every frame to handle input recording and clone creation.
    /// </summary>
    private void Update() {
        if (IsRecording) {
            RecordInput();
        }
        // Check for the key press to create a clone.
        if (Input.GetKeyDown(KeyCode.C)) {
            CreateClone();
        }
    }

    /// <summary>
    /// Captures the current player input and adds it to the recording list.
    /// </summary>
    private void RecordInput() {
        // Create a new input frame with the current inputs.
        _recordedInputs.Add(new PlayerInputFrame {
            move = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")),
            jump = Input.GetButton("Jump"),
            timestamp = Time.time - _recordingStartTime // Store time relative to start for easier replay.
        });
        
        // Prune old input frames to maintain the desired recording duration.
        // This creates a "rolling" buffer of the last X seconds of input.
        while (_recordedInputs.Count > 0 && _recordedInputs[^1].timestamp - _recordedInputs[0].timestamp > recordingDuration) {
            _recordedInputs.RemoveAt(0);
        }
    }

    /// <summary>
    /// Instantiates a time clone and passes the recorded inputs to it.
    /// </summary>
    private void CreateClone() {
        if (!clonePrefab || _recordedInputs.Count == 0) return;
        GameObject clone = Instantiate(clonePrefab, transform.position, transform.rotation);
        if (clone.TryGetComponent(out TimeCloneGhost ghost)) {
            // Pass a copy of the recorded inputs to the ghost for replaying.
            ghost.LoadInputs(new List<PlayerInputFrame>(_recordedInputs));
        }
    }
}

/// <summary>
/// A data structure to hold a snapshot of player inputs at a specific moment in time.
/// </summary>
[System.Serializable]
public class PlayerInputFrame {
    public Vector2 move;
    public bool jump;
    public float timestamp;
}