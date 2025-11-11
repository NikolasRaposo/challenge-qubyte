using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SceneEntryFlowCoordinator))]
public class SceneEntryFlowCoordinatorEditor : Editor
{
    SerializedProperty changeInputContextOnStart;
    SerializedProperty startInputContext;
    SerializedProperty enableUiInteractionsOnStart;
    SerializedProperty blockGameplayInputOnStart;
    SerializedProperty autoPlayCinematicAfterLoading;

    SerializedProperty cinematicRoot;
    SerializedProperty cinematicDirector;

    SerializedProperty loadingAnimator;
    SerializedProperty loadingStartTrigger;
    SerializedProperty loadingStopTrigger;

    SerializedProperty canvasUI;
    SerializedProperty canvasHUD;
    SerializedProperty defaultUiButton;
    SerializedProperty hudEnabledOnStart;
    SerializedProperty onStart;

    SerializedProperty freezePlayerPhysicsOnStart;
    SerializedProperty disablePlayerControllerOnStart;
    SerializedProperty restorePlayerControlOnCinematicEnd;

    SerializedProperty markGameAsInStartMenu;

    SerializedProperty uiController;
    SerializedProperty loadingController;
    SerializedProperty cinematicController;
    SerializedProperty playerGate;

    bool showUiSection = true;
    bool showLoadingSection = true;
    bool showCinematicSection = true;
    bool showPlayerSection = true;
    bool showOptionalControllers = false;
    bool showExplanation = false;
    bool showActivators = false;

    // Cache de resultados de varredura de UnityEvents
    private class ActivatorEntry
    {
        public Object targetObject; // Componente ou GameObject que contém o UnityEvent
        public string objectName;
        public string componentType;
        public string eventFieldName;
        public string methodName;
    }
    private System.Collections.Generic.List<ActivatorEntry> _activators;
    private GUIStyle _linkStyle;

