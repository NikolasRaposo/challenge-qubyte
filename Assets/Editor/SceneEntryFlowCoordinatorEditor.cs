using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SceneEntryFlowCoordinator))]
public class SceneEntryFlowCoordinatorEditor : Editor
{
    SerializedProperty overrideStartPhase;
    SerializedProperty startPhaseOverride;
    SerializedProperty changeInputContextOnStart;
    SerializedProperty startInputContext;
    SerializedProperty enableUiInteractionsOnStart;
    SerializedProperty blockGameplayInputOnStart;
    SerializedProperty autoPlayCinematicAfterLoading;
    // Dev Start Checkpoint
    SerializedProperty startAtCheckpoint;
    SerializedProperty startCheckpointName;
    SerializedProperty startCheckpointIndex;

    SerializedProperty cinematicRoot;
    SerializedProperty cinematicDirector;

    SerializedProperty loadingAnimator;
    SerializedProperty loadingStartTrigger;
    SerializedProperty loadingStopTrigger;

    SerializedProperty entryUiRoot; // UMainMenu raiz
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

    // Runtime status (somente leitura no editor)
    SerializedProperty currentPhase;
    SerializedProperty isLoading;
    SerializedProperty isCinematicPlaying;
    SerializedProperty hasHandoffHappened;

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
        overrideStartPhase = serializedObject.FindProperty("overrideStartPhase");
        startPhaseOverride = serializedObject.FindProperty("startPhaseOverride");
        changeInputContextOnStart = serializedObject.FindProperty("changeInputContextOnStart");
        startInputContext = serializedObject.FindProperty("startInputContext");
        enableUiInteractionsOnStart = serializedObject.FindProperty("enableUiInteractionsOnStart");
        blockGameplayInputOnStart = serializedObject.FindProperty("blockGameplayInputOnStart");
        autoPlayCinematicAfterLoading = serializedObject.FindProperty("autoPlayCinematicAfterLoading");
        // Dev Start Checkpoint
        startAtCheckpoint = serializedObject.FindProperty("startAtCheckpoint");
        startCheckpointName = serializedObject.FindProperty("startCheckpointName");
        startCheckpointIndex = serializedObject.FindProperty("startCheckpointIndex");

        cinematicRoot = serializedObject.FindProperty("cinematicRoot");
        cinematicDirector = serializedObject.FindProperty("cinematicDirector");

        loadingAnimator = serializedObject.FindProperty("loadingAnimator");
        loadingStartTrigger = serializedObject.FindProperty("loadingStartTrigger");
        loadingStopTrigger = serializedObject.FindProperty("loadingStopTrigger");

        entryUiRoot = serializedObject.FindProperty("entryUiRoot");
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

