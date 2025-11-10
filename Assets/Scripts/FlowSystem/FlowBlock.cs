using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;

namespace FlowSystem
{
    public enum InputContext { None, UI, Gameplay }
    public enum State { Enable, Disable }
    public enum ChangeStateLogicalPoint { Start, End }
    public enum DelayType { Pre, Post }
    public enum EndMode { Any, All }

    [Serializable]
    public class ControlObject
    {
        public GameObject target;
        public State state = State.Enable;
        public ChangeStateLogicalPoint logicalPoint = ChangeStateLogicalPoint.Start;
        public DelayType delayType = DelayType.Pre;
        public float delayTime = 0f;
    }

    [Serializable]
    public class AnimationItem
    {
        public Animator animator;
        public string animationName;
        public bool waitForEnd = true;
        public string outcomeOnEnd = "AnimationEnd";
        public string id; // identificador estável para chamada por nome
    }

    [Serializable]
    public class ButtonItem
    {
        public Button button;
        public string outcomeOnClick;
        public bool triggersEnd = true;
        public string id; // identificador estável para chamada por nome
        // Opcional: tocar animação ao clicar e só encerrar após o fim
        public bool playAnimationOnClick = false;
        public Animator animatorOnClick;
        public string animationNameOnClick;
        public bool waitAnimationOnClickEnd = true;
        public string outcomeAfterPlayedAnimation = "ButtonClicked";
    }

    public class FlowBlock : MonoBehaviour
    {
        [Header("Contexto")] public InputContext inputContext = InputContext.None;
        [Header("Objetos")] public List<ControlObject> controlObjects = new();
        [Header("Animações")] public List<AnimationItem> animations = new();
        [Header("Timeline")] public PlayableDirector director; public bool waitForDirectorEnd = false; public string timelineEndOutcome = "TimelineEnd"; public string timelineId;
        [Header("Botões")] public List<ButtonItem> buttons = new();
        [Header("Política")] public EndMode endMode = EndMode.Any; public float timeoutSeconds = 0f; public bool autoStartOnEnable = false;

        public event Action<FlowBlock> OnBlockStart;
        public event Action<FlowBlock, string> OnBlockEnd; // outcomeKey
        public event Action<string> OnIntermediateAction; // telemetria/observabilidade interna do bloco

        public string LastOutcomeKey { get; private set; }

        UniTaskCompletionSource<string> _forceOutcomeTcs;

        void OnEnable()
        {
            if (autoStartOnEnable)
            {
                var cts = new CancellationTokenSource();
                _ = StartBlockAndWaitAsync(cts.Token);
            }
        }

        public void ForceOutcome(string key)
        {
            LastOutcomeKey = key;
            _forceOutcomeTcs ??= new UniTaskCompletionSource<string>();
            _forceOutcomeTcs.TrySetResult(key);
        }

        // ===== API para UnityEvents / integrações =====
        public void FinishBlock(string outcomeKey = "Completed")
        {
            ForceOutcome(outcomeKey);
        }

        public void PublishIntermediate(string key)
        {
            OnIntermediateAction?.Invoke(key);
        }

        public void PlayAnimationAtIndex(int index)
        {
            if (index < 0 || index >= animations.Count) return;
            var a = animations[index];
            if (a?.animator == null) return;
            if (!string.IsNullOrEmpty(a.animationName))
                a.animator.Play(a.animationName);
        }

        public void PlayAnimationById(string animationId)
        {
            if (string.IsNullOrEmpty(animationId)) return;
            var a = animations.Find(x => x != null && x.id == animationId);
            if (a?.animator == null) return;
            if (!string.IsNullOrEmpty(a.animationName))
                a.animator.Play(a.animationName);
        }

        public void PlayAnimatorByName(Animator animator, string stateName)
        {
            if (animator == null || string.IsNullOrEmpty(stateName)) return;
            animator.Play(stateName);
        }

        public async UniTask<string> StartBlockAndWaitAsync(CancellationToken ct)
        {
            try
            {
                OnBlockStart?.Invoke(this);
                ApplyInputContext(inputContext);
                await EnterPhaseAsync(ct);
                await SetupPhaseAsync(ct);
                string outcome = await AwaitEndPhaseAsync(ct);
                LastOutcomeKey = outcome;
                Exit();
                OnBlockEnd?.Invoke(this, outcome);
                return outcome;
            }
            catch (Exception e)
            {
                Debug.LogError($"[FlowBlock] Falha em {name}: {e}");
                Exit();
                LastOutcomeKey = "GENERIC_ERROR";
                OnBlockEnd?.Invoke(this, LastOutcomeKey);
                return LastOutcomeKey;
            }
        }

        // ===== Fases =====
        async UniTask EnterPhaseAsync(CancellationToken ct)
        {
            // Aplicar objetos marcados para Start (Pre/Post)
            foreach (var co in controlObjects)
            {
                if (co == null || co.target == null) continue;
                if (co.logicalPoint != ChangeStateLogicalPoint.Start) continue;

                if (co.delayType == DelayType.Pre)
                {
                    await ApplyObjectStateAsync(co, ct);
                }
                else // Post
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(Mathf.Max(0f, co.delayTime)), cancellationToken: ct);
                    await ApplyObjectStateAsync(co, ct);
                }
            }

