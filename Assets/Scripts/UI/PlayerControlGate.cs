using UnityEngine;
using UnityEngine.Events;
using Player; // para ECMSaciController
using ECM.Controllers; // para BaseCharacterController
using ECM.Components; // para CharacterMovement
using Qubyte.Tracking;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody), typeof(ECMSaciController))]
public class PlayerControlGate : MonoBehaviour
{
    // Campos internos: detectados automaticamente no MESMO GameObject
    private Rigidbody playerRb;
    private BaseCharacterController playerController;
    private CharacterMovement movement;
    private ECMSaciController saci;

    [Header("Eventos")]
    public UnityEvent OnPhysicsFrozen;
    public UnityEvent OnPhysicsUnfrozen;
    public UnityEvent OnControllerDisabled;
    public UnityEvent OnControllerEnabled;

    private void Awake()
    {
        // Autodetecção local (sem procurar em pais/filhos)
        playerRb = GetComponent<Rigidbody>();
        playerController = GetComponent<BaseCharacterController>();
        movement = GetComponent<CharacterMovement>();
        saci = GetComponent<ECMSaciController>();
    }

    [TrackableCall]
    public void FreezePhysics()
    {
        if (playerRb == null) return;
        playerRb.linearVelocity = Vector3.zero;
        playerRb.angularVelocity = Vector3.zero;
        playerRb.isKinematic = true;
        OnPhysicsFrozen?.Invoke();
    }

    [TrackableCall]
    public void UnfreezePhysics()
    {
        if (playerRb == null) return;
        playerRb.isKinematic = false;
        OnPhysicsUnfrozen?.Invoke();
    }

    [TrackableCall]
    public void DisableController()
    {
        if (playerController == null) return;
        playerController.enabled = false;
        OnControllerDisabled?.Invoke();
    }

    [TrackableCall]
    public void EnableController()
    {
        if (playerController == null) return;
        playerController.enabled = true;
        OnControllerEnabled?.Invoke();
    }

    // ----- Centralização de pausa ECM -----
    [TrackableCall]
    public void PauseECM(bool restoreVelocityOnResume = false)
    {
        // Sinaliza pausa no controlador ECM
        if (playerController != null)
        {
            playerController.restoreVelocityOnResume = restoreVelocityOnResume;
            playerController.pause = true;
        }

        // Pausa movimentação imediatamente (torna RB kinematic internamente)
        if (movement != null)
        {
            movement.Pause(true, restoreVelocity: false);
        }
    }

    [TrackableCall]
    public void ResumeECM(bool restoreVelocityOnResume = false)
    {
        // Retoma movimentação
        if (movement != null)
        {
            movement.Pause(false, restoreVelocityOnResume);
        }

        // Sinaliza retomada no controlador ECM
        if (playerController != null)
        {
            playerController.restoreVelocityOnResume = restoreVelocityOnResume;
            playerController.pause = false;
        }
    }

    [TrackableCall]
    public void FreezePhysicsAndPauseECM(bool restoreVelocityOnResume = false)
    {
        FreezePhysics();
        PauseECM(restoreVelocityOnResume);
    }

    [TrackableCall]
    public void UnfreezePhysicsAndResumeECM(bool restoreVelocityOnResume = false)
    {
        ResumeECM(restoreVelocityOnResume);
        UnfreezePhysics();
    }

    // ----- Helpers de estado de movimento -----
    [TrackableCall]
    public void ResetVelocity()
    {
        // Zera velocidade em ambos os níveis (ECM e RB)
        if (movement != null)
            movement.velocity = Vector3.zero;
        if (playerRb != null)
        {
            playerRb.linearVelocity = Vector3.zero;
            playerRb.angularVelocity = Vector3.zero;
        }
    }

    [TrackableCall]
    public void DisableGroundingOnce()
    {
        if (movement != null)
            movement.DisableGrounding();
    }

    [TrackableCall]
    public void ClearJumpBuffer()
    {
        if (saci != null)
            saci.ClearJumpBufferAndConsumeInput();
    }

    // ----- Entra/Sai do modo spline através do Gate -----
    [TrackableCall]
    public void EnterSplineMode()
    {
        if (saci != null)
            saci.EnterSplinePathMode();
    }

    [TrackableCall]
    public void ExitSplineMode()
    {
        if (saci != null)
            saci.ExitSplinePathMode();
    }
}
