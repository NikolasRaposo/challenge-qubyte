// Localização: Assets/Scripts/UniqueIdComponent.cs
using UnityEngine;

[ExecuteAlways] // Para que ele rode no Editor e gere o GUID ao criar/salvar
public class UniqueIdComponent : MonoBehaviour
{
    [SerializeField]
    private string _guid;

    public string Guid => _guid;

    void Awake()
    {
        // Garante que o GUID seja gerado no Editor e persista
        if (string.IsNullOrEmpty(_guid))
        {
            GenerateGuid();
        }
    }

    void OnValidate() // Chamado no Editor quando script é carregado ou valor é alterado
    {
        if (string.IsNullOrEmpty(_guid))
        {
            GenerateGuid();
        }
    }

    private void GenerateGuid()
    {
        // Generate a new GUID
        _guid = System.Guid.NewGuid().ToString();
        Debug.Log($"Generated GUID for {gameObject.name}: {_guid}");
#if UNITY_EDITOR
        // Marca o objeto como dirty para garantir que a cena seja salva
        UnityEditor.EditorUtility.SetDirty(this);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif
    }

    // Método para ser chamado no Editor para regenerar o GUID se necessário
#if UNITY_EDITOR
    [ContextMenu("Regenerate GUID")]
    private void RegenerateGuidFromContextMenu()
    {
        GenerateGuid();
    }
#endif
}