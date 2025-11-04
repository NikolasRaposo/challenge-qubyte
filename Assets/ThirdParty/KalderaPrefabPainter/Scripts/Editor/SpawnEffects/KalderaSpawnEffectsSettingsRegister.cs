using CollisionBear.WorldEditor.Utils;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace CollisionBear.WorldEditor
{
    public static class KalderaSpawnEffectsSettingsRegister
    {

        private const string KeybindingDisclaimer = "Make sure there's no overlap with Unity Shortcuts, or the shortcuts of other editor extensions. Any overlap may cause the Kaldera, Unity, or other extension, to ignore certain input.";

        private static readonly GUIContent KeybindingsContent = new GUIContent("Keybindings", "");
        private static readonly GUIContent SpawnEffectSettingsContent = new GUIContent("Spawn effect settings", "");
        private static readonly GUIContent PaletteKeybindingsContent = new GUIContent("Palette", "");
        private static readonly GUIContent SceneViewKeybindingsContent = new GUIContent("Scene view", "");

        private static readonly GUIContent UseSpawnEffectContent = new GUIContent("Use Spawn effect", "Plays a quick spawn effect when placing Prefabs");
        private static readonly GUIContent SpawnDelayContent = new GUIContent("Spawn delay", "Delay between spawned Prefab batches");
        private static readonly GUIContent SpawnGrowTimeContent = new GUIContent("Spawn duration", "Duration Prefabs will take to grow into full scale");
        private static readonly GUIContent SpawnBatchDurationContent = new GUIContent("Max timer", "Max time for a full set to be spawned");
        private static readonly GUIContent PlaySpawnSoundContent = new GUIContent("Play sound", "Play Spawn Sound when a Prefab spawns");

        [SettingsProvider]
        public static SettingsProvider CreateMyCustomSettingsProvider()
        {
            var provider = new SettingsProvider("Collision Bear/", SettingsScope.User) {
                label = "Kaldera Prefab Painter",
                guiHandler = OptionsPageGuiHandler,
                keywords = new HashSet<string>(new[] { "Kaldera", "Prefab painter", "Prefab Spawner", "World Editor", "Collision Bear" })
            };

            return provider;
        }

        private static void OptionsPageGuiHandler(string search)
        {
            using (var changeDetection = new EditorGUI.ChangeCheckScope()) {
                KeybindingsSettings();
                EditorGUILayout.Space();
                SpawnEffectSettings();

                if (changeDetection.changed) {
                    KalderaSpawnEffectsSettings.SaveToEditorPrefs();
                }
            }
        }

        private static void KeybindingsSettings()
        {
            using (new EditorGUILayout.VerticalScope(GUI.skin.box)) {
                EditorGUILayout.LabelField(KeybindingsContent, EditorStyles.boldLabel);

                EditorGUILayout.HelpBox(KeybindingDisclaimer, MessageType.Info);
                EditorGUILayout.Space();

                using (new EditorGUILayout.HorizontalScope()) {
                    EditorGUILayout.LabelField("Tools");
                    EditorGUILayout.LabelField("Modifiers");
                    EditorGUILayout.LabelField("Key");
                }

                foreach (var keybinding in KalderaSpawnEffectsSettings.ToolsHotKeys) {
                    KeyBindingSetting(keybinding);
                }

                EditorGUILayout.Space();
                KeyBindingSetting(KalderaSpawnEffectsSettings.CyclePrefabHotkey);
                KeyBindingSetting(KalderaSpawnEffectsSettings.ScalePrefabHotkey);

                EditorGUILayout.Space();
                foreach (var keybinding in KalderaSpawnEffectsSettings.ToolSizeHotKeys) {
                    KeyBindingSetting(keybinding);
                }

                EditorGUILayout.LabelField(PaletteKeybindingsContent, EditorStyles.boldLabel);
                foreach (var keybinding in KalderaSpawnEffectsSettings.PaletteHotkeys) {
                    Instruction(keybinding);
                }

                EditorGUILayout.Space();

                EditorGUILayout.LabelField(SceneViewKeybindingsContent, EditorStyles.boldLabel);
                foreach (var keybinding in KalderaSpawnEffectsSettings.SceneViewHotkeys) {
                    Instruction(keybinding);
                }

                EditorGUILayout.Space();

                if(GUILayout.Button("Reset keybindings to default")){
                    if(EditorUtility.DisplayDialog("Reset settings?", "All settings will reset to their default value. Continue?", "Ok", "Cancel")) {
                        EditorWindow.GetWindow<PaletteWindow>().Close();
                        KalderaSpawnEffectsSettings.ResetToDefaults();
                        PaletteWindow.ShowWindow();
                    }
                }
            }
        }

        private static void KeyBindingSetting(KeyBinding keyBinding)
        {
            using (new EditorGUILayout.HorizontalScope()) {
                using (var changeDetection = new EditorGUI.ChangeCheckScope()) {
                    EditorGUILayout.LabelField(keyBinding.SettingName);
                    keyBinding.Modifiers = (ShortcutModifiers)EditorGUILayout.EnumFlagsField(GUIContent.none, keyBinding.Modifiers);
                    keyBinding.KeyCode = (BindingKeyCode)EditorGUILayout.EnumPopup(GUIContent.none, keyBinding.KeyCode);
                }
            }
        }

        private static void Instruction(KeyInstruction keyBinding)
        {
            using (new EditorGUILayout.HorizontalScope()) {
                EditorGUILayout.LabelField(keyBinding.SettingName);
                EditorGUILayout.LabelField(keyBinding.Description);
            }
        }

        private static void SpawnEffectSettings()
        {
            using (new EditorGUILayout.VerticalScope(GUI.skin.box)) {
                EditorGUILayout.LabelField(SpawnEffectSettingsContent, EditorStyles.boldLabel);
                KalderaSpawnEffectsSettings.UseSpawnEffects = EditorGUILayout.Toggle(UseSpawnEffectContent, KalderaSpawnEffectsSettings.UseSpawnEffects);

                using (new EditorGUI.DisabledGroupScope(!KalderaSpawnEffectsSettings.UseSpawnEffects)) {
                    KalderaSpawnEffectsSettings.SpawnDelay = EditorGUILayout.Slider(SpawnDelayContent, KalderaSpawnEffectsSettings.SpawnDelay, 0, 0.2f);
                    KalderaSpawnEffectsSettings.SpawnGrowTime = EditorGUILayout.Slider(SpawnGrowTimeContent, KalderaSpawnEffectsSettings.SpawnGrowTime, 0f, 1f);
                    KalderaSpawnEffectsSettings.SpawnBatchDuration = EditorGUILayout.Slider(SpawnBatchDurationContent, KalderaSpawnEffectsSettings.SpawnBatchDuration, 0, 1f);
                    KalderaSpawnEffectsSettings.PlaySpawnSound = EditorGUILayout.Toggle(PlaySpawnSoundContent, KalderaSpawnEffectsSettings.PlaySpawnSound);
                }
            }
        }
    }
}
