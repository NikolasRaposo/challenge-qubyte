using CollisionBear.WorldEditor.Utils;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.Networking.UnityWebRequest;

namespace CollisionBear.WorldEditor.Brushes
{
    public abstract class BrushBase : IBrushButton
    {
        private const float CompassRadius = 5f;
        private static readonly Vector3 CompassScale = new Vector3(CompassRadius, CompassRadius, CompassRadius);

        private const float MinScaleFactor = 0.05f;
        private const float MaxScaleFactor = 10f;

        private const float ScaleUpStepValue = 1.01f;
        private const float ScaleDownStepValue = 1f / ScaleUpStepValue;

        private static readonly Vector3[] LineMesh = new Vector3[] {
            new Vector3(-0.5f, 0, 0),
            new Vector3(0.5f, 0, 0),
            new Vector3(0.5f, 0, -1),
            new Vector3(-0.5f, 0, -1),
        };

        protected const float ArrowSize = 3f;
        protected static readonly Vector3 ArrowScale = new Vector3(ArrowSize, ArrowSize, ArrowSize);

        protected static readonly Quaternion QuaterRotationY = Quaternion.Euler(0, 90, 0);
        protected static readonly Quaternion QuaterRotationBackY = Quaternion.Euler(0, -90, 0);
        protected static readonly Quaternion QuaterRotationX = Quaternion.Euler(-90, 0, 0);

        protected const float DirectionSnapping = 22.5f;

        protected const string FloatFormat = "0.00";
        protected const string RotationFormat = "0";

        protected const float MinDragDistance = 0.25f;

        protected static readonly Vector2 InitialOffset = new Vector2(0, 50);
        protected static readonly Vector2 TextOffset = new Vector2(0, 16);

        protected static readonly Collider[] CastCollidersCache = new Collider[64];
        protected static readonly List<Vector3> EmptyPointList = new List<Vector3>();
        protected static readonly List<GameObject> EmptyGameObjectList = new List<GameObject>();

        protected static readonly Color HandleOutlineColor = new Color(1f, 0.5f, 0.8f, 0.8f);
        protected static readonly Color HandleBrushColor = new Color(1f, 0.5f, 0.8f, 0.1f);

        protected static readonly Vector3 DefaultRotation = Vector3.forward;

        public int Index;

        public Vector3 Position;
        public Vector3 Rotation = DefaultRotation;
        public float ScaleFactor = 1f;
        public Vector3 Normal = Vector3.up;

        protected Vector3? StartDragPosition;
        protected Vector3? EndDragPosition;
        protected Quaternion? LastRotation = null;

        public virtual bool UseNormalRotation => true;

        public KeyBinding KeyBinding { get; set; }

        protected abstract string ButtonImagePath { get; }

        private Texture2D ButtonTexture;

        public void OnButtonPress(PaletteWindow paletteWindow)
        {
            paletteWindow.SetBrushType(this);
        }

        public Texture2D GetButtonTexture()
        {
            if (ButtonTexture == null) {
                ButtonTexture = KalderaEditorUtils.LoadAssetPath(ButtonImagePath); ;
            }

            return ButtonTexture;
        }

        public GUIContent GetButtonContent() => new GUIContent(GetButtonTexture(), KeyBinding.GetToolTip());

        public virtual bool Disabled { get; } = false;
        public virtual bool ShowBrush(ScenePlacer placer) => !placer.IsCleared();

