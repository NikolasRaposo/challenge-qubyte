using System.Collections;
using UnityEngine;

[AddComponentMenu("Gameplay/Simple Step Rotate")]
public class SimpleStepRotate : MonoBehaviour
{
    public enum RotationAxis { X, Y, Z }

    [Header("Timing")]
    [Tooltip("Tempo entre inícios de rotação (inclui tremor).")]
    [SerializeField] private float interval = 3.0f;
    [Tooltip("Delay inicial antes do primeiro ciclo.")]
    [SerializeField] private float startDelay = 0.0f;

    [Header("Shake (pré-rotação)")]
    [SerializeField] private bool useShake = true;
    [Tooltip("Eixo do tremor (independente do eixo de rotação).")]
    [SerializeField] private RotationAxis shakeAxis = RotationAxis.Y;
    [SerializeField] private float shakeDuration = 0.5f;
    [Tooltip("Frequência do tremor em Hz.")]
    [SerializeField] private float shakeFrequency = 8.0f;
    [Tooltip("Amplitude do tremor em graus.")]
    [SerializeField] private float shakeAmplitudeDeg = 4.0f;

    [Header("Rotação")]
    [SerializeField] private RotationAxis axis = RotationAxis.Y;
    [SerializeField] private float rotateAngleDeg = 180.0f;
    [SerializeField] private float rotateDuration = 0.6f;
    [Tooltip("Quando ligado, alterna a direção da rotação (+angulo, depois -angulo, e assim por diante).")]
    [SerializeField] private bool alternateDirection = false;

    [Header("Controle")]
    [SerializeField] private bool playOnEnable = true;
    [SerializeField] private bool loop = true;

    private Coroutine _routine;
    private Quaternion _baseRotation;
    private bool _flipDirection = false;

    private void Awake()
    {
        _baseRotation = transform.localRotation;
    }

    private void OnEnable()
    {
        if (playOnEnable)
            StartRoutine();
    }

    private void OnDisable()
    {
        StopRoutine();
    }

    public void StartRoutine()
    {
        if (_routine != null) return;
        _routine = StartCoroutine(Run());
    }

    public void StopRoutine()
    {
        if (_routine != null)
        {
            StopCoroutine(_routine);
            _routine = null;
        }
    }

    public void TriggerNow()
    {
        // dispara imediatamente um ciclo (tremor + rotação)
        if (_routine != null)
        {
            StopRoutine();
        }
        _routine = StartCoroutine(RunSingle());
    }

    private IEnumerator Run()
    {
        if (startDelay > 0f)
            yield return new WaitForSeconds(startDelay);

        do
        {
            // espera até o próximo início
            if (interval > 0f)
                yield return new WaitForSeconds(interval);

            // ciclo de ação
            yield return RunSingle();

        } while (loop);

        _routine = null;
    }

    private IEnumerator RunSingle()
    {
        // Base é a rotação atual no início do ciclo
        var startBase = transform.localRotation;
        Vector3 axisVec = AxisVector(axis);
        Vector3 shakeAxisVec = AxisVector(shakeAxis);

        // Tremor pré-rotação
        if (useShake && shakeDuration > 0f && shakeAmplitudeDeg > 0f && shakeFrequency > 0f)
        {
            float t = 0f;
            while (t < shakeDuration)
            {
                t += Time.deltaTime;
                float phase = t * shakeFrequency * Mathf.PI * 2f; // 2π f t
                float angle = Mathf.Sin(phase) * shakeAmplitudeDeg;
                transform.localRotation = startBase * Quaternion.AngleAxis(angle, shakeAxisVec);
                yield return null;
            }
            // volta à rotação base antes de girar
            transform.localRotation = startBase;
        }

        // Rotação 180° (ou valor definido)
        float dirSign = alternateDirection ? (_flipDirection ? -1f : 1f) : 1f;
        Quaternion endRot = startBase * Quaternion.AngleAxis(rotateAngleDeg * dirSign, axisVec);
        if (rotateDuration <= 0f)
        {
            transform.localRotation = endRot;
        }
        else
        {
            float t = 0f;
            while (t < rotateDuration)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / rotateDuration);
                // Ease in-out suave
                float s = Mathf.SmoothStep(0f, 1f, u);
                transform.localRotation = Quaternion.Slerp(startBase, endRot, s);
                yield return null;
            }
            transform.localRotation = endRot;
        }

        if (alternateDirection)
            _flipDirection = !_flipDirection;
    }

    private static Vector3 AxisVector(RotationAxis a)
    {
        switch (a)
        {
            case RotationAxis.X: return Vector3.right;
            case RotationAxis.Y: return Vector3.up;
            case RotationAxis.Z: return Vector3.forward;
            default: return Vector3.up;
        }
    }
}
