using ECM.Examples.Common;
using UnityEngine;

namespace ECM.Examples.Components
{
    [RequireComponent(typeof(Rigidbody))]
    public class KinematicRotate : MonoBehaviour
    {
        #region FIELDS

        [SerializeField]
        private float _rotationSpeed = 30.0f;

        public enum RotationAxis { X, Y, Z }

        [SerializeField]
        [Tooltip("Eixo de rotação (X, Y, Z).")]
        private RotationAxis _axis = RotationAxis.Y;

        [SerializeField]
        [Tooltip("Quando ligado, inverte a direção da rotação.")]
        private bool _inverse = false;

        #endregion

        #region PRIVATE FIELDS

        private Rigidbody _rigidbody;

        private float _angle;

        #endregion

        #region PROPERTIES

        public float rotationSpeed
        {
            get { return _rotationSpeed; }
            set { _rotationSpeed = Mathf.Clamp(value, -360.0f, 360.0f); }
        }

        public float angle
        {
            get { return _angle; }
            set { _angle = Utils.WrapAngle(value); }
        }

        public RotationAxis axis
        {
            get { return _axis; }
            set { _axis = value; }
        }

        public bool inverse
        {
            get { return _inverse; }
            set { _inverse = value; }
        }

        #endregion

        #region MONOBEHAVIOUR

        public void OnValidate()
        {
            rotationSpeed = _rotationSpeed;
        }

        public void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _rigidbody.isKinematic = true;
        }

        public void FixedUpdate()
        {
            var delta = (inverse ? -rotationSpeed : rotationSpeed) * Time.deltaTime;
            angle += delta;
            
            Quaternion rotation;
            switch (_axis)
            {
                case RotationAxis.X:
                    rotation = Quaternion.Euler(angle, 0.0f, 0.0f);
                    break;
                case RotationAxis.Y:
                    rotation = Quaternion.Euler(0.0f, angle, 0.0f);
                    break;
                case RotationAxis.Z:
                    rotation = Quaternion.Euler(0.0f, 0.0f, angle);
                    break;
                default:
                    rotation = Quaternion.Euler(0.0f, angle, 0.0f);
                    break;
            }
            _rigidbody.MoveRotation(rotation);
        }

        #endregion
    }
}
