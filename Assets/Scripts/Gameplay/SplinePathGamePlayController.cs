using UnityEngine;
using UnityEngine.Splines;
using Player;

public class SplinePathGamePlayController : MonoBehaviour
{
    [Header("Spline Animation")]
    [Tooltip("Componente SplineAnimate configurado para seguir a spline.")]
    [SerializeField] private SplineAnimate splineAnimate;
    [Tooltip("Transform do objeto que movimenta pela spline (pai temporário do Saci). Se vazio, usa o transform do SplineAnimate.")]
    [SerializeField] private Transform animateHolder;

    [Header("End Trigger")]
    [Tooltip("Trigger colocado no final do caminho para finalizar o modo spline.")]
    [SerializeField] private SplinePathEndTrigger endTrigger;

    // Estado atual do Saci durante o modo da spline
    private ECMSaciController _currentSaci;
    private Transform _originalParent;
    private bool _isInSplineMode = false;
    [Header("Ativação")]
    [Tooltip("Atraso mínimo (s) após habilitar antes de aceitar entrada no trigger.")]
    [Min(0f)] [SerializeField] private float activationDelay = 0f;
    private float _enabledTime;

    private void Awake()
    {
        if (splineAnimate == null)
            splineAnimate = GetComponentInChildren<SplineAnimate>();
        if (animateHolder == null && splineAnimate != null)
            animateHolder = splineAnimate.transform;

        // Auto-descoberta do EndTrigger (opcional)
        if (endTrigger == null)
            endTrigger = GetComponentInChildren<SplinePathEndTrigger>();
        if (endTrigger != null)
            endTrigger.controller = this;
    }

    private void OnEnable()
    {
        _enabledTime = Time.time;
    }

    private bool IsActive()
    {
        return Time.time >= _enabledTime + activationDelay;
    }

    private void Update()
    {
        // Mantém a posição local zerada durante o modo spline
        if (_isInSplineMode && _currentSaci != null)
        {
            _currentSaci.transform.localPosition = Vector3.zero;
            _currentSaci.transform.localRotation = Quaternion.identity;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsActive())
        {
            Debug.Log($"[SplinePath] Ignorado (delay {activationDelay:F2}s ativo). Collider: '{other.name}' no {Time.time - _enabledTime:F2}s", this);
            return;
        }
        var saci = other.GetComponentInParent<ECMSaciController>();
        if (saci == null)
            return;

        // Guarda estado original
        _currentSaci = saci;
        _originalParent = saci.transform.parent;

        // Faz o Saci virar filho do holder de animação para seguir a spline
        if (animateHolder != null)
        {
            saci.transform.SetParent(animateHolder, true);
            saci.transform.localPosition = Vector3.zero;
        }
        else
        {
            saci.transform.SetParent(transform, true);
            saci.transform.localPosition = Vector3.zero;
        }

        Debug.Log($"[SplinePath] Entrada: '{saci.name}' parent -> '{(animateHolder != null ? animateHolder.name : name)}' pos -> { (animateHolder != null ? animateHolder.position : transform.position) }", this);

        // Pausa o movimento normal do ECM via Gate (centralizado)
        var gate = saci.GetComponent<PlayerControlGate>();
        if (gate != null)
            gate.EnterSplineMode();
        else
            saci.EnterSplinePathMode();

        // Ativa controle contínuo de posição
        _isInSplineMode = true;

        // Garante que a animação sempre comece do início em cada entrada
        // Primeiro reposiciona o holder no começo da spline (sem autoplay)
        if (splineAnimate != null)
        {
            splineAnimate.Restart(false);
            splineAnimate.Play();
            Debug.Log($"[SplinePath] Iniciando animação na spline '{splineAnimate.name}'", this);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // O trigger inicial não encerra o modo spline.
        // A saída é controlada exclusivamente pelo EndTrigger.
        return;
    }

    // Chamado pelo EndTrigger quando o Saci entra no trigger final
    public void HandleEndTriggerEnter(Collider other)
    {
        var saci = other.GetComponentInParent<ECMSaciController>();
        if (saci == null || _currentSaci != saci)
            return;

        // Pausa animação ao alcançar o fim
        if (splineAnimate != null)
            splineAnimate.Pause();

        // Restaura hierarquia
        saci.transform.SetParent(_originalParent, true);

        // Retoma movimento normal do ECM via Gate (centralizado)
        var gate = saci.GetComponent<PlayerControlGate>();
        if (gate != null)
            gate.ExitSplineMode();
        else
            saci.ExitSplinePathMode();

        // Desativa controle contínuo de posição
        _isInSplineMode = false;

        Debug.Log($"[SplinePath] Encerrado: '{saci.name}' restaurado para parent '{(_originalParent != null ? _originalParent.name : "<null>")}'", this);

        // Limpa estado
        _currentSaci = null;
        _originalParent = null;
    }

    // Força a saída imediata do modo spline e destaca o Saci do holder,
    // liberando controle de posição antes de qualquer outra lógica externa (ex.: teleporte).
    // Retorna true se um detach foi realizado.
    public bool TryForceDetachSaci(ECMSaciController saci)
    {
        if (!_isInSplineMode || saci == null || _currentSaci != saci)
            return false;

        // Pausa a animação da spline para evitar movimentos adicionais
        if (splineAnimate != null)
            splineAnimate.Pause();

        // Restaura hierarquia (se _originalParent for null, vai para a raiz)
        saci.transform.SetParent(_originalParent, true);

        // Retoma movimento normal do ECM via Gate (centralizado)
        var gate = saci.GetComponent<PlayerControlGate>();
        if (gate != null)
            gate.ExitSplineMode();
        else
            saci.ExitSplinePathMode();

        // Limpa estado local do controller para não mais ajustar a posição do Saci no Update
        _isInSplineMode = false;
        _currentSaci = null;
        _originalParent = null;

        Debug.Log("[SplinePath] ForceDetach acionado para Saci.", this);
        return true;
    }
}
