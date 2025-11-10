# FlowBlock — Documento Final V3 (Outcome + BlockGraph + UniTask)

Este documento consolida a especificação final do componente V3, agora chamado `FlowBlock`. A V3 adota `UniTask` (`async/await`) e transições por `Outcome` mapeadas via `BlockGraph` (ScriptableObject), priorizando desacoplamento, legibilidade e robustez.

## Resumo V3
- Nome do componente: `FlowBlock` (antes denominado `LogicalBlock`).
- Single‑active: apenas 1 bloco ativo por vez na cena.
- Stateless: blocos reentrantes, sem carregar estado de execuções anteriores.
- Resultado explícito: bloco encerra emitindo `OutcomeKey` (string) que descreve como terminou.
- Desacoplamento: orquestrador resolve o próximo bloco via `BlockGraph` usando `(blocoAtual, outcomeKey)`.
- Assíncrono moderno: ciclo de vida com `UniTask` e `CancellationToken`.
- Observabilidade: eventos de início/fim, logs e resumo de participantes no Inspector.
- Resiliência: `try/catch`, timeouts opcionais e regras de fallback no orquestrador.

## Impacto das Mudanças (Atual)
- Animações: só iniciam automaticamente as com `playOnEnter = true`; as demais são disparadas externamente (por `UnityEvent` ou pela API do `FlowBlock`). Isso evita múltiplos disparos simultâneos e dá controle fino.
- Espera de animações: `waitForEnd = true` participa da condição de término; se uma animação marcada como `waitForEnd` não for iniciada, o bloco pode aguardar indefinidamente. Configure com cuidado ou use `TimeoutSeconds`.
- Observabilidade mínima: use `OnBlockStart`, `OnBlockEnd` e `OnIntermediateAction(key)` apenas quando necessário. Evite coletar contexto de remetente/ações dentro do bloco; se precisar, use um observador externo.
- Botões pós‑clique: permanecem suportados; quando exigirem animação antes de terminar, prefira dividir em micro‑blocos em vez de misturar tudo em um único bloco.

ATENÇÃO: as ideias das versões anteriores (V1/V2) foram consolidadas e resumidas ao final em “Notas de Migração”. A V3 abaixo é a fonte de verdade para implementação.

## Decisão de Nome
- Preferência final: `FlowBlock` — comunica fluxo e orquestração de forma direta e concisa.
- Alias: `LogicalBlock` poderá permanecer como nome histórico no documento, porém o código novo deve adotar `FlowBlock`.

## Visão Geral da Arquitetura (V3)
- O `FlowBlock` define participantes (objetos, animações, timeline, botões, áudio/HUD) e uma política de término.
- Ao finalizar, o bloco emite um `OutcomeKey` (ex.: `"StartGame"`, `"OpenOptions"`, `"SceneReadyToGameplay"`).
- O `FlowBlockManager` recebe o `OutcomeKey` e consulta o `BlockGraph` para resolver o próximo bloco.
- Todo o ciclo de vida e composição de esperas usa `UniTask` (`await`), permitindo retornos de valor, cancelamento cooperativo e `try/catch`.

## Modelo de Dados (V3)
- `InputContext` (Enum): `None`, `UI`, `Gameplay`.
- `List<ControlObject>`: alvo, estado (`Enable/Disable`), ponto lógico (`Start/End`), `DelayType` (`Pre/Post`), `DelayTime`.
- `List<AnimationItem>`: `id`, `Animator`, `animationName`, `playOnEnter` (auto‑start no Enter), `waitForEnd`.
- `PlayableDirector` (+ sinalização): `timelineId`, `waitForDirectorEnd`.
- `List<ButtonItem>`: `id`, `Button`, `outcomeOnClick`, `triggersEnd`, `playAnimationOnClick` (opcional), `animatorOnClick`, `animationNameOnClick`, `waitAnimationOnClickEnd`, `outcomeAfterPlayedAnimation`.
- `List<AudioItem>`/`HudItem` (opcional): início/fim e participação no término.
- `EndMode` (Enum): `Any`, `All` — política de término.
- `float TimeoutSeconds` — 0 desabilita.
- `bool AutoStartOnEnable`.
- Observabilidade: nível de log (`Off`, `Errors`, `Info`, `Verbose`).
- Emissão: `OutcomeKey` final e evento `OnBlockEnd(FlowBlock, string outcomeKey)`.

