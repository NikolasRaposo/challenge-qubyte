using CollisionBear.WorldEditor.Utils;
using System.Collections.Generic;
using UnityEngine;

namespace CollisionBear.WorldEditor
{
    public abstract class PlacementColliderBase {
        public abstract void DrawCollider(float sizeFactor);
        public abstract int IsColliding(float sizeFactor, RaycastHit[] result);

        protected Vector3 MultiplyVector(Vector3 a, Vector3 b) => new Vector3( a.x * b.x, a.y * b.y, a.z * b.z );
    }

    public class BoxPlacementCollider : PlacementColliderBase {
        private BoxCollider Collider;

        public BoxPlacementCollider(BoxCollider boxCollider) {
            Collider = boxCollider;
        }

        public override void DrawCollider(float sizeFactor) {
            DrawUtilities.DrawSceneViewMesh(KalderaEditorUtils.CubeMesh, KalderaEditorUtils.PreviewMaterial, GetPosition(sizeFactor), Collider.gameObject.transform.rotation, GetScale(sizeFactor));
        }

        public override int IsColliding(float sizeFactor, RaycastHit[] result) {
            return Physics.BoxCastNonAlloc(GetPosition(sizeFactor), GetScale(sizeFactor) / 2f, Vector3.up, result, GetRotation(), float.MaxValue, int.MaxValue, QueryTriggerInteraction.Ignore);
        }

        private Vector3 GetPosition(float sizeFactor) => Collider.gameObject.transform.position + MultiplyVector(Collider.center, Collider.gameObject.transform.lossyScale * sizeFactor);
        private Quaternion GetRotation() => Collider.gameObject.transform.rotation;
        private Vector3 GetScale(float sizeFactor) => MultiplyVector(Collider.size, Collider.gameObject.transform.lossyScale * sizeFactor);
    }

    public class CapsulePlacementCollider : PlacementColliderBase {
        private CapsuleCollider Collider;

        public CapsulePlacementCollider(CapsuleCollider capsuleCollider) {
            Collider = capsuleCollider;
        }

        public override void DrawCollider(float sizeFactor) {
            var gameObject = Collider.gameObject;
            var position = GetBasePosition(sizeFactor);
            var rotation = GetRotation();

            DrawUtilities.DrawSceneViewMesh(KalderaEditorUtils.CylinderMesh, KalderaEditorUtils.PreviewMaterial, position, gameObject.transform.rotation, GetScale(sizeFactor));

            var capSize = GetCapSize(sizeFactor);

            DrawUtilities.DrawSceneViewMesh(KalderaEditorUtils.HalfSphereMesh, KalderaEditorUtils.PreviewMaterial, GetTopPosition(sizeFactor), rotation, capSize);
            DrawUtilities.DrawSceneViewMesh(KalderaEditorUtils.HalfSphereMesh, KalderaEditorUtils.PreviewMaterial, GetBottomPosition(sizeFactor), rotation * Quaternion.Euler(0, 0, 180), capSize);
        }

        public override int IsColliding(float sizeFactor, RaycastHit[] result) {
            return Physics.CapsuleCastNonAlloc(GetTopPosition(sizeFactor), GetBottomPosition(sizeFactor), Collider.radius * sizeFactor, Vector3.up, result, float.MaxValue, int.MaxValue, QueryTriggerInteraction.Ignore);
        }

        private float GetHeight() => Collider.height - Collider.radius * 2;
        private Vector3 GetBasePosition(float sizeFactor) => Collider.gameObject.transform.position + MultiplyVector(Collider.transform.rotation * Collider.center, Collider.gameObject.transform.lossyScale * sizeFactor);
        private Vector3 GetOffset() => GetRotation() * Vector3.up * ((GetHeight() * Collider.gameObject.transform.lossyScale.y) / 2);

        private Quaternion GetRotation() => Collider.gameObject.transform.rotation;
        private Vector3 GetScale(float sizeFactor) => MultiplyVector(new Vector3(Collider.radius * 2, GetHeight(), Collider.radius * 2), Collider.gameObject.transform.lossyScale * sizeFactor);
        private Vector3 GetCapSize(float sizeFactor) => MultiplyVector(new Vector3(Collider.radius * 2, Collider.radius * 2, Collider.radius * 2), Collider.gameObject.transform.lossyScale * sizeFactor);
        private Vector3 GetTopPosition(float sizeFactor) => GetBasePosition(sizeFactor) + GetOffset() * sizeFactor;
        private Vector3 GetBottomPosition(float sizeFactor) => GetBasePosition(sizeFactor) - GetOffset() * sizeFactor;
    }

