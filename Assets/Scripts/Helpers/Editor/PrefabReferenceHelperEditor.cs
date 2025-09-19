// Localização: Por exemplo, Assets/Editor/PrefabReferenceHelperEditor.cs
using UnityEditor;
using UnityEngine;

// CustomEditor para o componente PrefabReferenceHelper
[CustomEditor(typeof(PrefabReferenceHelper))]
public class PrefabReferenceHelperEditor : Editor
{
    // Este método é chamado quando o Inspector do PrefabReferenceHelper é desenhado
    public override void OnInspectorGUI()
    {
        // Desenha o Inspector padrão para as variáveis públicas
        DrawDefaultInspector();

        PrefabReferenceHelper myScript = (PrefabReferenceHelper)target;

        // Se o objeto inspecionado é um asset de prefab (não uma instância na cena)
        if (PrefabUtility.IsPartOfPrefabAsset(myScript.gameObject))
        {
            // Obtém o caminho do asset do prefab
            string currentAssetPath = AssetDatabase.GetAssetPath(myScript.gameObject);

            // Verifica se o caminho no campo precisa ser atualizado
            if (myScript.prefabAssetPath != currentAssetPath)
            {
                // Registra a mudança para permitir Undo
                Undo.RecordObject(myScript, "Update Prefab Asset Path");
                myScript.prefabAssetPath = currentAssetPath;
                // Marca o asset como modificado para que seja salvo
                EditorUtility.SetDirty(myScript);
            }
            EditorGUILayout.HelpBox("Caminho do prefab preenchido automaticamente.", MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox("Este componente funciona melhor em assets de Prefab. O caminho será preenchido automaticamente ao salvar o prefab.", MessageType.Warning);
        }
    }

    // Opcional: Para garantir que o caminho seja preenchido ao salvar o asset de prefab, mesmo sem abrir o Inspector.
    // Isso é mais robusto.
    [InitializeOnLoadMethod]
    private static void OnProjectLoadedInEditor()
    {
        EditorApplication.hierarchyChanged += CheckAllPrefabReferenceHelpers;
    }

    private static void CheckAllPrefabReferenceHelpers()
    {
        // Esta função pode ser pesada se executada com muita frequência, então seja cauteloso.
        // É melhor usar um AssetPostprocessor para quando assets são importados/salvos.
        // A função DrawDefaultInspector + EditorUtility.SetDirty já cobre a maioria dos casos.
    }

    // Usaremos um AssetPostprocessor para maior eficiência e para capturar a criação/modificação do asset de prefab
    class PrefabReferencePostprocessor : AssetPostprocessor
    {
        void OnPostprocessGameObjectWithUserProperties(GameObject g, string[] propNames, System.Object[] values)
        {
            // Este método é chamado quando um GameObject com propriedades de usuário é importado/reimportado,
            // que inclui prefabs salvos.
            PrefabReferenceHelper helper = g.GetComponent<PrefabReferenceHelper>();
            if (helper != null)
            {
                string assetPath = AssetDatabase.GetAssetPath(g);
                if (helper.prefabAssetPath != assetPath)
                {
                    // Não podemos usar Undo.RecordObject aqui diretamente no asset,
                    // mas podemos marcar o asset como sujo para ser salvo.
                    helper.prefabAssetPath = assetPath;
                    EditorUtility.SetDirty(helper); // Marca o componente como dirty
                    AssetDatabase.SaveAssets(); // Força o salvamento do asset
                    Debug.Log($"[PrefabReferenceHelper] Caminho do prefab '{g.name}' atualizado para: {assetPath}");
                }
            }
        }
    }
}