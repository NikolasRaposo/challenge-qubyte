using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class DollyMove : MonoBehaviour
{
    public CinemachineDollyCart dollyCart;
    public CinemachineVirtualCamera gameplayCamera;
    public float speed = 0.2f;
    public bool playOnStart = true;

    private bool isMoving = false;

    void Start()
    {
        if (playOnStart)
        {
            PlayCutscene();
        }
    }

    void Update()
    {
        if (isMoving)
        {
            dollyCart.m_Position += speed * Time.deltaTime;

            // Para quando chega ao final (se usar Normalized mode)
            if (dollyCart.m_Position >= 1f)
            {
                dollyCart.m_Position = 1f;
                isMoving = false;
                OnCutsceneEnd();
            }
        }
    }

    public void PlayCutscene()
    {
        dollyCart.m_Position = 0f;
        isMoving = true;
    }

    void OnCutsceneEnd()
    {
        // Aqui você pode ativar a câmera de gameplay, por exemplo
        Debug.Log("Fim da cutscene.");
        SwitchCamera(gameplayCamera);
    }
    
    void SwitchCamera(CinemachineVirtualCamera cam)
    {
        // Desativa todas
        foreach (var c in FindObjectsOfType<CinemachineVirtualCamera>())
            c.Priority = 0;

        // Ativa a câmera de Gameplay
        cam.Priority = 10;
    }
}