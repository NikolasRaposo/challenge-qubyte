using System.Collections;
using System.Collections.Generic;
using Gameplay;
using Player;
using UnityEngine;

public class PlayerVfxPosFeedbackControl : MonoBehaviour
{
    [Header("Configurações do Feedback Visual")]
    [SerializeField] private GameObject feedbackVisualObject;
    [SerializeField] private float maxRaycastDistance = 50f;
    [SerializeField] private LayerMask groundLayerMask = -1;
    [SerializeField] private LayerMask trampolineLayerMask = 0; // defina para Layer "Trampolim" no Inspector
    [SerializeField] private float trampolineRayRadius = 0.75f; // amplia detecção lateral do trampolim
    [SerializeField] private float groundCheckDistance = 0.1f;
    [SerializeField] private float updateFrequency = 0.02f; // 50 FPS para otimização
    
    [Header("Configurações de Offset")]
    [SerializeField] private Vector3 feedbackOffset = Vector3.zero;
    [SerializeField] private bool alignWithSurfaceNormal = true;
    
    [Header("Configurações de Suavização")]
    [SerializeField] private float lerpSpeedMultiplier = 1f;
    [SerializeField] private float rotationLerpSpeedMultiplier = 0.8f;
    
    private CharacterController characterController;
    private Rigidbody playerRigidbody;
    private bool isGrounded;
    private bool wasGrounded; // Para detectar transição de ar para chão
    private float lastUpdateTime;
    
    // Variáveis para interpolação suave
    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private bool hasValidTarget;

    // Cache do componente ECMSaciController para reset de altura
    private ECMSaciController saciController;
    
    void Start() {
        // Tenta obter o CharacterController primeiro, depois Rigidbody
        characterController = GetComponent<CharacterController>();
        if (characterController == null) {
            playerRigidbody = GetComponent<Rigidbody>();
        }
        
        // Cache do ECMSaciController para reset de altura
        saciController = GetComponent<ECMSaciController>();
        
        // Ajusta máscaras para evitar ruído: garante mask de Trampolim e exclui Trampolim do ground
        int trampLayer = LayerMask.NameToLayer("Trampolim");
        if (trampolineLayerMask.value == 0 && trampLayer >= 0) {
            trampolineLayerMask = LayerMask.GetMask("Trampolim");
        }
        if (trampLayer >= 0) {
            // Remove o layer Trampolim da máscara de chão para não marcar grounded antes do contato
            groundLayerMask &= ~(1 << trampLayer);
        }
        
        // Esconde o feedback visual inicialmente e desacopla do pai
        if (feedbackVisualObject != null) {
            feedbackVisualObject.SetActive(false);
            // Remove o objeto de feedback da hierarquia do player para evitar oscilação
            feedbackVisualObject.transform.SetParent(null);
        } else {
            Debug.LogWarning("PlayerVfxPosFeedbackControl: Objeto de feedback visual não foi atribuído!");
        }
    }
    
    void Update()
    {
        CheckGroundStatus();
        
        // Atualiza o target apenas na frequência definida para otimização
        if (Time.time - lastUpdateTime >= updateFrequency)
        {
            lastUpdateTime = Time.time;
            UpdateFeedbackTarget();
        }
        
        // Interpola suavemente a cada frame para movimento fluido
        UpdateFeedbackVisual();
    }
    
    private void CheckGroundStatus()
    {
        wasGrounded = isGrounded; // Salva estado anterior
        
        if (characterController != null)
        {
            // Usa CharacterController.isGrounded se disponível
            isGrounded = characterController.isGrounded;
        }
        else if (playerRigidbody != null)
        {
            // Faz um raycast curto para baixo para detectar o chão
            Vector3 rayOrigin = transform.position;
            isGrounded = Physics.Raycast(rayOrigin, Vector3.down, groundCheckDistance, groundLayerMask);
        }
        else
        {
            // Fallback: sempre considera como no ar se não há componente de movimento
            isGrounded = false;
        }
        
        // Detecta aterrissagem no chão normal (transição de ar para chão)
        if (!wasGrounded && isGrounded && saciController != null)
        {
            // Libera qualquer supressão de pulo ao tocar o chão para restaurar o controle
            saciController.SetExternalJumpSuppression(false);
            // Jogador acabou de aterrissar no chão normal, reseta altura armazenada em todos os trampolins
            ResetStoredHeightInAllTrampolines();
        }
    }
    
    private void UpdateFeedbackTarget()
    {
        if (feedbackVisualObject == null)
            return;
            
        if (isGrounded)
        {
            // Player está no chão, esconde o feedback
            if (feedbackVisualObject.activeSelf)
            {
                feedbackVisualObject.SetActive(false);
                hasValidTarget = false;
            }
        }
        else
        {
            // Player está no ar, calcula nova posição alvo
            CalculateTargetPosition();
        }
    }
    
