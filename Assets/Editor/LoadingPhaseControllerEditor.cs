using UnityEditor;

[CustomEditor(typeof(LoadingPhaseController), true)]
[CanEditMultipleObjects]
public class LoadingPhaseControllerEditor : Editor
{
    private bool showExplanation = false;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        showExplanation = EditorGUILayout.Foldout(showExplanation, "Explicação", true);
        if (showExplanation)
        {
            EditorGUILayout.HelpBox(
                "O LoadingPhaseController controla a fase de carregamento e bloqueio de input.\n" +
                "- StartLoading: bloqueia input de gameplay, desativa interações de UI e dispara o trigger 'StartLoading' no Animator.\n" +
                "- StopLoading: dispara o trigger 'StopLoading' no Animator.\n" +
                "- NotifyFinished: emite o evento 'OnLoadingFinished' para concluir a fase.\n" +
                "- Dicas: garanta que 'loadingAnimator' esteja ativo na hierarquia; os triggers devem existir no Animator.",
                MessageType.Info
            );
            EditorGUILayout.Space(4);
        }

        DrawDefaultInspector();

        serializedObject.ApplyModifiedProperties();
    }
}