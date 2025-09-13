using UnityEngine;
namespace Platform {
    /// <summary>
    /// An interface for objects that can provide velocity information to the player.
    /// Implemented by moving platforms to communicate their speed to the player's momentum system.
    /// </summary>
    public interface IPlatformVelocityProvider {
        /// <summary>
        /// Returns the current velocity of the platform in world space.
        /// </summary>
        /// <returns>The platform's velocity in meters per second.</returns>
        Vector3 GetPlatformVelocity();
        /// <summary>
        /// Indicates whether the platform is currently in motion.
        /// </summary>
        /// <returns>True if the platform is moving, otherwise false.</returns>
        bool IsMoving();
    }
}
