using CollisionBear.WorldEditor.Utils;
using UnityEditor;
using UnityEngine;

namespace CollisionBear.WorldEditor
{
    class KalderaSpawnEffectsSettings : Object
    {
        public const string SpawnAudioClipPath = "sfx/pop";

        private const string KalderaSettingsPath = "KalderaSpawnEffects";

        public static KeyInstruction[] PaletteHotkeys = new KeyInstruction[] {
            new KeyInstruction("Select", $"LMB selects the item for painting"),
            new KeyInstruction("Open", $"RMB opens the Palette configration"),
            new KeyInstruction("Prefab", $"MMB opens the Prefab editor"),
            new KeyInstruction("Multi select", $"Hold down [{ShortcutModifiers.Shift}] + {KeyCode.Mouse0} to select / deselect item"),
        };

        public static KeyInstruction[] SceneViewHotkeys = new KeyInstruction[] {
            new KeyInstruction("Pre rotate", $"SceneHold down [{ShortcutModifiers.Shift}] + move the mouse"),
            new KeyInstruction("Rotate in place", $"Hold down [{ShortcutModifiers.Shift}] + {KeyCode.Mouse0} + drag"),
        };

        public static KeyBinding[] ToolsHotKeys = new KeyBinding[] {
            new KeyBinding("Paint brush", "Always place a single object", BindingKeyCode.Q, ShortcutModifiers.Shift),
            new KeyBinding("Line brush", "\"Places multiple objects in a line from where you start drag to the end", BindingKeyCode.W, ShortcutModifiers.Shift),
            new KeyBinding("Path brush", "Places multiple objects in a path along the mouse drag", BindingKeyCode.E, ShortcutModifiers.Shift),
            new KeyBinding("Circle brush","Places multiple objects (always at least 1) in a circle", BindingKeyCode.R, ShortcutModifiers.Shift),
            new KeyBinding("Square brush", "Places multiple objects (always at least 1) in a square", BindingKeyCode.T, ShortcutModifiers.Shift),
            new KeyBinding("Spray brush", "Slowly plots down objects while keeping the mouse button pressed", BindingKeyCode.Y, ShortcutModifiers.Shift),
            new KeyBinding("Eraser brush", "Erases prefabs in the selected area", BindingKeyCode.U, ShortcutModifiers.Shift),
            new KeyBinding("Clear", "Clears the current selection and returns the Scene window to normal", BindingKeyCode.Escape, ShortcutModifiers.None),
        };

        // Custom controllers for stuff
        public static KeyBinding CyclePrefabHotkey = new KeyBinding("Cycle Prefabs", BindingKeyCode.ScrollWheel, ShortcutModifiers.Shift);

        public static KeyBinding ScalePrefabHotkey = new KeyBinding("Scale Prefabs", BindingKeyCode.ScrollWheel, ShortcutModifiers.Control);

        public static KeyBinding[] ToolSizeHotKeys = new KeyBinding[] {
            new KeyBinding("Brush preset size 1", "Preset size 1 for area brushes", BindingKeyCode.Alpha1, ShortcutModifiers.Shift),
            new KeyBinding("Brush preset size 2", "Preset size 2 for area brushes", BindingKeyCode.Alpha2, ShortcutModifiers.Shift),
            new KeyBinding("Brush preset size 3", "Preset size 3 for area brushes", BindingKeyCode.Alpha3, ShortcutModifiers.Shift),
            new KeyBinding("Brush preset size 4","Preset size 4 for area brushes", BindingKeyCode.Alpha4, ShortcutModifiers.Shift),
            new KeyBinding("Brush preset size 5", "Preset size 5 for area brushes", BindingKeyCode.Alpha5, ShortcutModifiers.Shift)
        };

        public static bool UseSpawnEffects = false;
        public static float SpawnGrowTime = .5f;
        public static float SpawnDelay = .08f;
        public static float SpawnBatchDuration = .64f;
        public static bool PlaySpawnSound = true;

        public static readonly AnimationCurve SpawnAnimation = new AnimationCurve() {
            keys = new[] {
                new Keyframe(0, 0, 0, 0),
                new Keyframe(0.33f, 1, 2, 2),
                new Keyframe(0.66f, 1, 2, 2),
                new Keyframe(1, 1, 0.5f, 0)
            }
        };

        static KalderaSpawnEffectsSettings()
        {
            UseSpawnEffects = EditorPrefs.GetBool(KalderaSettingsPath + nameof(UseSpawnEffects), UseSpawnEffects);
            SpawnGrowTime = EditorPrefs.GetFloat(KalderaSettingsPath + nameof(SpawnGrowTime), SpawnGrowTime);
            SpawnDelay = EditorPrefs.GetFloat(KalderaSettingsPath + nameof(SpawnDelay), SpawnDelay);
            SpawnBatchDuration = EditorPrefs.GetFloat(KalderaSettingsPath + nameof(SpawnBatchDuration), SpawnBatchDuration);
            PlaySpawnSound = EditorPrefs.GetBool(KalderaSettingsPath + nameof(PlaySpawnSound), PlaySpawnSound);

            for (int i = 0; i < ToolsHotKeys.Length; i++) {
                LoadToolKeybinding("tools", i, ToolsHotKeys[i]);
            }

            for (int i = 0; i < ToolSizeHotKeys.Length; i++) {
                LoadToolKeybinding("presets", i, ToolSizeHotKeys[i]);
            }

            LoadToolKeybinding("misc", 0, CyclePrefabHotkey);
            LoadToolKeybinding("misc", 1, ScalePrefabHotkey);
        }