## API Essencial (V3)
```csharp
using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;

public enum InputContext { None, UI, Gameplay }
public enum State { Enable, Disable }
public enum ChangeStateLogicalPoint { Start, End }
public enum DelayType { Pre, Post }
public enum EndMode { Any, All }

[Serializable]
public class ControlObject { public GameObject target; public State state = State.Enable; public ChangeStateLogicalPoint logicalPoint = ChangeStateLogicalPoint.Start; public DelayType delayType = DelayType.Pre; public float delayTime = 0f; }
[Serializable]
public class AnimationItem { public Animator animator; public string animationName; public bool waitForEnd = true; public string outcomeOnEnd = "AnimationEnd"; public string id; }
[Serializable]
public class ButtonItem {
    public Button button; public string outcomeOnClick; public bool triggersEnd = true; public string id;
    // Opcional: tocar animação ao clicar e só encerrar após o fim
    public bool playAnimationOnClick = false; public Animator animatorOnClick; public string animationNameOnClick; public bool waitAnimationOnClickEnd = true; public string outcomeAfterPlayedAnimation = "ButtonClicked";
}

public class FlowBlock : MonoBehaviour {
    [Header("Contexto")] public InputContext inputContext = InputContext.None;
    [Header("Objetos")] public List<ControlObject> controlObjects = new();
    [Header("Animações")] public List<AnimationItem> animations = new();
    [Header("Timeline")] public PlayableDirector director; public bool waitForDirectorEnd = false; public string timelineEndOutcome = "TimelineEnd"; public string timelineId;
    [Header("Botões")] public List<ButtonItem> buttons = new();
    [Header("Política")] public EndMode endMode = EndMode.Any; public float timeoutSeconds = 0f; public bool autoStartOnEnable = false;

    public event Action<FlowBlock> OnBlockStart;
    public event Action<FlowBlock, string> OnBlockEnd; // outcomeKey
    public event Action<string> OnIntermediateAction; // telemetria interna

    public string LastOutcomeKey { get; private set; }

    UniTaskCompletionSource<string> _forceOutcomeTcs;

    void OnEnable() { if (autoStartOnEnable) { var cts = new CancellationTokenSource(); _ = StartBlockAndWaitAsync(cts.Token); } }

    public void ForceOutcome(string key) { LastOutcomeKey = key; _forceOutcomeTcs ??= new UniTaskCompletionSource<string>(); _forceOutcomeTcs.TrySetResult(key); }
    public void FinishBlock(string outcomeKey = "Completed") { ForceOutcome(outcomeKey); }
    public void PublishIntermediate(string key) { OnIntermediateAction?.Invoke(key); }

    public async UniTask<string> StartBlockAndWaitAsync(CancellationToken ct) {
        try {
            OnBlockStart?.Invoke(this);
            ApplyInputContext(inputContext);
            await EnterPhaseAsync(ct);
            await SetupPhaseAsync(ct);
            string outcome = await AwaitEndPhaseAsync(ct);
            LastOutcomeKey = outcome;
            Exit();
            OnBlockEnd?.Invoke(this, outcome);
            return outcome;
        } catch (Exception e) {
            Debug.LogError($"[FlowBlock] Falha em {name}: {e}");
            Exit();
            LastOutcomeKey = "GENERIC_ERROR";
            OnBlockEnd?.Invoke(this, LastOutcomeKey);
            return LastOutcomeKey;
        }
    }

    async UniTask EnterPhaseAsync(CancellationToken ct) {
        foreach (var co in controlObjects) {
            if (co == null || co.target == null) continue;
            if (co.logicalPoint != ChangeStateLogicalPoint.Start) continue;
            if (co.delayType == DelayType.Pre) { await ApplyObjectStateAsync(co, ct); }
            else { await UniTask.Delay(TimeSpan.FromSeconds(Mathf.Max(0f, co.delayTime)), cancellationToken: ct); await ApplyObjectStateAsync(co, ct); }
        }
    }
    UniTask SetupPhaseAsync(CancellationToken ct) => UniTask.CompletedTask;

    async UniTask<string> AwaitEndPhaseAsync(CancellationToken ct) {
        var tasks = new List<UniTask<string>>();
        if (_forceOutcomeTcs != null) tasks.Add(_forceOutcomeTcs.Task);
        foreach (var b in buttons) { if (b?.button == null) continue; if (b.triggersEnd) tasks.Add(WaitButtonAsync(b, ct)); }
        if (director != null && waitForDirectorEnd) { tasks.Add(WaitDirectorAsync(ct)); }
        foreach (var a in animations) { if (a?.animator == null || !a.waitForEnd) continue; tasks.Add(WaitAnimationAsync(a, ct)); }
        if (timeoutSeconds > 0f) tasks.Add(TimeoutAsync(timeoutSeconds, ct));

        if (tasks.Count == 0) return "Completed";
        if (endMode == EndMode.Any) { var result = await UniTask.WhenAny(tasks); return result.result; }
        else { var results = await UniTask.WhenAll(tasks); return results.Length > 0 ? results[^1] : "Completed"; }
    }

    async UniTask<string> WaitButtonAsync(ButtonItem b, CancellationToken ct) {
        var tcs = new UniTaskCompletionSource(); void Handler() { tcs.TrySetResult(); }
        b.button.onClick.AddListener(Handler);
        try {
            await tcs.Task.AttachExternalCancellation(ct);
            if (b.playAnimationOnClick && b.animatorOnClick != null) {
                if (!string.IsNullOrEmpty(b.animationNameOnClick)) { b.animatorOnClick.Play(b.animationNameOnClick); }
                if (b.waitAnimationOnClickEnd) {
                    var ai = new AnimationItem { animator = b.animatorOnClick, animationName = b.animationNameOnClick, waitForEnd = true, outcomeOnEnd = string.IsNullOrEmpty(b.outcomeAfterPlayedAnimation) ? (b.outcomeOnClick ?? "ButtonClicked") : b.outcomeAfterPlayedAnimation };
                    var finalOutcome = await WaitAnimationAsync(ai, ct);
                    return finalOutcome;
                }
            }
            return b.outcomeOnClick ?? "ButtonClicked";
        } finally { b.button.onClick.RemoveListener(Handler); }
    }

    async UniTask<string> WaitDirectorAsync(CancellationToken ct) {
        var tcs = new UniTaskCompletionSource<string>(); void Handler(PlayableDirector _) { tcs.TrySetResult("TimelineEnd"); }
        director.stopped += Handler;
        try { await tcs.Task.AttachExternalCancellation(ct); return string.IsNullOrEmpty(timelineEndOutcome) ? "TimelineEnd" : timelineEndOutcome; }
        finally { director.stopped -= Handler; }
    }

    UniTask<string> WaitAnimationAsync(AnimationItem a, CancellationToken ct) { return UniTask.FromResult(string.IsNullOrEmpty(a.outcomeOnEnd) ? "AnimationEnd" : a.outcomeOnEnd); }
    async UniTask<string> TimeoutAsync(float seconds, CancellationToken ct) { await UniTask.Delay(TimeSpan.FromSeconds(seconds), cancellationToken: ct); return "Timeout"; }

    async UniTask ApplyObjectStateAsync(ControlObject co, CancellationToken ct) { if (co.target == null) return; await UniTask.Yield(ct); bool enable = co.state == State.Enable; co.target.SetActive(enable); }
    void ApplyInputContext(InputContext ctx) { /* integrar com seu InputRouter */ }
    void Exit() {
        foreach (var co in controlObjects) {
            if (co == null || co.target == null) continue; if (co.logicalPoint != ChangeStateLogicalPoint.End) continue;
            bool enable = co.state == State.Enable; if (co.delayType == DelayType.Pre) { co.target.SetActive(enable); } else { co.target.SetActive(enable); }
        }
    }
}
```

