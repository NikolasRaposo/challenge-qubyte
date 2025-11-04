using CollisionBear.WorldEditor.Brushes;
using CollisionBear.WorldEditor.Extensions;
using CollisionBear.WorldEditor.Utils;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager.UI;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CollisionBear.WorldEditor
{
    public class PaletteWindow : EditorWindow
    {
        const float LeftOffset = 2;
        const float ButtonPadding = 3;
        const float RightOffset = 24;
        const float ScrollBarWidth = 16;

        static readonly Vector2 WindowMinSize = new Vector2(200, 150);

        [MenuItem(KalderaEditorUtils.WindowBasePath)]
        public static void ShowWindow()
        {
            var window = GetWindow<PaletteWindow>();
            window.minSize = WindowMinSize;
            window.titleContent = KalderaEditorUtils.TitleGuiContent;
            window.ShowUtility();
        }

        public static void RefreshAllWindows()
        {
            foreach (var window in Resources.FindObjectsOfTypeAll<PaletteWindow>()) {
                window.UpdateAvailableAssets();
                window.Repaint();
            }
        }

        [SerializeField]
        private SelectableAsset SelectedAsset;

        [SerializeField]
        private Palette SelectedPalette;

        [SerializeField]
        private ScenePlacer ScenePlacer = new ScenePlacer();

        [SerializeField]
        public SelectionSettings SelectionSettings = new SelectionSettings();

        [SerializeField]
        private Vector2 CurrentWindowScroll;

        private Dictionary<KeyCode, Palette> ShortKeysIndexMapping;

        [SerializeField]
        private bool IsPlacementModeHintsOpen;
        [SerializeField]
        private bool IsRaycastModeHintsOpen;

        [SerializeField]
        private List<SelectableAsset> AvailableAssets = new List<SelectableAsset>();

        [SerializeField]
        private GUIContent[] AvailableAssetGuiContent = new GUIContent[0];

        [SerializeField]
        private List<Palette> AvailablePalettes = new List<Palette>();

        [SerializeField]
        private GUIContent[] AvailablePalettesGuiContent = new GUIContent[0];

        private bool IsMousePressed = false;
        private double LastTimestamp;

        void OnEnable()
        {
            wantsMouseEnterLeaveWindow = true;

            if (ScenePlacer == null) {
                ScenePlacer = new ScenePlacer();
            }

            ScenePlacer.OnEnable();

            LastTimestamp = EditorApplication.timeSinceStartup;
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

            SceneView.duringSceneGui += OnSceneView;
            EditorSceneManager.sceneClosing += SceneChange;
            AssemblyReloadEvents.afterAssemblyReload += ClearSelection;

            UpdateAvailableAssets();
        }

        void OnDisable()
        {
            ClearSelection();

            SceneView.duringSceneGui -= OnSceneView;
            EditorSceneManager.sceneClosing -= SceneChange;
            AssemblyReloadEvents.afterAssemblyReload -= ClearSelection;
        }

        public void UpdateAvailableAssets()
        {
            UpdateShortKeys();
            AvailableAssets = GetAvailableSelectableAssets();
            AvailableAssetGuiContent = GetSelectableAssetsContent(AvailableAssets);

            // TODO: Repeated code from 412. Unify
            if (SelectedAsset is PaletteSet paletteSet) {
                AvailablePalettes = paletteSet.Categories
                .Where(c => c != null)
                .ToList();

                AvailablePalettesGuiContent = AvailablePalettes
                    .Select(c => new GUIContent(c.name))
                    .ToArray();
            }

            if (!AvailablePalettes.Contains(SelectedPalette)) {
                if (AvailablePalettes.Count == 0) {
                    SelectedPalette = null;
                } else {
                    SelectedPalette = AvailablePalettes[0];
                }
            }
        }

        protected void SceneChange(Scene scene, bool removingScene)
        {
            ClearSelection();
        }

        protected void SelectionChanged()
        {
            ClearSelection();
        }

        protected bool IsSelectableAsset(SelectableAsset selectedAsset)
        {
            if (selectedAsset == null || !(selectedAsset is PaletteSet)) {
                return false;
            }

            var selectedAssetSet = (PaletteSet)SelectedAsset;
            if (selectedAssetSet.Categories == null) {
                return false;
            }

            return true;
        }

        protected void UpdateShortKeys()
        {
            ShortKeysIndexMapping = new Dictionary<KeyCode, Palette>();

            if (!IsSelectableAsset(SelectedAsset)) {
                return;
            }

            var selectedAssetSet = (PaletteSet)SelectedAsset;
            for (int i = 0; i < selectedAssetSet.Categories.Count; i++) {
                var category = selectedAssetSet.Categories[i];
                if (category == null) {
                    continue;
                }

                if (category.ShortKey != KeyCode.None && !ShortKeysIndexMapping.ContainsKey(category.ShortKey)) {
                    ShortKeysIndexMapping.Add(category.ShortKey, selectedAssetSet.Categories[i]);
                }
            }
        }

        public void ClearSelection()
        {
            ScenePlacer.ClearSelection();
            Repaint();
        }

        public void OnSceneView(SceneView sceneView)
        {
            sceneView.Repaint();

            Event currentEvent = Event.current;

            if (!ScenePlacer.CurrentBrush.ShowBrush(ScenePlacer)) {
                if (IsSpaceKey(currentEvent)) {
                    ScenePlacer.RestoreSelection();
                    currentEvent.Use();
                    Repaint();
                }

                return;
            }

            if (ScenePlacer.CurrentBrush == null) {
                ClearSelection();
                return;
            }

            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
            var adjustedInworldPosition = ScenePlacer.GetInWorldPoint(currentEvent.GUIPointToRay());
            if (adjustedInworldPosition.HasValue) {
                ScenePlacer.DrawBrushHandle(adjustedInworldPosition.Value);
            }

            HandleInput(currentEvent, adjustedInworldPosition);
            HandleShortCuts(currentEvent);
        }

        private void HandleInput(Event currentEvent, Vector3? adjustedInworldPosition)
        {
            if (currentEvent.type == EventType.Layout) {
                return;
            } else if (currentEvent.type == EventType.MouseEnterWindow) {
                IsMousePressed = false;
                ScenePlacer.UpdatePlacements();
                currentEvent.Use();
                return;
            } else if (currentEvent.type == EventType.MouseLeaveWindow) {
                // Ensures any drag motions is stopped if the mouse pointer leaves Unity's editor window
                if (IsMousePressed) {
                    ScenePlacer.EndPlacement();
                    ScenePlacer.DestroyPlacementObjects();
                    currentEvent.Use();
                    return;
                }
            } else

            if (currentEvent.isMouse && currentEvent.button == 0) {
                if (currentEvent.type == EventType.MouseDown) {
                    IsMousePressed = true;
                } else if (currentEvent.type == EventType.MouseUp) {
                    IsMousePressed = false;
                }
            }

            if (!adjustedInworldPosition.HasValue) {
                return;
            }

            HandleToolInput(currentEvent, adjustedInworldPosition.Value);
        }

        private void HandleToolInput(Event currentEvent, Vector3 adjustedInworldPosition)
        {
            if (currentEvent.type == EventType.Repaint) {
                return;
            }

            var deltaTime = EditorApplication.timeSinceStartup - LastTimestamp;
            LastTimestamp = EditorApplication.timeSinceStartup;

            // Resumes brush after placement
            if (currentEvent.isMouse) {
                if (!ScenePlacer.HasPlacementSelection) {
                    ScenePlacer.GeneratePlacement();
                    ScenePlacer.MovePosition(currentEvent.mousePosition, adjustedInworldPosition);
                }
            }

            // Start placement 
            if (!currentEvent.shift && currentEvent.isMouse && currentEvent.IsPureMouseStartClick()) {
                ScenePlacer.StartPlacement(currentEvent.mousePosition);
                currentEvent.Use();
                return;
            }

            // Active drag
            if (!currentEvent.shift && currentEvent.isMouse && currentEvent.IsPureMouseDrag()) {
                ScenePlacer.ActiveDragPlacement(adjustedInworldPosition, deltaTime);
                ScenePlacer.UpdatePlacements();
                currentEvent.Use();
                return;
            }

            // Toggling shift key
            if (currentEvent.keyCode == KeyCode.LeftShift && IsMousePressed && !currentEvent.alt && !currentEvent.control) {
                if (currentEvent.type == EventType.KeyDown) {
                    ScenePlacer.ShiftDragPlacement(adjustedInworldPosition, deltaTime);
                    ScenePlacer.UpdatePlacements();
                    currentEvent.Use();
                    return;
                } else if (currentEvent.type == EventType.KeyUp) {
                    ScenePlacer.ActiveDragPlacement(adjustedInworldPosition, deltaTime);
                    ScenePlacer.UpdatePlacements();
                    currentEvent.Use();
                    return;
                }
            }

            // Passive drag 
            if (!currentEvent.shift && IsMousePressed && currentEvent.NoModifiers()) {
                ScenePlacer.PassiveDragPlacement(adjustedInworldPosition, deltaTime);
                currentEvent.Use();
                return;
            }

            // Shift drag
            if (currentEvent.shift && !currentEvent.alt && !currentEvent.control) {
                if (IsMousePressed) {
                    ScenePlacer.ShiftDragPlacement(adjustedInworldPosition, deltaTime);
                    currentEvent.Use();
                    return;
                }
            }

            // End placement
            if (currentEvent.isMouse && currentEvent.IsPureMouseEndClick()) {
                ScenePlacer.EndPlacement();
                ScenePlacer.DestroyPlacementObjects();
                currentEvent.Use();
                return;
            }

            // Cycle prefab variant tool 
            if (KalderaSpawnEffectsSettings.CyclePrefabHotkey.EventMatch(currentEvent)) {
                ScenePlacer.CurrentBrush.CycleVariant(KalderaSpawnEffectsSettings.CyclePrefabHotkey.GetDelta(currentEvent), ScenePlacer);
                ScenePlacer.MovePosition(currentEvent.mousePosition, ScenePlacer.PlacementPosition);
                currentEvent.Use();
                return;
            }

            // Scale tool 
            if (KalderaSpawnEffectsSettings.ScalePrefabHotkey.EventMatch(currentEvent)) {
                ScenePlacer.CurrentBrush.ScaleBrush(-currentEvent.delta.y, ScenePlacer);
                ScenePlacer.UpdatePlacements();
                currentEvent.Use();
                return;
            }

            // Hover
            if (currentEvent.shift && !currentEvent.alt && !currentEvent.control) {
                if (!IsMousePressed) {
                    ScenePlacer.ShiftHoverPlacement(adjustedInworldPosition, deltaTime);
                    currentEvent.Use();
                    return;
                }
            }

            // Move
            if (currentEvent.NoModifiers()) {
                ScenePlacer.MovePosition(currentEvent.mousePosition, adjustedInworldPosition);
            }
        }

        private bool IsSpaceKey(Event currentEvent) => currentEvent.isKey && currentEvent.keyCode == KeyCode.Space && currentEvent.type == EventType.KeyDown;

        private void HandleShortCuts(Event currentEvent)
        {
            if (currentEvent.type == EventType.Layout || currentEvent.type == EventType.Repaint) {
                return;
            }

            if (ScenePlacer?.CurrentBrush == null) {
                return;
            }

            if (EditorGUIUtility.editingTextField) {
                return;
            }

            //if (currentEvent.type == EventType.KeyDown) {
            //    return;
            //}

            if (currentEvent.shift) {
                foreach (var key in ShortKeysIndexMapping.Keys) {
                    if (currentEvent.keyCode == key) {
                        SelectedPalette = ShortKeysIndexMapping[key];
                        currentEvent.Use();
                        return;
                    }
                }
            }

            foreach (var brush in ScenePlacer.GetBrushMapping()) {
                if (brush.KeyBinding.EventMatch(currentEvent)) {
                    PressBrushFromHotkey(brush, currentEvent);
                    return;
                }
            }

            if (ScenePlacer.CurrentBrush.HandleKeyEvents(currentEvent, ScenePlacer)) {
                currentEvent.Use();
                return;
            }
        }

        public void PressBrushFromHotkey(IBrushButton button, Event currentEvent)
        {
            button.OnButtonPress(this);
            NotifyChange();
            currentEvent.Use();
        }

        void OnGUI()
        {
            //OnGuiClearBrush(Event.current);
            HandleShortCuts(Event.current);

            DrawSelectedAsset();
            DrawSelectedCategory();
            EditorGUILayout.Space();

            using (new EditorGUILayout.VerticalScope(GUI.skin.box)) {
                DrawSetPlacementMode();
                DrawSetRaycastMode();
            }

            if (!ValidatePlacementSettings()) {
                return;
            }

            EditorGUILayout.Space();
            DrawBrushTools();
            EditorGUILayout.Space();
            DrawScrollWrapper();

            DrawVersionFooter();
            DrawLiteVersionNote();
        }

        private void DrawSelectedAsset()
        {
            SetSelectedAsset(GetSelectableAsset());
        }

        private void DrawSelectedCategory() {
            var tempPalette = GetSelectedWindowCategory(SelectedAsset);

            if (tempPalette != SelectedPalette) {
                LoadPalette(tempPalette);
                SelectedPalette = tempPalette;
            }
        }

        private void LoadPalette(Palette palette) {
            if(palette == null) {
                return;
            }

            var allAssets = palette.Groups
                .SelectMany(g => g.Items)
                .SelectMany(i => i.GameObjectVariants)
                .Where(g => g != null)
                .ToList();

            foreach(var asset in allAssets) {
                var path = AssetDatabase.GetAssetPath(asset);
                AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }
        }

        private void SetSelectedAsset(SelectableAsset newSelectedAsset)
        {
            if (SelectedAsset == newSelectedAsset) {
                return;
            }

            SelectedAsset = newSelectedAsset;
            UpdateShortKeys();

            if (SelectedAsset == null) {
                return;
            }

            if (!(SelectedAsset is PaletteSet paletteCollection)) {
                AvailablePalettes = new List<Palette>();
                AvailablePalettesGuiContent = new GUIContent[0];
                return;
            }

            AvailablePalettes = paletteCollection.Categories
                .Where(c => c != null)
                .ToList();

            AvailablePalettesGuiContent = AvailablePalettes
                .Select(c => new GUIContent(c.name))
                .ToArray();
        }

        private void DrawSetPlacementMode()
        {
            using (new EditorGUILayout.HorizontalScope()) {
                var placementModeIndex = EditorGUILayout.Popup(KalderaEditorUtils.SelectPlacementModeContent, ScenePlacer.SelectionSettings.PlacementModeIndex, ScenePlacer.GetPlacementModeGuiContent());
                if (placementModeIndex != ScenePlacer.SelectionSettings.PlacementModeIndex) {
                    SetPlacementMode(placementModeIndex);
                }

                EditorGUILayout.LabelField(KalderaEditorUtils.MoreInformationContent, GUILayout.Width(14));
                IsPlacementModeHintsOpen = EditorGUILayout.Toggle(IsPlacementModeHintsOpen, StylesUtility.BoldFoldoutStyle, GUILayout.Width(14));
            }

            if (IsPlacementModeHintsOpen) {
                EditorGUILayout.HelpBox($"{ScenePlacer.CurrentPlacementMode.Name}\n{ScenePlacer.CurrentPlacementMode.HintText}", MessageType.Info);
            }

            ScenePlacer.CurrentPlacementMode.DrawEditor(this);
            EditorGUILayout.Space();
        }

        private void DrawSetRaycastMode()
        {
            using (new EditorGUILayout.HorizontalScope()) {
                var raycastModeIndex = EditorGUILayout.Popup(KalderaEditorUtils.SelectRaycastModeContent, ScenePlacer.SelectionSettings.RaycastModeIndex, ScenePlacer.GeRaycastModeGuiContent());
                if (raycastModeIndex != ScenePlacer.SelectionSettings.RaycastModeIndex) {
                    SetRaycastNode(raycastModeIndex);
                }

                EditorGUILayout.LabelField(KalderaEditorUtils.MoreInformationContent, GUILayout.Width(14));
                IsRaycastModeHintsOpen = EditorGUILayout.Toggle(IsRaycastModeHintsOpen, StylesUtility.BoldFoldoutStyle, GUILayout.Width(14));
            }

            if (IsRaycastModeHintsOpen) {
                EditorGUILayout.HelpBox($"{ScenePlacer.CurrentRaycastMode.Name}\n{ScenePlacer.CurrentRaycastMode.HintText}", MessageType.Info);
            }

            ScenePlacer.CurrentRaycastMode.DrawEditor(this);
            EditorGUILayout.Space();
        }

        private void DrawScrollWrapper()
        {
            using (var scrollScope = new EditorGUILayout.ScrollViewScope(CurrentWindowScroll)) {
                CurrentWindowScroll = scrollScope.scrollPosition;

                if (SelectedPalette == null) {
                    return;
                }

                DrawCategory(SelectedPalette);
            }
        }

        private void DrawLiteVersionNote()
        {
            using(new EditorGUILayout.VerticalScope(EditorStyles.helpBox)) {
                EditorGUILayout.LabelField("This is free and trimmed down version of Kaldera Prefab Painter.");
                EditorGUILayout.LabelField("You can purchase the full version from Unity's Asset Store.");
                if(GUILayout.Button("Kaldera Prefab Painter (Full version)", EditorStyles.linkLabel)) {
                    Application.OpenURL("https://assetstore.unity.com/packages/tools/level-design/kaldera-prefab-painter-249492");
                }
            }
        }

        private List<SelectableAsset> GetAvailableSelectableAssets()
        {
            return AssetDatabase.FindAssets(string.Format("t:{0}", typeof(SelectableAsset).FullName))
                .Select(a => AssetDatabase.GUIDToAssetPath(a))
                .Select(a => AssetDatabase.LoadAssetAtPath<SelectableAsset>(a))
                .OrderBy(a => a.name)
                .ToList();
        }

        private GUIContent[] GetSelectableAssetsContent(List<SelectableAsset> selectableAssets)
        {
            return selectableAssets
            .Select(a => new GUIContent(a.GetName()))
            .ToArray();
        }

        private void SetPlacementMode(int placementModeIndex)
        {
            ScenePlacer.CurrentPlacementMode = ScenePlacer.GetPlacementModes()[placementModeIndex];
            ScenePlacer.SelectionSettings.PlacementModeIndex = placementModeIndex;
        }

        private void SetRaycastNode(int raycastModeIndex)
        {
            ScenePlacer.CurrentRaycastMode = ScenePlacer.GetRaycastModes()[raycastModeIndex];
            ScenePlacer.SelectionSettings.RaycastModeIndex = raycastModeIndex;
        }

        public void SetBrushType(BrushBase brush)
        {
            ScenePlacer.SelectionSettings.SelectedBrushIndex = brush.Index;
            Repaint();
        }

        private bool ValidatePlacementSettings()
        {
            var result = true;

            if (AvailableAssets.Count == 0) {
                return false;
            }

            if (SelectedAsset == null) {
                EditorGUILayout.HelpBox(KalderaEditorUtils.SelectPaletteToolTip, MessageType.Warning);
                result = false;
            }

            var validatePlacementMessage = ScenePlacer.CurrentPlacementMode.ValidatePlacementMode();
            if (validatePlacementMessage != null) {
                EditorGUILayout.HelpBox(validatePlacementMessage, MessageType.Warning);
                result = false;
            }

            return result;
        }

        private SelectableAsset GetSelectableAsset()
        {
            if (AvailableAssets.Count == 0) {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox(KalderaEditorUtils.NoPaletteToolTip, MessageType.Warning);
                EditorGUILayout.Space();
                if (GUILayout.Button(KalderaEditorUtils.CreatePaletteButton)) {
                    CreateDefaultPalette();
                }

                EditorGUILayout.Space();
                return null;
            }

            var currentIndex = Mathf.Max(AvailableAssets.IndexOf(SelectedAsset), 0);

            if (currentIndex >= AvailableAssetGuiContent.Length) {
                return AvailableAssets[AvailableAssetGuiContent.Length - 1];
            }

            using (new EditorGUILayout.HorizontalScope()) {
                var selectedIndex = EditorGUILayout.Popup(GetPaletteSelectionContent(AvailableAssets, currentIndex), currentIndex, AvailableAssetGuiContent);
                if (GUILayout.Button(KalderaEditorUtils.ShowInProjectContent, GUILayout.Width(KalderaEditorUtils.MiniButtonWidth), GUILayout.Height(KalderaEditorUtils.MiniButtonHeight))) {
                    ShowAssetInProject(AvailableAssets[selectedIndex]);
                }

                return AvailableAssets[selectedIndex];
            }
        }

        private void CreateDefaultPalette() {
            var palette = CreateInstance<Palette>();
            AssetDatabase.CreateAsset(palette, $"Assets/{KalderaEditorUtils.DefaultPaletteName}");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = palette;
        }

        private Palette GetSelectedWindowCategory(SelectableAsset selectedAsset)
        {
            if (selectedAsset == null) {
                using (new EditorGUI.DisabledGroupScope(true)) {
                    EditorGUILayout.LabelField(KalderaEditorUtils.SelectPaletteContent);
                }

                return null;
            } else if (selectedAsset is Palette windowCategory) {
                EditorGUILayout.LabelField(GUIContent.none);

                return windowCategory;
            } else if (selectedAsset is PaletteSet windowSet) {
                return CategoryDropdownFromSet(windowSet);
            } else {
                Debug.LogError($"Selected Palette {selectedAsset.name} is neither a Palette nor a Palette Collection");
                return null;
            }
        }

        private void ShowAssetInProject(SelectableAsset selectableAsset)
        {
            EditorGUIUtility.PingObject(selectableAsset);
            Selection.activeObject = selectableAsset;
        }

        private GUIContent GetPaletteSelectionContent(List<SelectableAsset> availableAssets, int index)
        {
            if (availableAssets[index] is Palette) {
                return KalderaEditorUtils.SelectPaletteContent;
            } else {
                return KalderaEditorUtils.SelectPaletteCollectionContent;
            }
        }

        protected Palette CategoryDropdownFromSet(PaletteSet paletteCollection)
        {
            if (AvailablePalettes.Count == 0) {
                EditorGUILayout.HelpBox(KalderaEditorUtils.EmptySetTooltip, MessageType.Warning);
                return null;
            }

            var currentItem = 0;
            if (SelectedPalette != null) {
                currentItem = AvailablePalettes.IndexOf(SelectedPalette);
            }

            using (new EditorGUILayout.HorizontalScope()) {
                var currentIndex = Mathf.Max(0, EditorGUILayout.Popup(KalderaEditorUtils.PaletteLabelContent, currentItem, AvailablePalettesGuiContent));
                if (GUILayout.Button(KalderaEditorUtils.ShowInProjectContent, GUILayout.Width(KalderaEditorUtils.MiniButtonWidth), GUILayout.Height(KalderaEditorUtils.MiniButtonHeight))) {
                    ShowAssetInProject(AvailablePalettes[currentIndex]);
                }

                return AvailablePalettes[currentIndex];
            }
        }

        protected void DrawCategory(Palette palette)
        {
            using (new EditorGUILayout.VerticalScope(GUI.skin.box)) {
                EditorGUILayout.LabelField(KalderaEditorUtils.PaletteLabelContent, EditorStyles.boldLabel);
                if (palette.HasAnyGroupWithItems()) {
                    foreach (var group in palette.Groups) {
                        DrawGroup(palette, group);
                    }
                    EditorGUILayout.Space();
                } else {
                    EditorGUILayout.HelpBox(KalderaEditorUtils.NoPaletteGroups, MessageType.Info);
                    if (GUILayout.Button(KalderaEditorUtils.GotoPaletteText, EditorStyles.miniButton)) {
                        EditorGUIUtility.PingObject(SelectedPalette);
                        Selection.activeObject = SelectedPalette;
                    }
                    EditorGUILayout.Space();
                }

                EditorGUILayout.LabelField(KalderaEditorUtils.MultiSelectTooltip, EditorStyles.miniLabel);
            }
        }

        protected void DrawGroup(Palette palette, PaletteGroup group) {
            var windowWidth = GetWindowWidth();
            var buttonsInRow = GetButtonCountInRow(windowWidth, KalderaEditorUtils.IconButtonSize);

            var totalIndex = 0;
            var currentIndex = 0;
            var currentRow = 0;

            if (!string.IsNullOrEmpty(group.GroupName)) {
                EditorGUILayout.LabelField(group.GroupName);
            } else {
                EditorGUILayout.Space();
            }

            while (totalIndex < group.Items.Count) {
                using (new GUI.GroupScope(GUILayoutUtility.GetRect(windowWidth, KalderaEditorUtils.IconButtonSize))) {
                    for (int rowIndex = 0; rowIndex < buttonsInRow; rowIndex++) {
                        if (totalIndex >= group.Items.Count) {
                            break;
                        }

                        if (DrawItem(palette, group, rowIndex, currentIndex)) {
                            currentIndex++;
                        }

                        totalIndex++;
                    }

                    GUI.color = Color.white;
                }
                currentRow++;
            }
        }

        private bool DrawItem(Palette palette, PaletteGroup group, int rowIndex, int totalIndex)
        {
            if (!group.Items[totalIndex].HasVariants()) {
                return false;
            }

            var drawRectatangle = GetRectForPosition(rowIndex, KalderaEditorUtils.IconButtonSize);

            var paletteItem = group.Items[totalIndex];

            // Make sure the currently selected button is blue
            EditorCustomGUILayout.SetGuiColorState(ScenePlacer.SelectionSettings.SelectedItems.Contains(paletteItem));

            var guiContent = PreviewRenderingUtility.GetGuiContentForItem(paletteItem);
            if (guiContent == null) {
                return false;
            }

            if (GUI.Button(drawRectatangle, guiContent, StylesUtility.IconButtonStyle)) {
                if (Event.current.button == 0) {
                    SelectItem(paletteItem);
                } else if (Event.current.button == 1) {
                    OpenPrefabInPaletteAsset(palette, group, paletteItem);
                } else if (Event.current.button == 2) {
                    OpenPrefabItem(paletteItem);
                }
            }

            return true;
        }

        private void DrawVersionFooter()
        {
            EditorGUILayout.LabelField(KalderaEditorUtils.VersionContent);
        }

        private float GetWindowWidth() => position.width - (LeftOffset + RightOffset + ScrollBarWidth);
        private int GetButtonCountInRow(float windowWidth, float iconButtonSize) => Mathf.FloorToInt(windowWidth / iconButtonSize);

        private Rect GetRectForPosition(int rowIndex, float iconButtonSize)
        {
            return new Rect(LeftOffset + (rowIndex * (KalderaEditorUtils.IconButtonSize + ButtonPadding)), 0, iconButtonSize, iconButtonSize);
        }

        private void SelectItem(PaletteItem assetItem)
        {
            if (Event.current.shift) {
                ScenePlacer.SelectionSettings.ToggleSelectedItem(assetItem);
            } else {
                ScenePlacer.SelectionSettings.SetSelectedItem(assetItem);
            }
            NotifyChange();
        }

        private void OpenPrefabItem(PaletteItem assetItem)
        {
            if (!assetItem.HasVariants()) {
                return;
            }

            var variantObject = assetItem.FirstObject();
            EditorGUIUtility.PingObject(variantObject);
            AssetDatabase.OpenAsset(variantObject);
        }

        private void OpenPrefabInPaletteAsset(Palette palette, PaletteGroup group, PaletteItem assetItem)
        {
            EditorGUIUtility.PingObject(palette);
            AssetDatabase.OpenAsset(palette);

            group.IsOpenInEditor = true;
            assetItem.IsOpenInEditor = true;
        }

        private void DrawBrushTools()
        {
            using (new EditorGUILayout.VerticalScope(GUI.skin.box)) {
                DrawBrushTypeTool();

                var settings = ScenePlacer.SelectionSettings;
                settings.OptionsExtended = EditorGUILayout.Foldout(settings.OptionsExtended, "Tool Options", StylesUtility.BoldFoldoutStyle);

                if (!settings.OptionsExtended) {
                    return;
                }

                ScenePlacer.CurrentBrush.DrawBrushEditor(ScenePlacer);
                ScenePlacer.CurrentBrush.DrawAdditionalSettings(ScenePlacer, settings);
            }

            EditorGUILayout.Space();
        }

        private void DrawBrushTypeTool()
        {
            using (new EditorGUILayout.HorizontalScope()) {
                EditorGUILayout.LabelField(KalderaEditorUtils.SelectToolsContent, EditorStyles.boldLabel, GUILayout.Width(KalderaEditorUtils.OptionLabelWidth));
                var currentIndex = Mathf.Min(ScenePlacer.SelectionSettings.SelectedBrushIndex, ScenePlacer.GetBrushMapping().Length - 1);
                EditorGUILayout.LabelField(ScenePlacer.GetBrushMapping()[currentIndex].KeyBinding.SettingName);

            }

            var brushes = ScenePlacer.GetBrushMapping();

            var windowWidth = GetWindowWidth();
            var buttonsInRow = GetButtonCountInRow(windowWidth, KalderaEditorUtils.IconButtonSize);

            var totalIndex = 0;
            var currentRow = 0;

            while (totalIndex < brushes.Length) {
                using (new GUI.GroupScope(GUILayoutUtility.GetRect(windowWidth, KalderaEditorUtils.IconButtonSize))) {
                    for (int rowIndex = 0; rowIndex < buttonsInRow; rowIndex++) {
                        if (totalIndex >= brushes.Length) {
                            break;
                        }

                        var brush = brushes[totalIndex];

                        EditorCustomGUILayout.SetGuiBackgroundColorState(ScenePlacer.CurrentBrush == brush);
                        var drawRectatangle = GetRectForPosition(rowIndex, KalderaEditorUtils.IconButtonSize);

                        using (new EditorGUI.DisabledGroupScope(brush.Disabled)) {
                            if (GUI.Button(drawRectatangle, brush.GetButtonContent())) {
                                brush.OnButtonPress(this);
                                NotifyChange();
                            }
                        }

                        totalIndex++;
                    }

                    GUI.color = Color.white;
                }
                currentRow++;
            }

            EditorCustomGUILayout.RestoreGuiColor();
        }

        private void NotifyChange()
        {
            ScenePlacer.NotifyChange();
        }
    }
}