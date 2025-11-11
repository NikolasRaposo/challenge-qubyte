using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Managers.GameManager))]
public class GameManagerEditor : Editor
{
    private bool showActivators = false;
    private bool showDeepScan = false;

    private class ActivatorEntry
    {
        public Object targetObject; // Componente ou GameObject que contém o UnityEvent
        public string objectName;
        public string componentType;
        public string eventFieldName;
        public string methodName;
    }

    private System.Collections.Generic.List<ActivatorEntry> _activators; // cache
    private GUIStyle _linkStyle;
    private System.Collections.Generic.List<DeepCallScanner.CallSite> _deepResults;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Desenha o inspector padrão (compatível com multi-seleção)
        Editor.DrawPropertiesExcluding(serializedObject, new string[] { "m_Script" });

        EditorGUILayout.Space(6);
        showActivators = EditorGUILayout.BeginFoldoutHeaderGroup(showActivators, "Ativadores do GameManager (UnityEvents)");
        if (showActivators)
        {
            EnsureLinkStyle();

            var mgr = (Managers.GameManager)target;
            if (_activators == null)
            {
                if (GUILayout.Button("Varrer UnityEvents na cena", GUILayout.Height(22)))
                {
                    _activators = ScanForActivators(mgr);
                }
                EditorGUILayout.HelpBox("Procura UnityEvents na cena que chamam métodos públicos do GameManager (ex.: TogglePause, AddCoin, RespawnPlayer, CompleteLevel, QuitGame).", MessageType.Info);
            }
            else
            {
                if (_activators.Count == 0)
                {
                    EditorGUILayout.HelpBox("Nenhum ativador encontrado. Configure componentes com UnityEvents apontando para métodos do GameManager quando necessário.", MessageType.Warning);
                }
                else
                {
                    foreach (var a in _activators)
                    {
                        EditorGUILayout.BeginHorizontal();
                        var label = $"<u>{a.objectName}</u>";
                        if (GUILayout.Button(label, _linkStyle))
                        {
                            Selection.activeObject = a.targetObject;
                            EditorGUIUtility.PingObject(a.targetObject);
                        }
                        EditorGUILayout.LabelField($"— {a.componentType}.{a.eventFieldName} → {a.methodName}");
                        EditorGUILayout.EndHorizontal();
                    }

                    // Denúncia de duplicidades por método
                    var counts = new System.Collections.Generic.Dictionary<string, int>();
                    foreach (var x in _activators)
                    {
                        if (!counts.ContainsKey(x.methodName)) counts[x.methodName] = 0;
                        counts[x.methodName]++;
                    }
                    int dupTotal = 0;
                    foreach (var kv in counts) if (kv.Value > 1) dupTotal++;
                    if (dupTotal > 0)
                    {
                        var sb = new System.Text.StringBuilder();
                        sb.AppendLine("Foram detectados ativadores duplicados para:");
                        foreach (var kv in counts)
                        {
                            if (kv.Value > 1)
                                sb.AppendLine($"- {kv.Key}: {kv.Value} referências");
                        }
                        EditorGUILayout.HelpBox(sb.ToString(), MessageType.Warning);
                    }
                }
                EditorGUILayout.Space(4);
                if (GUILayout.Button("Revarrer", GUILayout.Width(100)))
                {
                    _activators = ScanForActivators(mgr);
                }
            }
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        EditorGUILayout.Space(6);
        showDeepScan = EditorGUILayout.BeginFoldoutHeaderGroup(showDeepScan, "Varredura Profunda (código)");
        if (showDeepScan)
        {
            EditorGUILayout.HelpBox("Procura chamadas em código para métodos do GameManager via Singleton (GameManager.Instance.*).", MessageType.Info);
            if (GUILayout.Button("Varrer chamadas em código", GUILayout.Height(22)))
            {
                _deepResults = DeepCallScanner.ScanSingletonCalls(
                    typeName: nameof(Managers.GameManager),
                    methodNames: new[]
                    {
                        nameof(Managers.GameManager.TogglePause),
                        nameof(Managers.GameManager.SetStartMenuActive),
                        nameof(Managers.GameManager.NotifyPlayerDied),
                        nameof(Managers.GameManager.AddCoin),
                        nameof(Managers.GameManager.AddLife),
                        nameof(Managers.GameManager.IncrementDefeatedEnemies),
                        nameof(Managers.GameManager.RespawnPlayer),
                        nameof(Managers.GameManager.UpdateCheckpoint),
                        nameof(Managers.GameManager.CompleteLevel),
                        nameof(Managers.GameManager.RestartLevel),
                        nameof(Managers.GameManager.GoToMainMenu),
                        nameof(Managers.GameManager.QuitGame),
                        nameof(Managers.GameManager.ActivateHUDFromCinematicEnd)
                    }
                );
            }
            if (_deepResults != null)
            {
                if (_deepResults.Count == 0)
                {
                    EditorGUILayout.HelpBox("Nenhum uso encontrado.", MessageType.Info);
                }
                else
                {
                    foreach (var r in _deepResults)
                    {
                        EditorGUILayout.BeginHorizontal();
                        if (GUILayout.Button($"{r.assetPath}:{r.lineNumber}", GUILayout.Width(280)))
                        {
                            DeepCallScanner.OpenAt(r.assetPath, r.lineNumber);
                        }
                        EditorGUILayout.LabelField($"{r.methodName} — {r.lineText}");
                        EditorGUILayout.EndHorizontal();
                    }
                }
                EditorGUILayout.Space(4);
                if (GUILayout.Button("Limpar resultados", GUILayout.Width(140)))
                {
                    _deepResults = null;
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

    private System.Collections.Generic.List<ActivatorEntry> ScanForActivators(Managers.GameManager mgr)
    {
        var list = new System.Collections.Generic.List<ActivatorEntry>();
        var behaviours = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var mb in behaviours)
        {
            if (mb == null) continue;
            var type = mb.GetType();
            var fields = type.GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            foreach (var f in fields)
            {
                object value = null;
                try { value = f.GetValue(mb); } catch { value = null; }
                if (value == null) continue;

                // UnityEvent direto
                try
                {
                    var ueb = value as UnityEngine.Events.UnityEventBase;
                    if (ueb != null)
                    {
                        TryCollectActivator(ueb, mb, type, f.Name, mgr, list);
                        continue;
                    }
                }
                catch { /* ignore */ }

                // Coleções que possam conter UnityEvents (campo comum 'onEvent')
                try
                {
                    if (value is System.Collections.IList listObj)
                    {
                        for (int i = 0; i < listObj.Count; i++)
                        {
                            var item = listObj[i];
                            if (item == null) continue;
                            var itemType = item.GetType();
                            var onEventField = itemType.GetField("onEvent", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                            if (onEventField != null)
                            {
                                UnityEngine.Events.UnityEventBase innerVal = null;
                                try { innerVal = onEventField.GetValue(item) as UnityEngine.Events.UnityEventBase; } catch { innerVal = null; }
                                if (innerVal != null)
                                {
                                    TryCollectActivator(innerVal, mb, type, f.Name + ".onEvent", mgr, list);
                                }
                            }
                        }
                    }
                }
                catch { /* ignore */ }
            }
        }
        return list;
    }

    private static void TryCollectActivator(UnityEngine.Events.UnityEventBase ev, MonoBehaviour mb, System.Type compType, string fieldName, Managers.GameManager mgr, System.Collections.Generic.List<ActivatorEntry> acc)
    {
        if (ev == null) return;
        int count = 0;
        try { count = ev.GetPersistentEventCount(); } catch { count = 0; }
        for (int i = 0; i < count; i++)
        {
            Object targetObj = null;
            string method = null;
            try
            {
                targetObj = ev.GetPersistentTarget(i);
                method = ev.GetPersistentMethodName(i);
            }
            catch { targetObj = null; method = null; }

            if (targetObj == mgr && !string.IsNullOrEmpty(method))
            {
                acc.Add(new ActivatorEntry
                {
                    targetObject = mb.gameObject,
                    objectName = mb.gameObject.name,
                    componentType = compType.Name,
                    eventFieldName = fieldName,
                    methodName = method
                });
            }
        }
    }
}