// Localiza��o: Por exemplo, Assets/Editor/PrefabReferenceHelperEditor.cs
using UnityEditor;
using UnityEngine;
using System;

[Obsolete("Obsolete")]

// CustomEditor para o componente PrefabReferenceHelper
[CustomEditor(typeof(PrefabReferenceHelper))]
public class PrefabReferenceHelperEditor : Editor
{
    // Este m�todo � chamado quando o Inspector do PrefabReferenceHelper � desenhado
    public override void OnInspectorGUI()
    {
        // Desenha o Inspector padr�o para as vari�veis p�blicas
        DrawDefaultInspector();

        PrefabReferenceHelper myScript = (PrefabReferenceHelper)target;

        // Se o objeto inspecionado � um asset de prefab (n�o uma inst�ncia na cena)
        if (PrefabUtility.IsPartOfPrefabAsset(myScript.gameObject))
        {
            // Obt�m o caminho do asset do prefab
            string currentAssetPath = AssetDatabase.GetAssetPath(myScript.gameObject);

            // Verifica se o caminho no campo precisa ser atualizado
            if (myScript.prefabAssetPath != currentAssetPath)
            {
                // Registra a mudan�a para permitir Undo
                Undo.RecordObject(myScript, "Update Prefab Asset Path");
                myScript.prefabAssetPath = currentAssetPath;
                // Marca o asset como modificado para que seja salvo
                EditorUtility.SetDirty(myScript);
            }
            EditorGUILayout.HelpBox("Caminho do prefab preenchido automaticamente.", MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox("Este componente funciona melhor em assets de Prefab. O caminho ser� preenchido automaticamente ao salvar o prefab.", MessageType.Warning);
        }
    }

    // Opcional: Para garantir que o caminho seja preenchido ao salvar o asset de prefab, mesmo sem abrir o Inspector.
    // Isso � mais robusto.
    [InitializeOnLoadMethod]
    private static void OnProjectLoadedInEditor()
    {
        EditorApplication.hierarchyChanged += CheckAllPrefabReferenceHelpers;
    }

    private static void CheckAllPrefabReferenceHelpers()
    {
        // Esta fun��o pode ser pesada se executada com muita frequ�ncia, ent�o seja cauteloso.
        // � melhor usar um AssetPostprocessor para quando assets s�o importados/salvos.
        // A fun��o DrawDefaultInspector + EditorUtility.SetDirty j� cobre a maioria dos casos.
    }

    // Usaremos um AssetPostprocessor para maior efici�ncia e para capturar a cria��o/modifica��o do asset de prefab
    class PrefabReferencePostprocessor : AssetPostprocessor
    {
        void OnPostprocessGameObjectWithUserProperties(GameObject g, string[] propNames, System.Object[] values)
        {
            // Este m�todo � chamado quando um GameObject com propriedades de usu�rio � importado/reimportado,
            // que inclui prefabs salvos.
            PrefabReferenceHelper helper = g.GetComponent<PrefabReferenceHelper>();
            if (helper != null)
            {
                string assetPath = AssetDatabase.GetAssetPath(g);
                if (helper.prefabAssetPath != assetPath)
                {
                    // N�o podemos usar Undo.RecordObject aqui diretamente no asset,
                    // mas podemos marcar o asset como sujo para ser salvo.
                    helper.prefabAssetPath = assetPath;
                    EditorUtility.SetDirty(helper); // Marca o componente como dirty
                    AssetDatabase.SaveAssets(); // For�a o salvamento do asset
                    Debug.Log($"[PrefabReferenceHelper] Caminho do prefab '{g.name}' atualizado para: {assetPath}");
                }
            }
        }
    }
}