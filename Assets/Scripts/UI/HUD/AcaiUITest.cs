using UnityEngine;

public class AcaiUITest : MonoBehaviour
{
    [SerializeField] private CoinUIAnimation coinUI; // arrasta aqui o objeto com CoinUIAnimation
    [SerializeField] private int coinCount = 0;

    void Update()
    {
        // Quando apertar espaço, simula pegar uma moeda
        if (Input.GetKeyDown(KeyCode.Space))
        {
            coinCount++;
            coinUI.OnCollect(coinCount);
        }

        // Quando apertar C, reseta moedas
        if (Input.GetKeyDown(KeyCode.C))
        {
            coinCount = 0;
            coinUI.OnCollect(coinCount);
        }
    }
}