        public virtual void OnSelected(ScenePlacer placer) { }
        public virtual void OnClearSelection(ScenePlacer placer) { }
        public abstract void DrawBrushEditor(ScenePlacer placer);
        public virtual void DrawAdditionalSettings(ScenePlacer placer, SelectionSettings settings)
        {
            EditorGUILayout.Space();

            using (new EditorGUILayout.HorizontalScope()) {
                EditorCustomGUILayout.SetGuiBackgroundColorState(settings.ParentObjectsToBaseObject);
                if (GUILayout.Button(KalderaEditorUtils.ParentObjectToBaseObjectContent, GUILayout.Width(KalderaEditorUtils.IconButtonSize), GUILayout.Height(KalderaEditorUtils.IconButtonSize))) {
                    settings.ParentObjectsToBaseObject = !settings.ParentObjectsToBaseObject;
                }

                EditorCustomGUILayout.SetGuiBackgroundColorState(settings.AvoidCollisions);
                if (GUILayout.Button(KalderaEditorUtils.AvoidCollisionContent, GUILayout.Width(KalderaEditorUtils.IconButtonSize), GUILayout.Height(KalderaEditorUtils.IconButtonSize))) {
                    settings.AvoidCollisions = !settings.AvoidCollisions;
                    placer.NotifyChange();
                }

                EditorCustomGUILayout.SetGuiBackgroundColorState(settings.OrientToGroundNormal);
                if (GUILayout.Button(KalderaEditorUtils.OrientToGroundNormalContent, GUILayout.Width(KalderaEditorUtils.IconButtonSize), GUILayout.Height(KalderaEditorUtils.IconButtonSize))) {
                    settings.OrientToGroundNormal = !settings.OrientToGroundNormal;
                }

                EditorCustomGUILayout.RestoreGuiColor();
            }

            if (settings.AvoidCollisions) {
                using (new EditorGUILayout.VerticalScope(GUI.skin.box)) {
                    var tmpSizeFactor = EditorGUILayout.Slider(KalderaEditorUtils.AvoidCollisionSpacingContent, settings.CollisionSizeFactor, 0.1f, 2f);
                    if (tmpSizeFactor != settings.CollisionSizeFactor) {
                        settings.CollisionSizeFactor = tmpSizeFactor;
                        placer.NotifyChange();
                    }

                    EditorGUILayout.HelpBox("Collisions are determined by approximating the colliders. They might not match the colliders on the Prefabs perfectly.", MessageType.Info);
                    EditorGUILayout.Space();
                }
            }

            using (new EditorGUILayout.HorizontalScope()) {
                EditorGUILayout.LabelField(KalderaEditorUtils.ObjectLimitContent, GUILayout.Width(KalderaEditorUtils.OptionLabelWidth));
                var tmpObjectLimit = EditorGUILayout.IntSlider(settings.ObjectLimit, KalderaEditorUtils.ObjectLimitMin, KalderaEditorUtils.ObjectLimitMax);
                if (tmpObjectLimit != settings.ObjectLimit) {
                    settings.ObjectLimit = tmpObjectLimit;
                    placer.NotifyChange();
                }
            }
        }

        public virtual void DrawSceneHandleText(Vector2 screenPosition, Vector3 worldPosition, ScenePlacer placer)
        {
            DrawHandleTextAtOffset(screenPosition, 0, GetClearContent());
        }

        public virtual bool HandleKeyEvents(Event keyEvent, ScenePlacer placer) => false;
        public virtual bool CanPlaceObject(Vector3 position, PlacementCollection placement) => placement.HasItems();

        public virtual void StartPlacement(Vector3 position, ScenePlacer placer)
        {
            StartDragPosition = position;
        }

        public virtual void MoveBrush(Vector3 position, Vector3 brushNormal, ScenePlacer placer)
        {
            Position = position;
            Normal = brushNormal;
        }

        public virtual void ActiveDragPlacement(Vector3 position, SelectionSettings settings, double deltaTime, ScenePlacer placer)
        {
            EndDragPosition = position;
            Position = position;
        }

        public virtual void StaticDragPlacement(Vector3 position, SelectionSettings settings, double deltaTime, ScenePlacer placer)
        {
            EndDragPosition = position;
        }

        public virtual void ShiftDragPlacement(Vector3 position, SelectionSettings settings, double deltaTime, ScenePlacer placer)
        {
            // Defaults to the same as an active drag
            ActiveDragPlacement(position, settings, deltaTime, placer);
        }

        public virtual void ShiftHoverPlacement(Vector3 position, SelectionSettings settings, double deltaTime, ScenePlacer placer)
        {
            // Defaults to same as move
            MoveBrush(position, placer.CurrentRaycastMode.GetNormal(position), placer);
        }

        public virtual List<GameObject> EndPlacement(Vector3 position, GameObject parentCollider, SelectionSettings settings, ScenePlacer placer)
        {
            if (!CanPlaceObject(position, placer.PlacementCollection)) {
                return EmptyGameObjectList;
            }

            return PlaceObjects(position, parentCollider, settings, placer);
        }

