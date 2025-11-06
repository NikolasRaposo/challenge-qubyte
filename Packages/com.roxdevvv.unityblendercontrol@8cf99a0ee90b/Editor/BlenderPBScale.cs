using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEditor.ProBuilder;
using static TransformModeManager;
using PHandleUtility = UnityEngine.ProBuilder.HandleUtility;

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
        // Same precedence as PBMove, mirroring trigger logic
        if (!BlenderHelper.IsKeyDown(evt, KeyCode.S))
            return false;
        if (BlenderHelper.IsModifierPressed(evt) || BlenderHelper.RightMouseHeld)
            return false;
        if (Selection.transforms == null || Selection.transforms.Length == 0)
            return false;

        // Ensure the active context is ProBuilder, same as PBMove
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

        // Check for valid (coincident) indices as in PBMove to avoid false positives
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

        // Consume the event only when we have a valid PB selection
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

            // Coleta índices base e expande para vértices coincidentes para
            // manter o comportamento do ProBuilder (grupos compartilhados se movem juntos).
            var baseIndices = CollectSelectedIndices(mesh, mode);
            if (baseIndices.Count == 0)
                continue;
            var coincident = mesh.GetCoincidentVertices(baseIndices);
            var indices = new List<int>(new HashSet<int>(coincident));

            var world = mesh.VerticesInWorldSpace();
            var local = mesh.positions; // use ProBuilder public positions API

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
                LocalAxis = ComputeLocalAxis(mesh, GetBaseAxis())
            };
            _meshData.Add(md);
            _globalAverage += avg;
            contributing++;
        }

        if (contributing > 0) {
            _globalAverage /= contributing;
        } else {
            // No valid data: exit immediately and release state to avoid locking up
            ProBuilderEditor.Refresh(false);
            _meshData = null;
            BlenderManager.CurrentTransformMode = null;
            return;
        }
    }

    public override void Cancel() {
        // If there is no data, just refresh the UI and exit
        if (_meshData == null || _meshData.Count == 0) {
            ProBuilderEditor.Refresh(false);
            _meshData = null;
            return;
        }
        // Revert local positions to their initial values
        foreach (var md in _meshData) {
            // Copy into a mutable list and apply the initial positions
            var positions = new List<Vector3>(md.Mesh.positions);
            for (int j = 0; j < md.SelectedIndexes.Length; j++) {
                positions[md.SelectedIndexes[j]] = md.InitialLocalPositions[j];
            }
            md.Mesh.positions = positions;
            // Sync with the Unity Mesh after restoring positions
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
            // Ensure the Unity Mesh is up to date when finalizing
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

        // Optional snapping
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
            md.LocalAxis = ComputeLocalAxis(md.Mesh, GetBaseAxis());
            _meshData[i] = md;
        }
    }

    public override void OnAxisModeChange() {
        // no-op
    }

    public override void DrawSceneGUI(SceneView sceneView) {
        if (_meshData == null || _meshData.Count == 0)
            return;
        // Reference line from center to mouse for visual feedback
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
                BlenderManager.DrawAxisLine(center, GetBaseAxis(), true);
                break;
            case BlenderManager.AxisMode.Local:
                // Anchor local axis lines at the same pivot used for scaling
                foreach (var md in _meshData) {
                    BlenderManager.DrawAxisLine(center, md.LocalAxis, Selection.activeGameObject == md.Mesh.gameObject);
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

    // Helper: coleta índices distintos da seleção ativa (base).
    // A expansão para vértices coincidentes ocorre na Initialize.
    List<int> CollectSelectedIndices(ProBuilderMesh mesh, SelectMode mode) {
        var set = new HashSet<int>();
        if ((mode & SelectMode.Vertex) != 0) {
            foreach (var vi in mesh.selectedVertices) set.Add(vi);
        } else if ((mode & SelectMode.Edge) != 0) {
            foreach (var e in mesh.selectedEdges) { set.Add(e.a); set.Add(e.b); }
        } else if ((mode & SelectMode.Face) != 0) {
            foreach (var f in mesh.GetSelectedFaces()) {
                if (f == null || f.distinctIndexes == null) continue;
                foreach (var vi in f.distinctIndexes) set.Add(vi);
            }
        }
        return new List<int>(set);
    }

    void DoScale(MeshData md, float amount) {
        // Work on a mutable copy of positions and reassign to the mesh
        var positions = new List<Vector3>(md.Mesh.positions);
        Vector3 pivot = BlenderHelper.GetTransformationCenter(_globalAverage, _selectionBounds);

        if (BlenderManager.CurrentAxisMode == BlenderManager.AxisMode.Unlocked) {
            // Uniform scale around the pivot (in world space)
            for (int j = 0; j < md.SelectedIndexes.Length; j++) {
                var initialWorld = md.InitialWorldPositions[j];
                var newWorld = pivot + (initialWorld - pivot) * amount;
                positions[md.SelectedIndexes[j]] = md.Mesh.transform.InverseTransformPoint(newWorld);
            }
        } else {
            // Scale along a single axis (global or local), adjusting only the projected component
            Vector3 axis = BlenderManager.CurrentAxisMode == BlenderManager.AxisMode.Global
                ? GetBaseAxis()
                : md.LocalAxis;
            axis.Normalize();
            for (int j = 0; j < md.SelectedIndexes.Length; j++) {
                var initialWorld = md.InitialWorldPositions[j];
                float d = Vector3.Dot(axis, initialWorld - pivot); // distância ao longo do eixo
                var newWorld = initialWorld + axis * d * (amount - 1f);
                positions[md.SelectedIndexes[j]] = md.Mesh.transform.InverseTransformPoint(newWorld);
            }
        }

        // Reassign positions to ProBuilderMesh to avoid NotSupportedException
        md.Mesh.positions = positions;
        // Sync changes with the Unity Mesh in real time
        md.Mesh.ToMesh();
        md.Mesh.Refresh(RefreshMask.All);
    }

    Quaternion ComputeElementRotation(ProBuilderMesh mesh) {
        // Try to align to the active element selection; fall back to object rotation
        try {
            if (mesh.selectedFaceCount > 0)
                return PHandleUtility.GetFaceRotation(mesh, HandleOrientation.ActiveElement, mesh.GetSelectedFaces());
            if (mesh.selectedEdgeCount > 0)
                return PHandleUtility.GetEdgeRotation(mesh, HandleOrientation.ActiveElement, mesh.selectedEdges);
            if (mesh.selectedVertexCount > 0)
                return PHandleUtility.GetVertexRotation(mesh, HandleOrientation.ActiveElement, mesh.selectedVertices);
        } catch { /* fallback */ }
        return mesh.transform.rotation;
    }

    Vector3 ComputeLocalAxis(ProBuilderMesh mesh, Vector3 baseAxis) {
        // Use the element rotation when available; otherwise use the object's axis
        try {
            var rot = ComputeElementRotation(mesh);
            if (rot != mesh.transform.rotation)
                return rot * baseAxis;
        } catch { /* ignore */ }
        return BlenderHelper.GetObjectAxis(mesh.transform, baseAxis);
    }

    // Base axis: usa CurrentAxis diretamente (swap já aplicado ao definir CurrentAxis)
    Vector3 GetBaseAxis() {
        switch (BlenderManager.CurrentAxis) {
            case BlenderManager.Axis.X: return Vector3.right;
            case BlenderManager.Axis.Y: return Vector3.up;
            case BlenderManager.Axis.Z: return Vector3.forward;
            default: return Vector3.one;
        }
    }
}