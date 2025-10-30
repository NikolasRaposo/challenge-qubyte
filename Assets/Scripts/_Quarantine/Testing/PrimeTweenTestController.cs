using UnityEngine;
using PrimeTween;

public class PrimeTweenTestController : MonoBehaviour {
    [Header("Referências")]
    public Transform targetTransform;
    public Camera targetCamera;

    [Header("Configurações das Animações (Ajuste aqui)")]
    [SerializeField] private TweenSettings<Vector3> positionSettings = new(new Vector3(0,0,0), new Vector3(0, 5, 0), 1f);
    [SerializeField] private TweenSettings<Vector3> rotationSettings = new(Vector3.zero, new Vector3(0, 90, 0), 1f);
    [SerializeField] private TweenSettings<float> scaleSettings = new(1f, 2f, 1f);
    [SerializeField] private ShakeSettings cameraShakeSettings = new(new Vector3(0.5f, 0.5f, 0), 0.5f);
    
    [Header("Controles de Easing Elástico")]
    [Tooltip("Força do efeito elástico (0.1 = suave, 1.0 = padrão, 2.0 = intenso)")]
    [Range(0.1f, 3.0f)]
    [SerializeField] private float elasticStrength = 1.0f;
    
    [Tooltip("Período de oscilação elástica (0.1 = rápido, 0.3 = padrão, 0.6 = lento)")]
    [Range(0.1f, 1.0f)]
    [SerializeField] private float elasticPeriod = 0.3f;
    
    [Tooltip("Usar easing elástico nas animações de posição")]
    [SerializeField] private bool useElasticEasing = false;

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
        
        // Calcula posições relativas à posição inicial da plataforma
        Vector3 startPos = initialPosition + positionSettings.startValue;
        Vector3 endPos = initialPosition + positionSettings.endValue;
        
        // Aplica easing elástico customizado se habilitado
        if (useElasticEasing) {
            var elasticEasing = Easing.Elastic(elasticStrength, elasticPeriod);
            // Cria configurações com loop infinito e modo PingPong para ir e voltar
            var loopSettings = new TweenSettings(positionSettings.settings.duration, elasticEasing, 
                cycles: -1, cycleMode: CycleMode.Yoyo);
            currentTween = Tween.Position(targetTransform, startPos, endPos, loopSettings)
                .OnComplete(this, _this => _this.animatePosition = false);
        } else {
            // Configura loop infinito também para o modo normal
            var loopSettings = positionSettings.settings;
            loopSettings.cycles = -1;
            loopSettings.cycleMode = CycleMode.Yoyo;
            var loopPositionSettings = new TweenSettings<Vector3>(startPos, endPos, loopSettings);
            currentTween = Tween.Position(targetTransform, loopPositionSettings)
                .OnComplete(this, _this => _this.animatePosition = false);
        }
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

        // Calcula posições relativas à posição inicial da plataforma
        Vector3 startPos = initialPosition + positionSettings.startValue;
        Vector3 endPos = initialPosition + positionSettings.endValue;

        // Aplica easing elástico customizado se habilitado
        if (useElasticEasing) {
            var elasticEasing = Easing.Elastic(elasticStrength, elasticPeriod);
            // Cria configurações com loop infinito e modo PingPong para ir e voltar
            var loopSettings = new TweenSettings(positionSettings.settings.duration, elasticEasing, 
                cycles: -1, cycleMode: CycleMode.Yoyo);
            loopSettings.updateType = UpdateType.FixedUpdate;
            currentSequence = Sequence.Create()
                .Chain(Tween.Position(targetTransform, startPos, endPos, loopSettings))
                .OnComplete(this, _this => _this.chainPositionAndScale = false);
        } else {
            // Configura loop infinito também para o modo normal
            var loopSettings = settings.settings;
            loopSettings.cycles = -1;
            loopSettings.cycleMode = CycleMode.Yoyo;
            var loopPositionSettings = new TweenSettings<Vector3>(startPos, endPos, loopSettings);
            currentSequence = Sequence.Create()
                .Chain(Tween.Position(targetTransform, loopPositionSettings))
                .OnComplete(this, _this => _this.chainPositionAndScale = false);
        }
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

    // Gizmos visuais para mostrar os pontos de início e fim da animação
    void OnDrawGizmos() {
        if (positionSettings.startValue != Vector3.zero || positionSettings.endValue != Vector3.zero) {
            // Usa a posição atual do transform como referência (funciona tanto no editor quanto em runtime)
            Vector3 basePosition = Application.isPlaying ? initialPosition : transform.position;
            Vector3 startPos = basePosition + positionSettings.startValue;
            Vector3 endPos = basePosition + positionSettings.endValue;
            
            // Desenha esfera no ponto inicial (verde)
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(startPos, 0.5f);
            Gizmos.DrawSphere(startPos, 0.2f);
            
            // Desenha esfera no ponto final (vermelho)
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(endPos, 0.5f);
            Gizmos.DrawSphere(endPos, 0.2f);
            
            // Desenha linha conectando os pontos
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(startPos, endPos);
            
            // Desenha seta indicando direção
            Vector3 direction = (endPos - startPos).normalized;
            Vector3 midPoint = (startPos + endPos) * 0.5f;
            
            // Corpo da seta
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(midPoint - direction * 0.3f, midPoint + direction * 0.3f);
            
            // Ponta da seta
            Vector3 arrowHead = midPoint + direction * 0.3f;
            Vector3 perpendicular = Vector3.Cross(direction, Vector3.up).normalized * 0.1f;
            if (perpendicular == Vector3.zero) perpendicular = Vector3.Cross(direction, Vector3.right).normalized * 0.1f;
            
            Gizmos.DrawLine(arrowHead, arrowHead - direction * 0.15f + perpendicular);
            Gizmos.DrawLine(arrowHead, arrowHead - direction * 0.15f - perpendicular);
        }
    }

    void OnDrawGizmosSelected() {
        // Gizmos mais detalhados quando o objeto está selecionado
        if (positionSettings.startValue != Vector3.zero || positionSettings.endValue != Vector3.zero) {
            // Labels para os pontos
            Gizmos.color = Color.white;
            
            // Desenha cubo pequeno na posição atual do objeto
            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(transform.position, Vector3.one * 0.3f);
        }
    }
}