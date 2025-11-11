using UnityEngine;
using UnityEngine.Events;
using Player; // para ECMSaciController
using ECM.Controllers; // para BaseCharacterController

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody), typeof(ECMSaciController))]
public class PlayerControlGate : MonoBehaviour
{
    // Campos internos: detectados automaticamente no MESMO GameObject
    private Rigidbody playerRb;
    private BaseCharacterController playerController;

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
    }

    public void FreezePhysics()
    {
        if (playerRb == null) return;
        playerRb.linearVelocity = Vector3.zero;
        playerRb.angularVelocity = Vector3.zero;
        playerRb.isKinematic = true;
        OnPhysicsFrozen?.Invoke();
    }

    public void UnfreezePhysics()
    {
        if (playerRb == null) return;
        playerRb.isKinematic = false;
        OnPhysicsUnfrozen?.Invoke();
    }

    public void DisableController()
    {
        if (playerController == null) return;
        playerController.enabled = false;
        OnControllerDisabled?.Invoke();
    }

    public void EnableController()
    {
        if (playerController == null) return;
        playerController.enabled = true;
        OnControllerEnabled?.Invoke();
    }
}