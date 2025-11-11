using UnityEditor;

[CustomEditor(typeof(CinematicPhaseController), true)]
[CanEditMultipleObjects]
public class CinematicPhaseControllerEditor : Editor
{
    private bool showExplanation = false;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        showExplanation = EditorGUILayout.Foldout(showExplanation, "Explicação", true);
        if (showExplanation)
        {
            EditorGUILayout.HelpBox(
                "O CinematicPhaseController dispara a cinemática usando um PlayableDirector.\n" +
                "- Play: habilita o PlayableDirector, zera o tempo e inicia a reprodução.\n" +
                "- NotifyEnd: emite o evento 'OnCinematicFinished' ao término.\n" +
                "- Observação: se 'director' não estiver atribuído, o componente é buscado no mesmo GameObject.",
                MessageType.Info
            );
            EditorGUILayout.Space(4);
        }

        DrawDefaultInspector();

        serializedObject.ApplyModifiedProperties();
    }
}