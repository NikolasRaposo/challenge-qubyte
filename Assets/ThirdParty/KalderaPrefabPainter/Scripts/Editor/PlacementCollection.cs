using System.Collections.Generic;
using UnityEngine;

namespace CollisionBear.WorldEditor
{
    [System.Serializable]
    public class PlacementCollection
    {
        public List<PlacementInformation> Placements = new List<PlacementInformation>();

        public bool HasItems() => Placements.Count > 0;

        public void RotateTowardsPosition(Vector3 position)
        {
            foreach (var placementInformation in Placements) {
                placementInformation.RotateTowardsPosition(position);
            }
        }

        public void RotatePlacement(Vector3 rotation, Vector3 normal, ScenePlacer placer)
        {
            var normalRotation = placer.CurrentBrush.UseNormalRotation ? Quaternion.LookRotation(normal) * Quaternion.Euler(-90, 0, 0) : Quaternion.identity;
            var quaternion = rotation.sqrMagnitude == 0 ? Quaternion.identity : Quaternion.LookRotation(rotation);

            var offsetQuaternion = normalRotation * quaternion;

            foreach (var placement in Placements) {
                placement.SetNormalizedOffset(offsetQuaternion * placement.FixedOffset);
                placement.Rotation = quaternion * placement.NormalizedRotation;
            }
        }

        public void Hide()
        {
            foreach (var item in Placements) {
                if (item.GameObject == null) {
                    continue;
                }

                item.SetHidden(true);
            }
        }

        public void Show()
        {
            foreach (var item in Placements) {
                if (item.GameObject == null) {
                    continue;
                }

                item.SetHidden(false);
            }
        }
    }
}