            // Gatilhos de animação/timeline podem ser adicionados aqui conforme necessidade
        }

        UniTask SetupPhaseAsync(CancellationToken ct)
        {
            // Registrar ouvintes se preciso; as tarefas já cuidam do bind/unbind
            return UniTask.CompletedTask;
        }

        async UniTask<string> AwaitEndPhaseAsync(CancellationToken ct)
        {
            var tasks = new List<UniTask<string>>();

            // ForceOutcome
            if (_forceOutcomeTcs != null)
            {
                tasks.Add(_forceOutcomeTcs.Task);
            }

            // Botões
            foreach (var b in buttons)
            {
                if (b?.button == null) continue;
                if (b.triggersEnd)
                    tasks.Add(WaitButtonAsync(b, ct));
            }

            // Timeline
            if (director != null && waitForDirectorEnd)
            {
                tasks.Add(WaitDirectorAsync(ct));
            }

            // Animações
            foreach (var a in animations)
            {
                if (a?.animator == null || !a.waitForEnd) continue;
                tasks.Add(WaitAnimationAsync(a, ct));
            }

            // Timeout
            if (timeoutSeconds > 0f)
            {
                tasks.Add(TimeoutAsync(timeoutSeconds, ct));
            }

            if (tasks.Count == 0)
            {
                // Nada a esperar: finalizar com Completed
                return "Completed";
            }

            if (endMode == EndMode.Any)
            {
                var result = await UniTask.WhenAny(tasks);
                return result.result;
            }
            else
            {
                var results = await UniTask.WhenAll(tasks);
                return results.Length > 0 ? results[^1] : "Completed";
            }
        }

        // ===== Tarefas de Participantes =====
        async UniTask<string> WaitButtonAsync(ButtonItem b, CancellationToken ct)
        {
            var tcs = new UniTaskCompletionSource();
            void Handler() { tcs.TrySetResult(); }
            b.button.onClick.AddListener(Handler);
            try
            {
                await tcs.Task.AttachExternalCancellation(ct);

                // Se configurado para tocar animação e aguardar o fim antes de encerrar
                if (b.playAnimationOnClick && b.animatorOnClick != null)
                {
                    if (!string.IsNullOrEmpty(b.animationNameOnClick))
                    {
                        b.animatorOnClick.Play(b.animationNameOnClick);
                    }
                    if (b.waitAnimationOnClickEnd)
                    {
                        var ai = new AnimationItem
                        {
                            animator = b.animatorOnClick,
                            animationName = b.animationNameOnClick,
                            waitForEnd = true,
                            outcomeOnEnd = string.IsNullOrEmpty(b.outcomeAfterPlayedAnimation) ? (b.outcomeOnClick ?? "ButtonClicked") : b.outcomeAfterPlayedAnimation
                        };
                        var finalOutcome = await WaitAnimationAsync(ai, ct);
                        return finalOutcome;
                    }
                }

                // Caso contrário, encerra com o outcome do clique imediato
                return b.outcomeOnClick ?? "ButtonClicked";
            }
            finally
            {
                b.button.onClick.RemoveListener(Handler);
            }
        }

        async UniTask<string> WaitDirectorAsync(CancellationToken ct)
        {
            var tcs = new UniTaskCompletionSource<string>();
            void Handler(PlayableDirector _) { tcs.TrySetResult("TimelineEnd"); }
            director.stopped += Handler;
            try
            {
                await tcs.Task.AttachExternalCancellation(ct);
                return string.IsNullOrEmpty(timelineEndOutcome) ? "TimelineEnd" : timelineEndOutcome;
            }
            finally
            {
                director.stopped -= Handler;
            }
        }

        UniTask<string> WaitAnimationAsync(AnimationItem a, CancellationToken ct)
        {
            // Implementar via AnimationEvent/sinal/polling conforme projeto.
            // Placeholder retorna imediatamente; substitua por lógica real quando necessário.
            return UniTask.FromResult(string.IsNullOrEmpty(a.outcomeOnEnd) ? "AnimationEnd" : a.outcomeOnEnd);
        }

        async UniTask<string> TimeoutAsync(float seconds, CancellationToken ct)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(seconds), cancellationToken: ct);
            return "Timeout";
        }

        // ===== Utilitários =====
        async UniTask ApplyObjectStateAsync(ControlObject co, CancellationToken ct)
        {
            if (co.target == null) return;
            // Delay opcional antes de aplicar estado (já tratado acima)
            await UniTask.Yield(ct);
            bool enable = co.state == State.Enable;
            co.target.SetActive(enable);
        }

        void ApplyInputContext(InputContext ctx)
        {
            // Integre com seu InputRouter (ex.: SetUI/SetGameplay/SetNone)
            // Este método é um stub para implementação no projeto.
        }

        void Exit()
        {
            // Aplicar objetos marcados para End
            foreach (var co in controlObjects)
            {
                if (co == null || co.target == null) continue;
                if (co.logicalPoint != ChangeStateLogicalPoint.End) continue;

                bool enable = co.state == State.Enable;
                if (co.delayType == DelayType.Pre)
                {
                    co.target.SetActive(enable);
                }
                else
                {
                    // Nota: se desejar respeitar DelayTime em Exit, adapte para assíncrono externo
                    co.target.SetActive(enable);
                }
            }
        }
    }
}