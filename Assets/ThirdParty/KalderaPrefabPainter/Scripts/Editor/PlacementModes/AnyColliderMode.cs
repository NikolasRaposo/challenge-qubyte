using System.Linq;
using UnityEngine;

namespace CollisionBear.WorldEditor
{
    [System.Serializable]
    public class AnyColliderMode : IPlacementMode
    {
        public string Name => "Any collider";
        public GameObject ParentObject => null;

        public string HintText => "The brush will place objects against any collider already existing in the scene. This includes objects previously painted with the prefab painter.";

        public RaycastHit? IsValidPlacement(RaycastHit[] raycastHits, int hitCount, PlacementCollection placementCollection)
        {
            if (hitCount == 0) {
                return null;
            }

            return raycastHits
                .Take(hitCount)
                .Where(h => !h.collider.isTrigger)
                .OrderBy(h => h.distance)
                .FirstOrDefault();
        }

        public string ValidatePlacementMode()
        {
            return null;
        }

        public void DrawEditor(PaletteWindow paletteWindow) { }

        public bool GameObjectInPlacement(GameObject gameObject) => true;
    }
}