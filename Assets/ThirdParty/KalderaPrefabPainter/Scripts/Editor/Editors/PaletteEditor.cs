using CollisionBear.WorldEditor.Utils;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace CollisionBear.WorldEditor
{
    [CustomEditor(typeof(Palette))]
    public class PaletteEditor : Editor
    {
        private static readonly List<Rect> FieldRects = new List<Rect> {
            new Rect(0, 0, 64, 64),
            new Rect(64, 0, 64, 64),
            new Rect(0, 64, 64, 64),
            new Rect(64, 64, 64, 64)
        };

        private static readonly GUIContent PaletteContent = new GUIContent("Palette");
        private static readonly GUIContent PaletteGroupContent = new GUIContent("Palette Group:");
        private static readonly GUIContent PaletteItemContent = new GUIContent("Palette Item:");
        private static readonly GUIContent ShortcutKeyContent = new GUIContent("Shortcut", "Creates a shortcut key command of Shift + <key>");
        private static readonly GUIContent RotationTypeContent = new GUIContent("Rotation", "Determines how rotation is randomized");
        private static readonly GUIContent ScaleTypeContent = new GUIContent("Scale", "Determines how scale is randomized");

        private static readonly GUIContent SpacingFactorContent = new GUIContent("Spacing Factor", " Modifies the space between individual objects. The normal space is estimated based on the size of the object's renderers");
        private static readonly GUIContent MultiplyPrefabScaleContent = new GUIContent("Multiply Prefab Scale", "The scaling is multiplied by the scale of the prefab. Otherwise that prefab scale is ignored.");
        private static readonly GUIContent UsePrefabRotationContent = new GUIContent("Use prefab rotation", "The rotation of the prefab is taken into consideration when rotating a newly placed object.");
        private static readonly GUIContent UsePrefabHeightContent = new GUIContent("Use Prefab height", "The prefabs height offset (transform.position.y value) will be taken into consideration. This is also adjusted by the item's scale.");
        private static readonly GUIContent UseIndividualHeightDetection = new GUIContent("Individual height checks", "When placing multiple items their height/in-scene position is checked on an individual basis (Separate Raycast for each object in the brush).");
        private static readonly GUIContent AllowCollisionContent = new GUIContent("Allow collisions", "Overrides the Avoid Collision setting for this Item. Any Colliders in this GameObject is ignored when checking collision");
        private static readonly GUIContent RotationOffsetContent = new GUIContent("Rotation Offset", "Extra rotation to be applied. Can help with alignment on meshes that are oriented the wrong way.");
        private static readonly GUIContent ItemNamingContent = new GUIContent("Item naming", "Item name\nThe placed game object will take the name of the item.\n\nPrefab name\nWill take the name of the specific prefab used when placing it.");

        public class DraggedObjects
        {
            public int ControlId;
            public List<GameObject> GameObjects = new List<GameObject>();

            public void Clear()
            {
                GameObjects.Clear();
            }
        }

        private static readonly DraggedObjects DraggedAddedGameObjects = new DraggedObjects();

        private static GUIStyle SpriteGuiStyle = new GUIStyle();

        public override void OnInspectorGUI()
        {
            var category = (Palette)target;

            EditorCustomGUILayout.Reset();

            DrawHeader(category);
            EditorGUILayout.Space();
            using (var changeDetection = new EditorGUI.ChangeCheckScope()) {
                DrawGroups(category);

                if (changeDetection.changed) {
                    EditorUtility.SetDirty(target);
                    Undo.RecordObject(target, $"Updated {nameof(Palette)}");
                }
            }
        }

        protected void DrawHeader(Palette category)
        {
            EditorGUILayout.LabelField(PaletteContent, EditorStyles.boldLabel);
            category.ShortKey = (KeyCode)EditorGUILayout.EnumPopup(ShortcutKeyContent, category.ShortKey);
        }

        protected void DrawGroups(Palette palette)
        {
            foreach (var group in palette.Groups.ToList()) {
                using (new EditorGUILayout.VerticalScope(GUI.skin.box)) {
                    using (new EditorGUI.IndentLevelScope(increment: 1)) {
                        DrawGroup(palette, group);
                    }
                }
                EditorGUILayout.Space();
            }

            if(!palette.HasGroups()) {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox(KalderaEditorUtils.NoGroupsInPaletteHelpText, MessageType.Info);
                DrawNewItemButtons(palette, null);
                EditorGUILayout.Space();
            }

            if (GUILayout.Button("Create Palette group", GUILayout.Height(KalderaEditorUtils.AddButtonHeight))) {
                AddGroup(palette);
            }
        }

        protected void DrawGroup(Palette palette, PaletteGroup group)
        {
            using (var scope = new EditorGUILayout.HorizontalScope()) {
                group.IsOpenInEditor = EditorGUI.Foldout(new Rect(scope.rect.position, KalderaEditorUtils.FoldoutSize), group.IsOpenInEditor, GUIContent.none, true);
                EditorGUILayout.LabelField(PaletteGroupContent, EditorStyles.boldLabel, GUILayout.Width(KalderaEditorUtils.PrefixLabelWidth));
                if (group.IsOpenInEditor) {
                    group.GroupName = EditorGUILayout.TextField(group.GroupName, GUILayout.Height(KalderaEditorUtils.LineHeight));
                } else {
                    EditorGUILayout.LabelField(group.GroupName, EditorStyles.boldLabel, GUILayout.Height(KalderaEditorUtils.LineHeight));
                }

                if (GUILayout.Button(KalderaEditorUtils.MoveUpIconContent, StylesUtility.TinyButtonStyle)) {
                    palette.MoveGroupUp(group);
                }

                if (GUILayout.Button(KalderaEditorUtils.MoveDownIconContent, StylesUtility.TinyButtonStyle)) {
                    palette.MoveGroupDown(group);
                }

                if (GUILayout.Button(KalderaEditorUtils.ClearIconContent, StylesUtility.TinyButtonStyle)) {
                    if (group.Items.Count == 0 || EditorUtility.DisplayDialog("Clear group", KalderaEditorUtils.ClearGroupDialog, "Ok", "Cancel")) {
                        ClearGroup(palette, group);
                    }
                    GUIUtility.ExitGUI();
                }

                if (GUILayout.Button(KalderaEditorUtils.TrashIconContent, StylesUtility.TinyButtonStyle)) {
                    if (group.Items.Count == 0 || EditorUtility.DisplayDialog("Remove group", KalderaEditorUtils.RemoveGroupDialog, "Ok", "Cancel")) {
                        RemoveGroup(palette, group);
                    }
                    GUIUtility.ExitGUI();
                }
            }

            if (group.IsOpenInEditor) {
                EditorGUILayout.Space();
                if (group.Items.Count > 0) {
                    foreach (var item in group.Items.ToList()) {
                        DrawItem(item, palette, group);
                    }
                } else {
                    EditorGUILayout.HelpBox(KalderaEditorUtils.NoItemsInGroupHelpText, MessageType.Info);
                    EditorGUILayout.Space();
                }

                EditorGUILayout.Space();
                DrawNewItemButtons(palette, group);
            }
        }

        private void DrawNewItemButtons(Palette palette, PaletteGroup group) {
            using (var scope = new EditorGUILayout.HorizontalScope()) {
                var controlId = GUIUtility.GetControlID(FocusType.Passive);

                if (EditorCustomGUILayout.DropTargetButton(new GUIContent("Add Prefab\n[Drag prefab here]"), DraggedAddedGameObjects, GUILayout.Height(48))) {
                    EditorGUIUtility.ShowObjectPicker<GameObject>(null, false, string.Empty, controlId);
                }

                if (DraggedAddedGameObjects.GameObjects.Count == 1) {
                    if(group == null) {
                        group = AddGroup(palette);
                    }

                    var firstGameObject = DraggedAddedGameObjects.GameObjects.First();
                    var item = AddItemToGroup(group, firstGameObject);
                    item.Name = firstGameObject.name;
                } else if (DraggedAddedGameObjects.GameObjects.Count > 1) {
                    if (group == null) {
                        group = AddGroup(palette);
                    }

                    var firstGameObject = DraggedAddedGameObjects.GameObjects.First();
                    if (EditorUtility.DisplayDialog("Add multiple prefabs", "Do you want to add the game objects as separate items or variants in a single item?", "Separate items", "Variants")) {
                        foreach (var gameObject in DraggedAddedGameObjects.GameObjects) {
                            var item = AddItemToGroup(group, gameObject);
                            item.Name = firstGameObject.name;
                        }
                    } else {
                        var item = AddItemToGroup(group, firstGameObject);
                        item.Name = firstGameObject.name;

                        item.GameObjectVariants = new List<GameObject>(DraggedAddedGameObjects.GameObjects);
                    }
                    GUIUtility.ExitGUI();
                }

                if (Event.current.type == EventType.ExecuteCommand && Event.current.commandName == "ObjectSelectorClosed" && EditorGUIUtility.GetObjectPickerControlID() == controlId) {
                    if (group == null) {
                        group = AddGroup(palette);
                    }

                    var pickedObject = (GameObject)EditorGUIUtility.GetObjectPickerObject();
                    if (pickedObject != null) {
                        var item = AddItemToGroup(group, pickedObject);
                        item.Name = pickedObject.name;
                        GUI.changed = true;
                    }
                }

                if (GUILayout.Button("Create empty Object", GUILayout.Height(48))) {
                    if (group == null) {
                        group = AddGroup(palette);
                    }

                    AddEmptyItemToGroup(group);
                }
            }
        }

        protected void DrawItem(PaletteItem item, Palette category, PaletteGroup group)
        {
            using (new EditorGUILayout.VerticalScope(GUI.skin.box)) {
                using (var scope = new EditorGUILayout.HorizontalScope()) {
                    item.IsOpenInEditor = EditorGUI.Foldout(new Rect(scope.rect.position, KalderaEditorUtils.FoldoutSize), item.IsOpenInEditor, GUIContent.none, true);
                    EditorGUILayout.LabelField(PaletteItemContent, EditorStyles.boldLabel, GUILayout.Width(KalderaEditorUtils.PrefixLabelWidth));
                    if (item.IsOpenInEditor) {
                        item.Name = EditorGUILayout.TextField(item.Name);
                    } else {
                        EditorGUILayout.LabelField(item.Name, EditorStyles.boldLabel);
                    }

                    if (GUILayout.Button(KalderaEditorUtils.MoveUpIconContent, StylesUtility.TinyButtonStyle)) {
                        group.MoveItemUp(item);
                    }

                    if (GUILayout.Button(KalderaEditorUtils.MoveDownIconContent, StylesUtility.TinyButtonStyle)) {
                        group.MoveItemDown(item);
                    }

                    if (GUILayout.Button(KalderaEditorUtils.TrashIconContent, StylesUtility.TinyButtonStyle)) {
                        if (EditorUtility.DisplayDialog("Remove item", "Do you really want to remove this item?", "Ok", "Cancel")) {
                            RemoveItemFromGroup(item, group);
                        }
                        GUIUtility.ExitGUI();
                    }
                }

                if (item.IsOpenInEditor) {
                    DrawToolsItem(category, group, item);
                }
            }
        }

        protected void DrawToolsItem(Palette category, PaletteGroup group, PaletteItem item)
        {
            using (new EditorGUILayout.HorizontalScope()) {
                EditorGUILayout.Space();
                DrawPrefabSelect(item);
                using (new EditorGUILayout.VerticalScope()) {
                    DrawRotationFields(item);
                    EditorGUILayout.Space();
                    DrawScaleFields(item.Scale);

                    EditorGUILayout.Space();
                    using (var scope = new EditorGUILayout.HorizontalScope()) {
                        item.IsAdvancesOptionsOpenInEditor = EditorGUILayout.Foldout(item.IsAdvancesOptionsOpenInEditor, "Advanced options", true, StylesUtility.BoldFoldoutStyle);
                    }

                    if (item.IsAdvancesOptionsOpenInEditor) {
                        DrawAdvancedOptions(item);
                    }
                }
            }

            EditorGUILayout.Space();
        }

        protected Vector3 Vector3Field(GUIContent guiContent, Vector3 value, float labelWidth)
        {
            using (new EditorGUILayout.HorizontalScope()) {
                EditorGUILayout.LabelField(guiContent, GUILayout.Width(labelWidth));
                return new Vector3 {
                    x = EditorGUILayout.FloatField(value.x),
                    y = EditorGUILayout.FloatField(value.y),
                    z = EditorGUILayout.FloatField(value.z)
                };
            }
        }

        protected void DrawRotationFields(PaletteItem item)
        {
            item.Rotation.Mode = (RotationInformation.RotationMode)EditorGUILayout.EnumPopup(RotationTypeContent, item.Rotation.Mode);

            if (item.Rotation.Mode == RotationInformation.RotationMode.ConstantSeparateAxes) {
                item.Rotation.Constant = Vector3Field(new GUIContent("Constant", ""), item.Rotation.Constant, 80);
            } else if (item.Rotation.Mode == RotationInformation.RotationMode.RandomSeparateAxes) {
                item.Rotation.Min = Vector3Field(new GUIContent("Min", "Minimum rotation for each axis"), item.Rotation.Min, 80);
                item.Rotation.Max = Vector3Field(new GUIContent("Max", "Maximum rotation for each axis"), item.Rotation.Max, 80);
            }

            EditorGUILayout.Space();
        }

        protected void DrawScaleFields(ScaleInformation scale)
        {
            scale.Mode = (ScaleInformation.AxisType)EditorGUILayout.EnumPopup(ScaleTypeContent, scale.Mode, EditorStyles.popup);

            if (scale.Mode == ScaleInformation.AxisType.SingleAxis) {
                scale.MinScale.x = EditorGUILayout.FloatField(new GUIContent("Scale", "Scale for all axes"), scale.MinScale.x);
            } else if (scale.Mode == ScaleInformation.AxisType.SeperateAxes) {
                scale.MinScale = Vector3Field(new GUIContent("Scale", "Scale for each axis"), scale.MinScale, 80);
            } else if (scale.Mode == ScaleInformation.AxisType.RandomSingleAxis) {
                scale.MinScale.x = EditorGUILayout.FloatField(new GUIContent("Min scale", "Minimum scale for all axes"), scale.MinScale.x);
                scale.MaxScale.x = EditorGUILayout.FloatField(new GUIContent("Max scale", "Maximum scale for all axes"), scale.MaxScale.x);
            } else if (scale.Mode == ScaleInformation.AxisType.RandomSeperateAxis) {
                scale.MinScale = Vector3Field(new GUIContent("Min", "Minimum scale for each axis"), scale.MinScale, 80);
                scale.MaxScale = Vector3Field(new GUIContent("Max", "Maximum scale for each axis"), scale.MaxScale, 80);

                scale.UnitformScaling = EditorGUILayout.Toggle(new GUIContent("Uniform scaling", "If checked, the X, Y and Z axis are all interpolated equally between their min and max values"), scale.UnitformScaling);
            }

            EditorGUILayout.Space();
        }

        protected void DrawPrefabSelect(PaletteItem item)
        {
            if (item.IsVariantsOpen) {
                DrawVariantOpenPrefabSelect(item);
            } else {
                DrawVariantClosedPrefabSelect(item);
            }
        }

        private void DrawVariantOpenPrefabSelect(PaletteItem item)
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Height(120 + item.GameObjectVariants.Count * KalderaEditorUtils.VariantSelectionItemHeight))) {
                item.IsVariantsOpen = EditorGUILayout.Foldout(item.IsVariantsOpen, string.Format("Variants ({0})", item.GameObjectVariants.Count), StylesUtility.BoldFoldoutStyle);

                var controlId = GUIUtility.GetControlID(FocusType.Passive);
                if (EditorCustomGUILayout.DropTargetButton(
                    new GUIContent("Add variant\n[Drag prefabs here]"),
                    DraggedAddedGameObjects,
                    GUILayout.Width(KalderaEditorUtils.VariantSelectionBaseWidth),
                    GUILayout.Height(KalderaEditorUtils.AddVariantButtonHeight))
                ) {
                    EditorGUIUtility.ShowObjectPicker<GameObject>(null, false, string.Empty, controlId);
                }
                if (DraggedAddedGameObjects.GameObjects.Count > 0) {
                    foreach (var gameObject in DraggedAddedGameObjects.GameObjects) {
                        item.GameObjectVariants.Add(gameObject);
                    }
                }

                if (Event.current.type == EventType.ExecuteCommand && Event.current.commandName == "ObjectSelectorClosed" && EditorGUIUtility.GetObjectPickerControlID() == controlId) {
                    var pickedObject = (GameObject)EditorGUIUtility.GetObjectPickerObject();
                    if (pickedObject != null) {
                        item.GameObjectVariants.Add(pickedObject);
                        GUI.changed = true;
                        return;
                    }
                }

                EditorGUILayout.Space();

                using (var scrollScope = new EditorGUILayout.ScrollViewScope(item.VariantsScroll, GUILayout.Width(KalderaEditorUtils.VariantScrollWidth))) {
                    item.VariantsScroll = scrollScope.scrollPosition;
                    for (var i = 0; i < item.GameObjectVariants.Count; i++) {
                        using (new EditorGUILayout.VerticalScope()) {
                            DrawPrefabSelect(item, i);

                            if (GUILayout.Button(KalderaEditorUtils.TrashIconContent, GUILayout.Height(KalderaEditorUtils.AddButtonHeight), GUILayout.Width(KalderaEditorUtils.VariantSelectionBaseWidth))) {
                                if (EditorUtility.DisplayDialog("Remove variant", "Do you really want to remove this variant?", "Ok", "Cancel")) {
                                    item.RemoveVariantAt(i);
                                }
                                GUIUtility.ExitGUI();
                            }
                        }

                        EditorGUILayout.Space();
                    }
                }
            }
        }

        private void DrawVariantClosedPrefabSelect(PaletteItem item)
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(KalderaEditorUtils.VariantScrollWidth))) {
                item.IsVariantsOpen = EditorGUILayout.Foldout(item.IsVariantsOpen, string.Format("Variants ({0})", item.GameObjectVariants.Count), StylesUtility.BoldFoldoutStyle);

                if (item.GameObjectVariants.Count <= 1) {
                    DrawSingleVariant(item);
                } else {
                    DrawMultipleVariants(item);
                }
            }
        }

        private void DrawSingleVariant(PaletteItem item)
        {
            var firstIndex = item.GetFirstIndex();
            DrawPrefabSelect(item, firstIndex);
        }

        private void DrawMultipleVariants(PaletteItem item)
        {
            using (new GUI.GroupScope(GUILayoutUtility.GetRect(128, 128))) {
                var validItems = item.ValidObjects();
                for (int i = 0; i < Mathf.Min(4, validItems.Count); i++) {

                    var guiContent = AssetPreview.GetAssetPreview(validItems[i]);
                    if (guiContent == null) {
                        continue;
                    }

                    GUI.DrawTexture(FieldRects[i], guiContent);
                }
            }
        }

        private void DrawPrefabSelect(PaletteItem item, int index)
        {
            using (var check = new EditorGUI.ChangeCheckScope()) {
                try {
                    var previewTexture = PreviewRenderingUtility.GetPreviewTexture(item.GameObjectVariants[index]);
                    item.GameObjectVariants[index] = EditorCustomGUILayout.ObjectFieldWithPreview(item.GameObjectVariants[index], previewTexture, 128);

                } catch (System.Exception e) {
                    var previewTexture = KalderaEditorUtils.WarningIconTexture;

                    item.GameObjectVariants[index] = EditorCustomGUILayout.ObjectFieldWithPreview(item.GameObjectVariants[index], previewTexture, 128);
                    Debug.LogWarning($"Error: {e.Message}");
                }

                if (check.changed) {
                    if (item.Name == string.Empty && item.GameObjectVariants[index] != null) {
                        item.Name = item.GameObjectVariants[index].name;
                    }
                }
            }
        }

        protected string GetItemName(GameObject asset)
        {
            if (asset == null) {
                return "[Empty Asset]";
            } else {
                return asset.name;
            }
        }

        protected void DrawAdvancedOptions(PaletteItem item)
        {
            var advancedOptions = item.AdvancedOptions;

            advancedOptions.NameType = (AdvancedOptions.ItemNameType)EditorGUILayout.EnumPopup(ItemNamingContent, advancedOptions.NameType);
            EditorGUILayout.Space();

            advancedOptions.UsePrefabHeight = EditorGUILayout.Toggle(UsePrefabHeightContent, advancedOptions.UsePrefabHeight);
            advancedOptions.UseIndividualGroundHeight = EditorGUILayout.Toggle(UseIndividualHeightDetection, advancedOptions.UseIndividualGroundHeight);

            if(item.HasNonCenteredPositionedVariants() && advancedOptions.UseIndividualGroundHeight) {
                EditorGUILayout.LabelField("Some Variants' positions are not centered at (0, 0, 0)", EditorStyles.miniLabel);
            }

            EditorGUILayout.Space();

            advancedOptions.UsePrefabRotation = EditorGUILayout.Toggle(UsePrefabRotationContent, advancedOptions.UsePrefabRotation);
            if (item.HasNonCenteredRotationedVariants() && advancedOptions.UsePrefabRotation) {
                EditorGUILayout.LabelField("Some Variants' rotation are at not at (0, 0, 0)", EditorStyles.miniLabel);
            }

            advancedOptions.RotationOffset = EditorGUILayout.Vector3Field(RotationOffsetContent, advancedOptions.RotationOffset);
            EditorGUILayout.Space();

            advancedOptions.SpacingFactor = EditorGUILayout.Slider(SpacingFactorContent, advancedOptions.SpacingFactor, 0.1f, 10.0f);
            advancedOptions.MultiplyPrefabScale = EditorGUILayout.Toggle(MultiplyPrefabScaleContent, advancedOptions.MultiplyPrefabScale);

            EditorGUILayout.Space();
            advancedOptions.AllowCollision = EditorGUILayout.Toggle(AllowCollisionContent, advancedOptions.AllowCollision);
        }

        protected PaletteGroup AddGroup(Palette category)
        {
            var result = new PaletteGroup {
                GroupName = "",
                Items = new List<PaletteItem>(),
                IsOpenInEditor = true,
                NewGroupItem = new PaletteItem()
            };

            category.Groups.Add(result);
            return result;
        }

        protected void RemoveGroup(Palette category, PaletteGroup group)
        {
            if (group == null) {
                return;
            }

            if (!category.Groups.Contains(group)) {
                return;
            }

            category.Groups.Remove(group);
        }

        protected void ClearGroup(Palette category, PaletteGroup group)
        {
            if (group == null) {
                return;
            }

            if (!category.Groups.Contains(group)) {
                return;
            };

            group.Items.Clear();
        }

        protected PaletteItem AddEmptyItemToGroup(PaletteGroup group)
        {
            var result = new PaletteItem { IsOpenInEditor = true };
            group.Items.Add(result);
            return result;
        }

        protected PaletteItem AddItemToGroup(PaletteGroup group, GameObject gameObject)
        {
            if (gameObject == null) {
                Debug.Log("Tried to add Null Prefab. Falls back to adding empty item");
                return AddEmptyItemToGroup(group);
            } else {
                var result = new PaletteItem(gameObject) { IsOpenInEditor = true };
                group.Items.Add(result);
                return result;
            }
        }

        protected void RemoveItemFromGroup(PaletteItem item, PaletteGroup group)
        {
            if (item == null || group == null) {
                return;
            }

            if (!group.Items.Contains(item)) {
                return;
            }

            group.Items.Remove(item);
        }
    }
}