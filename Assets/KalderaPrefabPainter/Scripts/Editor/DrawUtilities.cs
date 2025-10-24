using UnityEditor;
using UnityEngine;

namespace CollisionBear.WorldEditor {
    public static class DrawUtilities {
        public static void DrawSceneViewMesh(Mesh mesh, Material material, Vector3 position, Quaternion rotation, Vector3 scale) {
            if (mesh == null || material == null) {
                return;
            }

            var bodyTrs = Matrix4x4.TRS(position, rotation, scale);

            for (int i = 0; i < material.passCount; i++) {
                if (material.SetPass(i)) {
                    Graphics.DrawMeshNow(mesh, bodyTrs);
                }
            }
        }
    }
}