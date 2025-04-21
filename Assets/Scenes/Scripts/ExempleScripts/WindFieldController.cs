using UnityEngine;

public class WindFieldController : MonoBehaviour
{
    public enum Polaridade { Atrair, Repelem }
    public Polaridade polaridadeAtual = Polaridade.Atrair;

    public float forca = 20f;
    public float raio = 10f;
    public LayerMask layerAfetada;

    public KeyCode mudarPolaridade = KeyCode.Q;

    private void Update()
    {
        // Troca de polaridade manual (pode ser substituído por animação/habilidade do Saci)
        if (Input.GetKeyDown(mudarPolaridade))
        {
            polaridadeAtual = (polaridadeAtual == Polaridade.Atrair) ? Polaridade.Repelem : Polaridade.Atrair;
        }

        AplicarForca();
    }

    private void AplicarForca()
    {
        Collider[] alvos = Physics.OverlapSphere(transform.position, raio, layerAfetada);

        foreach (var alvo in alvos)
        {
            WindReceiver receiver = alvo.GetComponent<WindReceiver>();
            if (receiver != null)
            {
                Vector3 direcao = (alvo.transform.position - transform.position).normalized;

                if (polaridadeAtual == Polaridade.Atrair)
                    direcao *= -1;

                receiver.AplicarVento(direcao * forca);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = (polaridadeAtual == Polaridade.Atrair) ? Color.blue : Color.red;
        Gizmos.DrawWireSphere(transform.position, raio);
    }
}
