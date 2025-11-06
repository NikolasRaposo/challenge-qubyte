using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEditor.ProBuilder;
using static TransformModeManager;
using PHandleUtility = UnityEngine.ProBuilder.HandleUtility;
using System.Linq;

public class BlenderPBMove : BlenderTransformMode {
    struct MeshData {
        public ProBuilderMesh Mesh;
        public int[] SelectedIndexes;
        public Vector3 InitialAverageWorld;
        public Vector3 InitialMouseWorld;
        public Vector3 LocalAxis;
        public Vector3 LastAppliedOffset; // world space offset applied so far
        public Quaternion ElementRotation; // rotation aligned with active element selection
    }

    private List<MeshData> _meshData;
    private Vector3 _globalAverage;
    private Bounds _selectionBounds;

    public override bool ShouldTrigger(Event evt) {
        // Soft-check for G without consuming the event.
        // We only call evt.Use() when we actually decide to trigger PB move.
        if (!BlenderHelper.IsKeyDown(evt, KeyCode.G))
            return false;
        if (BlenderHelper.IsModifierPressed(evt) || BlenderHelper.RightMouseHeld)
            return false;
        if (Selection.transforms == null || Selection.transforms.Length == 0)
            return false;

        // Ensure the active tool context is ProBuilder; otherwise do not intercept G
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
        // Consider element modes: Vertex, Edge, Face (ignore Texture modes and InputTool)
        bool isElementMode = (mode & (SelectMode.Vertex | SelectMode.Edge | SelectMode.Face)) != 0;
        if (!isElementMode)
            return false;

        // Ensure at least one ProBuilderMesh with elements selected
        foreach (var go in Selection.gameObjects) {
            if (go.TryGetComponent<ProBuilderMesh>(out var mesh)) {
                if (mesh.selectedVertexCount > 0 || mesh.selectedEdgeCount > 0 || mesh.selectedFaceCount > 0)
                {
                    // Now we are certain PBMove should handle this key. Consume the event.
                    evt.Use();
                    return true;
                }
            }
        }
        return false;
    }

    public override void Initialize() {
        Undo.RegisterCompleteObjectUndo(Selection.gameObjects, "Move Elements");

        _meshData = new List<MeshData>();
        var mode = ProBuilderEditor.selectMode;
        _globalAverage = Vector3.zero;
        _selectionBounds.SetMinMax(Vector3.positiveInfinity, Vector3.negativeInfinity);
        int contributingCount = 0;

        foreach (var go in Selection.gameObjects) {
            if (!go.TryGetComponent<ProBuilderMesh>(out var mesh))
                continue;

            // Alinhar ao fluxo do ProBuilder: expandir para vértices coincidentes
            // com base na seleção atual, garantindo que grupos compartilhados
            // sejam movidos juntos (após Extrude, os grupos são separados corretamente).
            var baseSelection = CollectSelectedIndicesNonCoincident(mesh, mode);
            if (baseSelection.Count == 0)
                continue;

            // Expande explicitamente para coincidentes como o ProBuilder faz
            var coincident = mesh.GetCoincidentVertices(baseSelection);
            // Mantém apenas índices distintos
            var indices = new List<int>(new HashSet<int>(coincident));

            // Compute initial average world position for selected indices
            var world = mesh.VerticesInWorldSpace();
            Vector3 avg = Vector3.zero;
            foreach (var i in indices)
            {
                avg += world[i];
                _selectionBounds.Encapsulate(world[i]);
            }
            avg /= indices.Count;

            var md = new MeshData {
                Mesh = mesh,
                SelectedIndexes = indices.ToArray(),
                InitialAverageWorld = avg,
                InitialMouseWorld = GetWorldMouse(avg),
                ElementRotation = ComputeElementRotation(mesh),
                LocalAxis = ComputeLocalAxis(mesh, GetBaseAxis()),
                LastAppliedOffset = Vector3.zero
            };
            _meshData.Add(md);
            _globalAverage += avg;
            contributingCount++;
        }

        if (contributingCount > 0)
            _globalAverage /= contributingCount;
    }

    public override void Cancel() {
        // Revert any applied offsets
        foreach (var md in _meshData) {
            if (md.LastAppliedOffset != Vector3.zero) {
                md.Mesh.TranslateVerticesInWorldSpace(md.SelectedIndexes, -md.LastAppliedOffset);
                md.Mesh.Refresh(RefreshMask.Normals | RefreshMask.UV | RefreshMask.Bounds);
            }
        }
        ProBuilderEditor.Refresh(false);
        _meshData = null;
    }

    public override void Apply() {
        // Finalize changes
        foreach (var md in _meshData) {
            md.Mesh.Refresh(RefreshMask.Normals | RefreshMask.UV | RefreshMask.Bounds);
        }
        ProBuilderEditor.Refresh(false);
        _meshData = null;
    }