## Outcome e Emissão
- Cada participante pode gerar um `OutcomeKey` simbólico; o bloco decide quais participam do término.
- Padrões comuns:
  - Botões: `StartGame`, `OpenOptions`, `Quit`.
  - Timeline: `CinematicEnded`, `SkipCinematic`.
  - Animações: `LogoFinished`, `HudClosed`.
- `ForceOutcome(string key)`: utilitário opcional para encerrar imediatamente com um `OutcomeKey`.

### Telemetria de Intermediários
- Intermediários não encerram o bloco e não produzem outcomes para o `BlockGraph`.
- Para rastreio/observabilidade interna, usar `OnIntermediateAction(string key)` ou chamar `PublishIntermediate(key)` via `UnityEvent`.
- Ex.: `OnAnimationEnd` (via `AnimationEndHandler` + `NotifyAnimationEndSMB`) chama `flowBlock.PublishIntermediate("IntroAnimFinished")`.

## Regras de Micro‑Blocos
- Um bloco, uma responsabilidade: escolha botão OU animação OU timeline como participante principal.
- Evite misturar muitos participantes num único bloco; se precisar, divida em dois ou mais micro‑blocos encadeados pelo `BlockGraph`.
- Outcomes curtos e estáveis: use nomes consistentes como `StartGame`, `OpenOptions`, `LogoFinished`, `TimelineEnd.CinematicA`.
- Preferir blocos de animação dedicados para iniciar e/ou esperar a animação, emitindo um único outcome claro.
- Botões terminam o bloco diretamente; se houver animação pós‑clique, mova a animação para um bloco seguinte.
- Timeline em bloco próprio: aguarde `PlayableDirector.stopped` e emita outcome único.
- Intermediários só quando necessário: use `PublishIntermediate("AlgumaEtapa")` para observabilidade mínima; não carregue contexto pesado.
- Mantenha o `FlowBlock` enxuto: sem acoplamentos a componentes externos; conecte via `UnityEvent` no Inspector.

### Convenções de Outcomes
- Terminadores padrão: `StartGame`, `OpenOptions`, `Quit`, `Completed`.
- Animações: `LogoFinished`, `AnimEnd.IntroFadeIn`, ou `HudClosed`.
- Timeline: `TimelineEnd` ou `TimelineEnd.CinematicA` quando houver múltiplas.
- Fallbacks: `Timeout` e `GENERIC_ERROR` para erros.

## Exemplo curto: Gameplay com pausa por input e HUD reativa
- Objetivo: manter `GameplayBlock` longo e enxuto, usar micro‑blocos só quando o fluxo exigir.

### GameplayBlock (mínimo)
- `inputContext = Gameplay`
- `controlObjects`: `HUDRoot → Enable @Start`, `HUDRoot → Disable @End`
- `buttons`: vazio (pausa será por input, não por botão de UI)
- `animations/timeline`: vazio (usar micro‑blocos dedicados quando gatearem)
- `EndMode = Any`, `timeoutSeconds = 0`

### Pausa por Input (sem botão)
- Adicione um componente de escuta de input que emite o outcome diretamente no bloco atual.
```csharp
using UnityEngine;
using UnityEngine.InputSystem;
using FlowSystem;

public class PauseInputListener : MonoBehaviour {
    public FlowBlock flowBlock;                 // atribua o bloco atual
    public InputActionReference pauseAction;    // ex.: Gameplay/Pause, Start (gamepad), Esc (keyboard)

    void OnEnable() {
        var action = pauseAction?.action;
        if (action != null) action.performed += OnPausePerformed;
    }
    void OnDisable() {
        var action = pauseAction?.action;
        if (action != null) action.performed -= OnPausePerformed;
    }
    void OnPausePerformed(InputAction.CallbackContext ctx) {
        flowBlock?.ForceOutcome("RequestPause");
    }
}
```
- No `FlowBlock`, mantenha `buttons` vazio; a pausa fica explícita pela presença do `PauseInputListener` apontando para o bloco e pelo outcome `RequestPause`.

### HUD reativa (dano/coleta) fora do FlowBlock
- Use um bus de eventos simples; o HUD assina e toca animações sem participar do término do bloco.
```csharp
using System;
using UnityEngine;

public static class GameplayEvents {
    public static event Action<int> OnDamageTaken;
    public static event Action<string> OnItemCollected;
    public static void RaiseDamageTaken(int amount) => OnDamageTaken?.Invoke(amount);
    public static void RaiseItemCollected(string itemId) => OnItemCollected?.Invoke(itemId);
}

public class HUDController : MonoBehaviour {
    [SerializeField] Animator hudAnimator;
    void OnEnable() {
        GameplayEvents.OnDamageTaken += HandleDamageTaken;
        GameplayEvents.OnItemCollected += HandleItemCollected;
    }
    void OnDisable() {
        GameplayEvents.OnDamageTaken -= HandleDamageTaken;
        GameplayEvents.OnItemCollected -= HandleItemCollected;
    }
    void HandleDamageTaken(int amount) { hudAnimator?.Play("DamageFlash"); }
    void HandleItemCollected(string itemId) { hudAnimator?.Play("ItemPickupPulse"); }
}
```

### Micro‑blocos quando necessário
- `PauseMenuBlock`: modal que bloqueia input, termina com `ResumeGameplay`.
- `ItemCardBlock`: apresentação de item raro; termina com `DismissItemCard`.
- `TransitionFadeBlock`: controla fade/transition; termina com `TransitionComplete`.

### Regras no BlockGraph (exemplo)
- `(GameplayBlock, "RequestPause") -> PauseMenuBlock`
- `(PauseMenuBlock, "ResumeGameplay") -> GameplayBlock`
- `(GameplayBlock, "GameOver") -> GameOverBlock`



## BlockGraph (ScriptableObject)
```csharp
[CreateAssetMenu(menuName: "Flow/BlockGraph", fileName: "BlockGraph")]
public class BlockGraph : ScriptableObject {
    [Serializable]
    public struct Rule { public FlowBlock from; public string outcomeKey; public FlowBlock to; }
    public List<Rule> rules = new();

    public FlowBlock Resolve(FlowBlock from, string outcomeKey) {
        for (int i = 0; i < rules.Count; i++) {
            var r = rules[i];
            if (r.from == from && r.outcomeKey == outcomeKey) return r.to;
        }
        return null;
    }
}
```

