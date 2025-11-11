using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(AnimationEndHandler))]
[CanEditMultipleObjects]
public class AnimationEndHandlerEditor : Editor
{
    private bool showTrace = false;
    private GUIStyle _linkStyle;

    private class StarterEntry
    {
        public Object targetObject;
        public string objectName;
        public string componentType;
        public string eventFieldName;
        public string methodName;
    }

    private System.Collections.Generic.List<StarterEntry> _starters; // single selection cache
    private System.Collections.Generic.Dictionary<AnimationEndHandler, System.Collections.Generic.List<StarterEntry>> _multiStarters; // multi selection cache

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Default inspector first (compatível com multi-seleção)
        Editor.DrawPropertiesExcluding(serializedObject, new string[] { "m_Script" });

        EditorGUILayout.Space(6);
        showTrace = EditorGUILayout.BeginFoldoutHeaderGroup(showTrace, "Rastreamento: origem da animação");
        if (showTrace)
        {
            EnsureLinkStyle();

            var handlers = System.Array.ConvertAll(targets, t => (AnimationEndHandler)t);
            bool isMulti = handlers != null && handlers.Length > 1;

            if (isMulti)
            {
                // Multi-seleção
                if (_multiStarters == null)
                {
                    if (GUILayout.Button($"Varrer origem ({handlers.Length} selecionados)", GUILayout.Height(22)))
                    {
                        _multiStarters = new System.Collections.Generic.Dictionary<AnimationEndHandler, System.Collections.Generic.List<StarterEntry>>();
                        foreach (var h in handlers)
                        {
                            if (h == null) continue;
                            _multiStarters[h] = ScanForAnimationStarters(h);
                        }
                    }
                    EditorGUILayout.HelpBox("Identifica UnityEvents na cena que chamam métodos em componentes vinculados ao mesmo Animator dos objetos selecionados.", MessageType.Info);
                }
                else
                {
                    foreach (var kv in _multiStarters)
                    {
                        var handler = kv.Key;
                        var entries = kv.Value;
                        EditorGUILayout.LabelField($"[{handler.gameObject.name}]", EditorStyles.boldLabel);
                        if (entries == null || entries.Count == 0)
                        {
                            EditorGUILayout.HelpBox("Nenhum iniciador encontrado para este objeto.", MessageType.None);
                            continue;
                        }
                        foreach (var s in entries)
                        {
                            EditorGUILayout.BeginHorizontal();
                            var label = $"<u>{s.objectName}</u>";
                            if (GUILayout.Button(label, _linkStyle))
                            {
                                Selection.activeObject = s.targetObject;
                                EditorGUIUtility.PingObject(s.targetObject);
                            }
                            EditorGUILayout.LabelField($"— {s.componentType}.{s.eventFieldName} → {s.methodName}");
                            EditorGUILayout.EndHorizontal();
                        }
                        EditorGUILayout.Space(4);
                    }
                    if (GUILayout.Button("Revarrer", GUILayout.Width(100)))
                    {
                        foreach (var h in handlers)
                        {
                            if (h == null) continue;
                            _multiStarters[h] = ScanForAnimationStarters(h);
                        }
                    }
                }
            }
            else
            {
                // Seleção simples
                if (_starters == null)
                {
                    if (GUILayout.Button("Varrer origem", GUILayout.Height(22)))
                    {
                        _starters = ScanForAnimationStarters((AnimationEndHandler)target);
                    }
                    EditorGUILayout.HelpBox("Identifica UnityEvents na cena que chamam métodos em componentes vinculados ao mesmo Animator.", MessageType.Info);
                }
                else
                {
                    if (_starters.Count == 0)
                    {
                        EditorGUILayout.HelpBox("Nenhum iniciador encontrado. Configure eventos (ex.: fim da animação da empresa) que disparem métodos no componente que referencia este Animator.", MessageType.Warning);
                    }
                    else
                    {
                        foreach (var s in _starters)
                        {
                            EditorGUILayout.BeginHorizontal();
                            var label = $"<u>{s.objectName}</u>";
                            if (GUILayout.Button(label, _linkStyle))
                            {
                                Selection.activeObject = s.targetObject;
                                EditorGUIUtility.PingObject(s.targetObject);
                            }
                            EditorGUILayout.LabelField($"— {s.componentType}.{s.eventFieldName} → {s.methodName}");
                            EditorGUILayout.EndHorizontal();
                        }
                    }
                    EditorGUILayout.Space(4);
                    if (GUILayout.Button("Revarrer", GUILayout.Width(100)))
                    {
                        _starters = ScanForAnimationStarters((AnimationEndHandler)target);
                    }
                }
            }
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        serializedObject.ApplyModifiedProperties();
    }

