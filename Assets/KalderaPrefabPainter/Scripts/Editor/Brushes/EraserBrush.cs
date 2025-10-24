using CollisionBear.WorldEditor.Utils;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace CollisionBear.WorldEditor.Brushes
{
    [System.Serializable]
    public class EraserBrush : BrushBase
    {
        private static readonly List<PlacementInformation> EmptyPlacementList = new List<PlacementInformation>();

        private static GameObject[] GameObjectsCache = new GameObject[100];

        private static readonly Color PassiveHandleBrushColor = new Color(1f, 1f, 0f, 0.1f);
        private static readonly Color PassiveHandleOutlineColor = new Color(1f, 1f, 0f, 0.5f);

        private static readonly Color ActiveHandleBrushColor = new Color(1f, 0.2f, 0f, 0.1f);
        private static readonly Color ActiveHandleOutlineColor = new Color(1f, 0.2f, 0f, 0.5f);

        public const float SprayIntensityMin = 1f;
        public const float SprayIntensityMax = 100f;

        private LayerMask LayerMask = int.MaxValue;
        private List<GameObject> ErasedGameObejcts = new List<GameObject>();

        [System.Serializable]
        public class EraserBrushSettings
        {
            public static readonly IReadOnlyList<AreaBrushSizePreset> BrushSizePresets = new List<AreaBrushSizePreset> {
            new AreaBrushSizePreset (0, 1f),
            new AreaBrushSizePreset (1, 5.0f),
            new AreaBrushSizePreset (2, 10.0f),
            new AreaBrushSizePreset (3, 15.0f),
            new AreaBrushSizePreset (4, 20.0f)
        };

            public float BrushSize = BrushSizePresets[1].BrushSize;
        }

        protected override string ButtonImagePath => "Icons/IconEraser.png";

        private bool IsActive;
        private bool IsDragging;

        [SerializeField]
        private EraserBrushSettings Settings = new EraserBrushSettings();

        public override bool ShowBrush(ScenePlacer scenePlacer) => IsActive;

        public override void OnSelected(ScenePlacer placer)
        {
            IsActive = true;
        }

        public override void OnClearSelection(ScenePlacer placer)
        {
            placer.SelectionSettings.ClearSelection();
            IsActive = false;
        }

        public override void DrawBrushEditor(ScenePlacer placer)
        {
            using (new EditorGUILayout.HorizontalScope()) {
                EditorGUILayout.LabelField(KalderaEditorUtils.BrushSizeContent, GUILayout.Width(KalderaEditorUtils.OptionLabelWidth));
                var tmpBrushSize = EditorGUILayout.Slider(Settings.BrushSize, AreaBrushBase.BrushSizeMin, AreaBrushBase.BrushSizeMax);
                if (tmpBrushSize != Settings.BrushSize) {
                    SetBrushSize(tmpBrushSize, placer);
                }
            }

            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope()) {
                LayerMask = InternalEditorUtility.ConcatenatedLayersMaskToLayerMask(
                    EditorGUILayout.MaskField("Layer Filter", InternalEditorUtility.LayerMaskToConcatenatedLayersMask(LayerMask), InternalEditorUtility.layers)
                );
            }

            EditorGUILayout.Space();

            if (placer.SelectionSettings.SelectedItems.Count == 0) {
                EditorGUILayout.LabelField("Select items in the Palette to only Erase those items.", EditorStyles.miniLabel);
            } else {
                EditorGUILayout.LabelField("Will only erases selected items. Clear selection to erased any item again.", EditorStyles.miniLabel);
            }
        }

        public override void DrawAdditionalSettings(ScenePlacer placer, SelectionSettings settings) { }

        protected override void UpdatePlacements(Vector3 position, SelectionSettings selectionSettings, PlacementCollection placementCollection, ScenePlacer placer) { }

        protected override List<Vector3> GetPlacementOffsetValues(Vector3 position, SelectionSettings selectionSettings, ScenePlacer placer) => EmptyPointList;

        protected override List<PlacementInformation> PlacementsToPlace(ScenePlacer placer) => EmptyPlacementList;

        public override void DrawBrushHandle(Vector3 placementPosition, Vector3 mousePosition)
        {
            if (IsDragging) {
                Handles.color = ActiveHandleBrushColor;
                Handles.DrawSolidDisc(placementPosition, Vector3.up, Settings.BrushSize);

                Handles.color = ActiveHandleOutlineColor;
                Handles.DrawWireDisc(placementPosition, Vector3.up, Settings.BrushSize);
            } else {
                Handles.color = PassiveHandleBrushColor;
                Handles.DrawSolidDisc(placementPosition, Vector3.up, Settings.BrushSize);

                Handles.color = PassiveHandleOutlineColor;
                Handles.DrawWireDisc(placementPosition, Vector3.up, Settings.BrushSize);
            }
        }

        public override void DrawSceneHandleText(Vector2 screenPosition, Vector3 worldPosition, ScenePlacer placer)
        {
        }

        public override bool HandleKeyEvents(Event currentEvent, ScenePlacer placer)
        {
            if (currentEvent.type == EventType.KeyDown) {
                foreach (var preset in EraserBrushSettings.BrushSizePresets) {
                    if (preset.EventMatch(currentEvent)) {
                        SetBrushSize(preset.BrushSize, placer);
                        return true;
                    }
                }
            }

            return false;
        }

        public override void StartPlacement(Vector3 position, ScenePlacer placer)
        {
            ErasedGameObejcts = GetPrefabsToDelete(placer);
            DistanceUtility.SegmentMap();
            EraseObjectsInRange(position, placer);
            IsDragging = true;
        }

        private List<GameObject> GetPrefabsToDelete(ScenePlacer placer) => placer.SelectionSettings.SelectedItems.SelectMany(i => i.GameObjectVariants).ToList();

        public override void ActiveDragPlacement(Vector3 worldPosition, SelectionSettings settings, double deltaTime, ScenePlacer placer)
        {
            UpdatePosition(worldPosition, placer);
            EraseObjectsInRange(worldPosition, placer);
        }

        private void EraseObjectsInRange(Vector3 worldPosition, ScenePlacer placer)
        {
            var amount = DistanceUtility.GetGameObjectsInRangeNonAlloc(worldPosition, Settings.BrushSize, GameObjectsCache);

            for (int i = 0; i < amount; i++) {
                var gameObject = GameObjectsCache[i];
                if (gameObject == null) {
                    continue;
                }

                var rootObject = PrefabUtility.GetOutermostPrefabInstanceRoot(gameObject);
                if (gameObject != rootObject) {
                    continue;
                }

                if (((1 << rootObject.layer) & LayerMask.value) == 0) {
                    continue;
                }

                if (ErasedGameObejcts.Count > 0 && !ErasedGameObejcts.Contains(PrefabUtility.GetCorrespondingObjectFromSource(gameObject))) {
                    continue;
                }


                Undo.DestroyObjectImmediate(gameObject);
            }
        }

        public override void StaticDragPlacement(Vector3 position, SelectionSettings settings, double deltaTime, ScenePlacer placer)
        {
            UpdatePosition(position, placer);
        }

        private void UpdatePosition(Vector3 worldPosition, ScenePlacer placer)
        {
            placer.MovePosition(placer.ScreenPosition, worldPosition);
        }

        public override List<GameObject> EndPlacement(Vector3 position, GameObject parentCollider, SelectionSettings settings, ScenePlacer placer)
        {
            IsDragging = false;
            return EmptyGameObjectList;
        }

        private void SetBrushSize(float size, ScenePlacer placer)
        {
            Settings.BrushSize = size;
            placer.NotifyChange();
        }
    }
}