## FlowBlockManager (V3, UniTask)
```csharp
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class FlowBlockManager : MonoBehaviour {
    public FlowBlock initial;
    public BlockGraph graph;

    public async UniTask RunAsync(CancellationToken ct) {
        var current = initial;
        while (current != null && !ct.IsCancellationRequested) {
            try {
                string outcome = await current.StartBlockAndWaitAsync(ct);
                current = graph.Resolve(current, outcome);
            } catch (System.Exception e) {
                Debug.LogError($"[FlowManager] Erro no bloco {current?.name}: {e}");
                current = graph.Resolve(current, "GENERIC_ERROR");
            }
        }
    }
}
```

## Single‑Active e Stateless
- Single‑active garantido pelo `FlowBlockManager`: apenas um `StartBlockAndWaitAsync` por vez.
- Stateless: blocos não retomam corrotinas/animadores anteriores; cada execução faz setup idempotente e teardown limpo.
- Pausa por swap: `Gameplay` → `PauseMenu` → outcome `ResumeGameplay` → novo `Gameplay` configurado por `ConfigureGameplay`.

## Mapeamento de Blocos (exemplo)
- `EmpresaNomeBlock`: mostra logo, outcome `LogoFinished` → `MainMenu`.
- `MainMenuBlock`: outcome `StartGame` → `Loading`; outcome `OpenOptions` → `OptionsMenu`.
- `LoadingBlock`: outcomes `SceneReadyToGameplay`/`SceneReadyToCinematic`.
- `IntroCinematicBlock`: outcome `CinematicEnded` → `Gameplay`.
- `ConfigureGameplayBlock`: prepara player/câmeras/contadores → outcome `Configured` → `Gameplay`.
- `GameplayBlock`: outcome `RequestPause` → `PauseMenu`.
- `PauseMenuBlock`: outcome `ResumeGameplay` → `ConfigureGameplay`/`Gameplay`; outcome `GoToMainMenu` → `MainMenu`.

## Integrações
- Input System: `ApplyInputContext` alterna mapas `UI`/`Gameplay`/`None` via `InputRouter`.
- Timeline: `PlayableDirector.stopped` e `SignalReceiver` como participantes.
- Animação: preferir `AnimationEvent`/sinal ao polling; se polling for necessário, encapsular como tarefa.
  - Para fins intermediários (não-terminantes): usar `AnimationEndHandler` e `NotifyAnimationEndSMB` para acionar `UnityEvent`s.
  - Para acionar o próprio bloco: conectar `UnityEvent` ao `FlowBlock` (métodos `PlayAnimationAtIndex/ById`, `PublishIntermediate`, `FinishBlock`).
- Áudio/HUD: expor itens com `startOnEnter`/`stopOnExit` e participar do término quando fizer sentido (ex.: fade-out).

## Observabilidade e Editor
- Eventos: `OnBlockStart(FlowBlock)`, `OnBlockEnd(FlowBlock, string outcomeKey)`.
- Inspector: resumo de participantes, quem participa do término, botões de `Start`, `ForceOutcome`, `Cancel`.
- Logs com níveis e identificação clara de bloco/participante.

### Telemetria Rica (payload)
- Além de `OnIntermediateAction(string key)`, o `FlowBlock` expõe `OnIntermediateRich(IntermediateEvent evt)` para telemetria com contexto.
- Use `PublishIntermediate(key, sender, actions)` para enviar:
  - `key`: identificador do evento intermediário.
  - `sender`: quem publicou (ex.: `AnimationEndHandler`).
  - `actions`: rótulos das ações disparadas em conjunto.
- Payload sugerido:
```csharp
public class IntermediateEvent {
    public string key;
    public UnityEngine.Object sender;
    public List<string> actions; // rótulos das ações disparadas
    public long timestampUnixMs; // carimbo temporal em UTC
}
```
- `AnimationEndHandler` pode inspecionar ouvintes persistentes do `UnityEvent` (`GetPersistentEventCount/Target/MethodName`) para montar `actions`, além de aceitar rótulos extras manuais.

## Resiliência e Erros
- `try/catch` no `FlowBlock` e no `FlowBlockManager` com fallback `GENERIC_ERROR`.
- `CancellationToken` integra mudança de cena, `CancelBlock()` e desativação do GameObject.
- `TimeoutSeconds` evita deadlocks; participantes `null` são logados e ignorados.

## Testes (V3)
- Unidade: composição `Any/All` com `WhenAny/WhenAll`, emissão correta de outcomes, timeouts, cancelamento.
- Integração: `PlayableDirector`, `SignalReceiver`, `AnimationEvent`, `Button.onClick` via `OnClickAsync`.
  - Eventos intermediários: verificação de emissão `OnIntermediateAction` sem interferir na transição.
- Fluxo: simular sequência completa usando `BlockGraph` e verificar transições.

## Notas de Migração (V1/V2 → V3)
- V1 (acoplado, corrotinas) e V2 (desacoplado, corrotinas) são legados; manter apenas como referência histórica.
- Substituir helpers `StartBlockAndWait(IEnumerator)` por `StartBlockAndWaitAsync(UniTask<string>)` retornando `OutcomeKey`.
- Centralizar transições no `BlockGraph`; blocos não conhecem próximos blocos.
- Adotar `CancellationToken` e `try/catch` padrão.

## Checklist de Implementação
- Criar `FlowBlock.cs` com API `StartBlockAndWaitAsync(CancellationToken)` retornando `OutcomeKey`.
- Implementar participantes mínimos necessários (UI, timeline, botões) e política `EndMode`.
- Criar `BlockGraph.asset` com regras `(from, outcomeKey) -> to`.
- Implementar `FlowBlockManager.cs` (`RunAsync`) com `try/catch` e fallback.
- Integrar `InputRouter` em `ApplyInputContext`.
- Adicionar Custom Inspector com resumo e ações.
- Escrever testes de fluxo e participantes críticos.

