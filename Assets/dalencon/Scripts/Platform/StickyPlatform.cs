using UnityEngine;

/// <summary>
/// Este script faz com que qualquer objeto com a tag "Player"
/// que entre em seu trigger se mova junto com a plataforma.
/// Deve ser adicionado ao objeto da plataforma móvel.
/// </summary>
public class StickyPlatform : MonoBehaviour, IPlatformVelocityProvider
{

    [SerializeField] Transform Platform;
    [Tooltip("Transform que está sendo animado (geralmente o pai da plataforma)")]
    [SerializeField] Transform AnimatedTransform;
    
    // Variáveis para calcular velocidade da plataforma
    private Vector3 _lastPosition;
    private Vector3 _currentVelocity;
    private bool _isInitialized = false;
    private Transform _trackedTransform;
    
    private void Start()
    {
        // Determina qual transform rastrear para velocidade
        _trackedTransform = AnimatedTransform != null ? AnimatedTransform : Platform;
        
        if (_trackedTransform != null)
        {
            _lastPosition = _trackedTransform.position;
            _isInitialized = true;
        }
    }
    
    private void Update()
    {
        if (_isInitialized && _trackedTransform != null)
        {
            // Calcula a velocidade do transform que está sendo animado
            Vector3 currentPosition = _trackedTransform.position;
            _currentVelocity = (currentPosition - _lastPosition) / Time.deltaTime;
            _lastPosition = currentPosition;
            
            // Debug: Log da velocidade da plataforma quando está se movendo
            if (_currentVelocity.magnitude > 0.01f)
            {
                //Debug.Log($"[StickyPlatform] Velocidade da plataforma: {_currentVelocity} | Magnitude: {_currentVelocity.magnitude}");
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Verifica se o objeto que entrou é o jogador
        if (other.gameObject.tag.Equals("Player"))
        {
            other.gameObject.transform.parent = Platform;
            
            // Notifica o ThirdPersonController sobre a entrada na plataforma
            var controller = other.GetComponent<ThirdParty.StarterAssets.ThirdPersonController.Scripts.ThirdPersonController>();
            if (controller != null)
            {
                controller.OnEnterPlatform(this);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.tag.Equals("Player"))
        {
            // Remove o pai do jogador quando ele sai do trigger
            other.gameObject.transform.parent = null;
            
            // Notifica o ThirdPersonController sobre a saída da plataforma
            var controller = other.GetComponent<ThirdParty.StarterAssets.ThirdPersonController.Scripts.ThirdPersonController>();
            if (controller != null)
            {
                controller.OnExitPlatform();
            }
        }
    }
    
    #region IPlatformVelocityProvider Implementation
    
    public Vector3 GetPlatformVelocity()
    {
        return _currentVelocity;
    }
    
    public bool IsMoving()
    {
        return _currentVelocity.magnitude > 0.01f;
    }
    
    #endregion
}