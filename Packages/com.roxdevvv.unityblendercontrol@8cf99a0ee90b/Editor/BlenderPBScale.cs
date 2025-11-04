using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEditor.ProBuilder;
using static TransformModeManager;

public class BlenderPBScale : BlenderTransformMode {
    struct MeshData {
        public ProBuilderMesh Mesh;
        public int[] SelectedIndexes;
        public Vector3[] InitialLocalPositions;
        public Vector3[] InitialWorldPositions;
        public Vector3 InitialAverageWorld;
        public Vector3 LocalAxis;
    }

    private List<MeshData> _meshData;
    private Vector2 _mouseStartPosition;
    private Vector3 _globalAverage;
    private Bounds _selectionBounds;

    public override bool ShouldTrigger(Event evt) {
        // Mesma precedência do PBMove, espelhando lógica de gatilho
        if (!BlenderHelper.IsKeyDown(evt, KeyCode.S))
            return false;
        if (BlenderHelper.IsModifierPressed(evt) || BlenderHelper.RightMouseHeld)
            return false;
        if (Selection.transforms == null || Selection.transforms.Length == 0)
            return false;

        // Garantir que o contexto ativo seja ProBuilder, como no PBMove
        #if UNITY_EDITOR
        var ctxType = UnityEditor.EditorTools.ToolManager.activeContextType;
        bool proBuilderContext = false;
        if (ctxType != null) {
            var fullName = ctxType.FullName ?? ctxType.Name;
            proBuilderContext = !string.IsNullOrEmpty(fullName) && fullName.IndexOf("ProBuilder", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }
        if (!proBuilderContext)
            return false;
        #endif

        var mode = ProBuilderEditor.selectMode;
        bool isElementMode = (mode & (SelectMode.Vertex | SelectMode.Edge | SelectMode.Face)) != 0;
        if (!isElementMode)
            return false;

        // Verifique índices válidos (coincidentes) como no PBMove para evitar falso-positivo
        bool hasValidIndices = false;
        foreach (var go in Selection.gameObjects) {
            if (!go.TryGetComponent<ProBuilderMesh>(out var mesh))
                continue;
            var indices = CollectSelectedIndices(mesh, mode);
            if (indices != null && indices.Count > 0) {
                hasValidIndices = true;
                break;
            }
        }

        if (!hasValidIndices)
            return false;

        // Consumir o evento somente quando temos seleção PB válida
        evt.Use();
        return true;
    }

    public override void Initialize() {
        Undo.RegisterCompleteObjectUndo(Selection.gameObjects, "Scale Elements");

        _meshData = new List<MeshData>();
        _mouseStartPosition = Event.current.mousePosition;
        _globalAverage = Vector3.zero;
        _selectionBounds.SetMinMax(Vector3.positiveInfinity, Vector3.negativeInfinity);

        var mode = ProBuilderEditor.selectMode;
        int contributing = 0;

        foreach (var go in Selection.gameObjects) {
            if (!go.TryGetComponent<ProBuilderMesh>(out var mesh))
                continue;

            var indices = CollectSelectedIndices(mesh, mode);

            if (indices.Count == 0)
                continue;

            var world = mesh.VerticesInWorldSpace();
            var local = mesh.positions; // usar API pública de posições do ProBuilder

            Vector3 avg = Vector3.zero;
            var initLocal = new Vector3[indices.Count];
            var initWorld = new Vector3[indices.Count];
            for (int j = 0; j < indices.Count; j++) {
                int vi = indices[j];
                var wp = world[vi];
                initWorld[j] = wp;
                initLocal[j] = local[vi];
                avg += wp;
                _selectionBounds.Encapsulate(wp);
            }
            avg /= indices.Count;

            var md = new MeshData {
                Mesh = mesh,
                SelectedIndexes = indices.ToArray(),
                InitialLocalPositions = initLocal,
                InitialWorldPositions = initWorld,
                InitialAverageWorld = avg,
                LocalAxis = BlenderHelper.GetObjectAxis(mesh.transform, BlenderManager.CurrentAxisVector)
            };
            _meshData.Add(md);
            _globalAverage += avg;
            contributing++;
        }

        if (contributing > 0) {
            _globalAverage /= contributing;
        } else {
            // Não há dados válidos: sair imediatamente e liberar estado para evitar travamento
            ProBuilderEditor.Refresh(false);
            _meshData = null;
            BlenderManager.CurrentTransformMode = null;
            return;
        }
    }

    public override void Cancel() {
        // Se não houver dados, apenas atualizar a UI e sair
        if (_meshData == null || _meshData.Count == 0) {
            ProBuilderEditor.Refresh(false);
            _meshData = null;
            return;
        }
        // Reverter posições locais para os valores iniciais
        foreach (var md in _meshData) {
            // Copiar para uma lista mutável e aplicar as posições iniciais
            var positions = new List<Vector3>(md.Mesh.positions);
            for (int j = 0; j < md.SelectedIndexes.Length; j++) {
                positions[md.SelectedIndexes[j]] = md.InitialLocalPositions[j];
            }
            md.Mesh.positions = positions;
            // Sincronizar com a Unity Mesh após restaurar posições
            md.Mesh.ToMesh();
            md.Mesh.Refresh(RefreshMask.All);
        }
        ProBuilderEditor.Refresh(false);
        _meshData = null;
    }

    public override void Apply() {
        if (_meshData == null || _meshData.Count == 0) {
            ProBuilderEditor.Refresh(false);
            _meshData = null;
            return;
        }
        foreach (var md in _meshData) {
            // Garantir que a Unity Mesh esteja atualizada ao finalizar
            md.Mesh.ToMesh();
            md.Mesh.Refresh(RefreshMask.All);
        }
        ProBuilderEditor.Refresh(false);
        _meshData = null;
    }

    public override void Process(SceneView sv) {
        if (_meshData == null || _meshData.Count == 0)
            return;
        float amount;
        if (BlenderManager.MoveByNumber) {
            amount = BlenderManager.CurrentNumber;
        } else {
            amount = ComputeMouseScaleFactor();
        }

        // Snap opcional
        if (isSnappingEnabled) {
            float snap = BlenderHelper.GetSnapScale();
            amount = Mathf.Round(amount / snap) * snap;
            if (Mathf.Approximately(amount, 0f)) amount = 1f;
        }

        foreach (var md in _meshData) {
            DoScale(md, amount);
        }
    }

    public override void OnAxisChange() {
        for (int i = 0; i < _meshData.Count; i++) {
            var md = _meshData[i];
            md.LocalAxis = BlenderHelper.GetObjectAxis(md.Mesh.transform, BlenderManager.CurrentAxisVector);
            _meshData[i] = md;
        }
    }

    public override void OnAxisModeChange() {
        // no-op
    }

    public override void DrawSceneGUI(SceneView sceneView) {
        if (_meshData == null || _meshData.Count == 0)
            return;
        // Linha de referência do centro ao mouse para feedback visual
        var mp = Event.current.mousePosition;
        var cam = sceneView.camera;
        float screenScale = Screen.dpi / 96f;
        var mouseWorld = cam.ScreenToWorldPoint(new Vector3(screenScale * mp.x, screenScale * (sceneView.cameraViewport.height - mp.y), 1));
        Vector3 center = BlenderHelper.GetTransformationCenter(_globalAverage, _selectionBounds);
        Handles.color = Color.black;
        Handles.DrawLine(center, mouseWorld);

        switch (BlenderManager.CurrentAxisMode) {
            case BlenderManager.AxisMode.Unlocked:
                break;
            case BlenderManager.AxisMode.Global:
                BlenderManager.DrawAxisLine(center, BlenderManager.CurrentAxisVector, true);
                break;
            case BlenderManager.AxisMode.Local:
                foreach (var md in _meshData) {
                    BlenderManager.DrawAxisLine(md.InitialAverageWorld, md.LocalAxis, Selection.activeGameObject == md.Mesh.gameObject);
                }
                break;
        }
    }

    float ComputeMouseScaleFactor() {
        Vector3 centerGUIPoint = UnityEditor.HandleUtility.WorldToGUIPoint(BlenderHelper.GetTransformationCenter(_globalAverage, _selectionBounds));
        Vector3 centerToStart = _mouseStartPosition - (Vector2)centerGUIPoint;
        Vector3 centerToCurrent = Event.current.mousePosition - (Vector2)centerGUIPoint;
        float initialLen = centerToStart.magnitude;
        if (initialLen < 1e-5f) return 1f;
        float currentLen = centerToCurrent.magnitude;
        float factor = currentLen / initialLen;
        if (Vector3.Dot(centerToStart, centerToCurrent) < 0f) factor = -factor;
        return factor;
    }

    // Helper: coletar índices válidos da seleção atual com coincidências
    List<int> CollectSelectedIndices(ProBuilderMesh mesh, SelectMode mode) {
        var indices = new List<int>();
        if ((mode & SelectMode.Vertex) != 0) {
            mesh.GetCoincidentVertices(mesh.selectedVertices, indices);
        } else if ((mode & SelectMode.Edge) != 0) {
            mesh.GetCoincidentVertices(mesh.selectedEdges, indices);
        } else if ((mode & SelectMode.Face) != 0) {
            mesh.GetCoincidentVertices(mesh.GetSelectedFaces(), indices);
        }
        return indices;
    }

    void DoScale(MeshData md, float amount) {
        // Trabalhar sobre uma cópia mutável das posições e reatribuir ao mesh
        var positions = new List<Vector3>(md.Mesh.positions);
        Vector3 pivot = BlenderHelper.GetTransformationCenter(_globalAverage, _selectionBounds);

        if (BlenderManager.CurrentAxisMode == BlenderManager.AxisMode.Unlocked) {
            // Escala uniforme em torno do pivô (em espaço de mundo)
            for (int j = 0; j < md.SelectedIndexes.Length; j++) {
                var initialWorld = md.InitialWorldPositions[j];
                var newWorld = pivot + (initialWorld - pivot) * amount;
                positions[md.SelectedIndexes[j]] = md.Mesh.transform.InverseTransformPoint(newWorld);
            }
        } else {
            // Escala em um único eixo (global ou local), ajustando apenas a componente projetada
            Vector3 axis = BlenderManager.CurrentAxisMode == BlenderManager.AxisMode.Global
                ? BlenderManager.CurrentAxisVector
                : md.LocalAxis;
            axis.Normalize();
            for (int j = 0; j < md.SelectedIndexes.Length; j++) {
                var initialWorld = md.InitialWorldPositions[j];
                float d = Vector3.Dot(axis, initialWorld - pivot); // distância ao longo do eixo
                var newWorld = initialWorld + axis * d * (amount - 1f);
                positions[md.SelectedIndexes[j]] = md.Mesh.transform.InverseTransformPoint(newWorld);
            }
        }

        // Reatribuir posições ao ProBuilderMesh para evitar NotSupportedException
        md.Mesh.positions = positions;
        // Sincronizar alterações com a Unity Mesh em tempo real
        md.Mesh.ToMesh();
        md.Mesh.Refresh(RefreshMask.All);
    }
}