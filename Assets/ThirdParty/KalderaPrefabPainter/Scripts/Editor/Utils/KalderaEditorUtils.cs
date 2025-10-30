using System.IO;
using UnityEditor;
using UnityEngine;

namespace CollisionBear.WorldEditor.Utils {
    public static class KalderaEditorUtils {
        public const int MiniButtonWidth = 26;
        public const int MiniButtonHeight = 18;

        public const float PrefixLabelWidth = 100;
        public const float TinyButtonWidth = 22;

        public const int ObjectLimitMin = 1;
        public const int ObjectLimitMax = 1000;

        public const int OptionLabelWidth = 100;

        public const float AddButtonHeight = 32;
        public const float IconButtonSize = 48;

        public const float VariantSelectionItemHeight = 162;
        public const float VariantSelectionBaseWidth = 128;
        public const float VariantScrollWidth = VariantSelectionBaseWidth + 20;
        public const float AddVariantButtonHeight = 48;

        public const string IconsBasePath = "Assets/KalderaPrefabPainter/Images/";

        public const string PluginName = "Kaldera Prefab Painter Lite";
        public const string Version = "1.7.0";

        public const string WindowBasePath = "Window/" + PluginName;
        public const string AssetBasePath = "Kaldera Prefab Painter";

        public const string EmptySetTooltip = "Empty editor Palette Set.\nAdd a Palette to this set or select another Palette/Palette set";
        public const string SelectPaletteToolTip = "Please select a Palette or a Palette Set!";
        public const string NoPaletteToolTip = "No Palettes or Palette Sets in the Project\n\nPlease create a Palette or a Palette Set to start using the Prefab Painter.\n\nCreate one from the button below or \nAssets -> Create -> Kaldera Prefab Painter -> Prefab Palette";
        public const string CreatePaletteButton = "Create Palette";
        public const string DefaultPaletteName = "New_Kaldera_Palette.asset";

        public const string ClearGroupDialog = "Do you really want to clear this group?\\nThis will remove every single item in this group.";
        public const string RemoveGroupDialog = "Do you really want to remove this group?\nThis will remove this group and every single item in it.";

        public const string NoGroupsInPaletteHelpText = "Palette has no groups!\nCreate a new group to start adding Prefabs to your palette.\n\nOtherwise, start adding Prefabs and a new Group will be created automatically.";
        public const string NoItemsInGroupHelpText = "Group has no items!\nAdd some Prefabs to this group and they will be made available in the Palete window.";

        public const string NoPaletteGroups = "Palette contains no groups with Prefabs in them.\n\nBefore you can paint Prefabs, you need to add some to the Palette.";
        public const string GotoPaletteText = "Go to Palette";

        public const string MultiSelectTooltip = "Hold down shift to select/deselect several prefabs";
        public const string ClearSelectionToolTip = "Press {0} to clear brush";

        public static readonly Vector2 FoldoutSize = new Vector2(36, 18);

        public static readonly float LineHeight = EditorGUIUtility.singleLineHeight;

        public static readonly GUIContent TrashIconContent;
        public static readonly GUIContent ClearIconContent;
        public static readonly GUIContent MoveUpIconContent;
        public static readonly GUIContent MoveDownIconContent;
        public static readonly GUIContent ShowInProjectContent;

        public static readonly GUIContent TitleGuiContent;
        public static readonly GUIContent SelectPaletteCollectionContent;
        public static readonly GUIContent SelectPlacementModeContent;
        public static readonly GUIContent SelectRaycastModeContent;
        public static readonly GUIContent SelectPaletteContent;
        public static readonly GUIContent SelectToolsContent;

        public static readonly GUIContent BrushDistanceContent;
        public static readonly GUIContent ParentObjectToBaseObjectContent;
        public static readonly GUIContent AvoidCollisionContent;
        public static readonly GUIContent AvoidCollisionSpacingContent;
        public static readonly GUIContent OrientToGroundNormalContent;
        public static readonly GUIContent OrientToBrushNormalContent;
        public static readonly GUIContent MaintainRotationContent;
        public static readonly GUIContent OrientWithBrushContent;
        public static readonly GUIContent BrushSizeContent;
        public static readonly GUIContent BrushSpacingContent;
        public static readonly GUIContent ObjectDensityContent;
        public static readonly GUIContent SprayIntensityContent;
        public static readonly GUIContent BrushDistributionContent;
        public static readonly GUIContent ObjectLimitContent;

        public static readonly GUIContent MoreInformationContent;

        public static readonly GUIContent PaletteLabelContent;

        public static readonly GUIContent VersionContent;

        public static readonly Texture2D WarningIconTexture;

        public static readonly Mesh PlaneMesh = new Mesh() {
            vertices = new Vector3[] {
                new Vector3(-1, 0, -1),
                new Vector3(1, 0, -1),
                new Vector3(1, 0, 1),
                new Vector3(-1, 0, 1),
            },
            uv = new Vector2[] {
                new Vector2(0, 0),
                new Vector2(1, 0),
                new Vector2(1, 1),
                new Vector2(0, 1),
            },
            normals = new Vector3[] {
                Vector3.up,
                Vector3.up,
                Vector3.up,
                Vector3.up,
            },
            triangles = new int[] {
                2, 1, 0, 0, 3, 2
            }
        };

        public static readonly Mesh CubeMesh = new Mesh() {
            vertices = new Vector3[] {
                new Vector3 (-0.5f, -0.5f, -0.5f),
                new Vector3 (0.5f, -0.5f, -0.5f),
                new Vector3 (0.5f, 0.5f, -0.5f),
                new Vector3 (-0.5f, 0.5f, -0.5f),
                new Vector3 (-0.5f, 0.5f, 0.5f),
                new Vector3 (0.5f, 0.5f, 0.5f),
                new Vector3 (0.5f, -0.5f, 0.5f),
                new Vector3 (-0.5f, -0.5f, 0.5f),
            },
            triangles = new int[] {
                0, 2, 1, //face front
			    0, 3, 2,
                2, 3, 4, //face top
			    2, 4, 5,
                1, 2, 5, //face right
			    1, 5, 6,
                0, 7, 4, //face left
			    0, 4, 3,
                5, 4, 7, //face back
			    5, 7, 6,
                0, 6, 7, //face bottom
			    0, 1, 6
            }
        };

