using UnityEngine;
[System.Serializable]
public class SavedObjectData
{
    public string prefabPath;
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 scale;
    public string scenePath;

    // Usaremos o nome como fallback, mas o GUID é o principal identificador para objetos existentes
    public string instanceIdAtPlayMode;
    public string objectGuid; // NOVO: Um GUID persistente para objetos já existentes na cena

    public bool isNewObject;
}