    public class SpherePlacementCollider : PlacementColliderBase {
        private SphereCollider Collider;

        public SpherePlacementCollider(SphereCollider sphereCollider) {
            Collider = sphereCollider;
        }

        public override void DrawCollider(float sizeFactor) {
            var position = GetPosition(sizeFactor);
            var size = GetScale(sizeFactor);

            DrawUtilities.DrawSceneViewMesh(KalderaEditorUtils.HalfSphereMesh, KalderaEditorUtils.PreviewMaterial, position, GetRotation(), size);
            DrawUtilities.DrawSceneViewMesh(KalderaEditorUtils.HalfSphereMesh, KalderaEditorUtils.PreviewMaterial, position, GetRotation() * Quaternion.Euler(0, 0, 180), size);
        }

        public override int IsColliding(float sizeFactor, RaycastHit[] result) {
            var scale = GetScale(sizeFactor);
            var radius = Mathf.Max(scale.x, scale.y, scale.y);
            return Physics.SphereCastNonAlloc(GetPosition(sizeFactor), Collider.radius * radius, Vector3.up, result, float.MaxValue, int.MaxValue, QueryTriggerInteraction.Ignore);
        }

        private Vector3 GetPosition(float sizeFactor) => Collider.gameObject.transform.position + MultiplyVector(Collider.center, Collider.gameObject.transform.lossyScale * sizeFactor);
        private Quaternion GetRotation() => Collider.gameObject.transform.rotation;
        private Vector3 GetScale(float sizeFactor) => MultiplyVector(Vector3.one * Collider.radius * 2, Collider.gameObject.transform.lossyScale * sizeFactor);
    }

    public class MeshPlacementCollider : PlacementColliderBase {
        private MeshCollider Collider;

        public MeshPlacementCollider(MeshCollider meshCollider) {
            Collider = meshCollider;
        }

        public override void DrawCollider(float sizeFactor) {
            DrawUtilities.DrawSceneViewMesh(KalderaEditorUtils.CubeMesh, KalderaEditorUtils.PreviewMaterial, GetPosition(sizeFactor), GetRotation(), GetScale(sizeFactor));
        }

        public override int IsColliding(float sizeFactor, RaycastHit[] result) {
            return Physics.BoxCastNonAlloc(GetPosition(sizeFactor), (GetMeshBounds().size * sizeFactor) / 2f, Vector3.up, result, GetRotation(), float.MaxValue, int.MaxValue, QueryTriggerInteraction.Ignore);
        }

        private Vector3 GetPosition(float sizeFactor) => Collider.gameObject.transform.position + MultiplyVector(GetMeshBounds().center, Collider.gameObject.transform.lossyScale * sizeFactor);
        private Quaternion GetRotation () => Collider.gameObject.transform.rotation;
        private Vector3 GetScale(float sizeFactor) => MultiplyVector(GetMeshBounds().size, Collider.gameObject.transform.lossyScale);
        private Bounds GetMeshBounds() => Collider.sharedMesh.bounds;
    }

    [System.Serializable]
    public class PlacementInformation
    {
        public PaletteItem Item;
        public GameObject PrefabObject;
        public GameObject GameObject;
        public Vector3 Offset;
        public Quaternion Rotation;
        public float ScaleFactor;

        public Vector3 FixedOffset;
        public Vector3 NormalizedOffset;
        public Vector3 NormalizedHeightOffset;
        public Quaternion NormalizedRotation;
        public Vector3 NormalizedScale;

        public List<PlacementColliderBase> Colliders = new List<PlacementColliderBase>();

        public GameObject GroundGameObject;

        // Hide states
        private bool PlacedOutsideWorld;
        private bool InactiveCollision;
        private bool Hidden;

        public PlacementInformation(PaletteItem item, GameObject prefab, Vector3 offset, Vector3 rotationEuler, Vector3 scale, float scaleFactor)
        {
            Item = item;
            PrefabObject = prefab;
            FixedOffset = offset;
            NormalizedOffset = offset;
            Offset = offset;

            Rotation = Quaternion.Euler(rotationEuler);
            NormalizedRotation = Rotation;
            NormalizedScale = scale;
            ScaleFactor = scaleFactor;

            if (Item.AdvancedOptions.UsePrefabHeight) {
                var height = PrefabObject.transform.localPosition.y * ScaleFactor;
                NormalizedHeightOffset.y = height;
            }
        }

