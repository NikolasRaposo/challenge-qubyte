#if UNITY_EDITOR
using UnityEngine;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public class PlayModeChangeTracker : MonoBehaviour
{
    public PlayModeChangesData changesData;

    // trackedObjectsData não será mais preenchido em LateUpdate.
    // Ele será preenchido e processado completamente no SaveCurrentPlayModeChanges.
    // Removed private Dictionary<GameObject, SavedObjectData> trackedObjectsData = new Dictionary<GameObject, SavedObjectData>();

    private HashSet<int> initialChildInstanceIDs = new HashSet<int>();

    void OnEnable()
    {
        initialChildInstanceIDs.Clear();
        // Populamos initialChildInstanceIDs para diferenciar o que existia antes do Play Mode.
        PopulateInitialChildrenIDs(this.transform);
    }

    private void PopulateInitialChildrenIDs(Transform parentTransform)
    {
        foreach (Transform child in parentTransform)
        {
            if (child.gameObject == this.gameObject) continue;
            initialChildInstanceIDs.Add(child.gameObject.GetInstanceID());
            PopulateInitialChildrenIDs(child); // Recursivo
        }
    }

    // LateUpdate REMOVIDO para otimização
    /*
    void LateUpdate()
    {
        // ... (código anterior do LateUpdate) ...
    }
    */

    // O método CheckObjectAndChildrenForChanges será chamado internamente pelo SaveCurrentPlayModeChanges
    // e não mais de LateUpdate. Ele agora recebe o dicionário para preencher.
    private void CheckAndRegisterChildrenRecursive(GameObject currentObject, Dictionary<GameObject, SavedObjectData> currentSessionTrackedData)
    {
        if (currentObject == null) return;

        bool isNewObjectThisSession = !initialChildInstanceIDs.Contains(currentObject.GetInstanceID());

        // --- Lógica de detecção e remoção/registro para DUPLICADOS / NOVOS ---
        UniqueIdComponent uniqueIdComp = currentObject.GetComponent<UniqueIdComponent>();

        if (isNewObjectThisSession && uniqueIdComp != null)
        {
            // Se o objeto é novo nesta sessão (foi arrastado ou duplicado)
            // E ele já tem um UniqueIdComponent (significa que o original tinha, ou o prefab base tinha)
            // REMOVE o UniqueIdComponent para que ele seja tratado como um "novo objeto" no salvamento.
            Debug.LogWarning($"[PlayModeTracker - {currentObject.name}] Objeto detectado como NOVO na sessão e possui UniqueIdComponent. Removendo para tratá-lo como nova instância.");
            Destroy(uniqueIdComp); // Destrói o componente da instância no Play Mode
            uniqueIdComp = null; // Zera a referência para que RegisterOrUpdateObjectData não o encontre
        }
        // --- FIM DA LÓGICA DE DUPLICAÇÃO ---

        RegisterOrUpdateObjectData(currentObject, isNewObjectThisSession, uniqueIdComp, currentSessionTrackedData);

        foreach (Transform child in currentObject.transform)
        {
            CheckAndRegisterChildrenRecursive(child.gameObject, currentSessionTrackedData); // Chamada recursiva
        }
    }

    private void RegisterOrUpdateObjectData(GameObject obj, bool isNewObjectStatus, UniqueIdComponent uniqueIdComp, Dictionary<GameObject, SavedObjectData> currentSessionTrackedData)
    {
        SavedObjectData data = new SavedObjectData();
        data.position = obj.transform.position;
        data.rotation = obj.transform.rotation;
        data.scale = obj.transform.localScale;
        data.scenePath = SceneManager.GetActiveScene().path;
        data.instanceIdAtPlayMode = obj.name;
        data.isNewObject = isNewObjectStatus;

        PrefabReferenceHelper prefabHelper = obj.GetComponent<PrefabReferenceHelper>();
        if (prefabHelper != null && !string.IsNullOrEmpty(prefabHelper.prefabAssetPath))
        {
            data.prefabPath = prefabHelper.prefabAssetPath;
        }
        else
        {
            data.prefabPath = "";
        }

        // Usa uniqueIdComp passado como parâmetro, que reflete o estado após a remoção, se aplicável.
        if (uniqueIdComp != null && !string.IsNullOrEmpty(uniqueIdComp.Guid))
        {
            data.objectGuid = uniqueIdComp.Guid;
            Debug.Log($"[PlayModeTracker - {obj.name}] Registrando/Atualizando. GUID: {data.objectGuid}");
        }
        else
        {
            data.objectGuid = "";
            if (!isNewObjectStatus) // Se não é um novo objeto mas não tem GUID (e deveria ter)
            {
                Debug.LogWarning($"[PlayModeTracker - {obj.name}] Objeto existente sem UniqueIdComponent ou GUID. Será buscado pelo nome ('{obj.name}'), o que pode falhar se o nome for duplicado ou alterado.");
            }
        }

        currentSessionTrackedData[obj] = data; // Adiciona ao dicionário temporário da sessão de salvamento
    }

    public void SaveCurrentPlayModeChanges()
    {
        if (changesData == null)
        {
            Debug.LogError("PlayModeChangesData ScriptableObject não foi atribuído!");
            return;
        }

        Scene currentScene = SceneManager.GetActiveScene();
        string currentScenePath = currentScene.path;

        if (string.IsNullOrEmpty(currentScenePath))
        {
            Debug.LogError("A cena atual não foi salva ou não tem um caminho válido. Salve a cena antes de tentar salvar as mudanças do Play Mode.");
            return;
        }

        // --- NOVA LÓGICA DE VARREDURA E PROCESSAMENTO NO MOMENTO DO SAVE ---
        // Cria um dicionário temporário para esta sessão de salvamento.
        // Isso garante que estamos processando o estado atual da hierarquia.
        Dictionary<GameObject, SavedObjectData> currentSessionTrackedData = new Dictionary<GameObject, SavedObjectData>();

        foreach (Transform child in this.transform)
        {
            CheckAndRegisterChildrenRecursive(child.gameObject, currentSessionTrackedData);
        }
        // --- FIM DA NOVA LÓGICA DE VARREDURA ---

        changesData.ClearData(); // Limpa dados anteriores do ScriptableObject

        foreach (var entry in currentSessionTrackedData) // Itera sobre o dicionário processado
        {
            if (entry.Key != null && entry.Key.transform.IsChildOf(this.transform))
            {
                SavedObjectData data = entry.Value;
                data.scenePath = currentScenePath;
                changesData.objectsToSave.Add(data);
                string debugId = !string.IsNullOrEmpty(data.objectGuid) ? data.objectGuid : data.instanceIdAtPlayMode;
                Debug.Log($"[PlayModeTracker - Salvando] Objeto: {debugId}, Prefab Path: '{data.prefabPath}', É Novo: {data.isNewObject}");
            }
        }

        EditorUtility.SetDirty(changesData);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Dados do Play Mode salvos no ScriptableObject para a cena: " + currentScene.name);
    }
}
#endif