    private void UpdateFeedbackVisual()
    {
        if (feedbackVisualObject == null || !hasValidTarget)
            return;
            
        if (!isGrounded && feedbackVisualObject.activeSelf)
        {
            // Calcula a velocidade de lerp baseada na frequência de atualização
            float adaptiveLerpSpeed = (lerpSpeedMultiplier / updateFrequency) * Time.deltaTime;
            float adaptiveRotationLerpSpeed = (rotationLerpSpeedMultiplier / updateFrequency) * Time.deltaTime;
            
            // Interpola suavemente para a posição alvo
            feedbackVisualObject.transform.position = Vector3.Lerp(
                feedbackVisualObject.transform.position, 
                targetPosition, 
                adaptiveLerpSpeed
            );
            
            // Interpola suavemente para a rotação alvo se habilitado
            if (alignWithSurfaceNormal)
            {
                feedbackVisualObject.transform.rotation = Quaternion.Lerp(
                    feedbackVisualObject.transform.rotation,
                    targetRotation,
                    adaptiveRotationLerpSpeed
                );
            }
        }
    }
    
    private void CalculateTargetPosition()
    {
        Vector3 rayOrigin = transform.position;
        RaycastHit hit;
        // Primeiro, raycast para o chão (feedback visual)
        if (Physics.Raycast(rayOrigin, Vector3.down, out hit, maxRaycastDistance, groundLayerMask))
        {
            // Calcula a posição alvo no ponto de impacto do raycast
            targetPosition = hit.point + feedbackOffset;
            
            // Calcula a rotação alvo se habilitado
            if (alignWithSurfaceNormal)
            {
                Vector3 surfaceNormal = hit.normal;
                targetRotation = Quaternion.LookRotation(Vector3.Cross(surfaceNormal, Vector3.right), surfaceNormal);
            }
            
            // Mostra o feedback se não estiver visível
            if (!feedbackVisualObject.activeSelf)
            {
                feedbackVisualObject.SetActive(true);
                // Posiciona imediatamente na primeira vez para evitar salto visual
                feedbackVisualObject.transform.position = targetPosition;
                if (alignWithSurfaceNormal)
                {
                    feedbackVisualObject.transform.rotation = targetRotation;
                }
            }
            
            hasValidTarget = true;
        }
        else
        {
            // Se não encontrar chão dentro da distância máxima, esconde o feedback
            hasValidTarget = false;
            if (feedbackVisualObject.activeSelf)
            {
                feedbackVisualObject.SetActive(false);
            }
        }

        // Segundo, raycast dedicado para trampolim (anti-ruído)
        RaycastHit trampHit;
        // Força colisão com triggers para detectar trampolins configurados como triggers
        bool trampHitOk = Physics.Raycast(rayOrigin, Vector3.down, out trampHit, maxRaycastDistance, trampolineLayerMask, QueryTriggerInteraction.Collide);
        if (!trampHitOk && trampolineRayRadius > 0f)
        {
            // Fallback: SphereCast para ampliar detecção quando se aproxima pela lateral
            trampHitOk = Physics.SphereCast(rayOrigin, trampolineRayRadius, Vector3.down, out trampHit, maxRaycastDistance, trampolineLayerMask, QueryTriggerInteraction.Collide);
        }
        if (trampHitOk)
        {
            var trampoline = trampHit.collider != null ? trampHit.collider.GetComponentInParent<ECMTrampolineController>() : null;
            if (trampoline != null)
            {
                var saci = GetComponent<ECMSaciController>();
                if (saci != null)
                {
                    // Reporta apenas quando está descendo para evitar valores durante subida
                    float vy = (saci.movement != null) ? saci.movement.velocity.y : 0f;
                    if (vy < 0f)
                    {
                        trampoline.ReportPreContactHeight(saci, trampHit.distance);
                    }
                    else
                    {
                        Debug.Log($"[TrampoDebug] Raycast parou: vy={vy:F2} (não está descendo)", this);
                    }
                }
            }
        }
    }
    
    // Reseta altura armazenada em todos os trampolins quando o jogador aterrissa no chão normal
    private void ResetStoredHeightInAllTrampolines()
    {
        // Encontra todos os trampolins na cena e reseta a altura armazenada
        ECMTrampolineController[] trampolines = FindObjectsOfType<ECMTrampolineController>();
        foreach (var trampoline in trampolines)
        {
            trampoline.ResetStoredHeight(saciController);
        }
    }
    
    // Método para debug - desenha o raycast na Scene View
    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying)
            return;
            
        // Desenha o raycast de detecção de chão
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Vector3 rayOrigin = transform.position;
        Gizmos.DrawRay(rayOrigin, Vector3.down * groundCheckDistance);
        
        // Desenha o raycast do feedback visual
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(rayOrigin, Vector3.down * maxRaycastDistance);
    }
}
