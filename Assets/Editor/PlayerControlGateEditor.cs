using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PlayerControlGate), true)]
[CanEditMultipleObjects]
public class PlayerControlGateEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Inspector padrão do Unity
        DrawDefaultInspector();

        // Texto funcional: o que o Gate FAZ
        EditorGUILayout.Space(4);
        EditorGUILayout.HelpBox(
            "PlayerControlGate: utilitário para controle do jogador em fluxos.\n" +
            "Ações principais:\n" +
            "- FreezePhysics: zera velocidades e torna o Rigidbody cinemático.\n" +
            "- UnfreezePhysics: restaura o Rigidbody para dinâmico.\n" +
            "- DisableController: desativa o controlador ECM do personagem.\n" +
            "- EnableController: reativa o controlador ECM do personagem.\n\n" +
            "Sem configuração no Inspector; utilize os métodos acima via código.",
            MessageType.Info);
    }
}