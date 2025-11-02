using System.Collections;
using DG.Tweening;
using Player;
using UnityEngine;

namespace Gameplay
{
    /// <summary>
    /// Variante ECM do BoxInteractor. Mantém mesmas opções visuais/respawn/explosão,
    /// mas aplica efeito de trampolim via ECMSaciController quando disponível.
    /// </summary>
    public class ECMBoxInteractor : MonoBehaviour
    {
        [Header("Box Settings")]
        public GameObject boxModel;

        [Header("General Settings")]
        [Tooltip("Pode ser disparada por pulo do player por cima?")]
        public bool canInteractOnJump = true;
        [Tooltip("Se verdadeiro, a caixa só pode ser usada uma vez.")]
        public bool interactOnce = true;

        [Header("Box Actions")]
        [Tooltip("Se verdadeiro, a caixa quebra visualmente e desativa.")]
        public bool breakOnInteract = true;
        [Tooltip("Se verdadeiro, a caixa desaparece. Sobrescrito por 'breakOnInteract'.")]
        public bool disappearOnInteract;
        [Tooltip("Se verdadeiro, a caixa reaparece após um tempo.")]
        public bool respawnAfterTime;
        [Tooltip("Tempo (s) para respawn da caixa.")]
        public float respawnTime = 3f;

        [Header("Item Spawning")]
        [Tooltip("Se verdadeiro, a caixa irá spawnar um item ao interagir.")]
        public bool spawnItem;
        [Tooltip("Prefab do item a spawnar.")]
        public GameObject itemPrefab;
        [Tooltip("Ponto específico de spawn (se nulo, usa posição da caixa).")]
        public Transform spawnPoint;
        [Tooltip("Configurações do efeito de spawn (quantidade, padrão de espalhamento, etc.).")]
        public ItemEffectSettings itemEffectSettings = new ItemEffectSettings();

        [Header("Explosion Effect")]
        [Tooltip("Se verdadeiro, cria efeito de explosão.")]
        public bool explodeOnBreak;
        [Tooltip("Força da explosão aplicada aos fragmentos.")]
        public float explosionForce = 300f;
        [Tooltip("Prefab dos fragmentos para instanciar na explosão.")]
        public GameObject fragmentsPrefab;

        [Header("Trampoline")]
        [Tooltip("Se verdadeiro, a caixa atua como trampolim lançando o player.")]
        public bool isTrampoline;
        [Tooltip("Impulso vertical aplicado ao alvo (ECM ou Rigidbody).")]
        public float trampolineForce = 10f;

        [Header("Visual Feedback")]
        [Tooltip("Se verdadeiro, toca feedback visual (shake).")]
        public bool useVisualFeedback = true;
        [Tooltip("Intensidade do shake (posicional).")]
        public float shakeIntensity = 0.05f;
        [Tooltip("Duração do shake (s).")]
        public float shakeDuration = 0.3f;

        private bool _hasBeenInteracted;
        private Vector3 _originalScale;
        private Renderer _boxRenderer;
        private Collider _boxCollider;

        private void Start()
        {
            _originalScale = transform.localScale;
            _boxRenderer = boxModel != null ? boxModel.GetComponent<Renderer>() : GetComponent<Renderer>();
            _boxCollider = GetComponent<Collider>();
        }

        /// <summary>
        /// Processa uma interação com a caixa (ex: chamada por triggers/ataques).
        /// </summary>
        /// <param name="interactor">Transform do objeto que interagiu (ex: player).</param>
        public void Interact(Transform interactor)
        {
            if (interactOnce && _hasBeenInteracted) return;
            _hasBeenInteracted = true;

            if (useVisualFeedback)
                PlayShakeFeedback();

            if (spawnItem && itemPrefab != null)
                SpawnItemsWithEffect();

            if (isTrampoline && interactor != null)
                ApplyTrampolineEffect(interactor);

            if (explodeOnBreak)
                Explode();
            else if (breakOnInteract)
                StartCoroutine(Break());
            else if (disappearOnInteract)
                StartCoroutine(Disappear());
        }

        private void PlayShakeFeedback()
        {
            transform.DOShakePosition(shakeDuration, shakeIntensity);
            transform.DOShakeRotation(shakeDuration, new Vector3(5f, 5f, 5f));
        }

        private void SpawnItemsWithEffect()
        {
            Vector3 spawnPosition = (spawnPoint != null) ? spawnPoint.position : transform.position + Vector3.up;
            GameObject effectControllerObject = new GameObject("ItemEffectController_Temp")
            {
                transform = { position = spawnPosition }
            };
            ItemEffectController controller = effectControllerObject.AddComponent<ItemEffectController>();
            controller.itemPrefab = this.itemPrefab;
            controller.settings = this.itemEffectSettings;
            controller.CreateItems();
            Destroy(effectControllerObject, 5f);
        }

        private void ApplyTrampolineEffect(Transform target)
        {
            if (target.TryGetComponent(out ECMSaciController saci) && saci.movement != null)
            {
                var mv = saci.movement;
                Vector3 lateral = Vector3.ProjectOnPlane(mv.velocity, saci.transform.up);
                mv.velocity = new Vector3(lateral.x, 0f, lateral.z);
                mv.ApplyVerticalImpulse(trampolineForce);
                mv.DisableGrounding();
            }
            else if (target.TryGetComponent(out Rigidbody rb))
            {
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
                rb.AddForce(Vector3.up * trampolineForce, ForceMode.VelocityChange);
            }
        }

        private void Explode()
        {
            if (fragmentsPrefab != null)
            {
                GameObject fragments = Instantiate(fragmentsPrefab, transform.position, transform.rotation);
                foreach (Rigidbody rb in fragments.GetComponentsInChildren<Rigidbody>())
                    rb.AddExplosionForce(explosionForce, transform.position, 2f);
                Destroy(fragments, 5f);
            }

            if (_boxRenderer != null) _boxRenderer.enabled = false;
            if (_boxCollider != null) _boxCollider.enabled = false;

            if (respawnAfterTime)
                Invoke(nameof(Respawn), respawnTime);
            else
                Destroy(gameObject, 0.5f);
        }

        private IEnumerator Break()
        {
            yield return new WaitForSeconds(0.1f);
            if (_boxRenderer != null) _boxRenderer.enabled = false;
            if (_boxCollider != null) _boxCollider.enabled = false;
            if (respawnAfterTime)
                Invoke(nameof(Respawn), respawnTime);
            else
                Destroy(gameObject, 0.5f);
        }

        private IEnumerator Disappear()
        {
            yield return new WaitForSeconds(0.1f);
            if (_boxRenderer != null) _boxRenderer.enabled = false;
            if (_boxCollider != null) _boxCollider.enabled = false;
            if (respawnAfterTime)
                Invoke(nameof(Respawn), respawnTime);
            else
                Destroy(gameObject, 0.5f);
        }

        private void Respawn()
        {
            _hasBeenInteracted = false;
            if (_boxRenderer != null) _boxRenderer.enabled = true;
            if (_boxCollider != null) _boxCollider.enabled = true;
            transform.localScale = Vector3.zero;
            DOTween.Sequence()
                .Append(transform.DOScale(_originalScale, 0.5f).SetEase(Ease.OutBack))
                .Join(transform.DOShakePosition(0.3f, 0.05f));
        }
    }
}