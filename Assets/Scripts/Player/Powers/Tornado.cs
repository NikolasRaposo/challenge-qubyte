using ThirdParty.StarterAssets.ThirdPersonController.Scripts;
using UnityEngine;

namespace Player.Powers
{
    public class Tornado : MonoBehaviour {
        public float speed = 5f;
        public float lifeTime = 3f;
        public float moveDuration = 1f; // tempo em que o tornado se move

        private float moveTimer;

        private void Start() {
            moveTimer = moveDuration;
            Destroy(gameObject, lifeTime);
        }

        private void Update() {
            if (moveTimer > 0f) {
                transform.Translate(Vector3.forward * speed * Time.deltaTime);
                moveTimer -= Time.deltaTime;
            }
        }

        private void OnTriggerEnter(Collider other) {
            Debug.Log("Tornado encostou em " + other + " e ele tem a tag " + other.tag);

            if (other.CompareTag("Enemy")) {
                Destroy(other.gameObject);
            }

            if (other.CompareTag("Player")) {
                other.GetComponent<ThirdPersonController>().ToggleDoubleJump(true);
            }
        }
    }
}