using UnityEngine;

/// <summary>
/// Interface para objetos que podem fornecer informações de velocidade para o player.
/// Implementada por plataformas móveis para comunicar sua velocidade ao sistema de momentum.
/// </summary>
public interface IPlatformVelocityProvider
{
    /// <summary>
    /// Retorna a velocidade atual da plataforma em world space.
    /// </summary>
    /// <returns>Velocidade da plataforma em m/s</returns>
    Vector3 GetPlatformVelocity();
    
    /// <summary>
    /// Indica se a plataforma está se movendo atualmente.
    /// </summary>
    /// <returns>True se a plataforma está em movimento</returns>
    bool IsMoving();
}