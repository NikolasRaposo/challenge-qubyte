using CollisionBear.WorldEditor.Utils;
using UnityEditor;
using UnityEngine;

namespace CollisionBear.WorldEditor.RaycastModes
{
    [System.Serializable]
    public class TopdownRaycastMode : IRaycastMode
    {
        public float MaxDistance = 100;

        public string Name => "Top-Down";

        public string HintText => "Top-Down\nChecks collision into the scene from a top-down perspective no matter how you rotate the camera.";

        public void DrawEditor(PaletteWindow paletteWindow)
        {
            MaxDistance = EditorGUILayout.Slider(KalderaEditorUtils.BrushDistanceContent, MaxDistance, 0, 1000);
        }

        public Ray GetRay(Vector3 position)
        {
            var normal = GetNormal(position);
            return new Ray(position - (normal * MaxDistance / 2), normal);
        }

        public float GetMaxDistance() => MaxDistance;

        public Vector3 GetRotationDirection(Vector3 startPosition, Vector3 endPosition) => GetDelta(startPosition, endPosition).normalized;

        public Vector3 GetNormal(Vector3 position) => Vector3.down;

        public Vector3 GetDelta(Vector3 startPosition, Vector3 endPosition)
        {
            endPosition.y = startPosition.y;
            var result = endPosition - startPosition;
            return result;
        }

        public Vector3 IndividualHeight(Vector3 result, Vector3 raycasyHit, Vector3 offset)
        {
            result.y = raycasyHit.y + offset.y;
            return result;
        }
    }
}