        public static readonly Mesh CylinderMesh = new Mesh() {
            vertices = new Vector3[] {
                new Vector3 (0, -0.5f, 0.5f),
                new Vector3 (0, 0.5f, 0.5f),
                new Vector3 (0.1710101f, -0.5f, 0.4698463f),
                new Vector3 (0.1710101f, 0.5f,  0.4698463f),
                new Vector3 (0.3213938f, -0.5f, 0.3830222f),
                new Vector3 (0.3213938f, 0.5f,  0.3830222f),
                new Vector3 (0.4330127f, -0.5f, 0.25f),
                new Vector3 (0.4330127f, 0.5f,  0.25f),
                new Vector3 (0.4924038f, -0.5f, 0.08682412f),
                new Vector3 (0.4924038f, 0.5f,  0.08682412f),
                new Vector3 (0.4924039f, -0.5f, -0.08682406f),
                new Vector3 (0.4924039f, 0.5f,  -0.08682406f),
                new Vector3 (0.4330127f, -0.5f, -0.2500001f),
                new Vector3 (0.4330127f, 0.5f,  -0.2500001f),
                new Vector3 (0.3213938f, -0.5f, -0.3830222f),
                new Vector3 (0.3213938f, 0.5f,  -0.3830222f),
                new Vector3 (0.1710101f, -0.5f, -0.4698462f),
                new Vector3 (0.1710101f, 0.5f,  -0.4698462f),
                new Vector3 (0,         -0.5f,  -0.5f),
                new Vector3 (0,         0.5f,   -0.5f),
                new Vector3 (-0.1710101f, -0.5f, -0.4698462f),
                new Vector3 (-0.1710101f, 0.5f,     -0.4698462f),
                new Vector3 (-0.3213938f, -0.5f, -0.3830222f),
                new Vector3 (-0.3213938f, 0.5f,     -0.3830222f),
                new Vector3 (-0.4330128f, -0.5f, -0.25f),
                new Vector3 (-0.4330128f, 0.5f,     -0.25f),
                new Vector3 (-0.4924039f, -0.5f, -0.08682406f),
                new Vector3 (-0.4924039f, 0.5f, -0.08682406f),
                new Vector3 (-0.4924039f, -0.5f, 0.08682406f),
                new Vector3 (-0.4924039f, 0.5f, 0.08682406f),
                new Vector3 (-0.4330128f, -0.5f, 0.2499999f),
                new Vector3 (-0.4330128f, 0.5f, 0.2499999f),
                new Vector3 (-0.3213939f, -0.5f, 0.3830222f),
                new Vector3 (-0.3213939f, 0.5f, 0.3830222f),
                new Vector3 (-0.1710102f, -0.5f, 0.4698463f),
                new Vector3 (-0.1710102f, 0.5f, 0.4698463f),
            },
            triangles = new int[] {
                0, 2, 1, 1, 2, 3,
                2, 4, 3, 3, 4, 5,
                4, 6, 5, 5, 6, 7,
                6, 8, 7, 7, 8, 9,
                8, 10, 9, 9, 10, 11,
                10, 12, 11, 11, 12, 13,
                12, 14, 13, 13, 14, 15,
                14, 16, 15, 15, 16, 17,
                16, 18, 17, 17, 18, 19,
                18, 20, 19, 19, 20, 21,
                20, 22, 21, 21, 22, 23,
                22, 24, 23, 23, 24, 25,
                24, 26, 25, 25, 26, 27,
                26, 28, 27, 27, 28, 29,
                28, 30, 29, 29, 30, 31,
                30, 32, 31, 31, 32, 33,
                32, 34, 33, 33, 34, 35,
                34, 0, 35, 35, 0, 1,
            }
        };

