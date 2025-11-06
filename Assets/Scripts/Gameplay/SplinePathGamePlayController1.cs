using UnityEngine;
using UnityEngine.Splines;
using Player;

public class SplinePathGamePlayController1 : MonoBehaviour
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

    private void Awake()
    {
        if (splineAnimate == null)
            splineAnimate = GetComponentInChildren<SplineAnimate>();
        if (animateHolder == null && splineAnimate != null)
            animateHolder = splineAnimate.transform;

        // Auto-descoberta do EndTrigger (opcional)
        if (endTrigger == null)
            endTrigger = GetComponentInChildren<SplinePathEndTrigger>();
        //if (endTrigger != null)
          //  endTrigger.controller = this;
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

        // Pausa o movimento normal do ECM (torna RB kinematic internamente)
        saci.EnterSplinePathMode();

        // Ativa controle contínuo de posição
        _isInSplineMode = true;

        // Garante que a animação sempre comece do início em cada entrada
        // Primeiro reposiciona o holder no começo da spline (sem autoplay)
        if (splineAnimate != null)
        {
            splineAnimate.Restart(false);
            splineAnimate.Play();
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

        // Retoma movimento normal do ECM
        saci.ExitSplinePathMode();

        // Desativa controle contínuo de posição
        _isInSplineMode = false;

        // Limpa estado
        _currentSaci = null;
        _originalParent = null;
    }
}
