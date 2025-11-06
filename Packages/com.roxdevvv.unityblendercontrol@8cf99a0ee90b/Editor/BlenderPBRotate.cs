using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEditor.ProBuilder;
using static TransformModeManager;
using PHandleUtility = UnityEngine.ProBuilder.HandleUtility;
using System.Linq;

public class BlenderPBRotate : BlenderTransformMode {
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
        // Mirror PBMove/PBScale: only trigger in ProBuilder context with an element selection.
        if (!BlenderHelper.IsKeyDown(evt, KeyCode.R))
            return false;
        if (BlenderHelper.IsModifierPressed(evt) || BlenderHelper.RightMouseHeld)
            return false;
        if (Selection.transforms == null || Selection.transforms.Length == 0)
            return false;

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

        foreach (var go in Selection.gameObjects) {
            if (!go.TryGetComponent<ProBuilderMesh>(out var mesh))
                continue;
            var indices = CollectSelectedIndices(mesh, mode);
            if (indices.Count > 0) {
                evt.Use();
                return true;
            }
        }
        return false;
    }

    public override void Initialize() {
        Undo.RegisterCompleteObjectUndo(Selection.gameObjects, "Rotate Elements");

        _meshData = new List<MeshData>();
        _mouseStartPosition = Event.current.mousePosition;
        _globalAverage = Vector3.zero;
        _selectionBounds.SetMinMax(Vector3.positiveInfinity, Vector3.negativeInfinity);

        var mode = ProBuilderEditor.selectMode;
        int contributing = 0;

        foreach (var go in Selection.gameObjects) {
            if (!go.TryGetComponent<ProBuilderMesh>(out var mesh))
                continue;

            // Coleta índices base e expande para vértices coincidentes,
            // replicando o comportamento padrão do ProBuilder (move/rotate/scale em grupos compartilhados).
            var baseIndices = CollectSelectedIndices(mesh, mode);
            if (baseIndices.Count == 0)
                continue;
            var coincident = mesh.GetCoincidentVertices(baseIndices);
            var indices = new List<int>(new HashSet<int>(coincident));

            var world = mesh.VerticesInWorldSpace();
            var local = mesh.positions;

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
            ProBuilderEditor.Refresh(false);
            _meshData = null;
            BlenderManager.CurrentTransformMode = null;
            return;
        }
    }

    public override void Cancel() {
        if (_meshData == null) return;
        foreach (var md in _meshData) {
            var positions = new List<Vector3>(md.Mesh.positions);
            for (int j = 0; j < md.SelectedIndexes.Length; j++) {
                positions[md.SelectedIndexes[j]] = md.InitialLocalPositions[j];
            }
            md.Mesh.positions = positions;
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
            md.Mesh.ToMesh();
            md.Mesh.Refresh(RefreshMask.All);
        }
        ProBuilderEditor.Refresh(false);
        _meshData = null;
    }

    public override void Process(SceneView sv) {
        if (_meshData == null) return;
        float angle;
        if (BlenderManager.MoveByNumber) {
            angle = BlenderManager.CurrentNumber;
        } else {
            angle = ComputeMouseRotationAngle();
            float snap = BlenderHelper.GetSnapRotate();
            if (isSnappingEnabled) angle = Mathf.Round(angle / snap) * snap;
        }

        foreach (var md in _meshData) {
            DoRotate(md, sv, angle);
        }
    }

    public override void OnAxisChange() {
        if (_meshData == null) return;
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

        float screenScale = Screen.dpi / 96f;
        var mp = Event.current.mousePosition;
        var cam = sceneView.camera;
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
                // Anchor local axis lines at the same pivot used for transform to avoid visual misalignment
                foreach (var md in _meshData) {
                    BlenderManager.DrawAxisLine(center, md.LocalAxis, Selection.activeGameObject == md.Mesh.gameObject);
                }
                break;
        }
    }

    // Coleta índices distintos da seleção ativa (base), que serão expandidos
    // para vértices coincidentes na Initialize.
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

    void DoRotate(MeshData md, SceneView sv, float angle) {
        var positions = new List<Vector3>(md.Mesh.positions);
        Vector3 pivot = BlenderHelper.GetTransformationCenter(_globalAverage, _selectionBounds);

        Vector3 axis = BlenderManager.CurrentAxisMode switch {
            BlenderManager.AxisMode.Local => md.LocalAxis,
            BlenderManager.AxisMode.Global => GetBaseAxis(),
            _ => -sv.camera.transform.forward
        };
        axis.Normalize();
        Quaternion delta = Quaternion.AngleAxis(angle, axis);

        for (int j = 0; j < md.SelectedIndexes.Length; j++) {
            var initialWorld = md.InitialWorldPositions[j];
            var dir = initialWorld - pivot;
            var rotatedDir = delta * dir;
            var newWorld = pivot + rotatedDir;
            positions[md.SelectedIndexes[j]] = md.Mesh.transform.InverseTransformPoint(newWorld);
        }

        md.Mesh.positions = positions;
        md.Mesh.ToMesh();
        md.Mesh.Refresh(RefreshMask.All);
    }

    float ComputeMouseRotationAngle() {
        Vector3 centerGUIPoint = UnityEditor.HandleUtility.WorldToGUIPoint(BlenderHelper.GetTransformationCenter(_globalAverage, _selectionBounds));
        float initialAngle = AngleBetweenVector2(centerGUIPoint, _mouseStartPosition);
        float currentAngle = AngleBetweenVector2(centerGUIPoint, Event.current.mousePosition);
        float rotationAngle = initialAngle - currentAngle;

        // Invert when the view direction is opposite to the current axis
        if (SceneView.lastActiveSceneView != null) {
            Vector3 viewDir = SceneView.lastActiveSceneView.camera.transform.forward;
            bool invert = false;
            if (BlenderManager.CurrentAxisMode == BlenderManager.AxisMode.Local) {
                var go = Selection.activeGameObject;
                if (go != null && go.TryGetComponent<ProBuilderMesh>(out var mesh)) {
                    var axis = ComputeLocalAxis(mesh, GetBaseAxis());
                    invert = Vector3.Dot(viewDir, axis) > 0f;
                } else {
                    invert = Vector3.Dot(viewDir, Selection.activeTransform.rotation * GetBaseAxis()) > 0f;
                }
            } else if (BlenderManager.CurrentAxisMode == BlenderManager.AxisMode.Global) {
                invert = Vector3.Dot(viewDir, GetBaseAxis()) > 0f;
            }
            if (invert) rotationAngle = -rotationAngle;
        }
        return rotationAngle;
    }

    float AngleBetweenVector2(Vector3 vec1, Vector3 vec2) {
        Vector3 from = vec2 - vec1;
        Vector3 to = new Vector3(1, 0, 0);
        return Vector3.SignedAngle(from, to, Vector3.forward);
    }

    Quaternion ComputeElementRotation(ProBuilderMesh mesh) {
        // Align to the active element when possible; fall back to object rotation
        try {
            if (mesh.selectedFaceCount > 0) {
                var face = mesh.GetSelectedFaces().LastOrDefault();
                if (face != null)
                    return PHandleUtility.GetFaceRotation(mesh, HandleOrientation.ActiveElement, new List<Face> { face });
            }
            if (mesh.selectedEdgeCount > 0) {
                var edge = mesh.selectedEdges.Last();
                return PHandleUtility.GetEdgeRotation(mesh, HandleOrientation.ActiveElement, new List<Edge> { edge });
            }
            if (mesh.selectedVertexCount > 0) {
                var vi = mesh.selectedVertices.Last();
                return PHandleUtility.GetVertexRotation(mesh, HandleOrientation.ActiveElement, new List<int> { vi });
            }
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

    // Base axis: use CurrentAxis diretamente (swap já aplicado ao definir CurrentAxis)
    Vector3 GetBaseAxis() {
        switch (BlenderManager.CurrentAxis) {
            case BlenderManager.Axis.X: return Vector3.right;
            case BlenderManager.Axis.Y: return Vector3.up;
            case BlenderManager.Axis.Z: return Vector3.forward;
            default: return Vector3.one;
        }
    }
}