        public virtual PlacementCollection GeneratePlacementForBrush(Vector3 position, SelectionSettings selectionSettings, ScenePlacer placer)
        {
            var result = new PlacementCollection();

            if (!selectionSettings.HasItems()) {
                return result;
            }

            var validItems = selectionSettings.GetItemsWithVariants();
            if (validItems.Count == 0) {
                return result;
            }

            UpdatePlacements(position, selectionSettings, result, placer);
            return result;
        }

        public virtual void DrawBrushHandle(Vector3 placementPosition, Vector3 mousePosition) { }

        protected GUIContent GetClearContent()
            => new GUIContent($"Press {KalderaSpawnEffectsSettings.ToolsHotKeys[7].GetHotKeyString()} to clear the brush");


        protected Vector3 ScreenToWorldPosition(Vector2 screenPosition) => HandleUtility.GUIPointToWorldRay(screenPosition).GetPoint(0.1f);

        protected bool HasDrag(Vector3? startPosition, Vector3? endPosition)
        {
            if (!startPosition.HasValue || !endPosition.HasValue) {
                return false;
            }

            var startValue = startPosition.Value;
            var endValue = endPosition.Value;

            startValue.y = 0;
            endValue.y = 0;

            return (startValue - endValue).sqrMagnitude > 0.01f;
        }

        protected void DrawRotationCompass(Vector3 startPosition, Vector3 worldPosition, Vector3 direction, Vector3 normal)
        {
            var rotation = Quaternion.LookRotation(normal) * QuaterRotationX * QuaterRotationY * Quaternion.LookRotation(direction);

            DrawOnSceneViewMesh(KalderaEditorUtils.PlaneMesh, KalderaEditorUtils.CompassMaterial, startPosition, rotation, CompassScale);
        }

        protected void DrawRotationArrow(Vector3 startPosition, Vector3 worldPosition, Vector3 direction, Vector3 normal)
        {
            if ((worldPosition - startPosition).sqrMagnitude < MinScaleFactor) {
                return;
            }

            var rotation = Quaternion.LookRotation(normal) * QuaterRotationX * QuaterRotationY * Quaternion.LookRotation(direction);
            DrawOnSceneViewMesh(KalderaEditorUtils.PlaneMesh, KalderaEditorUtils.ArrowMaterial, worldPosition, rotation, ArrowScale);
        }

        protected void DrawLineArrow(Vector3 startPosition, Vector3 worldPosition, Vector3 normal)
        {
            var rotation = Quaternion.LookRotation((worldPosition - startPosition).normalized) * QuaterRotationY;
            DrawOnSceneViewMesh(KalderaEditorUtils.PlaneMesh, KalderaEditorUtils.ArrowMaterial, startPosition - (rotation * Vector3.left * ArrowSize * 0.75f), rotation, new Vector3(ArrowSize, ArrowSize, ArrowSize));

            DrawOnSceneViewMesh(KalderaEditorUtils.PlaneMesh, KalderaEditorUtils.LongArrowMaterial, worldPosition - (rotation * Vector3.left * ArrowSize * 0.5f), rotation, new Vector3(ArrowSize, ArrowSize, ArrowSize));
        }

        protected void DrawOnSceneViewMesh(Mesh mesh, Material material, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            DrawUtilities.DrawSceneViewMesh(mesh, material, position, rotation, scale);
        }

        protected void DrawLine(Vector3 position, Vector3 direction, float width, float length, Color color)
        {
            Handles.color = color;
            var bodyTrs = Matrix4x4.TRS(Vector3.zero, Quaternion.LookRotation(direction), new Vector3(width, width, length));
            var bodyCorners = LineMesh
                .Select(v => position + bodyTrs.MultiplyVector(v))
                .ToArray();

            Handles.DrawAAConvexPolygon(bodyCorners);
        }

        protected void DrawHandleTextAtOffset(Vector2 screenPosition, int textIndex, GUIContent content)
        {
            var offset = new Vector2(50, 0);
            Handles.Label(ScreenToWorldPosition(screenPosition + InitialOffset + (TextOffset * textIndex) - offset), content, StylesUtility.HandleTextStyle);
        }

