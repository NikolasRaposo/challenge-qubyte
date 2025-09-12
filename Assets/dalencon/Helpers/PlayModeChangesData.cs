using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "PlayModeChangesData", menuName = "Custom Tools/Play Mode Changes Data")]
public class PlayModeChangesData : ScriptableObject
{
    public List<SavedObjectData> objectsToSave = new List<SavedObjectData>();

    // Método para limpar os dados se necessário
    public void ClearData()
    {
        objectsToSave.Clear();
    }
}