---
As seções legadas abaixo foram mantidas apenas como referência histórica. Para implementação, use exclusivamente a especificação V3 acima.
- Centralizar regras de um bloco: contexto de input, ativação/desativação de objetos, animações, timelines, botões.
- Tornar o bloco observável: registrar participantes e eventos de início/fim, com logs e sinais.
- Padronizar o ciclo de vida: `Enter → Setup → AwaitEnd → Exit`.
- Minimizar scripts espalhados e facilitar manutenção e testes.

## Nome do Componente
- Preferência: `LogicalBlock`.
  - Curto, direto e comunica exatamente o papel do componente.
  - Alternativas: `FlowBlock` (bom também), `LogicalSequenceUnit` (descritivo, porém longo), `ExtraLogicalBlock` (vago sobre o que é “extra”), `StableLogicBlock` (foca em estabilidade, não na função).

## Responsabilidades
- Receber configuração serializada do bloco (input, objetos, animações, timeline, botões, política de término).
- Executar o ciclo de vida do bloco e só liberar o fluxo após a condição de término ser satisfeita.
- Registrar os participantes e expor um resumo (para inspeção no Editor e debug em tempo de execução).
- Publicar eventos: `OnBlockStart`, `OnBlockEnd`, `OnParticipantEvent` (opcional).

## Ciclo de Vida
1. Enter
   - Define `InputContext`.
   - Aplica ativação/desativação `Pre` para objetos marcados com `Start`.
   - Dispara animações/timeline quando configurado como “start-on-enter”.
2. Setup
   - Registra ouvintes: `OnAnimationEnd`, `PlayableDirector.stopped`, `Button.onClick`.
   - Avalia atrasos (`DelayTime`) por participante.
3. AwaitEnd
   - Aguarda condições conforme política de término: `Any` (OR) ou `All` (AND).
4. Exit
   - Aplica ativação/desativação `Post` para objetos marcados com `End`.
   - Reseta/limpa ouvintes e restaura `InputContext` se necessário.

## Modelo de Dados (Serializado)
- `InputContext` (Enum): `None`, `UI`, `Gameplay`.
- `List<ControlObject>`
  - `GameObject target`
  - `State` (Enum): `Enable`, `Disable`
  - `ChangeStateLogicalPoint` (Enum): `Start`, `End`
  - `DelayType` (Enum): `Pre`, `Post`
  - `float DelayTime`
- `bool HasAnimations`
  - `List<AnimationItem>`
    - `Animator animator`
    - `string animationName`
    - `bool waitForEnd` (se verdadeiro, participa da condição de término)
- `bool HasTimeline`
  - `PlayableDirector director`
  - `List<Object> signalReceivers` (referências a `SignalReceiver` ou objetos que recebem sinais)
  - `bool waitForDirectorEnd`
- `bool HasButtons`
  - `List<ButtonItem>`
    - `UnityEngine.UI.Button button`
    - `bool triggersEnd` (participa da condição de término)
- `EndMode` (Enum): `Any`, `All` — política de término do bloco.
- `float TimeoutSeconds` (0 para desabilitar) — fallback se nada terminar.
- `bool AutoStartOnEnable` — útil para blocos simples que disparam ao habilitar.

## APIs Sugeridas
- Métodos
  - `void StartBlock()` — inicia o ciclo `Enter/Setup/AwaitEnd`.
  - `void CancelBlock()` — cancela aguardas e chama `Exit` imediato.
  - `void ForceEnd()` — marca condições satisfeitas e avança para `Exit`.
  - `void PauseBlock()`/`ResumeBlock()` (opcional) — pausa/resume animações/timeline.
- Eventos
  - `event Action<LogicalBlock> OnBlockStart`
  - `event Action<LogicalBlock> OnBlockEnd`
  - `event Action<string, UnityEngine.Object> OnParticipantEvent` (canal genérico para debug/telemetria)

## Esqueleto do Componente (C#)
```csharp
public enum InputContext { None, UI, Gameplay }
public enum State { Enable, Disable }
public enum ChangeStateLogicalPoint { Start, End }
public enum DelayType { Pre, Post }
public enum EndMode { Any, All }

[Serializable]
public class ControlObject {
    public GameObject target;
    public State state;
    public ChangeStateLogicalPoint logicalPoint;
    public DelayType delayType;
    public float delayTime;
}

[Serializable]
public class AnimationItem {
    public Animator animator;
    public string animationName;
    public bool waitForEnd = true;
}

[Serializable]
public class ButtonItem {
    public UnityEngine.UI.Button button;
    public bool triggersEnd = true;
}

public class LogicalBlock : MonoBehaviour {
    [Header("Contexto")] public InputContext inputContext = InputContext.None;
    [Header("Objetos") ] public List<ControlObject> controlObjects = new();
    [Header("Animações")] public bool hasAnimations; public List<AnimationItem> animations = new();
    [Header("Timeline") ] public bool hasTimeline; public PlayableDirector director; public List<UnityEngine.Object> signalReceivers = new();
    [Header("Botões") ] public bool hasButtons; public List<ButtonItem> buttons = new();
    [Header("Política") ] public EndMode endMode = EndMode.Any; public float timeoutSeconds = 0f; public bool autoStartOnEnable = false;

    public event Action<LogicalBlock> OnBlockStart;
    public event Action<LogicalBlock> OnBlockEnd;

    Coroutine running;

    void OnEnable() { if (autoStartOnEnable) StartBlock(); }
    public void StartBlock() { if (running == null) running = StartCoroutine(Run()); }
    public void CancelBlock() { if (running != null) { StopCoroutine(running); running = null; Exit(); } }
    public void ForceEnd() { _endRequested = true; }

    bool _endRequested;

    IEnumerator Run() {
        OnBlockStart?.Invoke(this);
        ApplyInputContext(inputContext);
        yield return EnterPhase();
        yield return SetupPhase();
        yield return AwaitEndPhase();
        Exit();
        OnBlockEnd?.Invoke(this);
        running = null;
    }

    void Exit() { /* aplicar objetos Post/End, limpar ouvintes, restaurar input se preciso */ }
    IEnumerator EnterPhase() { /* aplicar objetos Pre/Start, gatilhar animações/timeline */ yield break; }
    IEnumerator SetupPhase() { /* registrar eventos: anim end, director.stopped, button.onClick */ yield break; }
    IEnumerator AwaitEndPhase() { /* aguardar conforme EndMode ou timeout/_endRequested */ yield break; }
    void ApplyInputContext(InputContext context) { /* integrar com seu sistema de Input */ }
}
```