        public static readonly Mesh HalfSphereMesh = new Mesh() {
            vertices = new Vector3[] {
                new Vector3(0.0000000f, 0.5000000f, 0.0000000f),
                new Vector3(0.0000000f, 0.5000000f, 0.0000000f),
                new Vector3(0.0000000f, 0.5000000f, 0.0000000f),
                new Vector3(0.0000000f, 0.5000000f, 0.0000000f),
                new Vector3(-0.0000000f, 0.5000000f, 0.0000000f),
                new Vector3(-0.0000000f, 0.5000000f, 0.0000000f),
                new Vector3(-0.0000000f, 0.5000000f, 0.0000000f),
                new Vector3(-0.0000000f, 0.5000000f, 0.0000000f),
                new Vector3(-0.0000000f, 0.5000000f, -0.0000000f),
                new Vector3(-0.0000000f, 0.5000000f, -0.0000000f),
                new Vector3(-0.0000000f, 0.5000000f, -0.0000000f),
                new Vector3(-0.0000000f, 0.5000000f, -0.0000000f),
                new Vector3(0.0000000f, 0.5000000f, -0.0000000f),
                new Vector3(0.0000000f, 0.5000000f, -0.0000000f),
                new Vector3(0.0000000f, 0.5000000f, -0.0000000f),
                new Vector3(0.0000000f, 0.5000000f, -0.0000000f),
                new Vector3(0.0490086f, 0.4975924f, 0.0000000f),
                new Vector3(0.0452780f, 0.4975924f, 0.0187548f),
                new Vector3(0.0346543f, 0.4975924f, 0.0346543f),
                new Vector3(0.0187548f, 0.4975924f, 0.0452780f),
                new Vector3(-0.0000000f, 0.4975924f, 0.0490086f),
                new Vector3(-0.0187548f, 0.4975924f, 0.0452780f),
                new Vector3(-0.0346543f, 0.4975924f, 0.0346543f),
                new Vector3(-0.0452780f, 0.4975924f, 0.0187548f),
                new Vector3(-0.0490086f, 0.4975924f, -0.0000000f),
                new Vector3(-0.0452780f, 0.4975924f, -0.0187548f),
                new Vector3(-0.0346543f, 0.4975924f, -0.0346543f),
                new Vector3(-0.0187548f, 0.4975924f, -0.0452780f),
                new Vector3(0.0000000f, 0.4975924f, -0.0490086f),
                new Vector3(0.0187548f, 0.4975924f, -0.0452780f),
                new Vector3(0.0346543f, 0.4975924f, -0.0346543f),
                new Vector3(0.0452780f, 0.4975924f, -0.0187548f),
                new Vector3(0.0975452f, 0.4903926f, 0.0000000f),
                new Vector3(0.0901200f, 0.4903926f, 0.0373289f),
                new Vector3(0.0689748f, 0.4903926f, 0.0689748f),
                new Vector3(0.0373289f, 0.4903926f, 0.0901200f),
                new Vector3(-0.0000000f, 0.4903926f, 0.0975452f),
                new Vector3(-0.0373289f, 0.4903926f, 0.0901200f),
                new Vector3(-0.0689748f, 0.4903926f, 0.0689748f),
                new Vector3(-0.0901200f, 0.4903926f, 0.0373289f),
                new Vector3(-0.0975452f, 0.4903926f, -0.0000000f),
                new Vector3(-0.0901200f, 0.4903926f, -0.0373289f),
                new Vector3(-0.0689748f, 0.4903926f, -0.0689749f),
                new Vector3(-0.0373289f, 0.4903926f, -0.0901200f),
                new Vector3(0.0000000f, 0.4903926f, -0.0975452f),
                new Vector3(0.0373289f, 0.4903926f, -0.0901200f),
                new Vector3(0.0689749f, 0.4903926f, -0.0689748f),
                new Vector3(0.0901200f, 0.4903926f, -0.0373289f),
                new Vector3(0.1451423f, 0.4784702f, 0.0000000f),
                new Vector3(0.1340940f, 0.4784702f, 0.0555436f),
                new Vector3(0.1026311f, 0.4784702f, 0.1026311f),
                new Vector3(0.0555436f, 0.4784702f, 0.1340940f),
                new Vector3(-0.0000000f, 0.4784702f, 0.1451423f),
                new Vector3(-0.0555436f, 0.4784702f, 0.1340940f),
                new Vector3(-0.1026311f, 0.4784702f, 0.1026311f),
                new Vector3(-0.1340940f, 0.4784702f, 0.0555435f),
                new Vector3(-0.1451423f, 0.4784702f, -0.0000000f),
                new Vector3(-0.1340940f, 0.4784702f, -0.0555436f),
                new Vector3(-0.1026311f, 0.4784702f, -0.1026311f),
                new Vector3(-0.0555435f, 0.4784702f, -0.1340940f),
                new Vector3(0.0000000f, 0.4784702f, -0.1451423f),
                new Vector3(0.0555436f, 0.4784702f, -0.1340940f),
                new Vector3(0.1026312f, 0.4784702f, -0.1026311f),
                new Vector3(0.1340940f, 0.4784702f, -0.0555436f),
                new Vector3(0.1913417f, 0.4619398f, 0.0000000f),
                new Vector3(0.1767767f, 0.4619398f, 0.0732233f),
                new Vector3(0.1352990f, 0.4619398f, 0.1352990f),
                new Vector3(0.0732233f, 0.4619398f, 0.1767767f),
                new Vector3(-0.0000000f, 0.4619398f, 0.1913417f),
                new Vector3(-0.0732233f, 0.4619398f, 0.1767767f),
                new Vector3(-0.1352990f, 0.4619398f, 0.1352990f),
                new Vector3(-0.1767767f, 0.4619398f, 0.0732233f),
                new Vector3(-0.1913417f, 0.4619398f, -0.0000000f),
                new Vector3(-0.1767767f, 0.4619398f, -0.0732233f),
                new Vector3(-0.1352990f, 0.4619398f, -0.1352991f),
                new Vector3(-0.0732232f, 0.4619398f, -0.1767767f),
                new Vector3(0.0000000f, 0.4619398f, -0.1913417f),
                new Vector3(0.0732233f, 0.4619398f, -0.1767767f),
                new Vector3(0.1352991f, 0.4619398f, -0.1352990f),
                new Vector3(0.1767767f, 0.4619398f, -0.0732233f),
                new Vector3(0.2356984f, 0.4409606f, 0.0000000f),
                new Vector3(0.2177569f, 0.4409606f, 0.0901979f),
                new Vector3(0.1666639f, 0.4409606f, 0.1666639f),
                new Vector3(0.0901979f, 0.4409606f, 0.2177569f),
                new Vector3(-0.0000000f, 0.4409606f, 0.2356984f),
                new Vector3(-0.0901979f, 0.4409606f, 0.2177569f),
                new Vector3(-0.1666639f, 0.4409606f, 0.1666639f),
                new Vector3(-0.2177569f, 0.4409606f, 0.0901978f),
                new Vector3(-0.2356984f, 0.4409606f, -0.0000000f),
                new Vector3(-0.2177569f, 0.4409606f, -0.0901979f),
                new Vector3(-0.1666639f, 0.4409606f, -0.1666639f),
                new Vector3(-0.0901978f, 0.4409606f, -0.2177569f),
                new Vector3(0.0000000f, 0.4409606f, -0.2356984f),
                new Vector3(0.0901979f, 0.4409606f, -0.2177569f),
                new Vector3(0.1666640f, 0.4409606f, -0.1666639f),
                new Vector3(0.2177569f, 0.4409606f, -0.0901979f),
                new Vector3(0.2777851f, 0.4157348f, 0.0000000f),
                new Vector3(0.2566400f, 0.4157348f, 0.1063038f),
                new Vector3(0.1964237f, 0.4157348f, 0.1964237f),
                new Vector3(0.1063038f, 0.4157348f, 0.2566400f),
                new Vector3(-0.0000000f, 0.4157348f, 0.2777851f),
                new Vector3(-0.1063038f, 0.4157348f, 0.2566400f),
                new Vector3(-0.1964237f, 0.4157348f, 0.1964237f),
                new Vector3(-0.2566400f, 0.4157348f, 0.1063037f),
                new Vector3(-0.2777851f, 0.4157348f, -0.0000000f),
                new Vector3(-0.2566400f, 0.4157348f, -0.1063038f),
                new Vector3(-0.1964237f, 0.4157348f, -0.1964238f),
                new Vector3(-0.1063037f, 0.4157348f, -0.2566400f),
                new Vector3(0.0000000f, 0.4157348f, -0.2777851f),
                new Vector3(0.1063038f, 0.4157348f, -0.2566400f),
                new Vector3(0.1964238f, 0.4157348f, -0.1964237f),
                new Vector3(0.2566400f, 0.4157348f, -0.1063038f),
                new Vector3(0.3171967f, 0.3865052f, 0.0000000f),
                new Vector3(0.2930515f, 0.3865052f, 0.1213859f),
                new Vector3(0.2242919f, 0.3865052f, 0.2242919f),
                new Vector3(0.1213859f, 0.3865052f, 0.2930515f),
                new Vector3(-0.0000000f, 0.3865052f, 0.3171967f),
                new Vector3(-0.1213859f, 0.3865052f, 0.2930515f),
                new Vector3(-0.2242919f, 0.3865052f, 0.2242919f),
                new Vector3(-0.2930515f, 0.3865052f, 0.1213859f),
                new Vector3(-0.3171967f, 0.3865052f, -0.0000000f),
                new Vector3(-0.2930515f, 0.3865052f, -0.1213859f),
                new Vector3(-0.2242919f, 0.3865052f, -0.2242920f),
                new Vector3(-0.1213858f, 0.3865052f, -0.2930516f),
                new Vector3(0.0000000f, 0.3865052f, -0.3171967f),
                new Vector3(0.1213860f, 0.3865052f, -0.2930515f),
                new Vector3(0.2242920f, 0.3865052f, -0.2242918f),
                new Vector3(0.2930515f, 0.3865052f, -0.1213859f),
                new Vector3(0.3535534f, 0.3535534f, 0.0000000f),
                new Vector3(0.3266407f, 0.3535534f, 0.1352990f),
                new Vector3(0.2500000f, 0.3535534f, 0.2500000f),
                new Vector3(0.1352990f, 0.3535534f, 0.3266407f),
                new Vector3(-0.0000000f, 0.3535534f, 0.3535534f),
                new Vector3(-0.1352991f, 0.3535534f, 0.3266407f),
                new Vector3(-0.2500000f, 0.3535534f, 0.2500000f),
                new Vector3(-0.3266408f, 0.3535534f, 0.1352990f),
                new Vector3(-0.3535534f, 0.3535534f, -0.0000000f),
                new Vector3(-0.3266407f, 0.3535534f, -0.1352990f),
                new Vector3(-0.2500000f, 0.3535534f, -0.2500000f),
                new Vector3(-0.1352989f, 0.3535534f, -0.3266408f),
                new Vector3(0.0000000f, 0.3535534f, -0.3535534f),
                new Vector3(0.1352991f, 0.3535534f, -0.3266407f),
                new Vector3(0.2500001f, 0.3535534f, -0.2499999f),
                new Vector3(0.3266408f, 0.3535534f, -0.1352990f),
                new Vector3(0.3865052f, 0.3171966f, 0.0000000f),
                new Vector3(0.3570842f, 0.3171966f, 0.1479091f),
                new Vector3(0.2733005f, 0.3171966f, 0.2733005f),
                new Vector3(0.1479091f, 0.3171966f, 0.3570842f),
                new Vector3(-0.0000000f, 0.3171966f, 0.3865052f),
                new Vector3(-0.1479092f, 0.3171966f, 0.3570842f),
                new Vector3(-0.2733005f, 0.3171966f, 0.2733005f),
                new Vector3(-0.3570843f, 0.3171966f, 0.1479091f),
                new Vector3(-0.3865052f, 0.3171966f, -0.0000000f),
                new Vector3(-0.3570842f, 0.3171966f, -0.1479091f),
                new Vector3(-0.2733004f, 0.3171966f, -0.2733005f),
                new Vector3(-0.1479090f, 0.3171966f, -0.3570843f),
                new Vector3(0.0000000f, 0.3171966f, -0.3865052f),
                new Vector3(0.1479092f, 0.3171966f, -0.3570842f),
                new Vector3(0.2733006f, 0.3171966f, -0.2733003f),
                new Vector3(0.3570843f, 0.3171966f, -0.1479091f),
                new Vector3(0.4157348f, 0.2777851f, 0.0000000f),
                new Vector3(0.3840889f, 0.2777851f, 0.1590948f),
                new Vector3(0.2939689f, 0.2777851f, 0.2939689f),
                new Vector3(0.1590948f, 0.2777851f, 0.3840889f),
                new Vector3(-0.0000000f, 0.2777851f, 0.4157348f),
                new Vector3(-0.1590949f, 0.2777851f, 0.3840889f),
                new Vector3(-0.2939689f, 0.2777851f, 0.2939689f),
                new Vector3(-0.3840889f, 0.2777851f, 0.1590948f),
                new Vector3(-0.4157348f, 0.2777851f, -0.0000000f),
                new Vector3(-0.3840889f, 0.2777851f, -0.1590948f),
                new Vector3(-0.2939689f, 0.2777851f, -0.2939689f),
                new Vector3(-0.1590947f, 0.2777851f, -0.3840890f),
                new Vector3(0.0000000f, 0.2777851f, -0.4157348f),
                new Vector3(0.1590949f, 0.2777851f, -0.3840889f),
                new Vector3(0.2939690f, 0.2777851f, -0.2939688f),
                new Vector3(0.3840889f, 0.2777851f, -0.1590948f),
                new Vector3(0.4409606f, 0.2356983f, 0.0000000f),
                new Vector3(0.4073945f, 0.2356983f, 0.1687483f),
                new Vector3(0.3118063f, 0.2356983f, 0.3118063f),
                new Vector3(0.1687483f, 0.2356983f, 0.4073945f),
                new Vector3(-0.0000000f, 0.2356983f, 0.4409606f),
                new Vector3(-0.1687484f, 0.2356983f, 0.4073945f),
                new Vector3(-0.3118063f, 0.2356983f, 0.3118063f),
                new Vector3(-0.4073946f, 0.2356983f, 0.1687483f),
                new Vector3(-0.4409606f, 0.2356983f, -0.0000000f),
                new Vector3(-0.4073945f, 0.2356983f, -0.1687483f),
                new Vector3(-0.3118062f, 0.2356983f, -0.3118063f),
                new Vector3(-0.1687482f, 0.2356983f, -0.4073946f),
                new Vector3(0.0000000f, 0.2356983f, -0.4409606f),
                new Vector3(0.1687484f, 0.2356983f, -0.4073945f),
                new Vector3(0.3118064f, 0.2356983f, -0.3118061f),
                new Vector3(0.4073945f, 0.2356983f, -0.1687483f),
                new Vector3(0.4619398f, 0.1913417f, 0.0000000f),
                new Vector3(0.4267767f, 0.1913417f, 0.1767767f),
                new Vector3(0.3266407f, 0.1913417f, 0.3266407f),
                new Vector3(0.1767767f, 0.1913417f, 0.4267767f),
                new Vector3(-0.0000000f, 0.1913417f, 0.4619398f),
                new Vector3(-0.1767767f, 0.1913417f, 0.4267767f),
                new Vector3(-0.3266407f, 0.1913417f, 0.3266407f),
                new Vector3(-0.4267767f, 0.1913417f, 0.1767766f),
                new Vector3(-0.4619398f, 0.1913417f, -0.0000000f),
                new Vector3(-0.4267767f, 0.1913417f, -0.1767767f),
                new Vector3(-0.3266407f, 0.1913417f, -0.3266408f),
                new Vector3(-0.1767765f, 0.1913417f, -0.4267767f),
                new Vector3(0.0000000f, 0.1913417f, -0.4619398f),
                new Vector3(0.1767768f, 0.1913417f, -0.4267766f),
                new Vector3(0.3266408f, 0.1913417f, -0.3266406f),
                new Vector3(0.4267767f, 0.1913417f, -0.1767767f),
                new Vector3(0.4784702f, 0.1451423f, 0.0000000f),
                new Vector3(0.4420488f, 0.1451423f, 0.1831026f),
                new Vector3(0.3383295f, 0.1451423f, 0.3383295f),
                new Vector3(0.1831026f, 0.1451423f, 0.4420488f),
                new Vector3(-0.0000000f, 0.1451423f, 0.4784702f),
                new Vector3(-0.1831027f, 0.1451423f, 0.4420488f),
                new Vector3(-0.3383295f, 0.1451423f, 0.3383295f),
                new Vector3(-0.4420488f, 0.1451423f, 0.1831025f),
                new Vector3(-0.4784702f, 0.1451423f, -0.0000000f),
                new Vector3(-0.4420488f, 0.1451423f, -0.1831026f),
                new Vector3(-0.3383294f, 0.1451423f, -0.3383296f),
                new Vector3(-0.1831025f, 0.1451423f, -0.4420489f),
                new Vector3(0.0000000f, 0.1451423f, -0.4784702f),
                new Vector3(0.1831027f, 0.1451423f, -0.4420488f),
                new Vector3(0.3383296f, 0.1451423f, -0.3383294f),
                new Vector3(0.4420488f, 0.1451423f, -0.1831026f),
                new Vector3(0.4903927f, 0.0975451f, 0.0000000f),
                new Vector3(0.4530637f, 0.0975451f, 0.1876651f),
                new Vector3(0.3467600f, 0.0975451f, 0.3467600f),
                new Vector3(0.1876651f, 0.0975451f, 0.4530637f),
                new Vector3(-0.0000000f, 0.0975451f, 0.4903927f),
                new Vector3(-0.1876652f, 0.0975451f, 0.4530637f),
                new Vector3(-0.3467600f, 0.0975451f, 0.3467600f),
                new Vector3(-0.4530638f, 0.0975451f, 0.1876651f),
                new Vector3(-0.4903927f, 0.0975451f, -0.0000000f),
                new Vector3(-0.4530637f, 0.0975451f, -0.1876651f),
                new Vector3(-0.3467599f, 0.0975451f, -0.3467600f),
                new Vector3(-0.1876650f, 0.0975451f, -0.4530638f),
                new Vector3(0.0000000f, 0.0975451f, -0.4903927f),
                new Vector3(0.1876652f, 0.0975451f, -0.4530637f),
                new Vector3(0.3467601f, 0.0975451f, -0.3467599f),
                new Vector3(0.4530638f, 0.0975451f, -0.1876651f),
                new Vector3(0.4975924f, 0.0490086f, 0.0000000f),
                new Vector3(0.4597154f, 0.0490086f, 0.1904204f),
                new Vector3(0.3518509f, 0.0490086f, 0.3518509f),
                new Vector3(0.1904203f, 0.0490086f, 0.4597154f),
                new Vector3(-0.0000000f, 0.0490086f, 0.4975924f),
                new Vector3(-0.1904204f, 0.0490086f, 0.4597154f),
                new Vector3(-0.3518509f, 0.0490086f, 0.3518509f),
                new Vector3(-0.4597155f, 0.0490086f, 0.1904203f),
                new Vector3(-0.4975924f, 0.0490086f, -0.0000000f),
                new Vector3(-0.4597154f, 0.0490086f, -0.1904203f),
                new Vector3(-0.3518509f, 0.0490086f, -0.3518510f),
                new Vector3(-0.1904202f, 0.0490086f, -0.4597155f),
                new Vector3(0.0000000f, 0.0490086f, -0.4975924f),
                new Vector3(0.1904204f, 0.0490086f, -0.4597154f),
                new Vector3(0.3518510f, 0.0490086f, -0.3518508f),
                new Vector3(0.4597154f, 0.0490086f, -0.1904203f),
                new Vector3(0.5000000f, -0.0000000f, 0.0000000f),
                new Vector3(0.4619398f, -0.0000000f, 0.1913417f),
                new Vector3(0.3535534f, -0.0000000f, 0.3535534f),
                new Vector3(0.1913417f, -0.0000000f, 0.4619398f),
                new Vector3(-0.0000000f, -0.0000000f, 0.5000000f),
                new Vector3(-0.1913418f, -0.0000000f, 0.4619398f),
                new Vector3(-0.3535534f, -0.0000000f, 0.3535534f),
                new Vector3(-0.4619398f, -0.0000000f, 0.1913416f),
                new Vector3(-0.5000000f, -0.0000000f, -0.0000000f),
                new Vector3(-0.4619398f, -0.0000000f, -0.1913417f),
                new Vector3(-0.3535533f, -0.0000000f, -0.3535534f),
                new Vector3(-0.1913416f, -0.0000000f, -0.4619398f),
                new Vector3(0.0000000f, -0.0000000f, -0.5000000f),
                new Vector3(0.1913418f, -0.0000000f, -0.4619397f),
                new Vector3(0.3535535f, -0.0000000f, -0.3535533f),
                new Vector3(0.4619398f, -0.0000000f, -0.1913417f),
            },
            triangles = new int[] {
               0,1,16,16,1,17,
                1,2,17,17,2,18,
                2,3,18,18,3,19,
                3,4,19,19,4,20,
                4,5,20,20,5,21,
                5,6,21,21,6,22,
                6,7,22,22,7,23,
                7,8,23,23,8,24,
                8,9,24,24,9,25,
                9,10,25,25,10,26,
                10,11,26,26,11,27,
                11,12,27,27,12,28,
                12,13,28,28,13,29,
                13,14,29,29,14,30,
                14,15,30,30,15,31,
                15,0,31,31,0,16,
                16,17,32,32,17,33,
                17,18,33,33,18,34,
                18,19,34,34,19,35,
                19,20,35,35,20,36,
                20,21,36,36,21,37,
                21,22,37,37,22,38,
                22,23,38,38,23,39,
                23,24,39,39,24,40,
                24,25,40,40,25,41,
                25,26,41,41,26,42,
                26,27,42,42,27,43,
                27,28,43,43,28,44,
                28,29,44,44,29,45,
                29,30,45,45,30,46,
                30,31,46,46,31,47,
                31,16,47,47,16,32,
                32,33,48,48,33,49,
                33,34,49,49,34,50,
                34,35,50,50,35,51,
                35,36,51,51,36,52,
                36,37,52,52,37,53,
                37,38,53,53,38,54,
                38,39,54,54,39,55,
                39,40,55,55,40,56,
                40,41,56,56,41,57,
                41,42,57,57,42,58,
                42,43,58,58,43,59,
                43,44,59,59,44,60,
                44,45,60,60,45,61,
                45,46,61,61,46,62,
                46,47,62,62,47,63,
                47,32,63,63,32,48,
                48,49,64,64,49,65,
                49,50,65,65,50,66,
                50,51,66,66,51,67,
                51,52,67,67,52,68,
                52,53,68,68,53,69,
                53,54,69,69,54,70,
                54,55,70,70,55,71,
                55,56,71,71,56,72,
                56,57,72,72,57,73,
                57,58,73,73,58,74,
                58,59,74,74,59,75,
                59,60,75,75,60,76,
                60,61,76,76,61,77,
                61,62,77,77,62,78,
                62,63,78,78,63,79,
                63,48,79,79,48,64,
                64,65,80,80,65,81,
                65,66,81,81,66,82,
                66,67,82,82,67,83,
                67,68,83,83,68,84,
                68,69,84,84,69,85,
                69,70,85,85,70,86,
                70,71,86,86,71,87,
                71,72,87,87,72,88,
                72,73,88,88,73,89,
                73,74,89,89,74,90,
                74,75,90,90,75,91,
                75,76,91,91,76,92,
                76,77,92,92,77,93,
                77,78,93,93,78,94,
                78,79,94,94,79,95,
                79,64,95,95,64,80,
                80,81,96,96,81,97,
                81,82,97,97,82,98,
                82,83,98,98,83,99,
                83,84,99,99,84,100,
                84,85,100,100,85,101,
                85,86,101,101,86,102,
                86,87,102,102,87,103,
                87,88,103,103,88,104,
                88,89,104,104,89,105,
                89,90,105,105,90,106,
                90,91,106,106,91,107,
                91,92,107,107,92,108,
                92,93,108,108,93,109,
                93,94,109,109,94,110,
                94,95,110,110,95,111,
                95,80,111,111,80,96,
                96,97,112,112,97,113,
                97,98,113,113,98,114,
                98,99,114,114,99,115,
                99,100,115,115,100,116,
                100,101,116,116,101,117,
                101,102,117,117,102,118,
                102,103,118,118,103,119,
                103,104,119,119,104,120,
                104,105,120,120,105,121,
                105,106,121,121,106,122,
                106,107,122,122,107,123,
                107,108,123,123,108,124,
                108,109,124,124,109,125,
                109,110,125,125,110,126,
                110,111,126,126,111,127,
                111,96,127,127,96,112,
                112,113,128,128,113,129,
                113,114,129,129,114,130,
                114,115,130,130,115,131,
                115,116,131,131,116,132,
                116,117,132,132,117,133,
                117,118,133,133,118,134,
                118,119,134,134,119,135,
                119,120,135,135,120,136,
                120,121,136,136,121,137,
                121,122,137,137,122,138,
                122,123,138,138,123,139,
                123,124,139,139,124,140,
                124,125,140,140,125,141,
                125,126,141,141,126,142,
                126,127,142,142,127,143,
                127,112,143,143,112,128,
                128,129,144,144,129,145,
                129,130,145,145,130,146,
                130,131,146,146,131,147,
                131,132,147,147,132,148,
                132,133,148,148,133,149,
                133,134,149,149,134,150,
                134,135,150,150,135,151,
                135,136,151,151,136,152,
                136,137,152,152,137,153,
                137,138,153,153,138,154,
                138,139,154,154,139,155,
                139,140,155,155,140,156,
                140,141,156,156,141,157,
                141,142,157,157,142,158,
                142,143,158,158,143,159,
                143,128,159,159,128,144,
                144,145,160,160,145,161,
                145,146,161,161,146,162,
                146,147,162,162,147,163,
                147,148,163,163,148,164,
                148,149,164,164,149,165,
                149,150,165,165,150,166,
                150,151,166,166,151,167,
                151,152,167,167,152,168,
                152,153,168,168,153,169,
                153,154,169,169,154,170,
                154,155,170,170,155,171,
                155,156,171,171,156,172,
                156,157,172,172,157,173,
                157,158,173,173,158,174,
                158,159,174,174,159,175,
                159,144,175,175,144,160,
                160,161,176,176,161,177,
                161,162,177,177,162,178,
                162,163,178,178,163,179,
                163,164,179,179,164,180,
                164,165,180,180,165,181,
                165,166,181,181,166,182,
                166,167,182,182,167,183,
                167,168,183,183,168,184,
                168,169,184,184,169,185,
                169,170,185,185,170,186,
                170,171,186,186,171,187,
                171,172,187,187,172,188,
                172,173,188,188,173,189,
                173,174,189,189,174,190,
                174,175,190,190,175,191,
                175,160,191,191,160,176,
                176,177,192,192,177,193,
                177,178,193,193,178,194,
                178,179,194,194,179,195,
                179,180,195,195,180,196,
                180,181,196,196,181,197,
                181,182,197,197,182,198,
                182,183,198,198,183,199,
                183,184,199,199,184,200,
                184,185,200,200,185,201,
                185,186,201,201,186,202,
                186,187,202,202,187,203,
                187,188,203,203,188,204,
                188,189,204,204,189,205,
                189,190,205,205,190,206,
                190,191,206,206,191,207,
                191,176,207,207,176,192,
                192,193,208,208,193,209,
                193,194,209,209,194,210,
                194,195,210,210,195,211,
                195,196,211,211,196,212,
                196,197,212,212,197,213,
                197,198,213,213,198,214,
                198,199,214,214,199,215,
                199,200,215,215,200,216,
                200,201,216,216,201,217,
                201,202,217,217,202,218,
                202,203,218,218,203,219,
                203,204,219,219,204,220,
                204,205,220,220,205,221,
                205,206,221,221,206,222,
                206,207,222,222,207,223,
                207,192,223,223,192,208,
                208,209,224,224,209,225,
                209,210,225,225,210,226,
                210,211,226,226,211,227,
                211,212,227,227,212,228,
                212,213,228,228,213,229,
                213,214,229,229,214,230,
                214,215,230,230,215,231,
                215,216,231,231,216,232,
                216,217,232,232,217,233,
                217,218,233,233,218,234,
                218,219,234,234,219,235,
                219,220,235,235,220,236,
                220,221,236,236,221,237,
                221,222,237,237,222,238,
                222,223,238,238,223,239,
                223,208,239,239,208,224,
                224,225,240,240,225,241,
                225,226,241,241,226,242,
                226,227,242,242,227,243,
                227,228,243,243,228,244,
                228,229,244,244,229,245,
                229,230,245,245,230,246,
                230,231,246,246,231,247,
                231,232,247,247,232,248,
                232,233,248,248,233,249,
                233,234,249,249,234,250,
                234,235,250,250,235,251,
                235,236,251,251,236,252,
                236,237,252,252,237,253,
                237,238,253,253,238,254,
                238,239,254,254,239,255,
                239,224,255,255,224,240,
                240,241,256,256,241,257,
                241,242,257,257,242,258,
                242,243,258,258,243,259,
                243,244,259,259,244,260,
                244,245,260,260,245,261,
                245,246,261,261,246,262,
                246,247,262,262,247,263,
                247,248,263,263,248,264,
                248,249,264,264,249,265,
                249,250,265,265,250,266,
                250,251,266,266,251,267,
                251,252,267,267,252,268,
                252,253,268,268,253,269,
                253,254,269,269,254,270,
                254,255,270,270,255,271,
                255,240,271,271,240,256,
            }
        };
            