        protected Vector3 GetRotationSnappedPosition(Vector3 start, Vector3 end)
        {
            var drag = end - start;
            var angle = Vector3.Angle(Vector3.forward, drag.normalized);

            // We're on the other side
            if (drag.x < 0) {
                angle = -angle;
            }

            angle = Mathf.Round(angle / DirectionSnapping) * DirectionSnapping;
            var distance = drag.magnitude;

            return start + (Quaternion.Euler(0, angle, 0) * Vector3.forward * distance);
        }

        protected virtual void UpdatePlacements(Vector3 position, SelectionSettings selectionSettings, PlacementCollection placementCollection, ScenePlacer placer)
        {
            var placementPoints = GetPlacementPositions(position, selectionSettings, placementCollection, placer).ToList();

            foreach (var point in placementPoints) {
                var placementInformation = CreatePlacementInformation(position, point, selectionSettings);
                placementCollection.Placements.Add(placementInformation);
            }

            if(selectionSettings.AvoidCollisions) {
                CheckSceneCollisions(placementCollection, placer);
            } else {

            }
        }

        private void CheckSceneCollisions(PlacementCollection placementCollection, ScenePlacer placer) {
            foreach (var placement in placementCollection.Placements) {
                placement.SetColliderStatus(true);
            }

            var allCollidersInBrush = placementCollection.Placements
                .SelectMany(p => p.GameObject.GetComponentsInChildren<Collider>())
                .ToList();

            var itemsToRemove = new List<PlacementInformation>();
            var collidersToIgnore = new List<Collider>();

            foreach (var placement in placementCollection.Placements) {
                var colliderSizeFactor = placement.GetCollisionSizeFactor(placer);
                var collidersInObject = placement.GameObject.GetComponentsInChildren<Collider>();

                foreach (var placementCollider in placement.Colliders) {
                    var count = placementCollider.IsColliding(colliderSizeFactor, KalderaEditorUtils.RaycastHitCache);

                    if (count == 0) {
                        continue;
                    }

                    var allColliders = KalderaEditorUtils.RaycastHitCache.Take(count).Select(r => r.collider).ToList();

                    foreach (var collider in allColliders) {
                        if (collidersInObject.Contains(collider)) {
                            continue;
                        }

                        if (collidersToIgnore.Contains(collider)) {
                            continue;
                        }

                        if (!allCollidersInBrush.Contains(collider)) {
                            continue;
                        }

                        collidersToIgnore.AddRange(collidersInObject);
                        itemsToRemove.Add(placement);

                        break;
                    }
                }
            }

            foreach (var item in itemsToRemove) {
                item.ClearPlacementGameObject();
                placementCollection.Placements.Remove(item);
            }

            foreach (var placement in placementCollection.Placements) {
                placement.SetColliderStatus(false);
            }
        }

        private IEnumerable<Vector3> GetPlacementPositions(Vector3 position, SelectionSettings selectionSettings, PlacementCollection placementCollection, ScenePlacer placer) => 
            GetPlacementOffsetValues(position, selectionSettings, placer)
                .Take(selectionSettings.ObjectLimit)
                .DefaultIfEmpty(Vector2.zero);

        protected virtual List<PlacementInformation> PlacementsToPlace(ScenePlacer placer) => placer.PlacementCollection.Placements;

        protected List<GameObject> PlaceObjects(Vector3 position, GameObject parentObject, SelectionSettings settings, ScenePlacer placer)
        {
            var placements = PlacementsToPlace(placer);
            foreach (var placementInformation in placements) {
                if (placementInformation.GameObject == null) {
                    Debug.LogError($"GameObject is null in {nameof(PlaceObjects)}");
                    continue;
                }

                var collider = placementInformation.GameObject.GetComponent<Collider>();
                if (collider != null) {
                    collider.enabled = false;
                }
            }

            var colliderStatus = new Dictionary<Collider, bool>();

            var result = new List<GameObject>();
            foreach (var placementInformation in placements) {
                if (placementInformation.GameObject == null) {
                    continue;
                }

                if (!placementInformation.GameObject.activeSelf) {
                    continue;
                }

                var placedGameObject = PlaceObject(placementInformation, position, parentObject, settings, colliderStatus);
                if (placedGameObject != null) {
                    result.Add(placedGameObject);
                }
            }

            foreach (var key in colliderStatus.Keys) {
                key.enabled = colliderStatus[key];
            }

            return result;
        }

