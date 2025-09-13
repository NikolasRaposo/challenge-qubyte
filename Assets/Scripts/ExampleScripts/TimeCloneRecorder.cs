using System.Collections.Generic;
using UnityEngine;

public class TimeCloneRecorder : MonoBehaviour
{
    [Header("Configurações de Gravação")]
    public float duracaoGravacao = 5f;
    public GameObject prefabClone;

    private List<PlayerInputFrame> inputGravado = new();
    private bool gravando = true;
    private float tempoAtual = 0f;

    void Update()
    {
        if (gravando)
        {
            GravarInput();
        }

        if (Input.GetKeyDown(KeyCode.C)) // Botão para criar o clone
        {
            CriarClone();
        }
    }

    void GravarInput()
    {
        tempoAtual += Time.deltaTime;
        if (tempoAtual > duracaoGravacao)
        {
            inputGravado.RemoveAt(0); // remove o mais antigo
        }

        // Exemplo de gravação básica de movimento e pulo
        Vector2 movimento = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        bool pulo = Input.GetButton("Jump");

        inputGravado.Add(new PlayerInputFrame
        {
            movimento = movimento,
            pulo = pulo,
            tempo = Time.time
        });
    }

    void CriarClone()
    {
        GameObject clone = Instantiate(prefabClone, transform.position, transform.rotation);
        var ghost = clone.GetComponent<TimeCloneGhost>();
        ghost.CarregarInputs(new List<PlayerInputFrame>(inputGravado));
    }
}

[System.Serializable]
public class PlayerInputFrame
{
    public Vector2 movimento;
    public bool pulo;
    public float tempo;
}
