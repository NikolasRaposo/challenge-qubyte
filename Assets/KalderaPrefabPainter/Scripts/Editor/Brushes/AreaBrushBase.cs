using CollisionBear.WorldEditor.Extensions;
using CollisionBear.WorldEditor.Generation;
using CollisionBear.WorldEditor.Utils;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace CollisionBear.WorldEditor.Brushes
{
    public class AreaBrushSizePreset
    {
        public float BrushSize;

        private int KeyBindingIndex;

        public AreaBrushSizePreset() { }

        public AreaBrushSizePreset(int keybindingIndex, float size)
        {
            KeyBindingIndex = keybindingIndex;
            BrushSize = size;
        }

        public bool EventMatch(Event currentEvent) => GetKeyBinding()?.EventMatch(currentEvent) ?? false;

        private KeyBinding GetKeyBinding()
        {
            if (KalderaSpawnEffectsSettings.ToolSizeHotKeys != null && KalderaSpawnEffectsSettings.ToolSizeHotKeys.Length >= KeyBindingIndex - 1) {
                return KalderaSpawnEffectsSettings.ToolSizeHotKeys[KeyBindingIndex];
            } else {
                return null;
            }
        }
    }

    [System.Serializable]
    public class AreaBrushSettings
    {
        public static readonly IReadOnlyList<AreaBrushSizePreset> BrushSizePresets = new List<AreaBrushSizePreset> {
            new AreaBrushSizePreset (0, 1f),
            new AreaBrushSizePreset (1, 5.0f),
            new AreaBrushSizePreset (2, 10.0f),
            new AreaBrushSizePreset (3, 15.0f),
            new AreaBrushSizePreset (4, 20.0f)
        };

        public int DistributionTypeIndex = 0;
        public float BrushSize = BrushSizePresets[1].BrushSize;
        public float ObjectDensity = 1.0f;
    }

    public abstract class AreaBrushBase : BrushBase
    {
        public const float BrushSizeMin = 0.1f;
        public const float BrushSizeMax = 25;
        public const float BrushSpacingMin = 0.2f;
        public const float BrushSpacingMax = 10.0f;

        [SerializeField]
        protected AreaBrushSettings Settings = new AreaBrushSettings();

        protected IGenerationBounds GenerationBounds;

        protected Vector3? SavedRotation;
        protected Vector3? StartHoverPosition;

        public override void MoveBrush(Vector3 position, Vector3 brushNormal, ScenePlacer placer)
        {
            base.MoveBrush(position, brushNormal, placer);

            placer.RotatatePlacement(Rotation, Normal);
            placer.UpdatePlacements();

            SavedRotation = null;
            StartHoverPosition = null;
        }

        public override void StartPlacement(Vector3 position, ScenePlacer placer)
        {
            StartDragPosition = position;
            EndDragPosition = null;
            base.StartPlacement(position, placer);
        }

        public override void ActiveDragPlacement(Vector3 position, SelectionSettings settings, double deltaTime, ScenePlacer placer)
        {
            EndDragPosition = position;

            Rotation = placer.RotatatePlacementInPlace(position, Normal);
            SavedRotation = Rotation;
        }

        public override void ShiftDragPlacement(Vector3 position, SelectionSettings settings, double deltaTime, ScenePlacer placer)
        {
            if (!StartDragPosition.HasValue) {
                return;
            }

            var snappedPosition = GetRotationSnappedPosition(StartDragPosition.Value, position);
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

            Rotation = placer.RotatatePlacementInPlace(position, Normal);
            SavedRotation = Rotation;
        }

        public override List<GameObject> EndPlacement(Vector3 position, GameObject parentCollider, SelectionSettings settings, ScenePlacer placer)
        {
            StartDragPosition = null;
            StartHoverPosition = null;
            SavedRotation = null;

            return base.EndPlacement(position, parentCollider, settings, placer);
        }

        public override void DrawBrushEditor(ScenePlacer placer)
        {
            using (new EditorGUILayout.HorizontalScope()) {
                EditorGUILayout.LabelField(KalderaEditorUtils.BrushSizeContent, GUILayout.Width(KalderaEditorUtils.OptionLabelWidth));
                var tmpBrushSize = EditorGUILayout.Slider(Settings.BrushSize, BrushSizeMin, BrushSizeMax);
                if (tmpBrushSize != Settings.BrushSize) {
                    SetBrushSize(tmpBrushSize, placer);
                }
            }

            using (new EditorGUILayout.HorizontalScope()) {
                EditorGUILayout.LabelField(KalderaEditorUtils.ObjectDensityContent, GUILayout.Width(KalderaEditorUtils.OptionLabelWidth));
                var tmpBrushSpacing = EditorGUILayout.Slider(Settings.ObjectDensity, BrushSpacingMin, BrushSpacingMax);
                if (tmpBrushSpacing != Settings.ObjectDensity) {
                    Settings.ObjectDensity = tmpBrushSpacing;
                    placer.NotifyChange();
                }
            }

            EditorGUILayout.Space();

            using (new EditorGUILayout.HorizontalScope()) {
                EditorGUILayout.LabelField(KalderaEditorUtils.BrushDistributionContent, EditorStyles.boldLabel, GUILayout.Width(KalderaEditorUtils.OptionLabelWidth));
                EditorGUILayout.LabelField(placer.GetDistributionModes()[placer.SelectionSettings.SelectedDistributionIndex].Name);
            }

            using (new EditorGUILayout.HorizontalScope()) {
                foreach (var distributionMode in placer.GetDistributionModes()) {
                    EditorCustomGUILayout.SetGuiBackgroundColorState(placer.CurrentDistribution == distributionMode);
                    if (GUILayout.Button(distributionMode.GetGUIContent(), GUILayout.Width(KalderaEditorUtils.IconButtonSize), GUILayout.Height(KalderaEditorUtils.IconButtonSize))) {
                        placer.SelectionSettings.SelectedDistributionIndex = distributionMode.Index;
                        placer.NotifyChange();
                    }
                }
            }

            EditorCustomGUILayout.RestoreGuiColor();
        }

        public override void DrawSceneHandleText(Vector2 screenPosition, Vector3 worldPosition, ScenePlacer placer)
        {
            DrawHandleTextAtOffset(screenPosition, 0, new GUIContent($"Object count: {placer.PlacementCollection.Placements.Count}"));
            DrawHandleTextAtOffset(screenPosition, 1, new GUIContent($"Rotation:\t {placer.CurrentBrush.Rotation.DirectionToEuler().ToString(RotationFormat)}"));
            DrawHandleTextAtOffset(screenPosition, 2, GetClearContent());
        }

        public override bool HandleKeyEvents(Event currentEvent, ScenePlacer placer)
        {
            if (currentEvent.type == EventType.KeyDown) {
                foreach (var preset in AreaBrushSettings.BrushSizePresets) {
                    if (preset.EventMatch(currentEvent)) {
                        SetBrushSize(preset.BrushSize, placer);
                        currentEvent.Use();
                        return true;
                    }
                }
            }

            return false;
        }

        protected override List<Vector3> GetPlacementOffsetValues(Vector3 position, SelectionSettings selectionSettings, ScenePlacer placer)
        {
            var spacing = selectionSettings.GetSelectedItemSize() * (1f / Settings.ObjectDensity);
            return placer
                .GetDistributionModes()[selectionSettings.SelectedDistributionIndex]
                .GetPoints(Settings.BrushSize, spacing, GenerationBounds);
        }

        private void SetBrushSize(float size, ScenePlacer placer)
        {
            Settings.BrushSize = size;
            placer.NotifyChange();
        }
    }
}
