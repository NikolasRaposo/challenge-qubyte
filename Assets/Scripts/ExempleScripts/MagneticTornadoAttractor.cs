using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MagneticTornadoController : MonoBehaviour
{
    public enum Polaridade { Atrair, Repelem }
    public Polaridade polaridadeAtual = Polaridade.Atrair;

    public float raioInicial = 5f;
    public float raioFinal = 0.5f;
    public float velocidadeAngular = 360f;
    public float forcaDeArremesso = 15f;

    [SerializeField] private float anguloMinimo = -30f; // Esquerda
    [SerializeField] private float anguloMaximo = 0f;   // Frente

    public KeyCode mudarPolaridade = KeyCode.Q;
    public KeyCode ativarAcaoFinal = KeyCode.E;

    private bool acaoFinalAtivada = false;
    private List<Rigidbody> objetosAfetados = new List<Rigidbody>();
    private Dictionary<Rigidbody, Coroutine> orbitando = new Dictionary<Rigidbody, Coroutine>();
    private HashSet<Rigidbody> objetosDentroDoTornado = new HashSet<Rigidbody>();

    private void Update()
    {
        if (Input.GetKeyDown(mudarPolaridade))
        {
            polaridadeAtual = (polaridadeAtual == Polaridade.Atrair) ? Polaridade.Repelem : Polaridade.Atrair;
        }

        if (Input.GetKeyDown(ativarAcaoFinal))
        {
            acaoFinalAtivada = true;
        }
    }

    private void LateUpdate()
    {
        acaoFinalAtivada = false; // Reseta após um frame
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.attachedRigidbody != null && other.CompareTag("Magnetico"))
        {
            Rigidbody rb = other.attachedRigidbody;

            if (!objetosAfetados.Contains(rb))
            {
                objetosAfetados.Add(rb);
                objetosDentroDoTornado.Add(rb);

                if (polaridadeAtual == Polaridade.Atrair)
                {
                    Coroutine c = StartCoroutine(OrbitarObjeto(rb));
                    orbitando[rb] = c;
                }
                else // Repelem
                {
                    Vector3 dir = (rb.position - transform.position).normalized;
                    rb.velocity = dir * forcaDeArremesso;
                }
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.attachedRigidbody != null && objetosDentroDoTornado.Contains(other.attachedRigidbody))
        {
            Rigidbody rb = other.attachedRigidbody;

            objetosDentroDoTornado.Remove(rb);
            objetosAfetados.Remove(rb);

            if (orbitando.ContainsKey(rb))
            {
                StopCoroutine(orbitando[rb]);
                orbitando.Remove(rb);
            }

            rb.useGravity = true;
        }
    }

    private IEnumerator OrbitarObjeto(Rigidbody rb)
    {
        rb.useGravity = false;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        float anguloAtual = Random.Range(0f, 360f);
        float t = 0f;

        while (true)
        {
            if (rb == null || polaridadeAtual != Polaridade.Atrair)
                break;

            if (!objetosDentroDoTornado.Contains(rb))
        {
            break; // Sai da órbita se saiu do trigger
        }

            if (acaoFinalAtivada)
            {
                Vector3 direcaoAoObjeto = (rb.position - transform.position).normalized;
                float angulo = Vector3.SignedAngle(transform.forward, direcaoAoObjeto, Vector3.up);

                if (angulo >= anguloMinimo && angulo <= anguloMaximo)
                {
                    Vector3 direcaoFinal = transform.forward.normalized;
                    rb.velocity = direcaoFinal * forcaDeArremesso;
                    rb.useGravity = true;

                    objetosAfetados.Remove(rb);
                    orbitando.Remove(rb);
                    yield break;
                }
            }

            t += Time.deltaTime;
            anguloAtual += velocidadeAngular * Time.deltaTime;

            float progresso = Mathf.PingPong(t, 1f);
            float raio = Mathf.Lerp(raioInicial, raioFinal, progresso);
            float rad = anguloAtual * Mathf.Deg2Rad;

            Vector3 offset = new Vector3(Mathf.Cos(rad), 0, Mathf.Sin(rad)) * raio;
            Vector3 posFinal = transform.position + offset;

            posFinal.y = Mathf.Lerp(rb.position.y, transform.position.y, Time.deltaTime * 5f);
            rb.MovePosition(posFinal);

            yield return null;
        }

        // Sai da órbita por mudança de polaridade ou null
        rb.useGravity = true;
        objetosAfetados.Remove(rb);
        orbitando.Remove(rb);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = (polaridadeAtual == Polaridade.Atrair) ? Color.cyan : Color.magenta;
        Gizmos.DrawWireSphere(transform.position, raioInicial);
    }
}
