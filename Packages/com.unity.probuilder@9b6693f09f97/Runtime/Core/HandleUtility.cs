using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace UnityEngine.ProBuilder
{
    /// <summary>
    /// Provides static methods for working with ProBuilderMesh objects in the Editor.
    /// </summary>
    public static class HandleUtility
    {
        // Cache do vetor "up" (bitangente) por face para manter continuidade e evitar flips de 180°.
        static Dictionary<int, Dictionary<int, Vector3>> s_LastFaceUp = new Dictionary<int, Dictionary<int, Vector3>>();
        /// <summary>
        /// Convert a screen point (0,0 bottom left, in pixels) to a GUI point (0,0 top left, in points).
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="point"></param>
        /// <param name="pixelsPerPoint"></param>
        /// <returns></returns>
        internal static Vector3 ScreenToGuiPoint(this Camera camera, Vector3 point, float pixelsPerPoint)
        {
            return new Vector3(point.x / pixelsPerPoint, (camera.pixelHeight - point.y) / pixelsPerPoint, point.z);
        }

        /// <summary>
        /// Find a triangle intersected by InRay on InMesh.  InRay is in world space.
        /// Returns the index in mesh.faces of the hit face, or -1.  Optionally can ignore backfaces.
        /// </summary>
        /// <param name="worldRay"></param>
        /// <param name="mesh"></param>
        /// <param name="hit"></param>
        /// <param name="ignore"></param>
        /// <returns></returns>
        internal static bool FaceRaycast(Ray worldRay, ProBuilderMesh mesh, out RaycastHit hit, HashSet<Face> ignore = null)
        {
            return FaceRaycast(worldRay, mesh, out hit, Mathf.Infinity, CullingMode.Back, ignore);
        }

        /// <summary>
        /// Find the nearest face intersected by InWorldRay on this pb_Object.
        /// </summary>
        /// <param name="worldRay">A ray in world space.</param>
        /// <param name="mesh">The ProBuilder object to raycast against.</param>
        /// <param name="hit">If the mesh was intersected, hit contains information about the intersect point in local coordinate space.</param>
        /// <param name="distance">The distance from the ray origin to the intersection point.</param>
        /// <param name="cullingMode">Which sides of a face are culled when hit testing. Default is back faces are culled.</param>
        /// <param name="ignore">Optional collection of faces to ignore when raycasting.</param>
        /// <returns>True if the ray intersects with the mesh, false if not.</returns>
        internal static bool FaceRaycast(Ray worldRay, ProBuilderMesh mesh, out RaycastHit hit, float distance, CullingMode cullingMode, HashSet<Face> ignore = null)
        {
            // Transform ray into model space
            worldRay.origin -= mesh.transform.position; // Why doesn't worldToLocalMatrix apply translation?
            worldRay.origin = mesh.transform.worldToLocalMatrix * worldRay.origin;
            worldRay.direction = mesh.transform.worldToLocalMatrix * worldRay.direction;

            var positions = mesh.positionsInternal;
            var faces = mesh.facesInternal;

            float OutHitPoint = Mathf.Infinity;
            int OutHitFace = -1;
            Vector3 OutNrm = Vector3.zero;

            // Iterate faces, testing for nearest hit to ray origin. Optionally ignores backfaces.
            for (int i = 0, fc = faces.Length; i < fc; ++i)
            {
                if (ignore != null && ignore.Contains(faces[i]))
                    continue;

                int[] indexes = mesh.facesInternal[i].indexesInternal;

                for (int j = 0, ic = indexes.Length; j < ic; j += 3)
                {
                    Vector3 a = positions[indexes[j + 0]];
                    Vector3 b = positions[indexes[j + 1]];
                    Vector3 c = positions[indexes[j + 2]];

                    Vector3 nrm = Vector3.Cross(b - a, c - a);
                    float dot = Vector3.Dot(worldRay.direction, nrm);

                    bool skip = false;

                    switch (cullingMode)
                    {
                        case CullingMode.Front:
                            if (dot < 0f) skip = true;
                            break;

                        case CullingMode.Back:
                            if (dot > 0f) skip = true;
                            break;
                    }

                    var dist = 0f;

                    Vector3 point;
                    if (!skip && Math.RayIntersectsTriangle(worldRay, a, b, c, out dist, out point))
                    {
                        if (dist > OutHitPoint || dist > distance)
                            continue;

                        OutNrm = nrm;
                        OutHitFace = i;
                        OutHitPoint = dist;
                    }
                }
            }

            hit = new RaycastHit(OutHitPoint,
                    worldRay.GetPoint(OutHitPoint),
                    OutNrm,
                    OutHitFace);

            return OutHitFace > -1;
        }

        internal static bool FaceRaycastBothCullModes(Ray worldRay, ProBuilderMesh mesh, ref SimpleTuple<Face, Vector3> back, ref SimpleTuple<Face, Vector3> front)
        {
            // Transform ray into model space
            worldRay.origin -= mesh.transform.position; // Why doesn't worldToLocalMatrix apply translation?
            worldRay.origin = mesh.transform.worldToLocalMatrix * worldRay.origin;
            worldRay.direction = mesh.transform.worldToLocalMatrix * worldRay.direction;

            var positions = mesh.positionsInternal;
            var faces = mesh.facesInternal;

            back.item1 = null;
            front.item1 = null;

            float backDistance = Mathf.Infinity;
            float frontDistance = Mathf.Infinity;

            // Iterate faces, testing for nearest hit to ray origin. Optionally ignores backfaces.
            for (int i = 0, fc = faces.Length; i < fc; ++i)
            {
                int[] indexes = mesh.facesInternal[i].indexesInternal;

                for (int j = 0, ic = indexes.Length; j < ic; j += 3)
                {
                    Vector3 a = positions[indexes[j + 0]];
                    Vector3 b = positions[indexes[j + 1]];
                    Vector3 c = positions[indexes[j + 2]];

                    float dist;
                    Vector3 point;

                    if (Math.RayIntersectsTriangle(worldRay, a, b, c, out dist, out point))
                    {
                        if (dist < backDistance || dist < frontDistance)
                        {
                            Vector3 nrm = Vector3.Cross(b - a, c - a);
                            float dot = Vector3.Dot(worldRay.direction, nrm);

                            if (dot < 0f)
                            {
                                if (dist < backDistance)
                                {
                                    backDistance = dist;
                                    back.item1 = faces[i];
                                }
                            }
                            else
                            {
                                if (dist < frontDistance)
                                {
                                    frontDistance = dist;
                                    front.item1 = faces[i];
                                }
                            }
                        }
                    }
                }
            }

            if (back.item1 != null)
                back.item2 = worldRay.GetPoint(backDistance);

            if (front.item1 != null)
                front.item2 = worldRay.GetPoint(frontDistance);

            return back.item1 != null || front.item1 != null;
        }

        /// <summary>
        /// Find the all faces intersected by InWorldRay on this pb_Object.
        /// </summary>
        /// <param name="InWorldRay">A ray in world space.</param>
        /// <param name="mesh">The ProBuilder object to raycast against.</param>
        /// <param name="hits">If the mesh was intersected, hits contains all intersection point RaycastHit information.</param>
        /// <param name="cullingMode">What sides of triangles does the ray intersect with.</param>
        /// <param name="ignore">Optional collection of faces to ignore when raycasting.</param>
        /// <returns>True if the ray intersects with the mesh, false if not.</returns>
        internal static bool FaceRaycast(
            Ray InWorldRay,
            ProBuilderMesh mesh,
            out List<RaycastHit> hits,
            CullingMode cullingMode,
            HashSet<Face> ignore = null)
        {
            // Transform ray into model space
            InWorldRay.origin -= mesh.transform.position;  // Why doesn't worldToLocalMatrix apply translation?

            InWorldRay.origin       = mesh.transform.worldToLocalMatrix * InWorldRay.origin;
            InWorldRay.direction    = mesh.transform.worldToLocalMatrix * InWorldRay.direction;

            Vector3[] vertices = mesh.positionsInternal;

            hits = new List<RaycastHit>();

            // Iterate faces, testing for nearest hit to ray origin.  Optionally ignores backfaces.
            for (int CurFace = 0; CurFace < mesh.facesInternal.Length; ++CurFace)
            {
                if (ignore != null && ignore.Contains(mesh.facesInternal[CurFace]))
                    continue;

                int[] indexes = mesh.facesInternal[CurFace].indexesInternal;

                for (int CurTriangle = 0; CurTriangle < indexes.Length; CurTriangle += 3)
                {
                    Vector3 a = vertices[indexes[CurTriangle + 0]];
                    Vector3 b = vertices[indexes[CurTriangle + 1]];
                    Vector3 c = vertices[indexes[CurTriangle + 2]];

                    var dist = 0f;
                    Vector3 point;

                    if (Math.RayIntersectsTriangle(InWorldRay, a, b, c, out dist, out point))
                    {
                        Vector3 nrm = Vector3.Cross(b - a, c - a);

                        float dot; // vars used in loop
                        switch (cullingMode)
                        {
                            case CullingMode.Front:
                                dot = Vector3.Dot(InWorldRay.direction, nrm);

                                if (dot > 0f)
                                    goto case CullingMode.FrontBack;
                                break;

                            case CullingMode.Back:
                                dot = Vector3.Dot(InWorldRay.direction, nrm);

                                if (dot < 0f)
                                    goto case CullingMode.FrontBack;
                                break;

                            case CullingMode.FrontBack:
                                hits.Add(new RaycastHit(dist,
                                    InWorldRay.GetPoint(dist),
                                    nrm,
                                    CurFace));
                                break;
                        }

                        continue;
                    }
                }
            }

            return hits.Count > 0;
        }

        /// <summary>
        /// Transform a ray from world space to a transform local space.
        /// </summary>
        /// <param name="transform"></param>
        /// <param name="InWorldRay"></param>
        /// <returns></returns>
        internal static Ray InverseTransformRay(this Transform transform, Ray InWorldRay)
        {
            Vector3 o = InWorldRay.origin;
            o -= transform.position;
            o = transform.worldToLocalMatrix * o;
            Vector3 d = transform.worldToLocalMatrix.MultiplyVector(InWorldRay.direction);
            return new Ray(o, d);
        }

        /// <summary>
        /// Find the nearest triangle intersected by InWorldRay on this mesh.
        /// </summary>
        /// <param name="InWorldRay"></param>
        /// <param name="hit"></param>
        /// <param name="distance"></param>
        /// <returns></returns>
        internal static bool MeshRaycast(Ray InWorldRay, GameObject gameObject, out RaycastHit hit, float distance = Mathf.Infinity)
        {
            var meshFilter = gameObject.GetComponent<MeshFilter>();
            var mesh = meshFilter != null ? meshFilter.sharedMesh : null;

            if (!mesh)
            {
                hit = default(RaycastHit);
                return false;
            }

            var transform = gameObject.transform;
            var ray = transform.InverseTransformRay(InWorldRay);
            return MeshRaycast(ray, mesh.vertices, mesh.triangles, out hit, distance);
        }

        /// <summary>
        /// Cast a ray (in model space) against a mesh.
        /// </summary>
        /// <param name="InRay"></param>
        /// <param name="mesh"></param>
        /// <param name="triangles"></param>
        /// <param name="hit"></param>
        /// <param name="distance"></param>
        /// <returns></returns>
        internal static bool MeshRaycast(Ray InRay, Vector3[] mesh, int[] triangles, out RaycastHit hit, float distance = Mathf.Infinity)
        {
            // float dot;               // vars used in loop
            float hitDistance = Mathf.Infinity;
            Vector3 hitNormal = Vector3.zero;    // vars used in loop
            Vector3 a, b, c, n = Vector3.zero;
            int hitFace = -1;
            Vector3 o = InRay.origin, d = InRay.direction;

            // Iterate faces, testing for nearest hit to ray origin.
            for (int CurTri = 0; CurTri < triangles.Length; CurTri += 3)
            {
                a = mesh[triangles[CurTri + 0]];
                b = mesh[triangles[CurTri + 1]];
                c = mesh[triangles[CurTri + 2]];

                if (Math.RayIntersectsTriangle2(o, d, a, b, c, ref distance, ref n))
                {
                    if(distance < hitDistance)
                    {
                        hitFace = CurTri / 3;
                        hitDistance = distance;
                        hitNormal = n;
                    }
                }
            }

            hit = new RaycastHit(hitDistance,
                    InRay.GetPoint(hitDistance),
                    hitNormal,
                    hitFace);

            return hitFace > -1;
        }

        /// <summary>
        /// Returns true if this point in world space is occluded by a triangle on this object.
        /// </summary>
        /// <remarks>This is very slow, do not use.</remarks>
        /// <param name="cam"></param>
        /// <param name="pb"></param>
        /// <param name="worldPoint"></param>
        /// <returns></returns>
        internal static bool PointIsOccluded(Camera cam, ProBuilderMesh pb, Vector3 worldPoint)
        {
            Vector3 dir = (cam.transform.position - worldPoint).normalized;

            // move the point slightly towards the camera to avoid colliding with its own triangle
            Ray ray = new Ray(worldPoint + dir * .0001f, dir);

            RaycastHit hit;

            return FaceRaycast(ray, pb, out hit, Vector3.Distance(cam.transform.position, worldPoint), CullingMode.Front);
        }

        /// <summary>
        /// Collects coincident vertices and returns a rotation calculated from the average normal and bitangent.
        /// </summary>
        /// <param name="mesh">The target mesh.</param>
        /// <param name="indices">Vertex indices to consider in the rotation calculations.</param>
        /// <returns>A rotation calculated from the average normal of each vertex.</returns>
        public static Quaternion GetRotation(ProBuilderMesh mesh, IEnumerable<int> indices)
        {
            if (!mesh.HasArrays(MeshArrays.Normal))
                Normals.CalculateNormals(mesh);

            if (!mesh.HasArrays(MeshArrays.Tangent))
                Normals.CalculateTangents(mesh);

            var normals = mesh.normalsInternal;
            var tangents = mesh.tangentsInternal;

            var nrm = Vector3.zero;
            var tan = Vector4.zero;
            float count = 0;

            foreach (var index in indices)
            {
                var n = normals[index];
                var t = tangents[index];

                nrm.x += n.x;
                nrm.y += n.y;
                nrm.z += n.z;

                tan.x += t.x;
                tan.y += t.y;
                tan.z += t.z;
                tan.w += t.w;

                count++;
            }

            nrm.x /= count;
            nrm.y /= count;
            nrm.z /= count;

            tan.x /= count;
            tan.y /= count;
            tan.z /= count;
            tan.w /= count;

            if (nrm == Vector3.zero || tan == Vector4.zero)
                return mesh.transform.rotation;

            var bit = Vector3.Cross(nrm, tan * tan.w);

            return mesh.transform.rotation * Quaternion.LookRotation(nrm, bit);
        }

        /// <summary>
        /// Returns a rotation suitable for orienting a handle or gizmo relative to the Face selection.
        /// </summary>
        /// <param name="mesh">The target mesh.</param>
        /// <param name="orientation">The type of <see cref="HandleOrientation"/> to calculate.</param>
        /// <param name="faces">Which faces to consider in the rotation calculations. This is only used when the
        /// <see cref="HandleOrientation"/> is set to <see cref="HandleOrientation.ActiveElement"/>.</param>
        /// <returns>A rotation appropriate to the orientation and element selection.</returns>
        public static Quaternion GetFaceRotation(ProBuilderMesh mesh, HandleOrientation orientation, IEnumerable<Face> faces)
        {
            if (mesh == null)
                return Quaternion.identity;

            switch (orientation)
            {
                case HandleOrientation.ActiveElement:
                    // Intentionally not using coincident vertices here. We want the normal of just the face, not an
                    // average of it's neighbors.
                    return GetFaceRotation(mesh, faces.Last());

                case HandleOrientation.ActiveObject:
                    return mesh.transform.rotation;

                default:
                    return Quaternion.identity;
            }
        }

        /// <summary>
        /// Returns the rotation of a <see cref="Face"/> in world space.
        /// </summary>
        /// <param name="mesh">The mesh that the face belongs to.</param>
        /// <param name="face">The face you want to calculate the rotation for.</param>
        /// <returns>The rotation of the face in world space coordinates.</returns>
        public static Quaternion GetFaceRotation(ProBuilderMesh mesh, Face face)
        {
            if (mesh == null)
                return Quaternion.identity;

            if (face == null)
                return mesh.transform.rotation;

            // Usar uma base geométrica estável (normal + tangente por aresta) para evitar inversões de 180°.
            Vector3 normal = Math.Normal(mesh, face);
            if (normal == Vector3.zero)
                return mesh.transform.rotation;

            var positions = mesh.positionsInternal;
            var distinct = face.distinctIndexes;
            // Calcular a direção principal do plano da face via PCA (covariância 2D),
            // tornando o eixo "tangent" alinhado com o maior alongamento da geometria da face.
            Vector3 tangent = Vector3.zero;
            Vector3 bitangent = Vector3.zero;
            if (distinct != null && distinct.Count >= 2)
            {
                // Base no plano: u e v ortogonais, ambos ortogonais a normal
                Vector3 u = Vector3.ProjectOnPlane(Vector3.right, normal);
                if (u.sqrMagnitude < 1e-6f)
                    u = Vector3.ProjectOnPlane(Vector3.up, normal);
                if (u.sqrMagnitude < 1e-6f)
                    u = Vector3.ProjectOnPlane(Vector3.forward, normal);
                u.Normalize();
                Vector3 v = Vector3.Cross(normal, u).normalized;

                // Centróide
                Vector3 centroid = Vector3.zero;
                for (int i = 0; i < distinct.Count; i++)
                    centroid += positions[distinct[i]];
                centroid /= distinct.Count;

                // Coordenadas 2D centradas
                float sumX = 0f, sumY = 0f;
                var xs = new float[distinct.Count];
                var ys = new float[distinct.Count];
                for (int i = 0; i < distinct.Count; i++)
                {
                    Vector3 r = positions[distinct[i]] - centroid;
                    // projeta no plano
                    Vector3 rproj = r - Vector3.Dot(r, normal) * normal;
                    float x = Vector3.Dot(rproj, u);
                    float y = Vector3.Dot(rproj, v);
                    xs[i] = x; ys[i] = y;
                    sumX += x; sumY += y;
                }
                float meanX = sumX / distinct.Count;
                float meanY = sumY / distinct.Count;

                float a = 0f, b = 0f, c = 0f; // [[a, b], [b, c]]
                for (int i = 0; i < distinct.Count; i++)
                {
                    float dx = xs[i] - meanX;
                    float dy = ys[i] - meanY;
                    a += dx * dx;
                    b += dx * dy;
                    c += dy * dy;
                }
                a /= distinct.Count; b /= distinct.Count; c /= distinct.Count;

                // Maior autovalor
                float trace = a + c;
                float det = a * c - b * b;
                float disc = trace * trace - 4f * det;
                float sqrtDisc = Mathf.Sqrt(Mathf.Max(0f, disc));
                float lambda = 0.5f * (trace + sqrtDisc);

                float px, py;
                if (Mathf.Abs(b) > 1e-8f)
                {
                    px = lambda - c; py = b;
                }
                else
                {
                    if (a >= c) { px = 1f; py = 0f; }
                    else { px = 0f; py = 1f; }
                }

                Vector3 principal = (px * u + py * v);
                if (principal.sqrMagnitude > 1e-12f)
                    tangent = principal.normalized;
            }

            // Se PCA falhar, recorre ao método baseado em UVs
            if (tangent == Vector3.zero)
            {
                Normal nrm = Math.NormalTangentBitangent(mesh, face);
                if (nrm.normal != Vector3.zero && nrm.bitangent != Vector3.zero)
                {
                    normal = nrm.normal;
                    bitangent = nrm.bitangent.normalized;
                }
                else
                {
                    // fallback mínimo: escolha um eixo arbitrário no plano
                    Vector3 refAxis = Mathf.Abs(Vector3.Dot(normal.normalized, Vector3.right)) < 0.9f ? Vector3.right : Vector3.forward;
                    tangent = Vector3.ProjectOnPlane(refAxis, normal).normalized;
                    bitangent = Vector3.Cross(normal, tangent).normalized;
                }
            }

            // Se tangente definida por PCA, obter bitangente coerente
            if (tangent != Vector3.zero)
                bitangent = Vector3.Cross(normal, tangent).normalized;

            // Garantir que bitangent esteja válido; se ainda for zero, crie um fallback seguro
            if (bitangent.sqrMagnitude < 1e-12f)
            {
                Vector3 refAxis = Mathf.Abs(Vector3.Dot(normal.normalized, Vector3.right)) < 0.9f ? Vector3.right : Vector3.forward;
                Vector3 safeTangent = Vector3.ProjectOnPlane(refAxis, normal).normalized;
                bitangent = Vector3.Cross(normal, safeTangent).normalized;
                if (tangent == Vector3.zero)
                    tangent = safeTangent;
            }

            // Bloquear flip de sinal mantendo continuidade do "up" por face.
            int meshIdLock = mesh.GetInstanceID();
            int faceIndexLock = Array.IndexOf(mesh.facesInternal, face);
            if (faceIndexLock >= 0)
            {
                if (!s_LastFaceUp.TryGetValue(meshIdLock, out var dict))
                {
                    dict = new Dictionary<int, Vector3>();
                    s_LastFaceUp[meshIdLock] = dict;
                }
                if (dict.TryGetValue(faceIndexLock, out var lastUp))
                {
                    if (Vector3.Dot(bitangent, lastUp) < 0f)
                    {
                        bitangent = -bitangent;
                        tangent = Vector3.Cross(normal, bitangent).normalized;
                    }
                }
                dict[faceIndexLock] = bitangent;
            }

            // Tie-breaker determinístico: alinhar sinal do up com projeção do world up no plano da face.
            // Evita flips quando não há histórico (ex.: primeira seleção após operações).
            Vector3 worldUpLocal = mesh.transform.InverseTransformDirection(Vector3.up);
            Vector3 refUpPlane = Vector3.ProjectOnPlane(worldUpLocal, normal);
            if (refUpPlane.sqrMagnitude > 1e-6f)
            {
                refUpPlane.Normalize();
                if (Vector3.Dot(bitangent, refUpPlane) < 0f)
                {
                    bitangent = -bitangent;
                    tangent = Vector3.Cross(normal, bitangent).normalized;
                }
            }

            // Orientação em espaço de mundo usando TransformDirection
            // Corrigir normal em escala não uniforme: usar inverse-transpose do localToWorld
            Matrix4x4 l2w = mesh.transform.localToWorldMatrix;
            Matrix4x4 invTrans = l2w.inverse.transpose;
            Vector3 worldNormal = invTrans.MultiplyVector(normal).normalized;

            // Transformar up/tangent para mundo e projetar no plano da face para ortogonalidade
            Vector3 worldUpRaw = l2w.MultiplyVector(bitangent);
            Vector3 worldUp = Vector3.ProjectOnPlane(worldUpRaw, worldNormal);
            if (worldUp.sqrMagnitude < 1e-6f)
            {
                // Fallback quando up quase paralelo à normal após transformações
                Vector3 fallback = Mathf.Abs(Vector3.Dot(worldNormal, Vector3.up)) > 0.99f ? Vector3.right : Vector3.up;
                worldUp = Vector3.ProjectOnPlane(fallback, worldNormal);
            }
            worldUp.Normalize();

            Vector3 worldTangent = Vector3.Cross(worldNormal, worldUp).normalized;
            // Reconstituir up para garantir base ortonormal direita
            worldUp = Vector3.Cross(worldTangent, worldNormal).normalized;

            // Tie-breaker adicional em espaço de mundo para consistência com world up
            Vector3 refUpPlaneW = Vector3.ProjectOnPlane(Vector3.up, worldNormal);
            if (refUpPlaneW.sqrMagnitude > 1e-6f)
            {
                refUpPlaneW.Normalize();
                if (Vector3.Dot(worldUp, refUpPlaneW) < 0f)
                {
                    worldUp = -worldUp;
                    worldTangent = -worldTangent;
                }
            }

            return Quaternion.LookRotation(worldNormal, worldUp);
        }

        /// <summary>
        /// Returns a rotation suitable for orienting a handle or gizmo relative to the Edge selection.
        /// </summary>
        /// <param name="mesh">The target mesh.</param>
        /// <param name="orientation">The type of <see cref="HandleOrientation"/> to calculate.</param>
        /// <param name="edges">Which edges to consider in the rotation calculations. This is only used when the
        /// <see cref="HandleOrientation"/> is set to <see cref="HandleOrientation.ActiveElement"/>.</param>
        /// <returns>A rotation appropriate to the orientation and element selection.</returns>
        public static Quaternion GetEdgeRotation(ProBuilderMesh mesh, HandleOrientation orientation, IEnumerable<Edge> edges)
        {
            if (mesh == null)
                return Quaternion.identity;

            switch (orientation)
            {
                case HandleOrientation.ActiveElement:
                    // Getting an average of the edge normals isn't very helpful in real world uses, so we just use the
                    // first selected edge for orientation.
                    // This function accepts an enumerable because in the future we may want to do something more
                    // sophisticated, and it's convenient because selections are stored as collections.
                    return GetEdgeRotation(mesh, edges.Last());

                case HandleOrientation.ActiveObject:
                    return mesh.transform.rotation;

                default:
                    return Quaternion.identity;
            }
        }

        /// <summary>
        /// Returns the rotation of an <see cref="Edge"/> in world space.
        /// </summary>
        /// <param name="mesh">The mesh that edge belongs to.</param>
        /// <param name="edge">The edge you want to calculate the rotation for.</param>
        /// <returns>The rotation of the edge in world space coordinates.</returns>
        public static Quaternion GetEdgeRotation(ProBuilderMesh mesh, Edge edge)
        {
            if (mesh == null)
                return Quaternion.identity;

            var positions = mesh.positionsInternal;
            if (edge.a < 0 || edge.b < 0 || edge.a >= positions.Length || edge.b >= positions.Length)
                return mesh.transform.rotation;

            // Eixo X: direção da aresta em espaço de mundo
            Vector3 localEdge = positions[edge.b] - positions[edge.a];
            if (localEdge.sqrMagnitude < 1e-12f)
                return mesh.transform.rotation;

            Vector3 worldX = mesh.transform.TransformDirection(localEdge).normalized;

            // Transformação inversa-transposta para normals com escala não uniforme
            Matrix4x4 invTrans = Matrix4x4.Transpose(Matrix4x4.Inverse(mesh.transform.localToWorldMatrix));

            // Somatório dos vetores de plano por face adjacente: Ui = cross(X, Ni_world)
            Vector3 sumUp = Vector3.zero;
            var faces = mesh.facesInternal;
            int foundAdjFaces = 0;
            Vector3 sumNormalsWorld = Vector3.zero; // para definir "fora" pela média das normais
            // Posições dos vértices da aresta (lidas uma vez)
            Vector3 aPos = positions[edge.a];
            Vector3 bPos = positions[edge.b];
            const float eps = 1e-5f; // tolerância para coincidência de posição
            for (int i = 0; i < faces.Length; ++i)
            {
                var f = faces[i];
                // Em muitas malhas ProBuilder, faces adjacentes usam índices distintos para o mesmo vértice coincidente.
                // Então detectamos adjacência por posição, não apenas por índices crus.
                var faceIdx = f.indexesInternal;
                bool hasA = false, hasB = false;
                if (faceIdx != null && faceIdx.Length > 0)
                {
                    for (int k = 0; k < faceIdx.Length; ++k)
                    {
                        Vector3 p = positions[faceIdx[k]];
                        if (!hasA && (p - aPos).sqrMagnitude <= eps * eps) hasA = true;
                        if (!hasB && (p - bPos).sqrMagnitude <= eps * eps) hasB = true;
                        if (hasA && hasB) break;
                    }
                }

                if (!hasA || !hasB)
                {
                    // Como fallback, verificar índices distintos internos se disponível
                    var distinct = f.distinctIndexesInternal;
                    if (distinct != null && distinct.Length > 0)
                    {
                        for (int k = 0; k < distinct.Length && !(hasA && hasB); ++k)
                        {
                            Vector3 p = positions[distinct[k]];
                            if (!hasA && (p - aPos).sqrMagnitude <= eps * eps) hasA = true;
                            if (!hasB && (p - bPos).sqrMagnitude <= eps * eps) hasB = true;
                        }
                    }
                }

                if (!hasA || !hasB)
                    continue; // face não adjacente à aresta

                Vector3 nLocal = Math.Normal(mesh, f);
                if (nLocal.sqrMagnitude < 1e-12f)
                    continue;

                Vector3 nWorld = invTrans.MultiplyVector(nLocal).normalized;
                sumNormalsWorld += nWorld;
                Vector3 ui = Vector3.Cross(worldX, nWorld);
                // Acumular ponderado pela magnitude (sin do ângulo entre X e N)
                sumUp += ui;
                foundAdjFaces++;
                if (foundAdjFaces >= 2)
                    break; // apenas duas faces devem ser adjacentes a uma aresta em malhas manifold
            }

            // Se o somatório for muito pequeno, usar fallback estável com world up projetado
            Vector3 worldUp;
            if (sumUp.sqrMagnitude < 1e-6f)
            {
                Vector3 fallbackUp = Vector3.ProjectOnPlane(Vector3.up, worldX);
                if (fallbackUp.sqrMagnitude < 1e-6f)
                    fallbackUp = Vector3.ProjectOnPlane(Vector3.right, worldX);
                worldUp = fallbackUp.normalized;
            }
            else
            {
                worldUp = sumUp.normalized;
            }

            // Z é perpendicular ao plano definido por X e Y (bissetor)
            Vector3 worldZ = Vector3.Cross(worldX, worldUp).normalized;
            worldUp = Vector3.Cross(worldZ, worldX).normalized; // ortonormalizar Y e garantir base dextra

            // Desempate determinístico com world up
            Vector3 refUp = Vector3.ProjectOnPlane(Vector3.up, worldX);
            if (refUp.sqrMagnitude > 1e-6f)
            {
                refUp.Normalize();
                if (Vector3.Dot(worldUp, refUp) < 0f)
                {
                    worldUp = -worldUp;
                    worldZ = -worldZ;
                }
            }

            // Garantir que +Y aponte para fora (média das normais das faces adjacentes)
            if (sumNormalsWorld.sqrMagnitude > 1e-6f)
            {
                Vector3 outward = sumNormalsWorld.normalized;
                // Y é worldZ (LookRotation(forward=worldUp, up=worldZ)). Se estiver apontando para dentro, inverta Y (e forward para preservar X).
                if (Vector3.Dot(worldZ, outward) < 0f)
                {
                    worldZ = -worldZ;
                    worldUp = -worldUp;
                }
            }

            // Mapeamento de eixos: Y (up) deve ser o normal/bissetor e Z (forward) o lateral
            return Quaternion.LookRotation(worldUp, worldZ);
        }

        /// <summary>
        /// Returns a rotation suitable for orienting a handle or gizmo relative to the Vertex selection.
        /// </summary>
        /// <param name="mesh">The target mesh.</param>
        /// <param name="orientation">The type of <see cref="HandleOrientation"/> to calculate.</param>
        /// <param name="vertices">Array of <see cref="Vertex"/> indices pointing to the vertices to consider in the rotation calculations. This is only used when the
        /// <see cref="HandleOrientation"/> is set to <see cref="HandleOrientation.ActiveElement"/>.</param>
        /// <returns>A rotation appropriate to the orientation and element selection.</returns>
        public static Quaternion GetVertexRotation(ProBuilderMesh mesh, HandleOrientation orientation, IEnumerable<int> vertices)
        {
            if (mesh == null)
                return Quaternion.identity;

            switch (orientation)
            {
                case HandleOrientation.ActiveElement:
                    if (mesh.selectedVertexCount < 1)
                        goto case HandleOrientation.ActiveObject;
                    return GetRotation(mesh, vertices);

                case HandleOrientation.ActiveObject:
                    return mesh.transform.rotation;

                default:
                    return Quaternion.identity;
            }
        }

        /// <summary>
        /// Get the rotation of a vertex in world space.
        /// </summary>
        /// <param name="mesh">The mesh that the vertex belongs to.</param>
        /// <param name="vertex">The index that points to the vertex to calculate the rotation for.</param>
        /// <returns>The rotation of a vertex normal in world space coordinates.</returns>
        public static Quaternion GetVertexRotation(ProBuilderMesh mesh, int vertex)
        {
            if (mesh == null)
                return Quaternion.identity;

            if (vertex < 0)
                return mesh.transform.rotation;

            return GetRotation(mesh, new int[] { vertex });
        }

        internal static Vector3 GetActiveElementPosition(ProBuilderMesh mesh, IEnumerable<Face> faces)
        {
            return mesh.transform.TransformPoint(Math.GetBounds(mesh.positionsInternal, faces.Last().distinctIndexesInternal).center);
        }

        internal static Vector3 GetActiveElementPosition(ProBuilderMesh mesh, IEnumerable<Edge> edges)
        {
            var edge = edges.Last();
            return mesh.transform.TransformPoint(Math.GetBounds(mesh.positionsInternal, new int[] { edge.a, edge.b }).center);
        }

        internal static Vector3 GetActiveElementPosition(ProBuilderMesh mesh, IEnumerable<int> vertices)
        {
            return mesh.transform.TransformPoint(mesh.positionsInternal[vertices.First()]);
        }
    }
}
