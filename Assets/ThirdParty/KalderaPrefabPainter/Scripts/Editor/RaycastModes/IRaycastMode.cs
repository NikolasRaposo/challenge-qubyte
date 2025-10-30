using UnityEngine;

namespace CollisionBear.WorldEditor.RaycastModes
{
    public interface IRaycastMode
    {
        string Name { get; }
        string HintText { get; }
        Vector3 GetNormal(Vector3 position);
        Ray GetRay(Vector3 position);
        Vector3 GetDelta(Vector3 startPosition, Vector3 endPosition);
        Vector3 GetRotationDirection(Vector3 startPosition, Vector3 endPosition);
        float GetMaxDistance();

        void DrawEditor(PaletteWindow paletteWindow);

        Vector3 IndividualHeight(Vector3 result, Vector3 raycasyHit, Vector3 offset);
    }
}