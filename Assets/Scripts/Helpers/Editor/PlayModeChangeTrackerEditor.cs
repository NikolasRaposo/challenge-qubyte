using UnityEditor;
using UnityEngine;

// Indica que este é um editor customizado para a classe PlayModeChangeTracker
[CustomEditor(typeof(PlayModeChangeTracker))]
public class PlayModeChangeTrackerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Desenha o Inspector padrão para o PlayModeChangeTracker
        DrawDefaultInspector();

        // Obtém uma referência ao script PlayModeChangeTracker que estamos inspecionando
        PlayModeChangeTracker myScript = (PlayModeChangeTracker)target;

        // Adiciona um espaço
        EditorGUILayout.Space(20);

        // Adiciona um botão
        if (GUILayout.Button("Salvar Mudanças do Play Mode Agora"))
        {
            // Verifica se estamos no modo Play antes de chamar o método
            if (Application.isPlaying)
            {
                myScript.SaveCurrentPlayModeChanges();
            }
            else
            {
                Debug.LogWarning("O botão 'Salvar Mudanças do Play Mode Agora' só funciona no modo Play.");
            }
        }
    }
}