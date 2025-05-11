using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MaskMapGenerator))]
public class MaskMapToolEditor : Editor
{
    private MaskMapGenerator maskMapGenerator;

    private void OnEnable()
    {
        maskMapGenerator = (MaskMapGenerator)target;
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GUILayout.Label("Mask Map Settings", EditorStyles.boldLabel);

        maskMapGenerator.metallicTexture = (Texture2D)EditorGUILayout.ObjectField("Metallic Texture", maskMapGenerator.metallicTexture, typeof(Texture2D), false);
        maskMapGenerator.aoTexture = (Texture2D)EditorGUILayout.ObjectField("AO Texture", maskMapGenerator.aoTexture, typeof(Texture2D), false);
        maskMapGenerator.detailMaskTexture = (Texture2D)EditorGUILayout.ObjectField("Detail Mask Texture", maskMapGenerator.detailMaskTexture, typeof(Texture2D), false);
        maskMapGenerator.smoothingTexture = (Texture2D)EditorGUILayout.ObjectField("Smoothness Texture", maskMapGenerator.smoothingTexture, typeof(Texture2D), false);

        maskMapGenerator.metallicAdjustment = EditorGUILayout.Slider("Metallic Adjustment", maskMapGenerator.metallicAdjustment, 0f, 1f);
        maskMapGenerator.aoAdjustment = EditorGUILayout.Slider("AO Adjustment", maskMapGenerator.aoAdjustment, 0f, 1f);
        maskMapGenerator.detailMaskAdjustment = EditorGUILayout.Slider("Detail Mask Adjustment", maskMapGenerator.detailMaskAdjustment, 0f, 1f);
        maskMapGenerator.smoothingAdjustment = EditorGUILayout.Slider("Smoothness Adjustment", maskMapGenerator.smoothingAdjustment, 0f, 1f);

        if (GUILayout.Button("Generate Mask Map"))
        {
            maskMapGenerator.GenerateMaskMap();
        }
    }
}