        public void RotateTowardsPosition(Vector3 position)
        {
            if (GameObject == null) {
                return;
            }

            position.y = GameObject.transform.position.y;
            GameObject.transform.LookAt(position);
            GameObject.transform.rotation *= Quaternion.Euler(Item.AdvancedOptions.RotationOffset);
            Rotation = GameObject.transform.rotation;
        }

        public void SetRotation(Vector3 eulerRotation)
        {
            SetRotation(Quaternion.Euler(eulerRotation));
        }

        public void SetRotation(Quaternion rotation)
        {
            Rotation = rotation;

            if (GameObject == null) {
                return;
            }

            GameObject.transform.rotation = Rotation;
        }

        public GameObject CreatePlacementGameObject(Vector3 position, float scaleFactor)
        {
            if (PrefabObject == null) {
                return null;
            }

            var result = GameObject.Instantiate(PrefabObject, Vector3.zero, Rotation);
            result.hideFlags = HideFlags.HideAndDontSave;

            SetSaleFactor(scaleFactor);

            result.transform.localScale = NormalizedScale * scaleFactor;
            result.transform.position = position + Offset;
            ScaleFactor = scaleFactor;

            result.name = PrefabObject.name;
            result.SetActive(false);

            // Moving static object occasionally causes horrible performance 
            SetNonStaticRecursiveAndDisableCollider(result);

            Colliders = GetColliders(result);

            GameObject = result;
            return result;
        }

        public static List<PlacementColliderBase> GetColliders(GameObject gameObject) {
            var result = new List<PlacementColliderBase>();

            var colliders = gameObject.GetComponentsInChildren<Collider>();
            foreach (var collider in colliders) {
                if (collider is BoxCollider boxCollider) {
                    result.Add(new BoxPlacementCollider(boxCollider));
                } else if (collider is CapsuleCollider capsuleCollider) {
                    result.Add(new CapsulePlacementCollider(capsuleCollider));
                } else if (collider is SphereCollider sphereCollider) {
                    result.Add(new SpherePlacementCollider(sphereCollider));
                } else if (collider is MeshCollider meshCollider) {
                    result.Add(new MeshPlacementCollider(meshCollider));
                }
            }

            return result;
        }

        public bool HasColliders() => Colliders.Count > 0;

        public void ClearPlacementGameObject()
        {
            Colliders.Clear();
            if (GameObject == null) {
                return;
            }

            GameObject.DestroyImmediate(GameObject);
        }

        public void ReplacePlacementObject(int newVariantIndex, Vector3 position, float scaleFactor)
        {
            ClearPlacementGameObject();
            var validObjects = Item.ValidObjects();
            PrefabObject = validObjects[newVariantIndex];
            CreatePlacementGameObject(position, scaleFactor);
        }

        public void SetNonStaticRecursiveAndDisableCollider(GameObject gameObject)
        {
            gameObject.isStatic = false;
            foreach (var collider in gameObject.GetComponents<Collider>()) {
                collider.enabled = false;
            }

            foreach (Transform child in gameObject.transform) {
                SetNonStaticRecursiveAndDisableCollider(child.gameObject);
            }
        }

        public void SetColliderStatus(bool enabled) {
            GameObject.SetActive(enabled);
            foreach (var collider in GameObject.GetComponentsInChildren<Collider>()) {
                collider.enabled = enabled;
            }
        }

        public void SetNormalizedOffset(Vector3 offset)
        {
            NormalizedOffset = offset + NormalizedHeightOffset * ScaleFactor;
        }

        public void SetSaleFactor(float scaleFactor)
        {
            ScaleFactor = scaleFactor;
            Offset = NormalizedOffset + NormalizedHeightOffset * scaleFactor;
        }

        public void SetPlaceOutsideWorld(bool value) {
            PlacedOutsideWorld = value;

            GameObject.SetActive(ShouldSetActive());
        }

        public void SetCollision(bool value) {
            InactiveCollision = value;

            GameObject.SetActive(ShouldSetActive());
        }

        public void SetHidden(bool value) {
            Hidden = value;

            GameObject.SetActive(ShouldSetActive());
        }

        public bool IsColliding() => InactiveCollision;

        private bool ShouldSetActive() => !PlacedOutsideWorld && !InactiveCollision && !Hidden;

        public float GetCollisionSizeFactor(ScenePlacer scenePlacer) => Item.AdvancedOptions.SpacingFactor * scenePlacer.SelectionSettings.CollisionSizeFactor;
    }
}