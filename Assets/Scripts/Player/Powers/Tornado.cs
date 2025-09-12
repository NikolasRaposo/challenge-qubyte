using System.Collections.Generic;
using ThirdParty.StarterAssets.ThirdPersonController.Scripts;
using UnityEngine;

namespace Player.Powers
{
    public class Tornado : MonoBehaviour {
        public float speed = 5f;
        public float lifeTime = 3f;

        public float upwardforce = 5f; // Força para levantar o jogador
        public float moveDuration = 1f; // tempo em que o tornado se move
        public float stabilizationHeight = 2f; // Altura de estabilização acima do tornado
        public float stabilizationForce = 5f; // Força para estabilizar o jogador
        public float heightVariation = 0.2f; // Variação de altura para simular suspensão
        public float variationSpeed = 2f; // Velocidade da variação de altura

        private float moveTimer;
        private HashSet<ThirdPersonController> playersInTornado = new HashSet<ThirdPersonController>();

        private void Start() {
            moveTimer = moveDuration;
            Destroy(gameObject, lifeTime);
        }

        private void OnTriggerEnter(Collider other) {
            if (other.CompareTag("Player")) {
                var playerController = other.GetComponent<ThirdPersonController>();
                if (playerController != null) {
                    playersInTornado.Add(playerController);
                    playerController.ApplyUpwardForce(upwardforce);
                    playerController.SetFreeFallAnimation(true);
                    
                    // --- MUDANÇA AQUI ---
                    playerController.SetGravityOverride(true);
                }
            }
        }



        private void OnTriggerExit(Collider other) {
            if (other.CompareTag("Player")) {
                var playerController = other.GetComponent<ThirdPersonController>();
                if (playerController != null) {
                    playersInTornado.Remove(playerController);
                    playerController.SetFreeFallAnimation(false);
                    
                    // --- MUDANÇA AQUI ---
                    playerController.SetGravityOverride(false);
                }
            }
        }

        private void OnDestroy() {
            foreach (var playerController in playersInTornado) {
                if (playerController != null) // Boa prática verificar se o jogador ainda existe
                {
                    playerController.SetFreeFallAnimation(false);
                    
                    // --- MUDANÇA AQUI ---
                    playerController.SetGravityOverride(false);
                }
            }
            playersInTornado.Clear();
        }

        /*private void StabilizePlayer(ThirdPersonController player) {
            // Calcula a altura desejada com variação
            float targetHeight = transform.position.y + stabilizationHeight +
                                 Mathf.Sin(Time.time * variationSpeed) * heightVariation;

            // Calcula a força para estabilizar o jogador
            float force = (targetHeight - player.transform.position.y) * stabilizationForce;

            // Aplica a força no jogador
            player.ApplyUpwardForce(force);
        }*/
    }
}