        public static readonly Material CompassMaterial;
        public static readonly Material ArrowMaterial;
        public static readonly Material LongArrowMaterial;
        public static readonly Material PreviewMaterial;

        public static readonly RaycastHit[] RaycastHitCache = new RaycastHit[256];

        static KalderaEditorUtils() {
            TrashIconContent = new GUIContent(LoadAssetPath("Icons/TrashIcon.png"), "Remove\nRemoves this item");
            ClearIconContent = new GUIContent(LoadAssetPath("Icons/ClearIcon.png"), "Clear\nClears away all prefabs in this group");
            MoveUpIconContent = new GUIContent(LoadAssetPath("Icons/MoveUpIcon.png"), "Move up");
            MoveDownIconContent = new GUIContent(LoadAssetPath("Icons/MoveDownIcon.png"), "Move down");
            ShowInProjectContent = new GUIContent(LoadAssetPath("Icons/ShowInProjectIcon.png"), "Show asset in project");

            TitleGuiContent = new GUIContent(PluginName, LoadAssetPath("Kaldera_Logo_colored_tiny"));
            SelectPaletteCollectionContent = new GUIContent("Palette Collection", "Palette Collection\nYou can select either a Palette, or a Palette Set to paint for.");
            SelectPaletteContent = new GUIContent("Palette", "Palette\nYou can select either a Palette, or a Palette Set to paint for.");
            SelectPlacementModeContent = new GUIContent("Collider mode", "Collider mode\nDetermines what colliders to check against");
            SelectRaycastModeContent = new GUIContent("Paint mode", "Paint mode\nDetermines from what angle automatic collider checks are done from");
            SelectToolsContent = new GUIContent("Tool:", "Determines the shape and properties of the tool.");

            // Options
            BrushDistanceContent = new GUIContent("Distance", "Distance\nMax distance the collision checks in the scene are made at. Use a large number if painting large scenes zoomed out and a small value");
            AvoidCollisionContent = new GUIContent(LoadAssetPath("Icons/AllowCollisionIcon.png"), "Avoid Collider overlap\nUses collider casts against the scene to avoid painting Prefabs colliding with other Colliders in the scene.\n\nThis can be disabled per Prefab Item in the Advanced Options section.\n\nIf the Prefab has no Colliders, the option does nothing.");
            ParentObjectToBaseObjectContent = new GUIContent(LoadAssetPath("Icons/ChildObjectsIcon.png"), "Child prefabs to parent collider\nAny placed prefabs will be placed as a child object to the found parent collider");
            OrientToGroundNormalContent = new GUIContent(LoadAssetPath("Icons/OrientToNormalIcon.png"), "Orient to ground normal\nPlaced prefab will be placed perpendicular to the ground where they are placed");
            OrientToBrushNormalContent = new GUIContent(LoadAssetPath("Icons/OrientToNormalIcon.png"), "Orient to brush normal\nPrefabs will be rotated along the brush direction axis, along side any other rotation");
            MaintainRotationContent = new GUIContent(LoadAssetPath("Icons/MaintainRotationIcon.png"), "Maintain rotation\nThe initial rotation of the next prefabs be the same as the last placed Prefab's orientation. Only works for single brush");
            OrientWithBrushContent = new GUIContent(LoadAssetPath("Icons/OrientToPlacementIcon.png"), "Orient with stroke\nRandomization of the Y axis will be ignored and placed prefabs will instead be rotated based on the brush's direction.");
            MoreInformationContent = new GUIContent(LoadAssetPath("Icons/MoreInforIcon.png"), "Show more information");
            AvoidCollisionSpacingContent = new GUIContent("Collider size", "Adjusts the Size of Colliders to allow for tweaking of the collision detection. Lower value means Prefabs can be placed closed together.");
            BrushSizeContent = new GUIContent("Brush Size", "Brush Size\nDetermines the size of the tool.\n[Shft] + [1-5]");
            BrushSpacingContent = new GUIContent("Prefab Spacing", "Prefab Spacing\nSpace between the individual prefabs");
            ObjectDensityContent = new GUIContent("Prefab Density", "Prefab Density\nDetermines the space between the individual prefabs.");
            SprayIntensityContent = new GUIContent("Spray Intensity", "Spray Intensity\nDetermines how many Prefabs are painted per second when spraying");
            BrushDistributionContent = new GUIContent("Distribution", "Distribution\nDetermines how multiple prefabs are spaced out.");
            ObjectLimitContent = new GUIContent("Prefabs limit", "Prefabs limit\nDetermines to max amount of prefabs generated for a single stroke.\n\nNote!\nIncreasing the value too much can cause the editor to be slow and/or unresponsive");

            PaletteLabelContent = new GUIContent("Palette");

            VersionContent = new GUIContent($"{PluginName} - Version {Version}");

            WarningIconTexture = LoadAssetPath("Icons/IconWarning.png");

            var onTopShader = Shader.Find("Kaldera/OnTop");
            CompassMaterial = new Material(onTopShader) {
                mainTexture = LoadAssetPathAsync("Icons/Kaldera_EditorWidget.png").asset as Texture2D
            };
            ArrowMaterial = new Material(onTopShader) {
                mainTexture = LoadAssetPathAsync("Icons/Kaldera_EditorWidget_arrow.png").asset as Texture2D
            };
            LongArrowMaterial = new Material(onTopShader) {
                mainTexture = LoadAssetPathAsync("Icons/Kaldera_EditorWidget_arrow_long.png").asset as Texture2D
            };

            var previewShader = Shader.Find("Kaldera/Preview");
            PreviewMaterial = new Material(previewShader) {
                color = new Color(1f, 0f, 0f, 0.75f)
            };
        }

        public static Texture2D LoadAssetPath(string localPath) => Resources.Load<Texture2D>(Path.GetFileNameWithoutExtension(localPath));

        public static ResourceRequest LoadAssetPathAsync(string localPath) => Resources.LoadAsync<Texture2D>(Path.GetFileNameWithoutExtension(localPath));
    }
}
