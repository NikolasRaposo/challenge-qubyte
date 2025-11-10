using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Managers;
using UnityEngine;

namespace FlowSystem
{
    /// <summary>
    /// Escuta eventos do GameManager e aciona micro-blocos correspondentes.
    /// Mantém a orquestração fora do GameManager para alinhar com FlowBlock.
    /// </summary>
    public class FlowRouter : MonoBehaviour
    {
        [Header("Blocos de UI/Fluxo")]
        [Tooltip("Bloco exibido ao completar nível.")]
        public FlowBlock levelCompleteBlock;
        [Tooltip("Bloco exibido ao entrar em Game Over.")]
        public FlowBlock gameOverBlock;
        [Tooltip("Bloco exibido durante o countdown de respawn.")]
        public FlowBlock respawnBlock;

        private CancellationTokenSource _cts;
        private GameManager _boundGameManager;

        private void OnEnable()
        {
            _cts = new CancellationTokenSource();

            // Tenta bind imediato; se não houver GameManager ainda, aguarda e faz bind assim que disponível
            var gm = GameManager.Instance;
            if (gm != null)
            {
                BindToGameManager(gm);
            }
            else
            {
                Debug.LogWarning("[FlowRouter] GameManager.Instance não está disponível no OnEnable. Aguardando inicialização para vincular eventos...");
                _ = WaitAndBindAsync(_cts.Token);
            }
        }

        private void OnDisable()
        {
            if (_cts != null)
            {
                _cts.Cancel();
                _cts.Dispose();
                _cts = null;
            }

            if (_boundGameManager != null)
            {
                _boundGameManager.OnLevelComplete -= HandleLevelComplete;
                _boundGameManager.OnGameOver -= HandleGameOver;
                _boundGameManager.OnRespawnRequested -= HandleRespawnRequested;
                _boundGameManager = null;
            }
        }

        private void BindToGameManager(GameManager gm)
        {
            _boundGameManager = gm;
            gm.OnLevelComplete += HandleLevelComplete;
            gm.OnGameOver += HandleGameOver;
            gm.OnRespawnRequested += HandleRespawnRequested;
        }

        private async UniTaskVoid WaitAndBindAsync(CancellationToken ct)
        {
            // Aguarda até que o singleton esteja pronto, respeitando cancelamento
            try
            {
                await UniTask.WaitUntil(() => GameManager.Instance != null, cancellationToken: ct);
                if (ct.IsCancellationRequested) return;
                var gm = GameManager.Instance;
                if (gm != null)
                    BindToGameManager(gm);
            }
            catch (OperationCanceledException)
            {
                // Ignorado: foi cancelado ao desabilitar
            }
        }

        private async void HandleLevelComplete()
        {
            if (levelCompleteBlock == null)
            {
                Debug.LogWarning("[FlowRouter] levelCompleteBlock não atribuído.");
                return;
            }
            try
            {
                await levelCompleteBlock.StartBlockAndWaitAsync(_cts.Token);
            }
            catch (Exception e)
            {
                Debug.LogError($"[FlowRouter] Erro ao executar levelCompleteBlock: {e}");
            }
        }

        private async void HandleGameOver()
        {
            if (gameOverBlock == null)
            {
                Debug.LogWarning("[FlowRouter] gameOverBlock não atribuído.");
                return;
            }
            try
            {
                await gameOverBlock.StartBlockAndWaitAsync(_cts.Token);
            }
            catch (Exception e)
            {
                Debug.LogError($"[FlowRouter] Erro ao executar gameOverBlock: {e}");
            }
        }

        private async void HandleRespawnRequested(float countdownSeconds)
        {
            if (respawnBlock == null)
            {
                Debug.LogWarning("[FlowRouter] respawnBlock não atribuído.");
                return;
            }

            // Ajusta timeout do bloco para igualar a duração do countdown
            var originalTimeout = respawnBlock.timeoutSeconds;
            respawnBlock.timeoutSeconds = countdownSeconds;
            try
            {
                await respawnBlock.StartBlockAndWaitAsync(_cts.Token);
            }
            catch (Exception e)
            {
                Debug.LogError($"[FlowRouter] Erro ao executar respawnBlock: {e}");
            }
            finally
            {
                // Restaura configuração original
                respawnBlock.timeoutSeconds = originalTimeout;
            }
        }
    }
}