Notas:
- O uso de `Coroutine` mantém dependências mínimas; pode-se migrar para `async/await` ou `UniTask` se já houver no projeto.
- `ApplyInputContext` deve chamar seu gerenciador de input (ex.: desativar mapas de Gameplay e ativar UI no `MainMenu`).
- Para aguardar fim de animação, considerar:
  - Event de `AnimationEvent` no último frame.
  - `Animator.GetCurrentAnimatorStateInfo` + polling com `normalizedTime >= 1f`.
  - Parâmetros/Triggers e camada específica para UI.
- Para timeline, usar `director.stopped` e/ou sinal custom via `SignalReceiver`.

## Integração com State Machine / Sequenciador
- Um `SequenceOrchestrator` pode possuir referência a vários `LogicalBlock`s e executar em ordem:
  - `EmpresaNome → MainMenu → Loading → (GamePlay | Cinemática)`.
- Pseudocódigo:
```csharp
public class SequenceOrchestrator : MonoBehaviour {
    public LogicalBlock empresaNome, mainMenu, loading, gameplay, cinematica;
    public bool goToCinematic;

    IEnumerator Start() {
        yield return empresaNome.StartBlockAndWait();
        yield return mainMenu.StartBlockAndWait();
        yield return loading.StartBlockAndWait();
        yield return (goToCinematic ? cinematica.StartBlockAndWait() : gameplay.StartBlockAndWait());
    }
}
```
- Sugerir `StartBlockAndWait()` como helper que inicia e espera `OnBlockEnd`.

## Observabilidade e Editor
- Inspector organizado por seções, com resumo de participantes (objetos, animators, timeline, botões) e flags de participação no término.
- Logs opcionais com níveis: `Off`, `Errors`, `Info`, `Verbose`.
- Botão `Simular End` no Inspector para testar fluxo sem depender de animações.
- Gizmos (opcional) para destacar objetos controlados.

## Resiliência e Falhas
- Null-checks para `Animator`, `PlayableDirector`, `Button` e `GameObject` faltantes.
- Timeout configurável para evitar deadlocks.
- “Best-effort”: se um participante falhar, logar e continuar quando a política permitir (`EndMode.Any`).
- Parada limpa ao trocar de cena ou desabilitar o GameObject.

## Testes
- Unidade
  - Política `EndMode.Any` e `All` com combinações de participantes.
  - Aplicação de `ControlObject` nas fases `Start`/`End` e `Pre`/`Post`.
  - Timeout dispara `Exit` e eventos corretamente.
- Integração
  - Com `PlayableDirector` e sinais.
  - Com `Animator` e finalização por evento/estado.
  - Com `Button` e múltiplos cliques.

## Roadmap
- Versão 1: Componente básico com corrotinas, inspector simples, eventos.
- Versão 2: Custom Editor rico, painel de depuração em runtime, `StartBlockAndWait()` helper.
- Versão 3: Extensões para `async/await`/`UniTask`, e integração direta com seu state machine global.

## Opinião Sincera
- A ideia é excelente para reduzir dispersão de lógica e consolidar responsabilidades por “telas/blocos” — especialmente útil em jogos com muitas transições visuais e de input.
- Recomendo `LogicalBlock` (ou `FlowBlock`) pelo equilíbrio de clareza e concisão. Evite nomes muito longos ou pouco específicos.
- O ganho vem da previsibilidade e das integrações padronizadas: sempre saber onde configurar input, quem ativa/desativa objetos e qual é a política de término.
- Invista em um inspector claro e em relatórios de participantes; isso evita “lógica invisível” e facilita onboarding/manutenção.

---
## Princípios e Invariantes (Single‑Active)
- Sempre existir apenas 1 `LogicalBlock` ativo por vez na cena.
- O bloco ativo controla integralmente seu `InputContext` e os objetos participantes; nenhuma lógica externa deve alterar esses aspectos enquanto o bloco estiver rodando.
- Toda interferência externa deve ser encapsulada como participante do bloco (botão, trigger, sinal, animação), para manter visibilidade e previsibilidade.
 
 ## Blocos Sem Estado (Stateless)
 - Cada bloco é reentrante e não carrega informações da execução anterior.
 - Setup e teardown devem ser idempotentes: repetir o bloco não deve duplicar efeitos nem resetar indevidamente estados globais.
 - Persistência fica fora do bloco (ex.: `GameManager`, `GameStats`, `AudioService`, `InputRouter`). O bloco consome essas APIs, não guarda cópias locais.
 - Benefícios:
   - Simplifica Pause/Resume como troca de bloco (swap), sem retomar corrotinas/animadores em andamento.
   - Facilita testes e repetição de blocos (ex.: `DialogueBlock` e `GameplayBlock` podem ser executados várias vezes).
   - Evita acoplamento entre blocos e permite composição livre.

## Contratos de Transição (EndThisBlock / StartAnotherBlock)
- Cada bloco define sua política de término (`EndMode.Any/All`, timeout, eventos).
- Ao completar, dispara `OnBlockEnd` e comunica ao orquestrador qual próximo bloco iniciar.
- Transições suportadas:
  - `NextFixed`: próximo bloco pré-configurado.
  - `Branch`: decide em runtime (ex.: MainMenu → Loading → [GamePlay | Cinemática]).
  - `Return`: opcional se houver um stack (não usado no modo single‑active puro; útil para Pause).
