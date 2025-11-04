using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEditor.ProBuilder;
using static TransformModeManager;

public class BlenderPBMove : BlenderTransformMode {
    struct MeshData {
        public ProBuilderMesh Mesh;
        public int[] SelectedIndexes;
        public Vector3 InitialAverageWorld;
        public Vector3 InitialMouseWorld;
        public Vector3 LocalAxis;
        public Vector3 LastAppliedOffset; // world space offset applied so far
    }

    private List<MeshData> _meshData;
    private Vector3 _globalAverage;

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
        int contributingCount = 0;

        foreach (var go in Selection.gameObjects) {
            if (!go.TryGetComponent<ProBuilderMesh>(out var mesh))
                continue;

            var indices = new List<int>();
            if ((mode & SelectMode.Vertex) != 0) {
                mesh.GetCoincidentVertices(mesh.selectedVertices, indices);
            } else if ((mode & SelectMode.Edge) != 0) {
                mesh.GetCoincidentVertices(mesh.selectedEdges, indices);
            } else if ((mode & SelectMode.Face) != 0) {
                mesh.GetCoincidentVertices(mesh.GetSelectedFaces(), indices);
            }

            if (indices.Count == 0)
                continue;

            // Compute initial average world position for selected indices
            var world = mesh.VerticesInWorldSpace();
            Vector3 avg = Vector3.zero;
            foreach (var i in indices)
                avg += world[i];
            avg /= indices.Count;

            var md = new MeshData {
                Mesh = mesh,
                SelectedIndexes = indices.ToArray(),
                InitialAverageWorld = avg,
                InitialMouseWorld = GetWorldMouse(avg),
                LocalAxis = BlenderHelper.GetObjectAxis(mesh.transform, BlenderManager.CurrentAxisVector),
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
                md.Mesh.Refresh(RefreshMask.Normals | RefreshMask.Bounds);
            }
        }
        ProBuilderEditor.Refresh(false);
        _meshData = null;
    }

    public override void Apply() {
        // Finalize changes
        foreach (var md in _meshData) {
            md.Mesh.Refresh(RefreshMask.Normals | RefreshMask.Bounds);
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
                md.LastAppliedOffset = targetOffset;
            }

            _meshData[i] = md;
        }
    }

    public override void OnAxisChange() {
        // Update local axis for each mesh
        for (int i = 0; i < _meshData.Count; i++) {
            var md = _meshData[i];
            md.LocalAxis = BlenderHelper.GetObjectAxis(md.Mesh.transform, BlenderManager.CurrentAxisVector);
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
                BlenderManager.DrawAxisLine(_globalAverage, BlenderManager.CurrentAxisVector, true);
                break;
            case BlenderManager.AxisMode.Local:
                foreach (var md in _meshData) {
                    BlenderManager.DrawAxisLine(md.InitialAverageWorld, md.LocalAxis, Selection.activeGameObject == md.Mesh.gameObject);
                }
                break;
        }
    }

    Vector3 GetAxis(MeshData md) {
        return BlenderManager.CurrentAxisMode switch {
            BlenderManager.AxisMode.Local => md.LocalAxis,
            BlenderManager.AxisMode.Global => BlenderManager.CurrentAxisVector,
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

    Vector3 SnapVector(Vector3 v, Vector3 snap) {
        if (snap.x == 0f && snap.y == 0f && snap.z == 0f)
            return v;
        return new Vector3(
            Mathf.Round(v.x / (snap.x == 0f ? 1f : snap.x)) * (snap.x == 0f ? 0f : snap.x),
            Mathf.Round(v.y / (snap.y == 0f ? 1f : snap.y)) * (snap.y == 0f ? 0f : snap.y),
            Mathf.Round(v.z / (snap.z == 0f ? 1f : snap.z)) * (snap.z == 0f ? 0f : snap.z)
        );
    }
}