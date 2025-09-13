using PrimeTween;
using UnityEngine;

public class PrimeTweenTestController : MonoBehaviour {
    [Header("Referências")]
    public Transform targetTransform;
    public Camera targetCamera;

    [Header("Configurações das Animações (Ajuste aqui)")]
    [SerializeField] private TweenSettings<Vector3> positionSettings = new(new Vector3(0,0,0), new Vector3(0, 5, 0), 1f);
    [SerializeField] private TweenSettings<Vector3> rotationSettings = new(Vector3.zero, new Vector3(0, 90, 0), 1f);
    [SerializeField] private TweenSettings<float> scaleSettings = new(1f, 2f, 1f);
    [SerializeField] private ShakeSettings cameraShakeSettings = new(new Vector3(0.5f, 0.5f, 0), 0.5f);

    [Header("Bools de Ativação (One-shot)")]
    [Tooltip("Move o objeto no eixo Y.")]
    public bool animatePosition;
    [Tooltip("Rotaciona o objeto.")]
    public bool animateRotation;
    [Tooltip("Altera a escala uniforme do objeto.")]
    public bool animateScale;
    [Tooltip("Aplica um shake na câmera.")]
    public bool shakeCamera;
    [Tooltip("Executa uma sequência de Posições e Escala.")]
    public bool chainPositionAndScale;
    [Tooltip("Executa uma sequência de Rotações e Shake.")]
    public bool chainRotationAndShake;

    private Tween currentTween;
    private Sequence currentSequence;

    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private Vector3 initialScale;
    private Rigidbody targetRigidbody;
    
    // Variáveis para detectar a mudança de estado dos bools
    private bool wasAnimatePosition;
    private bool wasAnimateRotation;
    private bool wasAnimateScale;
    private bool wasShakeCamera;
    private bool wasChainPositionAndScale;
    private bool wasChainRotationAndShake;

    void Start() {
        // Guarda o estado inicial para resetar entre os testes
        if (targetTransform != null) {
            initialPosition = targetTransform.position;
            initialRotation = targetTransform.rotation;
            initialScale = targetTransform.localScale;
            
        }
        // Desabilita o aviso sobre o valor final ser igual ao inicial
        PrimeTweenConfig.warnEndValueEqualsCurrent = false;
    }

    void Update() {
        // Verifica a mudança de estado dos bools para disparar as animações
        HandleToggle(animatePosition, ref wasAnimatePosition, RunPositionTween);
        HandleToggle(animateRotation, ref wasAnimateRotation, RunRotationTween);
        HandleToggle(animateScale, ref wasAnimateScale, RunScaleTween);
        HandleToggle(shakeCamera, ref wasShakeCamera, RunShakeCameraTween);
        HandleToggle(chainPositionAndScale, ref wasChainPositionAndScale, RunChainPositionAndScale);
        HandleToggle(chainRotationAndShake, ref wasChainRotationAndShake, RunChainRotationAndShake);
    }

    /// <summary>
    /// Detecta quando um bool é ativado e chama a ação correspondente.
    /// </summary>
    private void HandleToggle(bool toggle, ref bool wasToggled, System.Action startAction) {
        if (toggle && !wasToggled) {
            startAction();
        }
        wasToggled = toggle;
    }

    /// <summary>
    /// Para todas as animações e reseta o estado do Transform.
    /// </summary>
    private void StopAndReset() {
        if (currentTween.isAlive) {
            currentTween.Stop();
        }
        if (currentSequence.isAlive) {
            currentSequence.Stop();
        }
        if (targetTransform != null) {
            targetTransform.position = initialPosition;
            targetTransform.rotation = initialRotation;
            targetTransform.localScale = initialScale;
        }
    }

    void RunPositionTween() {
        StopAndReset();
        currentTween = Tween.Position(targetTransform, positionSettings)
            .OnComplete(this, _this => _this.animatePosition = false);
    }

    void RunRotationTween() {
        StopAndReset();
        currentTween = Tween.EulerAngles(targetTransform, rotationSettings)
            .OnComplete(this, _this => _this.animateRotation = false);
    }

    void RunScaleTween() {
        StopAndReset();
        currentTween = Tween.Scale(targetTransform, scaleSettings)
            .OnComplete(this, _this => _this.animateScale = false);
    }

    void RunShakeCameraTween() {
        StopAndReset();
        // Tween.ShakeCamera não aceita ShakeSettings.
        // Em vez disso, usamos ShakeLocalPosition/Rotation no transform da câmera, que alcança o mesmo efeito.
        currentSequence = Sequence.Create()
            .Group(Tween.ShakeLocalPosition(targetCamera.transform, cameraShakeSettings))
            .Group(Tween.ShakeLocalRotation(targetCamera.transform, cameraShakeSettings))
            .OnComplete(this, _this => _this.shakeCamera = false);
    }

    void RunChainPositionAndScale() {
        StopAndReset();
        if (targetRigidbody == null) return;

        var settings = positionSettings;
        settings.settings.updateType = UpdateType.FixedUpdate; // Animações de física devem rodar no FixedUpdate

        currentSequence = Sequence.Create()
            .Chain(Tween.Position(targetTransform, positionSettings))
            .Chain(Tween.Scale(targetTransform, scaleSettings))
            .OnComplete(this, _this => _this.chainPositionAndScale = false);
    }

    void RunChainRotationAndShake() {
        StopAndReset();
        currentSequence = Sequence.Create()
            .Chain(Tween.EulerAngles(targetTransform, rotationSettings))
            // Agrupamos os shakes para que ocorram ao mesmo tempo que a rotação termina
            .Group(Tween.ShakeLocalPosition(targetCamera.transform, cameraShakeSettings))
            .Group(Tween.ShakeLocalRotation(targetCamera.transform, cameraShakeSettings))
            .OnComplete(this, _this => _this.chainRotationAndShake = false);
    }
}