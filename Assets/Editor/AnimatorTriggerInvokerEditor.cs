using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(AnimatorTriggerInvoker))]
[CanEditMultipleObjects]
public class AnimatorTriggerInvokerEditor : Editor
{
    SerializedProperty _animatorProp;
    SerializedProperty _validateProp;
    SerializedProperty _ensureParentsActiveProp;
    SerializedProperty _enableAnimatorProp;

    private bool _showHelp = true;
    private bool _showTriggers = true;

    private void OnEnable()
    {
        _animatorProp = serializedObject.FindProperty("animator");
        _validateProp = serializedObject.FindProperty("validateParameter");
        _ensureParentsActiveProp = serializedObject.FindProperty("ensureParentsActive");
        _enableAnimatorProp = serializedObject.FindProperty("enableAnimatorIfDisabled");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("Animator Trigger Invoker", EditorStyles.boldLabel);

        // Ajuda
        _showHelp = EditorGUILayout.Foldout(_showHelp, "Ajuda e uso recomendado");
        if (_showHelp)
        {
            EditorGUILayout.HelpBox(
                "Este componente não guarda nome de trigger. Dispare diretamente via UnityEvent usando 'InvokeTriggerByName(string)'. " +
                "No evento, selecione o método e digite o nome exato do Trigger definido nos Parameters do Animator.",
                MessageType.Info);

            EditorGUILayout.HelpBox(
                "Boas práticas:\n- Mantenha 'Validar parâmetro' ativo para evitar nomes incorretos.\n" +
                "- Garanta que o Animator esteja ativo com as opções abaixo.\n" +
                "- Para reuso: adicione múltiplas entradas no UnityEvent chamando 'InvokeTriggerByName' com nomes diferentes.",
                MessageType.None);
        }

        EditorGUILayout.Space(6);
        EditorGUILayout.PropertyField(_animatorProp);
        EditorGUILayout.Space(4);

        EditorGUILayout.LabelField("Validações e Garantias", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_validateProp);
        EditorGUILayout.PropertyField(_ensureParentsActiveProp);
        EditorGUILayout.PropertyField(_enableAnimatorProp);

        EditorGUILayout.Space(6);

        // Lista de triggers do(s) Animator(es)
        _showTriggers = EditorGUILayout.Foldout(_showTriggers, "Triggers disponíveis no Animator");
        if (_showTriggers)
        {
            if (_animatorProp.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("Atribua um Animator para visualizar os triggers disponíveis.", MessageType.Warning);
            }
            else
            {
                foreach (var obj in targets)
                {
                    var inv = obj as AnimatorTriggerInvoker;
                    if (inv == null) continue;

                    var anim = inv.GetType()
                        .GetField("animator", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                        ?.GetValue(inv) as Animator;

                    if (anim == null)
                    {
                        EditorGUILayout.HelpBox("Animator não atribuído em " + inv.name + ".", MessageType.Warning);
                        continue;
                    }

                    EditorGUILayout.LabelField($"{inv.gameObject.name} → {anim.name}", EditorStyles.miniBoldLabel);

                    var parameters = anim.parameters;
                    int count = 0;
                    foreach (var p in parameters)
                    {
                        if (p.type == AnimatorControllerParameterType.Trigger)
                            count++;
                    }

                    if (count == 0)
                    {
                        EditorGUILayout.HelpBox("Nenhum parâmetro Trigger encontrado neste Animator.", MessageType.Info);
                    }
                    else
                    {
                        foreach (var p in parameters)
                        {
                            if (p.type != AnimatorControllerParameterType.Trigger) continue;
                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField(p.name, GUILayout.MaxWidth(250));
                            if (GUILayout.Button("Copiar", GUILayout.MaxWidth(60)))
                            {
                                EditorGUIUtility.systemCopyBuffer = p.name;
                            }
                            EditorGUILayout.EndHorizontal();
                        }
                    }

                    EditorGUILayout.Space(4);
                }
            }
        }

        serializedObject.ApplyModifiedProperties();
    }
}