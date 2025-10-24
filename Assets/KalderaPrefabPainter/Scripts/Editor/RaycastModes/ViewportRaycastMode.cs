using CollisionBear.WorldEditor.Utils;
using System;
using UnityEditor;
using UnityEngine;

namespace CollisionBear.WorldEditor.RaycastModes
{
    [System.Serializable]
    public class ViewportRaycastMode : IRaycastMode
    {
        public float MaxDistance = 100;

        public string Name => "Viewport";
        public string HintText => "Viewport\nChecks collision into the scene from the cameras position, with it's starting point being the camera itself";
        public void DrawEditor(PaletteWindow paletteWindow)
        {
            MaxDistance = EditorGUILayout.Slider(KalderaEditorUtils.BrushDistanceContent, MaxDistance, 0.1f, 1000);
        }

        public Ray GetRay(Vector3 position)
        {
            var viewPortPosition = HandleUtility.WorldToGUIPoint(position);
            var viewPortRay = HandleUtility.GUIPointToWorldRay(viewPortPosition);

            var distanceToCamera = (position - viewPortRay.origin).magnitude;
            var distance = Mathf.Min(distanceToCamera, MaxDistance);

            viewPortRay.direction = viewPortRay.direction;
            viewPortRay.origin = position - viewPortRay.direction * distance;

            return viewPortRay;
        }

        public float GetMaxDistance() => MaxDistance;

        public Vector3 GetRotationDirection(Vector3 startPosition, Vector3 endPosition)
        {
            var screenDirection = (HandleUtility.WorldToGUIPoint(endPosition) - HandleUtility.WorldToGUIPoint(startPosition)).normalized;
            var angle = Mathf.Atan2 (screenDirection.y, screenDirection.x) * Mathf.Rad2Deg;
            angle = Mathf.Repeat(angle + 90, 360);
            var rotation = Quaternion.Euler(0, angle, 0);

            return rotation * Vector3.forward;
        }

        public Vector3 GetNormal(Vector3 position)
        {
            var viewPortPosition = HandleUtility.WorldToGUIPoint(position);
            var viewPortRay = HandleUtility.GUIPointToWorldRay(viewPortPosition);
            return viewPortRay.direction;
        }

        public Vector3 GetDelta(Vector3 startPosition, Vector3 endPosition) => endPosition - startPosition;

        public Vector3 IndividualHeight(Vector3 result, Vector3 raycasyHit, Vector3 offset)
        {
            var normal = GetNormal(result);
            var delta = (raycasyHit - result).magnitude + offset.y;
            result.y = raycasyHit.y + offset.y;

            result -= normal * delta;

            return result;
        }

        public Vector3 GetRotation(Vector3 startPosition, Vector3 endPosition) => Vector3.forward;
    }
}