    private void OnEnable()
    {
        changeInputContextOnStart = serializedObject.FindProperty("changeInputContextOnStart");
        startInputContext = serializedObject.FindProperty("startInputContext");
        enableUiInteractionsOnStart = serializedObject.FindProperty("enableUiInteractionsOnStart");
        blockGameplayInputOnStart = serializedObject.FindProperty("blockGameplayInputOnStart");
        autoPlayCinematicAfterLoading = serializedObject.FindProperty("autoPlayCinematicAfterLoading");

        cinematicRoot = serializedObject.FindProperty("cinematicRoot");
        cinematicDirector = serializedObject.FindProperty("cinematicDirector");

        loadingAnimator = serializedObject.FindProperty("loadingAnimator");
        loadingStartTrigger = serializedObject.FindProperty("loadingStartTrigger");
        loadingStopTrigger = serializedObject.FindProperty("loadingStopTrigger");

        canvasUI = serializedObject.FindProperty("canvasUI");
        canvasHUD = serializedObject.FindProperty("canvasHUD");
        defaultUiButton = serializedObject.FindProperty("defaultUiButton");
        hudEnabledOnStart = serializedObject.FindProperty("hudEnabledOnStart");
        onStart = serializedObject.FindProperty("onStart");

        freezePlayerPhysicsOnStart = serializedObject.FindProperty("freezePlayerPhysicsOnStart");
        disablePlayerControllerOnStart = serializedObject.FindProperty("disablePlayerControllerOnStart");
        restorePlayerControlOnCinematicEnd = serializedObject.FindProperty("restorePlayerControlOnCinematicEnd");

        markGameAsInStartMenu = serializedObject.FindProperty("markGameAsInStartMenu");

        uiController = serializedObject.FindProperty("uiController");
        loadingController = serializedObject.FindProperty("loadingController");
        cinematicController = serializedObject.FindProperty("cinematicController");
        playerGate = serializedObject.FindProperty("playerGate");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(markGameAsInStartMenu);

        // Explicação geral do funcionamento
        showExplanation = EditorGUILayout.BeginFoldoutHeaderGroup(showExplanation, "Explicação");
        if (showExplanation)
        {
            EditorGUILayout.HelpBox(
                "SceneEntryFlowCoordinator: orquestra o fluxo de entrada da cena.\n" +
                "- UI: entra no contexto de UI e pode habilitar interações; define Canvas UI/HUD e botão padrão.\n" +
                "- Loading: bloqueia input de gameplay, aciona animações de loading (Start/Stop).\n" +
                "- Cinemática: opcional; pode iniciar automaticamente após o loading e usa raiz/diretora.\n" +
                "- Player Control: congela física e desativa/ativa controlador via PlayerControlGate.\n" +
                "- Controladores Opcionais: referências para UI/Loading/Cinemática/PlayerGate; quando ausentes, o coordenador usa fallback interno.\n\n" +
                "Dica: todos os campos são opcionais conforme seu fluxo. Use os foldouts para focar apenas no que está usando.",
                MessageType.Info);
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        // Ativadores do fluxo: varre UnityEvents e mostra links
        showActivators = EditorGUILayout.BeginFoldoutHeaderGroup(showActivators, "Ativadores do fluxo");
        if (showActivators)
        {
            EnsureLinkStyle();
            if (_activators == null)
            {
                if (GUILayout.Button("Varrer UnityEvents na cena", GUILayout.Height(22)))
                {
                    _activators = ScanForFlowActivators((SceneEntryFlowCoordinator)target);
                }
                EditorGUILayout.HelpBox("Clique em 'Varrer UnityEvents' para encontrar componentes que disparam métodos de entrada do fluxo (ex.: EnterUiContextWithFocus).", MessageType.Info);
            }
            else
            {
                if (_activators.Count == 0)
                {
                    EditorGUILayout.HelpBox("Nenhum ativador encontrado. Configure AnimationEndHandler/Ui/Loading/Cinemática com eventos apontando para o FlowCoordinator.", MessageType.Warning);
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
                }
                EditorGUILayout.Space(4);
                if (GUILayout.Button("Revarrer", GUILayout.Width(100)))
                {
                    _activators = ScanForFlowActivators((SceneEntryFlowCoordinator)target);
                }
            }
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        // UI
        showUiSection = EditorGUILayout.BeginFoldoutHeaderGroup(showUiSection, "UI");
        if (showUiSection)
        {
            bool hasUiOverride = uiController != null && uiController.objectReferenceValue != null;
            EditorGUILayout.PropertyField(changeInputContextOnStart, new GUIContent("Alterar Input Context On Start"));
            if (changeInputContextOnStart.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(startInputContext, new GUIContent("Contexto Inicial"));

                // 0=None, 1=Gameplay, 2=UI (ordem definida no enum)
                if (startInputContext.enumValueIndex == 2)
                {
                    EditorGUILayout.PropertyField(enableUiInteractionsOnStart, new GUIContent("Enable UI Interactions On Start"));
                    EditorGUI.BeginDisabledGroup(hasUiOverride);
                    EditorGUILayout.PropertyField(canvasUI);
                    EditorGUILayout.PropertyField(defaultUiButton);
                    EditorGUI.EndDisabledGroup();
                }
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.PropertyField(canvasHUD);
            EditorGUILayout.PropertyField(hudEnabledOnStart, new GUIContent("HUD Enabled On Start"));
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Events", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(onStart, new GUIContent("On Start"));
        EditorGUILayout.EndFoldoutHeaderGroup();

        // Loading
        showLoadingSection = EditorGUILayout.BeginFoldoutHeaderGroup(showLoadingSection, "Loading");
        if (showLoadingSection)
        {
            bool hasLoadingOverride = loadingController != null && loadingController.objectReferenceValue != null;
            EditorGUILayout.PropertyField(blockGameplayInputOnStart);

            EditorGUI.BeginDisabledGroup(hasLoadingOverride);
            EditorGUILayout.PropertyField(loadingAnimator);
            if (loadingAnimator.objectReferenceValue != null)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(loadingStartTrigger);
                EditorGUILayout.PropertyField(loadingStopTrigger);
                EditorGUI.indentLevel--;
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.PropertyField(loadingController);
            EditorGUILayout.HelpBox("Se não houver Loading, deixe estes campos em branco.", MessageType.Info);
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        // Cinemática
        showCinematicSection = EditorGUILayout.BeginFoldoutHeaderGroup(showCinematicSection, "Cinemática");
        if (showCinematicSection)
        {
            bool hasCinematicOverride = cinematicController != null && cinematicController.objectReferenceValue != null;
            EditorGUILayout.PropertyField(autoPlayCinematicAfterLoading);
            if (autoPlayCinematicAfterLoading.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUI.BeginDisabledGroup(hasCinematicOverride);
                EditorGUILayout.PropertyField(cinematicRoot);
                EditorGUILayout.PropertyField(cinematicDirector);
                EditorGUI.EndDisabledGroup();
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.PropertyField(cinematicController);
            EditorGUILayout.HelpBox("Sem cinemática? Desmarque auto-play ou deixe os campos vazios.", MessageType.None);
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        // Player Control
        showPlayerSection = EditorGUILayout.BeginFoldoutHeaderGroup(showPlayerSection, "Player Control");
        if (showPlayerSection)
        {
            EditorGUILayout.PropertyField(freezePlayerPhysicsOnStart);
            EditorGUILayout.PropertyField(disablePlayerControllerOnStart);
            EditorGUILayout.PropertyField(restorePlayerControlOnCinematicEnd);
            if (freezePlayerPhysicsOnStart.boolValue || disablePlayerControllerOnStart.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(playerGate);
                EditorGUI.indentLevel--;
            }
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        // Controladores Opcionais (visão geral)
        showOptionalControllers = EditorGUILayout.BeginFoldoutHeaderGroup(showOptionalControllers, "Avançado (override)");
        if (showOptionalControllers)
        {
            EditorGUILayout.PropertyField(uiController);
            EditorGUILayout.PropertyField(loadingController);
            EditorGUILayout.PropertyField(cinematicController);
            EditorGUILayout.PropertyField(playerGate);
            EditorGUILayout.HelpBox("Configure overrides aqui. Quando um controlador está definido, os campos brutos acima ficam desabilitados. O coordenador faz fallback quando ausentes.", MessageType.None);
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

    private System.Collections.Generic.List<ActivatorEntry> ScanForFlowActivators(SceneEntryFlowCoordinator coordinator)
    {
        var list = new System.Collections.Generic.List<ActivatorEntry>();
        var behaviours = GameObject.FindObjectsOfType<MonoBehaviour>(true);
        foreach (var mb in behaviours)
        {
            if (mb == null) continue;
            var type = mb.GetType();
            var fields = type.GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            foreach (var f in fields)
            {
                object value = null;
                try
                {
                    value = f.GetValue(mb);
                }
                catch
                {
                    // Alguns campos podem lançar em Editor; ignore com segurança
                    continue;
                }

                // 1) UnityEvent direto no componente
                try
                {
                    var ueb = value as UnityEngine.Events.UnityEventBase;
                    if (ueb != null)
                    {
                        TryCollectActivator(ueb, mb, type, f.Name, coordinator, list);
                        continue;
                    }
                }
                catch { /* ignore */ }

                // 2) Caso específico: AnimationEndHandler.events (lista de NamedAnimEvent com campo 'onEvent')
                try
                {
                    if (type.Name == "AnimationEndHandler" && f.Name == "events" && value is System.Collections.IList listObj)
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
                                try { innerVal = onEventField.GetValue(item) as UnityEngine.Events.UnityEventBase; }
                                catch { innerVal = null; }
                                if (innerVal != null)
                                {
                                    TryCollectActivator(innerVal, mb, type, f.Name + ".onEvent", coordinator, list);
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

    private static void TryCollectActivator(UnityEngine.Events.UnityEventBase ev, MonoBehaviour mb, System.Type compType, string fieldName, SceneEntryFlowCoordinator coordinator, System.Collections.Generic.List<ActivatorEntry> acc)
    {
        if (ev == null) return;
        int count = ev.GetPersistentEventCount();
        for (int i = 0; i < count; i++)
        {
            var target = ev.GetPersistentTarget(i);
            var method = ev.GetPersistentMethodName(i);
            if (target == coordinator && IsFlowEntryMethod(method))
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

    private static bool IsFlowEntryMethod(string methodName)
    {
        // Lista de métodos considerados pontos de entrada do fluxo
        string[] names = {
            "EnterUiContextWithFocus",
            "ActivateLoadingUI",
            "DeactivateLoadingUI",
            "OnLoadingFinished",
            "OnCinematicFinished",
            "EnterPlayerContext",
            "EnterBlockInputContext"
        };
        foreach (var n in names)
        {
            if (methodName == n) return true;
        }
        return false;
    }
}