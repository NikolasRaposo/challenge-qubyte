using System;
using Managers;
using UnityEngine;

namespace Player.Powers {
    public class TornadoAttack : MonoBehaviour
    {
        public GameObject tornadoPrefab;
        public Transform spawnPoint;
        public float cooldown = 2f;
        
        private float _lastTornadoTime = -Mathf.Infinity;

        private void Start() {
            InputManager.Instance.OnTornado += InstanceOnOnTornado;
        }

        private void InstanceOnOnTornado() {
            if (!(Time.time >= _lastTornadoTime + cooldown)) return;
            LauchTornado();
            _lastTornadoTime = Time.time;
        }

        private void LauchTornado()
        {
            Instantiate(tornadoPrefab, spawnPoint.position, transform.rotation);
        }
    }
}