    private void EnsureLinkStyle()
    {
        if (_linkStyle != null) return;
        _linkStyle = new GUIStyle(EditorStyles.label)
        {
            richText = true,
            normal = { textColor = new Color(0.2f, 0.5f, 1f) },
            hover = { textColor = new Color(0.1f, 0.4f, 0.9f) },
            active = { textColor = new Color(0.1f, 0.4f, 0.9f) },
            margin = new RectOffset(4, 4, 2, 2),
            padding = new RectOffset(0, 0, 0, 0)
        };
    }

    private System.Collections.Generic.List<StarterEntry> ScanForAnimationStarters(AnimationEndHandler handler)
    {
        var list = new System.Collections.Generic.List<StarterEntry>();

        // Heurística: Animator no mesmo GameObject
        Animator listenedAnimator = null;
        try { listenedAnimator = handler.GetComponent<Animator>(); } catch { listenedAnimator = null; }
        if (listenedAnimator == null)
        {
            // Sem Animator diretamente — nada a fazer por enquanto
            return list;
        }

        // Passo 1: encontre componentes que referenciam exatamente este Animator em campos públicos/privados
        var behaviours = GameObject.FindObjectsOfType<MonoBehaviour>(true);
        var candidates = new System.Collections.Generic.List<MonoBehaviour>();
        foreach (var mb in behaviours)
        {
            if (mb == null) continue;
            var type = mb.GetType();
            var fields = type.GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            bool referencesAnimator = false;
            foreach (var f in fields)
            {
                if (f.FieldType == typeof(Animator))
                {
                    object val = null;
                    try { val = f.GetValue(mb); } catch { val = null; }
                    if (val == listenedAnimator)
                    {
                        referencesAnimator = true;
                        break;
                    }
                }
            }
            if (referencesAnimator)
            {
                candidates.Add(mb);
            }
        }

        if (candidates.Count == 0) return list;

        // Passo 2: procurar UnityEvents na cena que tenham como target algum candidato
        foreach (var mb in behaviours)
        {
            if (mb == null) continue;
            var type = mb.GetType();
            var fields = type.GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            foreach (var f in fields)
            {
                UnityEngine.Events.UnityEventBase ueb = null;
                try { ueb = f.GetValue(mb) as UnityEngine.Events.UnityEventBase; } catch { ueb = null; }
                if (ueb == null) continue;
                int count = 0;
                try { count = ueb.GetPersistentEventCount(); } catch { count = 0; }
                for (int i = 0; i < count; i++)
                {
                    Object targetObj = null;
                    string method = null;
                    try
                    {
                        targetObj = ueb.GetPersistentTarget(i);
                        method = ueb.GetPersistentMethodName(i);
                    }
                    catch { targetObj = null; method = null; }
                    var targetMb = targetObj as MonoBehaviour;
                    if (targetMb != null && candidates.Contains(targetMb))
                    {
                        list.Add(new StarterEntry
                        {
                            targetObject = mb.gameObject,
                            objectName = mb.gameObject.name,
                            componentType = type.Name,
                            eventFieldName = f.Name,
                            methodName = method ?? "(método desconhecido)"
                        });
                    }
                }
            }
        }

        return list;
    }
}