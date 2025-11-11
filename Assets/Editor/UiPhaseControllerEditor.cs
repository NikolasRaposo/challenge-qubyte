using UnityEditor;

[CustomEditor(typeof(UiPhaseController), true)]
[CanEditMultipleObjects]
public class UiPhaseControllerEditor : Editor
{
    private bool showExplanation = false;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        showExplanation = EditorGUILayout.Foldout(showExplanation, "Explicação", true);
        if (showExplanation)
        {
            EditorGUILayout.HelpBox(
                "O UiPhaseController gerencia a entrada e saída do contexto de UI.\n" +
                "- EnterUi: entra no contexto de UI, habilita interações e define foco em 'defaultButton'.\n" +
                "- ExitUi: desabilita interações de UI e limpa a seleção atual do EventSystem.\n" +
                "- Dica: atribua 'canvasUI' e 'defaultButton' conforme necessário; se 'defaultButton' estiver vazio, nenhuma seleção é definida.",
                MessageType.Info
            );
            EditorGUILayout.Space(4);
        }

        DrawDefaultInspector();

        serializedObject.ApplyModifiedProperties();
    }
}