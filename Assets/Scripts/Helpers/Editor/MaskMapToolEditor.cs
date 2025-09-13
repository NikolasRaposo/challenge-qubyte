using UnityEditor;
using UnityEngine;
namespace EditorTools.Editor {
    /// <summary>
    /// Custom editor for the MaskMapGenerator component.
    /// This script creates a user-friendly interface in the Unity Inspector,
    /// including a button to trigger the mask map generation process.
    /// </summary>
    [CustomEditor(typeof(MaskMapGenerator))]
    public class MaskMapToolEditor : UnityEditor.Editor {
        // A reference to the MaskMapGenerator component being inspected.
        private MaskMapGenerator _maskMapGenerator;

        /// <summary>
        /// This method is called when the editor object is enabled.
        /// It gets a reference to the component this editor is inspecting.
        /// </summary>
        private void OnEnable() {
            // 'target' is a property of the Editor class that refers to the object being inspected.
            // We cast it to our specific component type.
            _maskMapGenerator = (MaskMapGenerator)target;
        }

        /// <summary>
        /// This method is called to draw the custom GUI in the Inspector.
        /// </summary>
        public override void OnInspectorGUI() {
            // Draw the default inspector fields (like the script reference).
            DrawDefaultInspector();

            // --- Custom GUI Section ---

            // Add a bold label to create a section header.
            GUILayout.Label("Mask Map Settings", EditorStyles.boldLabel);

            // Create object fields for assigning the input textures.
            _maskMapGenerator.metallicTexture = (Texture2D)EditorGUILayout.ObjectField("Metallic Texture", _maskMapGenerator.metallicTexture, typeof(Texture2D), false);
            _maskMapGenerator.aoTexture = (Texture2D)EditorGUILayout.ObjectField("AO Texture", _maskMapGenerator.aoTexture, typeof(Texture2D), false);
            _maskMapGenerator.detailMaskTexture = (Texture2D)EditorGUILayout.ObjectField("Detail Mask Texture", _maskMapGenerator.detailMaskTexture, typeof(Texture2D), false);
            _maskMapGenerator.smoothingTexture = (Texture2D)EditorGUILayout.ObjectField("Smoothness Texture", _maskMapGenerator.smoothingTexture, typeof(Texture2D), false);

            // Create sliders to adjust the intensity of each channel.
            _maskMapGenerator.metallicAdjustment = EditorGUILayout.Slider("Metallic Adjustment", _maskMapGenerator.metallicAdjustment, 0f, 1f);
            _maskMapGenerator.aoAdjustment = EditorGUILayout.Slider("AO Adjustment", _maskMapGenerator.aoAdjustment, 0f, 1f);
            _maskMapGenerator.detailMaskAdjustment = EditorGUILayout.Slider("Detail Mask Adjustment", _maskMapGenerator.detailMaskAdjustment, 0f, 1f);
            _maskMapGenerator.smoothingAdjustment = EditorGUILayout.Slider("Smoothness Adjustment", _maskMapGenerator.smoothingAdjustment, 0f, 1f);

            // Add a button to the inspector.
            if (GUILayout.Button("Generate Mask Map")) {
                // If the button is clicked, call the GenerateMaskMap method on the target component.
                _maskMapGenerator.GenerateMaskMap();
            }
        }
    }
}