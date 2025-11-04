using ECM.Examples.Common;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ECM.Examples.Components
{
    [RequireComponent(typeof(Rigidbody))]
    public class KinematicMove : MonoBehaviour
    {
        #region FIELDS

        [SerializeField]
        public float _moveTime = 3.0f;

        [SerializeField]
        private Vector3 _offset;
        [Tooltip("Quando ligado, o offset é interpretado no espaço local (Z=frente, Y=cima, X=lados) alinhado à rotação do objeto.")]
        [SerializeField]
        private bool _useLocalOffset = true;

        [Header("Editor Gizmos")]
        [SerializeField]
        private bool _showGizmos = true;
        [SerializeField]
        private bool _drawOnlyWhenSelected = false;
        [SerializeField]
        private float _startSphereRadius = 0.15f;
        [SerializeField]
        private float _targetSphereRadius = 0.2f;
        [SerializeField]
        private Color _pathColor = new Color(0.2f, 0.8f, 1f, 0.9f);
        [SerializeField]
        private Color _startColor = new Color(0f, 1f, 0f, 0.9f);
        [SerializeField]
        private Color _targetColor = new Color(1f, 0.5f, 0f, 0.9f);

        #endregion

        #region PRIVATE FIELDS

        private Rigidbody _rigidbody;

        private Vector3 _startPosition;
        private Vector3 _targetPosition;

        #endregion

        #region PROPERTIES
        
        public float moveTime
        {
            get { return _moveTime; }
            set { _moveTime = Mathf.Max(1.0f, value); }
        }

        public Vector3 offset
        {
            get { return _offset; }
            set { _offset = value; }
        }

        #endregion

        #region MONOBEHAVIOUR

        public void OnValidate()
        {
            moveTime = _moveTime;
            // Atualiza a posição alvo para refletir mudanças enquanto edita no Inspector
            var start = transform.position;
            var rotOffset = _useLocalOffset ? (transform.rotation * _offset) : _offset;
            _targetPosition = start + rotOffset;
        }

        public void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _rigidbody.isKinematic = true;

            _startPosition = transform.position;
            var rotOffset = _useLocalOffset ? (transform.rotation * _offset) : _offset;
            _targetPosition = _startPosition + rotOffset;
        }

        public void FixedUpdate()
        {
            var t = Utils.EaseInOut(Mathf.PingPong(Time.time, _moveTime), _moveTime);
            var p = Vector3.Lerp(_startPosition, _targetPosition, t);

            _rigidbody.MovePosition(p);
        }

        #endregion

        #region GIZMOS

        public void OnDrawGizmos()
        {
            if (!_showGizmos || _drawOnlyWhenSelected)
                return;
            DrawGizmosVisuals();
        }

        public void OnDrawGizmosSelected()
        {
            if (!_showGizmos)
                return;
            DrawGizmosVisuals();
        }

        private void DrawGizmosVisuals()
        {
            var start = transform.position;
            var rotOffset = _useLocalOffset ? (transform.rotation * offset) : offset;
            var target = start + rotOffset;

            // Linha do caminho
            Gizmos.color = _pathColor;
            Gizmos.DrawLine(start, target);

            // Indicadores de posição
            Gizmos.color = _startColor;
            Gizmos.DrawWireSphere(start, _startSphereRadius);

            Gizmos.color = _targetColor;
            Gizmos.DrawWireSphere(target, _targetSphereRadius);

            // Seta e label usando Handles (somente no Editor)
            #if UNITY_EDITOR
            var dir = (target - start);
            var len = dir.magnitude;
            if (len > 0.0001f)
            {
                var n = dir / len;
                Handles.color = _pathColor;
                Handles.ArrowHandleCap(0, target - n * 0.25f, Quaternion.LookRotation(n), 0.25f, EventType.Repaint);
                Handles.Label(target + Vector3.up * 0.1f, $"Target: {target}");
            }
            #endif
        }

        #endregion
    }
}
