using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum FlipBookMode
{
    Loop,
    PingPong,
    PlayOneTime
}

public class FlipBookMatController : MonoBehaviour
{
    public FlipBookMode mode;
    public float animationSpeed = 1.0f;
    public int numberOfSprites = 1; // Número de sprites no flipbook
    public bool resetPlayOneTime = true; // Variável para resetar o estado inicial do PlayOneTime
    private Material material;
    private Coroutine flipBookCoroutine;

    void Start()
    {
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogError("SpriteRenderer não encontrado no objeto: " + gameObject.name);
            return;
        }

        material = spriteRenderer.material;
        if (material == null)
        {
            Debug.LogError("Material não encontrado no SpriteRenderer do objeto: " + gameObject.name);
            return;
        }

        flipBookCoroutine = StartCoroutine(ControlFlipBook());
    }

    void Update()
    {
        if (mode == FlipBookMode.PlayOneTime && resetPlayOneTime)
        {
            resetPlayOneTime = false; // Reseta o estado inicial
            if (flipBookCoroutine != null)
            {
                StopCoroutine(flipBookCoroutine);
            }
            flipBookCoroutine = StartCoroutine(ControlFlipBook());
        }
    }

    private IEnumerator ControlFlipBook()
    {
        float flipBookValue = 0.0f;
        float direction = 1.0f;

        while (true)
        {
            if (material != null)
            {
                material.SetFloat("_FlipBookControl", flipBookValue);

                switch (mode)
                {
                    case FlipBookMode.Loop:
                        flipBookValue += animationSpeed * Time.deltaTime;
                        if (flipBookValue >= numberOfSprites)
                        {
                            flipBookValue = 0.0f;
                        }
                        break;

                    case FlipBookMode.PingPong:
                        flipBookValue += direction * animationSpeed * Time.deltaTime;
                        if (flipBookValue >= numberOfSprites || flipBookValue < 0.0f)
                        {
                            direction *= -1.0f;
                            flipBookValue = Mathf.Clamp(flipBookValue, 0.0f, numberOfSprites - 1);
                        }
                        break;

                    case FlipBookMode.PlayOneTime:
                        flipBookValue += animationSpeed * Time.deltaTime;
                        if (flipBookValue >= numberOfSprites)
                        {
                            yield break;
                        }
                        break;
                }
            }

            yield return null;
        }
    }

    void OnDestroy()
    {
        if (flipBookCoroutine != null)
        {
            StopCoroutine(flipBookCoroutine);
        }
    }
}