- Recomendação: armazenar a transição como `BlockTransition` (tipo + referência), evitando lógica espalhada em scripts soltos.
 
 ### Alternativa Desacoplada: Outcome + TransitionMap (ScriptableObject)
 - Em vez de o bloco conhecer o próximo bloco, ele apenas emite um “resultado” simbólico (Outcome) ao terminar.
 - O orquestrador consulta um `TransitionMap` (SO) que mapeia `(bloco, outcomeKey)` para o próximo bloco.
 - Vantagens: desacoplamento total; blocos não referenciam outros blocos; fluxo definido centralmente.
 
 ```csharp
 public struct BlockOutcome { public string key; }
 
 // Evento detalhado opcional no LogicalBlock
 // public event Action<LogicalBlock, BlockOutcome> OnBlockEndDetailed;
 
 // ScriptableObject para mapear transições
 [CreateAssetMenu(menuName: "Flow/BlockGraph", fileName: "BlockGraph")]
 public class BlockGraph : ScriptableObject {
     [Serializable]
     public struct Rule { public LogicalBlock from; public string outcomeKey; public LogicalBlock to; }
     public List<Rule> rules = new();
 
     public LogicalBlock Resolve(LogicalBlock from, string outcomeKey) {
         for (int i = 0; i < rules.Count; i++) {
             var r = rules[i];
             if (r.from == from && r.outcomeKey == outcomeKey) return r.to;
         }
         return null;
     }
 }
 ```
 
 Exemplos de outcomes:
 - MainMenu: `StartGame`, `OpenOptions`, `Quit`.
 - Loading: `SceneReadyToCinematic`, `SceneReadyToGameplay`.
 - Pause: `ResumeGameplay`, `GoToMainMenu`.

### Interface sugerida
```csharp
public enum TransitionKind { NextFixed, Branch, None }
public struct BlockTransition {
    public TransitionKind kind;
    public LogicalBlock next;
    public Func<LogicalBlock> branchSelector; // usado quando kind == Branch
}

public interface ILogicalBlockTransition {
    BlockTransition GetTransition();
}
```

## Orquestrador Single‑Active
```csharp
public class LogicalBlockManager : MonoBehaviour {
    [Tooltip("Bloco inicial")] public LogicalBlock initial;
    LogicalBlock current;

    public IEnumerator Run() {
        current = initial;
        while (current != null) {
            yield return current.StartBlockAndWait();
            var transition = current.GetTransition();
            current = transition.kind switch {
                TransitionKind.NextFixed => transition.next,
                TransitionKind.Branch    => transition.branchSelector?.Invoke(),
                _ => null
            };
        }
    }
}
```
- Invariantes do manager:
  - Garante single‑active: só chama `StartBlockAndWait()` de um bloco por vez.
  - Recebe `OnBlockEnd` e decide próximo bloco conforme `BlockTransition`.
  - Opcionalmente aplica políticas globais (ex.: fallback para `MainMenu` se ocorrer falha).
 
 ### Pause sem Stack (Swap Semantics)
 - Fluxo simples e compatível com blocos sem estado:
   - `GameplayBlock` roda normalmente.
   - Evento de pausa desencadeia transição para `PauseMenuBlock` (Swap: termina o `GameplayBlock` e inicia `PauseMenuBlock`).
   - `PauseMenuBlock` emite outcome `ResumeGameplay` → orquestrador inicia um novo `GameplayBlock` (setup de HUD, música, etc.).
 - Pro:
   - Implementação simples, previsível, sem “retomar corrotinas”.
   - Blocos permanecem independentes e idempotentes.
 - Contra:
   - Não retoma animações/timelines no ponto exato (se isso for requisito de UX, considerar “Stack Manager” no futuro).

## Mapeamento dos Blocos do Projeto
- `EmpresaNomeBlock`
  - `SetInputContext(None)` → `ShowNomeEmpresaUI` → animação de logo → animação end → `EndThisBlock/StartAnotherBlock(MainMenu)`.
- `MainMenuBlock`
  - `ShowMainMenuUI` → animação inicial dos botões → animação termina → `SetInputContext(UI)` → `WaitForPlayerReaction` (ex.: `ClickStartButton`) → animação de saída do menu → `StartAnotherBlock(Loading)`.
- `LoadingBlock`
  - `SetInputContext(None)` → `ShowLoadingUI` → loop do ícone → `LoadingAssets/Scene` → `SceneLoaded` → animação de fim → `StartAnotherBlock(Branch: GamePlay | Cinemática)`.
- `IntroCinematicBlock`
  - `SetInputContext(UI)` → `Start Timeline` → sinais: `Skip?`, `PreparePlayer` → `TimelineEnd` → `StartAnotherBlock(GamePlay)`.
- `GamePlayBlock`
  - `SetInputContext(Gameplay)` → `Show HUD` → `AnimationsShowHud` → `StartBackgroundMusic` → `WaitForPlayerReaction` (ex.: trigger de início) → `StopBackgroundMusic` → `EndThisBlock/StartAnotherBlock(…)`.
 - `ConfigureGameplayBlock`
   - Posiciona/ativa jogador, configura câmeras, reseta contadores necessários e prepara `GameManager`/`GameStats` → `EndThisBlock/StartAnotherBlock(GamePlay)`.
- `PauseMenuBlock`
  - `SetInputContext(UI)` → mostra UI de pause → reações (resume, options, quit) → `EndThisBlock/StartAnotherBlock(retorna)`.

Observação: mesmo mantendo single‑active, o `PauseMenuBlock` pode substituir temporariamente o bloco corrente; o manager pode suportar `Branch` para ir ao `Pause` e depois ao bloco anterior (se futuro stack for desejado).

## Encapsulamento e Fronteira de Interferência
- Tudo que altera input, ativa/desativa objetos, inicia/termina animações ou timelines deve ser participante do bloco.
- Eventos externos que precisam influenciar o fluxo entram via canais padronizados:
  - Botões (`ButtonItem`) com `triggersEnd`.
  - Sinais da timeline (via `SignalReceiver`).
  - Triggers/Colliders expostos como `OnParticipantEvent("TriggerXYZ", sender)`.
- Fora do bloco: nenhum script deve alternar estado de objetos controlados pelo bloco durante sua execução.

## Integração com o Input System
- `ApplyInputContext(InputContext context)` deve:
  - UI: habilitar mapas de UI, desabilitar Gameplay.
  - Gameplay: habilitar mapas de Gameplay, desabilitar UI.
  - None: desabilitar ambos, exceto hotkeys globais (se houver).
