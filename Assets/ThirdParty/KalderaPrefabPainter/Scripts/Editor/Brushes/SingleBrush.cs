using CollisionBear.WorldEditor.Extensions;
using CollisionBear.WorldEditor.Utils;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace CollisionBear.WorldEditor.Brushes
{
    [System.Serializable]
    public class SingleBrush : BrushBase
    {
        [System.Serializable]
        public class SingleBrushSettings
        {
            public bool MaintainRotation = false;
        }

        protected override string ButtonImagePath => "Icons/IconGridPoint.png";

        [SerializeField]
        private SingleBrushSettings Settings = new SingleBrushSettings();

        private Vector3? StartHoverPosition;

        public override void DrawBrushEditor(ScenePlacer placer)
        {
            using (new EditorGUILayout.HorizontalScope()) {
                EditorCustomGUILayout.SetGuiBackgroundColorState(Settings.MaintainRotation);
                if (GUILayout.Button(KalderaEditorUtils.MaintainRotationContent, GUILayout.Width(KalderaEditorUtils.IconButtonSize), GUILayout.Height(KalderaEditorUtils.IconButtonSize))) {
                    Settings.MaintainRotation = !Settings.MaintainRotation;
                    placer.NotifyChange();
                }
            }
        }

        public override void DrawBrushHandle(Vector3 placementPosition, Vector3 adjustedWorldPosition)
        {
            if (HasDrag(StartDragPosition, EndDragPosition)) {
                DrawRotationCompass(StartDragPosition.Value, adjustedWorldPosition, Rotation, Normal);
                return;
            }

            if (StartHoverPosition.HasValue) {
                DrawRotationCompass(StartHoverPosition.Value, adjustedWorldPosition, Rotation, Normal);
                return;
            }
        }

        public override void DrawSceneHandleText(Vector2 screenPosition, Vector3 worldPosition, ScenePlacer placer)
        {
            DrawHandleTextAtOffset(screenPosition, 0, new GUIContent($"Scale:\t {placer.CurrentBrush.ScaleFactor.ToString(FloatFormat)}"));
            DrawHandleTextAtOffset(screenPosition, 1, new GUIContent($"Rotation:\t {placer.CurrentBrush.Rotation.DirectionToEuler().ToString(RotationFormat)}"));
            DrawHandleTextAtOffset(screenPosition, 2, GetClearContent());
        }

        public override void MoveBrush(Vector3 position, Vector3 brushNormal, ScenePlacer placer)
        {
            base.MoveBrush(position, brushNormal, placer);
            placer.UpdatePlacements();

            StartHoverPosition = null;
        }

        public override void ActiveDragPlacement(Vector3 position, SelectionSettings settings, double deltaTime, ScenePlacer placer)
        {
            EndDragPosition = position;

            if (!HasMinDragDistance(position)) {
                return;
            }

            Rotation = placer.RotateTowardsPosition(position);
        }

        public override void ShiftDragPlacement(Vector3 position, SelectionSettings settings, double deltaTime, ScenePlacer placer)
        {
            var snappedPosition = GetRotationSnappedPosition(GetDragPosition(placer.PlacementPosition, position), position);
            ActiveDragPlacement(snappedPosition, settings, deltaTime, placer);
        }

        public override void ShiftHoverPlacement(Vector3 position, SelectionSettings settings, double deltaTime, ScenePlacer placer)
        {
            if ((placer.PlacementPosition - position).magnitude < MinDragDistance) {
                StartHoverPosition = null;
                return;
            }

            if (!StartHoverPosition.HasValue) {
                StartHoverPosition = position;
            }

            Rotation = placer.RotateTowardsPosition(position);
        }

        public override List<GameObject> EndPlacement(Vector3 position, GameObject parentCollider, SelectionSettings settings, ScenePlacer placer)
        {
            LastRotation = placer.PlacementCollection.Placements.First().Rotation;
            var result = base.EndPlacement(GetDragPosition(StartDragPosition, position), parentCollider, settings, placer);

            StartDragPosition = null;
            EndDragPosition = null;
            StartHoverPosition = null;

            return result;
        }

        private bool HasMinDragDistance(Vector3 position) => DragDistance(position) > MinDragDistance;

        private float DragDistance(Vector3 position)
        {
            if (!StartDragPosition.HasValue) {
                return 0f;
            }

            return (StartDragPosition.Value - position).magnitude;
        }

        private Vector3 GetDragPosition(Vector3? startDragPosition, Vector3 position)
        {
            if (startDragPosition.HasValue) {
                return startDragPosition.Value;
            } else {
                return position;
            }
        }

        public override void CycleVariant(int delta, ScenePlacer placer)
        {
            foreach (var placement in placer.PlacementCollection.Placements) {
                var validObjects = placement.Item.ValidObjects();
                var selectedVariantIndex = validObjects.IndexOf(placement.PrefabObject);

                if (selectedVariantIndex == -1) {
                    continue;
                }
                var nextIndex = (int)Mathf.Repeat(selectedVariantIndex + delta, validObjects.Count);
                placement.ReplacePlacementObject(nextIndex, Position, ScaleFactor);
            }
        }

        protected override List<Vector3> GetPlacementOffsetValues(Vector3 position, SelectionSettings _s, ScenePlacer _p) => new List<Vector3> { Vector3.zero };

        protected override Vector3 GetItemRotation(Vector3 position, PaletteItem item, GameObject prefabObject)
        {
            var result = base.GetItemRotation(position, item, prefabObject);

            if (Settings.MaintainRotation && LastRotation.HasValue) {
                result = LastRotation.Value.eulerAngles;
            }

            return result;
        }
    }
}