        public static void ResetToDefaults()
        {
            ToolsHotKeys = new KeyBinding[] {
                new KeyBinding("Paint brush", "Always place a single object", BindingKeyCode.Q, ShortcutModifiers.Shift),
                new KeyBinding("Line brush", "\"Places multiple objects in a line from where you start drag to the end", BindingKeyCode.W, ShortcutModifiers.Shift),
                new KeyBinding("Path brush", "Places multiple objects in a path along the mouse drag", BindingKeyCode.E, ShortcutModifiers.Shift),
                new KeyBinding("Circle brush","Places multiple objects (always at least 1) in a circle", BindingKeyCode.R, ShortcutModifiers.Shift),
                new KeyBinding("Square brush", "Places multiple objects (always at least 1) in a square", BindingKeyCode.T, ShortcutModifiers.Shift),
                new KeyBinding("Spray brush", "Slowly plots down objects while keeping the mouse button pressed", BindingKeyCode.Y, ShortcutModifiers.Shift),
                new KeyBinding("Eraser brush", "Erases prefabs in the selected area", BindingKeyCode.U, ShortcutModifiers.Shift),
                new KeyBinding("Clear", "Clears the current selection and returns the Scene window to normal", BindingKeyCode.Escape, ShortcutModifiers.None)
            };

            CyclePrefabHotkey = new KeyBinding("Cycle Prefabs", BindingKeyCode.ScrollWheel, ShortcutModifiers.Shift);

            ScalePrefabHotkey = new KeyBinding("Scale Prefabs", BindingKeyCode.ScrollWheel, ShortcutModifiers.Control);

            ToolSizeHotKeys = new KeyBinding[] {
                new KeyBinding("Brush preset size 1", "Preset size 1 for area brushes", BindingKeyCode.Alpha1, ShortcutModifiers.Shift),
                new KeyBinding("Brush preset size 2", "Preset size 2 for area brushes", BindingKeyCode.Alpha2, ShortcutModifiers.Shift),
                new KeyBinding("Brush preset size 3", "Preset size 3 for area brushes", BindingKeyCode.Alpha3, ShortcutModifiers.Shift),
                new KeyBinding("Brush preset size 4","Preset size 4 for area brushes", BindingKeyCode.Alpha4, ShortcutModifiers.Shift),
                new KeyBinding("Brush preset size 5", "Preset size 5 for area brushes", BindingKeyCode.Alpha5, ShortcutModifiers.Shift)
            };

            SaveToEditorPrefs();
        }

        private static string GetKey(string prefix, int index, string postix) => $"{KalderaSettingsPath}_{prefix}_{index}_{postix}";

        private static void LoadToolKeybinding(string prefix, int index, KeyBinding result)
        {
            var modifiers = EditorPrefs.GetInt(GetKey(prefix, index, "modifiers"));
            var keycode = EditorPrefs.GetInt(GetKey(prefix, index, "keycode"));

            result.Modifiers = (ShortcutModifiers)modifiers;
            result.KeyCode = (BindingKeyCode)keycode;
        }

        public static void SaveToEditorPrefs()
        {
            EditorPrefs.SetBool(KalderaSettingsPath + nameof(UseSpawnEffects), UseSpawnEffects);
            EditorPrefs.SetFloat(KalderaSettingsPath + nameof(SpawnGrowTime), SpawnGrowTime);
            EditorPrefs.SetFloat(KalderaSettingsPath + nameof(SpawnDelay), SpawnDelay);
            EditorPrefs.SetFloat(KalderaSettingsPath + nameof(SpawnBatchDuration), SpawnBatchDuration);
            EditorPrefs.SetBool(KalderaSettingsPath + nameof(PlaySpawnSound), PlaySpawnSound);

            SaveToolKeybinding("misc", 0, CyclePrefabHotkey);
            SaveToolKeybinding("misc", 1, ScalePrefabHotkey);

            for (int i = 0; i < ToolsHotKeys.Length; i++) {
                SaveToolKeybinding("tools", i, ToolsHotKeys[i]);
            }

            for (int i = 0; i < ToolSizeHotKeys.Length; i++) {
                SaveToolKeybinding("presets", i, ToolSizeHotKeys[i]);
            }
        }

        private static void SaveToolKeybinding(string prefix, int index, KeyBinding result)
        {
            EditorPrefs.SetInt(GetKey(prefix, index, "modifiers"), (int)result.Modifiers);
            EditorPrefs.SetInt(GetKey(prefix, index, "keycode"), (int)result.KeyCode);
        }
    }
}
