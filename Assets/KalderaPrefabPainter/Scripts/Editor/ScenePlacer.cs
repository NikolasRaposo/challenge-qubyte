using CollisionBear.WorldEditor.Brushes;
using CollisionBear.WorldEditor.Distribution;
using CollisionBear.WorldEditor.RaycastModes;
using CollisionBear.WorldEditor.Utils;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace CollisionBear.WorldEditor
{
    [System.Serializable]
    public class ScenePlacer
    {
        [SerializeField]
        private ParentObjectPlacementMode ParentObjectPlacementMode = new ParentObjectPlacementMode();
        [SerializeField]
        private LayerObjectPlacementMode LayerObjectPlacementMode = new LayerObjectPlacementMode();

        [SerializeField]
        private TopdownRaycastMode TopdownRaycastMode = new TopdownRaycastMode();
        [SerializeField]
        private ViewportRaycastMode ViewportRaycastMode = new ViewportRaycastMode();

        [SerializeField]
        private SingleBrush SingleBrush = new SingleBrush();
        [SerializeField]
        private DummyBrush LineBrush = new DummyBrush("Line brush", KeyCode.None, "[Only in full version]\nPlaces multiple objects in a line from where you start drag to the end", "Icons/IconGridLine.png");

        [SerializeField]
        private DummyBrush PathBrush = new DummyBrush("Path brush", KeyCode.None, "[Only in full version]\nPlaces multiple objects in a path along the mouse drag", "Icons/IconPathTool.png");

        [SerializeField]
        private CircleBrush CircleBrush = new CircleBrush();
        [SerializeField]
        private DummyBrush SquareBrush = new DummyBrush("Square brush", KeyCode.None, "[Only in full version]\nPlaces multiple objects (always at least 1) in a square", "Icons/IconGridSquare.png");

        [SerializeField]
        private DummyBrush SprayBrush = new DummyBrush("Spray brush", KeyCode.None, "[Only in full version]\nSlowly plots down objects while keeping the mouse button pressed", "Icons/IconGridSpray.png");
        [SerializeField]
        private EraserBrush EraserBrush = new EraserBrush();
        [SerializeField]
        private ClearButton ClearButton = new ClearButton();

        private IBrushButton[] Brushes;
        private IPlacementMode[] PlacementModes;
        private IRaycastMode[] RaycastModes;
        private List<DistributionBase> Distributions;
        private GUIContent[] PlacementModeGUIContent;
        private GUIContent[] RaycastModeGUIContent;

        private List<PaletteItem> PreviousSelection;

        public IBrushButton[] GetBrushMapping()
        {
            if (Brushes == null) {
                Brushes = new IBrushButton[] {
                    SingleBrush,
                    LineBrush,
                    PathBrush,
                    CircleBrush,
                    SquareBrush,
                    SprayBrush,
                    EraserBrush,
                    ClearButton
                };

                for (int i = 0; i < Brushes.Length; i++) {
                    if (Brushes[i] is BrushBase brush) {
                        brush.Index = i;
                    }

                    Brushes[i].KeyBinding = KalderaSpawnEffectsSettings.ToolsHotKeys[i];
                }
            }

            return Brushes;
        }

        public IPlacementMode[] GetPlacementModes()
        {
            if (PlacementModes == null) {
                PlacementModes = new IPlacementMode[] {
                    new AnyColliderMode(),
                    ParentObjectPlacementMode,
                    LayerObjectPlacementMode
                };
            }

            return PlacementModes;
        }

        public IRaycastMode[] GetRaycastModes()
        {
            if (RaycastModes == null) {
                RaycastModes = new IRaycastMode[] {
                    TopdownRaycastMode,
                    ViewportRaycastMode
                };
            }

            return RaycastModes;
        }

        public IReadOnlyList<DistributionBase> GetDistributionModes()
        {
            if (Distributions == null) {
                Distributions = new List<DistributionBase> {
                    new RandomDistribution(0),
                    new PerlinNoiseDistribution(1),
                    new UniformDistribution(2),
                };
            }

            return Distributions;
        }

        public GUIContent[] GetPlacementModeGuiContent()
        {
            if (PlacementModeGUIContent == null) {

                PlacementModeGUIContent = GetPlacementModes()
                    .Select(p => new GUIContent(p.Name))
                    .ToArray();
            }

            return PlacementModeGUIContent;
        }

        public GUIContent[] GeRaycastModeGuiContent()
        {
            if (RaycastModeGUIContent == null) {

                RaycastModeGUIContent = GetRaycastModes()
                    .Select(p => new GUIContent(p.Name))
                    .ToArray();
            }

            return RaycastModeGUIContent;
        }

        public SelectionSettings SelectionSettings = new SelectionSettings();
        public PlacementCollection PlacementCollection = null;

        public Vector2 ScreenPosition;
        public Vector3 PlacementPosition;

        public IPlacementMode CurrentPlacementMode;
        public IRaycastMode CurrentRaycastMode;

        public BrushBase CurrentBrush;
        public DistributionBase CurrentDistribution;

        private bool IsHidden;

        private readonly RaycastHit[] RaycastHitsCache = new RaycastHit[128];

        public bool HasPlacementSelection = false;

        public void OnEnable()
        {
            CurrentPlacementMode = GetPlacementModes()[SelectionSettings.PlacementModeIndex];
            CurrentRaycastMode = GetRaycastModes()[SelectionSettings.RaycastModeIndex];
            CurrentBrush = GetCurrentbrushFromIndex(SelectionSettings.SelectedBrushIndex);
            CurrentDistribution = GetDistributionModes()[SelectionSettings.SelectedDistributionIndex];
        }

        public void NotifyChange()
        {
            CurrentBrush = GetCurrentbrushFromIndex(SelectionSettings.SelectedBrushIndex);
            CurrentBrush.OnSelected(this);
            CurrentDistribution = GetDistributionModes()[SelectionSettings.SelectedDistributionIndex];
            GeneratePlacementInformation(SelectionSettings, ScreenPosition, PlacementPosition);
        }

        private BrushBase GetCurrentbrushFromIndex(int index)
        {
            var result = GetBrushMapping()[index];

            if (result is BrushBase brushBase) {
                return brushBase;
            } else {
                return null;
            }
        }

        public void ClearSelection()
        {
            if (CurrentBrush != null) {
                CurrentBrush.ResetBrushRotation();
                CurrentBrush.OnClearSelection(this);
            }

            PreviousSelection = new List<PaletteItem>(SelectionSettings.SelectedItems);

            SelectionSettings.ClearSelection();
            DestroyPlacementObjects();
            PlacementCollection = null;
            IsHidden = true;
        }

        public void RestoreSelection()
        {
            IsHidden = false;
            Brushes[SelectionSettings.SelectedBrushIndex].OnSelected(this);
            if (PreviousSelection == null || PreviousSelection.Count == 0) {
                return;
            }

            SelectionSettings.SelectedItems = new List<PaletteItem>(PreviousSelection);
            GeneratePlacement();
        }

        public bool IsCleared() => (PlacementCollection?.Placements?.Count ?? 0) == 0;

        public Vector3? GetInWorldPoint(Ray ray)
        {
            var hitCount = Physics.RaycastNonAlloc(ray, RaycastHitsCache, 10000, int.MaxValue, QueryTriggerInteraction.Ignore);
            return CurrentPlacementMode.IsValidPlacement(RaycastHitsCache, hitCount, PlacementCollection)?.point;
        }

        public void MovePosition(Vector2 screenPosition, Vector3 position)
        {
            UnhidePlacement();

            ScreenPosition = screenPosition;
            PlacementPosition = position;

            // Avoids crashes if the data is lost due to recompilation
            if (SelectionSettings == null) {
                ClearSelection();
                return;
            }

            CurrentBrush.MoveBrush(PlacementPosition, CurrentRaycastMode.GetNormal(position), this);
            UpdatePlacements();
        }

        public void UpdatePlacements()
        {
            if (PlacementCollection == null) {
                return;
            }

            foreach (var placementInformation in PlacementCollection.Placements) {
                if (placementInformation.GameObject == null) {
                    continue;
                }

                placementInformation.GameObject.SetActive(true);
                var objectPosition = CurrentBrush.Position + placementInformation.NormalizedOffset;

                var objectRotation = placementInformation.Rotation;

                var raycastHit = GetTerrainRaycastHit(objectPosition);

                if(raycastHit.HasValue) {
                    placementInformation.GroundGameObject = raycastHit.Value.collider.gameObject;
                } else {
                    placementInformation.GroundGameObject = null;
                }

                if (placementInformation.Item.AdvancedOptions.UseIndividualGroundHeight || SelectionSettings.OrientToGroundNormal) {
                    var heightOffet = new Vector3(0, placementInformation.Offset.y, 0);

                    if (raycastHit.HasValue) {
                        if (placementInformation.Item.AdvancedOptions.UseIndividualGroundHeight) {
                            var transformedOffset = Quaternion.LookRotation(CurrentRaycastMode.GetNormal(CurrentBrush.Position)) * Quaternion.Euler(-90, 0, 0) * heightOffet;
                            objectPosition = raycastHit.Value.point + transformedOffset;
                        }

                        if (SelectionSettings.OrientToGroundNormal) {
                            objectRotation = Quaternion.LookRotation(raycastHit.Value.normal, Vector3.back) * Quaternion.Euler(90, 0, 0) * placementInformation.Rotation;
                        }
                    }
                }

                UpdatePosition(placementInformation, objectPosition);
                placementInformation.GameObject.transform.rotation = objectRotation;
                if (raycastHit.HasValue) {
                    placementInformation.SetPlaceOutsideWorld(false);
                } else {
                    placementInformation.SetPlaceOutsideWorld(true);
                }
            }
        }

        public void UpdatePosition(PlacementInformation placementInformation, Vector3 position) {
            placementInformation.GameObject.transform.position = position;

            if(!placementInformation.HasColliders()) {
                return;
            }

            CheckSceneCollision();
        }

        private void GeneratePlacementInformation(SelectionSettings settings, Vector2 screenPosition, Vector3 worldPosition)
        {
            DestroyPlacementObjects();

            PlacementCollection = CurrentBrush.GeneratePlacementForBrush(worldPosition, settings, this);
            RotatatePlacement(CurrentBrush.Rotation, CurrentBrush.Normal);

            if (IsHidden) {
                PlacementCollection.Hide();
            } else {
                MovePosition(ScreenPosition, worldPosition);
            }
        }

        public void DestroyPlacementObjects()
        {
            if (PlacementCollection == null) {
                return;
            }

            foreach (var placementInformationObject in PlacementCollection.Placements) {
                GameObject.DestroyImmediate(placementInformationObject.GameObject);
            }
            HasPlacementSelection = false;
        }

        private RaycastHit? GetTerrainRaycastHit(Vector3 position)
        {
            var ray = CurrentRaycastMode.GetRay(position);
            var hitCount = Physics.RaycastNonAlloc(ray, RaycastHitsCache, CurrentRaycastMode.GetMaxDistance() + 1f);    // Adds 1f to ensure the ray does not stop precisely where the hit obejct is supposed to be
            var result = CurrentPlacementMode.IsValidPlacement(RaycastHitsCache, hitCount, PlacementCollection);

            return result;
        }

        public Vector3 RotateTowardsPosition(Vector3 position)
        {
            if (PlacementCollection == null || !PlacementCollection.HasItems()) {
                return CurrentBrush.Rotation;
            }

            PlacementCollection.RotateTowardsPosition(position);
            return CurrentRaycastMode.GetRotationDirection(CurrentBrush.Position, position);
        }

        public Vector3 RotatatePlacementInPlace(Vector3 position, Vector3 normal)
        {
            if (PlacementCollection == null || !PlacementCollection.HasItems()) {
                return CurrentBrush.Rotation;
            }

            var rotation = CurrentRaycastMode.GetRotationDirection(CurrentBrush.Position, position);
            RotatatePlacement(rotation, normal);
            return rotation;
        }

        public void RotatatePlacement(Vector3 rotation, Vector3 normal)
        {
            PlacementCollection.RotatePlacement(rotation, normal, this);
            UpdatePlacements();
        }

        public void StartPlacement(Vector2 screenPosition)
        {
            ScreenPosition = screenPosition;
            CurrentBrush.StartPlacement(PlacementPosition, this);
        }

        public void ActiveDragPlacement(Vector3 worldPosition, double deltaTime)
        {
            CurrentBrush.ActiveDragPlacement(worldPosition, SelectionSettings, deltaTime, this);
        }

        public void ShiftDragPlacement(Vector3 worldPosition, double deltaTime)
        {
            CurrentBrush.ShiftDragPlacement(worldPosition, SelectionSettings, deltaTime, this);
        }

        public void ShiftHoverPlacement(Vector3 worldPosition, double deltaTime)
        {
            CurrentBrush.ShiftHoverPlacement(worldPosition, SelectionSettings, deltaTime, this);
            UpdatePlacements();
        }

        public void PassiveDragPlacement(Vector3 worldPosition, double deltaTime)
        {
            CurrentBrush.StaticDragPlacement(worldPosition, SelectionSettings, deltaTime, this);
        }

        public void EndPlacement()
        {
            var placedGameObjects = CurrentBrush.EndPlacement(PlacementPosition, CurrentPlacementMode.ParentObject, SelectionSettings, this);

            if (placedGameObjects.Count == 0) {
                return;
            }

            foreach (var placedObject in placedGameObjects) {
                Undo.RegisterCreatedObjectUndo(placedObject, "Placed object");
                SpawnEffects.RegisterObject(placedObject);
            }
        }

        public void GeneratePlacement()
        {
            GeneratePlacementInformation(SelectionSettings, ScreenPosition, PlacementPosition);
            HasPlacementSelection = true;
        }

        public void DrawBrushHandle(Vector3 adjustedInworldPosition)
        {
            if (IsHidden) {
                return;
            }

            DrawColliders();

            CurrentBrush.DrawBrushHandle(PlacementPosition, adjustedInworldPosition);
            CurrentBrush.DrawSceneHandleText(ScreenPosition, PlacementPosition, this);
        }

        private void DrawColliders() {
            if (!PlacementCollection.HasItems()) {
                return;
            }

            foreach (var placement in PlacementCollection.Placements) {
                if (placement.GameObject == null) {
                    continue;
                }

                var colliderSizeFactor = placement.GetCollisionSizeFactor(this);
                if (placement.IsColliding()) {
                    foreach (var collider in placement.Colliders) {
                        collider.DrawCollider(colliderSizeFactor);
                    }
                }
            }
        }

        private void CheckSceneCollision() {
            if (!PlacementCollection.HasItems()) {
                return;
            }

            foreach (var placement in PlacementCollection.Placements) {
                if (placement.GameObject == null) {
                    continue;
                }

                if (!SelectionSettings.AvoidCollisions || placement.Item.AdvancedOptions.AllowCollision) {
                    placement.SetCollision(false);
                    continue;
                }

                var colliderSizeFactor = placement.GetCollisionSizeFactor(this);
                var anyCollision = false;
                foreach (var collider in placement.Colliders) {
                    var hitCount = collider.IsColliding(colliderSizeFactor, KalderaEditorUtils.RaycastHitCache);

                    var collisionFound = false;
                    for (int i = 0; i < hitCount; i++) {
                        var collidingGameObject = KalderaEditorUtils.RaycastHitCache[i].collider.gameObject;

                        if (collidingGameObject != placement.GroundGameObject) {
                            collisionFound = true;
                            break;
                        }
                    }

                    if (collisionFound) {
                        anyCollision = true;
                        break;
                    }
                }

                placement.SetCollision(anyCollision);
            }
        }

        public void HidePlacement()
        {
            if (PlacementCollection != null) {
                PlacementCollection.Hide();
            }

            IsHidden = true;
        }

        public void UnhidePlacement()
        {
            if (!IsHidden) {
                return;
            }

            if (PlacementCollection != null) {
                PlacementCollection.Show();
            }

            IsHidden = false;
        }
    }
}