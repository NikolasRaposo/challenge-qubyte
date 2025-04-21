using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimeCloneGhost : MonoBehaviour
{
    private List<PlayerInputFrame> replayInputs;
    private int index = 0;
    private float startTime;
    private CharacterController controller;

    public float velocidade = 5f;
    public float forcaPulo = 5f;
    private bool estaNoChao;
    private Vector3 velocity;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    public void CarregarInputs(List<PlayerInputFrame> inputs)
    {
        replayInputs = inputs;
        startTime = Time.time;
    }

    void Update()
    {
        if (replayInputs == null || index >= replayInputs.Count)
            return;

        float tempoRelativo = Time.time - startTime;
        PlayerInputFrame frame = replayInputs[index];

        if (tempoRelativo >= frame.tempo - replayInputs[0].tempo)
        {
            ExecutarFrame(frame);
            index++;
        }
    }

    void ExecutarFrame(PlayerInputFrame frame)
    {
        Vector3 move = new Vector3(frame.movimento.x, 0, frame.movimento.y);
        controller.Move(move * velocidade * Time.deltaTime);

        if (frame.pulo && estaNoChao)
        {
            velocity.y = forcaPulo;
        }

        velocity.y += Physics.gravity.y * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        estaNoChao = controller.isGrounded;
        if (estaNoChao && velocity.y < 0) velocity.y = -1f;
    }
}
