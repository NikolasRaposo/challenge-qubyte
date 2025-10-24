using UnityEngine;

namespace CollisionBear.WorldEditor.Extensions
{
    public static class Vector3Extension
    {
        public static float DirectionToRotationY(this Vector3 vector)
        {
            vector.Normalize();
            return Mathf.Repeat(-Mathf.Atan2(vector.z, vector.x) * Mathf.Rad2Deg - 90, 360);
        }

        public static float DirectionToPerpendicularRotationY(this Vector3 vector)
        {
            vector.Normalize();
            return (-Mathf.Atan2(vector.z, vector.x) * Mathf.Rad2Deg + 90);
        }

        public static Vector3 DirectionToEuler(this Vector3 vector) => Quaternion.LookRotation(vector, Vector3.up).eulerAngles;
    }
}