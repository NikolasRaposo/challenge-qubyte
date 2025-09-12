using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class WindReceiver : MonoBehaviour
{
    public float sensibilidade = 1f;
    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void AplicarVento(Vector3 direcaoForca)
    {
        rb.AddForce(direcaoForca * sensibilidade, ForceMode.Acceleration);
    }
}
