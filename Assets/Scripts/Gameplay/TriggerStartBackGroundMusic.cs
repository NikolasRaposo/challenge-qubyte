using UnityEngine;

[RequireComponent(typeof(Collider))]
public class TriggerStartBackGroundMusic : MonoBehaviour
{
    [Tooltip("AudioSource com a música que será tocada.")]
    [SerializeField] private AudioSource audioSource;

    [Tooltip("Se verdadeiro, toca apenas uma vez e desativa o trigger.")]
    [SerializeField] private bool playOnce = true;

    private bool _played;

    private void Awake()
    {
        // Garante que o collider é trigger
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;

        // Usa o AudioSource do próprio GameObject se não for atribuído
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (playOnce && _played) return;
        if (audioSource == null) return;

        if (!audioSource.isPlaying)
        {
            audioSource.Play();
        }
        _played = true;

        if (playOnce)
        {
            // Opcional: desativa o collider para evitar re-disparo
            var col = GetComponent<Collider>();
            if (col != null) col.enabled = false;
        }
    }
}