        private GameObject PlaceObject(PlacementInformation placementInformation, Vector3 position, GameObject parentObject, SelectionSettings settings, Dictionary<Collider, bool> colliderStatus)
        {
            var result = (GameObject)PrefabUtility.InstantiatePrefab(placementInformation.PrefabObject);

            if (placementInformation.Item.AdvancedOptions.NameType == AdvancedOptions.ItemNameType.UsePaletteItemName) {
                result.name = placementInformation.Item.Name;
            } else {
                result.name = placementInformation.GameObject.name;
            }

            result.transform.position = placementInformation.GameObject.transform.position;
            result.transform.rotation = placementInformation.GameObject.transform.rotation;
            result.transform.localScale = placementInformation.NormalizedScale * ScaleFactor;

            SetParentObject(result, parentObject, settings);
            var collider = result.GetComponent<Collider>();
            if (collider != null) {
                colliderStatus.Add(collider, collider.enabled);
                collider.enabled = false;
            }

            return result;
        }

        public virtual void CycleVariant(int delta, ScenePlacer placer)
        {
            // Default implemetation is to randomize the placement
            placer.GeneratePlacement();
        }

        public virtual void ScaleBrush(float delta, ScenePlacer placer)
        {
            ScaleFactor = Mathf.Clamp(ScaleFactor * DeltaScale(delta), MinScaleFactor, MaxScaleFactor);

            foreach (var placement in placer.PlacementCollection.Placements) {
                var validObjects = placement.Item.ValidObjects();
                var selectedVariantIndex = validObjects.IndexOf(placement.PrefabObject);
                placement.ReplacePlacementObject(selectedVariantIndex, Position, ScaleFactor);
            }
        }

        public void ResetBrushRotation()
        {
            Rotation = DefaultRotation;
        }

        private float DeltaScale(float delta)
        {
            if (delta > 0) {
                return Mathf.Pow(ScaleUpStepValue, delta);
            } else if (delta < 0) {
                return Mathf.Pow(ScaleDownStepValue, Mathf.Abs(delta));
            } else {
                return 1f;
            }
        }

        protected Vector3 GetOffsetPoint(Vector3 point) => point - Position;

        protected PlacementInformation CreatePlacementInformation(Vector3 position, Vector3 pointOffset, SelectionSettings selectionSettings)
        {
            var item = GetRandomItem(selectionSettings.SelectedItems);

            var result = GetPlacementInformation(position, item, pointOffset, ScaleFactor, item.GetObjectVariant());
            result.CreatePlacementGameObject(position, ScaleFactor);

            return result;
        }

        protected virtual Vector3 GetItemRotation(Vector3 position, PaletteItem item, GameObject prefabObject) => item.GetRotation(prefabObject);

        protected abstract List<Vector3> GetPlacementOffsetValues(Vector3 position, SelectionSettings selectionSettings, ScenePlacer placer);

        private PaletteItem GetRandomItem(List<PaletteItem> selectedItems) => selectedItems[Random.Range(0, selectedItems.Count)];

        private PlacementInformation GetPlacementInformation(Vector3 position, PaletteItem item, Vector3 offset, float scaleFactor, GameObject prefabObject)
        {
            var eulerRotation = GetItemRotation(position, item, prefabObject);
            var localScale = GetItemScale(item, prefabObject);

            return new PlacementInformation(item, prefabObject, offset, eulerRotation, localScale, scaleFactor);
        }

        private Vector3 GetItemScale(PaletteItem item, GameObject prefabObject)
        {
            var result = item.GetScale();

            if (item.AdvancedOptions.MultiplyPrefabScale) {
                result.x *= prefabObject.transform.localScale.x;
                result.y *= prefabObject.transform.localScale.y;
                result.z *= prefabObject.transform.localScale.z;
            }

            return result;
        }

        private void SetParentObject(GameObject placedObject, GameObject parentObject, SelectionSettings settings)
        {
            if(parentObject == null) {
                return;
            }

            if(!settings.ParentObjectsToBaseObject) {
                return;
            }

            placedObject.transform.parent = parentObject.transform;
        }
    }
}