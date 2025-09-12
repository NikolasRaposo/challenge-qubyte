using UnityEditor;
using UnityEngine;

public class PrefabChecker : EditorWindow
{
    private GameObject selectedObject;

    [MenuItem("Custom Tools/Prefab Checker")]
    public static void ShowWindow()
    {
        GetWindow<PrefabChecker>("Prefab Checker");
    }

    void OnGUI()
    {
        GUILayout.Label("Verificador de Prefab", EditorStyles.boldLabel);

        selectedObject = (GameObject)EditorGUILayout.ObjectField("Objeto Selecionado", selectedObject, typeof(GameObject), true);

        if (selectedObject != null)
        {
            if (GUILayout.Button("Checar Status do Prefab"))
            {
                CheckPrefabStatus(selectedObject);
            }
        }
        else
        {
            EditorGUILayout.HelpBox("Selecione um GameObject na Hierarquia para verificar.", MessageType.Info);
        }
    }

    void CheckPrefabStatus(GameObject obj)
    {
        Debug.Log("--- Checando Prefab Status para: " + obj.name + " ---");

        // Verifica se é uma instância de prefab
        bool isPrefabInstance = PrefabUtility.IsAnyPrefabInstanceRoot(obj);
        Debug.Log("É instância de Prefab (root ou aninhado)? " + isPrefabInstance);

        // Tenta obter o prefab original ao qual esta instância corresponde
        GameObject correspondingObject = PrefabUtility.GetCorrespondingObjectFromSource(obj);

        if (correspondingObject != null)
        {
            Debug.Log("Objeto Correspondente (source prefab): " + correspondingObject.name);
            string prefabAssetPath = AssetDatabase.GetAssetPath(correspondingObject);
            Debug.Log("Caminho do Asset do Prefab: " + prefabAssetPath);
        }
        else
        {
            Debug.LogWarning("Não foi possível encontrar o Objeto Correspondente (source prefab). Isso pode indicar que o objeto não é uma instância de prefab vinculada, ou que foi desempacotado.");
        }

        // Verifica se é um objeto dentro de um prefab (aninhado)
        bool isPartOfAnyPrefab = PrefabUtility.IsPartOfAnyPrefab(obj);
        Debug.Log("Faz parte de algum prefab (incluindo aninhados)? " + isPartOfAnyPrefab);

        // Verifica se foi desempacotado
        bool isUnpacked = PrefabUtility.GetPrefabInstanceStatus(obj) == PrefabInstanceStatus.NotAPrefab;
        Debug.Log("Foi desempacotado (Unpacked)? " + isUnpacked);

        Debug.Log("--- Fim da Checagem ---");
    }
}