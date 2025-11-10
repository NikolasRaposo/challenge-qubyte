using System.IO;
using UnityEditor;
using UnityEngine;
using FlowSystem;

namespace FlowSystem.Editor
{
    public static class CreateBlockGraphSample
    {
        [MenuItem("Flow/Create Sample BlockGraph.asset")]
        public static void CreateSample()
        {
            var asset = ScriptableObject.CreateInstance<BlockGraph>();

            // Definir destino: Assets/_Project/SOs/BlockGraph.asset (se existir), senão Assets/BlockGraph.asset
            var preferredDir = "Assets/_Project/SOs";
            var fallbackPath = "Assets/BlockGraph.asset";
            string path;

            if (Directory.Exists(preferredDir))
            {
                path = Path.Combine(preferredDir, "BlockGraph.asset");
            }
            else
            {
                path = fallbackPath;
            }

            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.FocusProjectWindow();
            Selection.activeObject = asset;
            Debug.Log($"[Flow] BlockGraph.asset criado em: {path}");
        }
    }
}