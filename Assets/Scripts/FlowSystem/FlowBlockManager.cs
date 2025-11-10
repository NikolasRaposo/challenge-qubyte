using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace FlowSystem
{
    public class FlowBlockManager : MonoBehaviour
    {
        [Tooltip("Bloco inicial da sequência")] public FlowBlock initial;
        [Tooltip("Mapa de transições Outcome -> Próximo bloco")] public BlockGraph graph;

        CancellationTokenSource _cts;

        void OnEnable()
        {
            _cts = new CancellationTokenSource();
        }

        void OnDisable()
        {
            if (_cts != null)
            {
                _cts.Cancel();
                _cts.Dispose();
                _cts = null;
            }
        }

        async void Start()
        {
            if (initial == null || graph == null)
            {
                Debug.LogWarning("[FlowBlockManager] Inicial ou Graph não atribuídos.");
                return;
            }

            await RunAsync(_cts.Token);
        }

        public async UniTask RunAsync(CancellationToken ct)
        {
            var current = initial;
            while (current != null && !ct.IsCancellationRequested)
            {
                try
                {
                    string outcome = await current.StartBlockAndWaitAsync(ct);
                    current = graph.Resolve(current, outcome);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[FlowBlockManager] Erro no bloco {current?.name}: {e}");
                    current = graph.Resolve(current, "GENERIC_ERROR");
                }
            }
        }
    }
}