- Recomenda-se um `InputRouter` único com API simples: `SetUI()`, `SetGameplay()`, `SetNone()`.

## Timeline e Sinais
- `PlayableDirector` inicia no `Enter` ou quando configurado.
- `director.stopped` e/ou sinais marcam progresso e podem participar da condição de término (`waitForDirectorEnd`).
- Sinais úteis: `SkipCinematic`, `PreparePlayerForGameplay`, `CinematicEnded`.

## Áudio e HUD (exemplos)
- Participantes adicionais:
  - `AudioItem` (bgm/ambiente) com `startOnEnter`, `stopOnExit` e `waitForEnd` (se fade‑out participar do término).
  - `HudItem` com animações de entrada/saída e flags de participação.

## Falhas e Deadlocks
- Use `TimeoutSeconds` em blocos suscetíveis a dependências externas (rede, asset streaming, timeline sem eventos).
- Qualquer participante faltando (`null`) deve logar com nível `Errors` e ser ignorado; o bloco só encerra se a política permitir.
- Evite polling agressivo: prefira eventos (`AnimationEvent`, `director.stopped`, `onClick`).

## Editor e Debug
- Inspector com seções claras, contador de participantes e marcação de quais participam do término.
- Botões: `Start`, `ForceEnd`, `Cancel`, `SimularEnd`.
- Níveis de log configuráveis por bloco.

## Helpers de API
```csharp
public static class LogicalBlockExtensions {
    public static IEnumerator StartBlockAndWait(this LogicalBlock b) {
        bool done = false;
        void Handler(LogicalBlock _) => done = true;
        b.OnBlockEnd += Handler;
        b.StartBlock();
        while (!done) yield return null;
        b.OnBlockEnd -= Handler;
    }
}
```
 
 ## Versão Assíncrona (UniTask)
 - Benefícios: composição limpa, integração com operações assíncronas reais (Addressables, I/O), sem `while (!done)`.
 ```csharp
 using System.Threading;
 using Cysharp.Threading.Tasks;
 
 public partial class LogicalBlock {
     public async UniTask StartBlockAndWaitAsync(CancellationToken ct) {
         OnBlockStart?.Invoke(this);
         ApplyInputContext(inputContext);
         await EnterPhaseAsync(ct);
         await SetupPhaseAsync(ct);
         await AwaitEndPhaseAsync(ct);
         Exit();
         OnBlockEnd?.Invoke(this);
     }
 
     // stubs
     UniTask EnterPhaseAsync(CancellationToken ct) => UniTask.CompletedTask;
     UniTask SetupPhaseAsync(CancellationToken ct) => UniTask.CompletedTask;
     UniTask AwaitEndPhaseAsync(CancellationToken ct) => UniTask.CompletedTask;
 }
 
 public class LogicalBlockManagerAsync : MonoBehaviour {
     public LogicalBlock initial;
     public BlockGraph graph;
 
     public async UniTask RunAsync(CancellationToken ct) {
         var current = initial;
         while (current != null && !ct.IsCancellationRequested) {
             await current.StartBlockAndWaitAsync(ct);
             // Supondo que o bloco emita um outcomeKey acessível
             var outcomeKey = current.LastOutcomeKey; // placeholder para ilustração
             current = graph.Resolve(current, outcomeKey);
         }
     }
 }
 ```
 
 Notas:
 - `CancellationToken` integra `CancelBlock()` e mudanças de cena.
 - Migrar para UniTask pode ficar para V3, mas a estrutura já está prevista.

---
Se aprovar esta arquitetura atualizada, prossigo gerando `LogicalBlock.cs` completo (com `Start/Setup/AwaitEnd/Exit`), o `LogicalBlockManager` single‑active, e um exemplo de configuração para os blocos: EmpresaNome, MainMenu, Loading, IntroCinematic e GamePlay.
## Exemplos Práticos

### Loading (em um único bloco)
- Objetivo: FadeIn → Loop enquanto carrega → FadeOut → outcome final `LoadingCompleted`.
- Setup:
  - `animations`: `id="FadeIn"`, `id="Loop"`, `id="FadeOut"`.
  - Sinal externo de “assets prontos” chama `flowBlock.PlayAnimationById("FadeOut")` e, ao final (via `AnimationEndHandler`), chama `flowBlock.FinishBlock("LoadingCompleted")`.
- Política: `EndMode.Any` com `waitForEnd` apenas no `FadeOut` (terminador). Os demais são intermediários rastreados via `OnIntermediateAction`.

### Main Menu (em um único bloco)
- Objetivo: mostrar UI, esperar clique; ao clicar `Start`, tocar animação de saída e encerrar com `StartLevelX`.
- Setup:
  - Botão `Start`: `triggersEnd = true`, `playAnimationOnClick = true`, `animatorOnClick = MainMenuAnimator`, `animationNameOnClick = "FadeOut"`, `waitAnimationOnClickEnd = true`, `outcomeAfterPlayedAnimation = "StartLevelX"`.
  - Botão `Options`: `triggersEnd = false`; usa `onClick` local/UnityEvent para abrir painel, e opcionalmente `PublishIntermediate("OptionsOpened")`.
- Resultado: `FlowBlock` encerra apenas após o fim da animação de saída do `Start`, com outcome `StartLevelX`.
### Animações: Auto vs Externo
- Somente animações com `playOnEnter = true` iniciam automaticamente na fase `Enter` do bloco.
- Animações sem `playOnEnter` não são disparadas pelo bloco; devem ser acionadas externamente via `UnityEvent` ou com `PlayAnimationById/AtIndex`.
- O término só participa do bloqueio se `waitForEnd = true`. Se uma animação marcada com `waitForEnd` nunca for disparada, o bloco pode ficar aguardando indefinidamente (use com atenção).
- Para maior precisão de término, recomenda‑se integrar `AnimationEndHandler`/`NotifyAnimationEndSMB` ao `Animator` e publicar intermediários (`PublishIntermediate("YourKey")`) ou acionar `FinishBlock` quando o evento indicar fim.