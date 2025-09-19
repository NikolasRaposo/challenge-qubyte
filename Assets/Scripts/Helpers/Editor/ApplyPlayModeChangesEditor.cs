using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class ApplyPlayModeChangesEditor : EditorWindow
{
    public PlayModeChangesData changesData;

    [MenuItem("Custom Tools/Apply Play Mode Changes")]
    public static void ShowWindow()
    {
        GetWindow<ApplyPlayModeChangesEditor>("Apply Play Mode Changes");
    }

    void OnGUI()
    {
        GUILayout.Label("Ferramenta de Aplicação de Mudanças do Play Mode", EditorStyles.boldLabel);

        changesData = (PlayModeChangesData)EditorGUILayout.ObjectField("Dados de Mudanças", changesData, typeof(PlayModeChangesData), false);

        if (GUILayout.Button("Aplicar Mudanças na Cena"))
        {
            if (changesData == null)
            {
                Debug.LogError("Por favor, atribua o PlayModeChangesData ScriptableObject.");
                return;
            }

            ApplyChangesToScene();
        }

        if (GUILayout.Button("Limpar Dados de Mudanças"))
        {
            if (changesData != null)
            {
                changesData.ClearData();
                EditorUtility.SetDirty(changesData);
                AssetDatabase.SaveAssets();
                Debug.Log("Dados de mudanças limpos.");
            }
        }
    }

    void ApplyChangesToScene()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogError("Não é possível aplicar mudanças no modo Play. Saia do Play Mode primeiro.");
            return;
        }

        Scene currentScene = EditorSceneManager.GetActiveScene();
        string currentScenePath = currentScene.path;

        if (string.IsNullOrEmpty(currentScenePath))
        {
            Debug.LogError("A cena atual não foi salva ou não tem um caminho válido. Salve a cena antes de tentar aplicar as mudanças.");
            return;
        }

        bool hasChangesForCurrentScene = false;
        if (changesData.objectsToSave.Count > 0)
        {
            if (changesData.objectsToSave[0].scenePath == currentScenePath)
            {
                hasChangesForCurrentScene = true;
            }
        }

        if (!hasChangesForCurrentScene && changesData.objectsToSave.Count > 0)
        {
            Debug.LogWarning($"Não há mudanças salvas para a cena atual ('{currentScene.name}'). As mudanças salvas são para outra cena. Limpe os dados do Play Mode se não forem relevantes.");
            return;
        }
        else if (changesData.objectsToSave.Count == 0)
        {
            Debug.Log("Não há mudanças salvas para aplicar.");
            return;
        }

        Debug.Log($"Aplicando mudanças salvas do Play Mode para a cena: {currentScene.name}...");

        PlayModeChangeTracker sceneTracker = FindObjectOfType<PlayModeChangeTracker>();
        GameObject trackerParent = null;

        if (sceneTracker == null)
        {
            Debug.LogWarning("GameObject PlayModeTracker não encontrado na cena. Criando um novo para aplicar as mudanças.");
            trackerParent = new GameObject("PlayModeTracker");
            trackerParent.AddComponent<PlayModeChangeTracker>().changesData = this.changesData;
            Undo.RegisterCreatedObjectUndo(trackerParent, "Create PlayModeTracker for changes");
        }
        else
        {
            trackerParent = sceneTracker.gameObject;
        }

        if (trackerParent == null)
        {
            Debug.LogError("Falha ao obter/criar o GameObject PlayModeTracker. Não foi possível aplicar as mudanças.");
            return;
        }

        // Mapeia objetos existentes (filhos do PlayModeTracker) por GUID e por nome como fallback
        Dictionary<string, GameObject> existingObjectsByGuid = new Dictionary<string, GameObject>();
        Dictionary<string, GameObject> existingObjectsByName = new Dictionary<string, GameObject>();
        MapChildrenRecursive(trackerParent.transform, existingObjectsByGuid, existingObjectsByName);


        foreach (SavedObjectData data in changesData.objectsToSave)
        {
            GameObject processedObject = null; // Referência ao objeto que será criado ou atualizado

            if (data.isNewObject)
            {
                // Criar um novo objeto (instância de prefab)
                if (!string.IsNullOrEmpty(data.prefabPath))
                {
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(data.prefabPath);
                    if (prefab != null)
                    {
                        GameObject existingTempObj = null;
                        if (!string.IsNullOrEmpty(data.objectGuid) && existingObjectsByGuid.TryGetValue(data.objectGuid, out existingTempObj))
                        {
                            // Encontrado por GUID (se o novo objeto já tinha um GUID salvo, por exemplo, por ter vindo de um prefab que já tem UniqueIdComponent)
                            Debug.LogWarning($"Objeto {data.instanceIdAtPlayMode} (GUID: {data.objectGuid}) já existe na cena como filho do PlayModeTracker. Atualizando sua transformação em vez de criar um novo.");
                            processedObject = existingTempObj;
                        }
                        else if (existingObjectsByName.TryGetValue(data.instanceIdAtPlayMode, out existingTempObj))
                        {
                            // Fallback por nome
                            Debug.LogWarning($"Objeto {data.instanceIdAtPlayMode} já existe na cena como filho do PlayModeTracker (pelo nome). Atualizando sua transformação em vez de criar um novo.");
                            processedObject = existingTempObj;
                        }

                        if (processedObject != null)
                        {
                            Undo.RecordObject(processedObject.transform, "Update new object from Play Mode");
                            processedObject.transform.position = data.position;
                            processedObject.transform.rotation = data.rotation;
                            processedObject.transform.localScale = data.scale;
                            EditorUtility.SetDirty(processedObject);
                        }
                        else
                        {
                            GameObject newInstance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, trackerParent.transform);
                            if (newInstance != null)
                            {
                                newInstance.name = data.instanceIdAtPlayMode;
                                newInstance.transform.position = data.position;
                                newInstance.transform.rotation = data.rotation;
                                newInstance.transform.localScale = data.scale;
                                Undo.RegisterCreatedObjectUndo(newInstance, "Create new object from Play Mode");
                                EditorUtility.SetDirty(newInstance);
                                processedObject = newInstance; // Atribui para processamento de GUID abaixo
                                Debug.Log($"Instanciado novo objeto: {newInstance.name}");
                            }
                        }
                    }
                    else
                    {
                        Debug.LogError($"Prefab não encontrado no caminho: {data.prefabPath}. Não foi possível recriar o objeto '{data.instanceIdAtPlayMode}'.");
                    }
                }
                else
                {
                    Debug.LogWarning($"Objeto '{data.instanceIdAtPlayMode}' marcado como novo, mas sem caminho de prefab. Não será recriado.");
                }
            }
            else // Objeto EXISTENTE que foi movido/modificado
            {
                // Procura o objeto existente primariamente por GUID, depois por nome
                if (!string.IsNullOrEmpty(data.objectGuid))
                {
                    existingObjectsByGuid.TryGetValue(data.objectGuid, out processedObject);
                }

                if (processedObject == null && !string.IsNullOrEmpty(data.instanceIdAtPlayMode))
                {
                    existingObjectsByName.TryGetValue(data.instanceIdAtPlayMode, out processedObject);
                }

                if (processedObject != null)
                {
                    Undo.RecordObject(processedObject.transform, "Move existing object in Play Mode");
                    processedObject.transform.position = data.position;
                    processedObject.transform.rotation = data.rotation;
                    processedObject.transform.localScale = data.scale;
                    EditorUtility.SetDirty(processedObject);
                    Debug.Log($"Atualizado objeto existente: {processedObject.name} (GUID: {data.objectGuid})");
                }
                else
                {
                    Debug.LogWarning($"Objeto existente com ID/nome '{data.instanceIdAtPlayMode}' (GUID: '{data.objectGuid}') não encontrado na hierarquia do PlayModeTracker. Pode ter sido deletado ou renomeado no Editor. Ignorando.");
                }
            }

            // --- NOVO: Lógica para adicionar/garantir UniqueIdComponent após a aplicação da mudança ---
            if (processedObject != null)
            {
                UniqueIdComponent objUniqueId = processedObject.GetComponent<UniqueIdComponent>();
                if (objUniqueId == null)
                {
                    // Adiciona o componente usando Undo para que a ação possa ser desfeita (Ctrl+Z)
                    objUniqueId = Undo.AddComponent<UniqueIdComponent>(processedObject);
                    // O GUID será gerado automaticamente pelo Awake/OnValidate do UniqueIdComponent
                    EditorUtility.SetDirty(objUniqueId); // Marca o componente como dirty
                    Debug.Log($"Adicionado UniqueIdComponent automaticamente a: {processedObject.name}");
                }
                else if (string.IsNullOrEmpty(objUniqueId.Guid))
                {
                    // Caso raro: componente existe mas GUID não foi gerado (talvez erro anterior). Garante que ele gere.
                    // Isso é tratado internamente pelo UniqueIdComponent via OnValidate/Awake, mas um SetDirty ajuda.
                    EditorUtility.SetDirty(objUniqueId);
                }
            }
            // --- FIM DA NOVA LÓGICA ---
        }

        EditorSceneManager.MarkSceneDirty(currentScene);
        EditorSceneManager.SaveScene(currentScene);

        Debug.Log("Mudanças aplicadas e cena salva!");
        changesData.ClearData();
        EditorUtility.SetDirty(changesData);
        AssetDatabase.SaveAssets();
    }

    // Helper para mapear todos os filhos (recursivamente) por GUID e por Nome
    private static void MapChildrenRecursive(Transform parent, Dictionary<string, GameObject> byGuid, Dictionary<string, GameObject> byName)
    {
        foreach (Transform child in parent)
        {
            UniqueIdComponent uniqueId = child.GetComponent<UniqueIdComponent>();
            if (uniqueId != null && !string.IsNullOrEmpty(uniqueId.Guid))
            {
                if (!byGuid.ContainsKey(uniqueId.Guid))
                {
                    byGuid.Add(uniqueId.Guid, child.gameObject);
                }
                else
                {
                    Debug.LogWarning($"GUID duplicado encontrado na hierarquia do PlayModeTracker: '{uniqueId.Guid}' para objeto '{child.name}'. Apenas a primeira instância será considerada para atualização por GUID.");
                }
            }

            if (!byName.ContainsKey(child.name))
            {
                byName.Add(child.name, child.gameObject);
            }
            else
            {
                Debug.LogWarning($"Nome de objeto duplicado encontrado na hierarquia do PlayModeTracker: '{child.name}'. Apenas a primeira instância será considerada para atualização por nome.");
            }

            MapChildrenRecursive(child, byGuid, byName); // Chamada recursiva
        }
    }
}