    public override void Process(SceneView sv) {
        for (int i = 0; i < _meshData.Count; i++) {
            var md = _meshData[i];
            Vector3 targetOffset;

            if (BlenderManager.MoveByNumber) {
                // Move by unit along axis or local right when unlocked
                var axis = GetAxis(md);
                var baseDir = BlenderManager.CurrentAxisMode == BlenderManager.AxisMode.Unlocked ? md.Mesh.transform.right : axis;
                targetOffset = baseDir * BlenderManager.CurrentNumber;
            } else {
                // Mouse-driven move
                Vector3 currentMouse = GetWorldMouse(md.InitialAverageWorld);
                Vector3 rawOffset = currentMouse - md.InitialMouseWorld;

                if (BlenderManager.CurrentAxisMode == BlenderManager.AxisMode.Unlocked) {
                    if (isSnappingEnabled) {
                        targetOffset = SnapVector(rawOffset, BlenderHelper.GetSnapMove());
                    } else {
                        targetOffset = rawOffset;
                    }
                } else {
                    var axis = GetAxis(md);
                    float distance = Vector3.Dot(axis, rawOffset);
                    if (isSnappingEnabled) {
                        float snapStep = BlenderHelper.GetSnapMove().magnitude;
                        if (snapStep > 0f)
                            distance = Mathf.Round(distance / snapStep) * snapStep;
                    }
                    targetOffset = axis * distance;
                }
            }

            // Apply delta from last frame
            var delta = targetOffset - md.LastAppliedOffset;
            if (delta != Vector3.zero) {
                md.Mesh.TranslateVerticesInWorldSpace(md.SelectedIndexes, delta);
                // Mantém o comportamento do editor ProBuilder: sincroniza malha e caches
                md.Mesh.ToMesh();
                md.Mesh.Refresh(RefreshMask.Normals | RefreshMask.UV | RefreshMask.Bounds);
                md.LastAppliedOffset = targetOffset;
            }

            _meshData[i] = md;
        }
    }

    public override void OnAxisChange() {
        // Update local axis for each mesh (respect Element orientation when active)
        for (int i = 0; i < _meshData.Count; i++) {
            var md = _meshData[i];
            md.ElementRotation = ComputeElementRotation(md.Mesh);
            md.LocalAxis = ComputeLocalAxis(md.Mesh, GetBaseAxis());
            _meshData[i] = md;
        }
    }

    public override void OnAxisModeChange() {
        // No-op
    }

    public override void DrawSceneGUI(SceneView sceneView) {
        switch (BlenderManager.CurrentAxisMode) {
            case BlenderManager.AxisMode.Unlocked:
                break;
            case BlenderManager.AxisMode.Global:
                BlenderManager.DrawAxisLine(_globalAverage, GetBaseAxis(), true);
                break;
            case BlenderManager.AxisMode.Local:
                // Draw local axis lines from the actual pivot center for visual consistency
                Vector3 center = BlenderHelper.GetTransformationCenter(_globalAverage, _selectionBounds);
                foreach (var md in _meshData) {
                    var axis = GetAxis(md);
                    BlenderManager.DrawAxisLine(center, axis, Selection.activeGameObject == md.Mesh.gameObject);
                }
                break;
        }
    }

    Vector3 GetAxis(MeshData md) {
        return BlenderManager.CurrentAxisMode switch {
            BlenderManager.AxisMode.Local => md.LocalAxis,
            BlenderManager.AxisMode.Global => GetBaseAxis(),
            _ => Vector3.zero
        };
    }

    Vector3 GetWorldMouse(Vector3 pivotWorld) {
        Camera sceneViewCamera = SceneView.lastActiveSceneView.camera;
        float z = sceneViewCamera.WorldToScreenPoint(pivotWorld).z;
        Vector3 mouse = Event.current.mousePosition;
        mouse.y = sceneViewCamera.pixelHeight - mouse.y; // invert Y
        return sceneViewCamera.ScreenToWorldPoint(new Vector3(mouse.x, mouse.y, z));
    }

    Quaternion ComputeElementRotation(ProBuilderMesh mesh) {
        // Always try to align to the active element selection; fall back to object rotation
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
        // Use element rotation if available; otherwise use object axis
        try {
            var rot = ComputeElementRotation(mesh);
            if (rot != mesh.transform.rotation)
                return rot * baseAxis;
        } catch { /* ignore */ }
        return BlenderHelper.GetObjectAxis(mesh.transform, baseAxis);
    }

    // Base axis: use CurrentAxis directly (swap já aplicado ao definir CurrentAxis)
    Vector3 GetBaseAxis() {
        switch (BlenderManager.CurrentAxis) {
            case BlenderManager.Axis.X: return Vector3.right;
            case BlenderManager.Axis.Y: return Vector3.up;
            case BlenderManager.Axis.Z: return Vector3.forward;
            default: return Vector3.one;
        }
    }

    Vector3 SnapVector(Vector3 v, Vector3 snap) {
        if (snap.x == 0f && snap.y == 0f && snap.z == 0f)
            return v;
        return new Vector3(
            Mathf.Round(v.x / (snap.x == 0f ? 1f : snap.x)) * (snap.x == 0f ? 0f : snap.x),
            Mathf.Round(v.y / (snap.y == 0f ? 1f : snap.y)) * (snap.y == 0f ? 0f : snap.y),
            Mathf.Round(v.z / (snap.z == 0f ? 1f : snap.z)) * (snap.z == 0f ? 0f : snap.z)
        );
    }

    // Collect selected vertex indices without expanding to coincident vertices.
    // This prevents moving unselected but overlapping vertices (e.g., right after an Extrude).
    List<int> CollectSelectedIndicesNonCoincident(ProBuilderMesh mesh, SelectMode mode) {
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
}