        // Runtime status
        currentPhase = serializedObject.FindProperty("currentPhase");
        isLoading = serializedObject.FindProperty("isLoading");
        isCinematicPlaying = serializedObject.FindProperty("isCinematicPlaying");
        hasHandoffHappened = serializedObject.FindProperty("hasHandoffHappened");
    }

    // Cache de nomes de checkpoints para o dropdown
    private string[] _checkpointNames;
    private int _selectedCheckpointIdx = -1;
    private void RefreshCheckpointNames()
    {
        // Tenta usar CheckpointManager se estiver presente; caso contrário, faz uma busca por tag.
        System.Collections.Generic.List<string> names = new System.Collections.Generic.List<string>();
        var cm = Managers.CheckpointManager.Instance;
        if (cm != null)
        {
            try
            {
                var list = cm.GetAll();
                foreach (var it in list)
                {
                    if (it != null && it.transform != null)
                        names.Add(it.name);
                }
            }
            catch { /* segura no editor */ }
        }
        if (names.Count == 0)
        {
            try
            {
                var gos = GameObject.FindGameObjectsWithTag("CheckPoint");
                foreach (var go in gos)
                {
                    if (go != null) names.Add(go.name);
                }
            }
            catch { /* segura em caso de tag inexistente */ }
        }

        _checkpointNames = names.ToArray();
        // Sincroniza seleção com propriedade existente
        _selectedCheckpointIdx = -1;
        if (!string.IsNullOrEmpty(startCheckpointName.stringValue))
        {
            for (int i = 0; i < _checkpointNames.Length; i++)
            {
                if (_checkpointNames[i] == startCheckpointName.stringValue)
                {
                    _selectedCheckpointIdx = i;
                    break;
                }
            }
        }
        // Se não houver nome, usa índice quando válido
        if (_selectedCheckpointIdx < 0 && startCheckpointIndex.intValue >= 0 && startCheckpointIndex.intValue < _checkpointNames.Length)
        {
            _selectedCheckpointIdx = startCheckpointIndex.intValue;
            startCheckpointName.stringValue = _checkpointNames[_selectedCheckpointIdx];
        }
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Dev Overrides — destaque no topo
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Dev Overrides", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.HelpBox("Override de fase inicial para acelerar testes (ex.: ir direto para Gameplay).", MessageType.Warning);
        EditorGUILayout.PropertyField(overrideStartPhase, new GUIContent("Override Start Phase"));
        EditorGUI.BeginDisabledGroup(!overrideStartPhase.boolValue);
        EditorGUILayout.PropertyField(startPhaseOverride, new GUIContent("Start Phase Override"));
        // Dev Start Checkpoint UI
        if (overrideStartPhase.boolValue)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField("Dev Start Checkpoint", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(startAtCheckpoint, new GUIContent("Start At Checkpoint"));
            EditorGUI.BeginDisabledGroup(!startAtCheckpoint.boolValue || startPhaseOverride.enumValueIndex != (int)SceneEntryFlowCoordinator.Phase.Gameplay);
            // Atualiza lista sob demanda
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Atualizar lista", GUILayout.Width(120)))
            {
                RefreshCheckpointNames();
            }
            var countLabel = _checkpointNames != null ? _checkpointNames.Length.ToString() : "?";
            EditorGUILayout.LabelField($"Encontrados: {countLabel}");
            EditorGUILayout.EndHorizontal();

            if (_checkpointNames == null)
            {
                RefreshCheckpointNames();
            }
            if (_checkpointNames != null && _checkpointNames.Length > 0)
            {
                int newIdx = EditorGUILayout.Popup("Checkpoint", _selectedCheckpointIdx < 0 ? 0 : _selectedCheckpointIdx, _checkpointNames);
                if (newIdx != _selectedCheckpointIdx)
                {
                    _selectedCheckpointIdx = newIdx;
                    startCheckpointName.stringValue = _checkpointNames[newIdx];
                    startCheckpointIndex.intValue = newIdx;
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Nenhum checkpoint encontrado na cena. Certifique-se que objetos estão com tag 'CheckPoint'.", MessageType.Warning);
                EditorGUILayout.PropertyField(startCheckpointName, new GUIContent("Checkpoint Name (manual)"));
                EditorGUILayout.PropertyField(startCheckpointIndex, new GUIContent("Checkpoint Index"));
            }
            EditorGUI.EndDisabledGroup();
            EditorGUI.indentLevel--;
        }
        EditorGUI.EndDisabledGroup();
        EditorGUILayout.EndVertical();

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

        // Status em Runtime
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Status em Runtime", EditorStyles.boldLabel);
        if (Application.isPlaying)
        {
            var style = new GUIStyle(EditorStyles.helpBox) { richText = true };
            // Exibe fase atual em verde
            string phaseName = currentPhase.enumDisplayNames[currentPhase.enumValueIndex];
            EditorGUILayout.LabelField($"<color=#2ecc71><b>Fase Atual:</b> {phaseName}</color>", style);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("isLoading:", GUILayout.Width(130));
            EditorGUILayout.LabelField(isLoading.boolValue ? "true" : "false");
            EditorGUILayout.LabelField("isCinematicPlaying:", GUILayout.Width(160));
            EditorGUILayout.LabelField(isCinematicPlaying.boolValue ? "true" : "false");
            EditorGUILayout.LabelField("hasHandoffHappened:", GUILayout.Width(180));
            EditorGUILayout.LabelField(hasHandoffHappened.boolValue ? "true" : "false");
            EditorGUILayout.EndHorizontal();
        }
        else
        {
            EditorGUILayout.HelpBox("Este painel aparece durante Play Mode para facilitar QA.", MessageType.Info);
        }

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

                    // Denúncia de duplicidades por método de entrada do fluxo
                    var counts = new System.Collections.Generic.Dictionary<string, int>();
                    foreach (var x in _activators)
                    {
                        if (!counts.ContainsKey(x.methodName)) counts[x.methodName] = 0;
                        counts[x.methodName]++;
                    }
                    int dupTotal = 0;
                    foreach (var kv in counts)
                    {
                        if (kv.Value > 1) dupTotal++;
                    }
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
            EditorGUILayout.PropertyField(entryUiRoot, new GUIContent("Entry UI Root (UMainMenu)"));
            EditorGUILayout.HelpBox("Defina aqui a RAIZ do UMainMenu (Entry UI). O fluxo usa este objeto para ativar/desativar o menu inicial sem afetar o Canvas de gameplay.", MessageType.Info);
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
                    EditorGUILayout.PropertyField(canvasUI, new GUIContent("Canvas de Gameplay (HUD/Pause)"));
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
        var behaviours = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
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
            "OnStartingGameFinished",
            "EnterUiContextWithFocus",
            "ActivateLoadingUI",
            "DeactivateLoadingUI",
            "OnLoadingFinished",
            "OnCinematicFinished",
            "EnterPlayerContext",
            "EnterBlockInputContext",
            "ActivateAndPlayCinematic",
            "RequestTransitionTo"
        };
        foreach (var n in names)
        {
            if (methodName == n) return true;
        }
        return false;
    }
}